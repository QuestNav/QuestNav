using UnityEngine;

namespace QuestNav.Config
{
    public static class Tunables
    {
        // ===== QuestNav Configuration =====

        [Config(
            DisplayName = "Default Team Number",
            Description = "Default FRC team number for NetworkTables connection",
            Category = "QuestNav",
            Min = 1,
            Max = 9999,
            ControlType = "input",
            Order = 1
        )]
        public static int defaultTeamNumber = 9999;

        [Config(
            DisplayName = "NetworkTables Server Port",
            Description = "Port for NetworkTables server connection",
            Category = "QuestNav",
            Min = 1024,
            Max = 65535,
            ControlType = "input",
            RequiresRestart = true,
            Order = 2
        )]
        public static int ntServerPort = 5810;

        [Config(
            DisplayName = "Main Update Frequency (Hz)",
            Description = "Update rate for pose streaming to robot (higher = more responsive)",
            Category = "QuestNav",
            Min = 10,
            Max = 200,
            Step = 10,
            ControlType = "slider",
            RequiresRestart = true,
            Order = 3
        )]
        public static int mainUpdateHz = 100;

        [Config(
            DisplayName = "Slow Update Frequency (Hz)",
            Description = "Update rate for UI and health monitoring",
            Category = "QuestNav",
            Min = 1,
            Max = 10,
            Step = 1,
            ControlType = "slider",
            RequiresRestart = true,
            Order = 4
        )]
        public static int slowUpdateHz = 3;

        [Config(
            DisplayName = "Display Frequency (Hz)",
            Description = "Quest headset display refresh rate",
            Category = "QuestNav",
            Min = 72f,
            Max = 120f,
            Step = 1f,
            ControlType = "slider",
            RequiresRestart = true,
            Order = 5
        )]
        public static float displayFrequency = 120.0f;

        [Config(
            DisplayName = "Auto-Start on Boot",
            Description = "Automatically start NetworkTables connection on app launch",
            Category = "QuestNav",
            ControlType = "checkbox",
            Order = 6
        )]
        public static bool autoStartOnBoot = false;

        [Config(
            DisplayName = "Pose Reset TTL (ms)",
            Description = "Time-to-live for pose reset commands (commands older than this are ignored)",
            Category = "QuestNav",
            Min = 10,
            Max = 1000,
            Step = 10,
            ControlType = "slider",
            Order = 7
        )]
        public static int poseResetTtlMs = 50;

        [Config(
            DisplayName = "Max Pose Read Retries",
            Description = "Maximum number of attempts to read pose data",
            Category = "QuestNav",
            Min = 1,
            Max = 10,
            Step = 1,
            ControlType = "slider",
            Order = 8
        )]
        public static int maxPoseReadRetries = 3;

        [Config(
            DisplayName = "Position Error Threshold (m)",
            Description = "Position error threshold for warnings (meters)",
            Category = "QuestNav",
            Min = 0.001f,
            Max = 0.1f,
            Step = 0.001f,
            ControlType = "slider",
            Order = 9
        )]
        public static float positionErrorThreshold = 0.01f;

        [Config(
            DisplayName = "NetworkTables Log Level",
            Description = "Minimum log level for NetworkTables (lower = more verbose)",
            Category = "QuestNav",
            Min = 6,
            Max = 50,
            Step = 1,
            ControlType = "slider",
            Order = 10
        )]
        public static int ntLogLevelMin = 9;

        // ===== General Configuration =====

        [Config(
            DisplayName = "Server Port",
            Description = "HTTP server port for configuration UI",
            Category = "General",
            Min = 1024,
            Max = 65535,
            ControlType = "input",
            RequiresRestart = true,
            Order = 40
        )]
        public static int serverPort = 18080;

        [Config(
            DisplayName = "Enable CORS Dev Mode",
            Description = "Allow localhost connections for development",
            Category = "General",
            ControlType = "checkbox",
            Order = 41
        )]
        public static bool enableCORSDevMode = false;
    }
}
