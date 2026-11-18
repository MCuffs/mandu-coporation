using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// A WebGL test build of the throw prototype (2026-11-18, the owner: "shall we put it on Apps in Toss first?").
// Apps in Toss runs Unity games as WebGL inside the Toss app, so the first thing to learn is whether this game
// runs in a phone's web view at all: how large the download is, how long it takes to start, how much memory it
// holds and what frame rate a full street gets. Nothing of Apps in Toss itself (SDK, login, leaderboard, storage)
// is in this build.
//   Unity -batchmode -nographics -quit -projectPath <project> -buildTarget WebGL -executeMethod DadThrowWebBuild.Build
// The build is made with DAD_THROW_WEBTEST defined (DadThrowPrototype): the page's address may say
// ?stage=15&auto=1 to open a stage with a player in memory and throw by itself, and the frame rate is shown.
// The editor needs WebGL Build Support; on this project's own editor (2021.3.2f1) it may not be installed.
public static class DadThrowWebBuild
{
    public const string Output="Builds/ThrowWebGL";
    public static void Build()
    {
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))
            throw new InvalidOperationException("This editor has no WebGL Build Support installed.");
        WebGLCompressionFormat oldFormat=PlayerSettings.WebGL.compressionFormat;bool oldFallback=PlayerSettings.WebGL.decompressionFallback;
        try
        {
            // Gzip with the loader's own fallback: the files open from any plain file server, whatever headers it sends.
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{DadThrowBuild.ScenePath},locationPathName=Output,
                target=BuildTarget.WebGL,options=BuildOptions.None,extraScriptingDefines=new[]{"DAD_THROW_WEBTEST"}});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("WebGL build "+report.summary.result+": "+report.summary.totalErrors+" errors");
            // The page keeps the running game where a test can reach it (frame rate and memory are read from it).
            string page=Path.Combine(Output,"index.html");
            if(File.Exists(page))
            {
                string html=File.ReadAllText(page);const string hook=".then((unityInstance) => {";
                if(html.Contains(hook))File.WriteAllText(page,html.Replace(hook,hook+" window.unityInstance = unityInstance;"));
                else Debug.LogWarning("DadThrowWebBuild: the page has no place to keep the running game; it was left as built.");
            }
            long bytes=0;foreach(string file in Directory.GetFiles(Output,"*",SearchOption.AllDirectories))bytes+=new FileInfo(file).Length;
            Debug.Log("DadThrowWebBuild: "+Output+" | "+(bytes/1024f/1024f).ToString("0.0")+" MB on disk | built in "+report.summary.totalTime);
        }
        finally{PlayerSettings.WebGL.compressionFormat=oldFormat;PlayerSettings.WebGL.decompressionFallback=oldFallback;}
    }
}
