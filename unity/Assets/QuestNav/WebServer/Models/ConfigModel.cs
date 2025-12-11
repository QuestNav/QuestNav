using System;
using System.Collections.Generic;

namespace QuestNav.WebServer
{
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
