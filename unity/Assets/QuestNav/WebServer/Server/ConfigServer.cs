using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using Meta.XR;
using Newtonsoft.Json;
using QuestNav.Config;
using QuestNav.Core;
using QuestNav.Utils;
using UnityEngine;
using static QuestNav.Config.Config;
// The detection-pipeline namespaces are doubly-prefixed (QuestNav.QuestNav.*); using
// aliases here avoids ambiguity with the project root namespace QuestNav when this file
// is itself inside QuestNav.WebServer.Server. Otherwise a bare QuestNav.QuestNav.AprilTag
// resolves to QuestNav.WebServer.Server.QuestNav.QuestNav.AprilTag and fails to find it.
using AprilTagFieldLayoutDomain = QuestNav.QuestNav.AprilTag.AprilTagFieldLayout;

namespace QuestNav.WebServer.Server
{
    /// <summary>
    /// Cached server information captured on main thread.
    /// </summary>
    public class CachedServerInfo
    {
        public string AppName;
        public string Version;
        public string UnityVersion;
        public string BuildDate;
        public string Platform;
        public string DeviceModel;
        public string OperatingSystem;
    }

    /// <summary>
    /// HTTP server for configuration management using SQLite-based ConfigManager.
    /// </summary>
    public class ConfigServer
    {
        private EmbedIO.WebServer server;
        private CancellationTokenSource cancellationTokenSource;
        private readonly IConfigManager configManager;
        private readonly int port;
        private readonly bool enableCorsDevMode;
        private readonly string staticPath;
        private readonly ILogger logger;
        private readonly WebServerManager webServerManager;
        private readonly StatusProvider statusProvider;
        private readonly LogCollector logCollector;

        private CachedServerInfo cachedServerInfo;
        private readonly string cachedDatabasePath;

        private readonly VideoStreamProvider streamProvider;

        /// <summary>
        /// Meta SDK passthrough camera reference. Used to enumerate supported resolutions
        /// for the AprilTag video-modes endpoint independently of the passthrough stream
        /// state (the AprilTag detector and the passthrough stream both use the same
        /// physical camera but the AprilTag detector should still get a populated
        /// dropdown even when the passthrough stream is disabled).
        /// </summary>
        private readonly PassthroughCameraAccess cameraAccess;

        /// <summary>
        /// Stream provider instance (injected)
        /// </summary>
        private readonly System.Collections.Generic.Dictionary<string, DateTime> activeClients =
            new System.Collections.Generic.Dictionary<string, DateTime>();
        private readonly object clientsLock = new object();
        private readonly TimeSpan activeClientWindow = TimeSpan.FromSeconds(30);

        public bool IsRunning => server != null && server.State == WebServerState.Listening;
        public string BaseUrl => $"http://localhost:{port}/";

        /// <summary>
        /// Initializes a new ConfigServer instance.
        /// Must be called from Unity main thread to cache Unity-specific information.
        /// </summary>
        /// <param name="configManager">Configuration manager for reading/writing settings.</param>
        /// <param name="port">HTTP server port.</param>
        /// <param name="enableCorsDevMode">Enable CORS for development.</param>
        /// <param name="staticPath">Path to static web UI files.</param>
        /// <param name="logger">Logger implementation for background thread.</param>
        /// <param name="webServerManager">Web server manager for restart/reset callbacks.</param>
        /// <param name="statusProvider">Status provider instance for runtime data.</param>
        /// <param name="logCollector">Log collector instance for log messages.</param>
        /// <param name="streamProvider">Stream provider instance for video streaming.</param>
        /// <param name="cameraAccess">Meta SDK camera reference used for the AprilTag video-modes endpoint.</param>
        public ConfigServer(
            IConfigManager configManager,
            int port,
            bool enableCorsDevMode,
            string staticPath,
            ILogger logger,
            WebServerManager webServerManager,
            StatusProvider statusProvider,
            LogCollector logCollector,
            VideoStreamProvider streamProvider,
            PassthroughCameraAccess cameraAccess
        )
        {
            this.configManager = configManager;
            this.port = port;
            this.enableCorsDevMode = enableCorsDevMode;
            this.staticPath = staticPath;
            this.logger = logger;
            this.webServerManager = webServerManager;
            this.statusProvider = statusProvider;
            this.logCollector = logCollector;
            this.streamProvider = streamProvider;
            this.cameraAccess = cameraAccess;

            cachedDatabasePath = Path.Combine(Application.persistentDataPath, "config.db");

            CacheServerInfo();
        }

        private void CacheServerInfo()
        {
            cachedServerInfo = new CachedServerInfo
            {
                AppName = UnityEngine.Application.productName,
                Version = UnityEngine.Application.version,
                UnityVersion = UnityEngine.Application.unityVersion,
                BuildDate = System
                    .IO.File.GetLastWriteTime(UnityEngine.Application.dataPath)
                    .ToString("yyyy-MM-dd HH:mm:ss"),
                Platform = UnityEngine.Application.platform.ToString(),
                DeviceModel = UnityEngine.SystemInfo.deviceModel,
                OperatingSystem = UnityEngine.SystemInfo.operatingSystem,
            };
        }

