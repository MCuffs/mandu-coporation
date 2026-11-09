using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// Explicit background-only entry points. Generated test artifacts use the local debug key, never a production key.
public static class DadAndroidBuild
{
    public static void Configure()
    {
        if (!Application.unityVersion.StartsWith("6000.3.")) throw new InvalidOperationException("Use the isolated Unity 6.3 LTS upgrade worktree");
#if UNITY_6000_0_OR_NEWER
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.manducorporation.dadwar");
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
#else
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.manducorporation.dadwar");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
#endif
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        AssetDatabase.SaveAssets();
    }
    public static void ConfigureOnly()
    {
        try { Configure(); File.WriteAllText("Design/dad-android6-settings.txt", Settings()); EditorApplication.Exit(0); }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static string Settings()
    {
        return "Unity: " + Application.unityVersion + "\nApplication ID: " + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)
            + "\nBackend: " + PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)
            + "\nArchitectures: " + PlayerSettings.Android.targetArchitectures + "\nMinimum SDK: " + PlayerSettings.Android.minSdkVersion
            + "\nTarget SDK: " + PlayerSettings.Android.targetSdkVersion
            + "\nGraphics: " + string.Join(",", Array.ConvertAll(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android), item => item.ToString())) + "\n";
    }
    public static void TestApk() { Build(false); }
    public static void TestBundle() { Build(true); }
    static void Build(bool bundle)
    {
        int exit = 1;
        string reportPath = "Design/dad-android6-" + (bundle ? "bundle" : "apk") + "-build.txt";
        var report = new StringBuilder("Android test artifact build; not approved for store upload.\n");
        try
        {
            Configure(); report.Append(Settings());
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android) throw new InvalidOperationException("Launch Unity with -buildTarget Android");
            // A release credential must be supplied separately through the owner's secure signing process.
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = bundle;
            string folder = Path.GetFullPath("Builds/AndroidPlaytest"); Directory.CreateDirectory(folder);
            string output = Path.Combine(folder, bundle ? "DadWar-ARM64-test.aab" : "DadWar-ARM64-test.apk");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/DadSchoolBoss.unity" }, locationPathName = output,
                target = BuildTarget.Android, options = bundle ? BuildOptions.None : BuildOptions.Development
            });
            report.AppendLine("Result: " + result.summary.result);
            report.AppendLine("Errors: " + result.summary.totalErrors + "\nWarnings: " + result.summary.totalWarnings);
            report.AppendLine("Size: " + result.summary.totalSize + " bytes\nDuration: " + result.summary.totalTime);
            report.AppendLine("Artifact: " + output + "\nSigning: local DEBUG key, not the store upload key");
            exit = result.summary.result == BuildResult.Succeeded ? 0 : 1;
        }
        catch (Exception e) { report.AppendLine("FAIL " + e); Debug.LogException(e); }
        finally { File.WriteAllText(reportPath, report.ToString()); Debug.Log(report.ToString()); EditorApplication.Exit(exit); }
    }
}
