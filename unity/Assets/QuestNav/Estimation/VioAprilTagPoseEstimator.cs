using System;
using System.Collections.Generic;
using MathNet.Filtering.Kalman;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using QuestNav.QuestNav.Geometry;

namespace QuestNav.QuestNav.Estimation
{
    /// <summary>Interface for a pose estimator that fuses VIO and AprilTag observations.</summary>
    public interface IVioAprilTagPoseEstimator
    {
        /// <summary>Returns the fused pose: KF-filtered translation with yaw-corrected VIO rotation.</summary>
        Pose3d EstimatedPose { get; }

        /// <summary>Hard-resets the estimator to a known pose, clearing all history.</summary>
        void ResetPosition(Pose3d pose, double timestamp);

        /// <summary>Predicts state forward using VIO displacement. Call at VIO rate (~120 Hz).</summary>
        void AddVioObservation(Pose3d vioPose, double timestamp);

        /// <summary>Applies a latency-compensated AprilTag position and yaw correction.</summary>
        void AddAprilTagObservation(
            Translation3d measuredPosition,
            Rotation3d measuredRotation,
            double timestampSeconds,
            Matrix<double> stdDevs
        );
    }

    /// <summary>
    /// Fuses high-rate VIO pose with low-rate AprilTag position corrections using a Kalman filter
    /// with latency-compensated replay, similar to WPILib's SwerveDrivePoseEstimator.
    /// Yaw is corrected via an offset computed from the AprilTag-measured rotation.
    /// </summary>
    public class VioAprilTagPoseEstimator : IVioAprilTagPoseEstimator
    {
        private struct VIOSnapshot
        {
            public double Timestamp;
            public Translation3d Position;
            public Rotation3d Rotation;
            public Matrix<double> EstimatedState;

            public VIOSnapshot(
                double timestamp,
                Translation3d position,
                Rotation3d rotation,
                Matrix<double> estimatedState
            )
            {
                Timestamp = timestamp;
                Position = position;
                Rotation = rotation;
                EstimatedState = estimatedState;
            }
        }

        private readonly LinkedList<VIOSnapshot> snapshotBuffer = new LinkedList<VIOSnapshot>();
        private readonly double bufferDuration;

        private readonly Matrix<double> f;
        private readonly Matrix<double> h;
        private readonly Matrix<double> q;

        private DiscreteKalmanFilter filter;
        private Translation3d previousVioPosition;
        private Rotation3d latestRotation;
        private double yawOffset;
        private bool initialized;

        /// <summary>Creates a new estimator with optional noise tuning parameters.</summary>
        public VioAprilTagPoseEstimator(
            Matrix<double> vioStdDevs = null,
            double bufferDurationSeconds = VioAprilTagPoseEstimatorConstants.BUFFER_DURATION_SECONDS
        )
        {
            Matrix<double> qStdDev =
                vioStdDevs
                ?? DenseMatrix.OfArray(
                    new[,]
                    {
                        { VioAprilTagPoseEstimatorConstants.defaultVioStdDevs[0] },
                        { VioAprilTagPoseEstimatorConstants.defaultVioStdDevs[1] },
                        { VioAprilTagPoseEstimatorConstants.defaultVioStdDevs[2] },
                    }
                );

            bufferDuration = bufferDurationSeconds;

            f = DenseMatrix.CreateIdentity(3);
            h = DenseMatrix.CreateIdentity(3);

            q = DenseMatrix.CreateDiagonal(3, 3, i => qStdDev[i, 0] * qStdDev[i, 0]);

            latestRotation = Rotation3d.Zero;
            yawOffset = 0.0;
            initialized = false;
        }

        /// <inheritdoc/>
        public Pose3d EstimatedPose
        {
            get
            {
                if (filter == null)
                    return Pose3d.Zero;
                var s = filter.State;
                var correctedRotation = latestRotation.RotateBy(
                    new Rotation3d(0, 0, yawOffset)
                );
                return new Pose3d(s[0, 0], s[1, 0], s[2, 0], correctedRotation);
            }
        }

        /// <summary>Converts a Translation3d to a 3x1 column matrix for the Kalman filter.</summary>
        private static Matrix<double> ToColumnVector(Translation3d t)
        {
            return DenseMatrix.OfArray(
                new[,]
                {
                    { t.X },
                    { t.Y },
                    { t.Z },
                }
            );
        }

        /// <summary>Builds a diagonal R matrix from a 3x1 standard deviation column vector.</summary>
        private static Matrix<double> StdDevsToR(Matrix<double> stdDevs)
        {
            return DenseMatrix.CreateDiagonal(3, 3, i => stdDevs[i, 0] * stdDevs[i, 0]);
        }

