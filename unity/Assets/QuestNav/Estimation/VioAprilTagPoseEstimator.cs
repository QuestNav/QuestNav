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

        /// <summary>
        /// Hard-resets the estimator to a known pose, clearing all history and returning to
        /// Phase 1. A non-finite pose is ignored.
        /// </summary>
        void ResetPosition(Pose3d pose, double timestamp);

        /// <summary>
        /// Predicts state forward using VIO displacement. Call at VIO rate (~120 Hz).
        /// Non-finite samples are ignored.
        /// </summary>
        void AddVioObservation(Pose3d vioPose, double timestamp);

        /// <summary>
        /// Applies an AprilTag observation, subject to confidence-based gating. In Phase 1 it
        /// aligns the VIO frame to the field (position and yaw); in Phase 2 it applies a
        /// latency-compensated position correction if it passes the outlier gate.
        /// Observations with non-finite values or non-positive std devs are ignored, as are
        /// captures older than the snapshot buffer or not covered by recent VIO.
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
        /// Handles a VIO origin recenter (e.g., Quest logo long-press). The next VIO
        /// observation is taken as the new baseline: its jump is not integrated, and the yaw
        /// offset is shifted so the published field heading and the direction of later
        /// travel are unchanged. The KF state and alignment phase are kept.
        /// </summary>
        void HandleRecenter();

        /// <summary>
        /// Marks a VIO tracking outage; stop calling <see cref="AddVioObservation"/> until
        /// tracking is back. AprilTag observations are ignored until then. The jump to the
        /// first VIO observation after the outage is integrated when the robot could have
        /// driven it in that time; a larger jump is a relocalization: it is not integrated,
        /// and the estimator drops back to Phase 1 so the next tags re-anchor it.
        /// </summary>
        void HandleTrackingLost();

        /// <summary>
        /// Gets the raw VIO pose at <paramref name="timestamp"/> (the newest snapshot at or
        /// before it, or the oldest one kept when it is older than the buffer) and the latest
        /// raw VIO pose, to latency-compensate a pose reset. Returns false when there is no
        /// consistent VIO history: before the first observation, while tracking is lost, or
        /// while a recenter is pending.
        /// </summary>
        bool TryGetVioMotionSince(
            double timestamp,
            out Pose3d vioAtTimestamp,
            out Pose3d latestVio
        );

        /// <summary>
        /// Updates the Phase-2 correction confidence preset. Tighter presets reject more
        /// observations but produce a more conservative pose; looser presets are useful
        /// when the default tuning never converges (e.g. only a single tag is visible).
        /// The initial Phase-1 thresholds are not affected; a re-align after consistent
        /// gate rejections must meet both.
        /// </summary>
        void SetConfidencePreset(ConfidencePreset preset);

        /// <summary>
        /// Sets the user-supplied "Minimum Tags Required" floor used by both Phase-1
        /// alignment and Phase-2 corrections. Phase-2 takes the max of this and the
        /// preset's tag count, as does a re-align; the initial Phase-1 alignment uses this
        /// value directly.
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
    /// - Phase 2 (Aligned): VIO is primary. Only high-confidence AprilTag corrections that pass
    ///   a Mahalanobis outlier gate are applied. The yaw offset is kept from Phase 1 and only
    ///   re-based on a VIO recenter. A run of consistent rejected multi-tag observations drops
    ///   back to Phase 1 to re-align, which then needs an observation that also meets the
    ///   Phase-2 confidence preset.
    /// </summary>
    public class VioAprilTagPoseEstimator : IVioAprilTagPoseEstimator
    {
        private struct VIOSnapshot
        {
            public double Timestamp;
            public Translation3d Position;
            public Rotation3d Rotation;
            public Matrix<double> EstimatedState;
            public Matrix<double> Covariance;

            // Elapsed time process noise was added for on the step into this snapshot
            public double NoiseSeconds;

            public VIOSnapshot(
                double timestamp,
                Translation3d position,
                Rotation3d rotation,
                Matrix<double> estimatedState,
                Matrix<double> covariance,
                double noiseSeconds
            )
            {
                Timestamp = timestamp;
                Position = position;
                Rotation = rotation;
                EstimatedState = estimatedState;
                Covariance = covariance;
                NoiseSeconds = noiseSeconds;
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
        private bool recenterPending;
        private bool trackingLost;

        // Set when consistent gate rejections dropped back to Phase 1; the re-align then
        // needs a Phase-2 quality observation, since it also resets the heading.
        private bool realignPending;

        // Consecutive gate-rejected multi-tag observations whose innovations agree with
        // rejectionStreakInnovation, the first of the run.
        private int rejectionStreakCount;
        private Translation3d rejectionStreakInnovation;

        private int nonFiniteInputCount;
        private int uncompensatedObservationCount;

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
            Matrix<double> qStdDev = vioStdDevs
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

        private static bool IsFinite(Translation3d t)
        {
            return t != null
                && double.IsFinite(t.X)
                && double.IsFinite(t.Y)
                && double.IsFinite(t.Z);
        }

        private static bool IsFinite(Rotation3d r)
        {
            var q = r?.Quaternion;
            return q != null
                && double.IsFinite(q.W)
                && double.IsFinite(q.X)
                && double.IsFinite(q.Y)
                && double.IsFinite(q.Z);
        }

        private static bool IsFinite(Pose3d pose)
        {
            return pose != null && IsFinite(pose.Translation) && IsFinite(pose.Rotation);
        }

        /// <summary>True for a 3x1 matrix of finite, strictly positive std devs.</summary>
        private static bool IsValidStdDevs(Matrix<double> stdDevs)
        {
            if (stdDevs == null || stdDevs.RowCount != 3 || stdDevs.ColumnCount != 1)
                return false;
            for (int i = 0; i < 3; i++)
            {
                if (!double.IsFinite(stdDevs[i, 0]) || stdDevs[i, 0] <= 0)
                    return false;
            }
            return true;
        }

        /// <summary>Logs a dropped input, rate-limited so a bad stream cannot flood the log.</summary>
        private void LogNonFiniteInput(string source)
        {
            LogRateLimited(
                ref nonFiniteInputCount,
                $"Ignored {source} with non-finite or invalid values"
            );
        }

        /// <summary>Logs the first of a kind of dropped input, then every Nth one.</summary>
        private static void LogRateLimited(ref int count, string message)
        {
            count++;
            if ((count - 1) % VioAprilTagPoseEstimatorConstants.NON_FINITE_LOG_INTERVAL == 0)
                QueuedLogger.LogWarning($"{message} ({count} ignored so far)");
        }

        /// <summary>
        /// Elapsed time to add process noise for on a VIO step of <paramref name="dt"/>
        /// seconds. An ordinary step is clamped so a pause or app hitch does not inflate the
        /// covariance; the step across a tracking outage is not, since the VIO pose after
        /// it really is less certain.
        /// </summary>
        private static double NoiseSeconds(double dt, bool acrossOutage)
        {
            return acrossOutage
                ? Math.Max(dt, 0.0)
                : Math.Clamp(dt, 0.0, VioAprilTagPoseEstimatorConstants.VIO_NOISE_MAX_DT_SECONDS);
        }

        /// <summary>Process noise for <paramref name="noiseSeconds"/> of elapsed time.</summary>
        private Matrix<double> ProcessNoise(double noiseSeconds)
        {
            return q
                * (
                    noiseSeconds
                    / VioAprilTagPoseEstimatorConstants.VIO_NOISE_REFERENCE_PERIOD_SECONDS
                );
        }

        /// <inheritdoc/>
        public void ResetPosition(Pose3d pose, double timestamp)
        {
            if (!IsFinite(pose) || !double.IsFinite(timestamp))
            {
                LogNonFiniteInput("pose reset");
                return;
            }

            var x0 = ToColumnVector(pose.Translation);
            var p0 = DenseMatrix.CreateIdentity(3) * 0.001;

            filter = new DiscreteKalmanFilter(x0, p0);
            previousVioPosition = pose.Translation;
            latestRotation = pose.Rotation;
            yawOffset = 0.0;
            hasInitialAlignment = false;
            initialized = true;
            recenterPending = false;
            realignPending = false;
            rejectionStreakCount = 0;

            snapshotBuffer.Clear();
            snapshotBuffer.AddLast(
                new VIOSnapshot(
                    timestamp,
                    pose.Translation,
                    pose.Rotation,
                    x0.Clone(),
                    p0.Clone(),
                    0.0
                )
            );
        }

        /// <inheritdoc/>
        public void AddVioObservation(Pose3d vioPose, double timestamp)
        {
            if (!IsFinite(vioPose) || !double.IsFinite(timestamp))
            {
                LogNonFiniteInput("VIO observation");
                return;
            }

            if (!initialized)
            {
                ResetPosition(vioPose, timestamp);
                return;
            }

            if (recenterPending)
            {
                RebaseAfterRecenter(vioPose);
                recenterPending = false;
            }

            Translation3d rawDisplacement = vioPose.Translation - previousVioPosition;
            double dt = timestamp - snapshotBuffer.Last.Value.Timestamp;
            previousVioPosition = vioPose.Translation;
            latestRotation = vioPose.Rotation;

            bool acrossOutage = trackingLost;
            if (acrossOutage)
            {
                trackingLost = false;
                double maxJump =
                    VioAprilTagPoseEstimatorConstants.MAX_ROBOT_SPEED_MPS * Math.Max(dt, 0.0)
                    + VioAprilTagPoseEstimatorConstants.TRACKING_JUMP_MARGIN_METERS;
                if (rawDisplacement.Norm > maxJump)
                {
                    RebaseAfterRelocalization(vioPose, timestamp, dt, rawDisplacement.Norm);
                    return;
                }
            }

            // Rotate displacement from VIO frame to corrected FRC frame.
            // UnityToFrc3d produces a fixed axis mapping that depends on headset
            // orientation at startup. The yawOffset aligns VIO north with true FRC north.
            Translation3d displacement = rawDisplacement.RotateBy(new Rotation3d(0, 0, yawOffset));

            double noiseSeconds = NoiseSeconds(dt, acrossOutage);
            filter.Predict(f, ProcessNoise(noiseSeconds));

            var state = filter.State;
            var corrected = state + ToColumnVector(displacement);
            filter = new DiscreteKalmanFilter(corrected, filter.Cov.Clone());

            PruneBuffer(timestamp);
            snapshotBuffer.AddLast(
                new VIOSnapshot(
                    timestamp,
                    vioPose.Translation,
                    vioPose.Rotation,
                    corrected.Clone(),
                    filter.Cov.Clone(),
                    noiseSeconds
                )
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

            if (
                !IsFinite(measuredPosition)
                || (measuredRotation != null && !IsFinite(measuredRotation))
                || !double.IsFinite(timestampSeconds)
                || !IsValidStdDevs(stdDevs)
                || !double.IsFinite(inlierRatio)
            )
            {
                LogNonFiniteInput("AprilTag observation");
                return;
            }

            if (trackingLost)
            {
                LogRateLimited(
                    ref uncompensatedObservationCount,
                    "Ignored AprilTag observation: VIO tracking is lost"
                );
                return;
            }

            // The measurement is from capture time, so it is fused against the VIO snapshot
            // from then and the VIO motion since is replayed on top
            var captureNode = FindCaptureSnapshot(timestampSeconds);
            if (captureNode == null)
            {
                LogRateLimited(
                    ref uncompensatedObservationCount,
                    $"Ignored AprilTag observation captured at {timestampSeconds:F3}s: "
                        + "no VIO history to latency-compensate it against"
                );
                return;
            }

            // --- Phase 1: Initial alignment ---
            if (!hasInitialAlignment)
            {
                // Phase-1 tag count uses the user's "Minimum Tags Required" setting
                // (forwarded by AprilTagManager via SetMinimumTags). The inlier-ratio
                // threshold is intentionally still hardcoded (INITIAL_ALIGNMENT_MIN_INLIER_RATIO)
                // because a bad first observation mis-aligns the field origin for the
                // whole session, so a quality safety floor is non-negotiable.
                int alignMinTags = userMinimumTags;
                double alignMinInlierRatio =
                    VioAprilTagPoseEstimatorConstants.INITIAL_ALIGNMENT_MIN_INLIER_RATIO;
                if (realignPending)
                {
                    alignMinTags = Math.Max(alignMinTags, correctionMinTags);
                    alignMinInlierRatio = Math.Max(alignMinInlierRatio, correctionMinInlierRatio);
                }

                if (tagCount < alignMinTags || inlierRatio < alignMinInlierRatio)
                {
                    QueuedLogger.Log(
                        $"AprilTag rejected (Phase 1): tags={tagCount}, inlierRatio={inlierRatio:F2} "
                            + $"(need >={alignMinTags} tags, "
                            + $">={alignMinInlierRatio:F2} inlier ratio)"
                    );
                    return;
                }

                // Accept: hard-set KF state to measured position, set yaw, transition to Phase 2.
                // We bypass ApplyKfUpdate here because the KF was initialized with an arbitrary
                // VIO origin and tight covariance (P=0.001*I), so a standard KF update would
                // barely move the state. Instead, directly reset to the measured position.
                var x0 = ToColumnVector(measuredPosition);
                var p0 = StdDevsToR(stdDevs);
                filter = new DiscreteKalmanFilter(x0, p0);

                ApplyYawCorrection(measuredRotation, captureNode.Value.Rotation);

                // Older snapshots hold pre-alignment states that replay must never restore
                while (snapshotBuffer.First != captureNode)
                    snapshotBuffer.RemoveFirst();

                var capture = captureNode.Value;
                captureNode.Value = new VIOSnapshot(
                    capture.Timestamp,
                    capture.Position,
                    capture.Rotation,
                    x0.Clone(),
                    p0.Clone(),
                    capture.NoiseSeconds
                );
                ReplayFrom(captureNode);

                hasInitialAlignment = true;
                realignPending = false;
                rejectionStreakCount = 0;
                QueuedLogger.Log(
                    $"AprilTag initial alignment accepted: tags={tagCount}, inlierRatio={inlierRatio:F2}, "
                        + $"pos=({measuredPosition.X:F3}, {measuredPosition.Y:F3}, {measuredPosition.Z:F3})"
                );
                return;
            }

            // --- Phase 2: VIO-primary with selective corrections ---

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

            if (
                !PassesOutlierGate(measuredPosition, stdDevs, captureNode.Value, out var innovation)
            )
            {
                TrackGateRejection(innovation, tagCount);
                return;
            }

            // High-confidence correction — update position only, yaw is kept from Phase 1
            rejectionStreakCount = 0;
            ApplyKfUpdate(measuredPosition, stdDevs, captureNode);
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
            userMinimumTags = sanitized;
            QueuedLogger.Log(
                $"AprilTag minimum tags set to {sanitized} "
                    + "(applied to Phase 1 alignment and as a Phase 2 floor)"
            );
        }

        /// <inheritdoc/>
        public void HandleRecenter()
        {
            if (!initialized)
                return;

            // The camera anchors may not reflect the recenter yet when the event fires, so
            // the re-base waits for the next VIO observation.
            recenterPending = true;
            QueuedLogger.LogWarning("VIO recenter detected; re-basing on the next VIO frame");
        }

        /// <inheritdoc/>
        public bool TryGetVioMotionSince(
            double timestamp,
            out Pose3d vioAtTimestamp,
            out Pose3d latestVio
        )
        {
            vioAtTimestamp = null;
            latestVio = null;
            if (!initialized || trackingLost || recenterPending || !double.IsFinite(timestamp))
                return false;

            var node = FindSnapshotAtOrBefore(timestamp) ?? snapshotBuffer.First;
            vioAtTimestamp = new Pose3d(node.Value.Position, node.Value.Rotation);
            latestVio = new Pose3d(previousVioPosition, latestRotation);
            return true;
        }

        /// <inheritdoc/>
        public void HandleTrackingLost()
        {
            if (!initialized)
                return;

            trackingLost = true;
        }

        /// <summary>
        /// Re-bases onto the first VIO pose after a tracking outage whose jump is too large
        /// to be robot motion. The estimate holds its position instead of integrating the
        /// jump, the history from before the jump is dropped, and an aligned estimator drops
        /// back to Phase 1 so the next tags re-anchor position and heading.
        /// </summary>
        private void RebaseAfterRelocalization(
            Pose3d newVioPose,
            double timestamp,
            double outageSeconds,
            double jumpMeters
        )
        {
            filter.Predict(f, ProcessNoise(NoiseSeconds(outageSeconds, true)));
            snapshotBuffer.Clear();
            snapshotBuffer.AddLast(
                new VIOSnapshot(
                    timestamp,
                    newVioPose.Translation,
                    newVioPose.Rotation,
                    filter.State.Clone(),
                    filter.Cov.Clone(),
                    0.0
                )
            );

            if (hasInitialAlignment)
            {
                hasInitialAlignment = false;
                realignPending = true;
            }
            rejectionStreakCount = 0;

            QueuedLogger.LogWarning(
                $"VIO jumped {jumpMeters:F2}m over a {outageSeconds:F2}s tracking outage, "
                    + "more than the robot can drive; not integrating it, re-aligning on the "
                    + "next AprilTag observation"
            );
        }

        /// <summary>
        /// Re-bases onto the first VIO pose after a recenter. The recenter rotates the VIO
        /// frame about the vertical axis and moves its origin; the snapshot history is
        /// re-expressed in the new frame and the yaw offset absorbs the rotation, so the
        /// recenter jump is never integrated as motion.
        /// </summary>
        private void RebaseAfterRecenter(Pose3d newVioPose)
        {
            double frameYaw = NormalizeAngle(newVioPose.Rotation.Z - latestRotation.Z);
            var frameRotation = new Rotation3d(0, 0, frameYaw);
            Translation3d oldVioPosition = previousVioPosition;

            for (var node = snapshotBuffer.First; node != null; node = node.Next)
            {
                var snapshot = node.Value;
                node.Value = new VIOSnapshot(
                    snapshot.Timestamp,
                    (snapshot.Position - oldVioPosition).RotateBy(frameRotation)
                        + newVioPose.Translation,
                    snapshot.Rotation.RotateBy(frameRotation),
                    snapshot.EstimatedState,
                    snapshot.Covariance,
                    snapshot.NoiseSeconds
                );
            }

            yawOffset = NormalizeAngle(yawOffset - frameYaw);
            previousVioPosition = newVioPose.Translation;
            latestRotation = newVioPose.Rotation;

            QueuedLogger.LogWarning(
                $"VIO recenter applied: VIO yaw moved {frameYaw * 180.0 / Math.PI:F1} deg, "
                    + "field heading kept"
            );
        }

        /// <summary>
        /// Checks the measurement against the estimate at capture time. It is accepted within
        /// the minimum distance; beyond it, it must also pass the squared Mahalanobis distance
        /// (S = P + R, both at capture time) and the absolute distance cap.
        /// </summary>
        private bool PassesOutlierGate(
            Translation3d measuredPosition,
            Matrix<double> stdDevs,
            VIOSnapshot capture,
            out Translation3d innovation
        )
        {
            var nu = ToColumnVector(measuredPosition) - capture.EstimatedState;
            innovation = new Translation3d(nu[0, 0], nu[1, 0], nu[2, 0]);

            var innovationCov = capture.Covariance + StdDevsToR(stdDevs);
            double mahalanobisSquared = (nu.Transpose() * innovationCov.Solve(nu))[0, 0];
            double distance = innovation.Norm;

            if (
                distance <= VioAprilTagPoseEstimatorConstants.CORRECTION_GATE_MIN_DISTANCE
                || (
                    mahalanobisSquared
                        <= VioAprilTagPoseEstimatorConstants.CORRECTION_GATE_CHI_SQUARED
                    && distance <= VioAprilTagPoseEstimatorConstants.CORRECTION_MAX_POSITION_JUMP
                )
            )
                return true;

            QueuedLogger.Log(
                $"AprilTag rejected (Phase 2): {distance:F2}m from estimate, "
                    + $"Mahalanobis^2={mahalanobisSquared:F1} (limits "
                    + $"{VioAprilTagPoseEstimatorConstants.CORRECTION_GATE_CHI_SQUARED:F1}, "
                    + $"{VioAprilTagPoseEstimatorConstants.CORRECTION_MAX_POSITION_JUMP:F1}m)"
            );
            return false;
        }

        /// <summary>
        /// Counts consecutive gate rejections of multi-tag observations that agree with each
        /// other. Enough of them mean the estimate, not the tags, is wrong, so the estimator
        /// drops back to Phase 1 and re-aligns on the next accepted observation.
        /// </summary>
        private void TrackGateRejection(Translation3d innovation, int tagCount)
        {
            if (tagCount < VioAprilTagPoseEstimatorConstants.REALIGN_MIN_TAGS)
                return;

            if (
                rejectionStreakCount == 0
                || innovation.GetDistance(rejectionStreakInnovation)
                    > VioAprilTagPoseEstimatorConstants.REALIGN_CONSENSUS_RADIUS
            )
            {
                rejectionStreakInnovation = innovation;
                rejectionStreakCount = 1;
            }
            else
            {
                rejectionStreakCount++;
            }

            if (rejectionStreakCount < VioAprilTagPoseEstimatorConstants.REALIGN_REJECTION_COUNT)
                return;

            rejectionStreakCount = 0;
            hasInitialAlignment = false;
            realignPending = true;
            QueuedLogger.LogWarning(
                $"{VioAprilTagPoseEstimatorConstants.REALIGN_REJECTION_COUNT} consistent AprilTag "
                    + $"observations disagreed with the estimate by ~{innovation.Norm:F2}m; "
                    + "dropping back to Phase 1 to re-align"
            );
        }

        /// <summary>
        /// Applies a KF measurement update at capture time, rewinding the state and
        /// covariance to the capture snapshot, then replays the VIO motion since.
        /// </summary>
        private void ApplyKfUpdate(
            Translation3d measuredPosition,
            Matrix<double> stdDevs,
            LinkedListNode<VIOSnapshot> captureNode
        )
        {
            var snapshot = captureNode.Value;
            filter = new DiscreteKalmanFilter(
                snapshot.EstimatedState.Clone(),
                snapshot.Covariance.Clone()
            );

            filter.Update(ToColumnVector(measuredPosition), h, StdDevsToR(stdDevs));
            captureNode.Value = new VIOSnapshot(
                snapshot.Timestamp,
                snapshot.Position,
                snapshot.Rotation,
                filter.State.Clone(),
                filter.Cov.Clone(),
                snapshot.NoiseSeconds
            );
            ReplayFrom(captureNode);
        }

        /// <summary>Returns the newest snapshot at or before the timestamp, or null if none.</summary>
        private LinkedListNode<VIOSnapshot> FindSnapshotAtOrBefore(double timestampSeconds)
        {
            var node = snapshotBuffer.Last;
            while (node != null && node.Value.Timestamp > timestampSeconds)
                node = node.Previous;
            return node;
        }

        /// <summary>
        /// Returns the snapshot an AprilTag capture is fused against, or null when the capture
        /// is older than the buffer or too far past the snapshot before it.
        /// </summary>
        private LinkedListNode<VIOSnapshot> FindCaptureSnapshot(double timestampSeconds)
        {
            var node = FindSnapshotAtOrBefore(timestampSeconds);
            if (
                node == null
                || timestampSeconds - node.Value.Timestamp
                    > VioAprilTagPoseEstimatorConstants.MAX_CAPTURE_VIO_GAP_SECONDS
            )
                return null;
            return node;
        }

        /// <summary>
        /// Re-applies the VIO displacements recorded after startNode on top of the current
        /// filter state, rewriting each later snapshot's estimated state and covariance.
        /// </summary>
        private void ReplayFrom(LinkedListNode<VIOSnapshot> startNode)
        {
            var prevReplayNode = startNode;
            var replayNode = startNode.Next;

            while (replayNode != null)
            {
                Translation3d rawDisplacement =
                    replayNode.Value.Position - prevReplayNode.Value.Position;
                Translation3d displacement = rawDisplacement.RotateBy(
                    new Rotation3d(0, 0, yawOffset)
                );

                filter.Predict(f, ProcessNoise(replayNode.Value.NoiseSeconds));

                var state = filter.State;
                var corrected = state + ToColumnVector(displacement);
                filter = new DiscreteKalmanFilter(corrected, filter.Cov.Clone());

                replayNode.Value = new VIOSnapshot(
                    replayNode.Value.Timestamp,
                    replayNode.Value.Position,
                    replayNode.Value.Rotation,
                    corrected.Clone(),
                    filter.Cov.Clone(),
                    replayNode.Value.NoiseSeconds
                );

                prevReplayNode = replayNode;
                replayNode = replayNode.Next;
            }
        }

        /// <summary>Sets the yaw offset so the VIO yaw at capture maps onto the measured yaw.</summary>
        private void ApplyYawCorrection(
            Rotation3d measuredRotation,
            Rotation3d vioRotationAtCapture
        )
        {
            if (measuredRotation == null)
                return;

            yawOffset = NormalizeAngle(measuredRotation.Z - vioRotationAtCapture.Z);
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
