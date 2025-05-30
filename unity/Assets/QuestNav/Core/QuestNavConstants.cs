using QuestNav.Native.NTCore;

namespace QuestNav.Core
{
    /// <summary>
    /// Contains all constants used by the QuestNav application.
    /// Organized by functional category for easier maintenance.
    /// </summary>
    public static class QuestNavConstants
    {
        /// <summary>
        /// Constants related to network configuration and communication.
        /// </summary>
        public static class Network
        {

            public static PubSubOptions NT_PUBLISHER_SETTINGS = PubSubOptions.AllDefault;
            
            /// <summary>
            /// Tells NT4 to connect to this IP instead of via team number if not empty. DEBUGGING PURPOSES ONLY!
            /// </summary>
            public const string DEBUG_NT_SERVER_ADDRESS_OVERRIDE = "";

            /// <summary>
            /// NetworkTables server port
            /// </summary>
            public const int NT_SERVER_PORT = 5810;

            /// <summary>
            /// Default team number when none is provided
            /// </summary>
            public const int DEFAULT_TEAM_NUMBER = 9999;
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
            /// Command response topic (Quest to robot)
            /// </summary>
            public const string COMMAND_RESPONSE = NT_BASE_PATH + "/miso";

            /// <summary>
            /// Command request topic (robot to Quest)
            /// </summary>
            public const string COMMAND_REQUEST = NT_BASE_PATH + "/mosi";

            /// <summary>
            /// Frame count topic
            /// </summary>
            public const string FRAME_COUNT = NT_BASE_PATH + "/frameCount";

            /// <summary>
            /// Timestamp topic
            /// </summary>
            public const string TIMESTAMP = NT_BASE_PATH + "/timestamp";

            /// <summary>
            /// Position topic
            /// </summary>
            public const string POSITION = NT_BASE_PATH + "/position";

            /// <summary>
            /// Quaternion rotation topic
            /// </summary>
            public const string QUATERNION = NT_BASE_PATH + "/quaternion";

            /// <summary>
            /// Euler angles topic
            /// </summary>
            public const string EULER_ANGLES = NT_BASE_PATH + "/eulerAngles";

            /// <summary>
            /// Reset pose topic
            /// </summary>
            public const string RESET_POSE = NT_BASE_PATH + "/resetpose";
            
            /// <summary>
            /// How many times we have lost tracking this reboot
            /// </summary>
            public const string TRACKING_LOST_COUNTER = NT_BASE_PATH + "/device/trackingLostCounter";
            
            /// <summary>
            /// The current tracking state
            /// </summary>
            public const string CURRENTLY_TRACKING = NT_BASE_PATH + "/device/isTracking";
            
            /// <summary>
            /// Battery percentage topic
            /// </summary>
            public const string BATTERY_PERCENT = NT_BASE_PATH + "/device/batteryPercent";
        }

        /// <summary>
        /// Constants related to command processing.
        /// </summary>
        public static class Commands
        {
            /// <summary>
            /// Command code for no request/response
            /// </summary>
            public const int IDLE = 0;
            
            /// <summary>
            /// Command code for heading reset request
            /// </summary>
            public const int HEADING_RESET = 1;

            /// <summary>
            /// Command code for pose reset request
            /// </summary>
            public const int POSE_RESET = 2;

            /// <summary>
            /// Command code for ping request
            /// </summary>
            public const int PING = 3;

            /// <summary>
            /// Response code for ping
            /// </summary>
            public const int PING_RESPONSE = 97;

            /// <summary>
            /// Response code for successful pose reset
            /// </summary>
            public const int POSE_RESET_SUCCESS = 98;

            /// <summary>
            /// Response code for successful heading reset
            /// </summary>
            public const int HEADING_RESET_SUCCESS = 99;
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

            /// <summary>
            /// Maximum number of attempts to read pose data
            /// </summary>
            public const int MAX_POSE_READ_RETRIES = 3;

            /// <summary>
            /// Delay between retry attempts (ms)
            /// </summary>
            public const float POSE_RETRY_DELAY_MS = 50f;

            /// <summary>
            /// Position error threshold for warning (meters)
            /// </summary>
            public const float POSITION_ERROR_THRESHOLD = 0.01f; // 1cm
        }

        /// <summary>
        /// Constants related to logging
        /// </summary>
        public static class Logging
        {
            /** NetworkTables logging levels. */
            static class NTLogLevel
            {
                internal const int CRITICAL = 50;
                internal const int ERROR = 40;
                internal const int WARNING = 30;
                internal const int INFO = 20;
                internal const int DEBUG = 10;
                internal const int DEBUG1 = 9;
                internal const int DEBUG2 = 8;
                internal const int DEBUG3 = 7;
                internal const int DEBUG4 = 6;
            }

            /// <summary>
            /// The lowest level to log. Usually this is INFO, or DEBUG1
            /// </summary>
            public const int NT_LOG_LEVEL_MIN = NTLogLevel.DEBUG1;
            
            /// <summary>
            /// The lowest level to log. Almost ALWAYS this is CRITICAL.
            /// </summary>
            public const int NT_LOG_LEVEL_MAX = NTLogLevel.CRITICAL;
        }

        /// <summary>
        /// Constants related to non-main loop timing
        /// </summary>
        public static class Timing
        {
            /// <summary>
            /// The rate to run the "SlowUpdate" loop at
            /// </summary>
            public const int SLOW_UPDATE_HZ = 8;
        }
    }
}