using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace QuestNav.Config
{
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

        void OnApplicationQuit()
        {
            StopServer();
        }

        void OnDestroy()
        {
            StopServer();
        }

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
                logger
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
                "ui/assets/main-sKiS_H-Q.css",
                Path.Combine(assetsDir, "main-sKiS_H-Q.css")
            );
            yield return ExtractAndroidFile(
                "ui/assets/main-BJ3Kb4Nr.js",
                Path.Combine(assetsDir, "main-BJ3Kb4Nr.js")
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

        public bool IsServerRunning => m_server != null && m_server.IsRunning;
    }
}