        /// <summary>Wraps an angle to the range [-π, π].</summary>
        private static double NormalizeAngle(double angle)
        {
            angle = ((angle % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);
            return angle > Math.PI ? angle - 2 * Math.PI : angle;
        }

        /// <inheritdoc/>
        public void ResetPosition(Pose3d pose, double timestamp)
        {
            var x0 = ToColumnVector(pose.Translation);
            var p0 = DenseMatrix.CreateIdentity(3) * 0.001;

            filter = new DiscreteKalmanFilter(x0, p0);
            previousVioPosition = pose.Translation;
            latestRotation = pose.Rotation;
            yawOffset = 0.0;
            initialized = true;

            snapshotBuffer.Clear();
            snapshotBuffer.AddLast(
                new VIOSnapshot(timestamp, pose.Translation, pose.Rotation, x0.Clone())
            );
        }

        /// <inheritdoc/>
        public void AddVioObservation(Pose3d vioPose, double timestamp)
        {
            if (!initialized)
            {
                ResetPosition(vioPose, timestamp);
                return;
            }

            Translation3d rawDisplacement = vioPose.Translation - previousVioPosition;
            previousVioPosition = vioPose.Translation;
            latestRotation = vioPose.Rotation;

            // Rotate displacement from VIO frame to corrected FRC frame.
            // UnityToFrc3d produces a fixed axis mapping that depends on headset
            // orientation at startup. The yawOffset aligns VIO north with true FRC north.
            Translation3d displacement = rawDisplacement.RotateBy(new Rotation3d(0, 0, yawOffset));

            filter.Predict(f, q);

            var state = filter.State;
            var corrected = state + ToColumnVector(displacement);
            filter = new DiscreteKalmanFilter(corrected, filter.Cov.Clone());

            PruneBuffer(timestamp);
            snapshotBuffer.AddLast(
                new VIOSnapshot(timestamp, vioPose.Translation, vioPose.Rotation, corrected.Clone())
            );
        }

        /// <inheritdoc/>
        public void AddAprilTagObservation(
            Translation3d measuredPosition,
            Rotation3d measuredRotation,
            double timestampSeconds,
            Matrix<double> stdDevs
        )
        {
            if (!initialized)
                return;

            // --- Position correction via KF ---
            var z = ToColumnVector(measuredPosition);
            var R = StdDevsToR(stdDevs);

            LinkedListNode<VIOSnapshot> bestNode = null;
            var node = snapshotBuffer.Last;
            while (node != null)
            {
                if (node.Value.Timestamp <= timestampSeconds)
                {
                    bestNode = node;
                    break;
                }
                node = node.Previous;
            }

            if (bestNode == null)
            {
                filter.Update(z, h, R);
            }
            else
            {
                var snapshot = bestNode.Value;
                filter = new DiscreteKalmanFilter(
                    snapshot.EstimatedState.Clone(),
                    filter.Cov.Clone()
                );

                filter.Update(z, h, R);

                var replayNode = bestNode.Next;
                var prevReplayNode = bestNode;

                while (replayNode != null)
                {
                    Translation3d rawDisplacement =
                        replayNode.Value.Position - prevReplayNode.Value.Position;
                    Translation3d displacement = rawDisplacement.RotateBy(new Rotation3d(0, 0, yawOffset));

                    filter.Predict(f, q);

                    var state = filter.State;
                    var corrected = state + ToColumnVector(displacement);
                    filter = new DiscreteKalmanFilter(corrected, filter.Cov.Clone());

                    replayNode.Value = new VIOSnapshot(
                        replayNode.Value.Timestamp,
                        replayNode.Value.Position,
                        replayNode.Value.Rotation,
                        corrected.Clone()
                    );

                    prevReplayNode = replayNode;
                    replayNode = replayNode.Next;
                }
            }

            // --- Yaw correction via offset ---
            if (measuredRotation != null)
            {
                double measuredYaw = measuredRotation.Z;
                Rotation3d vioRotationAtCapture = (bestNode != null)
                    ? bestNode.Value.Rotation
                    : latestRotation;
                double vioYaw = vioRotationAtCapture.Z;
                yawOffset = NormalizeAngle(measuredYaw - vioYaw);
            }
        }

        /// <summary>Removes snapshots older than the buffer duration, keeping at least one.</summary>
        private void PruneBuffer(double currentTimestamp)
        {
            double cutoff = currentTimestamp - bufferDuration;
            while (snapshotBuffer.First != null && snapshotBuffer.First.Value.Timestamp < cutoff)
            {
                if (snapshotBuffer.Count <= 1)
                    break;
                snapshotBuffer.RemoveFirst();
            }
        }
    }
}
