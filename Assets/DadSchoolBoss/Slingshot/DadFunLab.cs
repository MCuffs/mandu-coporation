using UnityEngine;

// Open DadFunLab.unity manually. Never reads or writes campaign progress.
public sealed class DadFunLab : MonoBehaviour
{
    DadFunLabModel model;
    DadArt art;
    Texture2D background, pullPose, flightPose;
    AudioSource sound;
    bool aiming, paused;
    int finger = -1;
    Vector2 start, now;
    GUIStyle text;
    GUIStyle button;
    Font font;
    readonly System.Collections.Generic.Dictionary<string, AudioClip> clips = new System.Collections.Generic.Dictionary<string, AudioClip>();
    readonly System.Collections.Generic.List<Vector2> aimPath = new System.Collections.Generic.List<Vector2>();
    float scale;
    Vector2 offset;
    string feedback = "";
    float feedbackTime;
    void Awake()
    {
        art = DadArt.Load();
        font = Resources.Load<Font>("Fonts/GothicA1-Regular");
        foreach (var clip in Resources.LoadAll<AudioClip>("DadV13")) clips[clip.name] = clip;
        background = Resources.Load<Texture2D>("DadV13/bg/corridor_floor1_long");
        pullPose = Resources.Load<Texture2D>("DadV13/chars/dad_pull_0");
        flightPose = Resources.Load<Texture2D>("DadV13/chars/dad_flight_0");
        sound = gameObject.AddComponent<AudioSource>();
        StartLevel(0);
    }
    void StartLevel(int level) { model = new DadFunLabModel(level); aiming = paused = false; finger = -1; feedback = ""; feedbackTime = 0; }
    void OnApplicationFocus(bool focused) { if (!focused) { paused = true; aiming = false; finger = -1; } }
    void OnApplicationPause(bool pause) { if (pause) OnApplicationFocus(false); }
    void Layout()
    {
        Rect safe = Screen.safeArea; scale = Mathf.Min(safe.width / 540, safe.height / 960);
        offset = new Vector2(safe.x + (safe.width - 540 * scale) / 2, Screen.height - safe.yMax + (safe.height - 960 * scale) / 2);
    }
    Vector2 Point(Vector2 screen) { return (new Vector2(screen.x, Screen.height - screen.y) - offset) / scale; }
    void Update()
    {
        Layout();
        if (Input.GetKeyDown(KeyCode.Escape)) { paused = !paused; aiming = false; finger = -1; }
        if (paused) return;
        if (model.sim.Over) { model.sim.Tick(Mathf.Min(Time.unscaledDeltaTime, .1f)); return; }
        feedbackTime = Mathf.Max(0, feedbackTime - Time.unscaledDeltaTime);
        Vector2 p; bool down, held, up, cancel = false; int id;
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (finger >= 0)
            {
                bool found = false;
                for (int i = 0; i < Input.touchCount; i++) if (Input.GetTouch(i).fingerId == finger) { t = Input.GetTouch(i); found = true; break; }
                if (!found) { aiming = false; finger = -1; return; }
            }
            p = Point(t.position); id = t.fingerId; down = t.phase == TouchPhase.Began;
            held = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
            up = t.phase == TouchPhase.Ended; cancel = t.phase == TouchPhase.Canceled;
        }
        else { p = Point(Input.mousePosition); id = -1; down = Input.GetMouseButtonDown(0); held = Input.GetMouseButton(0); up = Input.GetMouseButtonUp(0); }
        if (cancel) { aiming = false; finger = -1; }
        else
        {
            if (down && p.y > 175 && p.y < 870)
            {
                if (model.sim.dad.flying) model.Boost(p - model.sim.dad.pos);
                else if (model.sim.shots == 0) { aiming = true; finger = id; start = now = p; }
            }
            if (aiming && (held || up)) now = p;
            if (aiming && up) { model.Launch(start - now); aiming = false; finger = -1; }
        }
        model.Tick(Mathf.Min(Time.unscaledDeltaTime, .1f));
        foreach (var e in model.sim.events)
        {
            if (!string.IsNullOrEmpty(e.text) && e.text != "제압") { feedback = e.text; feedbackTime = .65f; }
            AudioClip clip; if (!clips.TryGetValue(e.sound ?? "", out clip)) clip = art.Clip(e.sound ?? "");
            if (clip != null && DadPreferences.Effects) sound.PlayOneShot(clip, .4f);
        }
        model.sim.events.Clear();
    }
    void Box(Rect r, Color color) { GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
    void Label(Rect r, string value, int size = 18)
    { text.fontSize = size; GUI.Label(r, value, text); }
    bool Button(Rect r, string value) { return GUI.Button(r, value, button); }
    void Line(Vector2 a, Vector2 b, Color color, float width = 3)
    {
        Matrix4x4 matrix = GUI.matrix; GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg, a);
        Box(new Rect(a.x, a.y - width / 2, Vector2.Distance(a, b), width), color); GUI.matrix = matrix;
    }
    void Character(Texture2D image, Vector2 feet, bool centered = false, float angle = 0)
    {
        if (image == null) { Box(new Rect(feet.x - 15, feet.y - 40, 30, 40), Color.yellow); return; }
        float k = 108f / 560; Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, feet);
        GUI.DrawTexture(new Rect(feet.x - 384 * k, feet.y - (centered ? 384 : 720) * k, 768 * k, 768 * k), image);
        GUI.matrix = old;
    }
    void OnGUI()
    {
        if (model == null) return; Layout();
        if (text == null) text = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        if (button == null) button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18 };
        GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1));
        Box(new Rect(0, 0, 540, 960), new Color(.93f, .9f, .78f));
        if (background != null) GUI.DrawTextureWithTexCoords(new Rect(0, 0, 540, 960), background, new Rect(0, 210f / 2340, 1, 1920f / 2340));
        Box(new Rect(0, 0, 540, 175), new Color(.12f, .23f, .17f)); text.normal.textColor = Color.white;
        Label(new Rect(10, 8, 520, 30), "재미 실험실 · 성장/보상 없음", 20);
        for (int i = 0; i < 3; i++) if (Button(new Rect(12 + i * 176, 48, 164, 34), (i + 1) + "번 실험")) StartLevel(i);
        Label(new Rect(8, 88, 524, 36), DadFunLabModel.Titles[model.level], 24);
        Label(new Rect(10, 126, 520, 40), model.level == 2 ? "뒤로 당겨 발사 → 비행 중 원하는 곳을 탭" : model.level == 1 ? "카트를 맞혀 밀어보세요 · 발사는 한 번" : "쟁반 정면을 피해 벽에 튕겨보세요 · 한 발", 17);
        text.normal.textColor = Color.black;
        foreach (var f in model.sim.foes) if (f.hp > 0)
        {
            DadArt.Character c; if (art.characters.TryGetValue(f.kind == SlingshotSim.Kind.Tray ? "tray" : "gum", out c)) Character(c.Frame(f.Flying ? "hurt" : "idle", 0), f.pos);
        }
        foreach (var corpse in model.sim.corpses)
        {
            DadArt.Character c;
            if (art.characters.TryGetValue(corpse.kind == SlingshotSim.Kind.Tray ? "tray" : "gum", out c))
            {
                GUI.color = new Color(1, 1, 1, Mathf.Clamp01(corpse.life / .6f));
                Character(c.Frame("hurt", 0), corpse.pos); GUI.color = Color.white;
            }
        }
        if (model.cartEnabled)
        {
            Vector2 c = model.cart; Box(new Rect(c.x - 35, c.y - 40, 70, 35), new Color(.45f, .6f, .65f));
            Box(new Rect(c.x - 30, c.y - 5, 12, 12), Color.black); Box(new Rect(c.x + 18, c.y - 5, 12, 12), Color.black);
            Label(new Rect(c.x - 40, c.y - 38, 80, 30), "급식", 16);
        }
        var dad = model.sim.dad;
        DadArt.Character dadArt; Texture2D idle = art.characters.TryGetValue("dad", out dadArt) ? dadArt.Frame("idle", 0) : null;
        Character(dad.flying ? flightPose : aiming ? pullPose : idle, dad.pos, dad.flying, dad.flying ? Mathf.Atan2(dad.vel.y, dad.vel.x) * Mathf.Rad2Deg : 0);
        if (model.level == 2 && dad.flying && !model.boostUsed)
            Label(new Rect(65, 790, 410, 54), "지금 원하는 방향을 탭 → 추가 돌진", 22);
        if (feedbackTime > 0) Label(new Rect(80, 210, 380, 45), feedback, 25);
        if (aiming)
        {
            var path = model.sim.PredictShot(start - now, aimPath);
            for (int i = 1; i < path.Count; i++) Line(path[i - 1], path[i], Color.white);
        }
        Box(new Rect(0, 875, 540, 85), new Color(.95f, .9f, .72f));
        Label(new Rect(8, 878, 524, 28), "남은 상대 " + model.sim.AliveCount + " · 연쇄 " + model.sim.bestChain + (model.level == 2 ? (model.boostUsed ? " · 추가 돌진 사용" : " · 추가 돌진 1회") : ""));
        if (Button(new Rect(12, 914, 250, 34), "다시")) StartLevel(model.level);
        if (Button(new Rect(278, 914, 250, 34), paused ? "계속" : "정지")) { paused = !paused; aiming = false; finger = -1; }
        if (model.sim.Over || paused)
        {
            Box(new Rect(45, 360, 450, 200), new Color(.98f, .94f, .8f));
            Label(new Rect(55, 380, 430, 62), paused ? "잠깐 쉬기" : model.sim.phase == SlingshotSim.Phase.Won ? "한 발 성공!" : model.sim.lossReason, 28);
            Label(new Rect(60, 445, 420, 40), "다른 각도로 다시 해보고 싶은가요?", 20);
            if (Button(new Rect(100, 500, 340, 42), paused ? "계속하기" : "같은 판 바로 다시")) { if (paused) paused = false; else StartLevel(model.level); }
        }
        GUI.matrix = Matrix4x4.identity;
    }
}
