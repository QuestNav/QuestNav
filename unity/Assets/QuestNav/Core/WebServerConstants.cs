using QuestNav.WebServer;
using UnityEngine;

namespace QuestNav.Core
{
    /// <summary>
    /// Runtime-configurable settings for QuestNav application.
    /// All fields are exposed to the web configuration interface via [Config] attributes.
    /// Changes are automatically saved to persistent storage and can be modified at runtime.
    /// Access settings via static fields (e.g., WebServerConstants.webConfigTeamNumber).
    /// Values are automatically loaded on startup and persisted when changed via web interface.
    /// </summary>
    public static class WebServerConstants
    {
        #region QuestNav Configuration
        /// <summary>
        /// FRC team number from web configuration interface (1-25599).
        /// Used to automatically resolve robot IP address via FRC team number convention.
        /// Valid FRC team numbers range from 1 to 25599 as allocated by FIRST.
        /// Synced bidirectionally with VR UI team number input.
        /// </summary>
        [Config(
            DisplayName = "Team Number",
            Description = "FRC team number for NetworkTables connection (1-25599)",
            Category = "QuestNav",
            Min = 1,
            Max = 25599,
            ControlType = "input",
            Order = 1
        )]
        public static int webConfigTeamNumber = 9999;

        /// <summary>
        /// Debug IP address override for direct robot connection.
        /// When set, bypasses team number resolution and connects directly to specified IP.
        /// Leave empty to use team number.
        /// </summary>
        [Config(
            DisplayName = "Debug IP Override",
            Description = "Override team number and connect to specific IP address (leave empty to use team number)",
            Category = "General",
            ControlType = "input",
            Order = 43
        )]
        public static string debugNTServerAddressOverride = "";

        /// <summary>
        /// NetworkTables server port (default: 5810).
        /// Standard FRC NetworkTables 4 port. Only change if using custom configuration.
        /// </summary>
        [Config(
            DisplayName = "NetworkTables Server Port",
            Description = "Port for NetworkTables server connection (Default: 5810)",
            Category = "General",
            Min = 1024,
            Max = 65535,
            ControlType = "input",
            RequiresRestart = true,
            Order = 44
        )]
        public static int ntServerPort = 5810;

        /// <summary>
        /// Main update loop frequency in Hz (default: 100).
        /// Controls how often pose data is streamed to the robot.
        /// Higher values provide more responsive tracking but increase CPU usage.
        /// </summary>
        [Config(
            DisplayName = "Main Update Frequency (Hz)",
            Description = "Update rate for pose streaming to robot (higher = more responsive)",
            Category = "QuestNav",
            Min = 10,
            Max = 200,
            Step = 10,
            ControlType = "slider",
            RequiresRestart = true,
            Order = 2
        )]
        public static int mainUpdateHz = 100;

        /// <summary>
        /// Slow update loop frequency in Hz (default: 3).
        /// Controls update rate for UI elements and health monitoring.
        /// Lower frequency reduces overhead for non-critical operations.
        /// </summary>
        [Config(
            DisplayName = "Slow Update Frequency (Hz)",
            Description = "Update rate for UI and health monitoring",
            Category = "QuestNav",
            Min = 1,
            Max = 10,
            Step = 1,
            ControlType = "slider",
            RequiresRestart = true,
            Order = 3
        )]
        public static int slowUpdateHz = 3;

        /// <summary>
        /// Quest headset display refresh rate in Hz (72, 90, or 120).
        /// Quest 2 supports 72/90/120Hz, Quest Pro/3 support up to 120Hz.
        /// Higher refresh rates provide smoother visuals but drain battery faster.
        /// </summary>
        [Config(
            DisplayName = "Display Frequency (Hz)",
            Description = "Quest headset display refresh rate",
            Category = "QuestNav",
            Min = 72f,
            Max = 120f,
            Step = 1f,
            ControlType = "slider",
            RequiresRestart = true,
            Order = 4
        )]
        public static float displayFrequency = 120.0f;

        /// <summary>
        /// Auto-start NetworkTables connection when app launches.
        /// When enabled, immediately attempts to connect to robot on app startup.
        /// When disabled, requires manual connection via VR UI.
        /// </summary>
        [Config(
            DisplayName = "Auto-Start on Boot",
            Description = "Automatically start NetworkTables connection on app launch",
            Category = "QuestNav",
            ControlType = "checkbox",
            Order = 5
        )]
        public static bool autoStartOnBoot = false;

