namespace QuestNav.QuestNav.Estimation
{
    /// <summary>
    /// Holds the constants for VIO and AprilTag fusion
    /// TODO: MAKE THIS CONFIGURABLE VIA WEB DASHBOARD
    /// </summary>
    public static class VioAprilTagPoseEstimatorConstants
    {
        /// <summary>VIO displacement noise standard deviations [x, y, z] in meters.</summary>
        public static readonly double[] defaultVioStdDevs = { 0.02, 0.02, 0.01 };

        /// <summary>AprilTag measurement noise standard deviations [x, y, z] in meters.</summary>
        public static readonly double[] defaultAprilTagStdDevs = { 0.05, 0.05, 0.10 };

        /// <summary>How far back in time AprilTag corrections can be applied.</summary>
        public const double BUFFER_DURATION_SECONDS = 0.5;

        // --- Phase 1: Initial alignment gating ---

        /// <summary>Minimum number of detected tags to accept the first alignment observation.</summary>
        public const int INITIAL_ALIGNMENT_MIN_TAGS = 2;

        /// <summary>Minimum inlier ratio (acceptedPoints / totalPoints) for the first alignment.</summary>
        public const double INITIAL_ALIGNMENT_MIN_INLIER_RATIO = 0.6;

        // --- Phase 2: Ongoing correction gating ---

        /// <summary>Minimum number of detected tags for a Phase 2 correction to be accepted.</summary>
        public const int CORRECTION_MIN_TAGS = 3;

        /// <summary>Minimum inlier ratio for a Phase 2 correction to be accepted.</summary>
        public const double CORRECTION_MIN_INLIER_RATIO = 0.8;

        /// <summary>
        /// Maximum allowed distance (meters) between the measured position and the current
        /// estimate. Observations beyond this are rejected as likely reflections.
        /// </summary>
        public const double CORRECTION_MAX_POSITION_JUMP = 2.0;

        // --- Dynamic standard deviation scaling ---

        /// <summary>Base linear std dev for dynamic scaling: linearStdDev = base * (avgDist^2 / tagCount).</summary>
        public const double MULTI_TAG_LINEAR_STD_DEV_BASE = 0.05;
    }
}
