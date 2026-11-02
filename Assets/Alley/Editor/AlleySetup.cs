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
        StringBuilder report=new StringBuilder("Second Fist v7 - stages, arcade, economy and motion checks\n");
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
        Expect(StageCatalog.All.Length==4&&new AlleyCombat(3).targetKills==24,"four stages have increasing wave targets",report);
        var school=new AlleyCombat(0);var market=new AlleyCombat(1);
        Expect(school.AddEnemy(Vector2.zero).appearance!=market.AddEnemy(Vector2.zero).appearance,"school and market use different enemy archetypes",report);
        var save=new ArcadeProfile();save.highestStage=3;save.Normalize();Expect(JsonUtility.FromJson<ArcadeProfile>(JsonUtility.ToJson(save)).highestStage==3,"stage unlock survives profile persistence",report);
        Expect(StageCatalog.Get(99)==StageCatalog.Get(3),"stage lookup safely bounds invalid indices",report);
        var bosses=new System.Collections.Generic.HashSet<int>();for(int index=0;index<4;index++)bosses.Add(new AlleyCombat(index).AddEnemy(Vector2.zero,2).appearance);
        Expect(bosses.Count==4,"each stage has a distinct boss silhouette",report);
        c=Empty();c.spawning=true;c.kills=c.targetKills;c.spawnClock=0;c.Tick(.02f,Vector2.zero,false);Expect(c.bossSpawned&&c.enemies.Exists(e=>e.boss),"wave completion spawns the stage boss",report);
        CheckNaturalMotion(report);
        CheckPrologue(report);
        var profile=new ArcadeProfile();
        Expect(!profile.Unlock(1)&&profile.coins==0,"insufficient funds cannot unlock a style",report);
        Expect(profile.Settle("a",1000,120)&&!profile.Settle("a",1000,120)&&profile.coins==120&&profile.runs==1,"run settlement credits rewards exactly once",report);
        Expect(profile.Unlock(1)&&profile.coins==0&&profile.selectedStyle==1,"unlock consumes coins and equips the style",report);
        Expect(!profile.Unlock(1)&&!profile.Equip(2),"owned styles cannot be charged again and locked styles cannot equip",report);
        profile.coins=-5;profile.selectedStyle=99;profile.Normalize();Expect(profile.coins==0&&profile.selectedStyle==0,"invalid saved values are normalized",report);
        var restored=JsonUtility.FromJson<ArcadeProfile>(JsonUtility.ToJson(profile));Expect(restored.Owns(1)&&restored.bestScore==1000,"profile survives JSON persistence",report);
        c=Empty();Expect(c.targetKills==12&&c.AddEnemy(c.player,2).hp==18,"opening round uses a shorter enemy wave and boss",report);
        c.kills=4;c.wallHits=3;c.chainHits=2;Expect(AlleyGame.ArcadeReward(c)==24&&AlleyGame.ArcadeScore(c)==665,"combat achievements determine score and coins",report);
        Directory.CreateDirectory("Design");File.WriteAllText("Design/gameplay-checks.txt",report.ToString());Debug.Log("ALLEY_CHECKS_PASSED\n"+report);
    }


    static void CheckNaturalMotion(StringBuilder report){
        var slow=Empty();var fast=Empty();Rect wide=new Rect(0,0,2000,1200);slow.SetArena(wide);fast.SetArena(wide);
        for(int i=0;i<40;i++)slow.Tick(.05f,Vector2.right,false);for(int i=0;i<240;i++)fast.Tick(1/120f,Vector2.right,false);
        Expect((slow.player-fast.player).magnitude<.1f,"20 FPS and 120 FPS produce the same travel distance",report);
        var c=Empty();c.AddEnemy(c.player-new Vector2(90,0));c.Tick(.02f,Vector2.right,false);
        Expect(c.attackLead<=0&&c.hits==0&&c.moveVelocity.x>0,"retreat does not trigger an automatic backwards punch",report);
        c=Empty();c.AddEnemy(c.player+new Vector2(70,0));c.Tick(.02f,Vector2.zero,false);c.Tick(.02f,Vector2.left,false);
        Expect(c.attackLead<=0&&c.hits==0,"retreat input cancels attack anticipation",report);
        c=Empty();c.AddEnemy(c.player+new Vector2(70,0));c.Tick(.02f,Vector2.zero,true);
        Expect(c.attackLead<=0&&c.attackCooldown>.25f&&c.attackAnimation<=.32f,"ultimate is not immediately overwritten by an auto attack",report);
        c=Empty();c.SetArena(wide);var target=c.AddEnemy(c.player+new Vector2(70,0));c.Attack(target);
        Expect(target.lift==0,"knockback starts on the ground without a vertical pop",report);
        c.Tick(.07f,Vector2.zero,false);Expect(target.lift>0,"knockback rises along a timed airborne arc",report);
        for(int i=0;i<35;i++)c.Tick(.02f,Vector2.zero,false);
        Expect(target.lift==0,"knockback returns to the ground before walking resumes",report);
        c=Empty();c.attackCooldown=100;target=c.AddEnemy(c.player+new Vector2(60,0));target.windup=.001f;Vector2 before=c.player;c.Tick(.008f,Vector2.zero,false);
        Expect((c.player-before).magnitude<.1f&&c.playerImpulse.sqrMagnitude>0,"player recoil applies an impulse instead of teleporting",report);
        c=Empty();c.player=new Vector2(410,700);var a=c.AddEnemy(new Vector2(200,450));var b=c.AddEnemy(new Vector2(202,450));
        for(int i=0;i<55;i++)c.Tick(.02f,Vector2.zero,false);
        Expect((a.pos-b.pos).magnitude>32,"steering and soft separation keep approaching enemies apart",report);
        c=Empty();c.attackCooldown=100;target=c.AddEnemy(c.player+new Vector2(180,0),1);target.attack=0;
        bool recovered=false;for(int i=0;i<100;i++){c.Tick(.02f,Vector2.up,false);if(target.recovery>0)recovered=true;}
        Expect(recovered,"charge has a braking and recovery phase",report);

        c=Empty();c.attackCooldown=100;var first=c.AddEnemy(c.player+new Vector2(180,0),1);var second=c.AddEnemy(c.player-new Vector2(180,0),1);first.attack=second.attack=0;c.Tick(.02f,Vector2.zero,false);
        int charges=(first.windup>0||first.dash>0?1:0)+(second.windup>0||second.dash>0?1:0);
        Expect(charges==1,"bruisers take turns telegraphing a charge",report);
        c=Empty();c.player=new Vector2(410,700);first=c.AddEnemy(new Vector2(200,450),1);second=c.AddEnemy(new Vector2(202,450),1);first.attack=second.attack=100;
        for(int i=0;i<60;i++)c.Tick(.02f,Vector2.zero,false);
        Expect((first.pos-second.pos).magnitude>49,"large enemies reserve space appropriate to their bodies",report);

    }
    static void CheckPrologue(StringBuilder report){
        var timeline=new PrologueTimeline();for(int i=0;i<4;i++)timeline.Step(.1f);timeline.Step(0,true);
        Expect(timeline.index==0&&timeline.textComplete,"first input reveals dialogue before advancing",report);
        timeline.Step(.1f,true);Expect(timeline.index==1,"next input advances exactly one dialogue beat",report);
        timeline=new PrologueTimeline();timeline.SetMinimumDuration(0,8);
        for(int i=0;i<70;i++)timeline.Step(.1f);
        Expect(timeline.index==0,"dialogue duration respects longer voice recording",report);
        int transitions=0;timeline=new PrologueTimeline();for(int i=0;i<2000;i++)if(timeline.Step(.1f))transitions++;
        Expect(timeline.finished&&transitions==1,"prologue ends and hands over combat exactly once",report);
        timeline=new PrologueTimeline();Expect(timeline.Step(.1f,false,true)&&!timeline.Step(.1f,false,true),"skip completes prologue only once",report);
        Expect(PrologueTimeline.Beats[6].text=="야 가서 니네 부모 모셔와."&&PrologueTimeline.Beats[13].text=="나 팔룡회 정민수다!","key story dialogue preserved",report);
        var c=Empty();c.SetArena(new Rect(54,126,1600,734));for(int i=0;i<900;i++)c.Tick(.02f,Vector2.right,false);
        Expect(c.player.x<=c.arena.xMax-11&&c.player.x>1200,"arena and movement adapt to a wide fullscreen viewport",report);
    }
    [MenuItem("Second Fist/Build Windows")]
    public static void Build(){
        AlleyVisualPipeline.Prepare();RunChecks();PlayerSettings.productName="Second Fist - Arcade";PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;PlayerSettings.defaultIsNativeResolution=true;Directory.CreateDirectory("Build/Windows-v7-streets");
        BuildReport result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Build/Windows-v7-streets/SecondFist.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        File.WriteAllText("Design/build-result.txt",result.summary.result+"\nErrors: "+result.summary.totalErrors+"\nBytes: "+result.summary.totalSize);
        Directory.CreateDirectory("Build/Windows-v7-streets/Licenses");foreach(string license in Directory.GetFiles("Design/Licenses","*.txt"))File.Copy(license,"Build/Windows-v7-streets/Licenses/"+Path.GetFileName(license),true);
        if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed");Debug.Log("ALLEY_BUILD_SUCCEEDED");
    }
}
