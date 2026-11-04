using System;
using System.Collections.Generic;
using System.IO;
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
            Debug.Log("SLING_COMMAND_OK: " + command);
        }
        catch (Exception e) { Debug.LogException(e); }
    }
    static DadSlingshotPrototype Game() { return UnityEngine.Object.FindObjectOfType<DadSlingshotPrototype>(); }

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
    public static void RunChecks() { string report = SlingshotChecks.RunChecks(); File.WriteAllText("Design/dad-slingshot-checks.txt", report); Debug.Log(report); }
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
        Step(1, () => g.StartStage(3)); Step(120, () => { var boss = g.Sim.Boss; if (boss != null) { boss.groggy = 3; boss.cooldown = 5; } }); Step(4, () => shot("b08-boss-groggy"));
        Step(1, () => { g.StartStage(1); g.Sim.shots = 3; g.Sim.bestChain = 4; g.Sim.foes.Clear(); g.Sim.phase = SlingshotSim.Phase.Won; }); Step(6, () => shot("b09-result-win"));
        Step(1, () => { g.StartStage(2); g.Sim.dad.health = 0; g.Sim.phase = SlingshotSim.Phase.Lost; }); Step(6, () => shot("b10-result-lose"));
        Step(2, () =>
        {
            for (int i = 0; i < stars.Length; i++) { if (stars[i] > 0) PlayerPrefs.SetInt("DadSlingshot.Stars." + i, stars[i]); else PlayerPrefs.DeleteKey("DadSlingshot.Stars." + i); }
            PlayerPrefs.Save(); g.ShowMenu(); Debug.Log("SLING_AUDIT_DONE: " + dir);
        });
    }
}
