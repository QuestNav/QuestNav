using System;
using System.Collections.Generic;

namespace QuestNav.Config
{
    [Serializable]
    public class ConfigData
    {
        public Dictionary<string, object> values = new Dictionary<string, object>();
        public string version = "1.0";
        public long lastModified;
    }

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

    [Serializable]
    public class ConfigSchema
    {
        public List<ConfigFieldSchema> fields { get; set; } = new List<ConfigFieldSchema>();
        public Dictionary<string, List<ConfigFieldSchema>> categories { get; set; } =
            new Dictionary<string, List<ConfigFieldSchema>>();
        public string version { get; set; } = "1.0";
    }

    [Serializable]
    public class ConfigUpdateRequest
    {
        public string path;
        public object value;
    }

    [Serializable]
    public class ConfigUpdateResponse
    {
        public bool success;
        public string message;
        public object oldValue;
        public object newValue;
    }

    [Serializable]
    public class AuthToken
    {
        public string token;
        public long createdAt;
        public string deviceId;
    }
}
