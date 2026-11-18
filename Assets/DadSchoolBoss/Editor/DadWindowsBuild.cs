using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class DadWindowsBuild
{
    public static void Batch()
    {
        try
        {
            PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.bundleVersion="0.3.0";
            string output=Path.GetFullPath("Builds/DadWar-v0.3.0-Windows/DadWar.exe");Directory.CreateDirectory(Path.GetDirectoryName(output));
            var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/DadSchoolBoss.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            File.WriteAllText("Design/dad-windows-v03-build.txt","Unity: "+Application.unityVersion+"\nResult: "+r.summary.result+"\nErrors: "+r.summary.totalErrors+"\nWarnings: "+r.summary.totalWarnings+"\nSize: "+r.summary.totalSize+"\nTime: "+r.summary.totalTime+"\nArtifact: "+output+"\nSecurity patch and native smoke required before packaging.\n");
            EditorApplication.Exit(r.summary.result==BuildResult.Succeeded?0:1);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
