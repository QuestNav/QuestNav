using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// Opens the configuration database (creates tables if needed) without firing
        /// any initial events. Idempotent. Use this when a caller needs to read a
        /// boot-only setting before constructing the subsystems that subscribe to events.
        /// </summary>
        public Task OpenAsync();

        /// <summary>
        /// Initializes the configuration database and fires initial values.
        /// Calls <see cref="OpenAsync"/> internally.
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

        /// <summary>
        /// Raised when the Phase-2 confidence preset changes.
        /// </summary>
        public event Action<int> OnAprilTagConfidencePresetChanged;

        /// <summary>
        /// Raised when the AprilTag noise-scale multiplier changes.
        /// </summary>
        public event Action<double> OnAprilTagNoiseScaleChanged;
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

        /// <summary>
        /// Gets the AprilTag field-layout file currently selected. Read once at startup;
        /// changing this value via <see cref="SetAprilTagFieldLayoutFileAsync"/> takes
        /// effect on the next app restart.
        /// </summary>
        public Task<string> GetAprilTagFieldLayoutFileAsync();

        /// <summary>
        /// Gets the AprilTag Phase-2 confidence preset (0 / 1 / 2).
        /// </summary>
        public Task<int> GetAprilTagConfidencePresetAsync();

        /// <summary>
        /// Gets the AprilTag dynamic-std-dev noise-scale multiplier.
        /// </summary>
        public Task<double> GetAprilTagNoiseScaleAsync();
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

        /// <summary>
        /// Sets the AprilTag field-layout file. Persisted immediately; the new layout is
        /// picked up on the next app restart. There is no event for this change because
        /// hot-swapping the layout would invalidate the Kalman estimator's field alignment.
        /// </summary>
        public Task SetAprilTagFieldLayoutFileAsync(string fileName);

        /// <summary>
        /// Sets the AprilTag Phase-2 confidence preset. Fires
        /// <see cref="OnAprilTagConfidencePresetChanged"/> on the main thread.
        /// </summary>
        public Task SetAprilTagConfidencePresetAsync(int preset);

        /// <summary>
        /// Sets the AprilTag noise-scale multiplier. Clamped to [0.5, 2.0].
        /// </summary>
        public Task SetAprilTagNoiseScaleAsync(double scale);
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
        /// <summary>
        /// Opens the SQLite connection and creates / migrates tables, but does NOT fire
        /// any initial events. Idempotent. Used by <see cref="QuestNav"/> startup so that
        /// "boot-only" config values (currently the AprilTag field-layout file) can be
        /// read before the rest of the subsystems are constructed and subscribed.
        ///
        /// You almost certainly want <see cref="InitializeAsync"/> instead, which calls
        /// this and then fires initial events to all subscribers.
        /// </summary>
        public async Task OpenAsync()
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
            await MigrateAprilTagAllowedIdToIgnoredIdAsync();
            await connection.CreateTableAsync<Config.AprilTagIgnoredId>();
            await connection.CreateTableAsync<Config.Logging>();

            QueuedLogger.Log($"Database initialized at: {dbPath}");
        }

        /// <inheritdoc/>
        public async Task InitializeAsync()
        {
            await OpenAsync();

            // Fire initial values to all current subscribers
            OnTeamNumberChanged?.Invoke(await GetTeamNumberAsync());
            OnDebugIpOverrideChanged?.Invoke(await GetDebugIpOverrideAsync());
            OnEnableAutoStartOnBootChanged?.Invoke(await GetEnableAutoStartOnBootAsync());

            OnEnablePassthroughStreamChanged?.Invoke(await GetEnablePassthroughStreamAsync());
            OnPassthroughStreamModeChanged?.Invoke(await GetPassthroughStreamModeAsync());
            OnEnableHighQualityStreamsChanged?.Invoke(await GetEnableHighQualityStreamsAsync());

            // Mode MUST fire before Enable. AprilTagManager caches the requested
            // resolution from the Mode event and uses it when Enable triggers the
            // camera reservation. If Enable fires first, the camera is reserved at
            // (0,0) and the first AprilTag frame fed to libapriltag is malformed,
            // which causes a native SIGSEGV in gradient_clusters.
            OnAprilTagDetectorModeChanged?.Invoke(await GetAprilTagDetectorModeAsync());
            // Phase-2 confidence preset and noise scale must also fire BEFORE Enable so
            // the estimator and the dynamic-std-dev calc are configured with the user's
            // chosen values from the very first observation.
            OnAprilTagConfidencePresetChanged?.Invoke(await GetAprilTagConfidencePresetAsync());
            OnAprilTagNoiseScaleChanged?.Invoke(await GetAprilTagNoiseScaleAsync());
            OnEnableAprilTagDetectorChanged?.Invoke(await GetEnableAprilTagDetectorAsync());

            OnEnableDebugLoggingChanged?.Invoke(await GetEnableDebugLoggingAsync());
        }

        /// <summary>
        /// One-shot migration that renames the legacy <c>AprilTagAllowedId</c> table to
        /// <c>AprilTagIgnoredId</c>. The semantics flipped from a whitelist to a blacklist;
        /// silently reinterpreting the old rows would do the opposite of what the user
        /// originally intended, so any pre-existing rows are dropped (with a warning) rather
        /// than carried over. The previous UI was disabled, so no real-world data exists.
        /// </summary>
        private async Task MigrateAprilTagAllowedIdToIgnoredIdAsync()
        {
            try
            {
                int existing = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM AprilTagAllowedId"
                );
                if (existing > 0)
                {
                    QueuedLogger.LogWarning(
                        $"Found {existing} legacy AprilTagAllowedId rows. The whitelist/blacklist "
                            + "semantics have flipped; dropping legacy rows. Re-enter any IDs you "
                            + "want ignored via the new Ignored Tag IDs field."
                    );
                }
                // The table existed (the COUNT succeeded). Drop it; the fresh
                // AprilTagIgnoredId table is created by the caller right after.
                await connection.ExecuteAsync("DROP TABLE AprilTagAllowedId");
                QueuedLogger.Log(
                    "Migrated AprilTagAllowedId table out of the database (replaced by AprilTagIgnoredId)."
                );
            }
            catch (SQLite.SQLiteException)
            {
                // Fresh database; the legacy table never existed. Normal path on first launch
                // or on a database created after this migration was introduced.
            }
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
                    Array.Empty<int>(),
                    aprilTagDefaults.AprilTagDetectorMaxDistance,
                    aprilTagDefaults.AprilTagDetectorMinimumNumberOfTags
                )
            );
            await SetAprilTagFieldLayoutFileAsync(aprilTagDefaults.AprilTagFieldLayoutFile);
            await SetAprilTagConfidencePresetAsync(aprilTagDefaults.AprilTagConfidencePreset);
            await SetAprilTagNoiseScaleAsync(aprilTagDefaults.AprilTagNoiseScale);

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

        /// <inheritdoc/>
        public event Action<int> OnAprilTagConfidencePresetChanged;

        /// <inheritdoc/>
        public event Action<double> OnAprilTagNoiseScaleChanged;
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
            var ignoredIds = await GetAprilTagIgnoredIdsAsync();

            return new AprilTagDetectorMode(
                (AprilTagDetectorMode.DetectionMode)config.AprilTagDetectorMode,
                config.AprilTagDetectorWidth,
                config.AprilTagDetectorHeight,
                config.AprilTagDetectorFramerate,
                ignoredIds,
                config.AprilTagDetectorMaxDistance,
                config.AprilTagDetectorMinimumNumberOfTags
            );
        }

        /// <inheritdoc/>
        public async Task<string> GetAprilTagFieldLayoutFileAsync()
        {
            var config = await GetAprilTagConfigAsync();
            // Defensive: defaults are normally enforced by the SQLite POCO default value
            // but a corrupt or pre-migration row could yield empty/null.
            return string.IsNullOrEmpty(config.AprilTagFieldLayoutFile)
                ? QuestNavConstants.AprilTag.DEFAULT_FIELD_LAYOUT_FILE
                : config.AprilTagFieldLayoutFile;
        }

        /// <inheritdoc/>
        public async Task<int> GetAprilTagConfidencePresetAsync()
        {
            var config = await GetAprilTagConfigAsync();
            // Clamp to the supported [0, 3] range to defend against a corrupt row.
            // Range is 0=Permissive, 1=Balanced, 2=Strict, 3=Debug. Must stay in sync
            // with SetAprilTagConfidencePresetAsync's clamp and the POST validator in
            // ConfigServer; an out-of-range clamp here silently downgrades the user's
            // selection on the next /api/config poll, which manifests as the AprilTag
            // tab "snapping back" a few seconds after Apply.
            int v = config.AprilTagConfidencePreset;
            if (v < 0)
                v = 0;
            if (v > 3)
                v = 3;
            return v;
        }

        /// <inheritdoc/>
        public async Task<double> GetAprilTagNoiseScaleAsync()
        {
            var config = await GetAprilTagConfigAsync();
            // Clamp to the slider range so a corrupt row can't push the std-dev outside
            // sensible bounds and tank the Kalman filter.
            double v = config.AprilTagNoiseScale;
            if (v < 0.5)
                v = 0.5;
            if (v > 2.0)
                v = 2.0;
            return v;
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
            config.AprilTagDetectorMode = (int)mode.Mode;
            config.AprilTagDetectorWidth = mode.Width;
            config.AprilTagDetectorHeight = mode.Height;
            config.AprilTagDetectorFramerate = mode.Framerate;
            config.AprilTagDetectorMaxDistance = mode.MaxDistance;
            config.AprilTagDetectorMinimumNumberOfTags = mode.MinimumNumberOfTags;
            await SaveAprilTagConfigAsync(config);
            await SaveAprilTagIgnoredIdsAsync(mode.IgnoredIds ?? Array.Empty<int>());

            // Notify subscribed methods on the main thread
            invokeOnMainThread(() => OnAprilTagDetectorModeChanged?.Invoke(mode));
            QueuedLogger.Log($"Updated Key 'aprilTagDetectorMode' to {mode}");
        }

        /// <inheritdoc/>
        public async Task SetAprilTagConfidencePresetAsync(int preset)
        {
            // Clamp to [0, 3] - the only supported values map to ConfidencePreset enum
            // (0 = Permissive, 1 = Balanced, 2 = Strict, 3 = Debug).
            int sanitized = preset;
            if (sanitized < 0)
                sanitized = 0;
            if (sanitized > 3)
                sanitized = 3;

            var config = await GetAprilTagConfigAsync();
            if (config.AprilTagConfidencePreset == sanitized)
            {
                QueuedLogger.Log(
                    $"Key 'aprilTagConfidencePreset' already {sanitized}; no-op write"
                );
                return;
            }
            config.AprilTagConfidencePreset = sanitized;
            await SaveAprilTagConfigAsync(config);

            invokeOnMainThread(() => OnAprilTagConfidencePresetChanged?.Invoke(sanitized));
            QueuedLogger.Log($"Updated Key 'aprilTagConfidencePreset' to {sanitized}");
        }

        /// <inheritdoc/>
        public async Task SetAprilTagNoiseScaleAsync(double scale)
        {
            // Match the slider's clamped range. Refuse non-finite values defensively.
            if (double.IsNaN(scale) || double.IsInfinity(scale))
            {
                QueuedLogger.LogError(
                    $"Refusing non-finite AprilTag noise scale {scale}; ignoring."
                );
                return;
            }
            double sanitized = scale;
            if (sanitized < 0.5)
                sanitized = 0.5;
            if (sanitized > 2.0)
                sanitized = 2.0;

            var config = await GetAprilTagConfigAsync();
            // Use a tight epsilon so we don't write the row for a sub-microscopic delta.
            if (Math.Abs(config.AprilTagNoiseScale - sanitized) < 1e-9)
            {
                QueuedLogger.Log(
                    $"Key 'aprilTagNoiseScale' already {sanitized:F2}; no-op write"
                );
                return;
            }
            config.AprilTagNoiseScale = sanitized;
            await SaveAprilTagConfigAsync(config);

            invokeOnMainThread(() => OnAprilTagNoiseScaleChanged?.Invoke(sanitized));
            QueuedLogger.Log($"Updated Key 'aprilTagNoiseScale' to {sanitized:F2}");
        }

        /// <inheritdoc/>
        public async Task SetAprilTagFieldLayoutFileAsync(string fileName)
        {
            // Empty / null collapses to the default. The setter does NOT validate that the
            // file exists - validation is the caller's job (the web POST handler in
            // ConfigServer rejects unknown bundled names; commit 6 will add custom-file
            // existence checking). Persisting an unknown name will just cause the next
            // app start to fall back to the default layout (with a warning log).
            string sanitized = string.IsNullOrEmpty(fileName)
                ? QuestNavConstants.AprilTag.DEFAULT_FIELD_LAYOUT_FILE
                : fileName;

            var config = await GetAprilTagConfigAsync();
            if (config.AprilTagFieldLayoutFile == sanitized)
            {
                QueuedLogger.Log(
                    $"Key 'aprilTagFieldLayoutFile' already '{sanitized}'; no-op write"
                );
                return;
            }
            config.AprilTagFieldLayoutFile = sanitized;
            await SaveAprilTagConfigAsync(config);

            // No event on purpose. Hot-swapping the field layout would invalidate
            // VioAprilTagPoseEstimator.hasInitialAlignment and the yaw offset; the change
            // is intentionally restart-on-apply and the web UI surfaces that to the user.
            QueuedLogger.Log(
                $"Updated Key 'aprilTagFieldLayoutFile' to '{sanitized}' (effective on next restart)"
            );
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
        /// Gets AprilTag ignored IDs (blacklist) from the DB. Empty list means detect every tag.
        /// </summary>
        /// <returns>
        /// The AprilTag ignored IDs configuration.
        /// </returns>
        private async Task<int[]> GetAprilTagIgnoredIdsAsync()
        {
            // The default is an empty array so no records are created for the default
            var rows = await connection
                .Table<Config.AprilTagIgnoredId>()
                .Where(r => r.AprilTagConfigId == 1)
                .ToListAsync();

            return rows.Select(r => r.IgnoredId).ToArray();
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
        /// Persists AprilTag ignored IDs (blacklist) to the database.
        /// </summary>
        /// <param name="ids">The AprilTag ignored IDs configuration to save.</param>
        private async Task SaveAprilTagIgnoredIdsAsync(IEnumerable<int> ids)
        {
            // single config row uses AprilTagConfigId = 1
            await connection.ExecuteAsync(
                "DELETE FROM AprilTagIgnoredId WHERE AprilTagConfigId = ?",
                1
            );

            // bulk insert (one row per ignored id)
            foreach (var id in ids ?? Array.Empty<int>())
            {
                var entry = new Config.AprilTagIgnoredId { AprilTagConfigId = 1, IgnoredId = id };
                await connection.InsertAsync(entry);
            }
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
