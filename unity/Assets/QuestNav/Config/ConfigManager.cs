using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using QuestNav.Core;
using QuestNav.Utils;
using SQLite;
using UnityEngine;
using static QuestNav.Config.Config;

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

        #region Events
        #region Network
        /// <summary>
        /// Raised when team number changes.
        /// </summary>
        public event Action<int> OnTeamNumberChanged;

        /// <summary>
        /// Raised when debug IP override changes.
        /// </summary>
        public event Action<string> OnDebugIpOverrideChanged;
        #endregion

        #region System
        /// <summary>
        /// Raised when auto-start on boot setting changes.
        /// </summary>
        public event Action<bool> OnEnableAutoStartOnBootChanged;
        #endregion

        #region Camera
        /// <summary>
        /// Raised when passthrough stream setting changes.
        /// </summary>
        public event Action<bool> OnEnablePassthroughStreamChanged;

        /// <summary>
        /// Raised when stream mode changes.
        /// </summary>
        public event Action<StreamMode> OnPassthroughStreamModeChanged;

        /// <summary>
        /// Raised when the high quality stream setting changes.
        /// </summary>
        public event Action<bool> OnEnableHighQualityStreamsChanged;
        #endregion

        #region AprilTag
        /// <summary>
        /// Raised when AprilTag detector enabled setting changes.
        /// </summary>
        public event Action<bool> OnEnableAprilTagDetectorChanged;

        /// <summary>
        /// Raised when AprilTag detector mode changes.
        /// </summary>
        public event Action<AprilTagDetectorMode> OnAprilTagDetectorModeChanged;
        #endregion

        #region Logging
        /// <summary>
        /// Raised when debug logging setting changes.
        /// </summary>
        public event Action<bool> OnEnableDebugLoggingChanged;
        #endregion
        #endregion

        #region Getters
        #region Network
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
        #endregion

        #region System
        /// <summary>
        /// Gets whether auto-start on boot is enabled.
        /// </summary>
        /// <returns>
        /// True if auto-start is enabled.
        /// </returns>
        public Task<bool> GetEnableAutoStartOnBootAsync();
        #endregion

        #region Camera
        /// <summary>
        /// Gets whether streaming the passthrough camera feed over NT and
        /// </summary>
        /// <returns>
        /// True if streaming is enabled.
        /// </returns>
        public Task<bool> GetEnablePassthroughStreamAsync();

        /// <summary>
        /// Gets the stream mode configuration.
        /// </summary>
        /// <returns>
        /// The stream mode with width, height, and framerate.
        /// </returns>
        public Task<StreamMode> GetPassthroughStreamModeAsync();
        
        /// <summary>
        /// Gets whether high quality streaming is enabled for both the AprilTag and Passthrough streams.
        /// </summary>
        /// <returns>
        /// True if high quality streaming is enabled.
        /// </returns>
        public Task<bool> GetEnableHighQualityStreamsAsync();
        #endregion

        #region AprilTag
        /// <summary>
        /// Gets whether AprilTag detector is enabled.
        /// </summary>
        /// <returns>
        /// True if AprilTag detector is enabled.
        /// </returns>
        public Task<bool> GetEnableAprilTagDetectorAsync();

        /// <summary>
        /// Gets the AprilTag detector mode configuration.
        /// </summary>
        /// <returns>
        /// The detector mode with detection mode, resolution, framerate, and filter settings.
        /// </returns>
        public Task<AprilTagDetectorMode> GetAprilTagDetectorModeAsync();
        #endregion
        
        #region Logging
        /// <summary>
        /// Gets whether debug logging is enabled.
        /// </summary>
        /// <returns>
        /// True if debug logging is enabled.
        /// </returns>
        public Task<bool> GetEnableDebugLoggingAsync();
        #endregion
        #endregion

        #region Setters
        #region Network
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
        #endregion

        #region System
        /// <summary>
        /// Sets whether to auto-start on boot.
        /// </summary>
        public Task SetEnableAutoStartOnBootAsync(bool autoStart);
        #endregion

        #region Camera
        /// <summary>
        /// Sets whether to stream passthrough camera over NT and WebUI
        /// </summary>
        public Task SetEnablePassthroughStreamAsync(bool enable);

        /// <summary>
        /// Sets the stream mode configuration.
        /// </summary>
        public Task SetPassthroughStreamModeAsync(StreamMode mode);
        
        /// <summary>
        /// Sets whether to allow high quality stream modes.
        /// </summary>
        public Task SetEnableHighQualityStreamsAsync(bool enabled);
        #endregion

        #region AprilTag
        /// <summary>
        /// Sets whether to enable AprilTag detector.
        /// </summary>
        public Task SetEnableAprilTagDetectorAsync(bool enable);

        /// <summary>
        /// Sets the AprilTag detector mode configuration.
        /// </summary>
        public Task SetAprilTagDetectorModeAsnyc(AprilTagDetectorMode mode);
        #endregion
        #region Logging
        /// <summary>
        /// Sets whether debug logging is enabled.
        /// </summary>
        public Task SetEnableDebugLoggingAsync(bool enableDebugLogging);
        #endregion
        #endregion

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
            await connection.CreateTableAsync<Config.Camera>();
            await connection.CreateTableAsync<Config.AprilTag>();
            await connection.CreateTableAsync<Config.Logging>();

            QueuedLogger.Log($"Database initialized at: {dbPath}");

            // Fire initial values to all current subscribers
            OnTeamNumberChanged?.Invoke(await GetTeamNumberAsync());
            OnDebugIpOverrideChanged?.Invoke(await GetDebugIpOverrideAsync());
            OnEnableAutoStartOnBootChanged?.Invoke(await GetEnableAutoStartOnBootAsync());
            
            OnEnablePassthroughStreamChanged?.Invoke(await GetEnablePassthroughStreamAsync());
            OnPassthroughStreamModeChanged?.Invoke(await GetPassthroughStreamModeAsync());
            OnEnableHighQualityStreamsChanged?.Invoke(await GetEnableHighQualityStreamsAsync());
            
            OnEnableAprilTagDetectorChanged?.Invoke(await GetEnableAprilTagDetectorAsync());
            OnAprilTagDetectorModeChanged?.Invoke(await GetAprilTagDetectorModeAsync());
            
            OnEnableDebugLoggingChanged?.Invoke(await GetEnableDebugLoggingAsync());
        }

        /// <inheritdoc/>
        public async Task ResetToDefaultsAsync()
        {
            var networkDefaults = new Config.Network();
            var systemDefaults = new Config.System();
            var cameraDefaults = new Config.Camera();
            var aprilTagDefaults = new Config.AprilTag();
            var loggingDefaults = new Config.Logging();

            await SetTeamNumberAsync(networkDefaults.TeamNumber);
            await SetEnableAutoStartOnBootAsync(systemDefaults.EnableAutoStartOnBoot);
            
            await SetEnablePassthroughStreamAsync(cameraDefaults.EnablePassthroughStream);
            await SetEnableHighQualityStreamsAsync(cameraDefaults.EnableHighQualityStreams);
            await SetPassthroughStreamModeAsync(
                new StreamMode(
                    cameraDefaults.PassthroughStreamWidth,
                    cameraDefaults.PassthroughStreamHeight,
                    cameraDefaults.PassthroughStreamFramerate,
                    cameraDefaults.PassthroughStreamQuality
                )
            );

            await SetEnableAprilTagDetectorAsync(aprilTagDefaults.EnableAprilTagDetector);
            await SetAprilTagDetectorModeAsnyc(
                new AprilTagDetectorMode(
                    (AprilTagDetectorMode.DetectionMode)aprilTagDefaults.AprilTagDetectorMode,
                    aprilTagDefaults.AprilTagDetectorWidth,
                    aprilTagDefaults.AprilTagDetectorHeight,
                    aprilTagDefaults.AprilTagDetectorFramerate,
                    aprilTagDefaults.AprilTagDetectorAllowedIds,
                    aprilTagDefaults.AprilTagDetectorMaxDistance,
                    aprilTagDefaults.AprilTagDetectorMinimumNumberOfTags
                )
            );
            
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
        #region Network
        /// <inheritdoc/>
        public event Action<int> OnTeamNumberChanged;

        /// <inheritdoc/>
        public event Action<string> OnDebugIpOverrideChanged;
        #endregion

        #region System
        /// <inheritdoc/>
        public event Action<bool> OnEnableAutoStartOnBootChanged;
        #endregion

        #region Camera
        /// <inheritdoc/>
        public event Action<bool> OnEnablePassthroughStreamChanged;

        /// <inheritdoc/>
        public event Action<StreamMode> OnPassthroughStreamModeChanged;

        /// <inheritdoc/>
        public event Action<bool> OnEnableHighQualityStreamsChanged;
        #endregion

        #region AprilTag
        /// <inheritdoc/>
        public event Action<bool> OnEnableAprilTagDetectorChanged;

        /// <inheritdoc/>
        public event Action<AprilTagDetectorMode> OnAprilTagDetectorModeChanged;
        #endregion

        #region Logging
        /// <inheritdoc/>
        public event Action<bool> OnEnableDebugLoggingChanged;
        #endregion
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

        #region Camera
        /// <inheritdoc/>
        public async Task<bool> GetEnablePassthroughStreamAsync()
        {
            var config = await GetCameraConfigAsync();

            return config.EnablePassthroughStream;
        }

        /// <inheritdoc/>
        public async Task<StreamMode> GetPassthroughStreamModeAsync()
        {
            var config = await GetCameraConfigAsync();

            return new StreamMode(
                config.PassthroughStreamWidth,
                config.PassthroughStreamHeight,
                config.PassthroughStreamFramerate,
                config.PassthroughStreamQuality
            );
        }
        
        /// <inheritdoc/>
        public async Task<bool> GetEnableHighQualityStreamsAsync()
        {
            var config = await GetCameraConfigAsync();

            return config.EnableHighQualityStreams;
        }
        
        #endregion

        #region AprilTag
        /// <inheritdoc/>
        public async Task<bool> GetEnableAprilTagDetectorAsync()
        {
            var config = await GetAprilTagConfigAsync();

            return config.EnableAprilTagDetector;
        }

        /// <inheritdoc/>
        public async Task<AprilTagDetectorMode> GetAprilTagDetectorModeAsync()
        {
            var config = await GetAprilTagConfigAsync();

            return new AprilTagDetectorMode(
                (AprilTagDetectorMode.DetectionMode)config.AprilTagDetectorMode,
                config.AprilTagDetectorWidth,
                config.AprilTagDetectorHeight,
                config.AprilTagDetectorFramerate,
                config.AprilTagDetectorAllowedIds,
                config.AprilTagDetectorMaxDistance,
                config.AprilTagDetectorMinimumNumberOfTags
            );
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
        public async Task SetEnableAutoStartOnBootAsync(bool autoStart)
        {
            var config = await GetSystemConfigAsync();
            config.EnableAutoStartOnBoot = autoStart;
            await SaveSystemConfigAsync(config);

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnEnableAutoStartOnBootChanged?.Invoke(autoStart));
            QueuedLogger.Log($"Updated Key 'autoStartOnBoot' to {autoStart}");
        }
        #endregion

        #region Camera
        /// <inheritdoc/>
        public async Task SetEnablePassthroughStreamAsync(bool enable)
        {
            var config = await GetCameraConfigAsync();
            config.EnablePassthroughStream = enable;
            await SaveCameraConfigAsync(config);

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnEnablePassthroughStreamChanged?.Invoke(enable));
            QueuedLogger.Log($"Updated Key 'enablePassthroughStream' to {enable}");
        }

        /// <inheritdoc/>
        public async Task SetPassthroughStreamModeAsync(StreamMode mode)
        {
            var config = await GetCameraConfigAsync();
            config.PassthroughStreamWidth = mode.Width;
            config.PassthroughStreamHeight = mode.Height;
            config.PassthroughStreamFramerate = mode.Framerate;
            config.PassthroughStreamQuality = mode.Quality;
            await SaveCameraConfigAsync(config);

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnPassthroughStreamModeChanged?.Invoke(mode));
            QueuedLogger.Log($"Updated Key 'passthroughStreamMode' to {mode}");
        }
        
        /// <inheritdoc/>
        public async Task SetEnableHighQualityStreamsAsync(bool enabled)
        {
            var config = await GetCameraConfigAsync();
            config.EnableHighQualityStreams = enabled;
            await SaveCameraConfigAsync(config);

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnEnableHighQualityStreamsChanged?.Invoke(enabled));
            QueuedLogger.Log($"Updated Key 'enableHighQualityStreams' to {enabled}");
        }
        #endregion

        #region AprilTag
        /// <inheritdoc/>
        public async Task SetEnableAprilTagDetectorAsync(bool enable)
        {
            var config = await GetAprilTagConfigAsync();
            config.EnableAprilTagDetector = enable;
            await SaveAprilTagConfigAsync(config);
            
            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnEnableAprilTagDetectorChanged?.Invoke(enable));
            QueuedLogger.Log($"Updated Key 'enableAprilTagDetector' to {enable}");
        }

        /// <inheritdoc/>
        public async Task SetAprilTagDetectorModeAsnyc(AprilTagDetectorMode mode)
        {
            var config = await GetAprilTagConfigAsync();
            config.AprilTagDetectorMode = (int) mode.Mode;
            config.AprilTagDetectorWidth = mode.Width;
            config.AprilTagDetectorHeight = mode.Height;
            config.AprilTagDetectorFramerate = mode.Framerate;
            config.AprilTagDetectorAllowedIds = mode.AllowedIds;
            config.AprilTagDetectorMaxDistance = mode.MaxDistance;
            config.AprilTagDetectorMinimumNumberOfTags = mode.MinimumNumberOfTags;
            await SaveAprilTagConfigAsync(config);

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnAprilTagDetectorModeChanged?.Invoke(mode));
            QueuedLogger.Log($"Updated Key 'aprilTagDetectorMode' to {mode}");
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
        /// <returns>
        /// The network configuration.
        /// </returns>
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
        /// <returns>
        /// The system configuration.
        /// </returns>
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
        /// Gets camera config from DB, creating defaults if not found.
        /// </summary>
        /// <returns>
        /// The camera configuration.
        /// </returns>
        private async Task<Config.Camera> GetCameraConfigAsync()
        {
            var config = await connection.FindAsync<Config.Camera>(1);

            // If we don't find one, create a new one with defaults
            if (config == null)
            {
                config = new Config.Camera();
                await SaveCameraConfigAsync(config);
            }
            return config;
        }
        
        /// <summary>
        /// Gets AprilTag config from DB, creating defaults if not found.
        /// </summary>
        /// <returns>
        /// The AprilTag configuration.
        /// </returns>
        private async Task<Config.AprilTag> GetAprilTagConfigAsync()
        {
            var config = await connection.FindAsync<Config.AprilTag>(1);

            // If we don't find one, create a new one with defaults
            if (config == null)
            {
                config = new Config.AprilTag();
                await SaveAprilTagConfigAsync(config);
            }
            return config;
        }

        /// <summary>
        /// Gets logging config from DB, creating defaults if not found.
        /// </summary>
        /// <returns>
        /// The logging configuration.
        /// </returns>
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
        /// <param name="config">The system configuration to save.</param>
        private async Task SaveSystemConfigAsync(Config.System config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }

        /// <summary>
        /// Persists network config to the database.
        /// </summary>
        /// <param name="config">The network configuration to save.</param>
        private async Task SaveNetworkConfigAsync(Config.Network config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }

        /// <summary>
        /// Persists camera config to the database.
        /// </summary>
        /// <param name="config">The camera configuration to save.</param>
        private async Task SaveCameraConfigAsync(Config.Camera config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }
        
        /// <summary>
        /// Persists AprilTag config to the database.
        /// </summary>
        /// <param name="config">The AprilTag configuration to save.</param>
        private async Task SaveAprilTagConfigAsync(Config.AprilTag config)
        {
            config.ID = 1;
            await connection.InsertOrReplaceAsync(config);
        }

        /// <summary>
        /// Persists logging config to the database.
        /// </summary>
        /// <param name="config">The logging configuration to save.</param>
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
        /// <param name="ipString">The IP address string to validate.</param>
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
        /// <param name="action">The action to invoke on the main thread.</param>
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