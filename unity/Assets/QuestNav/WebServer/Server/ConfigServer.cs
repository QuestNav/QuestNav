using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using Newtonsoft.Json;

namespace QuestNav.WebServer
{
    #region Cached Server Information
    /// <summary>
    /// Cached server information captured on main thread.
    /// ConfigServer runs on background thread and cannot access Unity APIs directly,
    /// so we cache Unity-specific information during construction on the main thread.
    /// </summary>
    public class CachedServerInfo
    {
        /// <summary>
        /// Application product name
        /// </summary>
        public string appName;

        /// <summary>
        /// Application version string
        /// </summary>
        public string version;

        /// <summary>
        /// Unity engine version
        /// </summary>
        public string unityVersion;

        /// <summary>
        /// Build date timestamp
        /// </summary>
        public string buildDate;

        /// <summary>
        /// Current platform (Android, Windows, etc.)
        /// </summary>
        public string platform;

        /// <summary>
        /// Device model name
        /// </summary>
        public string deviceModel;

        /// <summary>
        /// Operating system version
        /// </summary>
        public string operatingSystem;
    }
    #endregion

    #region Callback Delegates
    /// <summary>
    /// Callback delegate for main thread actions from background thread.
    /// Used to execute Unity API calls safely on the main thread.
    /// </summary>
    public delegate void MainThreadAction();
    #endregion

    /// <summary>
    /// Pure C# class (NOT MonoBehaviour) that runs EmbedIO server on background thread.
    /// Provides HTTP REST API for runtime configuration management.
    /// Uses async/await which is acceptable per Unity rules for non-MonoBehaviour classes.
    /// Does not call Unity APIs directly - uses ILogger interface and cached data instead.
    /// Serves both REST API endpoints and static web UI files.
    /// </summary>
    public class ConfigServer
    {
        #region Fields
        /// <summary>
        /// EmbedIO web server instance
        /// </summary>
        private EmbedIO.WebServer server;

        /// <summary>
        /// Cancellation token source for server shutdown
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Reflection binding for configuration fields
        /// </summary>
        private readonly ReflectionBinding binding;

        /// <summary>
        /// Configuration persistence store
        /// </summary>
        private readonly ConfigStore store;

        /// <summary>
        /// HTTP server port
        /// </summary>
        private readonly int port;

        /// <summary>
        /// Enable CORS for development
        /// </summary>
        private readonly bool enableCORSDevMode;

        /// <summary>
        /// Path to static web UI files
        /// </summary>
        private readonly string staticPath;

        /// <summary>
        /// Logger implementation for background thread
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// Cached server info (captured on main thread)
        /// </summary>
        private CachedServerInfo cachedServerInfo;

        /// <summary>
        /// Callback for main thread restart action
        /// </summary>
        private MainThreadAction restartCallback;

        /// <summary>
        /// Callback for main thread pose reset action
        /// </summary>
        private MainThreadAction poseResetCallback;

        /// <summary>
        /// Status provider instance (injected)
        /// </summary>
        private readonly StatusProvider statusProvider;

        /// <summary>
        /// Log collector instance (injected)
        /// </summary>
        private readonly LogCollector logCollector;

        /// <summary>
        /// Stream provider instance (injected)
        /// </summary>
        private readonly VideoStreamProvider streamProvider;

        /// <summary>
        /// Dictionary tracking last activity time for each client IP
        /// </summary>
        private readonly System.Collections.Generic.Dictionary<string, DateTime> activeClients =
            new System.Collections.Generic.Dictionary<string, DateTime>();

        /// <summary>
        /// Lock object for thread-safe access to activeClients dictionary
        /// </summary>
        private readonly object clientsLock = new object();

        /// <summary>
        /// Time window for considering a client as "active" (30 seconds)
        /// </summary>
        private readonly TimeSpan activeClientWindow = TimeSpan.FromSeconds(30);
        #endregion

