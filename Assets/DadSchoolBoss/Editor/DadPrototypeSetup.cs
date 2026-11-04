using System;
using System.IO;
using System.Text;
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
    static void Poll()
    {
        if (pump && EditorApplication.isPlaying) { EditorApplication.QueuePlayerLoopUpdate(); UnityEditorInternal.InternalEditorUtility.RepaintAllViews(); }
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
            else if (command == "playchecks") RunPlayChecks();
            else if (command == "polishchecks") RunPolishChecks();
            else if (command == "bake") BakeArt();
            else if (command == "flowcheck") RunFlowCheck();
            else if (command == "balance") RunBalanceProbe();
            else if (command == "skipboss" && EditorApplication.isPlaying) UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>().Combat.elapsed = SchoolRunDirector.BossDeadline; // debug: jump to the boss
            else if (command == "refresh") AssetDatabase.Refresh();
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
        PlayerSettings.productName = "아빠가 학교 짱을 너무 잘 잡음";
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
    [MenuItem("Dad School Boss/Run Play Mode Checks")]
    public static void RunPlayChecks()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play Mode is required");
        var game = UnityEngine.Object.FindObjectOfType<DadSchoolPrototype>();
        var report = new StringBuilder("Dad School Boss Play Mode integration checks\n");
        string[] keys = { "DadV2.BestChain", "DadV2.Wins", "DadV2.Gear", "DadV2.TotalWallHits" };
        int[] values = new int[keys.Length]; bool[] exists = new bool[keys.Length];
        for (int i = 0; i < keys.Length; i++) { exists[i] = PlayerPrefs.HasKey(keys[i]); values[i] = PlayerPrefs.GetInt(keys[i]); }
        int originalGear = game.Gear;
        try
        {
            game.StartRun();
            Expect(!game.Home && game.Combat.state == RunState.Fighting, "start enters live battle", report);
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
            game.SelectGear(1); game.StartRun();
            Expect(game.Combat.chainSkill == 1 && game.Combat.wallSkill == 0, "unlocked lunchbox changes the next run build", report);
            Expect(game.Combat.elapsed == 0 && game.Combat.health == game.Combat.maxHealth, "retry resets health and timer", report);
            game.SelectGear(2); game.StartRun();
            Expect(game.Combat.phoneSpecial && game.Combat.chainSkill == 0, "phone replaces the special instead of stacking lunchbox", report);
            game.Combat.health = 0; game.Combat.state = RunState.Lost; game.SendMessage("Update");
            Expect(game.Wins == oldWins + 1, "defeat does not award a win", report);
            game.GoHome(); Expect(game.Home && !game.Paused, "home returns to a usable menu", report);
        }
        finally
        {
            game.SelectGear(originalGear);
            for (int i = 0; i < keys.Length; i++) { if (exists[i]) PlayerPrefs.SetInt(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]); }
            PlayerPrefs.Save(); game.ReloadProgress(); game.GoHome();
        }
        File.WriteAllText("Design/dad-prototype-playchecks.txt", report.ToString()); Debug.Log(report.ToString());
    }
    [MenuItem("Dad School Boss/Run Polish Checks")]
    public static void RunPolishChecks()
    {
        var report = new StringBuilder("School first-minute and equipment checks\n");
        var combat = new AlleyCombat(); combat.SetArena(new Rect(64, 240, 412, 520));
        combat.player = new Vector2(270, 570); combat.Start();
        var director = new SchoolRunDirector(); director.Begin(combat);
        Expect(!combat.spawning && combat.enemies.Count == 1 && combat.enemies[0].kind == 0, "first encounter isolates one basic target", report);
        combat.hits = 1; director.Tick(combat); Expect(director.Lesson == 1, "landing a punch advances to wall lesson", report);
        combat.wallHits = 1; director.Tick(combat); Expect(director.Lesson == 2, "wall contact advances to chain lesson", report);
        combat.chainHits = 1; director.Tick(combat); Expect(director.Lesson == 3, "real collision completes the chain lesson", report);
        combat.enemies.Clear(); combat.elapsed = 6; director.Tick(combat);
        Expect(combat.enemies.Count == 2 && combat.enemies.TrueForAll(e => e.kind == 0), "early chain wave uses two aligned basic targets", report);
        for (int i = 0; i < 2; i++) { combat.enemies.Clear(); combat.elapsed = 28 + i * 5; director.Tick(combat); }
        Expect(combat.enemies.Exists(e => e.kind == 1), "charge enemy is introduced after the teaching beats", report);
        Expect(combat.targetKills == SchoolRunDirector.KillTarget && combat.nextXp == 3 * SchoolRunDirector.XpCurveScale, "school run uses its own kill target and slower level curve", report);
        combat.enemies.Clear(); combat.elapsed = SchoolRunDirector.BossFloor; combat.kills = 0; director.Tick(combat);
        Expect(!director.BossReleased, "boss waits for the kill target after the floor time", report);
        combat.enemies.Clear(); combat.elapsed = SchoolRunDirector.BossDeadline; director.Tick(combat);
        Expect(director.BossReleased && combat.enemies.Exists(e => e.boss && e.hp == SchoolRunDirector.BossHp), "boss appears by the fallback deadline with school HP", report);
        Expect(combat.enemies.FindAll(e => !e.boss).Count == 2, "boss arrives with two escorts to throw", report);
        int bossCount = combat.enemies.FindAll(e => e.boss).Count; director.Tick(combat);
        Expect(combat.enemies.FindAll(e => e.boss).Count == bossCount, "boss is never spawned twice", report);
        combat.enemies.RemoveAll(e => !e.boss); combat.elapsed += SchoolRunDirector.EscortInterval + .1f; director.Tick(combat);
        Expect(combat.enemies.Exists(e => !e.boss), "escorts keep arriving during the boss fight", report);
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
        var run = new AlleyCombat(); run.SetArena(new Rect(64, 240, 412, 520)); run.player = new Vector2(270, 570); run.Start();
        var director = new SchoolRunDirector(); director.Begin(run); float introducedAt = -1;
        for (int i = 0; i < 28 && introducedAt < 0; i++)
        { run.enemies.Clear(); run.elapsed = 5 + i * 4; director.Tick(run); if (run.enemies.Exists(e => e.shield)) introducedAt = run.elapsed; }
        Expect(introducedAt >= SchoolRunDirector.TrayIntro && introducedAt < SchoolRunDirector.BossFloor && director.TrayOnField, "tray guy is introduced after the charger and before the boss", report);
    }
    [MenuItem("Dad School Boss/Run Full Run Simulation")]
    public static void RunFlowCheck()
    {
        var report = new StringBuilder("Deterministic full-run simulation (not a human fun test)\n");
        try
        {
            SimResult r = SimulateRun(731, true);
            report.AppendLine("Result: " + r.Line());
            Expect(r.choices > 0, "first skill choice is reachable through normal combat", report);
            Expect(r.bossAt >= 0, "boss is reachable through a continuous run", report);
            Expect(r.state == RunState.Won, "a full run can be won without editing health or enemy HP", report);
        }
        finally { File.WriteAllText("Design/dad-full-run-check.txt", report.ToString()); }
        Debug.Log(report.ToString());
    }

    // Scripted players are a balance probe, not a fun test: "skilled" sidesteps telegraphed charges and saves the
    // special for crowds or the boss; "novice" walks straight at the nearest enemy and fires the special as soon as it is full.
    public struct SimResult
    {
        public RunState state; public float end, bossAt, bossFight, hpLeft, minHp, damage, feverTime;
        public int kills, wall, chain, choices, specials, level, trayBlocks, trayBreaks;
        public System.Collections.Generic.Dictionary<string, int> bossDamage;
        public string Line()
        {
            return state + " t=" + end.ToString("F1") + " boss@" + (bossAt < 0 ? "-" : bossAt.ToString("F1")) + " bossFight=" + bossFight.ToString("F1")
                + " HP=" + hpLeft.ToString("F0") + " min=" + minHp.ToString("F0") + " dmg=" + damage.ToString("F0") + " kills=" + kills + " wall=" + wall
                + " chain=" + chain + " Lv=" + level + " specials=" + specials + " fever=" + feverTime.ToString("F1") + "s tray=" + trayBreaks + "/" + trayBlocks + "blk";
        }
    }
    public static SimResult SimulateRun(int seed, bool skilled, int gear = 0)
    {
        UnityEngine.Random.State previous = UnityEngine.Random.state;
        var r = new SimResult { bossAt = -1, minHp = 100, bossDamage = new System.Collections.Generic.Dictionary<string, int>() };
        try
        {
            UnityEngine.Random.InitState(seed);
            var combat = new AlleyCombat(0); combat.SetArena(new Rect(64, 240, 412, 520)); combat.hazardSpawnClock = 9999;
            combat.player = new Vector2(270, 570); DadSchoolPrototype.ConfigureGear(combat, gear); combat.Start();
            var director = new SchoolRunDirector(); director.Begin(combat);
            for (int frame = 0; frame < 14000 && combat.elapsed < DadSchoolPrototype.RunLimit; frame++)
            {
                if (combat.state == RunState.Won || combat.state == RunState.Lost) break;
                if (combat.state == RunState.Choosing) { combat.Choose(skilled ? 0 : UnityEngine.Random.Range(0, 3)); r.choices++; continue; }
                Fighter nearest = null; float distance = float.MaxValue;
                foreach (Fighter e in combat.enemies) { float d = (e.pos - combat.player).magnitude; if (d < distance) { nearest = e; distance = d; } }
                Vector2 movement = Vector2.zero;
                Fighter boss = combat.enemies.Find(e => e.boss);
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
                        foreach (Fighter e in combat.enemies) if (!e.boss) { float d = (e.pos - combat.player).magnitude; if (d < best) { best = d; escort = e; } }
                        Vector2 goal = boss.groggy > 0 || escort == null ? boss.pos : escort.pos + (escort.pos - boss.pos).normalized * 62;
                        goal = new Vector2(Mathf.Clamp(goal.x, combat.arena.xMin + 14, combat.arena.xMax - 14), Mathf.Clamp(goal.y, combat.arena.yMin + 14, combat.arena.yMax - 14));
                        Vector2 toGoal = goal - combat.player;
                        movement = toGoal.magnitude > (boss.groggy > 0 ? 70 : 16) ? toGoal.normalized : Vector2.zero;
                    }
                    if (skilled)
                        foreach (Fighter e in combat.enemies)
                            if (e.kind >= 1 && e.windup > 0 && (e.pos - combat.player).magnitude < 260)
                                movement = new Vector2(-e.aim.y, e.aim.x);
                }
                bool full = combat.charge >= 100;
                bool request = full && (!skilled || combat.enemies.Count >= 3 || director.BossReleased);
                float hpBefore = combat.health; int bossHpBefore = boss != null ? boss.hp : 0;
                combat.Tick(.02f, movement, request);
                if (boss != null && boss.hp < bossHpBefore)
                {
                    // Attribute the boss HP drop to the closest damage label emitted this frame (diagnostic only).
                    string label = "?"; float closest = 80;
                    foreach (CombatEvent ev in combat.events) { float d = (ev.pos - boss.pos).magnitude; if (d < closest && ev.text != "제압" && !ev.text.StartsWith("+")) { closest = d; label = ev.text; } }
                    if (label.StartsWith("−")) label = "−n(punch/special)";
                    r.bossDamage.TryGetValue(label, out int sum); r.bossDamage[label] = sum + bossHpBefore - Mathf.Max(0, boss.hp);
                }
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
    [MenuItem("Dad School Boss/Run Balance Probe")]
    public static void RunBalanceProbe()
    {
        var report = new StringBuilder("Balance probe: 8 seeds x 2 scripted players x 3 gears (not a human fun test)\n");
        string[] gearNames = { "slipper", "lunchbox", "phone" };
        for (int gear = 0; gear < 3; gear++) foreach (bool skilled in new[] { true, false })
        {
            int wins = 0; float end = 0, bossFight = 0, hp = 0, damage = 0, specials = 0, fever = 0, bossAt = 0;
            var breakdown = new System.Collections.Generic.Dictionary<string, int>();
            report.AppendLine("== " + gearNames[gear] + " / " + (skilled ? "skilled" : "novice"));
            for (int seed = 0; seed < 8; seed++)
            {
                SimResult r = SimulateRun(101 + seed * 37, skilled, gear);
                report.AppendLine("  seed " + seed + ": " + r.Line());
                if (r.state == RunState.Won) wins++;
                foreach (var kv in r.bossDamage) { breakdown.TryGetValue(kv.Key, out int sum); breakdown[kv.Key] = sum + kv.Value; }
                end += r.end; bossFight += r.bossFight; hp += r.hpLeft; damage += r.damage; specials += r.specials; fever += r.feverTime; bossAt += r.bossAt;
            }
            report.AppendLine("  AVG win=" + wins + "/8 t=" + (end / 8).ToString("F1") + " boss@" + (bossAt / 8).ToString("F1") + " bossFight=" + (bossFight / 8).ToString("F1")
                + " HP=" + (hp / 8).ToString("F0") + " dmg=" + (damage / 8).ToString("F0") + " specials=" + (specials / 8).ToString("F1") + " fever=" + (fever / 8).ToString("F1") + "s");
            var parts = new System.Collections.Generic.List<string>();
            foreach (var kv in breakdown) parts.Add(kv.Key + ":" + (kv.Value / 8f).ToString("F1"));
            report.AppendLine("  boss HP lost per run by source: " + string.Join("  ", parts));
        }
        File.WriteAllText("Design/dad-balance-probe.txt", report.ToString()); Debug.Log(report.ToString());
    }
}
