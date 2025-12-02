using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using QuestNav.Config;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.WebServer.Server;
using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Interface for WebServer management.
    /// </summary>
    public interface IWebServerManager
    {
        /// <summary>
        /// Gets whether the HTTP server is currently running.
        /// </summary>
        bool IsServerRunning { get; }

        /// <summary>
        /// Initializes the web server asynchronously.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Periodic update method. Called from QuestNav.SlowUpdate() at 3Hz.
        /// </summary>
        void Periodic(
            Vector3 position,
            Quaternion rotation,
            bool isTracking,
            int trackingLostEvents
        );

        /// <summary>
        /// Stops the web server and cleans up resources.
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// Manages the QuestNav configuration web server.
    /// Provides HTTP endpoints for configuration, status, and control.
    /// </summary>
    public class WebServerManager : IWebServerManager
    {
        #region Fields
        private readonly IConfigManager configManager;
        private readonly INetworkTableConnection networkTableConnection;
        private readonly Action poseResetCallback;
        private readonly SynchronizationContext mainThreadContext;

        private ConfigServer server;
        private StatusProvider statusProvider;
        private LogCollector logCollector;

        private bool isInitialized;

        // Cached values updated via config events
        private int cachedTeamNumber;
        private string cachedDebugIpOverride = "";
        private string cachedRobotIpAddress = "";
        #endregion

        /// <summary>
        /// Creates a new WebServerManager with required dependencies.
        /// </summary>
        /// <param name="configManager">Config manager for reading/writing settings</param>
        /// <param name="networkTableConnection">Network connection for status updates</param>
        /// <param name="poseResetCallback">Callback to invoke when pose reset is requested</param>
        public WebServerManager(
            IConfigManager configManager,
            INetworkTableConnection networkTableConnection,
            Action poseResetCallback
        )
        {
            this.configManager = configManager;
            this.networkTableConnection = networkTableConnection;
            this.poseResetCallback = poseResetCallback;
            this.mainThreadContext = SynchronizationContext.Current;

            statusProvider = new StatusProvider();
            logCollector = new LogCollector();

            // Subscribe to config change events
            configManager.OnTeamNumberChanged += OnTeamNumberChanged;
            configManager.OnDebugIpOverrideChanged += OnDebugIpOverrideChanged;
        }

        #region Properties
        public bool IsServerRunning => server?.IsRunning ?? false;
        #endregion

        #region Event Subscribers
        private void OnTeamNumberChanged(int teamNumber)
        {
            cachedTeamNumber = teamNumber;
            UpdateRobotIpAddress();
        }

        private void OnDebugIpOverrideChanged(string debugIpOverride)
        {
            cachedDebugIpOverride = debugIpOverride ?? "";
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

        #region Lifecycle Methods
        public async Task InitializeAsync()
        {
            if (isInitialized)
            {
                Debug.Log("[WebServerManager] Already initialized, skipping");
                return;
            }

            Debug.Log("[WebServerManager] Initializing...");

            // Load initial cached values from config
            OnTeamNumberChanged(await configManager.GetTeamNumberAsync());
            OnDebugIpOverrideChanged(await configManager.GetDebugIpOverrideAsync());

            logCollector.Initialize();

            await StartServerAsync();

            isInitialized = true;
            Debug.Log("[WebServerManager] Initialization complete");
        }

        public void Shutdown()
        {
            Debug.Log("[WebServerManager] Shutting down...");

            configManager.OnTeamNumberChanged -= OnTeamNumberChanged;
            configManager.OnDebugIpOverrideChanged -= OnDebugIpOverrideChanged;

            server?.Stop();
            server = null;
            logCollector?.Dispose();

            Debug.Log("[WebServerManager] Shutdown complete");
        }
        #endregion

        #region Periodic
        public void Periodic(
            Vector3 position,
            Quaternion rotation,
            bool isTracking,
            int trackingLostEvents
        )
        {
            string ipAddress = GetLocalIPAddress();
            float currentFps = 1f / Time.deltaTime;

            statusProvider?.UpdateStatus(
                position,
                rotation,
                isTracking,
                trackingLostEvents,
                SystemInfo.batteryLevel,
                SystemInfo.batteryStatus,
                networkTableConnection.IsConnected,
                ipAddress,
                cachedTeamNumber,
                cachedRobotIpAddress,
                currentFps,
                Time.frameCount
            );
        }
        #endregion

        #region Main Thread Callbacks
        /// <summary>
        /// Requests a pose reset to be executed on the main thread.
        /// Called from ConfigServer on background HTTP thread.
        /// </summary>
        internal void RequestPoseReset()
        {
            Debug.Log("[WebServerManager] Pose reset requested from web interface");
            mainThreadContext.Post(_ => poseResetCallback?.Invoke(), null);
        }

        /// <summary>
        /// Requests an app restart to be executed on the main thread.
        /// Called from ConfigServer on background HTTP thread.
        /// </summary>
        internal void RequestRestart()
        {
            Debug.Log("[WebServerManager] Restart requested from web interface");
            mainThreadContext.Post(_ => RestartApp(), null);
        }
        #endregion

        #region Server Setup
        private async Task StartServerAsync()
        {
            Debug.Log("[WebServerManager] Starting configuration server...");

            string staticPath = GetStaticFilesPath();
            if (string.IsNullOrEmpty(staticPath))
            {
                Debug.LogError("[WebServerManager] Failed to get static files path");
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            await ExtractAndroidUIFilesAsync(staticPath);
#else
            EnsureStaticFilesExist(staticPath);
            await Task.CompletedTask;
#endif

            server = new ConfigServer(
                configManager,
                QuestNavConstants.WebServer.SERVER_PORT,
                QuestNavConstants.WebServer.ENABLE_CORS_DEV_MODE,
                staticPath,
                new UnityLogger(),
                this,
                statusProvider,
                logCollector
            );

            await server.StartAsync();

            if (!server.IsRunning)
            {
                Debug.LogError("[WebServerManager] Server did not start successfully");
                return;
            }

            ShowConnectionInfo();
            Debug.Log("[WebServerManager] Server started successfully");
        }

        private string GetStaticFilesPath()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Path.Combine(Application.persistentDataPath, "ui");
#else
            return Path.Combine(Application.streamingAssetsPath, "ui");
#endif
        }
        #endregion

        #region Static File Management
#if UNITY_ANDROID && !UNITY_EDITOR
        private async Task ExtractAndroidUIFilesAsync(string targetPath)
        {
            if (Directory.Exists(targetPath))
            {
                Debug.Log("[WebServerManager] Clearing old UI files...");
                Directory.Delete(targetPath, true);
            }

            Debug.Log("[WebServerManager] Extracting UI files from APK...");

            Directory.CreateDirectory(targetPath);
            string assetsDir = Path.Combine(targetPath, "assets");
            Directory.CreateDirectory(assetsDir);

            await ExtractAndroidFileAsync("ui/index.html", Path.Combine(targetPath, "index.html"));
            await ExtractAndroidFileAsync(
                "ui/assets/main.css",
                Path.Combine(assetsDir, "main.css")
            );
            await ExtractAndroidFileAsync("ui/assets/main.js", Path.Combine(assetsDir, "main.js"));
            await ExtractAndroidFileAsync("ui/logo.svg", Path.Combine(targetPath, "logo.svg"));
            await ExtractAndroidFileAsync(
                "ui/logo-dark.svg",
                Path.Combine(targetPath, "logo-dark.svg")
            );

            Debug.Log("[WebServerManager] UI extraction complete");
        }

        private async Task ExtractAndroidFileAsync(string sourceRelative, string targetAbsolute)
        {
            string sourcePath = Path.Combine(Application.streamingAssetsPath, sourceRelative);

            using (var www = UnityEngine.Networking.UnityWebRequest.Get(sourcePath))
            {
                var operation = www.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

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
#endif

        private void EnsureStaticFilesExist(string path)
        {
            if (Directory.Exists(path))
                return;

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
                    Debug.Log($"[WebServerManager] Copied fallback HTML from {fallbackSourcePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebServerManager] Failed to copy fallback HTML: {ex.Message}");
            }
        }
        #endregion

        #region Utility Methods
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

        private void ShowConnectionInfo()
        {
            Debug.Log("╔═══════════════════════════════════════════════════════════╗");
            Debug.Log("║          QuestNav Configuration Server                    ║");
            Debug.Log("╠═══════════════════════════════════════════════════════════╣");
            Debug.Log($"║ Port: {QuestNavConstants.WebServer.SERVER_PORT}");
            Debug.Log("╠═══════════════════════════════════════════════════════════╣");
            Debug.Log($"║ Connect: http://<quest-ip>:{QuestNavConstants.WebServer.SERVER_PORT}/");
            Debug.Log("╚═══════════════════════════════════════════════════════════╝");
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
        #endregion
    }
}