        #region Properties
        /// <summary>
        /// Gets whether the server is currently running
        /// </summary>
        public bool IsRunning => server != null && server.State == EmbedIO.WebServerState.Listening;

        /// <summary>
        /// Gets the base URL of the server
        /// </summary>
        public string BaseUrl => $"http://localhost:{port}/";
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new ConfigServer instance.
        /// Must be called from Unity main thread to cache Unity-specific information.
        /// </summary>
        /// <param name="binding">Reflection binding for configuration fields</param>
        /// <param name="store">Configuration persistence store</param>
        /// <param name="port">HTTP server port</param>
        /// <param name="enableCORSDevMode">Enable CORS for development</param>
        /// <param name="staticPath">Path to static web UI files</param>
        /// <param name="logger">Logger implementation for background thread</param>
        /// <param name="restartCallback">Callback to restart application</param>
        /// <param name="poseResetCallback">Callback to reset VR pose</param>
        /// <param name="statusProvider">Status provider instance for runtime data</param>
        /// <param name="logCollector">Log collector instance for log messages</param>
        /// <param name="streamProvider">Stream provider instance for video streaming</param>
        public ConfigServer(
            ReflectionBinding binding,
            ConfigStore store,
            int port,
            bool enableCORSDevMode,
            string staticPath,
            ILogger logger,
            MainThreadAction restartCallback,
            MainThreadAction poseResetCallback,
            StatusProvider statusProvider,
            LogCollector logCollector,
            VideoStreamProvider streamProvider
        )
        {
            this.binding = binding;
            this.store = store;
            this.port = port;
            this.enableCORSDevMode = enableCORSDevMode;
            this.staticPath = staticPath;
            this.logger = logger;
            this.restartCallback = restartCallback;
            this.poseResetCallback = poseResetCallback;
            this.statusProvider = statusProvider;
            this.logCollector = logCollector;
            this.streamProvider = streamProvider;

            // Cache server info on main thread (before server starts on background thread)
            CacheServerInfo();
        }
        #endregion

        #region Server Lifecycle
        /// <summary>
        /// Caches Unity-specific server information on main thread.
        /// This must be called during construction before server starts on background thread.
        /// </summary>
        private void CacheServerInfo()
        {
            cachedServerInfo = new CachedServerInfo
            {
                // App Information
                appName = UnityEngine.Application.productName,
                version = UnityEngine.Application.version,
                unityVersion = UnityEngine.Application.unityVersion,
                buildDate = System
                    .IO.File.GetLastWriteTime(UnityEngine.Application.dataPath)
                    .ToString("yyyy-MM-dd HH:mm:ss"),

                // Platform Information
                platform = UnityEngine.Application.platform.ToString(),
                deviceModel = UnityEngine.SystemInfo.deviceModel,
                operatingSystem = UnityEngine.SystemInfo.operatingSystem,
            };
        }

