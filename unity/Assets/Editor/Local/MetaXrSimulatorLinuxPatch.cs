using System;
using System.IO;
using UnityEditor;

namespace QuestNav.EditorTools
{
    /// <summary>
    /// Meta's XR SDK core package ships Installer.cs with `downloadedInstallerPath` declared only
    /// inside #if UNITY_EDITOR_WIN / #elif UNITY_EDITOR_OSX, with no #else branch. On Linux this is
    /// a compile error (CS0103) that blocks the whole project. Library/PackageCache is regenerated
    /// (and this fix wiped) whenever Library is deleted or the package is re-resolved, so this
    /// re-applies the patch on every editor load.
    /// </summary>
    [InitializeOnLoad]
    internal static class MetaXrSimulatorLinuxPatch
    {
        private const string RelativePathUnderPackage = "Editor/MetaXRSimulator/Installer.cs";

        private const string BuggyBlock =
            "#elif UNITY_EDITOR_OSX\n"
            + "            var downloadedInstallerPath =\n"
            + "                            Path.Combine(XRSimConstants.DownloadFolderPath, $\"meta_xr_simulator.dmg\");\n"
            + "#endif";

        private const string PatchedBlock =
            "#elif UNITY_EDITOR_OSX\n"
            + "            var downloadedInstallerPath =\n"
            + "                            Path.Combine(XRSimConstants.DownloadFolderPath, $\"meta_xr_simulator.dmg\");\n"
            + "#else\n"
            + "            string downloadedInstallerPath = null;\n"
            + "            onError?.Invoke(\"Meta XR Simulator is not supported on this platform.\");\n"
            + "            return false;\n"
            + "#endif";

        static MetaXrSimulatorLinuxPatch()
        {
            try
            {
                ApplyPatch();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning(
                    $"[MetaXrSimulatorLinuxPatch] Failed to patch Meta XR SDK core for Linux: {ex}"
                );
            }
        }

        private static void ApplyPatch()
        {
            var packageCacheDir = Path.Combine(
                Path.GetDirectoryName(UnityEngine.Application.dataPath),
                "Library",
                "PackageCache"
            );
            if (!Directory.Exists(packageCacheDir))
            {
                return;
            }

            var patchedAny = false;

            foreach (
                var packageDir in Directory.GetDirectories(
                    packageCacheDir,
                    "com.meta.xr.sdk.core@*"
                )
            )
            {
                var installerPath = Path.Combine(
                    packageDir,
                    RelativePathUnderPackage.Replace('/', Path.DirectorySeparatorChar)
                );
                if (!File.Exists(installerPath))
                {
                    continue;
                }

                var contents = File.ReadAllText(installerPath);
                if (!contents.Contains(BuggyBlock))
                {
                    continue;
                }

                File.WriteAllText(installerPath, contents.Replace(BuggyBlock, PatchedBlock));
                patchedAny = true;
                UnityEngine.Debug.Log(
                    $"[MetaXrSimulatorLinuxPatch] Patched {installerPath} for Linux compilation."
                );
            }

            if (patchedAny)
            {
                AssetDatabase.Refresh();
            }
        }
    }
}