        public async Task StartAsync()
        {
            if (IsRunning)
            {
                logger?.LogWarning("[ConfigServer] Server already running");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            logger?.Log($"[ConfigServer] Starting server on port {port}");
            logger?.Log($"[ConfigServer] Static files path: {staticPath}");

            var listeningTcs = new TaskCompletionSource<bool>();

            server = new EmbedIO.WebServer(o =>
                o.WithUrlPrefix($"http://*:{port}/").WithMode(HttpListenerMode.EmbedIO)
            )
                .WithModule(new ActionModule("/api", HttpVerbs.Any, HandleApiRequest))
                .WithModule(new ActionModule("/video", HttpVerbs.Get, HandleVideoStream))
                .WithStaticFolder("/", staticPath, true);
            server.Listener.IgnoreWriteExceptions = false;

            server.StateChanged += (s, e) =>
            {
                if (e.NewState == WebServerState.Listening)
                {
                    listeningTcs.TrySetResult(true);
                }
            };

            try
            {
                Swan.Logging.Logger.UnregisterLogger<Swan.Logging.ConsoleLogger>();
            }
            catch
            {
                logger?.Log("[ConfigServer] Failed to unregister logger!");
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await server.RunAsync(cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    logger?.LogError($"[ConfigServer] Server error: {ex.Message}");
                    listeningTcs.TrySetResult(false);
                }
            });

            await listeningTcs.Task;

            logger?.Log($"[ConfigServer] Server started at {BaseUrl}");
        }

        private async Task HandleVideoStream(IHttpContext context)
        {
            if (streamProvider is not null)
            {
                await streamProvider.HandleStreamAsync(context);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                context.Response.StatusDescription = nameof(HttpStatusCode.NoContent);
                await context.SendStringAsync(
                    "streamProvider is not initialized",
                    "application/text",
                    Encoding.Default
                );
            }
        }

        /// <summary>
        /// Stops the HTTP server and releases resources.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;
            logger?.Log("[ConfigServer] Stopping server...");
            cancellationTokenSource?.Cancel();
            server?.Dispose();
            server = null;
            logger?.Log("[ConfigServer] Server stopped");
        }

        private void RecordClientActivity(string clientIp)
        {
            if (string.IsNullOrEmpty(clientIp))
                return;
            lock (clientsLock)
            {
                activeClients[clientIp] = DateTime.UtcNow;
            }
        }

        private int GetActiveClientCount()
        {
            lock (clientsLock)
            {
                var now = DateTime.UtcNow;
                var staleClients = new System.Collections.Generic.List<string>();
                foreach (var kvp in activeClients)
                {
                    if (now - kvp.Value > activeClientWindow)
                    {
                        staleClients.Add(kvp.Key);
                    }
                }
                foreach (var client in staleClients)
                {
                    activeClients.Remove(client);
                }
                return activeClients.Count;
            }
        }

        private async Task SendJsonResponse(IHttpContext context, object data)
        {
            context.Response.ContentType = "application/json";
            string json = JsonConvert.SerializeObject(data, Formatting.None);
            await context.SendStringAsync(json, "application/json", System.Text.Encoding.UTF8);
        }

