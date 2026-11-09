using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SecondFistGame;

[InitializeOnLoad]
public static class DadPrototypeSetup
{
    const string ScenePath = "Assets/DadSchoolBoss.unity";
    const string CommandPath = "Library/DadPrototypeCommand.txt";
    static DadPrototypeSetup() { EditorApplication.update += Poll; }
    static bool pump; // debug: keep Play Mode rendering while the editor window is in the background (for captures)
    static string delayedCapture; static int delayedCaptureFrame;
    // debug: a frame-timed script of setup actions and captures, used for the screen audit
    static readonly Queue<KeyValuePair<int, Action>> steps = new Queue<KeyValuePair<int, Action>>();
    static int nextStepFrame;
    static void Step(int waitFrames, Action action) { if (steps.Count == 0) nextStepFrame = Time.frameCount + waitFrames; steps.Enqueue(new KeyValuePair<int, Action>(waitFrames, action)); }
    static void SetMember(object target, string name, object value)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        FieldInfo field = target.GetType().GetField(name, flags);
        if (field != null) { field.SetValue(target, value); return; }
        target.GetType().GetProperty(name, flags).GetSetMethod(true).Invoke(target, new[] { value });
    }
    static void QueueCommercialAudit(string dir)
    {
        Directory.CreateDirectory(dir);var g=UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();var original=g.Progress;string temp=TempSavePath("commercial-v18");
        string[] keys={"DadV2.BestChain","DadV2.Wins","DadV2.TotalWallHits"};var saved=new Dictionary<string,int>();foreach(string key in keys)if(PlayerPrefs.HasKey(key))saved[key]=PlayerPrefs.GetInt(key);
        Action<string> shot=name=>ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"));
        Step(1,()=>{var p=new DadProgress(DadEconomy.Load(),temp);p.Save.coins=12450;p.Save.lunchboxes=3;p.Save.bestStage=12;p.Save.currentStage=8;p.Save.stars=new[]{3,3,2,3,1,2,1,2,2,1,1};p.Save.bestShots=new[]{3,3,4,4,6,6,8,8,8,9,10};for(int j=0;j<6;j++){p.Save.gearRarity[j]=j%3;p.Save.gearValue[j]=.04f;}g.UseProgress(p);g.GoHome();});Step(10,()=>shot("01-home"));
        Step(1,()=>g.ShowTitle());Step(6,()=>shot("00-title"));Step(1,()=>g.PlayStory(true));Step(6,()=>shot("00b-story"));Step(1,()=>g.GoHome());
        Step(1,()=>SetMember(g,"stagePicker",true));Step(6,()=>shot("01b-school-map"));Step(1,()=>SetMember(g,"stagePicker",false));
        Step(1,()=>g.OpenSettings());Step(6,()=>shot("02-settings"));Step(1,()=>SetMember(g,"settingsOpen",false));
        Step(1,()=>g.Tab=DadSchoolPrototype.HomeTab.Lunchbox);Step(6,()=>shot("03-equipment"));
        Step(1,()=>g.Tab=DadSchoolPrototype.HomeTab.Upgrade);Step(6,()=>shot("04-growth"));
        Step(1,()=>{g.StartRun(8);g.SlingView.DebugAim(new Vector2(270,660),new Vector2(190,778));});Step(10,()=>shot("05-aim"));
        Step(1,()=>g.SlingView.DebugRelease());Step(8,()=>shot("06-flight"));
        Step(1,()=>g.SlingView.SetPaused(true));Step(6,()=>shot("07-pause"));
        Step(1,()=>SetMember(g.SlingView,"pauseOptions",true));Step(6,()=>shot("08-battle-settings"));
        Step(1,()=>{g.SlingView.SetPaused(false);g.Sling.shots=3;g.FinishSlingBattleNow(true);});Step(8,()=>shot("09-result"));
        Step(1,()=>{g.StartRun(8);g.Sling.lossReason="발사 소진 · 2명 남음";g.FinishSlingBattleNow(false);});Step(8,()=>shot("10-retry"));
        Step(1,()=>{g.StartRun(15);SetMember(g.SlingView,"lessonKind",2);});Step(8,()=>shot("11-counter-lesson"));
        Step(1,()=>{SetMember(g,"settingsOpen",false);g.UseProgress(original);g.GoHome();foreach(string key in keys){if(saved.ContainsKey(key))PlayerPrefs.SetInt(key,saved[key]);else PlayerPrefs.DeleteKey(key);}PlayerPrefs.Save();g.ReloadProgress();DeleteTemp(temp);Debug.Log("DAD_COMMERCIAL_UI_DONE: "+dir);});
    }
    static void QueueOutfitMatrix(string dir)
    {
        Directory.CreateDirectory(dir);var game=UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();var view=game.SlingView;bool gameEnabled=game.enabled,viewEnabled=view!=null&&view.enabled;game.enabled=false;if(view!=null)view.enabled=false;var audit=new GameObject("Outfit visual audit").AddComponent<DadOutfitAuditView>();
        for(int i=0;i<7;i++){int page=i;Step(1,()=>audit.page=page);Step(6,()=>ScreenCapture.CaptureScreenshot(Path.Combine(dir,"matrix-"+page+".png"),2));}
        Step(2,()=>{UnityEngine.Object.Destroy(audit.gameObject);game.enabled=gameEnabled;if(view!=null)view.enabled=viewEnabled;Debug.Log("DAD_OUTFIT_MATRIX_DONE: "+dir);});
    }
    static void QueueOutfitAudit(string dir)
    {
        Directory.CreateDirectory(dir);var g=UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        DadProgress original=g.Progress;string temp=TempSavePath("outfit-audit");
        Action<string> shot=name=>ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"),2);
        Step(1,()=>{var p=new DadProgress(DadEconomy.Load(),temp);g.UseProgress(p);g.GoHome();g.Tab=DadSchoolPrototype.HomeTab.Lunchbox;SetMember(g,"toastTime",0f);});
        Step(8,()=>shot("00-unequipped"));
        Step(1,()=>{g.Progress.Save.gearRarity[3]=0;SetMember(g,"clock",0f);});Step(3,()=>shot("00-reading-idle0"));
        Step(1,()=>SetMember(g,"clock",.65f));Step(3,()=>shot("00-reading-idle1"));
        Step(1,()=>g.Progress.Save.gearRarity[3]=1);Step(3,()=>shot("00-sunglasses"));
        for(int rarity=0;rarity<3;rarity++)
        {
            int r=rarity;
            Step(1,()=>{for(int i=0;i<6;i++){g.Progress.Save.gearRarity[i]=r;g.Progress.Save.gearValue[i]=.04f;}g.Tab=DadSchoolPrototype.HomeTab.Lunchbox;});
            Step(10,()=>shot("01-outfit-"+r));
        }
        Step(1,()=>{for(int i=0;i<6;i++){g.Progress.Save.gearRarity[i]=i%3;}g.Tab=DadSchoolPrototype.HomeTab.Go;});Step(8,()=>shot("02-mixed-home"));
        Step(1,()=>{g.StartRun(1);g.Sling.foes.Clear();g.Sling.stage.spawns.Clear();g.Sling.Add(SlingshotSim.Kind.Tray,80,185).cooldown=999;});
        Step(8,()=>shot("03-battle-idle"));
        Step(1,()=>g.SlingView.DebugAim(new Vector2(270,620),new Vector2(170,700)));Step(8,()=>shot("04-pull"));
        Step(1,()=>g.SlingView.DebugRelease());Step(8,()=>shot("05-flight"));
        Step(1,()=>{g.UseProgress(original);DeleteTemp(temp);g.GoHome();Debug.Log("DAD_OUTFIT_SCREENS_DONE: "+dir);});
    }
    static void QueueDifficultyAudit(string dir)
    {
        Directory.CreateDirectory(dir);
        var g=UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        DadProgress original=g.Progress; string temp=TempSavePath("difficulty-audit");
        string[] keys={"DadV2.BestChain","DadV2.Wins","DadV2.TotalWallHits"};
        var saved=new Dictionary<string,int>(); foreach(string key in keys)if(PlayerPrefs.HasKey(key))saved[key]=PlayerPrefs.GetInt(key);
        Action<string> shot=name=>ScreenCapture.CaptureScreenshot(Path.Combine(dir,name+".png"),2);
        Step(1,()=>{var p=new DadProgress(DadEconomy.Load(),temp);p.Save.bestStage=11;p.Save.currentStage=8;p.Save.stars=new[]{3,3,2,2,2,3,2,2,1,1};g.UseProgress(p);g.GoHome();SetMember(g,"toastTime",0f);SetMember(g,"stagePicker",true);});
        Step(12,()=>shot("01-rematch-list"));
        Step(1,()=>{SetMember(g,"stagePicker",false);g.StartRematch(8,1);});Step(25,()=>shot("02-rematch-start"));
        Step(1,()=>g.SlingView.DebugAim(new Vector2(270,620),new Vector2(360,730)));Step(8,()=>shot("03-rematch-aim"));
        Step(1,()=>{g.Sling.shots=8;g.FinishSlingBattleNow(true);});Step(8,()=>shot("04-rematch-win"));
        Step(1,()=>{g.StartRematch(8,2);g.Sling.shots=g.Sling.stage.shotLimit;g.Sling.lossReason="발사 소진 · 3명 남음";g.FinishSlingBattleNow(false);});Step(8,()=>shot("05-rematch-loss"));
        Step(1,()=>{foreach(string key in keys){if(saved.ContainsKey(key))PlayerPrefs.SetInt(key,saved[key]);else PlayerPrefs.DeleteKey(key);}PlayerPrefs.Save();g.ReloadProgress();g.UseProgress(original);DeleteTemp(temp);SetMember(g,"stagePicker",false);g.GoHome();Debug.Log("DAD_DIFFICULTY_SCREENS_DONE: "+dir);});
    }
    static void QueueScreenAudit(string dir)
    {
        Directory.CreateDirectory(dir);
        var g = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        string[] keys = { "DadV2.BestChain", "DadV2.Wins", "DadV2.Gear", "DadV2.TotalWallHits" };
        var saved = new Dictionary<string, int>();
        foreach (string k in keys) if (PlayerPrefs.HasKey(k)) saved[k] = PlayerPrefs.GetInt(k);
        Action<string> shot = name => ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"), 2);
        DadProgress original = g.Progress; string temp = TempSavePath("audit");
        Step(1, () => g.UseProgress(new DadProgress(DadEconomy.Load(), temp)));
        Step(2, () => g.GoHome()); Step(20, () => shot("01-home"));
        Step(1, () => g.Tab = DadSchoolPrototype.HomeTab.Upgrade); Step(8, () => shot("02-gear")); Step(1, () => g.Tab = DadSchoolPrototype.HomeTab.Go);
        Step(1, () => g.StartRun()); Step(80, () => shot("03-battle-start"));
        Step(1, () => SetMember(g, "stick", new Vector2(.75f, -.35f)));
        for (int i = 0; i < 8; i++) { int n = i; Step(5, () => shot("04-motion-" + n)); }
        Step(1, () => SetMember(g, "stick", Vector2.zero));
        Step(1, () => g.Combat.xp = g.Combat.nextXp); Step(10, () => shot("05-choice")); Step(1, () => g.Combat.Choose(0));
        Step(5, () => SetMember(g, "Paused", true)); Step(8, () => shot("06-pause")); Step(1, () => SetMember(g, "Paused", false));
        Step(1, () => { g.Combat.health = 0; g.Combat.state = RunState.Lost; }); Step(10, () => shot("07-lose"));
        Step(1, () => { g.StartRun(); g.Combat.bestChain = 7; g.Combat.wallHits = 38; g.Combat.kills = 61; g.Combat.state = RunState.Won; }); Step(10, () => shot("08-win"));
        Step(5, () =>
        {
            foreach (string k in keys) { if (saved.ContainsKey(k)) PlayerPrefs.SetInt(k, saved[k]); else PlayerPrefs.DeleteKey(k); }
            PlayerPrefs.Save(); g.ReloadProgress(); g.UseProgress(original); DeleteTemp(temp); g.GoHome(); Debug.Log("DAD_SCREENS_DONE: " + dir);
        });
    }
    // v6 reward screens on a temporary save: lunchbox poses, the card, the gear panel, the result button and dents per floor.
    static void QueueRewardAudit(string dir)
    {
        Directory.CreateDirectory(dir);
        var g = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        Action<string> shot = name => ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"), 2);
        DadProgress original = g.Progress; string temp = TempSavePath("rewards");
        Step(1, () => { var p = new DadProgress(DadEconomy.Load(), temp) { Rng = new System.Random(3) }; p.Save.lunchboxes = 12; p.Save.bestStage = 41; p.Save.coins = 500; g.UseProgress(p); g.GoHome(); g.Tab = DadSchoolPrototype.HomeTab.Lunchbox; });
        Step(10, () => shot("r01-gear-empty"));
        Step(1, () => g.BeginReveal());
        foreach (float t in new[] { .1f, .28f, .45f, .62f, 1.2f }) { float at = t; Step(1, () => SetMember(g, "revealClock", at)); Step(1, () => shot("r02-reveal-" + Mathf.RoundToInt(at * 100))); }
        Step(1, () => g.DecideReveal(true)); Step(6, () => shot("r03-equipped"));
        Step(1, () => { g.Progress.Save.lunchboxes = 1; g.BeginReveal(); g.SkipRevealToCard(); }); Step(1, () => SetMember(g, "revealClock", 1.2f)); Step(2, () => shot("r04-card-compare"));
        Step(1, () => g.DecideReveal(false));
        Step(1, () => { g.Progress.Save.lunchboxes = 30; SetMember(g, "toastTime", 0f); }); Step(1, () => g.SendMessage("OpenAllBoxes")); Step(4, () => shot("r05-open-all"));
        Step(1, () => { g.Tab = DadSchoolPrototype.HomeTab.Go; g.Progress.Save.lunchboxes = 1; g.StartRun(1); g.Combat.state = RunState.Won; });
        Step(10, () => shot("r06-result-lunchbox"));
        foreach (int stage in new[] { 1, 21, 41 })
        {
            int s = stage;
            Step(1, () =>
            {
                g.StartStage(s); g.Combat.enemies.Clear(); g.Combat.spawning = false; Rect a = g.PlayArea; int n = 0;
                foreach (float y in new[] { 290f, 365f, 440f, 466f, 515f, 548f, 580f, 590f, 652f, 665f, 740f })
                    for (int k = 0; k <= (n++ % 3); k++) { g.DentWall(new Vector2(a.xMin, y + 60)); g.DentWall(new Vector2(a.xMax, y + 60)); }
            });
            Step(12, () => shot("r07-dents-" + s));
        }
        Step(2, () => { g.UseProgress(original); DeleteTemp(temp); g.GoHome(); Debug.Log("DAD_REWARDS_DONE: " + dir); });
    }
    // v7 poses in the real battle view, held long enough to photograph (they last 70-120 ms in play).
    static void QueueMotionAudit(string dir)
    {
        Directory.CreateDirectory(dir);
        var g = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        Action<string> shot = name => ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"), 2);
        DadProgress original = g.Progress; string temp = TempSavePath("motion");
        var mine = new HashSet<Fighter>();
        Action keepMine = () => g.Combat.enemies.RemoveAll(e => !mine.Contains(e));
        Step(1, () => { g.UseProgress(new DadProgress(DadEconomy.Load(), temp)); g.StartRun(1); g.Combat.enemies.Clear(); g.Combat.spawning = false; });
        Step(20, () => SetMember(g, "stick", new Vector2(1, 0)));
        Step(5, () => SetMember(g, "dadStartPose", 30f)); Step(2, () => shot("m01-dad-start"));
        Step(1, () => SetMember(g, "dadStartPose", 0f)); Step(3, () => shot("m02-dad-walk"));
        Step(1, () => SetMember(g, "stick", Vector2.zero)); Step(12, () => SetMember(g, "dadStopPose", 30f)); Step(2, () => shot("m03-dad-stop"));
        Step(1, () =>
        {
            SetMember(g, "dadStopPose", 0f); var c = g.Combat; Rect a = g.PlayArea; c.enemies.Clear();
            var w = c.AddEnemy(new Vector2(a.xMax, 470), 0); w.stun = 30; mine.Add(w);
            var k = c.AddEnemy(new Vector2(a.xMin, 600), 0); k.stun = 30; k.wallCooldown = .3f; k.hp = 0; mine.Add(k);
            var walker = c.AddEnemy(new Vector2(130, 300), 0); mine.Add(walker);
        });
        Step(1, () => { keepMine(); foreach (Fighter e in g.Combat.enemies) if (e.pos.x > 400) e.wallCooldown = 30; var list = (System.Collections.IList)ProtoField("corpses").GetValue(g); foreach (object o in list) o.GetType().GetField("life").SetValue(o, .4f); });
        Step(1, () => shot("m04-wall-hit-and-crumple"));
        for (int i = 0; i < 4; i++) { int n = i; Step(4, () => { keepMine(); foreach (Fighter e in g.Combat.enemies) if (e.pos.x > 400) e.wallCooldown = 30; shot("m05-gum-walk-" + n); }); }
        Step(2, () => { g.UseProgress(original); DeleteTemp(temp); g.GoHome(); Debug.Log("DAD_MOTION_DONE: " + dir); });
    }
    // "아빠 전쟁" story text on a temporary save: floor-boss clear, the ending, a timeout loss and the lunchbox line.
    static void QueueStoryAudit(string dir)
    {
        Directory.CreateDirectory(dir);
        var g = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        Action<string> shot = name => ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"), 2);
        DadProgress original = g.Progress; string temp = TempSavePath("story");
        Step(1, () => { var p = new DadProgress(DadEconomy.Load(), temp); p.Save.bestStage = 60; g.UseProgress(p); });
        foreach (int stage in new[] { 20, 60 }) { int st = stage; Step(1, () => { g.StartStage(st); g.Combat.state = RunState.Won; }); Step(10, () => shot("s-clear-" + st)); }
        Step(1, () => { g.StartStage(3); g.Combat.elapsed = 999; }); Step(10, () => shot("s-timeout"));
        Step(1, () => { g.GoHome(); g.Progress.Save.lunchboxes = 1; g.BeginReveal(); }); Step(1, () => SetMember(g, "revealClock", .1f)); Step(1, () => shot("s-lunchbox"));
        Step(1, () => g.DecideReveal(true));
        Step(2, () => { g.UseProgress(original); DeleteTemp(temp); g.GoHome(); Debug.Log("DAD_STORY_DONE: " + dir); });
    }
    // v11 opening and v9 screens on a temporary save: splash, title, the six cuts, the three home tabs, the special with
    // its prop (dad's face, then mom's phone), and the win / loss / mid-boss reports.
    static void QueueUiAudit(string dir)
    {
        Directory.CreateDirectory(dir);
        var g = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        Action<string> shot = name => ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"), 4);   // small text needs the extra resolution
        string[] keys = { "DadV2.BestChain", "DadV2.Wins", "DadV2.Gear", "DadV2.TotalWallHits", DadSchoolPrototype.StorySeenKey };
        var saved = new Dictionary<string, int>();
        foreach (string k in keys) if (PlayerPrefs.HasKey(k)) saved[k] = PlayerPrefs.GetInt(k);
        DadProgress original = g.Progress; string temp = TempSavePath("ui"); int gear = g.Gear;
        Step(1, () =>
        {
            var p = new DadProgress(DadEconomy.Load(), temp) { Rng = new System.Random(5) };
            p.Save.coins = 646; p.Save.lunchboxes = 12; p.Save.bestStage = 8; p.Save.currentStage = 8; p.Save.pendingIdleCoins = 77; p.Save.pendingIdleBoxes = 1;
            for (int i = 0; i < 4; i++) { p.OpenLunchbox(); p.EquipPending(); }
            p.Save.upgrades = new[] { 3, 2, 4, 1 }; g.UseProgress(p); g.ShowSplash(); SetMember(g, "openingClock", .8f);
        });
        Step(3, () => shot("u01-splash"));
        Step(1, () => g.ShowTitle()); Step(4, () => shot("u02-title"));
        for (int c = 0; c < 6; c++) { int n = c; Step(1, () => { g.PlayStory(true); SetMember(g, "cut", n); SetMember(g, "openingClock", .9f); }); Step(2, () => shot("u03-story-" + (n + 1))); }
        Step(1, () => g.GoHome()); Step(6, () => shot("u04-home-go"));
        Step(1, () => g.Tab = DadSchoolPrototype.HomeTab.Lunchbox); Step(4, () => shot("u05-home-lunchbox"));
        Step(1, () => g.Tab = DadSchoolPrototype.HomeTab.Upgrade); Step(4, () => shot("u06-home-upgrade"));
        Step(1, () => { g.StartRun(8); g.Combat.charge = 100; }); Step(20, () => shot("u07-battle-special"));
        Step(1, () => SetMember(g, "Gear", 2)); Step(3, () => shot("u08-battle-phone")); Step(1, () => SetMember(g, "Gear", gear));
        Step(1, () => { g.Combat.bestChain = 7; g.Combat.wallHits = 12; g.Combat.kills = 18; g.Combat.state = RunState.Won; }); Step(10, () => shot("u09-result-win"));
        Step(1, () => { g.StartRun(8); g.Progress.Save.lossStage = 8; g.Progress.Save.lossesAtStage = 4; g.Combat.health = 0; g.Combat.state = RunState.Lost; }); Step(10, () => shot("u10-result-loss"));
        Step(1, () => { g.StartStage(10); g.Combat.bestChain = 5; g.Combat.wallHits = 9; g.Combat.kills = 14; g.Combat.state = RunState.Won; }); Step(10, () => shot("u11-result-midboss"));
        Step(5, () =>
        {
            foreach (string k in keys) { if (saved.ContainsKey(k)) PlayerPrefs.SetInt(k, saved[k]); else PlayerPrefs.DeleteKey(k); }
            PlayerPrefs.Save(); g.ReloadProgress(); SetMember(g, "Gear", gear); g.UseProgress(original); DeleteTemp(temp); g.GoHome(); Debug.Log("DAD_UI_DONE: " + dir);
        });
    }
    // The old auto-punch battle is kept for comparison; its audits switch the slingshot fight off until their last step.
    static void Legacy(Action queue) { DadSchoolPrototype.UseSlingshot = false; queue(); Step(1, () => DadSchoolPrototype.UseSlingshot = true); }
    // D-008 main flow with the slingshot fight on a temporary save: home, a fight, an aim and a flight, win and loss reports,
    // the floor bosses and big-dad, and the ending.
    static void QueueSlingMainAudit(string dir)
    {
        Directory.CreateDirectory(dir);
        var g = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        Action<string> shot = name => ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"), 2);
        string[] keys = { "DadV2.BestChain", "DadV2.Wins", "DadV2.Gear", "DadV2.TotalWallHits" };
        var saved = new Dictionary<string, int>();
        foreach (string k in keys) if (PlayerPrefs.HasKey(k)) saved[k] = PlayerPrefs.GetInt(k);
        DadProgress original = g.Progress; string temp = TempSavePath("slingmain"); int gear = g.Gear;
        Step(1, () => { var p = new DadProgress(DadEconomy.Load(), temp) { Rng = new System.Random(5) }; p.Save.coins = 320; p.Save.bestStage = 3; p.Save.currentStage = 3; p.Save.lunchboxes = 1; g.UseProgress(p); g.GoHome(); });
        Step(10, () => shot("sm01-home"));
        // 출발 card with a first appearance and with the floor boss; 강화 tab with one stat at its cap.
        Step(1, () => { g.Progress.Save.bestStage = g.Progress.Save.currentStage = 9; }); Step(4, () => shot("sm01b-card-new-foe"));
        Step(1, () => { g.Progress.Save.bestStage = g.Progress.Save.currentStage = 20; }); Step(4, () => shot("sm01c-card-boss"));
        Step(1, () => { g.Progress.Save.upgrades = new[] { 20, 5, 8, 12 }; g.Tab = DadSchoolPrototype.HomeTab.Upgrade; }); Step(4, () => shot("sm01d-upgrade"));
        Step(1, () => { g.Progress.Save.upgrades = new[] { 0, 0, 0, 0 }; g.Progress.Save.bestStage = g.Progress.Save.currentStage = 3; g.Tab = DadSchoolPrototype.HomeTab.Go; });
        Step(1, () => g.StartRun()); Step(100, () => shot("sm02-fight-1-3"));
        Step(1, () => g.SlingView.DebugAim(new Vector2(270, 700), new Vector2(250, 830))); Step(20, () => shot("sm03-aim"));
        Step(1, () => g.SlingView.DebugRelease()); Step(10, () => shot("sm04-flight"));
        Step(80, () => { g.Sling.shots = 4; g.Sling.bestChain = 6; g.Sling.wallSlams = 9; g.FinishSlingBattleNow(true); }); Step(12, () => shot("sm05-result-win"));
        Step(1, () => g.StartRun(3)); Step(40, () => g.FinishSlingBattleNow(false)); Step(12, () => shot("sm06-result-loss"));
        Step(1, () => { g.Progress.Save.bestStage = 60; g.StartStage(20); }); Step(160, () => shot("sm07-boss-1-20"));
        Step(1, () => g.StartStage(30)); Step(160, () => shot("sm08-floor2-bigdad"));
        Step(1, () => g.StartStage(60)); Step(160, () => shot("sm09-floor3-boss"));
        Step(1, () => g.FinishSlingBattleNow(true)); Step(12, () => shot("sm10-ending"));
        Step(5, () =>
        {
            foreach (string k in keys) { if (saved.ContainsKey(k)) PlayerPrefs.SetInt(k, saved[k]); else PlayerPrefs.DeleteKey(k); }
            PlayerPrefs.Save(); g.ReloadProgress(); SetMember(g, "Gear", gear); g.UseProgress(original); DeleteTemp(temp); g.GoHome(); Debug.Log("DAD_SLINGMAIN_DONE: " + dir);
        });
    }
    static void Poll()
    {
        if (pump && EditorApplication.isPlaying) { EditorApplication.QueuePlayerLoopUpdate(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); }
        if (steps.Count > 0 && EditorApplication.isPlaying && Time.frameCount >= nextStepFrame)
        {
            var step = steps.Dequeue();
            try { step.Value(); } catch (Exception e) { Debug.LogException(e); }
            if (steps.Count > 0) nextStepFrame = Time.frameCount + steps.Peek().Key;
        }
        if (delayedCapture != null && EditorApplication.isPlaying && Time.frameCount >= delayedCaptureFrame) { ScreenCapture.CaptureScreenshot(delayedCapture); Debug.Log("DAD_CAPTURED: " + delayedCapture); delayedCapture = null; }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(CommandPath)) return;
        string command = File.ReadAllText(CommandPath).Trim();
        if (command.Length == 0) return; // writer has not finished yet; try again next update
        File.Delete(CommandPath);
        try
        {
            if (command == "setup") Setup();
            else if (command == "play") { if (!EditorApplication.isPlaying) { Open(); EditorApplication.isPlaying = true; } }
            else if (command == "checks") RunChecks();
            else if (command == "previewbuild") DadPreviewBuild.Queue();
            else if (command == "commercialchecks") DadCommercialChecks.Run();
            else if (command == "phonepreview") DadCommercialChecks.PhonePreview();
            else if (command == "outfitchecks") DadOutfitChecks.Run();
            else if (command == "difficultybot") DadDifficultySetup.Bot();
            else if (command == "difficultychecks") DadDifficultySetup.Run();
            else if (command == "playchecks") RunPlayChecks();
            else if (command == "polishchecks") RunPolishChecks();
            else if (command == "bake") BakeArt();
            else if (command == "flowcheck") RunFlowCheck();
            else if (command == "balance") RunBalanceProbe();
            else if (command == "skipboss" && EditorApplication.isPlaying) UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>().StartStage(20); // debug: play the 1-20 floor boss
            else if (command.StartsWith("stage:") && EditorApplication.isPlaying) UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>().StartStage(int.Parse(command.Substring(6))); // debug
            else if (command == "refresh") AssetDatabase.Refresh();
            else if (command == "applyicon") ApplyAppIcon();
            else if (command == "retitle") Retitle();
            else if (command.StartsWith("simdebug:"))
            {
                // debug: simdebug:<stage>:<power ratio>:<0 novice | 1 skilled> -> one line per seed in the log
                string[] a = command.Substring(9).Split(':');
                for (int seed = 0; seed < 6; seed++) Debug.Log("DAD_SIM " + a[0] + " x" + a[1] + " seed" + seed + " " + SimulateStage(int.Parse(a[0]), float.Parse(a[1]), 101 + seed * 37, a[2] == "1").Line());
            }
            else if (command == "home" && EditorApplication.isPlaying) UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>().GoHome();
            else if (command == "start" && EditorApplication.isPlaying) UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>().StartRun();
            else if (command.StartsWith("capture:") && EditorApplication.isPlaying) ScreenCapture.CaptureScreenshot(command.Substring(8));
            else if (command.StartsWith("groggycapture:") && EditorApplication.isPlaying)
            {
                // debug: open the boss mid-corridor and capture the same frame so the dizzy stars are visible
                var c = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>().Combat; var b = c.enemies.Find(e => e.boss);
                if (b != null) { b.pos = c.arena.center + new Vector2(0, -40); b.velocity = Vector2.zero; b.groggy = 6; b.stun = 6; c.player = b.pos + new Vector2(-150, 70); }
                ScreenCapture.CaptureScreenshot(command.Substring(14));
            }
            else if (command == "status" && EditorApplication.isPlaying)
            {
                var g = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>(); var b = g.Combat == null ? null : g.Combat.enemies.Find(e => e.boss);
                Debug.Log("DAD_STATUS frame=" + Time.frameCount + " realtime=" + Time.realtimeSinceStartup.ToString("F1") + " elapsed=" + (g.Combat == null ? -1 : g.Combat.elapsed).ToString("F2")
                    + " state=" + (g.Combat == null ? "-" : g.Combat.state.ToString()) + " paused=" + g.Paused + " boss=" + (b == null ? "none" : b.hp + "/" + b.maxHp + " groggy=" + b.groggy.ToString("F2")));
            }
            else if ((command == "spawntray" || command.StartsWith("traycapture:")) && EditorApplication.isPlaying)
            {
                if (command.StartsWith("traycapture:")) { delayedCapture = command.Substring(12); delayedCaptureFrame = Time.frameCount + 45; }
                // debug: drop a tray guy in front of dad, with the intro banner
                var c = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>().Combat; c.enemies.RemoveAll(e => !e.boss); c.xp = 0;
                var t = Tray(c, c.player + new Vector2(105, -10), -1); c.events.Add(new CombatEvent("쟁반 짱 등장", t.pos, 3));
                Tray(c, c.player + new Vector2(-60, -150), 1);
                var charger = c.AddEnemy(c.player + new Vector2(-30, 160), 1); charger.windup = 3; charger.aim = (c.player - charger.pos).normalized; // long telegraph for the capture
            }
            else if (command.StartsWith("screens:") && EditorApplication.isPlaying) Legacy(() => QueueScreenAudit(command.Substring(8)));
            else if (command.StartsWith("rewards:") && EditorApplication.isPlaying) Legacy(() => QueueRewardAudit(command.Substring(8)));
            else if (command.StartsWith("motion:") && EditorApplication.isPlaying) Legacy(() => QueueMotionAudit(command.Substring(7)));
            else if (command.StartsWith("story:") && EditorApplication.isPlaying) Legacy(() => QueueStoryAudit(command.Substring(6)));
            else if (command.StartsWith("ui:") && EditorApplication.isPlaying) Legacy(() => QueueUiAudit(command.Substring(3)));
            else if (command.StartsWith("commercialui:") && EditorApplication.isPlaying) QueueCommercialAudit(command.Substring(13));
            else if (command.StartsWith("outfitmatrix:") && EditorApplication.isPlaying) QueueOutfitMatrix(command.Substring(13));
            else if (command.StartsWith("outfitui:") && EditorApplication.isPlaying) QueueOutfitAudit(command.Substring(9));
            else if (command.StartsWith("difficultyui:") && EditorApplication.isPlaying) QueueDifficultyAudit(command.Substring(13));
            else if (command.StartsWith("slingmain:") && EditorApplication.isPlaying) QueueSlingMainAudit(command.Substring(10));
            else if (command == "pump" && EditorApplication.isPlaying) { pump = true; Application.runInBackground = true; }
            else if (command == "stop") { pump = false; EditorApplication.isPlaying = false; }
            Debug.Log("DAD_COMMAND_OK: " + command);
        }
        catch (Exception e) { Debug.LogException(e); }
    }
    [MenuItem("Dad School Boss/Prepare Prototype Scene")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode before preparing the scene."); return; }
        AssetDatabase.Refresh();
        foreach (string name in new[] { "Hero", "Enemies", "Arena" })
        {
            var importer = AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV2/" + name + ".png") as TextureImporter;
            if (importer == null) throw new Exception("Missing atlas " + name);
            importer.textureType = TextureImporterType.Default; importer.isReadable = name != "Arena";
            importer.alphaIsTransparency = name != "Arena"; importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear; importer.SaveAndReimport();
        }
        // Replace only the previously created school placeholder scene, preserving the old Alley scene.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor; camera.orthographic = true; camera.gameObject.AddComponent<AudioListener>();
        new GameObject("Dad School Prototype").AddComponent<DadSchoolPrototype>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var item in EditorBuildSettings.scenes) if (item.path != ScenePath) scenes.Add(new EditorBuildSettingsScene(item.path, false));
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = scenes.ToArray();
        PlayerSettings.productName = DadSchoolPrototype.Title;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.defaultIsNativeResolution = false; PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        AssetDatabase.SaveAssets(); RunChecks(); Debug.Log("DAD_SETUP_READY");
    }
    [MenuItem("Dad School Boss/Open Prototype")]
    public static void Open() { EditorSceneManager.OpenScene(ScenePath); }
    [MenuItem("Dad School Boss/Bake Sprite Frames")]
    public static void BakeArt()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode before baking art");
        foreach (string name in new[] { "Hero", "Enemies" }) DadSchoolPrototype.BakeArtFrames(name);
        AssetDatabase.Refresh();
        foreach (string name in new[] { "Hero", "Enemies" }) for (int i = 0; i < 4; i++)
        {
            string path = "Assets/DadSchoolBoss/Resources/DadV2/Baked/" + name + "/" + i + ".png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.isReadable = false; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        Debug.Log("DAD_ART_BAKED: 8 frames; runtime flood fill no longer required");
    }
    static string TempSavePath(string tag) { return Path.Combine(Path.GetTempPath(), "dad-" + tag + "-" + Guid.NewGuid().ToString("N") + ".json"); }
    static void DeleteTemp(string path) { if (File.Exists(path)) File.Delete(path); if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp"); if (File.Exists(path + ".bak")) File.Delete(path + ".bak"); }
    static AlleyCombat NewStageCombat() { var c = new AlleyCombat(0); c.SetArena(new Rect(64, 240, 412, 520)); c.player = new Vector2(270, 570); c.Start(); return c; }
    static void Expect(bool condition, string message, StringBuilder report)
    { if (!condition) throw new Exception("DAD_CHECK_FAILED: " + message); report.AppendLine("PASS " + message); }
    [MenuItem("Dad School Boss/Run Prototype Checks")]
    public static void RunChecks()
    {
        var report = new StringBuilder("Dad School Boss prototype checks\n");
        foreach (string name in new[] { "Hero", "Enemies", "Arena" })
        {
            Texture2D texture = Resources.Load<Texture2D>("DadV2/" + name);
            Expect(texture != null, name + " asset resolves through Resources", report);
            if (name != "Arena")
            {
                var colors = texture.GetPixels32(); int transparent = 0, opaque = 0;
                foreach (var c in colors) { if (c.a < 10) transparent++; if (c.a > 240) opaque++; }
                Expect(transparent > colors.Length / 10 && opaque > colors.Length / 10, name + " has real transparent background and visible pixels", report);
            }
        }
        RunAssetChecks(report);
        var cbt = new AlleyCombat(); cbt.SetArena(new Rect(64, 240, 412, 520)); cbt.spawning = false; cbt.Start();
        cbt.player = new Vector2(350, 550); cbt.wallSkill = 1;
        var target = cbt.AddEnemy(new Vector2(470, 550)); target.velocity = new Vector2(600, 0);
        cbt.Tick(.02f, Vector2.zero, false);
        Expect(cbt.wallHits == 1 && target.hp <= 1, "equipped slipper increases wall damage inside school arena", report);
        cbt = new AlleyCombat(); cbt.spawning = false; cbt.Start(); cbt.xp = 3; cbt.Tick(.02f, Vector2.zero, false);
        Expect(cbt.state == RunState.Choosing, "level up opens skill selection", report);
        cbt.Choose(1); Expect(cbt.chainSkill == 1 && cbt.state == RunState.Fighting, "choice applies and resumes battle", report);
        cbt = new AlleyCombat(); cbt.spawning = false; cbt.Start(); var boss = cbt.AddEnemy(cbt.player + new Vector2(60, 0), 2); boss.hp = 1;
        for (int i = 0; i < 20 && cbt.state == RunState.Fighting; i++) cbt.Tick(.02f, Vector2.zero, false);
        Expect(cbt.state == RunState.Won, "school boss defeat reaches victory", report);
        cbt = new AlleyCombat(); cbt.spawning = false; cbt.Start(); cbt.health = 0; cbt.Tick(.02f, Vector2.zero, false);
        Expect(cbt.state == RunState.Lost, "zero health reaches defeat", report);
        File.WriteAllText("Design/dad-prototype-checks.txt", report.ToString()); Debug.Log(report.ToString());
    }
    // v5 delivery: every frame the animation code asks for exists, and the import rules took effect.
    static void RunAssetChecks(StringBuilder report)
    {
        var art = DadArt.Load();
        string boss = "walk:2 windup:1 dash:1 groggy:1 hit:1 enrage:1 fly:1";
        var expected = new Dictionary<string, string>
        {
            { "dad", "idle:2 walk:4 punch:3 special:1 hurt:1 win:1" }, { "gum", "walk:2 attack:2 hit:1 fly:1" },
            { "chair", "walk:2 windup:1 dash:1 hit:1 fly:1" }, { "tray", "walk:2 block:1 attack:1 hit:1 fly:1 notray_walk:2 notray_hit:1" },
            { "boss1", boss }, { "boss2", boss }, { "boss3", boss }, { "dodge", "walk:2 throw:2 hit:1 fly:1" },
        };
        foreach (var kv in expected)
        {
            DadArt.Character c; bool ok = art.characters.TryGetValue(kv.Key, out c);
            if (ok) foreach (string part in kv.Value.Split(' ')) { string[] a = part.Split(':'); if (c.Count(a[0]) != int.Parse(a[1])) ok = false; }
            Expect(ok, "v5 frames complete for " + kv.Key + " (" + kv.Value + ")", report);
        }
        Expect(art.ui.Count == 21 && art.gear.Count == 18 && art.backgrounds.Count == 3 && art.audio.Count == 21 && art.props.Count == 2, "v5 delivery: 21 UI icons, 18 gear icons, 3 backgrounds, 21 sounds, 2 props", report);
        var frame = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV5/chars/dad/dad_walk_0.png");
        var backdrop = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV5/bg/bg_floor1_corridor.png");
        var bgm = (AudioImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV5/audio/bgm_battle.wav");
        Expect(frame.maxTextureSize == 512 && frame.textureCompression != TextureImporterCompression.Uncompressed && !frame.mipmapEnabled && backdrop.maxTextureSize == 2048
            && bgm.defaultSampleSettings.loadType == AudioClipLoadType.Streaming, "v5 import rules: 512 compressed frames, 2048 backgrounds, streamed music", report);
        // v6 reward art (Design/dad-reward-assets-v6.md), prepared by Tools/prepare_dad_rewards_v6.py
        bool rewards = art.rewards.Count == 12;
        foreach (string n in new[] { "lunchbox_closed", "lunchbox_opening", "lunchbox_open", "gear_card_common", "gear_card_rare", "gear_card_best", "locker_damage_1", "locker_damage_2", "locker_damage_3", "locker_damage_3_steel" }) if (art.Reward(n) == null) rewards = false;
        Expect(rewards, "v6 rewards: 3 lunchbox poses, 3 card frames, 3 locker dents and their steel copies", report);
        var lunch = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV6/lunchbox_open.png");
        var card = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV6/gear_card_rare.png");
        var dent = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV6/locker_damage_2.png");
        Expect(lunch != null && card != null && dent != null && lunch.maxTextureSize == 1024 && card.maxTextureSize == 512 && dent.maxTextureSize == 512 && card.alphaIsTransparency && !card.mipmapEnabled && card.wrapMode == TextureWrapMode.Clamp,
            "v6 import rules: lunchbox 1024, cards and dents 512, alpha, no mips, clamp", report);
        bool icons = true;
        for (int s = 0; s < DadProgress.SlotCount; s++) for (int r = 0; r < DadProgress.RarityCount; r++) if (art.Gear(DadProgress.ItemIcon(s, r)) == null) icons = false;
        Expect(icons, "every gear slot x rarity has its v5 icon", report);
        // v7 motion poses (Design/dad-motion-assets-v7.md), normalized by Tools/prepare_dad_motion_v7.py
        DadArt.Character dadArt, gumArt;
        Expect(art.characters.TryGetValue("dad", out dadArt) && art.characters.TryGetValue("gum", out gumArt) && dadArt.Count("movestart") == 1 && dadArt.Count("movestop") == 1
            && gumArt.Count("pass") == 2 && gumArt.Count("wallhit") == 1 && gumArt.Count("crumple") == 1 && gumArt.Count("walk") == 2, "v7 motion: dad start/stop, gum passing x2, wall hit, crumple (v5 walk untouched)", report);
        var motionPose = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV7/gum_pass_0.png");
        Expect(motionPose != null && motionPose.maxTextureSize == 512 && motionPose.alphaIsTransparency && !motionPose.mipmapEnabled, "v7 import rules match the v5 character frames", report);
        // v11 opening stills (Design/dad-war-opening-assets-v11.md), copied from Art/V11Opening into Resources/DadV11
        bool stills = true;
        foreach (string n in new[] { "title_keyart", "story_01_home", "story_02_prank", "story_03_where", "story_04_dash", "story_05_panic", "story_06_school" })
            if (Resources.Load<Texture2D>("DadV11/" + n) == null) stills = false;
        Expect(stills, "v11 opening: title key art and six story cuts resolve through Resources", report);
        var still = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV11/story_01_home.png");
        Expect(still != null && still.maxTextureSize == 2048 && !still.mipmapEnabled && !still.alphaIsTransparency && still.npotScale == TextureImporterNPOTScale.None,
            "v11 import rules: 2048 opaque stills, no mips, no NPOT rescale", report);
        bool screenIcons = true;
        foreach (string n in new[] { "tab_lunchbox", "tab_go", "tab_upgrade", "btn_idle_chest", "mom_cheer", "mom_call", "btn_awaken" }) if (art.Icon(n) == null) screenIcons = false;
        Expect(screenIcons, "v9 screens: three tab icons, mom's stash, cheer, phone and dad's angry face", report);
    }
    static MethodInfo ProtoMethod(string name) { return typeof(DadSchoolPrototype).GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); }
    static FieldInfo ProtoField(string name) { return typeof(DadSchoolPrototype).GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); }
    // Lunchbox gear: rolls, equip/dismantle, persistence, v1 migration, and calibration against the simulation's curve.
    static void RunGearChecks(StringBuilder report)
    {
        var eco = DadEconomy.Load(); string temp = TempSavePath("gear");
        try
        {
            var p = new DadProgress(eco, temp) { Rng = new System.Random(7) };
            Expect(Mathf.Abs(p.GearMultiplier - 1) < 1e-5f && !p.HasPendingGear && !p.OpenLunchbox(), "fresh save: no gear and nothing to open", report);
            p.Save.lunchboxes = 8;
            Expect(p.OpenLunchbox() && p.HasPendingGear && p.Save.pendingRarity == eco.gearFirstRarity && p.Save.lunchboxes == 7 && !p.OpenLunchbox(), "first lunchbox is a guaranteed rare card, decided before the next box opens", report);
            float value = p.Save.pendingValue; int first = p.Save.pendingSlot;
            p.EquipPending();
            Expect(!p.HasPendingGear && p.HasGear(first) && Mathf.Abs(p.GearMultiplier - (1 + value)) < 1e-5f, "equipping adds the card's value to power", report);
            var slots = new HashSet<int> { first };
            for (int i = 0; i < 5; i++) { p.OpenLunchbox(); slots.Add(p.Save.pendingSlot); p.EquipPending(); }
            Expect(slots.Count == DadProgress.SlotCount, "the first six boxes fill six different slots", report);
            p.OpenLunchbox(); int rarity = p.Save.pendingRarity; double coins = p.Save.coins, pay = p.DismantleCoins(rarity); float mult = p.GearMultiplier;
            p.DismantlePending();
            Expect(pay > 0 && Math.Abs(p.Save.coins - coins - pay) < 1e-6 && Mathf.Abs(p.GearMultiplier - mult) < 1e-6f, "dismantling pays coins and leaves the gear alone", report);
            p.Save.lunchboxes = 1; p.OpenLunchbox(); int oldRarity = p.Save.gearRarity[p.Save.pendingSlot]; coins = p.Save.coins;
            p.EquipPending();
            Expect(Math.Abs(p.Save.coins - coins - p.DismantleCoins(oldRarity)) < 1e-6, "equipping over an item dismantles the old one for coins", report);
            p.Save.lunchboxes = 1; p.OpenLunchbox(); int pendingSlot = p.Save.pendingSlot; float pendingValue = p.Save.pendingValue; p.Persist();
            var back = new DadProgress(eco, temp);
            Expect(back.HasPendingGear && back.Save.pendingSlot == pendingSlot && Mathf.Abs(back.Save.pendingValue - pendingValue) < 1e-6f && Mathf.Abs(back.GearMultiplier - p.GearMultiplier) < 1e-5f,
                "an undecided card and the equipped set survive a reload", report);
            back.Save.lunchboxes = 40; float beforeAll = back.GearMultiplier; int equipped; double paid;
            int opened = back.OpenAllLunchboxes(out equipped, out paid);
            Expect(opened == 40 && back.Save.lunchboxes == 0 && !back.HasPendingGear && back.GearMultiplier >= beforeAll && paid > 0, "open all decides every box, never lowers power and pays for the rest", report);
            foreach (int boxes in new[] { 50, 150 })
            {
                double sum = 0; const int seeds = 150;
                for (int seed = 0; seed < seeds; seed++)
                {
                    var q = new DadProgress(eco, TempSavePath("calib")) { Rng = new System.Random(seed) };   // never persisted
                    q.Save.lunchboxes = boxes; int e; double c; q.OpenAllLunchboxes(out e, out c);
                    sum += q.GearMultiplier - 1;
                }
                float ratio = (float)(sum / seeds) / (eco.gearLogScale * Mathf.Log(1 + boxes / 10f));
                Expect(ratio > .8f && ratio < 1.2f, "rolled gear after " + boxes + " boxes stays within 20% of the simulation curve (" + ratio.ToString("F2") + "x)", report);
            }
            File.WriteAllText(temp, "{\"version\":1,\"coins\":10,\"lunchboxes\":2,\"boxesOpened\":30,\"bestStage\":5,\"currentStage\":5}");
            var old = new DadProgress(eco, temp);
            Expect(old.Save.version == 2 && !old.HasPendingGear && old.Save.lunchboxes == 2 && Mathf.Abs(old.GearMultiplier - (1 + eco.gearLogScale * Mathf.Log(4f))) < 1e-4f, "a v1 save keeps its gear power as a starter set", report);
        }
        finally { DeleteTemp(temp); }
    }
    // The app name follows the game title (11/4: "아빠 전쟁"). Editor saves live under the product name, so copy them first.
    static void Retitle()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode before renaming the app."); return; }
        PlayerSettings.productName = DadSchoolPrototype.Title; AssetDatabase.SaveAssets();
        Debug.Log("DAD_RETITLED: " + PlayerSettings.productName + " data=" + Application.persistentDataPath);
    }
    [MenuItem("Dad School Boss/Apply App Icon")]
    public static void ApplyAppIcon()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/DadSchoolBoss/Art/V5/app_icon.png");
        if (icon == null) throw new Exception("Missing app icon");
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
        AssetDatabase.SaveAssets(); Debug.Log("DAD_ICON_APPLIED");
    }
    [MenuItem("Dad School Boss/Run Play Mode Checks")]
    public static void RunPlayChecks()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play Mode is required");
        var game = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        var report = new StringBuilder("Dad School Boss Play Mode integration checks\n");
        string[] keys = { "DadV2.BestChain", "DadV2.Wins", "DadV2.Gear", "DadV2.TotalWallHits", DadSchoolPrototype.StorySeenKey };
        int[] values = new int[keys.Length]; bool[] exists = new bool[keys.Length];
        for (int i = 0; i < keys.Length; i++) { exists[i] = PlayerPrefs.HasKey(keys[i]); values[i] = PlayerPrefs.GetInt(keys[i]); }
        int originalGear = game.Gear;
        DadProgress original = game.Progress; string temp = TempSavePath("playcheck"), slingTemp = TempSavePath("slingcheck");
        DadSchoolPrototype.UseSlingshot = false;
        try
        {
            game.UseProgress(new DadProgress(DadEconomy.Load(), temp));
            // v11 opening: splash -> title -> (new save) six-cut story -> 1-1; once the story is seen, game start goes home.
            PlayerPrefs.DeleteKey(DadSchoolPrototype.StorySeenKey);
            game.ShowSplash(); SetMember(game, "openingClock", 5f); game.SendMessage("Update");
            Expect(game.Opening == DadSchoolPrototype.OpeningStep.Title, "the company splash hands over to the title", report);
            game.StartGame();
            Expect(game.Opening == DadSchoolPrototype.OpeningStep.Story && game.StoryCut == 0, "game start on a new save plays the story", report);
            for (int i = 0; i < 6; i++) game.NextCut();
            Expect(!game.InOpening && !game.Home && game.PlayingStage == 1 && game.StorySeen, "the last cut leads straight into 1-1 and the story is remembered", report);
            game.ShowTitle(); game.StartGame();
            Expect(game.Home && !game.InOpening && game.Tab == DadSchoolPrototype.HomeTab.Go, "once the story is seen, game start goes home", report);
            game.SelectGear(0);   // the player's own loadout (e.g. the phone, which has no fever) is restored in finally
            game.StartRun();
            Expect(!game.Home && game.Combat.state == RunState.Fighting && game.PlayingStage == 1 && game.Director.Plan.lesson == 1, "start plays the saved stage (1-1 on a fresh save)", report);
            Expect(game.Combat.arena == game.PlayArea, "rendering and simulation use the same arena", report);
            game.Combat.enemies.Clear(); game.Combat.spawning = false;
            Vector2 before = game.Combat.player;
            for (int i = 0; i < 30; i++) game.Combat.Tick(.02f, new Vector2(0, 1), false);
            Expect(game.Combat.player.y > before.y, "movement input changes the live player position", report);
            game.Combat.charge = 100;
            Expect(game.Combat.Special() && game.Combat.isFever, "special triggers a live attack and fever", report);
            game.Combat.state = RunState.Choosing; game.Combat.level = 2; game.Combat.Choose(0);
            Expect(game.Combat.state == RunState.Fighting, "selection resumes a live battle", report);
            game.Combat.bestChain = game.BestChain + 3; game.Combat.wallHits = DadSchoolPrototype.PhoneWallGoal; game.Combat.state = RunState.Won;
            int oldWallHits = game.TotalWallHits;
            int oldWins = game.Wins; game.SendMessage("Update"); game.SendMessage("Update");
            Expect(game.Wins == oldWins + 1, "victory commits exactly once", report);
            Expect(game.TotalWallHits == oldWallHits + DadSchoolPrototype.PhoneWallGoal && game.PhoneGearUnlocked, "wall record commits once and unlocks phone", report);
            Expect(PlayerPrefs.GetInt(keys[0]) == game.BestChain && PlayerPrefs.GetInt(keys[1]) == game.Wins, "chain and win records persist", report);
            Expect(game.LastReward != null && game.LastReward.won && game.Progress.Save.bestStage == 2 && game.Progress.Save.coins > 0 && File.Exists(temp), "victory pays coins, unlocks 1-2 and saves to disk", report);
            game.StartRun();
            Expect(game.PlayingStage == 2 && game.Director.Plan.lesson == 2, "start moves on to the next stage", report);
            game.StartRun(5);
            Expect(game.PlayingStage == 2, "a locked stage cannot be started from the menu", report);
            game.SelectGear(1); game.StartRun();
            Expect(game.Combat.chainSkill == 1 && game.Combat.wallSkill == 0, "unlocked lunchbox changes the next run build", report);
            Expect(game.Combat.elapsed == 0 && game.Combat.health == game.Combat.maxHealth, "retry resets health and timer", report);
            game.SelectGear(2); game.StartRun();
            Expect(game.Combat.phoneSpecial && game.Combat.chainSkill == 0, "phone replaces the special instead of stacking lunchbox", report);
            double coins = game.Progress.Save.coins;
            game.Combat.health = 0; game.Combat.state = RunState.Lost; game.SendMessage("Update");
            Expect(game.Wins == oldWins + 1 && game.LastReward != null && !game.LastReward.won && game.Progress.Save.coins > coins && game.Progress.Save.lossesAtStage == 1, "defeat pays the consolation share and awards no win", report);
            game.GoHome(); Expect(game.Home && !game.Paused, "home returns to a usable menu", report);
            // v6: the lunchbox opens into a gear card; equipping it raises power.
            game.Progress.Save.lunchboxes = 2; float gearBefore = game.Progress.GearMultiplier;
            Expect(game.BeginReveal() && game.Revealing && !game.RevealShowsCard, "tapping a lunchbox starts the opening animation", report);
            game.SkipRevealToCard();
            Expect(game.RevealShowsCard && game.Progress.HasPendingGear, "the opening ends on a gear card", report);
            game.DecideReveal(true);
            Expect(!game.Revealing && game.Progress.GearMultiplier > gearBefore && game.Progress.Save.lunchboxes == 1, "equipping the card raises power and closes the reveal", report);
            // v6: side-wall slams dent the lockers on 1F; three stages per spot; doors and the back wall stay clean.
            game.StartRun(1); Rect a = game.PlayArea;
            MethodInfo impact = typeof(DadSchoolPrototype).GetMethod("ImpactJuice", BindingFlags.NonPublic | BindingFlags.Instance);
            impact.Invoke(game, new object[] { new CombatEvent("벽꿍!", new Vector2(a.xMin, 466 + 60), 1) });
            Expect(game.DentCount == 1 && game.MaxDentLevel == 1, "a wall slam next to a locker dents it", report);
            for (int i = 0; i < 4; i++) impact.Invoke(game, new object[] { new CombatEvent("짱 벽꿍!!", new Vector2(a.xMax, 548 + 70), 3) });
            Expect(game.DentCount == 2 && game.MaxDentLevel == 3, "boss slams count too, and a spot stops at stage 3", report);
            game.DentWall(new Vector2(a.xMin, 300 + 60)); game.DentWall(new Vector2(a.center.x, a.yMin));
            Expect(game.DentCount == 2, "doors and the back wall take no locker dents", report);
            game.StartRun(1); Expect(game.DentCount == 0, "a new battle starts with clean lockers", report);
            // v7: transition poses (README order) — dad idle > start > walk > stop > idle; gum walk_0 > pass_0 > walk_1 > pass_1.
            game.Combat.enemies.Clear(); game.Combat.spawning = false;
            Func<string> dadPose = () => (string)ProtoMethod("DadAction").Invoke(game, new object[] { 0 });
            Action<float> motion = dt => ProtoMethod("UpdateMotion").Invoke(game, new object[] { dt });
            Func<Fighter, string> enemyPose = f => { var args = new object[] { f, 0 }; string act = (string)ProtoMethod("EnemyAction").Invoke(game, args); return act + (int)args[1]; };
            game.Combat.moveVelocity = Vector2.zero; motion(.3f);
            game.Combat.moveVelocity = new Vector2(172, 0); motion(.016f); string start = dadPose(); motion(.1f); string walk = dadPose(); motion(.3f);
            game.Combat.moveVelocity = Vector2.zero; motion(.016f); string stop = dadPose(); motion(.12f); string idle = dadPose();
            game.Combat.moveVelocity = new Vector2(172, 0); motion(.016f); string jiggle = dadPose();
            Expect(start == "movestart" && walk == "walk" && stop == "movestop" && idle == "idle" && jiggle == "walk", "dad: idle > start > walk > stop > idle, and a quick restart skips the start pose (" + start + " " + walk + " " + stop + " " + idle + " " + jiggle + ")", report);
            game.Combat.moveVelocity = Vector2.zero;
            var gumGuy = game.Combat.AddEnemy(game.PlayArea.center, 0); gumGuy.locomotion = new Vector2(75, 0);
            var seen = new List<string> { enemyPose(gumGuy) };
            for (int i = 0; i < 30; i++) { motion(.05f); string pose = enemyPose(gumGuy); if (seen[seen.Count - 1] != pose) seen.Add(pose); }
            string cycle = string.Join(" ", seen.GetRange(0, Mathf.Min(5, seen.Count)));
            Expect(cycle == "walk0 pass0 walk1 pass1 walk0", "gum walks walk_0 > pass_0 > walk_1 > pass_1 by distance (" + cycle + ")", report);
            string held = enemyPose(gumGuy); gumGuy.velocity = new Vector2(30, 0); motion(.5f); gumGuy.velocity = Vector2.zero;
            Expect(enemyPose(gumGuy) == held, "knockback does not advance the walk cycle", report);
            gumGuy.pos = new Vector2(game.PlayArea.xMax, 500); gumGuy.wallCooldown = .30f; string wallPose = enemyPose(gumGuy);
            gumGuy.wallCooldown = .20f; string recovered = enemyPose(gumGuy);
            gumGuy.pos = new Vector2(game.PlayArea.center.x, game.PlayArea.yMin); gumGuy.wallCooldown = .30f; gumGuy.velocity = new Vector2(0, -400); string backWall = enemyPose(gumGuy);
            Expect(wallPose == "wallhit0" && recovered != "wallhit0" && backWall != "wallhit0", "gum shows the wall pose only right after a side-wall contact (" + wallPose + " " + recovered + " " + backWall + ")", report);
            var corpses = (System.Collections.IList)ProtoField("corpses").GetValue(game); corpses.Clear();
            var flyer = game.Combat.AddEnemy(game.PlayArea.center, 0);
            gumGuy.pos = new Vector2(game.PlayArea.xMin, 520); gumGuy.wallCooldown = .29f; gumGuy.velocity = Vector2.zero;
            ProtoMethod("RememberEnemies").Invoke(game, null); gumGuy.hp = 0; flyer.hp = 0;
            ProtoMethod("UpdateJuice").Invoke(game, new object[] { .016f });
            Func<object, string, bool> flag = (o, n) => (bool)o.GetType().GetField(n).GetValue(o);
            int wallBodies = 0, flying = 0; foreach (object o in corpses) { if (flag(o, "wall") && !flag(o, "right")) wallBodies++; else if (!flag(o, "wall")) flying++; }
            Expect(corpses.Count == 2 && wallBodies == 1 && flying == 1, "a gum knocked out on the wall slumps there; one knocked out mid-floor still flies off", report);
            game.GoHome();
            // D-008: the main game's battle is the slingshot fight; home, rewards and saves stay as they were.
            DadSchoolPrototype.UseSlingshot = true;
            game.UseProgress(new DadProgress(DadEconomy.Load(), slingTemp)); game.SelectGear(0);
            game.StartRun();
            Expect(game.SlingBattle && !game.Home && game.Combat == null && game.Sling != null && game.Sling.stage.label == "1-1" && game.PlayingStage == 1, "start opens the slingshot fight for the saved stage (1-1)", report);
            Expect(game.Sling.wallBonus == 1 && Mathf.Approximately(game.Sling.dad.maxHealth, 100) && game.SlingView.transform.parent == game.transform, "벽꿍형 gear adds wall damage, a fresh save has base health, the fight runs on a child", report);
            int slingWins = game.Wins;
            game.FinishSlingBattleNow(true); game.FinishSlingBattleNow(true);
            Expect(game.Wins == slingWins + 1 && game.LastReward != null && game.LastReward.won && game.LastReward.stars >= 1 && game.Progress.Save.bestStage == 2 && game.SlingResult != null, "a won fight commits once: coins, stars, 1-2 unlocked", report);
            Expect(game.Progress.Stars(1) == game.SlingResult.Stars, "the fight's own stars are saved", report);
            game.StartRun();
            Expect(game.SlingBattle && game.SlingResult == null && game.Sling.stage.label == "1-2", "start moves on to 1-2 in a fresh fight", report);
            double slingCoins = game.Progress.Save.coins;
            game.FinishSlingBattleNow(false);
            Expect(!game.LastReward.won && game.Progress.Save.coins > slingCoins && game.Progress.Save.lossesAtStage == 1 && game.Progress.Save.currentStage == 2, "a lost fight pays the consolation share and stays on the stage", report);
            game.Progress.Save.upgrades = new[] { 10, 5, 10, 10 }; game.StartRun();
            Expect(game.Sling.impactBonus > .29f && Mathf.Approximately(game.Sling.knockBonus, 1) && game.Sling.launchBonus > 1.14f && game.Sling.dad.maxHealth > 139 && game.Sling.wallBonus == 2, "upgrades reach the fight (들이받기, 체력, 도움닫기, 멀리 날리기)", report);
            Expect(Mathf.Approximately(DadSchoolPrototype.GrowthBonus(0, 30), .6f) && DadSchoolPrototype.GrowthCapped(0, 20) && !DadSchoolPrototype.GrowthCapped(1, 500), "upgrade caps the 강화 tab shows: 들이받기 stops at +60%, 체력 has none", report);
            DadEconomy cardEco = game.Progress.Eco;
            int[] firsts = new int[10]; for (int s = 1; s <= 10; s++) firsts[s - 1] = DadSchoolPrototype.NewKind(cardEco, s);
            Expect(firsts[3] == (int)SlingshotSim.Kind.Chair && firsts[6] == (int)SlingshotSim.Kind.Tray && firsts[8] == (int)SlingshotSim.Kind.Dodge && DadSchoolPrototype.NewKind(cardEco, 30) < 0
                && System.Array.FindAll(firsts, k => k >= 0).Length == 3, "the 출발 card flags only the first chair (1-4), tray (1-7) and dodgeball dad (1-9) (1-1..1-10: " + string.Join(",", firsts) + ")", report);
            game.StartStage(20);
            Expect(game.Sling.stage.floor == 1 && game.Sling.stage.spawns.Exists(sp => sp.kind == SlingshotSim.Kind.Boss), "1-20 is the floor-1 boss fight", report);
            game.StartStage(30);
            Expect(game.Sling.stage.label == "2-10" && game.Sling.stage.floor == 2 && game.Sling.stage.spawns.Exists(sp => sp.elite), "2-10 is floor 2's big-dad", report);
            game.Progress.Save.bestStage=2; game.Progress.Save.currentStage=2; game.Progress.Save.stars=new[]{3};
            Expect(game.StartRematch(1,1) && game.Sling.stage.challengeLevel==1 && game.Sling.stage.shotLimit>0,"main game starts an unlocked rematch with its shot limit",report);
            game.FinishSlingBattleNow(true);
            Expect(game.LastReward.challengeLevel==1 && game.Progress.RematchRank(1)==1 && game.Progress.Save.currentStage==2,"rematch finish commits rank and preserves onward stage",report);
            double rematchCoins=game.Progress.Save.coins;
            game.FinishSlingBattleNow(true);
            Expect(game.Progress.Save.coins==rematchCoins,"duplicate finish callback does not repay rematch",report);
            Expect(!game.StartRematch(1,3),"main entry rejects a locked rematch rank",report);
            game.StartRun(1);
            Expect(game.Sling.stage.challengeLevel==0 && game.Sling.stage.shotLimit==0,"normal replay after rematch has no leftover challenge restrictions",report);
            game.GoHome();
            Expect(game.Home && !game.SlingBattle && game.Sling == null && game.SlingView.Sim == null, "home ends the fight", report);
            if (game.PhoneGearUnlocked) { game.SelectGear(2); game.StartRun(); Expect(game.Sling.ragePending, "폭주형 gear starts with a rage shot", report); game.GoHome(); }
        }
        finally
        {
            DadSchoolPrototype.UseSlingshot = true;
            game.SelectGear(originalGear); game.UseProgress(original); DeleteTemp(temp); DeleteTemp(slingTemp);
            for (int i = 0; i < keys.Length; i++) { if (exists[i]) PlayerPrefs.SetInt(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]); }
            PlayerPrefs.Save(); game.ReloadProgress(); game.GoHome();
        }
        File.WriteAllText("Design/dad-prototype-playchecks.txt", report.ToString()); Debug.Log(report.ToString());
    }
    [MenuItem("Dad School Boss/Run Polish Checks")]
    public static void RunPolishChecks()
    {
        var report = new StringBuilder("School stage, economy and equipment checks\n");
        RunEconomyChecks(report);
        RunGearChecks(report);
        RunStageChecks(report);
        // Boss break rule: punches only shove; a thrown body makes it groggy; only then can it be launched.
        var rule = new AlleyCombat(); rule.Start(); rule.spawning = false; rule.groggyBoss = true;
        var schoolBoss = rule.AddEnemy(rule.player + new Vector2(60, 0), 2); Vector2 bossStart = schoolBoss.pos;
        rule.feverClock = 2; rule.Attack(schoolBoss);
        Expect(schoolBoss.velocity == Vector2.zero && schoolBoss.pos.x > bossStart.x && schoolBoss.hp == schoolBoss.maxHp, "plain punch only shoves a fresh boss, even in fever", report);
        var thrown = rule.AddEnemy(schoolBoss.pos - new Vector2(45, 0)); thrown.velocity = new Vector2(700, 0); int bossHp = schoolBoss.hp;
        rule.Tick(.02f, Vector2.zero, false);
        Expect(schoolBoss.groggy > 1.5f && schoolBoss.hp < bossHp, "a thrown body makes the boss groggy", report);
        rule.player = schoolBoss.pos - new Vector2(60, 0); rule.Attack(schoolBoss);
        Expect(schoolBoss.velocity.x > 300, "groggy boss can be launched toward a wall", report);
        rule.enemies.Remove(thrown); bossHp = schoolBoss.hp; int walls = rule.wallHits;
        for (int i = 0; i < 60 && schoolBoss.groggy > 0; i++) rule.Tick(.02f, Vector2.zero, false);
        Expect(rule.wallHits > walls && schoolBoss.groggy == 0 && bossHp - schoolBoss.hp >= 2 + rule.wallSkill, "wall slam is the big payoff and ends the opening", report);
        var standard = new AlleyCombat(); standard.Start(); standard.spawning = false;
        var normalEnemy = standard.AddEnemy(standard.player + new Vector2(60, 0)); standard.Attack(normalEnemy);
        var slipper = new AlleyCombat(); slipper.Start(); slipper.spawning = false; slipper.punchKnockbackMultiplier = 1.25f;
        var slipperEnemy = slipper.AddEnemy(slipper.player + new Vector2(60, 0)); slipper.Attack(slipperEnemy);
        Expect(slipperEnemy.velocity.magnitude > normalEnemy.velocity.magnitude * 1.24f, "slipper changes actual punch trajectory strength", report);
        var phone = new AlleyCombat(); phone.Start(); phone.spawning = false; phone.phoneSpecial = true;
        var charging = phone.AddEnemy(phone.player + new Vector2(220, 0), 1); charging.windup = .7f;
        int hp = charging.hp;
        Expect(phone.Special() && charging.velocity.magnitude > 0 && charging.windup == 0 && charging.stun >= 1.5f, "phone interrupts and scatters beyond normal special reach", report);
        Expect(charging.hp == hp && !phone.isFever && phone.charge == 0, "phone trades damage and fever for crowd control", report);
        Expect(!phone.Special(), "phone cannot be spammed without charge", report);
        var pep = new AlleyCombat(); pep.Start(); pep.spawning = false; DadSchoolPrototype.ConfigureGear(pep, 2); pep.health = 50; pep.Special();
        Expect(pep.health == 56 && !pep.isFever, "phone call restores a little health instead of fever", report);
        var scare = new AlleyCombat(); scare.Start(); scare.spawning = false; scare.phoneSpecial = true; scare.groggyBoss = true;
        var scared = scare.AddEnemy(scare.player + new Vector2(150, 0), 2); scare.Special();
        Expect(scared.groggy > 1 && scared.stun >= scared.groggy, "phone call scares the school boss into a groggy opening", report);
        RunTrayChecks(report);
        RunChecks(); AlleySetup.RunChecks();
        File.WriteAllText("Design/dad-polish-checks.txt", report.ToString()); Debug.Log(report.ToString());
    }
    static Fighter Tray(AlleyCombat c, Vector2 pos, int facing)
    { var t = c.AddEnemy(pos); t.shield = true; t.hp = t.maxHp = 4; t.facing = facing; return t; }
    static void RunTrayChecks(StringBuilder report)
    {
        var c = new AlleyCombat(); c.Start(); c.spawning = false;
        var front = Tray(c, c.player + new Vector2(60, 0), -1); c.Attack(front);
        Expect(front.hp == 4 && front.shield && front.velocity == Vector2.zero, "tray blocks a punch from the front", report);
        front.pos = c.player + new Vector2(0, 60); c.Attack(front);
        Expect(!front.shield && front.trayBroken && front.hp <= 2 && front.velocity.magnitude > 300, "punch from below the tray breaks it and launches", report);
        c = new AlleyCombat(); c.Start(); c.spawning = false;
        var brute = Tray(c, c.player + new Vector2(60, 0), -1); int blocked = 0;
        for (int i = 0; i < AlleyCombat.TrayDurability && brute.shield; i++) { brute.pos = c.player + new Vector2(60, 0); brute.velocity = Vector2.zero; c.Attack(brute); if (brute.shield) blocked++; }
        Expect(blocked == AlleyCombat.TrayDurability - 1 && !brute.shield && brute.hp < 4, "frontal punches dent the tray until it gives way", report);
        c = new AlleyCombat(); c.Start(); c.spawning = false;
        var rear = Tray(c, c.player + new Vector2(-60, 0), -1); c.Attack(rear);
        Expect(!rear.shield && rear.hp <= 2, "punch from behind breaks the tray", report);
        c = new AlleyCombat(); c.Start(); c.spawning = false; c.player = new Vector2(120, 690);
        var wallTray = Tray(c, new Vector2(300, 450), -1); var body = c.AddEnemy(new Vector2(250, 450)); body.velocity = new Vector2(700, 0);
        c.Tick(.02f, Vector2.zero, false);
        Expect(wallTray.shield && wallTray.hp == 4 && body.velocity.x < 0, "a body thrown into the tray face bounces off", report);
        c = new AlleyCombat(); c.Start(); c.spawning = false; c.player = new Vector2(120, 690);
        var sideTray = Tray(c, new Vector2(300, 450), -1); var drop = c.AddEnemy(new Vector2(300, 400)); drop.velocity = new Vector2(0, 700);
        c.Tick(.02f, Vector2.zero, false);
        Expect(!sideTray.shield && sideTray.hp <= 2, "a body hitting the tray from the side breaks it", report);
        c = new AlleyCombat(); c.Start(); c.spawning = false; c.player = new Vector2(150, 500);
        var turner = Tray(c, new Vector2(300, 500), -1); turner.stun = 5; c.player = new Vector2(450, 500);
        for (int i = 0; i < 10; i++) c.Tick(.02f, Vector2.zero, false);
        bool stillFacing = turner.facing == -1;
        for (int i = 0; i < 30; i++) c.Tick(.02f, Vector2.zero, false);
        Expect(stillFacing && turner.facing == 1, "tray guy turns around slowly, leaving a rear opening", report);
        c = new AlleyCombat(); c.Start(); c.spawning = false;
        var smashed = Tray(c, c.player + new Vector2(80, 0), -1); c.Special();
        Expect(!smashed.shield, "dad's special smashes trays", report);
        // "아빠 전쟁": every opponent is an adult and some kid's dad (the name tag or its line says whose).
        var ages = new System.Text.RegularExpressions.Regex(@"\((\d+)세\)");
        Func<string, string[]> table = f => (string[])typeof(DadSchoolPrototype).GetField(f, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        foreach (var pair in new[] { new[] { "ProfileNames", "ProfileLines" }, new[] { "EliteTagNames", "EliteTagLines" }, new[] { "BossTagNames", "BossTagLines" } })
        {
            string[] names = table(pair[0]), lines = table(pair[1]);
            for (int i = 0; i < names.Length; i++)
            {
                var age = ages.Match(names[i]);
                Expect(age.Success && int.Parse(age.Groups[1].Value) >= 30 && (names[i] + lines[i]).Contains("아빠"), "opponent is an adult dad: " + names[i] + " / " + lines[i], report);
            }
        }
    }
    static void RunEconomyChecks(StringBuilder report)
    {
        var eco = DadEconomy.Load();
        Expect(eco.TotalStages == 60 && eco.Label(21) == "2-1" && eco.IsMidBoss(10) && eco.IsFloorBoss(20) && !eco.IsBoss(19), "stage numbering: 3 floors x 20, mid-boss 10, floor boss 20", report);
        Expect(Mathf.Abs(eco.RequiredPower(1) - 72) < .01f && Mathf.Abs(eco.RequiredPower(20) - 941.3f) < 1 && Mathf.Abs(eco.RequiredPower(40) - 7589.3f) < 5, "recommended power matches the progression simulation (shared JSON)", report);
        Expect(eco.RequiredPower(21) < eco.RequiredPower(20) && eco.RequiredPower(21) < eco.RequiredPower(22) * .8f, "first stage of a floor relaxes after the boss spike", report);
        string temp = TempSavePath("economy");
        try
        {
            var p = new DadProgress(eco, temp);
            Expect(p.Save.bestStage == 1 && p.Save.currentStage == 1 && Mathf.Abs(p.Power - eco.basePower) < .01f, "fresh save starts at 1-1 with base power", report);
            var win = p.ApplyResult(1, true, 1f, 7);
            Expect(win.firstClear && win.stars == 3 && p.Save.bestStage == 2 && p.Save.currentStage == 2 && Math.Abs(p.Save.coins - eco.ClearCoins(1) - eco.BaseCoins(1)*.5) < .01 && p.Save.lunchboxes == eco.boxesPerClear+1, "first three-star clear pays base and mastery rewards once and unlocks the next stage", report);
            var replay = p.ApplyResult(1, true, .2f, 0);
            Expect(replay.replay && !replay.firstClear && Math.Abs(replay.coins - eco.BaseCoins(1) * eco.replayCoinShare) < .01 && p.Stars(1) == 3 && p.Save.bestStage == 2, "replaying a cleared stage pays the replay share and keeps the best stars", report);
            double before = p.Save.coins; p.ApplyResult(2, false, 0, 0);
            Expect(Math.Abs(p.Save.coins - before - eco.ClearCoins(2) * eco.failCoinShare) < .01 && p.Save.lossStage == 2 && p.Save.lossesAtStage == 1 && p.Save.bestStage == 2, "a loss pays the consolation share and counts toward the stuck assist", report);
            p.ApplyResult(2, false, 0, 0);
            Expect(p.StuckAssist(2) == 1, "no assist before the third loss", report);
            p.ApplyResult(2, false, 0, 0);
            Expect(Mathf.Abs(p.StuckAssist(2) - (1 + eco.stuckAssistStep)) < .001f, "the third loss starts mom's cheer", report);
            for (int i = 0; i < 10; i++) p.ApplyResult(2, false, 0, 0);
            Expect(Mathf.Abs(p.StuckAssist(2) - (1 + eco.stuckAssistCap)) < .001f, "mom's cheer is capped", report);
            p.ApplyResult(2, true, 1, 0);
            Expect(p.StuckAssist(2) == 1 && p.Save.bestStage == 3, "winning clears the assist", report);
            p.Save.coins = 0; Expect(!p.TryUpgrade(0), "an upgrade is refused without coins", report);
            p.Save.coins = p.NextUpgradeCost * 3; float power = p.Power; double cost = p.NextUpgradeCost;
            Expect(p.TryUpgrade(3) && Mathf.Abs(p.Power / power - (1 + eco.upgradePower)) < .001f && p.NextUpgradeCost > cost && p.Save.upgrades[3] == 1, "any upgrade adds the same power and raises the shared cost", report);
            p.Save.bestStage = 20; var boss = p.ApplyResult(20, true, 1, 0);
            Expect(boss.boxes == eco.boxesPerClear + eco.floorBossFirstClearBoxes && Math.Abs(boss.coins - eco.ClearCoins(20)) < .01 && p.Save.bestStage == 21, "floor boss first clear pays boss coins and bonus lunchboxes", report);
            var t0 = new DateTime(2026, 11, 4, 9, 0, 0, DateTimeKind.Utc);
            p.Touch(t0); p.AccrueIdle(t0.AddSeconds(30));
            Expect(!p.HasIdleReward, "under a minute away pays nothing", report);
            p.Touch(t0); p.AccrueIdle(t0.AddHours(20));
            double expected = eco.offlineClearsPerHour * eco.offlineCapHours * eco.BaseCoins(p.Save.bestStage - 1);
            Expect(Math.Abs(p.Save.pendingIdleCoins - expected) < 1 && p.Save.pendingIdleBoxes == (int)(eco.offlineBoxesPerHour * eco.offlineCapHours), "idle reward accrues at the best farm stage and caps at 8 hours", report);
            p.AccrueIdle(t0.AddHours(30));
            Expect(Math.Abs(p.Save.pendingIdleCoins - expected) < 1, "an unclaimed idle chest never grows past the cap", report);
            double coins = p.Save.coins; p.ClaimIdle(2);
            Expect(Math.Abs(p.Save.coins - coins - 2 * expected) < 1 && !p.HasIdleReward, "claiming pays (ad doubles) and empties the idle chest", report);
            p.Persist(); var reloaded = new DadProgress(eco, temp);
            Expect(reloaded.Save.bestStage == p.Save.bestStage && Math.Abs(reloaded.Save.coins - p.Save.coins) < .01 && reloaded.Stars(1) == 3 && reloaded.Save.upgrades[3] == 1, "save round-trips through the file", report);
            File.WriteAllText(temp, "{ broken");
            Expect(new DadProgress(eco, temp).Save.bestStage == 1, "an unreadable save starts fresh instead of crashing", report);
        }
        finally { DeleteTemp(temp); }
    }
    static void RunStageChecks(StringBuilder report)
    {
        var eco = DadEconomy.Load();
        var p1 = StagePlan.For(1, eco);
        Expect(p1.lesson == 1 && p1.WaveTotal == 5 && p1.maxAlive == 3 && p1.chargerShare == 0 && p1.trayShare == 0, "1-1 is a small teaching stage", report);
        Expect(StagePlan.For(4, eco).chargerShare > 0 && StagePlan.For(11, eco).trayShare == 0 && StagePlan.For(12, eco).trayShare > 0 && StagePlan.For(21, eco).trayShare > 0, "chargers from 1-4, tray guys from 1-12 and on every later floor", report);
        var mid = StagePlan.For(10, eco); var floorBoss = StagePlan.For(20, eco);
        Expect(mid.midBoss && !mid.floorBoss && mid.timeLimit == 75 && floorBoss.floorBoss && floorBoss.timeLimit == 90 && StagePlan.For(30, eco).eliteKind == 3, "stage 10 is an elite fight, stage 20 the floor boss", report);
        bool growing = true; int last = 0;
        for (int s = 4; s <= 19; s++) { var plan = StagePlan.For(s, eco); if (plan.midBoss) continue; if (plan.WaveTotal < last) growing = false; last = plan.WaveTotal; }
        Expect(growing && StagePlan.For(41, eco).WaveTotal > StagePlan.For(4, eco).WaveTotal, "regular stages grow in enemy count within and across floors", report);
        var c = NewStageCombat(); var d = new SchoolRunDirector(); d.Begin(c, p1);
        Expect(c.enemies.Count == 1 && c.enemies[0].stun > 0 && (c.enemies[0].pos - c.player).magnitude < 150 && d.Lesson == 0, "the first encounter is one dazed target in front of dad", report);
        c.hits = 1; d.Tick(c); Expect(d.Lesson == 1, "landing a punch advances the lesson", report);
        var c5 = NewStageCombat(); var d5 = new SchoolRunDirector(); var plan5 = StagePlan.For(5, eco); d5.Begin(c5, plan5); bool far = true; int seen = 0;
        for (int i = 0; i < 200 && seen < 4; i++)
        {
            c5.elapsed += .25f; int count = c5.enemies.Count; d5.Tick(c5);
            for (int k = count; k < c5.enemies.Count; k++) { seen++; if ((c5.enemies[k].pos - c5.player).magnitude < SchoolRunDirector.MinSpawnDistance) far = false; }
        }
        Expect(seen >= 4 && far, "enemies walk in from the doors, never next to dad", report);
        c5 = NewStageCombat(); d5 = new SchoolRunDirector(); d5.Begin(c5, plan5);
        for (int i = 0; i < 600 && c5.state == RunState.Fighting; i++) { c5.elapsed += .25f; d5.Tick(c5); c5.enemies.Clear(); }
        Expect(c5.state == RunState.Won && d5.Defeated == plan5.WaveTotal, "clearing every wave wins a regular stage", report);
        var cm = NewStageCombat(); var dm = new SchoolRunDirector(); dm.Begin(cm, mid); cm.elapsed = 2; dm.Tick(cm);
        Expect(dm.Key != null && dm.Key.elite && dm.Key.maxHp == 3 * eco.eliteHpMultiplier && cm.enemies.Count == mid.waves[0] + 1, "the mid-boss elite walks in with two escorts", report);
        cm.enemies.Remove(dm.Key); dm.Tick(cm);
        Expect(cm.state == RunState.Won, "defeating the elite wins the mid-boss stage", report);
        var cb = NewStageCombat(); var db = new SchoolRunDirector(); db.Begin(cb, floorBoss); cb.elapsed = 2; db.Tick(cb);
        Expect(db.Key != null && db.Key.boss && db.Key.hp == floorBoss.bossHp && cb.enemies.FindAll(e => !e.boss).Count == 2, "the floor boss arrives with school HP and two escorts", report);
        cb.enemies.RemoveAll(e => !e.boss); cb.elapsed += SchoolRunDirector.EscortInterval + .1f; db.Tick(cb);
        Expect(cb.enemies.Exists(e => !e.boss), "escorts keep arriving during the boss fight", report);
        var ct = NewStageCombat(); var dt = new SchoolRunDirector(); dt.Begin(ct, StagePlan.For(12, eco)); int intro = 0; bool tray = false;
        for (int i = 0; i < 600 && ct.state == RunState.Fighting; i++)
        {
            ct.elapsed += .25f; dt.Tick(ct);
            foreach (CombatEvent ev in ct.events) if (ev.text == "쟁반 짱 등장") intro++;
            ct.events.Clear(); if (ct.enemies.Exists(e => e.shield)) tray = true; ct.enemies.Clear();
        }
        Expect(tray && intro == 1, "1-12 introduces the tray guy once", report);
        var cs = NewStageCombat(); DadSchoolPrototype.ApplyRatio(cs, eco, 2); var strong = cs.AddEnemy(cs.player + new Vector2(200, 0));
        var cw = NewStageCombat(); DadSchoolPrototype.ApplyRatio(cw, eco, .5f); var weak = cw.AddEnemy(cw.player + new Vector2(200, 0));
        Expect(strong.maxHp > 3 && weak.maxHp < 3 && cs.incomingDamageScale > 1.7f * eco.combatDamageBase && cw.incomingDamageScale < .6f * eco.combatDamageBase, "the power ratio scales enemy HP and the damage dad takes", report);
    }

    [MenuItem("Dad School Boss/Run Stage Simulations")]
    public static void RunFlowCheck()
    {
        var report = new StringBuilder("Stage simulations at 20% above recommended power (scripted skilled player, not a fun test)\n");
        try
        {
            foreach (int stage in new[] { 1, 2, 3, 10, 20 })
            {
                SimResult r = SimulateStage(stage, 1.2f, 731 + stage, true);
                string label = DadEconomy.Load().Label(stage);
                report.AppendLine(label + ": " + r.Line());
                Expect(r.state == RunState.Won, label + " can be won at 20% above recommended power", report);
            }
        }
        finally { File.WriteAllText("Design/dad-full-run-check.txt", report.ToString()); }
        Debug.Log(report.ToString());
    }

    // Scripted players are a balance probe, not a fun test: "skilled" sidesteps telegraphed charges, flanks trays and
    // lines escorts up with the boss; "novice" walks straight at the nearest enemy and fires the special as soon as it is full.
    public struct SimResult
    {
        public RunState state; public float end, bossAt, bossFight, hpLeft, minHp, damage, feverTime;
        public int kills, wall, chain, choices, specials, level, trayBlocks, trayBreaks;
        public string Line()
        {
            return state + " t=" + end.ToString("F1") + " boss@" + (bossAt < 0 ? "-" : bossAt.ToString("F1")) + " HP=" + hpLeft.ToString("F0") + " dmg=" + damage.ToString("F0")
                + " kills=" + kills + " wall=" + wall + " chain=" + chain + " picks=" + choices + " specials=" + specials + " tray=" + trayBreaks + "/" + trayBlocks + "blk";
        }
    }
    public static SimResult SimulateStage(int stage, float powerOverRequired, int seed, bool skilled, int gear = 0)
    {
        var eco = DadEconomy.Load(); var plan = StagePlan.For(stage, eco);
        UnityEngine.Random.State previous = UnityEngine.Random.state;
        var r = new SimResult { bossAt = -1, minHp = 100 };
        try
        {
            UnityEngine.Random.InitState(seed);
            var combat = new AlleyCombat(0); combat.SetArena(new Rect(64, 240, 412, 520)); combat.hazardSpawnClock = 9999;
            combat.player = new Vector2(270 + UnityEngine.Random.Range(-40f, 40f), 570 + UnityEngine.Random.Range(-40f, 40f)); DadSchoolPrototype.ConfigureGear(combat, gear);
            DadSchoolPrototype.ApplyRatio(combat, eco, 1f / powerOverRequired);
            // Human-ish noise: the novice wobbles, the skilled player notices telegraphs late by a per-run amount.
            float reactBelow = skilled ? UnityEngine.Random.Range(.25f, .65f) : 0, wobble = 0, wobbleClock = 0;
            var director = new SchoolRunDirector(); director.Begin(combat, plan); combat.Start();
            int frames = Mathf.CeilToInt(plan.timeLimit / .02f) + 50;
            for (int frame = 0; frame < frames && combat.elapsed < plan.timeLimit; frame++)
            {
                if (combat.state == RunState.Won || combat.state == RunState.Lost) break;
                if (combat.state == RunState.Choosing) { combat.Choose(skilled ? 0 : UnityEngine.Random.Range(0, 3)); r.choices++; continue; }
                Fighter nearest = null; float distance = float.MaxValue;
                foreach (Fighter e in combat.enemies) { float dd = (e.pos - combat.player).magnitude; if (dd < distance) { nearest = e; distance = dd; } }
                Vector2 movement = Vector2.zero;
                Fighter boss = director.Key != null && director.Key.boss && combat.enemies.Contains(director.Key) ? director.Key : null;
                if (nearest != null)
                {
                    Vector2 delta = nearest.pos - combat.player;
                    movement = distance > 75 ? delta.normalized : Vector2.zero;
                    if (skilled && boss == null && nearest.shield)
                    {
                        // Flank the tray: stand above or below it instead of in front of it.
                        Vector2 flank = nearest.pos + new Vector2(0, combat.player.y <= nearest.pos.y ? -78 : 78);
                        if (flank.y < combat.arena.yMin + 14 || flank.y > combat.arena.yMax - 14) flank = nearest.pos + new Vector2(0, combat.player.y <= nearest.pos.y ? 78 : -78);
                        Vector2 toFlank = flank - combat.player;
                        movement = toFlank.magnitude > 14 ? toFlank.normalized : Vector2.zero;
                    }
                    if (skilled && boss != null)
                    {
                        // Line an escort up between dad and the boss; once the boss is groggy, go hit it directly.
                        Fighter escort = null; float best = float.MaxValue;
                        foreach (Fighter e in combat.enemies) if (!e.boss) { float dd = (e.pos - combat.player).magnitude; if (dd < best) { best = dd; escort = e; } }
                        Vector2 goal = boss.groggy > 0 || escort == null ? boss.pos : escort.pos + (escort.pos - boss.pos).normalized * 62;
                        goal = new Vector2(Mathf.Clamp(goal.x, combat.arena.xMin + 14, combat.arena.xMax - 14), Mathf.Clamp(goal.y, combat.arena.yMin + 14, combat.arena.yMax - 14));
                        Vector2 toGoal = goal - combat.player;
                        movement = toGoal.magnitude > (boss.groggy > 0 ? 70 : 16) ? toGoal.normalized : Vector2.zero;
                    }
                    if (skilled)
                        foreach (Fighter e in combat.enemies)
                            if (e.kind >= 1 && e.windup > 0 && e.windup < reactBelow && (e.pos - combat.player).magnitude < 260)
                                movement = new Vector2(-e.aim.y, e.aim.x);
                    if (!skilled)
                    {
                        wobbleClock -= .02f;
                        if (wobbleClock <= 0) { wobbleClock = .3f; wobble = UnityEngine.Random.Range(-35f, 35f); }
                        movement = (Vector2)(Quaternion.Euler(0, 0, wobble) * movement);
                    }
                }
                bool request = combat.charge >= 100 && (!skilled || combat.enemies.Count >= 3 || director.BossReleased);
                float hpBefore = combat.health;
                combat.Tick(.02f, movement, request);
                if (request && combat.charge < 100) r.specials++;
                if (combat.isFever) r.feverTime += .02f;
                r.damage += Mathf.Max(0, hpBefore - combat.health); r.minHp = Mathf.Min(r.minHp, combat.health);
                foreach (CombatEvent ev in combat.events) { if (ev.text == "쟁반 막기!" || ev.text == "팅!") r.trayBlocks++; else if (ev.text == "쟁반 박살!") r.trayBreaks++; }
                bool hadBoss = director.BossReleased; director.Tick(combat);
                if (!hadBoss && director.BossReleased) r.bossAt = combat.elapsed;
                combat.events.Clear();
            }
            r.state = combat.state == RunState.Fighting ? RunState.Lost : combat.state; // time limit counts as a loss
            r.end = combat.elapsed; r.bossFight = r.bossAt < 0 ? 0 : combat.elapsed - r.bossAt; r.hpLeft = combat.health;
            r.kills = combat.kills; r.wall = combat.wallHits; r.chain = combat.bestChain; r.level = combat.level;
        }
        finally { UnityEngine.Random.state = previous; }
        return r;
    }
    // Headless entry point: Unity -batchmode -projectPath <p> -executeMethod DadPrototypeSetup.BatchChecks -logFile <log>
    public static void BatchChecks()
    {
        int code = 0;
        try { RunPolishChecks(); RunFlowCheck(); RunBalanceProbe(); Debug.Log("DAD_BATCH_OK"); }
        catch (Exception e) { Debug.LogException(e); code = 1; }
        EditorApplication.Exit(code);
    }
    // Real combat win rates at a given power ratio next to the design model's win chance: the calibration table for
    // plan v5 step 9 (combat must win about as often as DadEconomy.ModelWinChance predicts).
    [MenuItem("Dad School Boss/Run Balance Probe")]
    public static void RunBalanceProbe()
    {
        var eco = DadEconomy.Load();
        const int seeds = 6;
        var report = new StringBuilder("Balance probe: real combat win rate vs design model, " + seeds + " seeds per cell (scripted players, not a fun test)\n");
        report.AppendLine("stage | dad power / recommended | model | skilled win | novice win | skilled avg time | novice avg HP left");
        foreach (int stage in new[] { 1, 4, 9, 10, 15, 19, 20, 25, 30, 40 })
            foreach (float ratio in new[] { .85f, 1f, 1.2f })
            {
                int skilledWins = 0, noviceWins = 0; float time = 0, hp = 0;
                for (int seed = 0; seed < seeds; seed++)
                {
                    SimResult a = SimulateStage(stage, ratio, 101 + seed * 37, true); if (a.state == RunState.Won) skilledWins++; time += a.end;
                    SimResult b = SimulateStage(stage, ratio, 101 + seed * 37, false); if (b.state == RunState.Won) noviceWins++; hp += b.hpLeft;
                }
                report.AppendLine(eco.Label(stage) + (eco.IsFloorBoss(stage) ? " (boss)" : eco.IsMidBoss(stage) ? " (elite)" : "") + " | " + ratio.ToString("F2") + " | " + (100 * eco.ModelWinChance(ratio, 1)).ToString("F0") + "% | "
                    + (100 * skilledWins / seeds) + "% | " + (100 * noviceWins / seeds) + "% | " + (time / seeds).ToString("F1") + "s | " + (hp / seeds).ToString("F0"));
            }
        File.WriteAllText("Design/dad-balance-probe.txt", report.ToString()); Debug.Log(report.ToString());
    }

}
