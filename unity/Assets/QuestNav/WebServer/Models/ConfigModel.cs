using System;
using System.Collections.Generic;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Stream mode configuration for web API
    /// </summary>
    [Serializable]
    public class StreamModeModel
    {
        public int width;
        public int height;
        public int framerate;
        public int quality;
    }

    /// <summary>
    /// Available video mode option for the current stream source
    /// </summary>
    [Serializable]
    public class VideoModeModel
    {
        public int width;
        public int height;
        public int framerate;
    }

    /// <summary>
    /// AprilTag detector configuration for web API.
    /// <c>ignoredIds</c> is a blacklist: detections with one of these IDs are dropped
    /// before the PoseLib solver runs. Empty array means detect every tag.
    /// <c>fieldLayoutFile</c> takes effect on the next app restart only.
    /// </summary>
    [Serializable]
    public class AprilTagDetectorModeModel
    {
        public int mode;
        public int width;
        public int height;
        public int framerate;
        public int[] ignoredIds;
        public double maxDistance;
        public int minimumNumberOfTags;

        /// <summary>
        /// Filename of the field-layout JSON to use on the next app restart. Resolved
        /// against the bundled directory (and, in commit 6, the user-uploaded directory).
        /// Restart-on-change; the running app keeps using the previously-loaded layout
        /// until restart.
        /// </summary>
        public string fieldLayoutFile;
    }

    /// <summary>
    /// One entry in the response from <c>GET /api/apriltag-field-layouts</c>.
    /// </summary>
    [Serializable]
    public class AprilTagFieldLayoutEntry
    {
        /// <summary>The literal filename (e.g. "2026-rebuilt-welded.json").</summary>
        public string fileName;

        /// <summary>Human-friendly name derived from the filename for display in the UI.</summary>
        public string displayName;

        /// <summary>"bundled" or "custom" (commit 6 adds custom uploads).</summary>
        public string source;

        /// <summary>Number of tag entries in the layout JSON.</summary>
        public int tagCount;
    }

    /// <summary>
    /// Current configuration values response
    /// </summary>
    [Serializable]
    public class ConfigResponse
    {
        public bool success;
        public int teamNumber;
        public string debugIpOverride;
        public bool enableAutoStartOnBoot;
        public bool enablePassthroughStream;
        public bool enableHighQualityStream;
        public StreamModeModel streamMode;
        public bool enableAprilTagDetector;
        public AprilTagDetectorModeModel aprilTagDetectorMode;
        public bool enableDebugLogging;
        public long timestamp;
    }

    /// <summary>
    /// Request to update configuration
    /// </summary>
    [Serializable]
    public class ConfigUpdateRequest
    {
        public int? TeamNumber;
        public string debugIpOverride;
        public bool? EnableAutoStartOnBoot;
        public bool? EnablePassthroughStream;
        public bool? EnableHighQualityStream;
        public StreamModeModel StreamMode;
        public bool? EnableAprilTagDetector;
        public AprilTagDetectorModeModel AprilTagDetectorMode;
        public bool? EnableDebugLogging;
    }

    /// <summary>
    /// Simple success/failure response
    /// </summary>
    [Serializable]
    public class SimpleResponse
    {
        public bool success;
        public string message;
    }

    /// <summary>
    /// Response for log retrieval
    /// </summary>
    [Serializable]
    public class LogsResponse
    {
        public bool success;
        public List<LogCollector.LogEntry> logs;
    }

    /// <summary>
    /// Response for system information
    /// </summary>
    [Serializable]
    public class SystemInfoResponse
    {
        public string appName;
        public string version;
        public string unityVersion;
        public string buildDate;
        public string platform;
        public string deviceModel;
        public string operatingSystem;
        public int connectedClients;
        public int serverPort;
        public long timestamp;
    }
}
