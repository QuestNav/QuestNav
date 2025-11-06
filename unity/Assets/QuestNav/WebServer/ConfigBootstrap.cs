using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace QuestNav.Config
{
    /// <summary>
    /// Bootstraps the QuestNav configuration system on app startup.
    /// Initializes reflection binding, loads saved config, starts HTTP server, and extracts web UI files.
    /// Runs on Unity main thread using coroutines for proper thread safety.
    /// </summary>
    public class ConfigBootstrap : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField]
        private int m_serverPort = 18080;

        [SerializeField]
        private bool m_enableCORSDevMode = false;

        [SerializeField]
        private bool m_startOnAwake = true;

        private ConfigServer m_server;
        private ReflectionBinding m_binding;
        private ConfigStore m_store;
        private bool m_isInitialized = false;
        private bool m_restartRequested = false;
        private bool m_poseResetRequested = false;

        /// <summary>
        /// Initializes configuration system on app startup.
        /// Reads tunables, starts initialization coroutine if configured.
        /// </summary>
        void Awake()
        {
            if (typeof(Tunables).GetField("serverPort") != null)
            {
                m_serverPort = Tunables.serverPort;
                m_enableCORSDevMode = Tunables.enableCORSDevMode;
            }

            if (m_startOnAwake)
            {
                StartCoroutine(InitializeCoroutine());
            }
        }

        /// <summary>
        /// Checks for restart and pose reset request flags on main thread.
        /// </summary>
        void Update()
        {
            if (m_restartRequested)
            {
                m_restartRequested = false;
                Debug.Log("[ConfigBootstrap] Executing restart on main thread");
                RestartApp();
            }

            if (m_poseResetRequested)
            {
                m_poseResetRequested = false;
                ExecutePoseReset();
            }
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

        /// <summary>
        /// Handles app pause/resume events. Stops server on pause, restarts on resume.
        /// </summary>
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                StopServer();
            }
            else if (m_isInitialized)
            {
                StartCoroutine(RestartServerCoroutine());
            }
        }

        /// <summary>
        /// Stops server on app quit.
        /// </summary>
        void OnApplicationQuit()
        {
            StopServer();
        }

        /// <summary>
        /// Stops server on component destruction.
        /// </summary>
        void OnDestroy()
        {
            StopServer();
        }

        /// <summary>
        /// Initializes the configuration system: reflection binding, config store, and singletons.
        /// Loads saved configuration and applies values to tunables.
        /// Must run on main thread for Unity API access.
        /// </summary>
        private IEnumerator InitializeCoroutine()
        {
            if (m_isInitialized)
                yield break;

            Debug.Log("[ConfigBootstrap] Initializing configuration system...");

            // Initialize binding
            m_binding = new ReflectionBinding();
            if (m_binding == null)
            {
                Debug.LogError("[ConfigBootstrap] Failed to create ReflectionBinding");
                yield break;
            }
            Debug.Log($"[ConfigBootstrap] Found {m_binding.FieldCount} configurable fields");

            // Initialize store
            m_store = new ConfigStore();
            if (m_store == null)
            {
                Debug.LogError("[ConfigBootstrap] Failed to create ConfigStore");
                yield break;
            }

            // No authentication required - skip token generation

            // Load saved configuration
            var savedConfig = m_store.LoadConfig();
            if (savedConfig != null && savedConfig.values != null && savedConfig.values.Count > 0)
            {
                Debug.Log($"[ConfigBootstrap] Applying {savedConfig.values.Count} saved values");
                m_binding.ApplyValues(savedConfig.values);
            }

            // Initialize singletons on main thread before server starts
            var statusProvider = StatusProvider.Instance;
            var logCollector = LogCollector.Instance;

            m_isInitialized = true;
            Debug.Log("[ConfigBootstrap] Initialization complete");

            yield return StartServerCoroutine();
        }

        private IEnumerator StartServerCoroutine()
        {
            if (!m_isInitialized)
            {
                yield return InitializeCoroutine();
            }

            if (m_server != null && m_server.IsRunning)
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
            EnsureStaticFilesExist(staticPath);
#endif

            // Create server with logger interface
            var logger = new UnityLogger();
            m_server = new ConfigServer(
                m_binding,
                m_store,
                null, // No auth token needed
                m_serverPort,
                m_enableCORSDevMode,
                staticPath,
                logger,
                RestartApplication,
                RequestPoseReset
            );

            if (m_server == null)
            {
                Debug.LogError("[ConfigBootstrap] Failed to create SimpleHttpServer");
                yield break;
            }

            // Start server on background thread
            m_server.Start();

            // Wait a frame to let server start
            yield return null;

            if (!m_server.IsRunning)
            {
                Debug.LogError("[ConfigBootstrap] Server did not start successfully");
                yield break;
            }

            ShowConnectionInfo();
            Debug.Log("[ConfigBootstrap] Server started successfully");
        }

        private IEnumerator RestartServerCoroutine()
        {
            Debug.Log("[ConfigBootstrap] Restarting server...");
            
            // Wait a moment for the old server to fully stop and release the port
            yield return new WaitForSeconds(0.5f);
            
            yield return StartServerCoroutine();
        }

        private void StopServer()
        {
            if (m_server == null)
                return;

            Debug.Log("[ConfigBootstrap] Stopping configuration server...");
            m_server.Stop();
            Debug.Log("[ConfigBootstrap] Server stopped");
        }

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

            // Extract known Vite output files (check build output for actual names)
            yield return ExtractAndroidFile(
                "ui/assets/main-BF6XdPsA.css",
                Path.Combine(assetsDir, "main-BF6XdPsA.css")
            );
            yield return ExtractAndroidFile(
                "ui/assets/main-Dxpm8-xV.js",
                Path.Combine(assetsDir, "main-Dxpm8-xV.js")
            );

            Debug.Log("[ConfigBootstrap] UI extraction complete");
        }

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

        private void EnsureStaticFilesExist(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);

                string defaultHtml =
                    @"<!DOCTYPE html>
