using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using QuestNav.Core;
using QuestNav.Utils;
using SQLite;
using UnityEngine;

namespace QuestNav.Config
{
    public interface IConfigManager
    {
        public Task initializeAsync();
        public Task closeAsync();

        public event Action<int> onTeamNumberChanged;
        public event Action<string> onDebugIpOverrideChanged;
        public event Action<bool> onEnableAutoStartOnBootChanged;
        public event Action<bool> onEnableDebugLoggingChanged;

        public Task<int> getTeamNumberAsync();
        public Task<string> getDebugIpOverrideAsync();
        public Task<bool> getEnableAutoStartOnBootAsync();
        public Task<bool> getEnableDebugLoggingAsync();
        public Task setTeamNumberAsync(int teamNumber);
        public Task setDebugIpOverrideAsync(string ipOverride);
        public Task setEnableAutoStartOnBootAsync(bool autoStart);
        public Task setEnableDebugLoggingAsync(bool enableDebugLogging);
        public Task resetToDefaultsAsync();
    }

    public class ConfigManager : IConfigManager
    {
        private static readonly string dbPath = Path.Combine(
            Application.persistentDataPath,
            "config.db"
        );
        private SQLiteAsyncConnection connection;

        #region Lifecycle Methods
        public async Task initializeAsync()
        {
            SQLitePCL.Batteries_V2.Init();

            if (connection != null)
                return;

            connection = new SQLiteAsyncConnection(dbPath);

            // Create with defaults if they don't already exist
            await connection.CreateTableAsync<Config.Network>();
            await connection.CreateTableAsync<Config.System>();
            await connection.CreateTableAsync<Config.Logging>();

            QueuedLogger.Log($"Database initialized at: {dbPath}");
        }

        public async Task resetToDefaultsAsync()
        {
            await saveNetworkConfigAsync(new Config.Network());
            await saveSystemConfigAsync(new Config.System());
            await saveLoggingConfigAsync(new Config.Logging());

            QueuedLogger.Log("Database reset to defaults");
        }

        public async Task closeAsync()
        {
            if (connection != null)
            {
                await connection.CloseAsync();
                connection = null;
            }
        }
        #endregion

        #region Events
        // Network events
        public event Action<int> onTeamNumberChanged;
        public event Action<string> onDebugIpOverrideChanged;

        // System events
        public event Action<bool> onEnableAutoStartOnBootChanged;

        // Logging events
        public event Action<bool> onEnableDebugLoggingChanged;
        #endregion

        #region Getters
        #region Network
        public async Task<int> getTeamNumberAsync()
        {
            var config = await getNetworkConfigAsync();

            return config.teamNumber;
        }

        public async Task<string> getDebugIpOverrideAsync()
        {
            var config = await getNetworkConfigAsync();

            return config.debugIpOverride;
        }
        #endregion

        #region System
        public async Task<bool> getEnableAutoStartOnBootAsync()
        {
            var config = await getSystemConfigAsync();

            return config.enableAutoStartOnBoot;
        }
        #endregion

        #region Logging
        public async Task<bool> getEnableDebugLoggingAsync()
        {
            var config = await getLoggingConfigAsync();

            return config.enableDebugLogging;
        }
        #endregion
        #endregion

        #region Setters

        #region Network
        public async Task setTeamNumberAsync(int teamNumber)
        {
            // Validate new value
            if (!isValidTeamNumber(teamNumber))
            {
                QueuedLogger.LogError(
                    "Attempted to write a non-valid team number to the config! Aborting..."
                );
                return;
            }

            var config = await getNetworkConfigAsync();
            config.teamNumber = teamNumber;
            config.debugIpOverride = ""; // Blank out IP override
            await saveNetworkConfigAsync(config);

            // Notify subscribed methods
            onTeamNumberChanged?.Invoke(config.teamNumber);
            onDebugIpOverrideChanged?.Invoke(config.debugIpOverride);
            QueuedLogger.Log($"Updated Key 'teamNumber' to {teamNumber}");
        }

