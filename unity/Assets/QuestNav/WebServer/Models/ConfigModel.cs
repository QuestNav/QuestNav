using System;
using System.Collections.Generic;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Configuration data container for persistent storage.
    /// Contains all runtime-configurable values indexed by path (e.g., "Tunables/webConfigTeamNumber").
    /// Serialized to JSON and saved to Application.persistentDataPath/config.json.
    /// </summary>
    [Serializable]
    public class ConfigData
    {
        /// <summary>
        /// Dictionary of configuration values keyed by field path
        /// </summary>
        public Dictionary<string, object> values = new Dictionary<string, object>();

        /// <summary>
        /// Configuration schema version for compatibility checking
        /// </summary>
        public string version = "1.0";

        /// <summary>
        /// Unix timestamp of last modification
        /// </summary>
        public long lastModified;
    }

    /// <summary>
    /// Schema definition for a single configuration field.
    /// Describes the field type, constraints, and UI rendering information.
    /// Uses properties (not fields) for proper JSON.NET serialization.
    /// Generated via reflection from [Config] attributes on static fields.
    /// </summary>
    [Serializable]
    public class ConfigFieldSchema
    {
        /// <summary>
        /// Unique path to the field (e.g., "Tunables/webConfigTeamNumber")
        /// </summary>
        public string path { get; set; }

        /// <summary>
        /// Human-readable display name for the web interface
        /// </summary>
        public string displayName { get; set; }

        /// <summary>
        /// Description/tooltip text for the field
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// Category for grouping related fields
        /// </summary>
        public string category { get; set; }

        /// <summary>
        /// Field data type (int, float, bool, string, color)
        /// </summary>
        public string type { get; set; }

        /// <summary>
        /// UI control type (slider, input, checkbox, select, color)
        /// </summary>
        public string controlType { get; set; }

        /// <summary>
        /// Minimum allowed value (for numeric types)
        /// </summary>
        public object min { get; set; }

        /// <summary>
        /// Maximum allowed value (for numeric types)
        /// </summary>
        public object max { get; set; }

        /// <summary>
        /// Step/increment value for sliders
        /// </summary>
        public object step { get; set; }

        /// <summary>
        /// Default value for the field
        /// </summary>
        public object defaultValue { get; set; }

        /// <summary>
        /// Current runtime value of the field
        /// </summary>
        public object currentValue { get; set; }

        /// <summary>
        /// Whether changing this field requires an app restart
        /// </summary>
        public bool requiresRestart { get; set; }

        /// <summary>
        /// Display order within category (lower = first)
        /// </summary>
        public int order { get; set; }

        /// <summary>
        /// Available options for select/dropdown controls
        /// </summary>
        public string[] options { get; set; }
    }

    /// <summary>
    /// Complete configuration schema with all fields and categories.
    /// Generated dynamically via reflection from [Config] attributes.
    /// Uses properties (not fields) for proper JSON.NET serialization.
    /// Served to web interface via /api/schema endpoint.
    /// </summary>
    [Serializable]
    public class ConfigSchema
    {
        /// <summary>
        /// All configuration fields in the system
        /// </summary>
        public List<ConfigFieldSchema> fields { get; set; } = new List<ConfigFieldSchema>();

        /// <summary>
        /// Fields grouped by category for organized display
        /// </summary>
        public Dictionary<string, List<ConfigFieldSchema>> categories { get; set; } =
            new Dictionary<string, List<ConfigFieldSchema>>();

        /// <summary>
        /// Schema version for compatibility checking
        /// </summary>
        public string version { get; set; } = "1.0";
    }

    /// <summary>
    /// Request to update a single configuration value.
    /// Sent from web interface via POST /api/config.
    /// </summary>
    [Serializable]
    public class ConfigUpdateRequest
    {
        /// <summary>
        /// Path to the field to update (e.g., "Tunables/webConfigTeamNumber")
        /// </summary>
        public string path;

        /// <summary>
        /// New value to set (will be converted to appropriate type)
        /// </summary>
        public object value;
    }

    /// <summary>
    /// Response to a configuration update request.
    /// Indicates success/failure and includes old/new values for confirmation.
    /// </summary>
    [Serializable]
    public class ConfigUpdateResponse
    {
        /// <summary>
        /// Whether the update was successful
        /// </summary>
        public bool success;

        /// <summary>
        /// Human-readable message describing the result
        /// </summary>
        public string message;

        /// <summary>
        /// Previous value before update
        /// </summary>
        public object oldValue;

        /// <summary>
        /// New value after update
        /// </summary>
        public object newValue;
    }

    /// <summary>
    /// Simple success/failure response
    /// </summary>
    [Serializable]
    public class SimpleResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool success;

        /// <summary>
        /// Message describing the result
        /// </summary>
        public string message;
    }

    /// <summary>
    /// Response for log retrieval
    /// </summary>
    [Serializable]
    public class LogsResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool success;

        /// <summary>
        /// List of log entries
        /// </summary>
        public System.Collections.Generic.List<LogCollector.LogEntry> logs;
    }

    /// <summary>
    /// Response for configuration retrieval
    /// </summary>
    [Serializable]
    public class ConfigValuesResponse
    {
        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool success;

        /// <summary>
        /// Dictionary of configuration values
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> values;

        /// <summary>
        /// Unix timestamp when response was generated
        /// </summary>
        public long timestamp;
    }

    /// <summary>
    /// Response for system information
    /// </summary>
    [Serializable]
    public class SystemInfoResponse
    {
        /// <summary>
        /// Application product name
        /// </summary>
        public string appName;

        /// <summary>
        /// Application version
        /// </summary>
        public string version;

        /// <summary>
        /// Unity engine version
        /// </summary>
        public string unityVersion;

        /// <summary>
        /// Build date
        /// </summary>
        public string buildDate;

        /// <summary>
        /// Current platform
        /// </summary>
        public string platform;

        /// <summary>
        /// Device model
        /// </summary>
        public string deviceModel;

        /// <summary>
        /// Device name
        /// </summary>
        public string deviceName;

        /// <summary>
        /// Operating system
        /// </summary>
        public string operatingSystem;

        /// <summary>
        /// Processor type
        /// </summary>
        public string processorType;

        /// <summary>
        /// Number of processor cores
        /// </summary>
        public int processorCount;

        /// <summary>
        /// System memory in MB
        /// </summary>
        public int systemMemorySize;

        /// <summary>
        /// Graphics device name
        /// </summary>
        public string graphicsDeviceName;

        /// <summary>
        /// Number of connected clients
        /// </summary>
        public int connectedClients;

        /// <summary>
        /// Path to configuration file
        /// </summary>
        public string configPath;

        /// <summary>
        /// Server port
        /// </summary>
        public int serverPort;

        /// <summary>
        /// Unix timestamp
        /// </summary>
        public long timestamp;
    }
}
