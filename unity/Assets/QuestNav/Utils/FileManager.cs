using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestNav.Utils
{
    public static class FileManager
    {
        /// <summary>
        /// Cached <see cref="Application.persistentDataPath"/>, captured on the main thread
        /// by <see cref="Initialize"/>. The property throws when accessed off the main
        /// thread, but <see cref="GetStaticFilesPath"/> and <see cref="GetCustomFieldLayoutDir"/>
        /// are called from ConfigServer's background HTTP request handlers.
        /// </summary>
        private static string persistentDataPath;

        /// <summary>
        /// Cached <see cref="Application.streamingAssetsPath"/>, captured on the main thread
        /// by <see cref="Initialize"/> for the same reason as <see cref="persistentDataPath"/>.
        /// </summary>
        private static string streamingAssetsPath;

        /// <summary>
        /// Caches the Unity path properties this class needs. Must be called from the main
        /// thread (e.g. from a MonoBehaviour's Awake) before any background-thread caller
        /// (such as ConfigServer's HTTP request handlers) uses this class.
        /// </summary>
        public static void Initialize()
        {
            persistentDataPath = Application.persistentDataPath;
            streamingAssetsPath = Application.streamingAssetsPath;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Extracts files from the APK StreamingAssets path to another specified path
        /// </summary>
        /// <param name="fileName">The name of the file to copy</param>
        /// <param name="sourceDirRelative">Source path from the StreamingAssets root</param>
        /// <param name="targetDirAbsolute">Destination path relative to the whole project
        /// (should most likely be extracted into StreamingAssets)</param>
        public async static Task ExtractAndroidFileAsync(
            string fileName,
            string sourceDirRelative,
            string targetDirAbsolute
        )
        {
            string sourceDirAbsolute = Path.Combine(streamingAssetsPath, sourceDirRelative);

            string sourceFileAbsolute = Path.Combine(sourceDirAbsolute, fileName);
            string targetFileAbsolute = Path.Combine(targetDirAbsolute, fileName);

            if (!Directory.Exists(targetDirAbsolute))
            {
                Directory.CreateDirectory(targetDirAbsolute);
            }

            using var www = UnityEngine.Networking.UnityWebRequest.Get(sourceFileAbsolute);
            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                await File.WriteAllBytesAsync(targetFileAbsolute, www.downloadHandler.data);
                QueuedLogger.Log($"Extracted: {fileName}");
            }
            else
            {
                QueuedLogger.LogWarning($"Failed to extract {fileName}: {www.error}");
            }
        }
#endif

        /// <summary>
        /// Gets the path where static, non-unity files are stored (e.g. WebUI builds)
        /// </summary>
        /// <param name="subPath">The subpath to find (e.g. ui for ui builds)</param>
        /// <returns>The path to static file storage based on platform</returns>
        public static string GetStaticFilesPath(string subPath)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Path.Combine(persistentDataPath, subPath);
#else
            return Path.Combine(streamingAssetsPath, subPath);
#endif
        }

        /// <summary>
        /// Returns the directory where user-uploaded AprilTag field-layout JSONs live.
        /// Always under <see cref="Application.persistentDataPath"/> (writable on every
        /// platform, persisted across app updates). Creates the directory on first call.
        /// </summary>
        public static string GetCustomFieldLayoutDir()
        {
            string path = Path.Combine(persistentDataPath, "apriltag", "fieldlayouts-custom");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}
