using QuestNav.Core;
using SQLite;

namespace QuestNav.Config
{
    public class Config
    {
        public class Network
        {
            /// <summary>
            /// The ID to be used as the primary key of this column in the database
            /// </summary>
            [PrimaryKey]
            public int ID { get; set; }

            /// <summary>
            /// The team number used for connecting to NetworkTables
            /// Cannot be combined with <see cref="DebugIpOverride"/> at the same time.
            /// </summary>
            public int TeamNumber { get; set; } = QuestNavConstants.Network.DEFAULT_TEAM_NUMBER;

            /// <summary>
            /// An optional value that allows NetworkTables to bypass FIRST's
            /// <see href="https://docs.wpilib.org/en/stable/docs/networking/networking-introduction/ip-configurations.html">IP configuration</see>
            /// and manually specify the IP of a NetworkTables server. This is intended to only be used for debugging.
            /// Cannot be combined with <see cref="TeamNumber"/> at the same time.
            /// </summary>
            public string DebugIpOverride { get; set; } = "";
        }

        public class System
        {
            /// <summary>
            /// The ID to be used as the primary key of this column in the database
            /// </summary>
            [PrimaryKey]
            public int ID { get; set; }

            /// <summary>
            /// Whether the headset should automatically start the QuestNav application when it turns on
            /// </summary>
            public bool EnableAutoStartOnBoot { get; set; } = true;
        }

        public class Camera
        {
            /// <summary>
            /// The ID to be used as the primary key of this column in the database
            /// </summary>
            [PrimaryKey]
            public int ID { get; set; }

            /// <summary>
            /// Whether the passthrough camera should be streamed over NT and WebUI
            /// </summary>
            public bool EnablePassthroughStream { get; set; } = false;

            /// <summary>
            /// The width of the stream in pixels
            /// </summary>
            public int StreamWidth { get; set; } = 320;

            /// <summary>
            /// The height of the stream in pixels
            /// </summary>
            public int StreamHeight { get; set; } = 240;

            /// <summary>
            /// The framerate of the stream in frames per second
            /// </summary>
            public int StreamFramerate { get; set; } = 24;

            /// <summary>
            /// JPEG compression quality (1-100). Higher values mean better quality and larger files.
            /// </summary>
            public int StreamQuality { get; set; } = 75;
        }

        public class Logging
        {
            /// <summary>
            /// The ID to be used as the primary key of this column in the database
            /// </summary>
            [PrimaryKey]
            public int ID { get; set; }

            /// <summary>
            /// Whether debug logging for NetworkTables should be logged to the Unity and WebUI consoles.
            /// </summary>
            public bool EnableDebugLogging { get; set; } = false;
        }

        /// <summary>
        /// Represents a video stream mode configuration with resolution and framerate.
        /// </summary>
        public readonly struct StreamMode
        {
            /// <summary>
            /// The image width in pixels.
            /// </summary>
            public int Width { get; }

            /// <summary>
            /// The image height in pixels.
            /// </summary>
            public int Height { get; }

            /// <summary>
            /// The stream's frames per second.
            /// </summary>
            public int Framerate { get; }

            /// <summary>
            /// JPEG compression quality (1-100). Higher values mean better quality and larger files.
            /// </summary>
            public int Quality { get; }

            /// <summary>
            /// Create a new stream mode.
            /// </summary>
            /// <param name="width">The image width in pixels.</param>
            /// <param name="height">The image height in pixels.</param>
            /// <param name="framerate">The stream's frames per second.</param>
            /// <param name="quality">JPEG compression quality (1-100).</param>
            public StreamMode(int width, int height, int framerate, int quality)
            {
                Width = width;
                Height = height;
                Framerate = framerate;
                Quality = quality;
            }

            /// <summary>
            /// Provide string description of stream mode.
            /// </summary>
            public override string ToString()
            {
                return $"{Width}x{Height}@{Framerate}fps Quality: {Quality}";
            }
        }
    }
}
