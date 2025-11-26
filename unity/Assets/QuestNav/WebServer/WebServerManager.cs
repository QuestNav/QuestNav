using System;
using System.Collections;
using System.IO;
using Meta.XR;
using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Main orchestrator for the WebServer subsystem.
    /// Manages configuration server, status provider, log collector, and pose reset provider.
    /// Designed for dependency injection from QuestNav.cs, replacing ConfigBootstrap MonoBehaviour.
    /// Handles server lifecycle, file extraction on Android, and main thread coordination.
    /// </summary>
    public class WebServerManager : IWebServerManager
    {
        #region Fields
        /// <summary>
        /// HTTP configuration server instance
        /// </summary>
        private ConfigServer server;

        /// <summary>
        /// Reflection binding for configuration fields
        /// </summary>
        private ReflectionBinding binding;

        /// <summary>
        /// Configuration persistence store
        /// </summary>
        private ConfigStore store;

        /// <summary>
        /// Status provider for web interface
        /// </summary>
        private StatusProvider statusProvider;

        /// <summary>
        /// Mjpeg stream provider for web interface
        /// </summary>
        private readonly VideoStreamProvider streamProvider;

        /// <summary>
        /// Log collector for web interface
        /// </summary>
        private LogCollector logCollector;

        /// <summary>
        /// Callback for pose reset requests from web interface
        /// </summary>
        private MainThreadAction poseResetCallback;

        /// <summary>
        /// HTTP server port for configuration UI
        /// </summary>
        private readonly int serverPort;

        /// <summary>
        /// Enable CORS for localhost development
        /// </summary>
        private readonly bool enableCORSDevMode;

        /// <summary>
        /// Flag indicating if initialization is complete
        /// </summary>
        private bool isInitialized = false;

        /// <summary>
        /// Flag for app restart request from background thread
        /// </summary>
        private bool restartRequested = false;

        /// <summary>
        /// Flag for pose reset request from background thread
        /// </summary>
        private volatile bool poseResetRequested = false;

        /// <summary>
        /// MonoBehaviour for coroutine execution
        /// </summary>
        private readonly MonoBehaviour coroutineHost;
        #endregion

        #region Properties
        /// <summary>
        /// Gets whether the server is currently running
        /// </summary>
        public bool IsServerRunning => server != null && server.IsRunning;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new WebServerManager with injected dependencies.
        /// </summary>
        /// <param name="vrCamera">Transform of the VR camera (center eye anchor)</param>
        /// <param name="vrCameraRoot">Transform of the VR camera root</param>
        /// <param name="passthroughOptions">Provides access to settings for video streaming</param>
        /// <param name="cameraAccess">Provides access to the headset cameras</param>
        /// <param name="coroutineHost">MonoBehaviour to use for coroutine execution</param>
        /// <param name="serverPort">HTTP server port for web interface</param>
        /// <param name="enableCORSDevMode">Enable CORS for localhost development</param>
        /// <param name="poseResetCallback">Callback to execute pose reset on main thread</param>
        public WebServerManager(
            Transform vrCamera,
            Transform vrCameraRoot,
            IPassthroughOptions passthroughOptions,
            PassthroughCameraAccess cameraAccess,
            MonoBehaviour coroutineHost,
            int serverPort,
            bool enableCORSDevMode,
            MainThreadAction poseResetCallback
        )
        {
            this.coroutineHost = coroutineHost;
            this.serverPort = serverPort;
            this.enableCORSDevMode = enableCORSDevMode;
            this.poseResetCallback = poseResetCallback;

            // Initialize services
            statusProvider = new StatusProvider();
            logCollector = new LogCollector();
            streamProvider = new VideoStreamProvider(cameraAccess, passthroughOptions);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initializes the web server system.
        /// Must be called on Unity main thread during application startup.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.Log("[WebServerManager] Already initialized, skipping");
                return;
            }

            Debug.Log("[WebServerManager] Initializing configuration system...");

            // Initialize log collector (subscribes to Unity log events)
            logCollector.Initialize();

            // Start initialization coroutine
            coroutineHost.StartCoroutine(InitializeCoroutine());
            
            // Start video stream coroutine
            coroutineHost.StartCoroutine(streamProvider.FrameCaptureCoroutine());
        }

        /// <summary>
        /// Periodic update method for web server operations.
        /// Should be called from QuestNav.SlowUpdate() at 3Hz.
        /// Handles pending operations that need to run on the main thread.
        /// </summary>
        public void Periodic()
        {
            // Check for restart request from web interface
            if (restartRequested)
            {
                restartRequested = false;
                Debug.Log("[WebServerManager] Executing restart on main thread");
                RestartApp();
            }

            // Check for pose reset request from web interface
            if (poseResetRequested)
            {
                poseResetRequested = false;
                Debug.Log("[WebServerManager] Executing pose reset on main thread");
                poseResetCallback?.Invoke();
            }
        }

        /// <summary>
        /// Updates status information for the web interface.
        /// Should be called from QuestNav.SlowUpdate() at 3Hz.
        /// </summary>
        public void UpdateStatus(
            Vector3 position,
            Quaternion rotation,
            bool isTracking,
            int trackingLostEvents,
            float batteryLevel,
            BatteryStatus batteryStatus,
            bool isConnected,
            string ipAddress,
            int teamNumber,
            string robotIpAddress,
            float fps,
            int frameCount
        )
        {
            statusProvider?.UpdateStatus(
                position,
                rotation,
                isTracking,
                trackingLostEvents,
                batteryLevel,
                batteryStatus,
                isConnected,
                ipAddress,
                teamNumber,
                robotIpAddress,
                fps,
                frameCount
            );
        }

        /// <summary>
        /// Stops the web server and cleans up resources.
        /// Should be called on application shutdown.
        /// </summary>
        public void Shutdown()
        {
            Debug.Log("[WebServerManager] Shutting down...");

            // Stop HTTP server
            if (server != null)
            {
                server.Stop();
                server = null;
            }

            // Dispose log collector
            logCollector?.Dispose();

            Debug.Log("[WebServerManager] Shutdown complete");
        }
        #endregion

        #region Private Methods - Initialization
        /// <summary>
        /// Initializes the configuration system: reflection binding, config store, and HTTP server.
        /// Loads saved configuration and applies values to tunables.
        /// Must run on main thread for Unity API access.
        /// </summary>
        private IEnumerator InitializeCoroutine()
        {
            Debug.Log("[WebServerManager] Starting initialization coroutine...");

            // Initialize reflection binding to scan for [Config] attributes
            binding = new ReflectionBinding();
            if (binding == null)
            {
                Debug.LogError("[WebServerManager] Failed to create ReflectionBinding");
                yield break;
            }
            Debug.Log($"[WebServerManager] Found {binding.FieldCount} configurable fields");

            // Initialize configuration store for persistence
            store = new ConfigStore();
            if (store == null)
            {
                Debug.LogError("[WebServerManager] Failed to create ConfigStore");
                yield break;
            }

            // Load saved configuration from disk
            var savedConfig = store.LoadConfig();
            if (savedConfig != null && savedConfig.values != null && savedConfig.values.Count > 0)
            {
                Debug.Log($"[WebServerManager] Applying {savedConfig.values.Count} saved values");
                binding.ApplyValues(savedConfig.values);
            }

            isInitialized = true;
            Debug.Log("[WebServerManager] Initialization complete");

            // Start HTTP server
            yield return StartServerCoroutine();
        }

        /// <summary>
        /// Starts the HTTP configuration server.
        /// Extracts web UI files on Android, then starts EmbedIO server.
        /// </summary>
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

            // Get static files path
            string staticPath = GetStaticFilesPath();
            if (string.IsNullOrEmpty(staticPath))
            {
                Debug.LogError("[WebServerManager] Failed to get static files path");
                yield break;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, extract UI files from APK first
            yield return ExtractAndroidUIFiles(staticPath);
#else
            // On other platforms, ensure static files directory exists
            EnsureStaticFilesExist(staticPath);
#endif

            // Create server with logger interface
            var logger = new UnityLogger();
            server = new ConfigServer(
                binding,
                store,
                serverPort,
                enableCORSDevMode,
                staticPath,
                logger,
                RestartApplication,
                RequestPoseReset, // Pass flag-setter method instead of direct callback
                statusProvider,
                logCollector,
                streamProvider
            );

            if (server == null)
            {
                Debug.LogError("[WebServerManager] Failed to create ConfigServer");
                yield break;
            }

            // Start server on background thread
            server.Start();

            // Wait a frame to let server start
            yield return null;

            if (!server.IsRunning)
            {
                Debug.LogError("[WebServerManager] Server did not start successfully");
                yield break;
            }

            ShowConnectionInfo();
            Debug.Log("[WebServerManager] Server started successfully");
        }
        #endregion

        #region Private Methods - Static Files Management
        /// <summary>
        /// Gets the appropriate static files path for the current platform.
        /// On Android: Application.persistentDataPath/ui (extracted from APK)
        /// On other platforms: Application.streamingAssetsPath/ui
        /// </summary>
        /// <returns>Path to static UI files</returns>
        private string GetStaticFilesPath()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android, must extract from APK to persistentDataPath
            string persistentPath = Path.Combine(Application.persistentDataPath, "ui");
            Debug.Log($"[WebServerManager] Using persistent path for Android: {persistentPath}");
            return persistentPath;
