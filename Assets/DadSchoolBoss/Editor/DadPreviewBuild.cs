using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public static class DadPreviewBuild
{
    const string Key="DadWar.BuildPreview";
    static DadPreviewBuild(){EditorApplication.update+=Poll;}
    public static void Queue(){SessionState.SetBool(Key,true);if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;}
    static void Poll()
    {
        if(!SessionState.GetBool(Key,false)||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        SessionState.SetBool(Key,false);Build();
    }
    static void Build()
    {
        string folder=Path.GetFullPath("Builds/DadWarPlaytest");Directory.CreateDirectory(folder);
        int width=PlayerSettings.defaultScreenWidth,height=PlayerSettings.defaultScreenHeight;FullScreenMode mode=PlayerSettings.fullScreenMode;
        try
        {
            PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/DadSchoolBoss.unity"},locationPathName=Path.Combine(folder,"DadWarPlaytest.app"),target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            string summary="macOS playtest build\nResult: "+report.summary.result+"\nErrors: "+report.summary.totalErrors+"\nWarnings: "+report.summary.totalWarnings+"\nSize: "+report.summary.totalSize+" bytes\nDuration: "+report.summary.totalTime+"\nApp: "+Path.Combine(folder,"DadWarPlaytest.app")+"\nAndroid: not built; Android module is not installed.\n";
            File.WriteAllText("Design/dad-macos-build-v18.txt",summary);Debug.Log("DAD_PREVIEW_BUILD: "+summary);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Preview build did not succeed");
        }
        catch(Exception e){Debug.LogError("DAD_PREVIEW_BUILD_FAILED: "+e);File.WriteAllText("Design/dad-macos-build-v18-failure.txt",e.ToString());}
        // Leave the editor stopped: completing a build must not expose a game window during screen sharing.
        finally{PlayerSettings.defaultScreenWidth=width;PlayerSettings.defaultScreenHeight=height;PlayerSettings.fullScreenMode=mode;}
    }
}
