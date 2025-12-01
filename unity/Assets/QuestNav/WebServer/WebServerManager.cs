using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using QuestNav.Config;
using QuestNav.Network;
using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Main orchestrator for the WebServer subsystem.
    /// Manages configuration server, status provider, and log collector.
    /// Follows same dependency injection pattern as UIManager.
    /// </summary>
    public class WebServerManager : IWebServerManager
    {
        private readonly IConfigManager configManager;
        private readonly INetworkTableConnection networkTableConnection;
        private readonly MonoBehaviour coroutineHost;
        private readonly Action poseResetCallback;

        private ConfigServer server;
        private StatusProvider statusProvider;
        private LogCollector logCollector;

        private const int SERVER_PORT = 8080;
        private const bool ENABLE_CORS_DEV_MODE = true;

        private bool isInitialized = false;
        private bool restartRequested = false;
        private volatile bool poseResetRequested = false;

        // Pending config update to be applied on main thread
        private volatile ConfigUpdateRequest pendingConfigUpdate = null;
        private readonly object configUpdateLock = new object();

        // Cached values updated via events
        private string cachedIpAddress = "0.0.0.0";
        private int cachedTeamNumber = 0;
        private string cachedDebugIpOverride = "";
        private string cachedRobotIpAddress = "";

        public bool IsServerRunning => server != null && server.IsRunning;

        /// <summary>
        /// Creates a new WebServerManager with required dependencies.
        /// </summary>
        public WebServerManager(
            IConfigManager configManager,
            INetworkTableConnection networkTableConnection,
            MonoBehaviour coroutineHost,
            Action poseResetCallback
        )
        {
            this.configManager = configManager;
            this.networkTableConnection = networkTableConnection;
            this.coroutineHost = coroutineHost;
            this.poseResetCallback = poseResetCallback;

            statusProvider = new StatusProvider();
            logCollector = new LogCollector();

            // Subscribe to config change events
            configManager.onTeamNumberChanged += OnTeamNumberChanged;
            configManager.onDebugIpOverrideChanged += OnDebugIpOverrideChanged;
        }

        #region Event Handlers
        private void OnTeamNumberChanged(int teamNumber)
        {
            cachedTeamNumber = teamNumber;
            UpdateRobotIpAddress();
        }

        private void OnDebugIpOverrideChanged(string debugIpOverride)
        {
            cachedDebugIpOverride = debugIpOverride;
            UpdateRobotIpAddress();
        }

        private void UpdateRobotIpAddress()
        {
            if (!string.IsNullOrEmpty(cachedDebugIpOverride))
            {
                cachedRobotIpAddress = cachedDebugIpOverride;
            }
            else if (cachedTeamNumber > 0)
            {
                cachedRobotIpAddress = $"10.{cachedTeamNumber / 100}.{cachedTeamNumber % 100}.2";
            }
            else
            {
                cachedRobotIpAddress = "";
            }
        }
        #endregion

        public async void Initialize()
        {
            if (isInitialized)
            {
                Debug.Log("[WebServerManager] Already initialized, skipping");
                return;
            }

            Debug.Log("[WebServerManager] Initializing...");

            // Load initial cached values
            cachedTeamNumber = await configManager.getTeamNumberAsync();
            cachedDebugIpOverride = await configManager.getDebugIpOverrideAsync();
            UpdateRobotIpAddress();

            logCollector.Initialize();
            coroutineHost.StartCoroutine(InitializeCoroutine());
        }

        public void Periodic(
            Vector3 position,
            Quaternion rotation,
            bool isTracking,
            int trackingLostEvents
        )
        {
            if (restartRequested)
            {
                restartRequested = false;
                Debug.Log("[WebServerManager] Executing restart on main thread");
                RestartApp();
            }

            if (poseResetRequested)
            {
                poseResetRequested = false;
                Debug.Log("[WebServerManager] Executing pose reset on main thread");
                poseResetCallback?.Invoke();
            }

            // Process pending config update on main thread
            ProcessPendingConfigUpdate();

            // Update IP address (this is synchronous, no Unity API calls)
            cachedIpAddress = GetLocalIPAddress();

            float currentFps = 1f / Time.deltaTime;
            statusProvider?.UpdateStatus(
                position,
                rotation,
                isTracking,
                trackingLostEvents,
                SystemInfo.batteryLevel,
                SystemInfo.batteryStatus,
                networkTableConnection.isConnected,
                cachedIpAddress,
                cachedTeamNumber,
                cachedRobotIpAddress,
                currentFps,
                Time.frameCount
            );
        }

        private async void ProcessPendingConfigUpdate()
        {
            ConfigUpdateRequest update = null;
            lock (configUpdateLock)
            {
                if (pendingConfigUpdate != null)
                {
                    update = pendingConfigUpdate;
                    pendingConfigUpdate = null;
                }
            }

            if (update == null)
                return;

            try
            {
                if (update.teamNumber.HasValue)
                    await configManager.setTeamNumberAsync(update.teamNumber.Value);
                if (update.debugIpOverride != null)
                    await configManager.setDebugIpOverrideAsync(update.debugIpOverride);
                if (update.enableAutoStartOnBoot.HasValue)
                    await configManager.setEnableAutoStartOnBootAsync(
                        update.enableAutoStartOnBoot.Value
                    );
                if (update.enableDebugLogging.HasValue)
                    await configManager.setEnableDebugLoggingAsync(update.enableDebugLogging.Value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebServerManager] Failed to apply config update: {ex.Message}");
            }
        }

        /// <summary>
        /// Queues a config update to be processed on the main thread.
        /// Called from ConfigServer on background thread.
        /// </summary>
        public void QueueConfigUpdate(ConfigUpdateRequest request)
        {
            lock (configUpdateLock)
            {
                pendingConfigUpdate = request;
            }
        }

        public void Shutdown()
        {
            Debug.Log("[WebServerManager] Shutting down...");

            // Unsubscribe from events
            configManager.onTeamNumberChanged -= OnTeamNumberChanged;
            configManager.onDebugIpOverrideChanged -= OnDebugIpOverrideChanged;

            server?.Stop();
            server = null;
            logCollector?.Dispose();
            Debug.Log("[WebServerManager] Shutdown complete");
        }

        private string GetLocalIPAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address.ToString() ?? "0.0.0.0";
                }
            }
            catch
            {
                return "0.0.0.0";
            }
        }

        private IEnumerator InitializeCoroutine()
        {
            Debug.Log("[WebServerManager] Starting initialization...");
            isInitialized = true;
            Debug.Log("[WebServerManager] Initialization complete");
            yield return StartServerCoroutine();
        }

        private IEnumerator StartServerCoroutine()
        {
            if (!isInitialized)
            {
                Debug.LogError("[WebServerManager] Cannot start server - not initialized");
                yield break;
            }

            if (server != null && server.IsRunning)
            {
                Debug.LogWarning("[WebServerManager] Server already running");
                yield break;
            }

            Debug.Log("[WebServerManager] Starting configuration server...");

            string staticPath = GetStaticFilesPath();
            if (string.IsNullOrEmpty(staticPath))
            {
                Debug.LogError("[WebServerManager] Failed to get static files path");
                yield break;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            yield return ExtractAndroidUIFiles(staticPath);
#else
            EnsureStaticFilesExist(staticPath);
#endif

            var logger = new UnityLogger();
            server = new ConfigServer(
                configManager,
                SERVER_PORT,
                ENABLE_CORS_DEV_MODE,
                staticPath,
                logger,
                RequestRestart,
                RequestPoseReset,
                QueueConfigUpdate,
                statusProvider,
                logCollector
            );

            server.Start();
            yield return null;

            if (!server.IsRunning)
            {
                Debug.LogError("[WebServerManager] Server did not start successfully");
                yield break;
            }

            ShowConnectionInfo();
            Debug.Log("[WebServerManager] Server started successfully");
        }

        private string GetStaticFilesPath()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string persistentPath = Path.Combine(Application.persistentDataPath, "ui");
            Debug.Log($"[WebServerManager] Using persistent path for Android: {persistentPath}");
            return persistentPath;
#else
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "ui");
            Debug.Log($"[WebServerManager] Using StreamingAssets path: {streamingPath}");
            return streamingPath;
