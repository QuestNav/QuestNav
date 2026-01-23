using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestNav.Utils
{
    public static class FileManager
    {
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
            string sourceDirAbsolute = Path.Combine(
                Application.streamingAssetsPath,
                sourceDirRelative
            );

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
            return Path.Combine(Application.persistentDataPath, subPath);
#else
            return Path.Combine(Application.streamingAssetsPath, subPath);
#endif
        }
    }
}
