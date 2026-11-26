using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class QuestNavAndroidBuild
{
    public static void Run()
    {
        try
        {
            BuildApk();
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
            EditorApplication.Exit(1);
            return;
        }
        EditorApplication.Exit(0);
    }

    private static void BuildApk()
    {
        var scenes = EditorBuildSettings
            .scenes.Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new InvalidOperationException("QuestNav has no enabled build scenes.");

        PlayerSettings.bundleVersion = Environment.GetEnvironmentVariable("QUESTNAV_VERSION");
        PlayerSettings.Android.bundleVersionCode = int.Parse(
            Environment.GetEnvironmentVariable("QUESTNAV_VERSION_CODE")
        );
        PlayerSettings.Android.useCustomKeystore = false;
        EditorUserBuildSettings.buildAppBundle = false;

        var report = BuildPipeline.BuildPlayer(
            new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.Android,
                options =
                    Environment.GetEnvironmentVariable("QUESTNAV_BUILD_KIND") == "release"
                        ? BuildOptions.None
                        : BuildOptions.Development,
                locationPathName = Environment.GetEnvironmentVariable("QUESTNAV_APK_PATH"),
            }
        );
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException(
                $"APK build {report.summary.result} with {report.summary.totalErrors} errors."
            );
        Console.WriteLine($"APK build succeeded in {report.summary.totalTime}.");
    }
}