        /// <summary>
        /// Time-to-live for pose reset commands in milliseconds (default: 50).
        /// Commands older than this threshold are ignored to prevent stale resets.
        /// Lower values improve safety but may miss legitimate commands on slow networks.
        /// </summary>
        [Config(
            DisplayName = "Pose Reset TTL (ms)",
            Description = "Time-to-live for pose reset commands (commands older than this are ignored)",
            Category = "QuestNav",
            Min = 10,
            Max = 1000,
            Step = 10,
            ControlType = "slider",
            Order = 6
        )]
        public static int poseResetTtlMs = 50;

        /// <summary>
        /// Maximum number of retry attempts when reading pose data (default: 3).
        /// Increases reliability but may introduce latency on retry.
        /// </summary>
        [Config(
            DisplayName = "Max Pose Read Retries",
            Description = "Maximum number of attempts to read pose data",
            Category = "QuestNav",
            Min = 1,
            Max = 10,
            Step = 1,
            ControlType = "slider",
            Order = 7
        )]
        public static int maxPoseReadRetries = 3;

        /// <summary>
        /// Position error threshold in meters for triggering warnings (default: 0.01).
        /// Errors above this threshold will generate warning logs.
        /// Useful for detecting tracking or calibration issues.
        /// </summary>
        [Config(
            DisplayName = "Position Error Threshold (m)",
            Description = "Position error threshold for warnings (meters)",
            Category = "QuestNav",
            Min = 0.001f,
            Max = 0.1f,
            Step = 0.001f,
            ControlType = "slider",
            Order = 8
        )]
        public static float positionErrorThreshold = 0.01f;

        /// <summary>
        /// Minimum log level for NetworkTables internal logging (default: 9 = DEBUG1).
        /// Lower values produce more verbose output. Range: 6 (DEBUG4) to 50 (CRITICAL).
        /// Useful for diagnosing NetworkTables connection issues.
        /// </summary>
        [Config(
            DisplayName = "NetworkTables Log Level",
            Description = "Minimum log level for NetworkTables (lower = more verbose)",
            Category = "General",
            Min = 6,
            Max = 50,
            Step = 1,
            ControlType = "slider",
            Order = 45
        )]
        public static int ntLogLevelMin = 9;
        #endregion

        #region Video Configuration
        /// <summary>
        /// Enable passthrough video stream.
        /// </summary>
        [Config(
            DisplayName = "Enable Passthrough Video",
            Description = "",
            Category = "Passthrough",
            ControlType = "checkbox",
            Order = 1
        )]
        public static bool enablePassThrough = false;
        #endregion

        #region General Configuration
        /// <summary>
        /// HTTP server port for web configuration interface (default: 5801).
        /// Access the web UI at http://quest-ip:serverPort
        /// Change if port conflicts with other services.
        /// </summary>
        [Config(
            DisplayName = "Web Server Port",
            Description = "HTTP server port for the webconfiguration UI (Default: 5801)",
            Category = "General",
            Min = 5801,
            Max = 5810,
            ControlType = "input",
            RequiresRestart = true,
            Order = 40
        )]
        public static int serverPort = 5801;

        /// <summary>
        /// Enable CORS for localhost development (default: false).
        /// Allows web UI development server on localhost to access Quest APIs.
        /// Only enable during development, disable for production.
        /// </summary>
        [Config(
            DisplayName = "Enable CORS Dev Mode",
            Description = "Allow web UI development from your computer (only enable if developing the UI)",
            Category = "General",
            ControlType = "checkbox",
            Order = 41
        )]
        public static bool enableCORSDevMode = false;

        /// <summary>
        /// Enable verbose debug logging (default: false).
        /// When enabled, shows detailed NetworkTables connection attempts and debug messages.
        /// When disabled, only shows warnings and errors for cleaner logs.
        /// Useful for troubleshooting connection issues.
        /// </summary>
        [Config(
            DisplayName = "Enable Debug Logging",
            Description = "Show verbose debug messages (connection attempts, detailed diagnostics)",
            Category = "General",
            ControlType = "checkbox",
            Order = 42
        )]
        public static bool enableDebugLogging = false;
        #endregion
    }
}
