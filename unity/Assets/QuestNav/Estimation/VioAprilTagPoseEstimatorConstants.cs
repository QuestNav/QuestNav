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
        /// Maximum allowed distance (meters) between the measured position and the current
        /// estimate. Observations beyond this are rejected as likely reflections.
        /// </summary>
        public const double CORRECTION_MAX_POSITION_JUMP = 2.0;

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