        private async Task HandleApiRequest(IHttpContext context)
        {
            try
            {
                string clientIp = context.Request.RemoteEndPoint?.Address?.ToString();
                RecordClientActivity(clientIp);

                if (enableCorsDevMode)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (context.Request.HttpVerb == HttpVerbs.Options)
                {
                    context.Response.StatusCode = 200;
                    return;
                }

                string path = context.Request.Url.AbsolutePath;

                if (path == "/api/config" && context.Request.HttpVerb == HttpVerbs.Get)
                {
                    await HandleGetConfig(context);
                }
                else if (path == "/api/config" && context.Request.HttpVerb == HttpVerbs.Post)
                {
                    await HandlePostConfig(context);
                }
                else if (path == "/api/reset-config" && context.Request.HttpVerb == HttpVerbs.Post)
                {
                    await HandleResetConfig(context);
                }
                else if (
                    path == "/api/download-database"
                    && context.Request.HttpVerb == HttpVerbs.Get
                )
                {
                    await HandleDownloadDatabase(context);
                }
                else if (
                    path == "/api/upload-database"
                    && context.Request.HttpVerb == HttpVerbs.Post
                )
                {
                    await HandleUploadDatabase(context);
                }
                else if (path == "/api/info" && context.Request.HttpVerb == HttpVerbs.Get)
                {
                    await HandleGetInfo(context);
                }
                else if (path == "/api/status" && context.Request.HttpVerb == HttpVerbs.Get)
                {
                    await HandleGetStatus(context);
                }
                else if (path == "/api/logs" && context.Request.HttpVerb == HttpVerbs.Get)
                {
                    await HandleGetLogs(context);
                }
                else if (path == "/api/logs" && context.Request.HttpVerb == HttpVerbs.Delete)
                {
                    await HandleClearLogs(context);
                }
                else if (path == "/api/restart" && context.Request.HttpVerb == HttpVerbs.Post)
                {
                    await HandleRestart(context);
                }
                else if (path == "/api/reset-pose" && context.Request.HttpVerb == HttpVerbs.Post)
                {
                    await HandleResetPose(context);
                }
                else if (path == "/api/video-modes" && context.Request.HttpVerb == HttpVerbs.Get)
                {
                    await HandleGetVideoModes(context);
                }
                else if (
                    path == "/api/apriltag-field-layouts"
                    && context.Request.HttpVerb == HttpVerbs.Get
                )
                {
                    await HandleGetAprilTagFieldLayouts(context);
                }
                else if (
                    path == "/api/apriltag-field-layouts"
                    && context.Request.HttpVerb == HttpVerbs.Post
                )
                {
                    await HandlePostAprilTagFieldLayout(context);
                }
                else if (
                    path.StartsWith("/api/apriltag-field-layouts/")
                    && context.Request.HttpVerb == HttpVerbs.Get
                )
                {
                    await HandleGetAprilTagFieldLayoutContent(
                        context,
                        path.Substring("/api/apriltag-field-layouts/".Length)
                    );
                }
                else if (
                    path.StartsWith("/api/apriltag-field-layouts/")
                    && context.Request.HttpVerb == HttpVerbs.Patch
                )
                {
                    await HandlePatchAprilTagFieldLayout(
                        context,
                        path.Substring("/api/apriltag-field-layouts/".Length)
                    );
                }
                else if (
                    path.StartsWith("/api/apriltag-field-layouts/")
                    && context.Request.HttpVerb == HttpVerbs.Delete
                )
                {
                    await HandleDeleteAprilTagFieldLayout(
                        context,
                        path.Substring("/api/apriltag-field-layouts/".Length)
                    );
                }
                else if (
                    path == "/api/apriltag-video-modes"
                    && context.Request.HttpVerb == HttpVerbs.Get
                )
                {
                    await HandleGetAprilTagVideoModes(context);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await SendJsonResponse(
                        context,
                        new SimpleResponse { success = false, message = "Not found" }
                    );
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"[ConfigServer] Request error: {ex.Message}");
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = ex.Message }
                );
            }
        }

        private async Task HandleGetConfig(IHttpContext context)
        {
            var streamMode = await configManager.GetPassthroughStreamModeAsync();
            var aprilTagDetectorMode = await configManager.GetAprilTagDetectorModeAsync();
            var response = new ConfigResponse
            {
                success = true,
                teamNumber = await configManager.GetTeamNumberAsync(),
                debugIpOverride = await configManager.GetDebugIpOverrideAsync(),
                enableAutoStartOnBoot = await configManager.GetEnableAutoStartOnBootAsync(),
                enablePassthroughStream = await configManager.GetEnablePassthroughStreamAsync(),
                enableHighQualityStream = await configManager.GetEnableHighQualityStreamsAsync(),
                streamMode = new StreamModeModel
                {
                    width = streamMode.Width,
                    height = streamMode.Height,
                    framerate = streamMode.Framerate,
                    quality = streamMode.Quality,
                },
                enableAprilTagDetector = await configManager.GetEnableAprilTagDetectorAsync(),
                aprilTagDetectorMode = new AprilTagDetectorModeModel
                {
                    mode = (int)aprilTagDetectorMode.Mode,
                    width = aprilTagDetectorMode.Width,
                    height = aprilTagDetectorMode.Height,
                    framerate = aprilTagDetectorMode.Framerate,
                    ignoredIds = aprilTagDetectorMode.IgnoredIds,
                    maxDistance = aprilTagDetectorMode.MaxDistance,
                    minimumNumberOfTags = aprilTagDetectorMode.MinimumNumberOfTags,
                    fieldLayoutFile = await configManager.GetAprilTagFieldLayoutFileAsync(),
                },
                enableDebugLogging = await configManager.GetEnableDebugLoggingAsync(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            await SendJsonResponse(context, response);
        }

        private async Task HandlePostConfig(IHttpContext context)
        {
            string body = await context.GetRequestBodyAsStringAsync();
            var request = JsonConvert.DeserializeObject<ConfigUpdateRequest>(body);

            if (request == null)
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = "Invalid request" }
                );
                return;
            }

            try
            {
                if (request.TeamNumber.HasValue)
                {
                    await configManager.SetTeamNumberAsync(request.TeamNumber.Value);
                }
                if (request.debugIpOverride != null)
                {
                    await configManager.SetDebugIpOverrideAsync(request.debugIpOverride);
                }
                if (request.EnableAutoStartOnBoot.HasValue)
                {
                    await configManager.SetEnableAutoStartOnBootAsync(
                        request.EnableAutoStartOnBoot.Value
                    );
                }
                if (request.EnablePassthroughStream.HasValue)
                {
                    await configManager.SetEnablePassthroughStreamAsync(
                        request.EnablePassthroughStream.Value
                    );
                }
                if (request.EnableHighQualityStream.HasValue)
                {
                    await configManager.SetEnableHighQualityStreamsAsync(
                        request.EnableHighQualityStream.Value
                    );
                }
                if (request.StreamMode != null)
                {
                    await configManager.SetPassthroughStreamModeAsync(
                        new StreamMode(
                            request.StreamMode.width,
                            request.StreamMode.height,
                            request.StreamMode.framerate,
                            request.StreamMode.quality
                        )
                    );
                }
                // Apply mode BEFORE enable when both are present in the same request.
                // OnAprilTagDetectorModeChanged caches the resolution that
                // OnEnableAprilTagDetectorChanged uses to reserve the camera; with the
                // wrong order, enabling would reserve at a stale resolution and the next
                // mode change would re-bounce the camera unnecessarily.
                if (request.AprilTagDetectorMode != null)
                {
                    var atm = request.AprilTagDetectorMode;

                    // Validate resolution / framerate against the camera's supported modes.
                    // Reject 400 BEFORE persisting anything from this request so the API and
                    // UI cannot get out of sync. The dropdown in the UI is driven from the
                    // same source (/api/apriltag-video-modes) so this should only fire for
                    // direct API callers / scripts.
                    if (!IsValidAprilTagMode(atm.width, atm.height, atm.framerate))
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(
                            context,
                            new SimpleResponse
                            {
                                success = false,
                                message =
                                    $"Resolution {atm.width}x{atm.height} @ {atm.framerate}fps is not supported on this camera. "
                                    + "Use GET /api/apriltag-video-modes to list valid combinations.",
                            }
                        );
                        return;
                    }

                    // Validate minimumNumberOfTags against the dropdown's allowed range.
                    bool validMinTags = false;
                    foreach (var n in QuestNavConstants.AprilTag.MINIMUM_TAGS_OPTIONS)
                    {
                        if (n == atm.minimumNumberOfTags)
                        {
                            validMinTags = true;
                            break;
                        }
                    }
                    if (!validMinTags)
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(
                            context,
                            new SimpleResponse
                            {
                                success = false,
                                message =
                                    $"minimumNumberOfTags={atm.minimumNumberOfTags} is not in the supported set "
                                    + "[1, 2, 3, 4]. Use one of those values.",
                            }
                        );
                        return;
                    }

                    await configManager.SetAprilTagDetectorModeAsnyc(
                        new AprilTagDetectorMode(
                            (AprilTagDetectorMode.DetectionMode)atm.mode,
                            atm.width,
                            atm.height,
                            atm.framerate,
                            atm.ignoredIds,
                            atm.maxDistance,
                            atm.minimumNumberOfTags
                        )
                    );

                    // Field layout is restart-on-change. A value that doesn't exist is
                    // rejected with a 400 and the rest of the AprilTagDetectorMode update
                    // sticks (it was already persisted by SetAprilTagDetectorModeAsnyc above).
                    if (!string.IsNullOrEmpty(atm.fieldLayoutFile))
                    {
                        if (!IsValidFieldLayoutChoice(atm.fieldLayoutFile))
                        {
                            context.Response.StatusCode = 400;
                            await SendJsonResponse(
                                context,
                                new SimpleResponse
                                {
                                    success = false,
                                    message =
                                        $"Unknown field layout '{atm.fieldLayoutFile}'. "
                                        + "Use GET /api/apriltag-field-layouts to list valid options.",
                                }
                            );
                            return;
                        }

                        await configManager.SetAprilTagFieldLayoutFileAsync(atm.fieldLayoutFile);
                    }
                }
                if (request.EnableAprilTagDetector.HasValue)
                {
                    await configManager.SetEnableAprilTagDetectorAsync(
                        request.EnableAprilTagDetector.Value
                    );
                }
                if (request.EnableDebugLogging.HasValue)
                {
                    await configManager.SetEnableDebugLoggingAsync(
                        request.EnableDebugLogging.Value
                    );
                }

                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = true, message = "Configuration updated" }
                );
            }
            catch (Exception ex)
            {
                logger?.LogError($"[ConfigServer] Failed to apply config update: {ex.Message}");
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = ex.Message }
                );
            }
        }

        private async Task HandleResetConfig(IHttpContext context)
        {
            try
            {
                await configManager.ResetToDefaultsAsync();
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = true,
                        message = "Configuration reset to defaults",
                    }
                );
            }
            catch (Exception ex)
            {
                logger?.LogError($"[ConfigServer] Failed to reset config: {ex.Message}");
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = ex.Message }
                );
            }
        }

        private async Task HandleDownloadDatabase(IHttpContext context)
        {
            if (!File.Exists(cachedDatabasePath))
            {
                context.Response.StatusCode = 404;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = "Database file not found" }
                );
                return;
            }

            try
            {
                byte[] fileBytes = File.ReadAllBytes(cachedDatabasePath);
                context.Response.ContentType = "application/octet-stream";
                context.Response.Headers.Add(
                    "Content-Disposition",
                    "attachment; filename=\"config.db\""
                );
                context.Response.ContentLength64 = fileBytes.Length;
                await context.Response.OutputStream.WriteAsync(fileBytes, 0, fileBytes.Length);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = ex.Message }
                );
            }
        }

        private async Task HandleUploadDatabase(IHttpContext context)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    await context.Request.InputStream.CopyToAsync(memoryStream);
                    byte[] fileBytes = memoryStream.ToArray();

                    if (fileBytes.Length == 0)
                    {
                        context.Response.StatusCode = 400;
                        await SendJsonResponse(
                            context,
                            new SimpleResponse
                            {
                                success = false,
                                message = "No file data received",
                            }
                        );
                        return;
                    }

                    // Write the uploaded database
                    File.WriteAllBytes(cachedDatabasePath, fileBytes);

                    await SendJsonResponse(
                        context,
                        new SimpleResponse
                        {
                            success = true,
                            message = "Database uploaded. Restart app to apply changes.",
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = ex.Message }
                );
            }
        }

        private async Task HandleGetInfo(IHttpContext context)
        {
            var info = new SystemInfoResponse
            {
                appName = cachedServerInfo.AppName,
                version = cachedServerInfo.Version,
                unityVersion = cachedServerInfo.UnityVersion,
                buildDate = cachedServerInfo.BuildDate,
                platform = cachedServerInfo.Platform,
                deviceModel = cachedServerInfo.DeviceModel,
                operatingSystem = cachedServerInfo.OperatingSystem,
                connectedClients = GetActiveClientCount(),
                serverPort = port,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            await SendJsonResponse(context, info);
        }

        private async Task HandleGetStatus(IHttpContext context)
        {
            statusProvider.UpdateConnectedClients(GetActiveClientCount());
            var status = statusProvider.GetStatus();
            await SendJsonResponse(context, status);
        }

        private async Task HandleGetLogs(IHttpContext context)
        {
            int count = 100;
            if (context.Request.QueryString["count"] != null)
            {
                int.TryParse(context.Request.QueryString["count"], out count);
            }

            var logs = logCollector.GetRecentLogs(count);
            await SendJsonResponse(context, new LogsResponse { success = true, logs = logs });
        }

        private async Task HandleClearLogs(IHttpContext context)
        {
            logCollector.ClearLogs();
            await SendJsonResponse(
                context,
                new SimpleResponse { success = true, message = "Logs cleared" }
            );
        }

        private async Task HandleRestart(IHttpContext context)
        {
            await SendJsonResponse(
                context,
                new SimpleResponse { success = true, message = "Restart initiated" }
            );
            webServerManager.RequestRestart();
        }

        private async Task HandleResetPose(IHttpContext context)
        {
            webServerManager.RequestPoseReset();
            await SendJsonResponse(
                context,
                new SimpleResponse { success = true, message = "Pose reset initiated" }
            );
        }

        private async Task HandleGetVideoModes(IHttpContext context)
        {
            var passthroughSource = streamProvider?.FrameSource;
            if (passthroughSource == null)
            {
                context.Response.StatusCode = 503;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = "Passthrough stream not available",
                    }
                );
                return;
            }

            var availableModes = passthroughSource.GetAvailableModes();

            if (availableModes == null || availableModes.Length == 0)
            {
                context.Response.StatusCode = 503;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = "Stream not initialized. Enable passthrough stream first.",
                    }
                );
                return;
            }

            // Convert VideoMode[] to VideoModeModel[]
            var modeModels = new VideoModeModel[availableModes.Length];
            for (int i = 0; i < availableModes.Length; i++)
            {
                modeModels[i] = new VideoModeModel
                {
                    width = availableModes[i].Width,
                    height = availableModes[i].Height,
                    framerate = availableModes[i].Fps,
                };
            }

            // Return just the array with 200 OK
            await SendJsonResponse(context, modeModels);
        }

        /// <summary>
        /// Returns true if the file name matches a bundled layout or an existing custom
        /// layout file in <see cref="FileManager.GetCustomFieldLayoutDir"/>. Used to
        /// validate POST /api/config writes to <c>fieldLayoutFile</c>.
        /// </summary>
        private bool IsValidFieldLayoutChoice(string fileName)
        {
            foreach (var bundled in QuestNavConstants.AprilTag.BUNDLED_FIELD_LAYOUTS)
            {
                if (string.Equals(bundled, fileName, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            string customPath = Path.Combine(FileManager.GetCustomFieldLayoutDir(), fileName);
            return File.Exists(customPath);
        }

        /// <summary>
        /// Handles GET /api/apriltag-field-layouts. Enumerates the bundled list (in commit
        /// 6 also the custom-uploaded list) and returns metadata for each so the web UI
        /// can populate the field-layout dropdown without having to enumerate the APK
        /// itself (Android forbids listing StreamingAssets at runtime).
        /// </summary>
        private async Task HandleGetAprilTagFieldLayouts(IHttpContext context)
        {
            var entries = new List<AprilTagFieldLayoutEntry>();
            string bundledDir = FileManager.GetStaticFilesPath("apriltag/fieldlayouts");
            foreach (var fileName in QuestNavConstants.AprilTag.BUNDLED_FIELD_LAYOUTS)
            {
                int tagCount = 0;
                try
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    // Bundled JSONs live inside the APK; extract them on demand so we can
                    // count tags. Subsequent loads are no-ops because the file already
                    // exists in persistent storage.
                    await FileManager.ExtractAndroidFileAsync(
                        fileName,
                        "apriltag/fieldlayouts",
                        bundledDir
                    );
#else
                    await Task.CompletedTask;
#endif
                    string filePath = Path.Combine(bundledDir, fileName);
                    if (File.Exists(filePath))
                    {
                        tagCount = CountTagsInLayoutFile(filePath);
                    }
                }
                catch (Exception ex)
                {
                    QueuedLogger.LogWarning(
                        $"Could not introspect bundled field layout '{fileName}': {ex.Message}"
                    );
                }

                entries.Add(
                    new AprilTagFieldLayoutEntry
                    {
                        fileName = fileName,
                        displayName = MakeFieldLayoutDisplayName(fileName),
                        source = "bundled",
                        tagCount = tagCount,
                    }
                );
            }

            // Custom (user-uploaded) layouts.
            string customDir = FileManager.GetCustomFieldLayoutDir();
            try
            {
                if (Directory.Exists(customDir))
                {
                    foreach (var path in Directory.GetFiles(customDir, "*.json"))
                    {
                        string fileName = Path.GetFileName(path);
                        // Defensive: never advertise a custom file that shadows a bundled
                        // name (POST rejects this so it shouldn't exist, but if it does
                        // we don't want two entries with identical fileName).
                        if (IsBundledLayoutName(fileName))
                        {
                            continue;
                        }
                        int tagCount = 0;
                        try
                        {
                            tagCount = CountTagsInLayoutFile(path);
                        }
                        catch (Exception ex)
                        {
                            QueuedLogger.LogWarning(
                                $"Could not introspect custom field layout '{fileName}': {ex.Message}"
                            );
                        }
                        entries.Add(
                            new AprilTagFieldLayoutEntry
                            {
                                fileName = fileName,
                                displayName = MakeFieldLayoutDisplayName(fileName),
                                source = "custom",
                                tagCount = tagCount,
                            }
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError(
                    $"Failed to enumerate custom field layouts in '{customDir}': {ex.Message}"
                );
            }

            await SendJsonResponse(context, entries);
        }

        /// <summary>
        /// Helper used by both the listing endpoint and the POST handler to check whether
        /// a name collides with a bundled layout (which is read-only and cannot be
        /// shadowed by a custom upload).
        /// </summary>
        private static bool IsBundledLayoutName(string fileName)
        {
            foreach (var bundled in QuestNavConstants.AprilTag.BUNDLED_FIELD_LAYOUTS)
            {
                if (string.Equals(bundled, fileName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sanitize and validate a user-supplied layout name. Returns the canonical
        /// "&lt;name&gt;.json" form on success or null on failure (caller returns 400 with
        /// the message). Forbids path traversal and collisions with bundled names.
        /// </summary>
        private static bool TrySanitizeFieldLayoutName(
            string name,
            out string canonical,
            out string error
        )
        {
            canonical = null;
            error = null;
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Layout name is required.";
                return false;
            }

            // Strip any caller-supplied .json suffix so the regex below sees only the stem.
            string stem = name;
            if (stem.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                stem = stem.Substring(0, stem.Length - 5);
            }

            // Reject anything that isn't [A-Za-z0-9._-]{1,64}. This covers path-traversal
            // attempts (../, /, \\), Windows reserved characters, leading dots, etc.
            if (!System.Text.RegularExpressions.Regex.IsMatch(stem, @"^[A-Za-z0-9._-]{1,64}$"))
            {
                error =
                    "Invalid layout name. Use only letters, numbers, '.', '_', and '-' (max 64 chars).";
                return false;
            }

            canonical = stem + ".json";
            if (IsBundledLayoutName(canonical))
            {
                error =
                    $"'{canonical}' is a bundled layout name. Custom layouts cannot shadow "
                    + "bundled layouts; choose a different name.";
                canonical = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a JSON string is a parseable AprilTagFieldLayout with at least
        /// one tag, finite translations, and no duplicate IDs. Returns the deserialized
        /// layout (and tagCount) on success.
        /// </summary>
        private static bool TryValidateFieldLayoutJson(
            string json,
            out int tagCount,
            out string error
        )
        {
            tagCount = 0;
            error = null;
            AprilTagFieldLayoutDomain parsed = null;
            try
            {
                parsed = JsonConvert.DeserializeObject<AprilTagFieldLayoutDomain>(json);
            }
            catch (Exception ex)
            {
                error = $"Failed to parse JSON: {ex.Message}";
                return false;
            }
            if (parsed == null || parsed.Tags == null || parsed.Tags.Count == 0)
            {
                error = "Layout must contain at least one tag in the 'tags' array.";
                return false;
            }
            var seenIds = new HashSet<int>();
            foreach (var tag in parsed.Tags)
            {
                if (tag == null || tag.Pose == null)
                {
                    error = "Every tag entry must have a 'pose' object.";
                    return false;
                }
                var t = tag.Pose.Translation;
                if (
                    t == null
                    || double.IsNaN(t.X)
                    || double.IsNaN(t.Y)
                    || double.IsNaN(t.Z)
                    || double.IsInfinity(t.X)
                    || double.IsInfinity(t.Y)
                    || double.IsInfinity(t.Z)
                )
                {
                    error = $"Tag {tag.ID}: translation has non-finite values.";
                    return false;
                }
                if (!seenIds.Add(tag.ID))
                {
                    error = $"Duplicate tag ID {tag.ID} in layout.";
                    return false;
                }
            }
            tagCount = parsed.Tags.Count;
            return true;
        }

        /// <summary>
        /// GET /api/apriltag-field-layouts/{name} - returns the raw JSON content of a
        /// custom layout so the web UI can populate the edit textarea. Refuses bundled
        /// layouts (they are read-only - if the user wants to modify a bundled layout
        /// they must rebuild the app).
        /// </summary>
        private async Task HandleGetAprilTagFieldLayoutContent(
            IHttpContext context,
            string fileName
        )
        {
            if (string.IsNullOrEmpty(fileName))
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = "Layout name is required." }
                );
                return;
            }
            if (IsBundledLayoutName(fileName))
            {
                context.Response.StatusCode = 403;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message =
                            "Bundled layouts are read-only. Edit the source JSON in StreamingAssets and rebuild the app.",
                    }
                );
                return;
            }
            string path = Path.Combine(FileManager.GetCustomFieldLayoutDir(), fileName);
            if (!File.Exists(path))
            {
                context.Response.StatusCode = 404;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"Custom layout '{fileName}' does not exist.",
                    }
                );
                return;
            }
            try
            {
                string content = await File.ReadAllTextAsync(path);
                context.Response.ContentType = "application/json";
                await context.SendStringAsync(content, "application/json", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = ex.Message }
                );
            }
        }

        /// <summary>
        /// POST /api/apriltag-field-layouts. Body shape:
        /// <code>{ "name": "team-1234-practice", "content": "..." }</code>
        /// where <c>content</c> is the raw layout JSON string.
        ///
        /// Creates the layout if it doesn't exist; replaces it if it does (the UI's
        /// "Edit" flow re-POSTs the same name with new content). Writes atomically via
        /// a *.tmp file + rename so a failed write cannot corrupt an existing layout.
        /// </summary>
        private async Task HandlePostAprilTagFieldLayout(IHttpContext context)
        {
            // 10 MB body cap. Field layouts are ~30 KB; this is generous abuse-protection.
            const long MAX_BODY_BYTES = 10L * 1024L * 1024L;
            if (
                context.Request.ContentLength64 > 0
                && context.Request.ContentLength64 > MAX_BODY_BYTES
            )
            {
                context.Response.StatusCode = 413;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = "Layout body too large (max 10 MB).",
                    }
                );
                return;
            }

            string body;
            try
            {
                using var reader = new StreamReader(
                    context.Request.InputStream,
                    Encoding.UTF8,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: 1024,
                    leaveOpen: false
                );
                var sb = new StringBuilder();
                char[] buf = new char[8192];
                long total = 0;
                int read;
                while ((read = await reader.ReadAsync(buf, 0, buf.Length)) > 0)
                {
                    total += read;
                    if (total > MAX_BODY_BYTES)
                    {
                        context.Response.StatusCode = 413;
                        await SendJsonResponse(
                            context,
                            new SimpleResponse
                            {
                                success = false,
                                message = "Layout body too large (max 10 MB).",
                            }
                        );
                        return;
                    }
                    sb.Append(buf, 0, read);
                }
                body = sb.ToString();
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"Could not read request body: {ex.Message}",
                    }
                );
                return;
            }

            FieldLayoutUploadRequest req;
            try
            {
                req = JsonConvert.DeserializeObject<FieldLayoutUploadRequest>(body);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"Invalid request envelope: {ex.Message}",
                    }
                );
                return;
            }
            if (req == null || string.IsNullOrEmpty(req.content))
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = "Request must contain a 'name' and a non-empty 'content' string.",
                    }
                );
                return;
            }

            if (!TrySanitizeFieldLayoutName(req.name, out string canonical, out string sanError))
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = sanError }
                );
                return;
            }

            if (!TryValidateFieldLayoutJson(req.content, out int tagCount, out string valError))
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = valError }
                );
                return;
            }

            string customDir = FileManager.GetCustomFieldLayoutDir();
            string finalPath = Path.Combine(customDir, canonical);
            string tmpPath = finalPath + ".tmp";

            try
            {
                await File.WriteAllTextAsync(tmpPath, req.content);
                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }
                File.Move(tmpPath, finalPath);
            }
            catch (Exception ex)
            {
                if (File.Exists(tmpPath))
                {
                    try
                    {
                        File.Delete(tmpPath);
                    }
                    catch
                    {
                        /* swallow */
                    }
                }
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"Failed to write file: {ex.Message}",
                    }
                );
                return;
            }

            await SendJsonResponse(
                context,
                new
                {
                    success = true,
                    fileName = canonical,
                    tagCount = tagCount,
                }
            );
        }

        /// <summary>
        /// PATCH /api/apriltag-field-layouts/{name}. Body: <c>{ "newName": "..." }</c>.
        /// Renames the file (sanitizing newName the same way as POST). If the renamed
        /// layout was the currently-selected fieldLayoutFile in config, updates the
        /// config to point at the new name atomically.
        /// </summary>
        private async Task HandlePatchAprilTagFieldLayout(IHttpContext context, string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || IsBundledLayoutName(fileName))
            {
                context.Response.StatusCode = 403;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = "Only custom layouts can be renamed.",
                    }
                );
                return;
            }

            string body = await context.GetRequestBodyAsStringAsync();
            FieldLayoutRenameRequest req = null;
            try
            {
                req = JsonConvert.DeserializeObject<FieldLayoutRenameRequest>(body);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"Invalid request: {ex.Message}",
                    }
                );
                return;
            }
            if (req == null || string.IsNullOrWhiteSpace(req.newName))
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = "Request must contain 'newName'.",
                    }
                );
                return;
            }

            if (
                !TrySanitizeFieldLayoutName(
                    req.newName,
                    out string newCanonical,
                    out string sanError
                )
            )
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = sanError }
                );
                return;
            }

            string customDir = FileManager.GetCustomFieldLayoutDir();
            string oldPath = Path.Combine(customDir, fileName);
            string newPath = Path.Combine(customDir, newCanonical);

            if (!File.Exists(oldPath))
            {
                context.Response.StatusCode = 404;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"Custom layout '{fileName}' does not exist.",
                    }
                );
                return;
            }
            if (File.Exists(newPath))
            {
                context.Response.StatusCode = 409;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"A layout named '{newCanonical}' already exists.",
                    }
                );
                return;
            }

            try
            {
                File.Move(oldPath, newPath);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"Failed to rename file: {ex.Message}",
                    }
                );
                return;
            }

            // If the currently-selected layout was just renamed, update the config so the
            // next restart loads the right one (and the dropdown shows the new name without
            // a stale "missing" reference).
            string currentSelection = await configManager.GetAprilTagFieldLayoutFileAsync();
            bool reselected = false;
            if (string.Equals(currentSelection, fileName, StringComparison.Ordinal))
            {
                await configManager.SetAprilTagFieldLayoutFileAsync(newCanonical);
                reselected = true;
            }

            await SendJsonResponse(
                context,
                new
                {
                    success = true,
                    oldFileName = fileName,
                    newFileName = newCanonical,
                    reselectedConfigPointer = reselected,
                }
            );
        }

        /// <summary>
        /// DELETE /api/apriltag-field-layouts/{name}. Refuses to delete bundled layouts.
        /// If the deleted layout was the currently-selected fieldLayoutFile, falls back
        /// to QuestNavConstants.AprilTag.DEFAULT_FIELD_LAYOUT_FILE.
        /// </summary>
        private async Task HandleDeleteAprilTagFieldLayout(IHttpContext context, string fileName)
        {
            if (string.IsNullOrEmpty(fileName) || IsBundledLayoutName(fileName))
            {
                context.Response.StatusCode = 403;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = "Only custom layouts can be deleted.",
                    }
                );
                return;
            }
            string customDir = FileManager.GetCustomFieldLayoutDir();
            string path = Path.Combine(customDir, fileName);
            if (!File.Exists(path))
            {
                context.Response.StatusCode = 404;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"Custom layout '{fileName}' does not exist.",
                    }
                );
                return;
            }
            try
            {
                File.Delete(path);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse
                    {
                        success = false,
                        message = $"Failed to delete file: {ex.Message}",
                    }
                );
                return;
            }

            string currentSelection = await configManager.GetAprilTagFieldLayoutFileAsync();
            string fellBackTo = null;
            if (string.Equals(currentSelection, fileName, StringComparison.Ordinal))
            {
                fellBackTo = QuestNavConstants.AprilTag.DEFAULT_FIELD_LAYOUT_FILE;
                await configManager.SetAprilTagFieldLayoutFileAsync(fellBackTo);
            }

            await SendJsonResponse(
                context,
                new
                {
                    success = true,
                    deletedFileName = fileName,
                    fellBackTo = fellBackTo,
                }
            );
        }

        /// <summary>
        /// Cheap tag-count introspection: just looks for the number of <c>"ID"</c> keys
        /// in the JSON. Avoids deserializing the entire layout (which costs ~50 ms per
        /// file with Newtonsoft.Json on the Quest) just to render a dropdown.
        /// </summary>
        private static int CountTagsInLayoutFile(string filePath)
        {
            try
            {
                string contents = File.ReadAllText(filePath);
                int count = 0;
                int idx = 0;
                while ((idx = contents.IndexOf("\"ID\"", idx, StringComparison.Ordinal)) >= 0)
                {
                    count++;
                    idx += 4;
                }
                return count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Strips the <c>.json</c> extension and replaces hyphens with spaces; the result
        /// is a friendlier dropdown label without forcing every layout file to be renamed
        /// (e.g. "2026-rebuilt-welded.json" -> "2026 rebuilt welded").
        /// </summary>
        private static string MakeFieldLayoutDisplayName(string fileName)
        {
            string stem = fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                ? fileName.Substring(0, fileName.Length - 5)
                : fileName;
            return stem.Replace('-', ' ').Replace('_', ' ');
        }

        /// <summary>
        /// Editor-mode fallback resolution list. The Meta SDK
        /// <c>GetSupportedResolutions</c> call returns nothing meaningful in playmode
        /// (no real Quest camera); without this fallback the AprilTag tab would show
        /// an empty resolution dropdown in dev.
        ///
        /// Per Meta's PCA documentation, Quest 3 / Quest 3S only support these two
        /// resolutions: 1280x960 (legacy 4:3, the original passthrough mode) and
        /// 1280x1280 (added in Horizon OS v83, expanded vertical FOV). Aspirational
        /// resolutions (320x240, 640x480, etc.) are NOT what the SDK returns at
        /// runtime; including them in the editor fallback would let dev users select
        /// modes that fail on device.
        /// </summary>
        private static readonly Vector2Int[] EditorFallbackResolutions =
        {
            new Vector2Int(1280, 1280),
            new Vector2Int(1280, 960),
        };

        /// <summary>
        /// Returns the cross-product of supported resolutions and supported framerates.
        /// Source order: Meta SDK first (real device), then editor fallback if the SDK
        /// returns an empty list. Wrapped in try/catch (issue 20: Meta SDK can throw
        /// during state changes).
        /// </summary>
        private VideoModeModel[] BuildAprilTagSupportedModes()
        {
            Vector2Int[] resolutions;
            try
            {
                resolutions =
                    cameraAccess != null
                        ? PassthroughCameraAccess.GetSupportedResolutions(
                            cameraAccess.CameraPosition
                        )
                        : Array.Empty<Vector2Int>();
            }
            catch (Exception ex)
            {
                QueuedLogger.LogWarning(
                    $"PassthroughCameraAccess.GetSupportedResolutions threw: {ex.Message}. "
                        + "Falling back to editor stub list."
                );
                resolutions = Array.Empty<Vector2Int>();
            }

            if (resolutions == null || resolutions.Length == 0)
            {
                resolutions = EditorFallbackResolutions;
            }

            var fpsOptions = QuestNavConstants.VideoStream.SUPPORTED_FPS;
            var modes = new VideoModeModel[resolutions.Length * fpsOptions.Length];
            int n = 0;
            foreach (var res in resolutions)
            {
                foreach (var fps in fpsOptions)
                {
                    modes[n++] = new VideoModeModel
                    {
                        width = res.x,
                        height = res.y,
                        framerate = fps,
                    };
                }
            }
            return modes;
        }

        /// <summary>
        /// GET /api/apriltag-video-modes - resolution x framerate cross-product the
        /// AprilTag detector can use. Note: AprilTag detection does NOT gate on
        /// EnableHighQualityStreams; the detector benefits from higher resolution and
        /// the user is expected to balance accuracy against thermal load themselves.
        /// </summary>
        private async Task HandleGetAprilTagVideoModes(IHttpContext context)
        {
            await SendJsonResponse(context, BuildAprilTagSupportedModes());
        }

        /// <summary>
        /// Returns true if (width, height, fps) is one of the modes returned by
        /// <see cref="BuildAprilTagSupportedModes"/>. Used by HandlePostConfig to reject
        /// resolution / framerate combinations that the camera cannot actually deliver.
        /// </summary>
        private bool IsValidAprilTagMode(int width, int height, int fps)
        {
            foreach (var mode in BuildAprilTagSupportedModes())
            {
                if (mode.width == width && mode.height == height && mode.framerate == fps)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
