using System;
using System.Collections.Generic;

namespace QuestNav.Config
{
    /// <summary>
    /// Configuration data container for persistent storage.
    /// Contains all runtime-configurable values indexed by path (e.g., "Tunables/defaultTeamNumber").
    /// </summary>
    [Serializable]
    public class ConfigData
    {
        public Dictionary<string, object> values = new Dictionary<string, object>();
        public string version = "1.0";
        public long lastModified;
    }

    /// <summary>
    /// Schema definition for a single configuration field.
    /// Describes the field type, constraints, and UI rendering information.
    /// Uses properties (not fields) for proper JSON.NET serialization.
    /// </summary>
    [Serializable]
    public class ConfigFieldSchema
    {
        public string path { get; set; }
        public string displayName { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public string type { get; set; }
        public string controlType { get; set; }
        public object min { get; set; }
        public object max { get; set; }
        public object step { get; set; }
        public object defaultValue { get; set; }
        public object currentValue { get; set; }
        public bool requiresRestart { get; set; }
        public int order { get; set; }
        public string[] options { get; set; }
    }

    /// <summary>
    /// Complete configuration schema with all fields and categories.
    /// Generated dynamically via reflection from [Config] attributes.
    /// Uses properties (not fields) for proper JSON.NET serialization.
    /// </summary>
    [Serializable]
    public class ConfigSchema
    {
        public List<ConfigFieldSchema> fields { get; set; } = new List<ConfigFieldSchema>();
        public Dictionary<string, List<ConfigFieldSchema>> categories { get; set; } =
            new Dictionary<string, List<ConfigFieldSchema>>();
        public string version { get; set; } = "1.0";
    }

    /// <summary>
    /// Request to update a single configuration value.
    /// Sent from web interface via POST /api/config.
    /// </summary>
    [Serializable]
    public class ConfigUpdateRequest
    {
        public string path;
        public object value;
    }

    /// <summary>
    /// Response to a configuration update request.
    /// Indicates success/failure and includes old/new values.
    /// </summary>
    [Serializable]
    public class ConfigUpdateResponse
    {
        public bool success;
        public string message;
        public object oldValue;
        public object newValue;
    }

    /// <summary>
    /// Authentication token data (currently unused - auth is disabled).
    /// </summary>
    [Serializable]
    public class AuthToken
    {
        public string token;
        public long createdAt;
        public string deviceId;
    }
}
