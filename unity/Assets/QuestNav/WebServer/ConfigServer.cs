using System;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using Newtonsoft.Json;

namespace QuestNav.Config
{
    /// <summary>
    /// Cached server information captured on main thread
    /// </summary>
    public class CachedServerInfo
    {
        public string appName;
        public string version;
        public string unityVersion;
        public string buildDate;
        public string platform;
        public string deviceModel;
        public string deviceName;
        public string operatingSystem;
        public string processorType;
        public int processorCount;
        public int systemMemorySize;
        public string graphicsDeviceName;
    }

    /// <summary>
    /// Callback delegate for main thread actions from background thread
    /// </summary>
    public delegate void MainThreadAction();

    /// <summary>
    /// Pure C# class (NOT MonoBehaviour) that runs EmbedIO server on background thread.
    /// Uses async/await which is acceptable per Unity rules for non-MonoBehaviour classes.
    /// Does not call Unity APIs directly - uses ILogger interface instead.
    /// </summary>
    public class ConfigServer
    {
        private WebServer m_server;
        private CancellationTokenSource m_cancellationTokenSource;
        private readonly ReflectionBinding m_binding;
        private readonly ConfigStore m_store;
        private readonly string m_authToken;
        private readonly int m_port;
        private readonly bool m_enableCORSDevMode;
        private readonly string m_staticPath;
        private readonly ILogger m_logger;

        // Cached server info (captured on main thread)
        private CachedServerInfo m_cachedServerInfo;

        // Callbacks for main thread actions
        private MainThreadAction m_restartCallback;
        private MainThreadAction m_poseResetCallback;

        public bool IsRunning => m_server != null && m_server.State == WebServerState.Listening;
        public string BaseUrl => $"http://localhost:{m_port}/";

        public ConfigServer(
            ReflectionBinding binding,
            ConfigStore store,
            string authToken,
            int port,
            bool enableCORSDevMode,
            string staticPath,
            ILogger logger,
            MainThreadAction restartCallback,
            MainThreadAction poseResetCallback
        )
        {
            m_binding = binding;
            m_store = store;
            m_authToken = authToken;
            m_port = port;
            m_enableCORSDevMode = enableCORSDevMode;
            m_staticPath = staticPath;
            m_logger = logger;
            m_restartCallback = restartCallback;
            m_poseResetCallback = poseResetCallback;

            // Cache server info on main thread (before server starts on background thread)
            CacheServerInfo();
        }

        private void CacheServerInfo()
        {
            m_cachedServerInfo = new CachedServerInfo
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
                deviceName = UnityEngine.SystemInfo.deviceName,
                operatingSystem = UnityEngine.SystemInfo.operatingSystem,

                // System Information
                processorType = UnityEngine.SystemInfo.processorType,
                processorCount = UnityEngine.SystemInfo.processorCount,
                systemMemorySize = UnityEngine.SystemInfo.systemMemorySize,
                graphicsDeviceName = UnityEngine.SystemInfo.graphicsDeviceName,
            };
        }

        public void Start()
        {
            if (IsRunning)
            {
                m_logger?.LogWarning("[ConfigServer] Server already running");
                return;
            }

            m_cancellationTokenSource = new CancellationTokenSource();

            m_logger?.Log($"[ConfigServer] Starting server on port {m_port}");
            m_logger?.Log($"[ConfigServer] Static files path: {m_staticPath}");

            m_server = new WebServer(o =>
                o.WithUrlPrefix($"http://*:{m_port}/").WithMode(HttpListenerMode.EmbedIO)
            )
                .WithModule(new ActionModule("/api", HttpVerbs.Any, HandleApiRequest))
                .WithStaticFolder("/", m_staticPath, true);

            // Disable verbose EmbedIO logging (only if not already unregistered)
            m_server.StateChanged += (s, e) => { }; // Suppress state change logs
            try
            {
                Swan.Logging.Logger.UnregisterLogger<Swan.Logging.ConsoleLogger>();
            }
            catch
            {
                // Logger already unregistered on previous start, ignore
            }

            Task.Run(async () =>
            {
                try
                {
                    await m_server.RunAsync(m_cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    m_logger?.LogError($"[ConfigServer] Server error: {ex.Message}");
                }
            });

            m_logger?.Log($"[ConfigServer] Server started at {BaseUrl}");
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            m_logger?.Log("[ConfigServer] Stopping server...");

            m_cancellationTokenSource?.Cancel();
            m_server?.Dispose();
            m_server = null;

            m_logger?.Log("[ConfigServer] Server stopped");
        }

        private async Task HandleApiRequest(IHttpContext context)
        {
            try
            {
                // CORS headers
                if (m_enableCORSDevMode)
                {
                    context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                }
                context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                context.Response.Headers.Add(
                    "Access-Control-Allow-Headers",
                    "Content-Type, Authorization"
                );

                // OPTIONS preflight
                if (context.Request.HttpVerb == HttpVerbs.Options)
                {
                    context.Response.StatusCode = 200;
                    return;
                }

                // No authentication required - open access

                // Route requests
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
                    await context.SendDataAsync(new { success = false, message = "Not found" });
                }
            }
            catch (Exception ex)
            {
                m_logger?.LogError($"[ConfigServer] Request error: {ex.Message}");
                context.Response.StatusCode = 500;
                await context.SendDataAsync(new { success = false, message = ex.Message });
            }
        }

