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

        /// <summary>
        /// Join table that stores ignored AprilTag IDs (blacklist) for the single AprilTag config row.
        /// Empty = detect every tag the camera sees.
        /// </summary>
        public class AprilTagIgnoredId
        {
            [PrimaryKey, AutoIncrement]
            public int ID { get; set; }

            /// <summary>
            /// Reference to the parent AprilTag config (we use ID=1 for the single config row)
            /// </summary>
            public int AprilTagConfigId { get; set; }

            /// <summary>
            /// The AprilTag ID to ignore. Detections with this ID are dropped before the
            /// PoseLib solver runs. tag36h11 supports 587 unique IDs but FRC only uses
            /// the first ~40, so the UI clamps user input to [0, 50] for headroom.
            /// </summary>
            public int IgnoredId { get; set; }
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
            public int PassthroughStreamWidth { get; set; } = 320;

            /// <summary>
            /// The height of the stream in pixels
            /// </summary>
            public int PassthroughStreamHeight { get; set; } = 240;

            /// <summary>
            /// The framerate of the stream in frames per second
            /// </summary>
            public int PassthroughStreamFramerate { get; set; } = 24;

            /// <summary>
            /// JPEG compression quality (1-100). Higher values mean better quality and larger files.
            /// </summary>
            public int PassthroughStreamQuality { get; set; } = 75;

            /// <summary>
            /// Whether to allow high-resolution stream modes (greater than 640x480).
            /// </summary>
            public bool EnableHighQualityStreams { get; set; } = false;
        }

        /// <summary>
        /// ApriTag detection configuration.
        /// </summary>
        /// <remarks>
        /// NOTE: ignored IDs (blacklist) are stored in a separate table <c>AprilTagIgnoredId</c>.
        /// See <see cref="ConfigManager"/> to read/write.
        /// </remarks>
        public class AprilTag
        {
            /// <summary>
            /// The ID to be used as the primary key of this column in the database
            /// </summary>
            [PrimaryKey]
            public int ID { get; set; }

            /// <summary>
            /// Whether the AprilTag detector is enabled. Default is true.
            /// </summary>
            public bool EnableAprilTagDetector { get; set; } = true;

            /// <summary>
            /// The mode to detect using.
            /// </summary>
            /// <remarks>
            /// <list type="table">
            /// <item>
            /// <term><c>TRADITIONAL</c></term>
            /// <description>Uses traditional PnP solving for tag detection.</description>
            /// </item>
            /// <item>
            /// <term><c>ANCHOR_ENHANCED</c></term>
            /// <description>Uses PnP solving combined with Meta Quest spatial anchors for enhanced performance.</description>
            /// </item>
            /// </list>
            /// Default is <c>TRADITIONAL</c>.
            /// </remarks>
            public int AprilTagDetectorMode { get; set; } =
                (int)Config.AprilTagDetectorMode.DetectionMode.TRADITIONAL;

            /// <summary>
            /// The width of the detection region in pixels. Default is 640.
            /// </summary>
            public int AprilTagDetectorWidth { get; set; } = 1280;

            /// <summary>
            /// The height of the detection region in pixels. Default is 480.
            /// </summary>
            public int AprilTagDetectorHeight { get; set; } = 1280;

            /// <summary>
            /// The detection framerate in frames per second. Default is 30.
            /// </summary>
            public int AprilTagDetectorFramerate { get; set; } = 30;

            /// <summary>
            /// Maximum detection distance in meters. Default is 4.0 meters.
            /// </summary>
            public double AprilTagDetectorMaxDistance { get; set; } = 4.0;

            /// <summary>
            /// Minimum number of tags required to report a valid pose. Default is 2.
            /// </summary>
            public int AprilTagDetectorMinimumNumberOfTags { get; set; } = 2;

            /// <summary>
            /// Filename of the AprilTag field-layout JSON to load at startup. The file is
            /// resolved against the bundled <c>StreamingAssets/apriltag/fieldlayouts</c>
            /// directory and the user-uploaded
            /// <c>persistentDataPath/apriltag/fieldlayouts-custom</c> directory.
            ///
            /// Changes to this value take effect on app restart only; AprilTagFieldLayout
            /// caches the loaded data and the Kalman estimator is aligned to it. A live
            /// swap would invalidate <c>VioAprilTagPoseEstimator.hasInitialAlignment</c>
            /// and is intentionally avoided.
            /// </summary>
            public string AprilTagFieldLayoutFile { get; set; } =
                QuestNavConstants.AprilTag.DEFAULT_FIELD_LAYOUT_FILE;

            /// <summary>
            /// Phase-2 correction confidence preset (0=Permissive, 1=Balanced, 2=Strict).
            /// Maps to <c>QuestNav.QuestNav.Estimation.ConfidencePreset</c>; tighter
            /// presets reject more pose updates but are more conservative against bad
            /// observations. Defaults to Balanced (the prior hardcoded values).
            /// </summary>
            public int AprilTagConfidencePreset { get; set; } = 1;

            /// <summary>
            /// Multiplier on the dynamic AprilTag measurement noise std-dev. 0.5x = the KF
            /// trusts AprilTag observations more than the default; 2.0x = the KF trusts
            /// AprilTag less. Slider range in the web UI is [0.5, 2.0]; the default is 1.0.
            /// </summary>
            public double AprilTagNoiseScale { get; set; } = 1.0;
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

        public readonly struct AprilTagDetectorMode
        {
            public DetectionMode Mode { get; }

            public enum DetectionMode
            {
                /// <summary>
                /// Uses traditional PnP solving for tag detection.
                /// </summary>
                TRADITIONAL = 0,

                /// <summary>
                /// Uses PnP solving combined with Meta Quest spatial anchors for enhanced performance.
                /// </summary>
                ANCHOR_ENHANCED = 1,
            }

            /// <summary>
            /// The width of the detection region in pixels.
            /// </summary>
            public int Width { get; }

            /// <summary>
            /// The height of the detection region in pixels.
            /// </summary>
            public int Height { get; }

            /// <summary>
            /// The detection framerate in frames per second.
            /// </summary>
            public int Framerate { get; }

            /// <summary>
            /// Array of AprilTag IDs to ignore (blacklist). Empty array detects every tag.
            /// FRC uses the first ~40 IDs of tag36h11; the UI clamps entries to [0, 50].
            /// </summary>
            public int[] IgnoredIds { get; }

            /// <summary>
            /// Maximum detection distance in meters.
            /// </summary>
            public double MaxDistance { get; }

            /// <summary>
            /// Minimum number of tags required to report a valid pose.
            /// </summary>
            public int MinimumNumberOfTags { get; }

            /// <summary>
            /// Creates a new AprilTag detector mode configuration.
            /// </summary>
            /// <param name="mode">The detection mode to use</param>
            /// <param name="width">The width of the detection region in pixels.</param>
            /// <param name="height">The height of the detection region in pixels.</param>
            /// <param name="framerate">The detection framerate in frames per second.</param>
            /// <param name="ignoredIds">Array of AprilTag IDs to ignore. Empty array detects every tag.</param>
            /// <param name="maxDistance">Maximum detection distance in meters.</param>
            /// <param name="minimumNumberOfTags">Minimum number of tags required to report a valid pose.</param>
            public AprilTagDetectorMode(
                DetectionMode mode,
                int width,
                int height,
                int framerate,
                int[] ignoredIds,
                double maxDistance,
                int minimumNumberOfTags
            )
            {
                Mode = mode;
                Width = width;
                Height = height;
                Framerate = framerate;
                IgnoredIds = ignoredIds;
                MaxDistance = maxDistance;
                MinimumNumberOfTags = minimumNumberOfTags;
            }
        }
    }
}