        /// <summary>
        /// Starts the HTTP server on a background thread.
        /// Configures EmbedIO with API routes and static file serving.
        /// </summary>
        public void Start()
        {
            if (IsRunning)
            {
                logger?.LogWarning("[ConfigServer] Server already running");
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            logger?.Log($"[ConfigServer] Starting server on port {port}");
            logger?.Log($"[ConfigServer] Static files path: {staticPath}");

            server = new EmbedIO.WebServer(o =>
                o.WithUrlPrefix($"http://*:{port}/").WithMode(HttpListenerMode.EmbedIO)
            )
                .WithModule(new ActionModule("/api", HttpVerbs.Any, HandleApiRequest))
                .WithModule(new ActionModule("/video", HttpVerbs.Get, HandleVideoStream))
                .WithStaticFolder("/", staticPath, true);

            // Disable silent handling of write exceptions to allow handling disconnects
            server.Listener.IgnoreWriteExceptions = false;

            // Disable verbose EmbedIO logging (only if not already unregistered)
            server.StateChanged += (s, e) => { }; // Suppress state change logs
            try
            {
                Swan.Logging.Logger.UnregisterLogger<Swan.Logging.ConsoleLogger>();
            }
            catch
            {
                // Logger already unregistered on previous start, ignore
            }

            // Start server on background thread
            Task.Run(async () =>
            {
                try
                {
                    await server.RunAsync(cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    logger?.LogError($"[ConfigServer] Server error: {ex.Message}");
                }
            });

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
        #endregion

        #region Client Tracking
        /// <summary>
        /// Records activity from a client IP address.
        /// Updates the last activity timestamp for the client.
        /// </summary>
        /// <param name="clientIp">Client IP address</param>
        private void RecordClientActivity(string clientIp)
        {
            if (string.IsNullOrEmpty(clientIp))
                return;

            lock (clientsLock)
            {
                activeClients[clientIp] = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Gets the count of active clients (clients that made a request within the active window).
        /// Cleans up stale client entries during counting.
        /// </summary>
        /// <returns>Number of active clients</returns>
        private int GetActiveClientCount()
        {
            lock (clientsLock)
            {
                var now = DateTime.UtcNow;
                var staleClients = new System.Collections.Generic.List<string>();

                // Find stale clients
                foreach (var kvp in activeClients)
                {
                    if (now - kvp.Value > activeClientWindow)
                    {
                        staleClients.Add(kvp.Key);
                    }
                }

                // Remove stale clients
                foreach (var client in staleClients)
                {
                    activeClients.Remove(client);
                }

                return activeClients.Count;
            }
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Sends a JSON response using Newtonsoft.Json serializer (Swan.Lite has IL2CPP issues).
        /// Sets proper content type and serializes with Newtonsoft.Json instead of Swan's serializer.
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <param name="data">Data object to serialize and send</param>
        private async Task SendJsonResponse(IHttpContext context, object data)
        {
            context.Response.ContentType = "application/json";
            string json = JsonConvert.SerializeObject(data, Formatting.None);
            await context.SendStringAsync(json, "application/json", System.Text.Encoding.UTF8);
        }
        #endregion

        #region API Request Handling
        /// <summary>
        /// Handles all incoming API requests.
        /// Routes requests to appropriate handler methods.
        /// Implements CORS support for development.
        /// </summary>
        /// <param name="context">HTTP request context</param>
        private async Task HandleApiRequest(IHttpContext context)
        {
            try
            {
                // Track client activity
                string clientIp = context.Request.RemoteEndPoint?.Address?.ToString();
                RecordClientActivity(clientIp);

                // CORS headers
                if (enableCORSDevMode)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                // OPTIONS preflight
                if (context.Request.HttpVerb == HttpVerbs.Options)
                {
                    context.Response.StatusCode = 200;
                    return;
                }

                // Route requests to handlers
                string path = context.Request.Url.AbsolutePath;

                if (path == "/api/schema" && context.Request.HttpVerb == HttpVerbs.Get)
                {
                    await HandleGetSchema(context);
                }
                else if (path == "/api/config" && context.Request.HttpVerb == HttpVerbs.Get)
                {
                    await HandleGetConfig(context);
                }
                else if (path == "/api/config" && context.Request.HttpVerb == HttpVerbs.Post)
                {
                    await HandlePostConfig(context);
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
                logger?.LogError(
                    $"[ConfigServer] Request error: {ex.GetType().Name}: {ex.Message}"
                );
                logger?.LogError($"[ConfigServer] Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    logger?.LogError(
                        $"[ConfigServer] Inner exception: {ex.InnerException.Message}"
                    );
                }
                context.Response.StatusCode = 500;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = ex.Message }
                );
            }
        }
        #endregion

        #region API Endpoint Handlers
        /// <summary>
        /// Handles GET /api/schema - Returns configuration schema
        /// </summary>
        private async Task HandleGetSchema(IHttpContext context)
        {
            var schema = binding.GenerateSchema();
            await SendJsonResponse(context, schema);
        }

        /// <summary>
        /// Handles GET /api/config - Returns current configuration values
        /// </summary>
        private async Task HandleGetConfig(IHttpContext context)
        {
            var values = binding.GetAllValues();
            var result = new ConfigValuesResponse
            {
                success = true,
                values = values,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            await SendJsonResponse(context, result);
        }

        /// <summary>
        /// Handles POST /api/config - Updates a configuration value
        /// </summary>
        private async Task HandlePostConfig(IHttpContext context)
        {
            string body = await context.GetRequestBodyAsStringAsync();
            var updateRequest = JsonConvert.DeserializeObject<ConfigUpdateRequest>(body);

            if (updateRequest == null || string.IsNullOrEmpty(updateRequest.path))
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new SimpleResponse { success = false, message = "Invalid request" }
                );
                return;
            }

            var oldValue = binding.GetValue(updateRequest.path);
            bool success = binding.SetValue(updateRequest.path, updateRequest.value);
            var newValue = binding.GetValue(updateRequest.path);

            if (success)
            {
                // Save configuration to disk
                var configData = new ConfigData { values = binding.GetAllValues() };
                store.SaveConfig(configData);

                var response = new ConfigUpdateResponse
                {
                    success = true,
                    message = "Configuration updated",
                    oldValue = oldValue,
                    newValue = newValue,
                };

                await SendJsonResponse(context, response);
            }
            else
            {
                context.Response.StatusCode = 400;
                await SendJsonResponse(
                    context,
                    new ConfigUpdateResponse
                    {
                        success = false,
                        message = "Failed to update configuration",
                    }
                );
            }
        }

        /// <summary>
        /// Handles GET /api/info - Returns system information
        /// </summary>
        private async Task HandleGetInfo(IHttpContext context)
        {
            // Use cached server info (captured on main thread during construction)
            var info = new SystemInfoResponse
            {
                // Cached from main thread
                appName = cachedServerInfo.appName,
                version = cachedServerInfo.version,
                unityVersion = cachedServerInfo.unityVersion,
                buildDate = cachedServerInfo.buildDate,
                platform = cachedServerInfo.platform,
                deviceModel = cachedServerInfo.deviceModel,
                operatingSystem = cachedServerInfo.operatingSystem,

                // Runtime information (safe on background thread)
                connectedClients = GetActiveClientCount(),
                configPath = store.GetConfigPath(),
                serverPort = port,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            await SendJsonResponse(context, info);
        }

        /// <summary>
        /// Handles GET /api/status - Returns runtime status from StatusProvider
        /// </summary>
        private async Task HandleGetStatus(IHttpContext context)
        {
            // Update connected clients count in StatusProvider
            statusProvider.UpdateConnectedClients(GetActiveClientCount());

            var status = statusProvider.GetStatus();
            await SendJsonResponse(context, status);
        }

        /// <summary>
        /// Handles GET /api/logs - Returns recent log entries
        /// </summary>
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

        /// <summary>
        /// Handles DELETE /api/logs - Clears all collected logs
        /// </summary>
        private async Task HandleClearLogs(IHttpContext context)
        {
            logCollector.ClearLogs();
            await SendJsonResponse(
                context,
                new SimpleResponse { success = true, message = "Logs cleared" }
            );
        }

        /// <summary>
        /// Handles POST /api/restart - Triggers application restart
        /// </summary>
        private async Task HandleRestart(IHttpContext context)
        {
            await SendJsonResponse(
                context,
                new SimpleResponse { success = true, message = "Restart initiated" }
            );

            // Trigger restart on main thread via callback
            restartCallback?.Invoke();
        }

        /// <summary>
        /// Handles POST /api/reset-pose - Triggers VR pose reset
        /// </summary>
        private async Task HandleResetPose(IHttpContext context)
        {
            poseResetCallback?.Invoke();
            await SendJsonResponse(
                context,
                new SimpleResponse { success = true, message = "Pose reset initiated" }
            );
        }
        #endregion
    }
}
