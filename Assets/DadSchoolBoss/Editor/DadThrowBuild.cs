using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Explicitly invoked builds only. No import hook, window activation or automatic Play Mode.
public static class DadThrowBuild
{
    public const string ScenePath="Assets/DadThrowDefense.unity";
    [MenuItem("Dad School Boss/Build Landscape Throw Prototype Android APK")]
    public static void Android()
    {
        string output=Path.GetFullPath("Builds/ThrowDefense/DadThrow-landscape-test.apk");
        Build(output,BuildTarget.Android);
    }
    public static void Build(string output,BuildTarget target)
    {
        if(!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target),target))
            throw new InvalidOperationException("Required build module is not installed: "+target);
        UIOrientation previous=PlayerSettings.defaultInterfaceOrientation;
        string previousId=PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
        try
        {
            // Separate app identity protects Dad War's installed test app and its save data.
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            if(target==BuildTarget.Android)PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,"com.manducorporation.dadthrow.prototype");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=output,target=target,options=BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Throw prototype build failed: "+result.summary.result);
            Debug.Log("THROW_PROTOTYPE_BUILD: "+output);
        }
        finally
        {
            PlayerSettings.defaultInterfaceOrientation=previous;
            if(target==BuildTarget.Android)PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,previousId);
        }
    }
}
