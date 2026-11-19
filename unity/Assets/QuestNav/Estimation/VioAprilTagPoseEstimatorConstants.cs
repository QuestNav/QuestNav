namespace QuestNav.QuestNav.Estimation
{
    /// <summary>
    /// Phase-2 correction confidence presets. Wider gap = more conservative pose updates;
    /// the dropdown in the AprilTag tab picks one. Mirrored on the web side as
    /// <c>questnav-web-ui/src/types.ts:ConfidencePreset</c>; keep the int values aligned.
    /// </summary>
    public enum ConfidencePreset
    {
        /// <summary>Permissive (2 tags, ratio >= 0.75). Use only when default tuning is too strict.</summary>
        Permissive = 0,

        /// <summary>Balanced (3 tags, ratio >= 0.80). Default; matches the prior hardcoded values.</summary>
        Balanced = 1,

        /// <summary>Strict (4 tags, ratio >= 0.90). Use in noisy / high-glare environments.</summary>
        Strict = 2,

        /// <summary>
        /// Debug (1 tag, ratio >= 0.60). For benchtop / confined-space testing where only
        /// one tag fits in view. Single-tag PnP is geometrically ambiguous (left/right
        /// reflection, depth uncertainty) so corrections under this preset can introduce
        /// pose jumps; do NOT use on a robot during competition.
        /// </summary>
        Debug = 3,
    }

    /// <summary>
    /// Holds the constants for VIO and AprilTag fusion. The CORRECTION_MIN_TAGS and
    /// CORRECTION_MIN_INLIER_RATIO values shown here are the BALANCED defaults; the
    /// effective values can be overridden at runtime via
    /// <see cref="VioAprilTagPoseEstimator.SetConfidencePreset"/> from the web UI.
    /// </summary>
    public static class VioAprilTagPoseEstimatorConstants
    {
        /// <summary>VIO displacement noise standard deviations [x, y, z] in meters.</summary>
        public static readonly double[] defaultVioStdDevs = { 0.02, 0.02, 0.01 };

        /// <summary>AprilTag measurement noise standard deviations [x, y, z] in meters.</summary>
        public static readonly double[] defaultAprilTagStdDevs = { 0.05, 0.05, 0.10 };

        /// <summary>How far back in time AprilTag corrections can be applied.</summary>
        public const double BUFFER_DURATION_SECONDS = 0.5;

        // --- Phase 1: Initial alignment gating (NOT user-configurable) ---

        /// <summary>Minimum number of detected tags to accept the first alignment observation.</summary>
        public const int INITIAL_ALIGNMENT_MIN_TAGS = 2;

        /// <summary>Minimum inlier ratio (acceptedPoints / totalPoints) for the first alignment.</summary>
        public const double INITIAL_ALIGNMENT_MIN_INLIER_RATIO = 0.6;

        // --- Phase 2: Ongoing correction gating (defaults; overridden by ConfidencePreset) ---

        /// <summary>Default minimum number of detected tags for a Phase 2 correction (Balanced preset).</summary>
        public const int CORRECTION_MIN_TAGS = 3;

        /// <summary>Default minimum inlier ratio for a Phase 2 correction (Balanced preset).</summary>
        public const double CORRECTION_MIN_INLIER_RATIO = 0.8;

        /// <summary>
        /// Phase-2 outlier gate: an observation is rejected when the squared Mahalanobis
        /// distance between the measured position and the capture-time estimate, using
        /// S = P + R, exceeds this. 16.27 is the 99.9% chi-square quantile for 3 DOF.
        /// </summary>
        public const double CORRECTION_GATE_CHI_SQUARED = 16.27;

        /// <summary>
        /// Absolute cap (meters) on the distance between the measured position and the
        /// capture-time estimate, applied on top of the Mahalanobis gate so a large
        /// covariance after a long stretch without tags cannot admit a wild measurement.
        /// </summary>
        public const double CORRECTION_MAX_POSITION_JUMP = 2.0;

        // --- Phase 2 -> Phase 1 recovery ---

        /// <summary>
        /// Number of consecutive gate-rejected multi-tag observations that must agree
        /// with each other before the estimator drops back to Phase 1 and re-aligns.
        /// </summary>
        public const int REALIGN_REJECTION_COUNT = 5;

        /// <summary>Minimum tag count for a gate rejection to count toward re-alignment.</summary>
        public const int REALIGN_MIN_TAGS = 2;

        /// <summary>
        /// Radius (meters) within which the innovations (measured minus capture-time
        /// estimate) of consecutive rejections must lie to count as agreeing.
        /// </summary>
        public const double REALIGN_CONSENSUS_RADIUS = 0.5;

        // --- Input validation ---

        /// <summary>The first dropped non-finite input is logged, then every Nth one after it.</summary>
        public const int NON_FINITE_LOG_INTERVAL = 100;

        // --- Confidence presets ---

        /// <summary>Permissive preset: (min tags, min inlier ratio).</summary>
        public const int PRESET_PERMISSIVE_MIN_TAGS = 2;
        public const double PRESET_PERMISSIVE_MIN_INLIER_RATIO = 0.75;

        /// <summary>Balanced preset: (min tags, min inlier ratio). Same as the legacy hardcoded values.</summary>
        public const int PRESET_BALANCED_MIN_TAGS = 3;
        public const double PRESET_BALANCED_MIN_INLIER_RATIO = 0.8;

        /// <summary>Strict preset: (min tags, min inlier ratio).</summary>
        public const int PRESET_STRICT_MIN_TAGS = 4;
        public const double PRESET_STRICT_MIN_INLIER_RATIO = 0.9;

        /// <summary>
        /// Debug preset: (min tags, min inlier ratio). For benchtop / confined-space
        /// testing only. The 0.60 ratio mirrors INITIAL_ALIGNMENT_MIN_INLIER_RATIO so a
        /// single tag that's good enough to lock in Phase 1 is also good enough to
        /// correct under this preset.
        /// </summary>
        public const int PRESET_DEBUG_MIN_TAGS = 1;
        public const double PRESET_DEBUG_MIN_INLIER_RATIO = 0.6;

        // --- Dynamic standard deviation scaling ---

        /// <summary>
        /// Base linear std dev for dynamic scaling:
        /// <c>linearStdDev = base * noiseScale * (avgDist^2 / tagCount)</c>.
        /// <c>noiseScale</c> is the user-tunable "AprilTag Trust" multiplier (0.5x = high
        /// trust, 1.0x = default, 2.0x = low trust).
        /// </summary>
        public const double MULTI_TAG_LINEAR_STD_DEV_BASE = 0.05;
    }
}
