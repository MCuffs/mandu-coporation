using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// Android builds of the throw game in the shape Google Play asks of a new app (2026-11-18, the owner: "today
// let's get ready to put it on the Play Store and Apps in Toss"): 64-bit only, IL2CPP, target API 36, landscape,
// and light: like DadThrowWebBuild.BuildLight they leave out everything the throw game does not load and give
// the pictures that stay an Android size and ASTC. Without that the package is several hundred megabytes.
//   Unity -batchmode -nographics -projectPath <copy> -buildTarget Android -executeMethod DadThrowAndroidBuild.TestApk
// Run them in the Unity 6 build copy, never in the project's own folder: they change player settings and write
// texture settings into .meta files, none of which is for committing.
//   TestApk      an APK to put on a phone: the local debug key and the prototype's own app id, so it installs
//                beside anything else and cannot be uploaded to a store.
//   TestBundle   the bundle Play takes, with the store's app id, still debug-signed: for checking the bundle
//                before the owner's key exists. Play will not accept it.
//   StoreBundle  the bundle that goes to Play, signed with the owner's upload key. The key is the owner's to make
//                and keep; this build is told where it is by four environment variables set in the terminal that
//                starts it, and writes none of them anywhere:
//                DAD_UPLOAD_KEYSTORE (path to the .jks), DAD_UPLOAD_KEYSTORE_PASS, DAD_UPLOAD_KEY_ALIAS, DAD_UPLOAD_KEY_PASS.
// Every build carries the project's own version unless the same terminal names another: DAD_VERSION_NAME (what a
// player sees, such as 1.0.0) and DAD_VERSION_CODE (a whole number; Play refuses a code an earlier upload used, so
// each upload counts one up). The build puts the project's own version back when it ends, so a later build that
// names none does not inherit this one's.
// StoreId was decided by the owner on 2026-11-18 ("yes, that one"). Play never lets an app change it after the
// first upload.
public static class DadThrowAndroidBuild
{
    public const string TestId="com.manducorporation.dadthrow.prototype",StoreId="com.bluemushroom.dadwar",Studio="Bluemushroom studio";
    public const string Output="Builds/ThrowAndroid/DadThrow-arm64-test.apk";
    public static void TestApk(){Run(false,TestId,false,Output,"Design/dad-throw-android-test-build.txt");}
    public static void TestBundle(){Run(true,StoreId,false,"Builds/ThrowAndroid/DadWar-test.aab","Design/dad-throw-android-test-bundle-build.txt");}
    // The store bundle is named after its version, which is only known once Version() has read the terminal.
    public static void StoreBundle(){Run(true,StoreId,true,null,"Design/dad-throw-android-store-bundle-build.txt");}
    static void Version()
    {
        string name=Environment.GetEnvironmentVariable("DAD_VERSION_NAME"),code=Environment.GetEnvironmentVariable("DAD_VERSION_CODE");
        if(!string.IsNullOrEmpty(name))PlayerSettings.bundleVersion=name.Trim();
        if(string.IsNullOrEmpty(code))return;
        int number;
        if(!int.TryParse(code.Trim(),out number)||number<1)throw new InvalidOperationException("DAD_VERSION_CODE must be a whole number from 1 up, not \""+code+"\"");
        PlayerSettings.Android.bundleVersionCode=number;
    }

