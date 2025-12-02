using System;
using System.IO;
using System.Threading;
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
        public Task InitializeAsync();
        public Task CloseAsync();

        public event Action<int> OnTeamNumberChanged;
        public event Action<string> OnDebugIpOverrideChanged;
        public event Action<bool> OnEnableAutoStartOnBootChanged;
        public event Action<bool> OnEnableDebugLoggingChanged;

        public Task<int> GetTeamNumberAsync();
        public Task<string> GetDebugIpOverrideAsync();
        public Task<bool> getEnableAutoStartOnBootAsync();
        public Task<bool> GetEnableDebugLoggingAsync();
        public Task SetTeamNumberAsync(int teamNumber);
        public Task SetDebugIpOverrideAsync(string ipOverride);
        public Task setEnableAutoStartOnBootAsync(bool autoStart);
        public Task SetEnableDebugLoggingAsync(bool enableDebugLogging);
        public Task ResetToDefaultsAsync();
    }

    public class ConfigManager : IConfigManager
    {
        private static readonly string dbPath = Path.Combine(
            Application.persistentDataPath,
            "config.db"
        );
        private SQLiteAsyncConnection connection;
        private SynchronizationContext mainThreadContext;

        #region Lifecycle Methods
        public async Task InitializeAsync()
        {
            // Capture the main thread context for event callbacks
            mainThreadContext = SynchronizationContext.Current;

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

        public async Task ResetToDefaultsAsync()
        {
            var networkDefaults = new Config.Network();
            var systemDefaults = new Config.System();
            var loggingDefaults = new Config.Logging();

            await SetTeamNumberAsync(networkDefaults.TeamNumber);
            await setEnableAutoStartOnBootAsync(systemDefaults.EnableAutoStartOnBoot);
            await SetEnableDebugLoggingAsync(loggingDefaults.EnableDebugLogging);

            QueuedLogger.Log("Database reset to defaults");
        }

        public async Task CloseAsync()
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
        public event Action<int> OnTeamNumberChanged;
        public event Action<string> OnDebugIpOverrideChanged;

        // System events
        public event Action<bool> OnEnableAutoStartOnBootChanged;

        // Logging events
        public event Action<bool> OnEnableDebugLoggingChanged;
        #endregion

        #region Getters
        #region Network
        public async Task<int> GetTeamNumberAsync()
        {
            var config = await GetNetworkConfigAsync();

            return config.TeamNumber;
        }

        public async Task<string> GetDebugIpOverrideAsync()
        {
            var config = await GetNetworkConfigAsync();

            return config.DebugIpOverride;
        }
        #endregion

        #region System
        public async Task<bool> getEnableAutoStartOnBootAsync()
        {
            var config = await GetSystemConfigAsync();

            return config.EnableAutoStartOnBoot;
        }
        #endregion

        #region Logging
        public async Task<bool> GetEnableDebugLoggingAsync()
        {
            var config = await GetLoggingConfigAsync();

            return config.EnableDebugLogging;
        }
        #endregion
        #endregion

        #region Setters

        #region Network
        public async Task SetTeamNumberAsync(int teamNumber)
        {
            // Validate new value
            if (!IsValidTeamNumber(teamNumber))
            {
                QueuedLogger.LogError(
                    "Attempted to write a non-valid team number to the config! Aborting..."
                );
                return;
            }

            var config = await GetNetworkConfigAsync();
            config.TeamNumber = teamNumber;
            config.DebugIpOverride = ""; // Blank out IP override
            await SaveNetworkConfigAsync(config);

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnTeamNumberChanged?.Invoke(config.TeamNumber));
            invokeOnMainThread(() => OnDebugIpOverrideChanged?.Invoke(config.DebugIpOverride));
            QueuedLogger.Log($"Updated Key 'teamNumber' to {teamNumber}");
        }

        public async Task SetDebugIpOverrideAsync(string ipOverride)
        {
            // Validate new value (blank means disable)
            if (!IsValidIPAddress(ipOverride))
            {
                QueuedLogger.LogError(
                    "Attempted to write a non-valid debug IP override to the config! Aborting..."
                );
                return;
            }

            var config = await GetNetworkConfigAsync();
            config.DebugIpOverride = ipOverride;
            config.TeamNumber = QuestNavConstants.Network.TEAM_NUMBER_DISABLED; // Team number -1 indicates IP override in use
            await SaveNetworkConfigAsync(config);

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnDebugIpOverrideChanged?.Invoke(config.DebugIpOverride));
            invokeOnMainThread(() => OnTeamNumberChanged?.Invoke(config.TeamNumber));
            QueuedLogger.Log($"Updated Key 'debugIpOverride' to {ipOverride}");
        }
        #endregion

        #region System
        public async Task setEnableAutoStartOnBootAsync(bool autoStart)
        {
            var config = await GetSystemConfigAsync();
            config.EnableAutoStartOnBoot = autoStart;
            await SaveSystemConfigAsync(config);

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnEnableAutoStartOnBootChanged?.Invoke(autoStart));
            QueuedLogger.Log($"Updated Key 'autoStartOnBoot' to {autoStart}");
        }
        #endregion

        #region Logging
        public async Task SetEnableDebugLoggingAsync(bool enableDebugLogging)
        {
            var config = await GetLoggingConfigAsync();
            config.EnableDebugLogging = enableDebugLogging;
            await SaveLoggingConfigAsync(config);

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnEnableDebugLoggingChanged?.Invoke(enableDebugLogging));
            QueuedLogger.Log($"Updated Key 'enableDebugLogging' to {enableDebugLogging}");
        }
        #endregion
        #endregion

        #region Private Methods
        #region Getters
        private async Task<Config.Network> GetNetworkConfigAsync()
        {
            var config = await connection.FindAsync<Config.Network>(1);

            // If we don't find one, create a new one with defaults
            if (config == null)
            {
                config = new Config.Network();
                await SaveNetworkConfigAsync(config);
            }
            return config;
        }

        private async Task<Config.System> GetSystemConfigAsync()
        {
            var config = await connection.FindAsync<Config.System>(1);

            // If we don't find one, create a new one with defaults
            if (config == null)
            {
                config = new Config.System();
                await SaveSystemConfigAsync(config);
            }
            return config;
        }

        private async Task<Config.Logging> GetLoggingConfigAsync()
        {
            var config = await connection.FindAsync<Config.Logging>(1);

            // If we don't find one, create a new one with defaults
            if (config == null)
            {
                config = new Config.Logging();
                await SaveLoggingConfigAsync(config);
            }
            return config;
        }
        #endregion

        #region Setters
        private async Task SaveSystemConfigAsync(Config.System config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }

        private async Task SaveNetworkConfigAsync(Config.Network config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }

        private async Task SaveLoggingConfigAsync(Config.Logging config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }
        #endregion

        #region Validatiors
        /// <summary>
        /// Validates if a string is a valid IPv4 address
        /// </summary>
        private bool IsValidIPAddress(string ipString)
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
        private bool IsValidTeamNumber(int teamNumber)
        {
            return teamNumber
                is >= QuestNavConstants.Network.MIN_TEAM_NUMBER
                    and <= QuestNavConstants.Network.MAX_TEAM_NUMBER;
        }
        #endregion

        /// <summary>
        /// Invokes an action on the main thread using the captured SynchronizationContext.
        /// Falls back to direct invocation if no context was captured.
        /// </summary>
        private void invokeOnMainThread(Action action)
        {
            if (mainThreadContext != null)
            {
                mainThreadContext.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }
        #endregion
    }
}