#else
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "ui");
            Debug.Log($"[WebServerManager] Using StreamingAssets path: {streamingPath}");
            return streamingPath;
#endif
        }

        /// <summary>
        /// Extracts UI files from Android APK to persistent storage.
        /// Android cannot serve files directly from APK, so we extract them first.
        /// Forces fresh extraction on each app start to ensure UI is up-to-date.
        /// </summary>
        /// <param name="targetPath">Destination path for extracted files</param>
        private IEnumerator ExtractAndroidUIFiles(string targetPath)
        {
            // Force delete old UI files to ensure fresh extraction
            if (Directory.Exists(targetPath))
            {
                Debug.Log("[WebServerManager] Clearing old UI files...");
                Directory.Delete(targetPath, true);
            }

            string indexPath = Path.Combine(targetPath, "index.html");
            string assetsDir = Path.Combine(targetPath, "assets");

            // Delete old fallback files if they exist
            if (File.Exists(indexPath))
            {
                File.Delete(indexPath);
            }

            Debug.Log("[WebServerManager] Extracting UI files from APK to persistent storage...");

            if (!Directory.Exists(targetPath))
            {
                Directory.CreateDirectory(targetPath);
            }

            // Extract index.html
            yield return ExtractAndroidFile("ui/index.html", indexPath);

            // Create assets directory
            if (!Directory.Exists(assetsDir))
            {
                Directory.CreateDirectory(assetsDir);
            }

            Debug.Log($"[WebServerManager] Extracting assets to: {assetsDir}");

            // Extract Vite output files (using consistent naming without hashes)
            yield return ExtractAndroidFile(
                "ui/assets/main.css",
                Path.Combine(assetsDir, "main.css")
            );
            yield return ExtractAndroidFile(
                "ui/assets/main.js",
                Path.Combine(assetsDir, "main.js")
            );

            // Extract logo files
            yield return ExtractAndroidFile("ui/logo.svg", Path.Combine(targetPath, "logo.svg"));
            yield return ExtractAndroidFile(
                "ui/logo-dark.svg",
                Path.Combine(targetPath, "logo-dark.svg")
            );

            Debug.Log("[WebServerManager] UI extraction complete");
        }

        /// <summary>
        /// Extracts a single file from Android APK using UnityWebRequest.
        /// Required because Android assets are compressed in APK.
        /// </summary>
        /// <param name="sourceRelative">Relative path in StreamingAssets</param>
        /// <param name="targetAbsolute">Absolute destination path</param>
        private IEnumerator ExtractAndroidFile(string sourceRelative, string targetAbsolute)
        {
            string sourcePath = Path.Combine(Application.streamingAssetsPath, sourceRelative);

            using (
                UnityEngine.Networking.UnityWebRequest www =
                    UnityEngine.Networking.UnityWebRequest.Get(sourcePath)
            )
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

        /// <summary>
        /// Ensures static files directory exists and creates a fallback index.html if needed.
        /// Used on non-Android platforms where StreamingAssets can be accessed directly.
        /// Copies fallback HTML from StreamingAssets if the UI directory doesn't exist.
        /// </summary>
        /// <param name="path">Path to static files directory</param>
        private void EnsureStaticFilesExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);

                // Copy fallback HTML page from StreamingAssets
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
                    else
                    {
                        Debug.LogWarning(
                            $"[WebServerManager] Fallback HTML not found at {fallbackSourcePath}"
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

        /// <summary>
        /// Displays connection information in Unity console.
        /// Shows server URL and configuration path for user reference.
        /// </summary>
        private void ShowConnectionInfo()
        {
            Debug.Log("╔═══════════════════════════════════════════════════════════╗");
            Debug.Log("║          QuestNav Configuration Server                    ║");
            Debug.Log("╠═══════════════════════════════════════════════════════════╣");
            Debug.Log($"║ Port: {serverPort}");
            Debug.Log($"║ Config Path: {store.GetConfigPath()}");
            Debug.Log("╠═══════════════════════════════════════════════════════════╣");
            Debug.Log($"║ Connect: http://<quest-ip>:{serverPort}/");
            Debug.Log("╚═══════════════════════════════════════════════════════════╝");
        }
        #endregion

        #region Private Methods - Application Restart
        /// <summary>
        /// Restarts the application. Called from background thread via ConfigServer callback.
        /// Sets flag that will be checked on main thread in Periodic().
        /// </summary>
        private void RestartApplication()
        {
            Debug.Log("[WebServerManager] Restart requested from web interface");
            restartRequested = true;
        }

        /// <summary>
        /// Requests pose reset to origin. Called from background thread via ConfigServer callback.
        /// Sets flag that will be checked on main thread in Periodic().
        /// Uses volatile flag for thread-safe communication between background and main threads.
        /// </summary>
        private void RequestPoseReset()
        {
            Debug.Log("[WebServerManager] Pose reset requested from web interface");
            poseResetRequested = true;
        }

        /// <summary>
        /// Restarts the application on Android/Quest by launching new instance then killing current process.
        /// On editor, just quits the application.
        /// </summary>
        private void RestartApp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (
                    AndroidJavaClass unityPlayer = new AndroidJavaClass(
                        "com.unity3d.player.UnityPlayer"
                    )
                )
                using (
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>(
                        "currentActivity"
                    )
                )
                using (AndroidJavaObject pm = activity.Call<AndroidJavaObject>("getPackageManager"))
                using (
                    AndroidJavaObject intent = pm.Call<AndroidJavaObject>(
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

                // Kill current process immediately after starting new instance
                using (AndroidJavaClass process = new AndroidJavaClass("android.os.Process"))
                {
                    int pid = process.CallStatic<int>("myPid");
                    process.CallStatic("killProcess", pid);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebServerManager] Failed to restart: {ex.Message}");
                // Fallback to normal quit
                Application.Quit();
            }
#else
            Application.Quit();
#endif
        }
        #endregion
    }
}
