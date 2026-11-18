using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// An Android build of the throw game in the shape Google Play asks of a new app (2026-11-18, the owner: "today
// let's get ready to put it on the Play Store and Apps in Toss"): 64-bit only, IL2CPP, target API 36, landscape,
// and light: like DadThrowWebBuild.BuildLight it leaves out everything the throw game does not load and gives
// the pictures that stay an Android size and ASTC. Without that the package is several hundred megabytes.
//   Unity -batchmode -nographics -quit -projectPath <copy> -buildTarget Android -executeMethod DadThrowAndroidBuild.TestApk
// Run it in the Unity 6 build copy, never in the project's own folder: it changes player settings and writes
// texture settings into .meta files, none of which is for committing.
// TestApk is signed with the local debug key and carries the prototype's own app id, so it installs beside
// anything else and cannot be uploaded to a store. The store build needs the owner's upload key and the app id
// the owner decides on; neither is in this file.
public static class DadThrowAndroidBuild
{
    public const string Output="Builds/ThrowAndroid/DadThrow-arm64-test.apk",TestId="com.manducorporation.dadthrow.prototype";
    const string ReportPath="Design/dad-throw-android-test-build.txt";
    public static void TestApk()
    {
        int exit=1;var report=new StringBuilder("Throw game Android test build; debug-signed, not for store upload.\n");
        try
        {
            if(!Application.unityVersion.StartsWith("6000."))throw new InvalidOperationException("Build in the Unity 6 copy: Play asks for target API 36 and 16 KB pages");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new InvalidOperationException("Launch Unity with -buildTarget Android");
            DadThrowWebBuild.Restore();
#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,TestId);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
#else
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,TestId);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
#endif
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)24;PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            // A release credential is the owner's to make and keep; this build never asks for one.
            PlayerSettings.Android.useCustomKeystore=false;EditorUserBuildSettings.buildAppBundle=false;
            report.AppendLine("Unity: "+Application.unityVersion+"\nVersion: "+PlayerSettings.bundleVersion+" ("+PlayerSettings.Android.bundleVersionCode+")\nApplication ID: "+TestId
                +"\nBackend: IL2CPP\nArchitectures: "+PlayerSettings.Android.targetArchitectures+"\nMinimum SDK: "+PlayerSettings.Android.minSdkVersion+"\nTarget SDK: "+PlayerSettings.Android.targetSdkVersion);
            report.AppendLine(Resolve());
            DadThrowWebBuild.Lighten("Android");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(Output)));
            BuildReport built=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{DadThrowBuild.ScenePath},locationPathName=Path.GetFullPath(Output),target=BuildTarget.Android,options=BuildOptions.None});
            long bytes=File.Exists(Output)?new FileInfo(Output).Length:0;
            report.AppendLine("Result: "+built.summary.result+"\nErrors: "+built.summary.totalErrors+"\nWarnings: "+built.summary.totalWarnings
                +"\nPackage: "+(bytes/1024f/1024f).ToString("0.0")+" MB\nDuration: "+built.summary.totalTime+"\nArtifact: "+Output+"\nSigning: local DEBUG key, not the store upload key");
            exit=built.summary.result==BuildResult.Succeeded?0:1;
        }
        catch(Exception error){report.AppendLine("FAIL "+error);Debug.LogException(error);}
        finally
        {
            try{DadThrowWebBuild.Restore();}catch(Exception error){report.AppendLine("RESTORE FAILED "+error);exit=1;}
            File.WriteAllText(ReportPath,report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(exit);
        }
    }
    // The Android libraries of the ads package come through the External Dependency Manager; they are resolved
    // here rather than by an editor window that a background build never opens.
    static string Resolve()
    {
        Type resolver=Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
        if(resolver==null)return "External Dependency Manager not found (no Android libraries to resolve)";
        var method=resolver.GetMethod("ResolveSync",new[]{typeof(bool)});
        if(method==null)return "PlayServicesResolver.ResolveSync not found";
        return "Android dependency resolution: "+method.Invoke(null,new object[]{true});
    }
}