#endif
        }

        private IEnumerator ExtractAndroidUIFiles(string targetPath)
        {
            if (Directory.Exists(targetPath))
            {
                Debug.Log("[WebServerManager] Clearing old UI files...");
                Directory.Delete(targetPath, true);
            }

            Debug.Log("[WebServerManager] Extracting UI files from APK...");

            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            string assetsDir = Path.Combine(targetPath, "assets");
            if (!Directory.Exists(assetsDir))
                Directory.CreateDirectory(assetsDir);

            yield return ExtractAndroidFile(
                "ui/index.html",
                Path.Combine(targetPath, "index.html")
            );
            yield return ExtractAndroidFile(
                "ui/assets/main.css",
                Path.Combine(assetsDir, "main.css")
            );
            yield return ExtractAndroidFile(
                "ui/assets/main.js",
                Path.Combine(assetsDir, "main.js")
            );
            yield return ExtractAndroidFile("ui/logo.svg", Path.Combine(targetPath, "logo.svg"));
            yield return ExtractAndroidFile(
                "ui/logo-dark.svg",
                Path.Combine(targetPath, "logo-dark.svg")
            );

            Debug.Log("[WebServerManager] UI extraction complete");
        }

        private IEnumerator ExtractAndroidFile(string sourceRelative, string targetAbsolute)
        {
            string sourcePath = Path.Combine(Application.streamingAssetsPath, sourceRelative);

            using (var www = UnityEngine.Networking.UnityWebRequest.Get(sourcePath))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(targetAbsolute, www.downloadHandler.data);
                    Debug.Log($"[WebServerManager] Extracted: {sourceRelative}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[WebServerManager] Failed to extract {sourceRelative}: {www.error}"
                    );
                }
            }
        }

        private void EnsureStaticFilesExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);

                string fallbackSourcePath = Path.Combine(
                    Application.streamingAssetsPath,
                    "ui",
                    "fallback.html"
                );
                string fallbackTargetPath = Path.Combine(path, "index.html");

                try
                {
                    if (File.Exists(fallbackSourcePath))
                    {
                        File.Copy(fallbackSourcePath, fallbackTargetPath);
                        Debug.Log(
                            $"[WebServerManager] Copied fallback HTML from {fallbackSourcePath}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(
                        $"[WebServerManager] Failed to copy fallback HTML: {ex.Message}"
                    );
                }
            }
        }

        private void ShowConnectionInfo()
        {
            Debug.Log("╔═══════════════════════════════════════════════════════════╗");
            Debug.Log("║          QuestNav Configuration Server                    ║");
            Debug.Log("╠═══════════════════════════════════════════════════════════╣");
            Debug.Log($"║ Port: {SERVER_PORT}");
            Debug.Log("╠═══════════════════════════════════════════════════════════╣");
            Debug.Log($"║ Connect: http://<quest-ip>:{SERVER_PORT}/");
            Debug.Log("╚═══════════════════════════════════════════════════════════╝");
        }

        private void RequestRestart()
        {
            Debug.Log("[WebServerManager] Restart requested from web interface");
            restartRequested = true;
        }

        private void RequestPoseReset()
        {
            Debug.Log("[WebServerManager] Pose reset requested from web interface");
            poseResetRequested = true;
        }

        private void RestartApp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var pm = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (
                    var intent = pm.Call<AndroidJavaObject>(
                        "getLaunchIntentForPackage",
                        Application.identifier
                    )
                )
                {
                    const int FLAG_ACTIVITY_NEW_TASK = 0x10000000;
                    const int FLAG_ACTIVITY_CLEAR_TASK = 0x00008000;
                    const int FLAG_ACTIVITY_CLEAR_TOP = 0x04000000;

                    intent.Call<AndroidJavaObject>(
                        "addFlags",
                        FLAG_ACTIVITY_NEW_TASK | FLAG_ACTIVITY_CLEAR_TASK | FLAG_ACTIVITY_CLEAR_TOP
                    );
                    activity.Call("startActivity", intent);

                    Debug.Log(
                        "[WebServerManager] New instance started, killing current process..."
                    );
                }

                using (var process = new AndroidJavaClass("android.os.Process"))
                {
                    int pid = process.CallStatic<int>("myPid");
                    process.CallStatic("killProcess", pid);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebServerManager] Failed to restart: {ex.Message}");
                Application.Quit();
            }
#else
            Application.Quit();
#endif
        }
    }
}
