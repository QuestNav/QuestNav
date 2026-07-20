using System;
using System.Collections.Generic;
using MathNet.Filtering.Kalman;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using QuestNav.QuestNav.Geometry;
using QuestNav.Utils;

namespace QuestNav.QuestNav.Estimation
{
    /// <summary>Interface for a pose estimator that fuses VIO and AprilTag observations.</summary>
    public interface IVioAprilTagPoseEstimator
    {
        /// <summary>Returns the fused pose: KF-filtered translation with yaw-corrected VIO rotation.</summary>
        Pose3d EstimatedPose { get; }

        /// <summary>True after the first accepted AprilTag observation has aligned the VIO frame to FRC.</summary>
        bool HasInitialAlignment { get; }

        /// <summary>Hard-resets the estimator to a known pose, clearing all history.</summary>
        void ResetPosition(Pose3d pose, double timestamp);

        /// <summary>Predicts state forward using VIO displacement. Call at VIO rate (~120 Hz).</summary>
        void AddVioObservation(Pose3d vioPose, double timestamp);

        /// <summary>
        /// Applies a latency-compensated AprilTag position and yaw correction,
        /// subject to confidence-based gating.
        /// </summary>
        void AddAprilTagObservation(
            Translation3d measuredPosition,
            Rotation3d measuredRotation,
            double timestampSeconds,
            Matrix<double> stdDevs,
            int tagCount,
            double inlierRatio
        );

        /// <summary>
        /// Handles a VIO origin recenter (e.g., Quest logo long-press) by updating
        /// the VIO baseline without disturbing the KF state or field alignment.
        /// </summary>
        void HandleRecenter(Pose3d newVioPose, double timestamp);

        /// <summary>
        /// Updates the Phase-2 correction confidence preset. Tighter presets reject more
        /// observations but produce a more conservative pose; looser presets are useful
        /// when the default tuning never converges (e.g. only a single tag is visible).
        /// The Phase-1 inlier-ratio threshold is not affected.
        /// </summary>
        void SetConfidencePreset(ConfidencePreset preset);

        /// <summary>
        /// Sets the user-supplied "Minimum Tags Required" floor used by both Phase-1
        /// alignment and Phase-2 corrections. Phase-2 takes the max of this and the
        /// preset's tag count; Phase-1 uses this value directly.
        /// </summary>
        void SetMinimumTags(int minimumTags);
    }

    /// <summary>
    /// Fuses high-rate VIO pose with low-rate AprilTag position corrections using a Kalman filter
    /// with latency-compensated replay, similar to WPILib's SwerveDrivePoseEstimator.
    ///
    /// Operates in two phases:
    /// - Phase 1 (Unaligned): Accepts the first AprilTag observation that meets a minimum quality
    ///   bar to establish the yaw offset and initial position correction.
    /// - Phase 2 (Aligned): VIO is primary. Only high-confidence AprilTag corrections are applied.
    ///   Yaw offset is locked from Phase 1.
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
        private bool hasInitialAlignment;

        // Runtime-tunable Phase-2 correction thresholds. Initialized from the BALANCED
        // preset; SetConfidencePreset(...) overrides them at runtime in response to a
        // web-UI change.
        private int correctionMinTags = VioAprilTagPoseEstimatorConstants.PRESET_BALANCED_MIN_TAGS;
        private double correctionMinInlierRatio =
            VioAprilTagPoseEstimatorConstants.PRESET_BALANCED_MIN_INLIER_RATIO;

