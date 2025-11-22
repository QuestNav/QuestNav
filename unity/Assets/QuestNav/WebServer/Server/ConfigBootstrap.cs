using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace QuestNav.WebServer
{
    /// <summary>
    /// Bootstraps the QuestNav configuration system on app startup.
    /// Initializes reflection binding, loads saved config, starts HTTP server, and extracts web UI files.
    /// Runs on Unity main thread using coroutines for proper thread safety.
    /// Handles server lifecycle management (start, stop, restart) and Android APK file extraction.
    /// Provides web interface access at http://quest-ip:18080/ (default port).
    /// </summary>
    public class ConfigBootstrap : MonoBehaviour
    {
        #region Serialized Fields
        /// <summary>
        /// HTTP server port for configuration UI
        /// </summary>
        [Header("Server Settings")]
        [SerializeField]
        private int serverPort = 18080;

        /// <summary>
        /// Enable CORS for localhost development
        /// </summary>
        [SerializeField]
        private bool enableCORSDevMode = false;

        /// <summary>
        /// Whether to start the server on Awake
        /// </summary>
        [SerializeField]
        private bool startOnAwake = true;
        #endregion

        #region Private Fields
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
        /// Flag indicating if initialization is complete
        /// </summary>
        private bool isInitialized = false;

        /// <summary>
        /// Flag for app restart request from background thread
        /// </summary>
        private bool restartRequested = false;

        /// <summary>
        /// Pose reset provider component
        /// </summary>
        private PoseResetProvider poseResetProvider;
        #endregion

        #region Properties
        /// <summary>
        /// Gets whether the server is currently running
        /// </summary>
        public bool IsServerRunning => server != null && server.IsRunning;
        #endregion

        #region Unity Lifecycle Methods
        /// <summary>
        /// Initializes configuration system on app startup.
        /// Reads tunables, starts initialization coroutine if configured.
        /// </summary>
        private void Awake()
        {
            // Load server settings from Tunables if available
            if (typeof(Tunables).GetField("serverPort") != null)
            {
                serverPort = Tunables.serverPort;
                enableCORSDevMode = Tunables.enableCORSDevMode;
            }

            if (startOnAwake)
            {
                StartCoroutine(InitializeCoroutine());
            }
        }

        /// <summary>
        /// Checks for restart request flag on main thread.
        /// This flag is set from background thread via ConfigServer callback.
        /// </summary>
        private void Update()
        {
            if (restartRequested)
            {
                restartRequested = false;
                Debug.Log("[ConfigBootstrap] Executing restart on main thread");
                RestartApp();
            }
        }

        /// <summary>
        /// Handles app pause/resume events. Stops server on pause, restarts on resume.
        /// Important for Quest headset sleep/wake cycles.
        /// </summary>
        /// <param name="pauseStatus">True if app is pausing, false if resuming</param>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopServer();
            }
            else if (isInitialized)
            {
                StartCoroutine(RestartServerCoroutine());
            }
        }

        /// <summary>
        /// Stops server on app quit to clean up resources.
        /// </summary>
        private void OnApplicationQuit()
        {
            StopServer();
        }

        /// <summary>
        /// Stops server on component destruction to clean up resources.
        /// </summary>
        private void OnDestroy()
        {
            StopServer();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initializes the configuration system: reflection binding, config store, and singletons.
        /// Loads saved configuration and applies values to tunables.
        /// Must run on main thread for Unity API access.
        /// </summary>
        private IEnumerator InitializeCoroutine()
        {
            if (isInitialized)
                yield break;

            Debug.Log("[ConfigBootstrap] Initializing configuration system...");

            // Initialize reflection binding to scan for [Config] attributes
            binding = new ReflectionBinding();
            if (binding == null)
            {
                Debug.LogError("[ConfigBootstrap] Failed to create ReflectionBinding");
                yield break;
            }
            Debug.Log($"[ConfigBootstrap] Found {binding.FieldCount} configurable fields");

            // Initialize configuration store for persistence
            store = new ConfigStore();
            if (store == null)
            {
                Debug.LogError("[ConfigBootstrap] Failed to create ConfigStore");
                yield break;
            }

            // Load saved configuration from disk
            var savedConfig = store.LoadConfig();
            if (savedConfig != null && savedConfig.values != null && savedConfig.values.Count > 0)
            {
                Debug.Log($"[ConfigBootstrap] Applying {savedConfig.values.Count} saved values");
                binding.ApplyValues(savedConfig.values);
            }

            // Initialize singletons on main thread before server starts
            // This ensures they're available when server starts on background thread
            var statusProvider = StatusProvider.Instance;
            var logCollector = LogCollector.Instance;

            // Initialize PoseResetProvider component
            poseResetProvider = gameObject.GetComponent<PoseResetProvider>();
            if (poseResetProvider == null)
            {
                poseResetProvider = gameObject.AddComponent<PoseResetProvider>();
                Debug.Log("[ConfigBootstrap] Added PoseResetProvider component");
            }

            isInitialized = true;
            Debug.Log("[ConfigBootstrap] Initialization complete");

            // Start HTTP server
            yield return StartServerCoroutine();
        }
        #endregion

        #region Server Lifecycle
        /// <summary>
        /// Starts the HTTP configuration server.
        /// Extracts web UI files on Android, then starts EmbedIO server.
        /// </summary>
        private IEnumerator StartServerCoroutine()
        {
            if (!isInitialized)
            {
                yield return InitializeCoroutine();
            }

            if (server != null && server.IsRunning)
            {
                Debug.LogWarning("[ConfigBootstrap] Server already running");
                yield break;
            }

            Debug.Log("[ConfigBootstrap] Starting configuration server...");

            // Get static files path
            string staticPath = GetStaticFilesPath();
            if (string.IsNullOrEmpty(staticPath))
            {
                Debug.LogError("[ConfigBootstrap] Failed to get static files path");
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
                poseResetProvider.RequestPoseReset
            );

            if (server == null)
            {
                Debug.LogError("[ConfigBootstrap] Failed to create ConfigServer");
                yield break;
            }

            // Start server on background thread
            server.Start();

            // Wait a frame to let server start
            yield return null;

            if (!server.IsRunning)
            {
                Debug.LogError("[ConfigBootstrap] Server did not start successfully");
                yield break;
            }

            ShowConnectionInfo();
            Debug.Log("[ConfigBootstrap] Server started successfully");
        }

        /// <summary>
        /// Restarts the HTTP server after a brief delay.
        /// Used when resuming from app pause.
        /// </summary>
        private IEnumerator RestartServerCoroutine()
        {
            Debug.Log("[ConfigBootstrap] Restarting server...");

            // Wait a moment for the old server to fully stop and release the port
            yield return new WaitForSeconds(0.5f);

            yield return StartServerCoroutine();
        }

        /// <summary>
        /// Stops the HTTP server and releases resources.
        /// </summary>
        private void StopServer()
        {
            if (server == null)
                return;

            Debug.Log("[ConfigBootstrap] Stopping configuration server...");
            server.Stop();
            Debug.Log("[ConfigBootstrap] Server stopped");
        }
        #endregion

        #region Static Files Management
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
            Debug.Log($"[ConfigBootstrap] Using persistent path for Android: {persistentPath}");
            return persistentPath;
#else
            string streamingPath = Path.Combine(Application.streamingAssetsPath, "ui");
            Debug.Log($"[ConfigBootstrap] Using StreamingAssets path: {streamingPath}");
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
                Debug.Log("[ConfigBootstrap] Clearing old UI files...");
                Directory.Delete(targetPath, true);
            }

            string indexPath = Path.Combine(targetPath, "index.html");
            string assetsDir = Path.Combine(targetPath, "assets");

            // Delete old fallback files if they exist
            if (File.Exists(indexPath))
            {
                File.Delete(indexPath);
            }

            Debug.Log("[ConfigBootstrap] Extracting UI files from APK to persistent storage...");

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

            Debug.Log($"[ConfigBootstrap] Extracting assets to: {assetsDir}");

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

            Debug.Log("[ConfigBootstrap] UI extraction complete");
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
                    Debug.Log($"[ConfigBootstrap] Extracted: {sourceRelative}");
                }
                else
                {
                    Debug.LogWarning(
                        $"[ConfigBootstrap] Failed to extract {sourceRelative}: {www.error}"
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
                            $"[ConfigBootstrap] Copied fallback HTML from {fallbackSourcePath}"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[ConfigBootstrap] Fallback HTML not found at {fallbackSourcePath}"
                        );
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ConfigBootstrap] Failed to copy fallback HTML: {ex.Message}");
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

        #region Application Restart
        /// <summary>
        /// Restarts the application. Called from background thread via ConfigServer callback.
        /// Sets flag that will be checked on main thread in Update().
        /// </summary>
        private void RestartApplication()
        {
            Debug.Log("[ConfigBootstrap] Restart requested from web interface");
            restartRequested = true;
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

                    Debug.Log("[ConfigBootstrap] New instance started, killing current process...");
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
                Debug.LogError($"[ConfigBootstrap] Failed to restart: {ex.Message}");
                // Fallback to normal quit
                UnityEngine.Application.Quit();
            }
#else
            UnityEngine.Application.Quit();
#endif
        }
        #endregion
    }
}
