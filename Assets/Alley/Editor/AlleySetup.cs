using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using SecondFistGame;
using System;
using System.IO;
using System.Text;

[InitializeOnLoad]
public static class AlleySetup {
    const string ScenePath="Assets/Alley/AlleyFight.unity";
    static AlleySetup(){EditorApplication.delayCall+=Prepare;}
    static void Prepare(){
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=Prepare;return;}
        if(!File.Exists("Assets/Alley/Resources/AlleyBackground.png")){EditorApplication.delayCall+=Prepare;return;}
        if(!SessionState.GetBool("AlleyChecksV2",false)){RunChecks();SessionState.SetBool("AlleyChecksV2",true);}
        if(File.Exists(ScenePath))return;
        ConfigureTexture("Assets/Alley/Resources/FighterAtlas.png",true);
        ConfigureTexture("Assets/Alley/Resources/AlleyBackground.png",false);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.05f,.075f);camera.orthographic=true;camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Alley Game").AddComponent<AlleyGame>();
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene("Assets/Scenes/FirstFight.unity",false)};
        PlayerSettings.productName="Second Fist - Alley";PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;PlayerSettings.resizableWindow=true;PlayerSettings.defaultIsNativeResolution=false;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        AssetDatabase.SaveAssets();Debug.Log("ALLEY_READY: new playable scene and assets saved");
    }
    static void ConfigureTexture(string path,bool alpha){
        TextureImporter t=AssetImporter.GetAtPath(path) as TextureImporter;if(t==null)return;
        t.textureType=TextureImporterType.Default;t.alphaIsTransparency=alpha;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;t.filterMode=FilterMode.Bilinear;t.maxTextureSize=2048;t.textureCompression=TextureImporterCompression.Uncompressed;t.SaveAndReimport();
    }
    [MenuItem("Second Fist/Open Alley Fight")]
    static void Open(){EditorSceneManager.OpenScene(ScenePath);}
    static void Expect(bool condition,string label,StringBuilder report){if(!condition)throw new Exception("ALLEY CHECK FAILED: "+label);report.AppendLine("PASS  "+label);}
    static AlleyCombat Empty(){var c=new AlleyCombat();c.spawning=false;c.Start();return c;}
    [MenuItem("Second Fist/Run Gameplay Checks")]
    public static void RunChecks(){
        StringBuilder report=new StringBuilder("Second Fist v3 - gameplay checks\n");
        var c=Empty();for(int i=0;i<200;i++)c.Tick(.02f,new Vector2(-1,-1),false);
        Expect(Mathf.Abs(c.player.x-(AlleyCombat.Left+12))<.1f&&Mathf.Abs(c.player.y-(AlleyCombat.Top+12))<.1f,"movement respects arena bounds",report);
        c=Empty();var target=c.AddEnemy(c.player+new Vector2(65,0));c.Tick(.02f,Vector2.zero,false);
        Expect(c.hits==0&&target.hp==3&&c.attackLead>0,"attack anticipation precedes damage",report);
        for(int i=0;i<8;i++)c.Tick(.02f,Vector2.zero,false);
        Expect(c.hits==1&&target.hp==2&&target.velocity.x>0,"automatic attack deals damage and knockback",report);
        c=Empty();target=c.AddEnemy(new Vector2(473,400));target.velocity=new Vector2(600,0);c.Tick(.02f,Vector2.zero,false);
        Expect(c.wallHits==1&&target.hp==2&&target.pos.x<=AlleyCombat.Right,"wall impact damages and contains enemy",report);
        c=Empty();c.player=new Vector2(410,700);var source=c.AddEnemy(new Vector2(200,450));target=c.AddEnemy(new Vector2(236,450));source.velocity=new Vector2(520,0);c.Tick(.02f,Vector2.zero,false);
        Expect(c.chainHits==1&&target.hp==2&&target.velocity.x>0,"moving enemy transfers impact to another enemy",report);
        c=Empty();target=c.AddEnemy(c.player+new Vector2(70,0));c.charge=0;Expect(!c.Special()&&target.hp==3,"special requires full charge",report);c.charge=100;
        Expect(c.Special()&&target.hp==1&&target.velocity.magnitude>800,"special damages and launches nearby targets",report);
        c=Empty();c.xp=3;c.Tick(.02f,Vector2.zero,false);float elapsed=c.elapsed;c.Tick(.03f,Vector2.one,true);
        Expect(c.state==RunState.Choosing&&c.elapsed==elapsed,"skill selection pauses simulation",report);c.Choose(1);
        Expect(c.state==RunState.Fighting&&c.chainSkill==1,"skill choice changes build and resumes combat",report);
        c=Empty();c.health=1;c.attackCooldown=10;target=c.AddEnemy(c.player+new Vector2(60,0));target.windup=.001f;c.Tick(.02f,Vector2.zero,false);
        Expect(c.state==RunState.Lost,"enemy telegraph resolves into defeat at zero health",report);
        c=Empty();target=c.AddEnemy(c.player+new Vector2(70,0),2);target.windup=.8f;c.Attack(target);
        Expect(target.windup>.7f&&target.velocity==Vector2.zero,"basic attack cannot permanently interrupt boss",report);
        c=Empty();target=c.AddEnemy(c.player+new Vector2(70,0),2);target.hp=1;for(int i=0;i<9;i++)c.Tick(.02f,Vector2.zero,false);
        Expect(c.state==RunState.Won,"boss defeat ends the run",report);
        c=Empty();c.attackCooldown=100;target=c.AddEnemy(c.player+new Vector2(180,0),1);target.attack=0;c.Tick(.02f,Vector2.zero,false);
        Expect(target.windup>0&&target.aim.x<-.9f,"bruiser telegraphs charge outside basic attack range",report);
        for(int i=0;i<75;i++)c.Tick(.02f,Vector2.zero,false);
        Expect(c.health<100,"standing still can be punished by a charge",report);
        c=Empty();c.attackCooldown=100;target=c.AddEnemy(c.player+new Vector2(180,0),1);target.attack=0;c.Tick(.02f,Vector2.zero,false);
        for(int i=0;i<75;i++)c.Tick(.02f,new Vector2(0,-1),false);
        Expect(c.health==100,"moving out of a telegraphed charge avoids damage",report);
        Directory.CreateDirectory("Design");File.WriteAllText("Design/gameplay-checks.txt",report.ToString());Debug.Log("ALLEY_CHECKS_PASSED\n"+report);
    }
    [MenuItem("Second Fist/Build Windows")]
    static void Build(){
        AlleyVisualPipeline.Prepare();RunChecks();PlayerSettings.productName="Second Fist - Motion Edition";Directory.CreateDirectory("Build/Windows-v3-polished");
        BuildReport result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Build/Windows-v3-polished/SecondFist.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText("Design/build-result.txt",result.summary.result+"\nErrors: "+result.summary.totalErrors+"\nBytes: "+result.summary.totalSize);
        Directory.CreateDirectory("Build/Windows-v3-polished/Licenses");foreach(string license in Directory.GetFiles("Design/Licenses","*.txt"))File.Copy(license,"Build/Windows-v3-polished/Licenses/"+Path.GetFileName(license),true);
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed");Debug.Log("ALLEY_BUILD_SUCCEEDED");
    }
}
