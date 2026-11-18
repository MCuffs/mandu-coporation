using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// WebGL test builds of the throw prototype (2026-11-18, the owner: "shall we put it on Apps in Toss first?").
// Apps in Toss runs Unity games as WebGL inside the Toss app, so the first thing to learn is whether this game
// runs in a phone's web view at all: how large the download is, how long it takes to start, how much memory it
// holds and what frame rate a full street gets. Nothing of Apps in Toss itself (SDK, login, leaderboard, storage)
// is in these builds.
//   Unity -batchmode -nographics -quit -projectPath <project> -buildTarget WebGL -executeMethod DadThrowWebBuild.Build
//   Unity ... -executeMethod DadThrowWebBuild.BuildLight
// Build takes the project as it is: every picture under a Resources folder goes in, used or not, at full size
// and uncompressed (96 MB to download, 407 MB of textures on 2026-11-18).
// BuildLight is what a phone can carry: for the length of the build it moves out of Resources everything the
// throw game does not load, and gives the pictures that stay a WebGL setting of their own: no larger than about
// twice the size they are drawn at, and ASTC-compressed. Run it in a copy of the project kept for builds
// (a separate worktree): the texture settings it writes stay in that copy's .meta files and are not for committing.
// Both are made with DAD_THROW_WEBTEST defined (DadThrowPrototype): the page's address may say ?stage=15&auto=1
// to open a stage with a player in memory and throw by itself, and the frame rate is shown.
// The editor needs WebGL Build Support; on this project's own editor (2021.3.2f1) it may not be installed.
public static class DadThrowWebBuild
{
    public const string Output="Builds/ThrowWebGL",LightOutput="Builds/ThrowWebGLLight";
    const string Parked="Assets/DadThrowParked",ParkedList="Library/DadThrowParked.txt";
    // The Resources folders, and what the throw game loads from them (a path is kept when it starts with one of
    // these): its own folders, the five people it draws, and the fonts.
    static readonly string[] Roots={"Assets/DadSchoolBoss/Resources","Assets/Alley/Resources"};
    static readonly string[] Keep={"Assets/DadSchoolBoss/Resources/DadThrow","Assets/DadSchoolBoss/Resources/DadV5/chars/gum","Assets/DadSchoolBoss/Resources/DadV5/chars/chair",
        "Assets/DadSchoolBoss/Resources/DadV5/chars/dodge","Assets/DadSchoolBoss/Resources/DadV5/chars/tray","Assets/DadSchoolBoss/Resources/DadV5/chars/boss1","Assets/Alley/Resources/Fonts"};

