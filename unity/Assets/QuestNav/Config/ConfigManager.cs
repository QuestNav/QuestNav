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
    /// <summary>
    /// Interface for managing application configuration settings.
    /// </summary>
    public interface IConfigManager
    {
        /// <summary>
        /// Initializes the configuration database and fires initial values.
        /// </summary>
        public Task InitializeAsync();

        /// <summary>
        /// Closes the database connection.
        /// </summary>
        public Task CloseAsync();

        /// <summary>
        /// Raised when team number changes.
        /// </summary>
        public event Action<int> OnTeamNumberChanged;

        /// <summary>
        /// Raised when debug IP override changes.
        /// </summary>
        public event Action<string> OnDebugIpOverrideChanged;

        /// <summary>
        /// Raised when auto-start on boot setting changes.
        /// </summary>
        public event Action<bool> OnEnableAutoStartOnBootChanged;

        /// <summary>
        /// Raised when debug logging setting changes.
        /// </summary>
        public event Action<bool> OnEnableDebugLoggingChanged;

        /// <summary>
        /// Gets the configured team number.
        /// </summary>
        /// <returns>
        /// The team number, or -1 if using IP override.
        /// </returns>
        public Task<int> GetTeamNumberAsync();

        /// <summary>
        /// Gets the debug IP override address.
        /// </summary>
        /// <returns>
        /// The IP address string, or empty if using team number.
        /// </returns>
        public Task<string> GetDebugIpOverrideAsync();

        /// <summary>
        /// Gets whether auto-start on boot is enabled.
        /// </summary>
        /// <returns>
        /// True if auto-start is enabled.
        /// </returns>
        public Task<bool> GetEnableAutoStartOnBootAsync();

        /// <summary>
        /// Gets whether debug logging is enabled.
        /// </summary>
        /// <returns>
        /// True if debug logging is enabled.
        /// </returns>
        public Task<bool> GetEnableDebugLoggingAsync();

        /// <summary>
        /// Sets the team number and clears IP override
        /// .</summary>
        /// <seealso cref="SetDebugIpOverrideAsync"/>
        public Task SetTeamNumberAsync(int teamNumber);

        /// <summary>
        /// Sets the debug IP override and disables team number.
        /// </summary>
        /// <seealso cref="SetTeamNumberAsync"/>
        public Task SetDebugIpOverrideAsync(string ipOverride);

        /// <summary>
        /// Sets whether to auto-start on boot.
        /// </summary>
        public Task setEnableAutoStartOnBootAsync(bool autoStart);

        /// <summary>
        /// Sets whether debug logging is enabled.
        /// </summary>
        public Task SetEnableDebugLoggingAsync(bool enableDebugLogging);

        /// <summary>
        /// Resets all settings to defaults.
        /// </summary>
        public Task ResetToDefaultsAsync();
    }

    /// <summary>
    /// Manages application configuration using SQLite persistence.
    /// </summary>
    public class ConfigManager : IConfigManager
    {
        private static readonly string dbPath = Path.Combine(
            Application.persistentDataPath,
            "config.db"
        );
        private SQLiteAsyncConnection connection;
        private SynchronizationContext mainThreadContext;

        #region Lifecycle Methods
        /// <inheritdoc/>
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

            // Fire initial values to all current subscribers
            OnTeamNumberChanged?.Invoke(await GetTeamNumberAsync());
            OnDebugIpOverrideChanged?.Invoke(await GetDebugIpOverrideAsync());
            OnEnableAutoStartOnBootChanged?.Invoke(await GetEnableAutoStartOnBootAsync());
            OnEnableDebugLoggingChanged?.Invoke(await GetEnableDebugLoggingAsync());
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
        /// <inheritdoc/>
        public event Action<int> OnTeamNumberChanged;

        /// <inheritdoc/>
        public event Action<string> OnDebugIpOverrideChanged;

        /// <inheritdoc/>
        public event Action<bool> OnEnableAutoStartOnBootChanged;

        /// <inheritdoc/>
        public event Action<bool> OnEnableDebugLoggingChanged;
        #endregion

        #region Getters
        #region Network
        /// <inheritdoc/>
        public async Task<int> GetTeamNumberAsync()
        {
            var config = await GetNetworkConfigAsync();

            return config.TeamNumber;
        }

        /// <inheritdoc/>
        public async Task<string> GetDebugIpOverrideAsync()
        {
            var config = await GetNetworkConfigAsync();

            return config.DebugIpOverride;
        }
        #endregion

        #region System
        /// <inheritdoc/>
        public async Task<bool> GetEnableAutoStartOnBootAsync()
        {
            var config = await GetSystemConfigAsync();

            return config.EnableAutoStartOnBoot;
        }
        #endregion

        #region Logging
        /// <inheritdoc/>
        public async Task<bool> GetEnableDebugLoggingAsync()
        {
            var config = await GetLoggingConfigAsync();

            return config.EnableDebugLogging;
        }
        #endregion
        #endregion

        #region Setters

        #region Network
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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
        /// <inheritdoc/>
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
        /// <inheritdoc/>
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
        /// <summary>
        /// Gets network config from DB, creating defaults if not found.
        /// </summary>
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

        /// <summary>
        /// Gets system config from DB, creating defaults if not found.
        /// </summary>
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

        /// <summary>
        /// Gets logging config from DB, creating defaults if not found.
        /// </summary>
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
        /// <summary>
        /// Persists system config to the database.
        /// </summary>
        private async Task SaveSystemConfigAsync(Config.System config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }

        /// <summary>
        /// Persists network config to the database.
        /// </summary>
        private async Task SaveNetworkConfigAsync(Config.Network config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }

        /// <summary>
        /// Persists logging config to the database.
        /// </summary>
        private async Task SaveLoggingConfigAsync(Config.Logging config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }
        #endregion

        #region Validators
        /// <summary>
        /// Validates if a string is a valid IPv4 address.
        /// </summary>
        /// <returns>
        /// True if the string is a valid IPv4 address.
        /// </returns>
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
            if (mainThreadContext == null)
            {
                action();
                
            }
            else
            {
                mainThreadContext.Post(_ => action(), null);
            }
        }
        #endregion
    }
}
