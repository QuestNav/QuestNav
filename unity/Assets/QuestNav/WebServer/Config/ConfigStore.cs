using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Handles persistence of configuration data to disk.
    /// Stores configuration in Application.persistentDataPath as JSON files.
    /// Thread-safe for use from both main thread and ConfigServer background thread.
    /// </summary>
    public class ConfigStore
    {
        #region Constants
        /// <summary>
        /// Configuration file name
        /// </summary>
        private const string CONFIG_FILENAME = "config.json";
        #endregion

        #region Fields
        /// <summary>
        /// Full path to configuration file
        /// </summary>
        private readonly string configPath;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new ConfigStore instance.
        /// Sets up file paths in Application.persistentDataPath for cross-platform compatibility.
        /// </summary>
        public ConfigStore()
        {
            configPath = Path.Combine(Application.persistentDataPath, CONFIG_FILENAME);
        }
        #endregion

        #region Configuration Persistence
        /// <summary>
        /// Loads configuration data from persistent storage.
        /// Returns empty ConfigData if file doesn't exist or fails to load.
        /// </summary>
        /// <returns>ConfigData with loaded values, or empty ConfigData on error</returns>
        public ConfigData LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    return JsonConvert.DeserializeObject<ConfigData>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigStore] Failed to load config: {ex.Message}");
            }

            return new ConfigData();
        }

        /// <summary>
        /// Saves configuration data to persistent storage.
        /// Automatically updates the lastModified timestamp.
        /// </summary>
        /// <param name="config">Configuration data to save</param>
        /// <returns>True if save was successful, false otherwise</returns>
        public bool SaveConfig(ConfigData config)
        {
            if (config == null)
            {
                Debug.LogError("[ConfigStore] Cannot save null config data");
                return false;
            }

            try
            {
                config.lastModified = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConfigStore] Failed to save config: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the full path to the configuration file
        /// </summary>
        public string GetConfigPath() => configPath;
        #endregion
    }
}