        public async Task setDebugIpOverrideAsync(string ipOverride)
        {
            // Validate new value (blank means disable)
            if (!isValidIPAddress(ipOverride))
            {
                QueuedLogger.LogError(
                    "Attempted to write a non-valid debug IP override to the config! Aborting..."
                );
                return;
            }

            var config = await getNetworkConfigAsync();
            config.debugIpOverride = ipOverride;
            config.teamNumber = QuestNavConstants.Network.TEAM_NUMBER_DISABLED; // Team number -1 indicates IP override in use
            await saveNetworkConfigAsync(config);

            // Notify subscribed methods
            onDebugIpOverrideChanged?.Invoke(config.debugIpOverride);
            onTeamNumberChanged?.Invoke(config.teamNumber);
            QueuedLogger.Log($"Updated Key 'debugIpOverride' to {ipOverride}");
        }
        #endregion

        #region System
        public async Task setEnableAutoStartOnBootAsync(bool autoStart)
        {
            var config = await getSystemConfigAsync();
            config.enableAutoStartOnBoot = autoStart;
            await saveSystemConfigAsync(config);

            // Notify subscribed methods
            onEnableAutoStartOnBootChanged?.Invoke(autoStart);
            QueuedLogger.Log($"Updated Key 'autoStartOnBoot' to {autoStart}");
        }
        #endregion

        #region Logging
        public async Task setEnableDebugLoggingAsync(bool enableDebugLogging)
        {
            var config = await getLoggingConfigAsync();
            config.enableDebugLogging = enableDebugLogging;
            await saveLoggingConfigAsync(config);

            // Notify subscribed methods
            onEnableDebugLoggingChanged?.Invoke(enableDebugLogging);
            QueuedLogger.Log($"Updated Key 'enableDebugLogging' to {enableDebugLogging}");
        }
        #endregion
        #endregion

        #region Private Methods
        #region Getters
        private async Task<Config.Network> getNetworkConfigAsync()
        {
            var config = await connection.FindAsync<Config.Network>(1);

            // If we don't find one, create a new one with defaults
            if (config == null)
            {
                config = new Config.Network();
                await saveNetworkConfigAsync(config);
            }
            return config;
        }

        private async Task<Config.System> getSystemConfigAsync()
        {
            var config = await connection.FindAsync<Config.System>(1);

            // If we don't find one, create a new one with defaults
            if (config == null)
            {
                config = new Config.System();
                await saveSystemConfigAsync(config);
            }
            return config;
        }

        private async Task<Config.Logging> getLoggingConfigAsync()
        {
            var config = await connection.FindAsync<Config.Logging>(1);

            // If we don't find one, create a new one with defaults
            if (config == null)
            {
                config = new Config.Logging();
                await saveLoggingConfigAsync(config);
            }
            return config;
        }
        #endregion

        #region Setters
        private async Task saveSystemConfigAsync(Config.System config)
        {
            config.id = 1;
            await connection.InsertOrReplaceAsync(config);
        }

        private async Task saveNetworkConfigAsync(Config.Network config)
        {
            config.id = 1;
            await connection.InsertOrReplaceAsync(config);
        }

        private async Task saveLoggingConfigAsync(Config.Logging config)
        {
            config.id = 1;
            await connection.InsertOrReplaceAsync(config);
        }
        #endregion

        #region Validatiors
        /// <summary>
        /// Validates if a string is a valid IPv4 address
        /// </summary>
        private bool isValidIPAddress(string ipString)
        {
            if (string.IsNullOrEmpty(ipString))
                return false;

            string[] parts = ipString.Split('.');
            if (parts.Length != 4)
                return false;

            foreach (string part in parts)
            {
                if (!int.TryParse(part, out int num))
                    return false;

                if (num < 0 || num > 255)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Validates if an integer is a valid team number
        /// </summary>
        /// <param name="teamNumber">The team number to check</param>
        /// <returns>Whether a team number is valid or not</returns>
        private bool isValidTeamNumber(int teamNumber)
        {
            return teamNumber
                is >= QuestNavConstants.Network.MIN_TEAM_NUMBER
                    and <= QuestNavConstants.Network.MAX_TEAM_NUMBER;
        }
        #endregion
        #endregion
    }
}
