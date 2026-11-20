using System;
using System.IO;
using System.Text.RegularExpressions;
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

        /// <summary>
        /// Error text for a name rejected by <see cref="TryResolveFieldLayoutPath"/>.
        /// </summary>
        public const string INVALID_FIELD_LAYOUT_NAME_MESSAGE =
            "Invalid layout name. Use only letters, numbers, '.', '_' and '-' (max 64 characters before '.json').";

        private static readonly Regex FieldLayoutNamePattern = new Regex(
            @"^[A-Za-z0-9_-][A-Za-z0-9._-]{0,63}\.json$",
            RegexOptions.CultureInvariant
        );

        /// <summary>
        /// Resolves a field-layout file name to a full path directly inside
        /// <paramref name="directory"/>. Accepts only a plain "name.json" file name: no
        /// directory separators, no "..", no leading dot, at most 64 characters before
        /// ".json". Every layout name that comes from a request or from config goes
        /// through this.
        /// </summary>
        /// <returns>False if the name is not acceptable or would resolve outside the directory.</returns>
        public static bool TryResolveFieldLayoutPath(
            string directory,
            string fileName,
            out string fullPath
        )
        {
            fullPath = null;
            if (
                string.IsNullOrEmpty(directory)
                || string.IsNullOrEmpty(fileName)
                || !FieldLayoutNamePattern.IsMatch(fileName)
                || fileName.Contains("..")
                || Path.GetFileName(fileName) != fileName
            )
            {
                return false;
            }

            string root = Path.GetFullPath(directory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string candidate = Path.GetFullPath(Path.Combine(root, fileName));
            if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                return false;
            }

            fullPath = candidate;
            return true;
        }

        /// <summary>
        /// <see cref="TryResolveFieldLayoutPath"/> against <see cref="GetCustomFieldLayoutDir"/>.
        /// </summary>
        public static bool TryResolveCustomFieldLayoutPath(string fileName, out string fullPath)
        {
            return TryResolveFieldLayoutPath(GetCustomFieldLayoutDir(), fileName, out fullPath);
        }
    }
}
