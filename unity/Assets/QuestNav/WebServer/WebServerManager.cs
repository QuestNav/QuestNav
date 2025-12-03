using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using QuestNav.Commands;
using QuestNav.Commands.Commands;
using QuestNav.Config;
using QuestNav.Core;
using QuestNav.Network;
using QuestNav.Protos.Generated;
using QuestNav.Utils;
using QuestNav.WebServer.Server;
using UnityEngine;
using Wpi.Proto;

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
        private readonly Transform vrCamera;
        private readonly Transform vrCameraRoot;
        private readonly Transform resetTransform;
        private SynchronizationContext mainThreadContext;

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
        /// <param name="vrCamera">Reference to the VR camera transform</param>
        /// <param name="vrCameraRoot">Reference to the VR camera root transform</param>
        /// <param name="resetTransform">Reference to the reset position transform</param>
        public WebServerManager(
            IConfigManager configManager,
            INetworkTableConnection networkTableConnection,
            Transform vrCamera,
            Transform vrCameraRoot,
            Transform resetTransform
        )
        {
            this.configManager = configManager;
            this.networkTableConnection = networkTableConnection;
            this.vrCamera = vrCamera;
            this.vrCameraRoot = vrCameraRoot;
            this.resetTransform = resetTransform;

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
            mainThreadContext = SynchronizationContext.Current;

            if (isInitialized)
            {
                QueuedLogger.Log("Already initialized, skipping");
                return;
            }

            QueuedLogger.Log("Initializing...");

            // Load initial cached values from config
            OnTeamNumberChanged(await configManager.GetTeamNumberAsync());
            OnDebugIpOverrideChanged(await configManager.GetDebugIpOverrideAsync());

            logCollector.Initialize();

            await StartServerAsync();

            isInitialized = true;
            QueuedLogger.Log("Initialization complete");
        }

        public void Shutdown()
        {
            QueuedLogger.Log("Shutting down...");

            configManager.OnTeamNumberChanged -= OnTeamNumberChanged;
            configManager.OnDebugIpOverrideChanged -= OnDebugIpOverrideChanged;

            server?.Stop();
            server = null;
            logCollector?.Dispose();

            QueuedLogger.Log("Shutdown complete");
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
            QueuedLogger.Log("Pose reset requested from web interface");
            mainThreadContext.Post(_ => ExecutePoseResetToOrigin(), null);
        }

        /// <summary>
        /// Executes pose reset to origin (0,0,0) with no rotation.
        /// Uses the existing PoseResetCommand implementation to ensure single source of truth.
        /// </summary>
        private void ExecutePoseResetToOrigin()
        {
            QueuedLogger.Log("Web interface requested pose reset to origin");

            // Create a protobuf command payload for origin reset in FRC coordinates
            var resetPose = new ProtobufPose3d
            {
                Translation = new ProtobufTranslation3d
                {
                    X = 0,
                    Y = 0,
                    Z = 0,
                },
                Rotation = new ProtobufRotation3d
                {
                    Q = new ProtobufQuaternion
                    {
                        X = 0,
                        Y = 0,
                        Z = 0,
                        W = 1,
                    },
                },
            };

            var command = new ProtobufQuestNavCommand
            {
                Type = QuestNavCommandType.PoseReset,
                CommandId = (uint)DateTime.UtcNow.Ticks,
                PoseResetPayload = new ProtobufQuestNavPoseResetPayload { TargetPose = resetPose },
            };

            // Create web command context for web-initiated reset
            // (no NetworkTables response needed for web interface)
            var webContext = new WebCommandContext();

            // Create a temporary command instance for web-initiated reset
            var webPoseResetCommand = new PoseResetCommand(
                webContext, // Web context is no-op (no NetworkTables responses)
                vrCamera,
                vrCameraRoot,
                resetTransform
            );

            // Execute the pose reset using the existing command implementation
            webPoseResetCommand.Execute(command);

            QueuedLogger.Log("[QuestNav] Pose reset to origin completed");
        }

        /// <summary>
        /// Requests an app restart to be executed on the main thread.
        /// Called from ConfigServer on background HTTP thread.
        /// </summary>
        internal void RequestRestart()
        {
            QueuedLogger.Log("Restart requested from web interface");
            mainThreadContext.Post(_ => RestartApp(), null);
        }
        #endregion

        #region Server Setup
        private async Task StartServerAsync()
        {
            QueuedLogger.Log("Starting configuration server...");

            string staticPath = GetStaticFilesPath();
            if (string.IsNullOrEmpty(staticPath))
            {
                QueuedLogger.LogError("Failed to get static files path");
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
                QueuedLogger.LogError("Server did not start successfully");
                return;
            }

            ShowConnectionInfo();
            QueuedLogger.Log("Server started successfully");
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
                QueuedLogger.Log("Clearing old UI files...");
                Directory.Delete(targetPath, true);
            }

            QueuedLogger.Log("Extracting UI files from APK...");

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

            QueuedLogger.Log("UI extraction complete");
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
                    QueuedLogger.Log($"Extracted: {sourceRelative}");
                }
                else
                {
                    QueuedLogger.LogWarning($"Failed to extract {sourceRelative}: {www.error}");
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
                    QueuedLogger.Log($"Copied fallback HTML from {fallbackSourcePath}");
                }
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"Failed to copy fallback HTML: {ex.Message}");
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
            QueuedLogger.Log(
                $"Connect to WebUI at http://{GetLocalIPAddress()}:{QuestNavConstants.WebServer.SERVER_PORT}"
            );
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

                    QueuedLogger.Log("New instance started, killing current process...");
                }

                using (var process = new AndroidJavaClass("android.os.Process"))
                {
                    int pid = process.CallStatic<int>("myPid");
                    process.CallStatic("killProcess", pid);
                }
            }
            catch (Exception ex)
            {
                QueuedLogger.LogError($"Failed to restart: {ex.Message}");
                Application.Quit();
            }
#else
            Application.Quit();
#endif
        }
        #endregion
    }
}