<html>
<head>
    <title>Config UI</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; background: #1a1a1a; color: #fff; }
        h1 { color: #00d4ff; }
        .info { background: #2a2a2a; padding: 20px; border-radius: 5px; border: 1px solid #444; }
    </style>
</head>
<body>
    <h1>QuestNav Configuration</h1>
    <div class='info'>
        <p><strong>Server is running!</strong></p>
        <p>The Vue UI has not been built yet.</p>
        <p>Build the Vue UI: <code>cd ui && pnpm build</code></p>
    </div>
</body>
</html>";
                File.WriteAllText(Path.Combine(path, "index.html"), defaultHtml);
            }
        }

        private void ShowConnectionInfo()
        {
            Debug.Log("╔═══════════════════════════════════════════════════════════╗");
            Debug.Log("║          QuestNav Configuration Server                    ║");
            Debug.Log("╠═══════════════════════════════════════════════════════════╣");
            Debug.Log($"║ Port: {m_serverPort}");
            Debug.Log($"║ Config Path: {m_store.GetConfigPath()}");
            Debug.Log("╠═══════════════════════════════════════════════════════════╣");
            Debug.Log($"║ Connect: http://<quest-ip>:{m_serverPort}/");
            Debug.Log("║ No authentication required - open access");
            Debug.Log("╚═══════════════════════════════════════════════════════════╝");
        }

        /// <summary>
        /// Restarts the application. Called from background thread via ConfigServer callback.
        /// Sets flag that will be checked on main thread in Update().
        /// </summary>
        private void RestartApplication()
        {
            Debug.Log("[ConfigBootstrap] Restart requested from web interface");
            m_restartRequested = true;
        }

        /// <summary>
        /// Requests pose reset. Called from background thread via ConfigServer.
        /// </summary>
        private void RequestPoseReset()
        {
            m_poseResetRequested = true;
        }

        /// <summary>
        /// Executes pose reset to origin by calling PoseResetCommand directly via reflection.
        /// Uses reflection to avoid circular assembly dependencies between Config and QuestNav.
        /// </summary>
        private void ExecutePoseReset()
        {
            Debug.Log("[ConfigBootstrap] Executing pose reset to origin");

            // Find QuestNav instance
            var questNavType = Type.GetType("QuestNav.Core.QuestNav, QuestNav");
            if (questNavType == null)
            {
                Debug.LogError("[ConfigBootstrap] QuestNav.Core.QuestNav type not found");
                return;
            }

            var questNav = FindFirstObjectByType(questNavType);
            if (questNav != null)
            {
                // Get VR camera and camera root via reflection
                var vrCameraField = questNavType.GetField(
                    "vrCamera",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );
                var vrCameraRootField = questNavType.GetField(
                    "vrCameraRoot",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (vrCameraField != null && vrCameraRootField != null)
                {
                    Transform vrCamera = vrCameraField.GetValue(questNav) as Transform;
                    Transform vrCameraRoot = vrCameraRootField.GetValue(questNav) as Transform;

                    if (vrCamera != null && vrCameraRoot != null)
                    {
                        // Recenter tracking using the same algorithm as PoseResetCommand
                        // Target: position (0,0,0) with identity rotation

                        Vector3 targetPosition = Vector3.zero;
                        Quaternion targetRotation = Quaternion.identity;

                        // Calculate rotation difference between current camera and target
                        Quaternion newRotation =
                            targetRotation * Quaternion.Inverse(vrCamera.localRotation);

                        // Apply rotation to root
                        vrCameraRoot.rotation = newRotation;

                        // Recalculate position after rotation
                        Vector3 newRootPosition =
                            targetPosition - (newRotation * vrCamera.localPosition);

                        // Apply position to root
                        vrCameraRoot.position = newRootPosition;

                        Debug.Log("[ConfigBootstrap] Tracking recentered to origin");
                    }
                }
            }
        }

        public bool IsServerRunning => m_server != null && m_server.IsRunning;
    }
}
