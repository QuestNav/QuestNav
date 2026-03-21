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
    }
}