    public static void Build(){Supported();Make(Output);}
    public static void BuildLight()
    {
        Supported();Restore();
        try
        {
#if UNITY_2021_2_OR_NEWER
            EditorUserBuildSettings.webGLBuildSubtarget=WebGLTextureSubtarget.ASTC;
#endif
            Lighten("WebGL");
            Make(LightOutput);
        }
        finally{Restore();}
    }
    // What both light builds do before they build (the Android one is DadThrowAndroidBuild): everything the throw
    // game does not load leaves Resources, and the pictures that stay get the platform's own size and ASTC.
    // Restore() afterwards puts back what was moved; the texture settings stay in the build copy.
    internal static void Lighten(string platform)
    {
        if(!AssetDatabase.IsValidFolder(Parked))AssetDatabase.CreateFolder("Assets","DadThrowParked");
        int parked=0;foreach(string root in Roots)parked+=Park(root);
        AssetDatabase.Refresh();
        int squeezed=Squeeze(platform);
        Debug.Log("DadThrowWebBuild: parked "+parked+" folders and files the throw game does not load; gave "+squeezed+" pictures a "+platform+" size and ASTC");
    }
    static void Supported()
    {
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))
            throw new InvalidOperationException("This editor has no WebGL Build Support installed.");
    }
    static bool Kept(string path){foreach(string keep in Keep)if(path.StartsWith(keep))return true;return false;}
    // Moves everything under `folder` that is not kept out of Resources, going down into a folder only when
    // something kept lies inside it. Each move is written down at once, so a build that dies can still be undone.
    static int Park(string folder)
    {
        int moved=0;
        foreach(string entry in Directory.GetFileSystemEntries(folder))
        {
            string path=entry.Replace('\\','/');if(path.EndsWith(".meta")||Kept(path))continue;
            bool holds=false;foreach(string keep in Keep)holds|=keep.StartsWith(path+"/");
            if(holds&&Directory.Exists(path)){moved+=Park(path);continue;}
            string to=Parked+"/"+Guid.NewGuid().ToString("N").Substring(0,8)+"_"+Path.GetFileName(path);
            string error=AssetDatabase.MoveAsset(path,to);
            if(!string.IsNullOrEmpty(error))throw new InvalidOperationException("could not park "+path+": "+error);
            File.AppendAllText(ParkedList,path+"\t"+to+"\n");moved++;
        }
        return moved;
    }
    // Puts back whatever a light build moved out, this run's or an earlier one's that died.
    public static void Restore()
    {
        if(File.Exists(ParkedList))
        {
            foreach(string line in File.ReadAllLines(ParkedList))
            {
                string[] pair=line.Split('\t');if(pair.Length!=2||!File.Exists(pair[1])&&!Directory.Exists(pair[1]))continue;
                string error=AssetDatabase.MoveAsset(pair[1],pair[0]);
                if(!string.IsNullOrEmpty(error))Debug.LogError("DadThrowWebBuild: could not put "+pair[0]+" back: "+error);
            }
            File.Delete(ParkedList);
        }
        if(AssetDatabase.IsValidFolder(Parked)&&Directory.GetFileSystemEntries(Parked).Length==0)AssetDatabase.DeleteAsset(Parked);
        AssetDatabase.Refresh();
    }
    // The longest side a picture needs in a phone's web view: about twice the size it is drawn at on the 1280x720
    // canvas. Pictures stretched by their corners keep their own size, because the corners are counted in their pixels.
    static int Cap(string path)
    {
        string name=Path.GetFileNameWithoutExtension(path);
        if(name=="paper_note"||name=="rule_card_frame"||name=="rule_chip"||name=="entrance_balloon")return 2048;
        if(name.StartsWith("bg_")||name.EndsWith("_wide")||name=="title_keyart")return 2048;
        if(name.EndsWith("_entrance")||name.StartsWith("entrance_")||name=="fog_veil"||name=="dad_clear"||name=="dad_retry"||path.Contains("/DadThrowStoryV1/"))return 1024;
        if(path.Contains("/DadV5/chars/")||name.StartsWith("dad_"))return 512;
        return 256;
    }
    static int Squeeze(string platform)
    {
        int changed=0;
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",Roots))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null||!Kept(path))continue;
            TextureImporterPlatformSettings web=importer.GetPlatformTextureSettings(platform);int cap=Cap(path);
            if(web.overridden&&web.maxTextureSize==cap&&web.format==TextureImporterFormat.ASTC_4x4)continue;
            web.overridden=true;web.maxTextureSize=cap;web.format=TextureImporterFormat.ASTC_4x4;web.compressionQuality=50;
            importer.SetPlatformTextureSettings(web);importer.SaveAndReimport();changed++;
        }
        return changed;
    }
    static void Make(string output)
    {
        WebGLCompressionFormat oldFormat=PlayerSettings.WebGL.compressionFormat;bool oldFallback=PlayerSettings.WebGL.decompressionFallback;
        try
        {
            // Gzip with the loader's own fallback: the files open from any plain file server, whatever headers it sends.
            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;PlayerSettings.WebGL.decompressionFallback=true;
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{DadThrowBuild.ScenePath},locationPathName=output,
                target=BuildTarget.WebGL,options=BuildOptions.None,extraScriptingDefines=new[]{"DAD_THROW_WEBTEST"}});
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("WebGL build "+report.summary.result+": "+report.summary.totalErrors+" errors");
            // The page keeps the running game where a test can reach it (frame rate and memory are read from it).
            string page=Path.Combine(output,"index.html");
            if(File.Exists(page))
            {
                string html=File.ReadAllText(page);const string hook=".then((unityInstance) => {";
                if(html.Contains(hook))File.WriteAllText(page,html.Replace(hook,hook+" window.unityInstance = unityInstance;"));
                else Debug.LogWarning("DadThrowWebBuild: the page has no place to keep the running game; it was left as built.");
            }
            long bytes=0;foreach(string file in Directory.GetFiles(output,"*",SearchOption.AllDirectories))bytes+=new FileInfo(file).Length;
            Debug.Log("DadThrowWebBuild: "+output+" | "+(bytes/1024f/1024f).ToString("0.0")+" MB on disk | built in "+report.summary.totalTime);
        }
        finally{PlayerSettings.WebGL.compressionFormat=oldFormat;PlayerSettings.WebGL.decompressionFallback=oldFallback;}
    }
}