        /// <summary>
        /// User-supplied "Minimum Tags Required" floor. Phase-1 alignment uses this
        /// directly; Phase-2 corrections take the max of this and the preset's tag count.
        /// Initialized from the legacy hardcoded INITIAL_ALIGNMENT_MIN_TAGS so behavior
        /// matches the prior estimator until the web UI fires its first config event.
        /// </summary>
        private int userMinimumTags = VioAprilTagPoseEstimatorConstants.INITIAL_ALIGNMENT_MIN_TAGS;

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
            hasInitialAlignment = false;
        }

        /// <inheritdoc/>
        public Pose3d EstimatedPose
        {
            get
            {
                if (filter == null)
                    return Pose3d.Zero;
                var s = filter.State;
                var correctedRotation = latestRotation.RotateBy(new Rotation3d(0, 0, yawOffset));
                return new Pose3d(s[0, 0], s[1, 0], s[2, 0], correctedRotation);
            }
        }

        /// <inheritdoc/>
        public bool HasInitialAlignment => hasInitialAlignment;

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
            hasInitialAlignment = false;
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
            Matrix<double> stdDevs,
            int tagCount,
            double inlierRatio
        )
        {
            if (!initialized)
                return;

            // --- Phase 1: Initial alignment ---
            if (!hasInitialAlignment)
            {
                // Phase-1 tag count uses the user's "Minimum Tags Required" setting
                // (forwarded by AprilTagManager via SetMinimumTags). The inlier-ratio
                // threshold is intentionally still hardcoded (INITIAL_ALIGNMENT_MIN_INLIER_RATIO)
                // because a bad first observation mis-aligns the field origin for the
                // whole session, so a quality safety floor is non-negotiable.
                if (
                    tagCount < userMinimumTags
                    || inlierRatio
                        < VioAprilTagPoseEstimatorConstants.INITIAL_ALIGNMENT_MIN_INLIER_RATIO
                )
                {
                    QueuedLogger.Log(
                        $"AprilTag rejected (Phase 1): tags={tagCount}, inlierRatio={inlierRatio:F2} "
                            + $"(need >={userMinimumTags} tags, "
                            + $">={VioAprilTagPoseEstimatorConstants.INITIAL_ALIGNMENT_MIN_INLIER_RATIO:F2} inlier ratio)"
                    );
                    return;
                }

                // Accept: hard-set KF state to measured position, set yaw, transition to Phase 2.
                // We bypass ApplyKfUpdate here because the KF was initialized with an arbitrary
                // VIO origin and tight covariance (P=0.001*I), so a standard KF update would
                // barely move the state. Instead, directly reset to the measured position.
                var x0 = ToColumnVector(measuredPosition);
                var p0 = DenseMatrix.CreateDiagonal(3, 3, i => stdDevs[i, 0] * stdDevs[i, 0]);
                filter = new DiscreteKalmanFilter(x0, p0);

                snapshotBuffer.Clear();
                snapshotBuffer.AddLast(
                    new VIOSnapshot(
                        timestampSeconds,
                        previousVioPosition,
                        latestRotation,
                        x0.Clone()
                    )
                );

                ApplyYawCorrection(measuredRotation, timestampSeconds);
                hasInitialAlignment = true;
                QueuedLogger.Log(
                    $"AprilTag initial alignment accepted: tags={tagCount}, inlierRatio={inlierRatio:F2}, "
                        + $"pos=({measuredPosition.X:F3}, {measuredPosition.Y:F3}, {measuredPosition.Z:F3})"
                );
                return;
            }

            // --- Phase 2: VIO-primary with selective corrections ---

            // Sanity check: reject large position jumps (likely reflections)
            var s = filter.State;
            double dx = measuredPosition.X - s[0, 0];
            double dy = measuredPosition.Y - s[1, 0];
            double dz = measuredPosition.Z - s[2, 0];
            double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            if (distance > VioAprilTagPoseEstimatorConstants.CORRECTION_MAX_POSITION_JUMP)
            {
                QueuedLogger.Log(
                    $"AprilTag rejected (Phase 2): position jump {distance:F2}m > "
                        + $"{VioAprilTagPoseEstimatorConstants.CORRECTION_MAX_POSITION_JUMP:F1}m limit"
                );
                return;
            }

            // Phase-2 takes the max of the user's "Minimum Tags Required" and the
            // confidence preset's tag count. The user's setting is a HARD FLOOR; the
            // preset cannot loosen it below what the user explicitly chose.
            int effectiveMinTags = Math.Max(userMinimumTags, correctionMinTags);
            if (tagCount < effectiveMinTags || inlierRatio < correctionMinInlierRatio)
            {
                QueuedLogger.Log(
                    $"AprilTag rejected (Phase 2): tags={tagCount}, inlierRatio={inlierRatio:F2} "
                        + $"(need >={effectiveMinTags} tags, "
                        + $">={correctionMinInlierRatio:F2} inlier ratio)"
                );
                return;
            }

            // High-confidence correction — update position only, yaw is locked from Phase 1
            ApplyKfUpdate(measuredPosition, stdDevs, timestampSeconds);
        }

        /// <inheritdoc/>
        public void SetConfidencePreset(ConfidencePreset preset)
        {
            switch (preset)
            {
                case ConfidencePreset.Permissive:
                    correctionMinTags =
                        VioAprilTagPoseEstimatorConstants.PRESET_PERMISSIVE_MIN_TAGS;
                    correctionMinInlierRatio =
                        VioAprilTagPoseEstimatorConstants.PRESET_PERMISSIVE_MIN_INLIER_RATIO;
                    break;
                case ConfidencePreset.Strict:
                    correctionMinTags = VioAprilTagPoseEstimatorConstants.PRESET_STRICT_MIN_TAGS;
                    correctionMinInlierRatio =
                        VioAprilTagPoseEstimatorConstants.PRESET_STRICT_MIN_INLIER_RATIO;
                    break;
                case ConfidencePreset.Debug:
                    correctionMinTags = VioAprilTagPoseEstimatorConstants.PRESET_DEBUG_MIN_TAGS;
                    correctionMinInlierRatio =
                        VioAprilTagPoseEstimatorConstants.PRESET_DEBUG_MIN_INLIER_RATIO;
                    break;
                case ConfidencePreset.Balanced:
                default:
                    correctionMinTags = VioAprilTagPoseEstimatorConstants.PRESET_BALANCED_MIN_TAGS;
                    correctionMinInlierRatio =
                        VioAprilTagPoseEstimatorConstants.PRESET_BALANCED_MIN_INLIER_RATIO;
                    break;
            }
            QueuedLogger.Log(
                $"AprilTag confidence preset set to {preset}: "
                    + $"tags>={correctionMinTags}, inlierRatio>={correctionMinInlierRatio:F2}"
            );
        }

        /// <inheritdoc/>
        public void SetMinimumTags(int minimumTags)
        {
            // Refuse zero or negative; the estimator cannot work with no observations.
            int sanitized = minimumTags < 1 ? 1 : minimumTags;
            if (userMinimumTags == sanitized)
            {
                return;
            }
            userMinimumTags = sanitized;
            QueuedLogger.Log(
                $"AprilTag minimum tags set to {sanitized} "
                    + "(applied to Phase 1 alignment and as a Phase 2 floor)"
            );
        }

        /// <inheritdoc/>
        public void HandleRecenter(Pose3d newVioPose, double timestamp)
        {
            if (!initialized)
                return;

            previousVioPosition = newVioPose.Translation;
            latestRotation = newVioPose.Rotation;

            snapshotBuffer.Clear();
            snapshotBuffer.AddLast(
                new VIOSnapshot(
                    timestamp,
                    newVioPose.Translation,
                    newVioPose.Rotation,
                    filter.State.Clone()
                )
            );

            QueuedLogger.LogWarning("VIO recenter detected — estimator VIO baseline reset");
        }

        /// <summary>Applies a KF measurement update with latency-compensated replay.</summary>
        private void ApplyKfUpdate(
            Translation3d measuredPosition,
            Matrix<double> stdDevs,
            double timestampSeconds
        )
        {
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
                    Translation3d displacement = rawDisplacement.RotateBy(
                        new Rotation3d(0, 0, yawOffset)
                    );

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
        }

        /// <summary>Computes and sets the yaw offset from the AprilTag-measured rotation.</summary>
        private void ApplyYawCorrection(Rotation3d measuredRotation, double timestampSeconds)
        {
            if (measuredRotation == null)
                return;

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

            double measuredYaw = measuredRotation.Z;
            Rotation3d vioRotationAtCapture =
                (bestNode != null) ? bestNode.Value.Rotation : latestRotation;
            double vioYaw = vioRotationAtCapture.Z;
            yawOffset = NormalizeAngle(measuredYaw - vioYaw);
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
