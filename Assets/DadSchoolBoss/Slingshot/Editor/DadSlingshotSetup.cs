using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Prototype B tools: builds Assets/DadSlingshot.unity, checks the slingshot rules headless, runs a simple aiming bot over
// the four stages for pacing, and captures the screens. Has its own command file so it never competes with
// DadPrototypeSetup (Library/DadSlingshotCommand.txt: setup | open | play | stop | checks | bot | start:<i> | audit:<dir>).
[InitializeOnLoad]
public static class DadSlingshotSetup
{
    const string ScenePath = "Assets/DadSlingshot.unity", CommandPath = "Library/DadSlingshotCommand.txt";
    static readonly Queue<KeyValuePair<int, Action>> steps = new Queue<KeyValuePair<int, Action>>();
    static int nextStepFrame;
    static DadSlingshotSetup() { EditorApplication.update += Poll; }
    static void Step(int waitFrames, Action action) { if (steps.Count == 0) nextStepFrame = Time.frameCount + waitFrames; steps.Enqueue(new KeyValuePair<int, Action>(waitFrames, action)); }

    static void Poll()
    {
        if (steps.Count > 0 && EditorApplication.isPlaying && Time.frameCount >= nextStepFrame)
        {
            var step = steps.Dequeue();
            try { step.Value(); } catch (Exception e) { Debug.LogException(e); }
            if (steps.Count > 0) nextStepFrame = Time.frameCount + steps.Peek().Key;
        }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(CommandPath)) return;
        string command = File.ReadAllText(CommandPath).Trim();
        if (command.Length == 0) return;
        File.Delete(CommandPath);
        try
        {
            if (command == "setup") Setup();
            else if (command == "open") Open();
            else if (command == "play") { if (!EditorApplication.isPlaying && Open()) EditorApplication.isPlaying = true; }
            else if (command == "stop") EditorApplication.isPlaying = false;
            else if (command == "checks") RunChecks();
            else if (command == "bot") RunBot();
            else if (command.StartsWith("start:") && EditorApplication.isPlaying) Game().StartStage(int.Parse(command.Substring(6)));
            else if (command.StartsWith("audit:") && EditorApplication.isPlaying) QueueAudit(command.Substring(6));
            else if (command.StartsWith("aspect:")) SetGameViewSize(command.Substring(7));
            else if (command.StartsWith("walls:")) DadSlingshotPrototype.DebugWalls = command.EndsWith("on");
            Debug.Log("SLING_COMMAND_OK: " + command);
        }
        catch (Exception e) { Debug.LogException(e); }
    }
    static DadSlingshotPrototype Game() { return UnityEngine.Object.FindObjectOfType<DadSlingshotPrototype>(); }
    // Game view resolution for aspect captures: "1080x1920", "1080x2340", or "restore" (the selection before the first switch).
    // Unity keeps these sizes internal, so this goes through reflection; it only touches the editor's Game view.
    static int savedSizeIndex = -1;
    static void SetGameViewSize(string spec)
    {
        var asm = typeof(Editor).Assembly; var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var viewType = asm.GetType("UnityEditor.GameView"); var view = EditorWindow.GetWindow(viewType);
        var indexProp = viewType.GetProperty("selectedSizeIndex", flags);
        if (spec == "restore") { if (savedSizeIndex >= 0) viewType.GetMethod("SizeSelectionCallback", flags).Invoke(view, new object[] { savedSizeIndex, null }); savedSizeIndex = -1; return; }
        if (savedSizeIndex < 0) savedSizeIndex = (int)indexProp.GetValue(view);
        string[] wh = spec.Split('x'); int w = int.Parse(wh[0]), h = int.Parse(wh[1]);
        var sizesType = asm.GetType("UnityEditor.GameViewSizes");
        var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(instance, new object[] { (int)GameViewSizeGroupType.Standalone });
        var sizeType = asm.GetType("UnityEditor.GameViewSize"); var kindType = asm.GetType("UnityEditor.GameViewSizeType");
        int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null), found = -1;
        for (int i = 0; i < total; i++)
        {
            var size = group.GetType().GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
            if ((int)sizeType.GetProperty("width").GetValue(size) == w && (int)sizeType.GetProperty("height").GetValue(size) == h) { found = i; break; }
        }
        if (found < 0)
        {
            var size = sizeType.GetConstructor(new[] { kindType, typeof(int), typeof(int), typeof(string) }).Invoke(new object[] { Enum.ToObject(kindType, 1), w, h, "Dad " + spec });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size }); found = total;
        }
        viewType.GetMethod("SizeSelectionCallback", flags).Invoke(view, new object[] { found, null });
    }

    [MenuItem("Dad School Boss/Slingshot B/Prepare Scene")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play Mode before preparing the slingshot scene."); return; }
        // Built additively and closed again, so whatever scene is open stays untouched. Build settings are not changed.
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var camera = new GameObject("Main Camera"); camera.tag = "MainCamera";
        var cam = camera.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.orthographic = true; camera.AddComponent<AudioListener>();
        var game = new GameObject("Dad Slingshot Prototype"); game.AddComponent<DadSlingshotPrototype>();
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camera, scene);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(game, scene);
        EditorSceneManager.SaveScene(scene, ScenePath); EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.Refresh(); Debug.Log("SLING_SETUP_READY: " + ScenePath);
    }
    [MenuItem("Dad School Boss/Slingshot B/Open Prototype")]
    public static bool Open()
    {
        if (!File.Exists(ScenePath)) Setup();
        var active = EditorSceneManager.GetActiveScene();
        if (active.path == ScenePath) return true;
        if (active.isDirty) { Debug.LogWarning("The open scene has unsaved changes; save it before opening the slingshot prototype."); return false; }
        EditorSceneManager.OpenScene(ScenePath); return true;
    }

    // ---- headless rule checks and pacing bot (logic in SlingshotChecks, shared with the out-of-editor harness) ----------
    [MenuItem("Dad School Boss/Slingshot B/Run Checks")]
    public static void RunChecks() { string report = SlingshotChecks.RunChecks() + AssetChecks(); File.WriteAllText("Design/dad-slingshot-checks.txt", report); Debug.Log(report); }
    // V13 pack (Art/V13Slingshot/README.md) as the runtime sees it: every prepared file under Resources/DadV13 with its import rule.
    static string AssetChecks()
    {
        var report = new System.Text.StringBuilder();
        string[] textures = { "corridor_floor1_long", "dad_pull_0", "dad_flight_0", "dad_flight_1", "dad_bounce_0", "dad_land_0", "wind", "swoosh", "rage_0", "rage_1", "drag_hand", "wall_launch", "chain_launch", "rage_launch" };
        string[] sounds = { "sfx_pull_tension", "sfx_launch_whoosh", "sfx_wall_boing", "sfx_landing_skid" };
        var loaded = new System.Collections.Generic.HashSet<string>();
        foreach (Texture2D t in Resources.LoadAll<Texture2D>("DadV13")) loaded.Add(t.name);
        foreach (AudioClip a in Resources.LoadAll<AudioClip>("DadV13")) loaded.Add(a.name);
        bool all = true; foreach (string n in textures) all &= loaded.Contains(n); foreach (string n in sounds) all &= loaded.Contains(n);
        if (!all) throw new Exception("SLING_CHECK_FAILED: V13 pack resolves through Resources/DadV13");
        report.AppendLine("PASS V13 pack: 14 images and 4 sounds resolve through Resources/DadV13");
        var bg = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV13/bg/corridor_floor1_long.png");
        var pose = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV13/chars/dad_flight_0.png");
        var hand = (TextureImporter)AssetImporter.GetAtPath("Assets/DadSchoolBoss/Resources/DadV13/ui/drag_hand.png");
        if (bg == null || pose == null || hand == null || bg.alphaIsTransparency || bg.maxTextureSize != 4096 || pose.maxTextureSize != 1024 || !pose.alphaIsTransparency || pose.mipmapEnabled || hand.maxTextureSize != 256)
            throw new Exception("SLING_CHECK_FAILED: V13 import rules");
        report.AppendLine("PASS V13 import rules: opaque corridor at native 1080x2340 (cap 4096), alpha poses and effects at native size (cap 1024) without mips, 256 hand and icons");
        return report.ToString();
    }
    [MenuItem("Dad School Boss/Slingshot B/Run Pacing Bot")]
    public static void RunBot() { string report = SlingshotChecks.RunBot(); File.WriteAllText("Design/dad-slingshot-bot.txt", report); Debug.Log(report); }

    // ---- screen audit -----------------------------------------------------------------------------------------------
    static void QueueAudit(string dir)
    {
        Directory.CreateDirectory(dir);
        var g = Game(); Action<string> shot = name => ScreenCapture.CaptureScreenshot(Path.Combine(dir, name + ".png"), 4);
        var stars = new int[g.StageCount]; for (int i = 0; i < stars.Length; i++) stars[i] = g.BestStars(i);   // staged results must not count
        Step(1, () => g.ShowMenu()); Step(6, () => shot("b01-menu"));
        Step(1, () => g.StartStage(0)); Step(150, () => shot("b02-first-shot-hint"));
        Step(1, () => { g.StartStage(1); }); Step(150, () => { var d = g.Sim.dad.pos; g.DebugAim(d + new Vector2(0, -40), d + new Vector2(70, 100)); }); Step(6, () => shot("b03-aiming"));
        Step(1, () => g.DebugRelease()); Step(8, () => shot("b04-flying"));
        Step(14, () => shot("b05-impact"));
        Step(40, () => shot("b06-after-shot"));
        Step(1, () => g.StartStage(2)); Step(420, () => shot("b07-chair-and-tray"));
        Step(1, () => g.StartStage(3)); Step(300, () => shot("b07b-dodgeball"));
        Step(1, () => g.StartStage(4)); Step(150, () => shot("b07c-big-dad"));
        Step(1, () => { g.StartStage(1); g.Sim.ragePending = true; }); Step(40, () => shot("b07d-rage-ready"));
        // A dad thrown into each side's second locker run, to see the dents on the long corridor's flat locker strips.
        Step(1, () => { g.StartStage(1); var a = g.Sim.Add(SlingshotSim.Kind.Tray, DadSlingshotPrototype.Arena.xMin + 60, 520); a.vel = new Vector2(-900, 0); var b = g.Sim.Add(SlingshotSim.Kind.Tray, DadSlingshotPrototype.Arena.xMax - 60, 520); b.vel = new Vector2(900, 0); });
        Step(30, () => shot("b07e-locker-dents"));
        Step(1, () => g.StartStage(5)); Step(120, () => { var boss = g.Sim.Boss; if (boss != null) { boss.groggy = 3; boss.cooldown = 5; } }); Step(4, () => shot("b08-boss-groggy"));
        // Results wait out the finisher (about a second of slow motion) before the card comes up.
        Step(1, () => { g.StartStage(1); g.Sim.shots = 3; g.Sim.bestChain = 6; g.Sim.foes.Clear(); g.Sim.phase = SlingshotSim.Phase.Won; }); Step(100, () => shot("b09-result-win"));
        Step(1, () => { g.StartStage(2); g.Sim.dad.health = 0; g.Sim.phase = SlingshotSim.Phase.Lost; }); Step(100, () => shot("b10-result-lose"));
        Step(2, () =>
        {
            for (int i = 0; i < stars.Length; i++) { if (stars[i] > 0) PlayerPrefs.SetInt("DadSlingshot.Stars." + i, stars[i]); else PlayerPrefs.DeleteKey("DadSlingshot.Stars." + i); }
            PlayerPrefs.Save(); g.ShowMenu(); Debug.Log("SLING_AUDIT_DONE: " + dir);
        });
    }
}