    static void Run(bool bundle,string id,bool store,string output,string reportPath)
    {
        string versionWas=PlayerSettings.bundleVersion;int codeWas=PlayerSettings.Android.bundleVersionCode;
        int exit=1;var report=new StringBuilder(store?"Throw game Android store bundle; signed with the owner's upload key.\n":"Throw game Android test build; debug-signed, not for store upload.\n");
        try
        {
            if(!Application.unityVersion.StartsWith("6000."))throw new InvalidOperationException("Build in the Unity 6 copy: Play asks for target API 36 and 16 KB pages");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)throw new InvalidOperationException("Launch Unity with -buildTarget Android");
            Version();
            if(output==null)output="Builds/ThrowAndroid/DadWar-"+PlayerSettings.bundleVersion+"-"+PlayerSettings.Android.bundleVersionCode+".aab";
            string alias=null;
            if(store)
            {
                string keystore=Environment.GetEnvironmentVariable("DAD_UPLOAD_KEYSTORE"),storePass=Environment.GetEnvironmentVariable("DAD_UPLOAD_KEYSTORE_PASS"),keyPass=Environment.GetEnvironmentVariable("DAD_UPLOAD_KEY_PASS");
                alias=Environment.GetEnvironmentVariable("DAD_UPLOAD_KEY_ALIAS");
                if(string.IsNullOrEmpty(keystore)||!File.Exists(keystore)||string.IsNullOrEmpty(storePass)||string.IsNullOrEmpty(alias)||string.IsNullOrEmpty(keyPass))
                    throw new InvalidOperationException("The store bundle needs the owner's upload key: set DAD_UPLOAD_KEYSTORE (path to the .jks), DAD_UPLOAD_KEYSTORE_PASS, DAD_UPLOAD_KEY_ALIAS and DAD_UPLOAD_KEY_PASS in the terminal that starts the build");
                PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=keystore;PlayerSettings.Android.keystorePass=storePass;
                PlayerSettings.Android.keyaliasName=alias;PlayerSettings.Android.keyaliasPass=keyPass;
            }
            else PlayerSettings.Android.useCustomKeystore=false;
            DadThrowWebBuild.Restore();
#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,id);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
#else
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android,id);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,ScriptingImplementation.IL2CPP);
#endif
            if(id==StoreId)PlayerSettings.companyName=Studio;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=(AndroidSdkVersions)24;PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
            EditorUserBuildSettings.buildAppBundle=bundle;
            report.AppendLine("Unity: "+Application.unityVersion+"\nVersion: "+PlayerSettings.bundleVersion+" ("+PlayerSettings.Android.bundleVersionCode+")\nApplication ID: "+id
                +"\nBackend: IL2CPP\nArchitectures: "+PlayerSettings.Android.targetArchitectures+"\nMinimum SDK: "+PlayerSettings.Android.minSdkVersion+"\nTarget SDK: "+PlayerSettings.Android.targetSdkVersion);
            report.AppendLine(Resolve());
            DadThrowWebBuild.Lighten("Android");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            BuildReport built=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{DadThrowBuild.ScenePath},locationPathName=Path.GetFullPath(output),target=BuildTarget.Android,options=BuildOptions.None});
            long bytes=File.Exists(output)?new FileInfo(output).Length:0;
            report.AppendLine("Result: "+built.summary.result+"\nErrors: "+built.summary.totalErrors+"\nWarnings: "+built.summary.totalWarnings
                +"\nPackage: "+(bytes/1024f/1024f).ToString("0.0")+" MB\nDuration: "+built.summary.totalTime+"\nArtifact: "+output
                +"\nSigning: "+(store?"the owner's upload key, alias "+alias:"local DEBUG key, not the store upload key"));
            exit=built.summary.result==BuildResult.Succeeded?0:1;
        }
        catch(Exception error){report.AppendLine("FAIL "+error.Message);Debug.LogError("DadThrowAndroidBuild: "+error.Message);}
        finally
        {
            // Nothing about the key stays in the build copy's settings.
            PlayerSettings.Android.useCustomKeystore=false;PlayerSettings.Android.keystoreName="";PlayerSettings.Android.keystorePass="";
            PlayerSettings.Android.keyaliasName="";PlayerSettings.Android.keyaliasPass="";
            PlayerSettings.bundleVersion=versionWas;PlayerSettings.Android.bundleVersionCode=codeWas;
            try{DadThrowWebBuild.Restore();}catch(Exception error){report.AppendLine("RESTORE FAILED "+error.Message);exit=1;}
            File.WriteAllText(reportPath,report.ToString());Debug.Log(report.ToString());EditorApplication.Exit(exit);
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