        private async Task HandleGetSchema(IHttpContext context)
        {
            var schema = m_binding.GenerateSchema();
            await context.SendDataAsync(schema);
        }

        private async Task HandleGetConfig(IHttpContext context)
        {
            var values = m_binding.GetAllValues();
            var result = new
            {
                success = true,
                values = values,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            await context.SendDataAsync(result);
        }

        private async Task HandlePostConfig(IHttpContext context)
        {
            string body = await context.GetRequestBodyAsStringAsync();
            var updateRequest = JsonConvert.DeserializeObject<ConfigUpdateRequest>(body);

            if (updateRequest == null || string.IsNullOrEmpty(updateRequest.path))
            {
                context.Response.StatusCode = 400;
                await context.SendDataAsync(new { success = false, message = "Invalid request" });
                return;
            }

            var oldValue = m_binding.GetValue(updateRequest.path);
            bool success = m_binding.SetValue(updateRequest.path, updateRequest.value);
            var newValue = m_binding.GetValue(updateRequest.path);

            if (success)
            {
                var configData = new ConfigData { values = m_binding.GetAllValues() };
                m_store.SaveConfig(configData);

                var response = new ConfigUpdateResponse
                {
                    success = true,
                    message = "Configuration updated",
                    oldValue = oldValue,
                    newValue = newValue,
                };

                await context.SendDataAsync(response);
            }
            else
            {
                context.Response.StatusCode = 400;
                await context.SendDataAsync(
                    new ConfigUpdateResponse
                    {
                        success = false,
                        message = "Failed to update configuration",
                    }
                );
            }
        }

        private async Task HandleGetInfo(IHttpContext context)
        {
            // Use cached server info (captured on main thread during construction)
            var info = new
            {
                // Cached from main thread
                appName = m_cachedServerInfo.appName,
                version = m_cachedServerInfo.version,
                unityVersion = m_cachedServerInfo.unityVersion,
                buildDate = m_cachedServerInfo.buildDate,
                platform = m_cachedServerInfo.platform,
                deviceModel = m_cachedServerInfo.deviceModel,
                deviceName = m_cachedServerInfo.deviceName,
                operatingSystem = m_cachedServerInfo.operatingSystem,
                processorType = m_cachedServerInfo.processorType,
                processorCount = m_cachedServerInfo.processorCount,
                systemMemorySize = m_cachedServerInfo.systemMemorySize,
                graphicsDeviceName = m_cachedServerInfo.graphicsDeviceName,

                // Runtime information (safe on background thread)
                connectedClients = 0,
                configPath = m_store.GetConfigPath(),
                serverPort = m_port,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            await context.SendDataAsync(info);
        }

        private async Task HandleGetStatus(IHttpContext context)
        {
            var status = StatusProvider.Instance.GetStatus();
            await context.SendDataAsync(status);
        }

        private async Task HandleGetLogs(IHttpContext context)
        {
            int count = 100;
            if (context.Request.QueryString["count"] != null)
            {
                int.TryParse(context.Request.QueryString["count"], out count);
            }

            var logs = LogCollector.Instance.GetRecentLogs(count);
            await context.SendDataAsync(new { success = true, logs = logs });
        }

        private async Task HandleClearLogs(IHttpContext context)
        {
            LogCollector.Instance.ClearLogs();
            await context.SendDataAsync(new { success = true, message = "Logs cleared" });
        }

        private async Task HandleRestart(IHttpContext context)
        {
            await context.SendDataAsync(new { success = true, message = "Restart initiated" });

            // Trigger restart on main thread via callback
            m_restartCallback?.Invoke();
        }

        private async Task HandleResetPose(IHttpContext context)
        {
            m_poseResetCallback?.Invoke();
            await context.SendDataAsync(new { success = true, message = "Pose reset initiated" });
        }
    }
}
