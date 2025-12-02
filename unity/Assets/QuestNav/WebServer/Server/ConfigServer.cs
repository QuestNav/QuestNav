using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using Newtonsoft.Json;
using QuestNav.Config;
using UnityEngine;

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

        private readonly System.Collections.Generic.Dictionary<string, DateTime> activeClients =
            new System.Collections.Generic.Dictionary<string, DateTime>();
        private readonly object clientsLock = new object();
        private readonly TimeSpan activeClientWindow = TimeSpan.FromSeconds(30);

        public bool IsRunning => server != null && server.State == WebServerState.Listening;
        public string BaseUrl => $"http://localhost:{port}/";

        public ConfigServer(
            IConfigManager configManager,
            int port,
            bool enableCorsDevMode,
            string staticPath,
            ILogger logger,
            WebServerManager webServerManager,
            StatusProvider statusProvider,
            LogCollector logCollector
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
                .WithStaticFolder("/", staticPath, true);

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
            catch { }

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
                else if (path == "/api/download-database" && context.Request.HttpVerb == HttpVerbs.Get)
                {
                    await HandleDownloadDatabase(context);
                }
                else if (path == "/api/upload-database" && context.Request.HttpVerb == HttpVerbs.Post)
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
            var response = new ConfigResponse
            {
                success = true,
                teamNumber = await configManager.GetTeamNumberAsync(),
                debugIpOverride = await configManager.GetDebugIpOverrideAsync(),
                enableAutoStartOnBoot = await configManager.getEnableAutoStartOnBootAsync(),
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
                    await configManager.setEnableAutoStartOnBootAsync(request.EnableAutoStartOnBoot.Value);
                }
                if (request.EnableDebugLogging.HasValue)
                {
                    await configManager.SetEnableDebugLoggingAsync(request.EnableDebugLogging.Value);
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
                    new SimpleResponse { success = true, message = "Configuration reset to defaults" }
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
    }
}
