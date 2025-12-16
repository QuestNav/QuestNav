using QuestNav.Native.NTCore;

namespace QuestNav.Core
{
    /// <summary>
    /// Contains all constants used by the QuestNav application.
    /// These are constants meant NOT to change by the user or during runtime.
    /// </summary>
    public static class QuestNavConstants
    {
        /// <summary>
        /// Constants related to network configuration and communication.
        /// </summary>
        public static class Network
        {
            /// <summary>
            /// Default NetworkTables publisher/subscriber options
            /// </summary>
            public static PubSubOptions NtPublisherSettings = PubSubOptions.AllDefault;

            /// <summary>
            /// NetworkTables server port
            /// </summary>
            public const int NT_SERVER_PORT = 5810;

            /// <summary>
            /// Default team number when none is provided
            /// </summary>
            public const int DEFAULT_TEAM_NUMBER = 9999;

            /// <summary>
            /// Team number to be set when IP override is being used
            /// </summary>
            public const int TEAM_NUMBER_DISABLED = -1;

            /// <summary>
            /// Minimum team number allowed to be set
            /// </summary>
            public const int MIN_TEAM_NUMBER = 1;

            /// <summary>
            /// Maximum team number allowed to be set
            /// </summary>
            public const int MAX_TEAM_NUMBER = 25599;
        }

        /// <summary>
        /// Constants related to NetworkTables topics and paths.
        /// </summary>
        public static class Topics
        {
            /// <summary>
            /// Base path for all QuestNav topics
            /// </summary>
            public const string NT_BASE_PATH = "/QuestNav";

            /// <summary>
            /// Base path for all QuestNav topics
            /// </summary>
            public const string CAMERA_PUBLISHER = "/CameraPublisher";

            /// <summary>
            /// Command response topic (Quest to robot)
            /// </summary>
            public const string COMMAND_RESPONSE = NT_BASE_PATH + "/response";

            /// <summary>
            /// Command request topic (robot to Quest)
            /// </summary>
            public const string COMMAND_REQUEST = NT_BASE_PATH + "/request";

            /// <summary>
            /// Frame data topic
            /// </summary>
            public const string FRAME_DATA = NT_BASE_PATH + "/frameData";

            /// <summary>
            /// Device data topic
            /// </summary>
            public const string DEVICE_DATA = NT_BASE_PATH + "/deviceData";

            /// <summary>
            /// Video streams topic
            /// </summary>
            public const string VIDEO_STREAMS = NT_BASE_PATH + "/streams";
        }

        /// <summary>
        /// Constants related to command processing.
        /// </summary>
        public static class Commands
        {
            /// <summary>
            /// Time to live for pose reset command (ms). Commands older than this will be ignored.
            /// </summary>
            public const int POSE_RESET_TTL_MS = 75;
            
            /// <summary>
            /// The max history to store for latency compensation pose queue. Shouldn't be larger than POSE_RESET_TTL_MS / 6.33ms 
            /// </summary>
            public const int MAX_POSE_HISTORY = 10;
        }

        /// <summary>
        /// Constants related to the display and update frequency.
        /// </summary>
        public static class Display
        {
            /// <summary>
            /// Quest display frequency (in Hz)
            /// </summary>
            public const float DISPLAY_FREQUENCY = 120.0f;
        }

        /// <summary>
        /// Constants related to FRC field dimensions and pose resets.
        /// </summary>
        public static class Field
        {
            /// <summary>
            /// FRC field length in meters
            /// </summary>
            public const float FIELD_LENGTH = 16.54f;

            /// <summary>
            /// FRC field width in meters
            /// </summary>
            public const float FIELD_WIDTH = 8.02f;
        }

        /// <summary>
        /// Constants related to logging
        /// </summary>
        public static class Logging
        {
            /// <summary>
            /// NetworkTables logging levels constants
            /// </summary>
            public static class NtLogLevel
            {
                /// <summary>Critical level logging</summary>
                internal const int CRITICAL = 50;

                /// <summary>Error level logging</summary>
                internal const int ERROR = 40;

                /// <summary>Warning level logging</summary>
                internal const int WARNING = 30;

                /// <summary>Info level logging</summary>
                internal const int INFO = 20;

                /// <summary>Debug level logging</summary>
                internal const int DEBUG = 10;

                /// <summary>Debug1 level logging</summary>
                internal const int DEBUG1 = 9;

                /// <summary>Debug2 level logging</summary>
                internal const int DEBUG2 = 8;

                /// <summary>Debug3 level logging</summary>
                internal const int DEBUG3 = 7;

                /// <summary>Debug4 level logging</summary>
                internal const int DEBUG4 = 6;
            }

            /// <summary>
            /// The lowest level to log when using DEBUG logging. Usually this is INFO, or DEBUG1
            /// </summary>
            public const int NT_LOG_LEVEL_MIN_DEBUG = NtLogLevel.DEBUG1;

            /// <summary>
            /// The lowest level to log when using STANDARD logging. Usually this is WARNING
            /// </summary>
            public const int NT_LOG_LEVEL_MIN_STANDARD = NtLogLevel.WARNING;

            /// <summary>
            /// The highest level to log. Almost ALWAYS this is CRITICAL.
            /// </summary>
            public const int NT_LOG_LEVEL_MAX = NtLogLevel.CRITICAL;

            /// <summary>
            /// Maximum number of logs to keep in memory
            /// </summary>
            public const int MAX_LOGS = 500;
        }

        /// <summary>
        /// Constants related to non-main loop timing
        /// </summary>
        public static class Timing
        {
            /// <summary>
            /// The rate to run the "SlowUpdate" loop at
            /// </summary>
            public const int SLOW_UPDATE_HZ = 3;

            /// <summary>
            /// The rate to run the "MainUpdate" loop at
            /// </summary>
            public const int MAIN_UPDATE_HZ = 120;
        }

        /// <summary>
        /// Constants for the web server configuration
        /// </summary>
        public static class WebServer
        {
            /// <summary>
            /// The port the web server listens on
            /// </summary>
            public const int SERVER_PORT = 5801;

            /// <summary>
            /// Whether to enable CORS headers for development mode
            /// </summary>
            public const bool ENABLE_CORS_DEV_MODE = true;
        }

        /// <summary>
        /// Constants for the Tagalong UI behavior
        /// </summary>
        public static class TagalongUI
        {
            /// <summary>
            /// The distance the UI should follow the user at (in meters).
            /// </summary>
            public const float FOLLOW_DISTANCE = 2.0f;

            /// <summary>
            /// How quickly the UI moves towards the target position.
            /// </summary>
            public const float POSITION_SPEED = 4.0f;

            /// <summary>
            /// How quickly the UI rotates to match the user's rotation.
            /// </summary>
            public const float ROTATION_SPEED = 4.0f;

            /// <summary>
            /// Distance threshold for UI movement along the World X-axis (sideways).
            /// </summary>
            public const float POSITION_THRESHOLD_X = 0.5f;

            /// <summary>
            /// Distance threshold for UI movement along the World Y-axis (up/down).
            /// </summary>
            public const float POSITION_THRESHOLD_Y = 0.3f;

            /// <summary>
            /// The difference in angle at which the UI starts rotating.
            /// </summary>
            public const float ANGLE_THRESHOLD = 0.1f;
        }
    }
}
