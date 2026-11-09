using UnityEngine;

// Open DadFunLab.unity manually. Never reads or writes campaign progress.
public sealed class DadFunLab : MonoBehaviour
{
    DadFunLabModel model;
    DadArt art;
    Texture2D background, pullPose, flightPose, cartSprite, padSprite;
    public System.Action Exit;
    bool recorded;
    float dashFlash;
    int bestShots;
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
        cartSprite = Resources.Load<Texture2D>("DadContentV2/lunch_cart");
        padSprite = Resources.Load<Texture2D>("DadContentV2/rebound_mat");
        sound = gameObject.AddComponent<AudioSource>();
        StartLevel(DadFunLabModel.PlayOrder[0]);
    }
    void StartLevel(int level)
    {
        model = new DadFunLabModel(level); aiming = paused = recorded = false; finger = -1;
        feedback = ""; feedbackTime = dashFlash = 0;
        bestShots = PlayerPrefs.GetInt("DadContentV2.Best." + model.level, 0);
    }
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
        if (model.sim.Over)
        {
            model.sim.Tick(Mathf.Min(Time.unscaledDeltaTime, .1f));
            if (!recorded && model.sim.phase == SlingshotSim.Phase.Won)
            {
                recorded = true;
                if (bestShots == 0 || model.sim.shots < bestShots)
                { bestShots = model.sim.shots; PlayerPrefs.SetInt("DadContentV2.Best." + model.level, bestShots); PlayerPrefs.Save(); }
            }
            return;
        }
        dashFlash = Mathf.Max(0, dashFlash - Time.unscaledDeltaTime);
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
                if (model.sim.dad.flying) { if (model.Boost(p - model.sim.dad.pos)) dashFlash = .28f; }
                else if (model.CanLaunch) { aiming = true; finger = id; start = now = p; }
            }
            if (aiming && (held || up)) now = p;
            if (aiming && up) { model.Launch(start - now); aiming = false; finger = -1; }
        }
        model.Tick(Mathf.Min(Time.unscaledDeltaTime, .1f) * (model.focusTime > 0 && !model.boostUsed ? .35f : aiming ? .5f : 1));
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
    public static Rect BackgroundUv(Rect frame)
    {
        // Canonical 9:16 crop starts at source y=210. Extend the painting while preserving x-wall positions
        // and the same two source pixels per gameplay unit, including asymmetric notch safe areas.
        return new Rect(0, (2130 - 2 * frame.yMax) / 2340f, 1, 2 * frame.height / 2340f);
    }
    void OnGUI()
    {
        if (model == null) return; Layout();
        if (text == null) text = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        if (button == null) button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 18 };
        GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1));
        Rect screen = new Rect(-offset.x / scale, -offset.y / scale, Screen.width / scale, Screen.height / scale);
        Box(screen, new Color(.93f, .9f, .78f));
        Rect painting = new Rect(0, screen.y, 540, screen.height);
        if (background != null) GUI.DrawTextureWithTexCoords(painting, background, BackgroundUv(painting));
        Box(new Rect(0, 0, 540, 175), new Color(.12f, .23f, .17f)); text.normal.textColor = Color.white;
        Label(new Rect(10, 8, 425, 30), "아빠 대소동 · 8개 도전", 23);
        if (Exit != null && Button(new Rect(448, 6, 80, 34), "집으로")) { Exit(); GUI.matrix = Matrix4x4.identity; return; }
        for (int i = 0; i < DadFunLabModel.Titles.Length; i++)
        {
            int index = DadFunLabModel.PlayOrder[i];
            GUI.backgroundColor = index == model.level ? new Color(1, .8f, .2f) : Color.white;
            bool clear = PlayerPrefs.GetInt("DadContentV2.Best." + index, 0) > 0;
            if (Button(new Rect(10 + i * 65, 48, 60, 34), (i + 1) + (clear ? "★" : ""))) StartLevel(index);
        }
        GUI.backgroundColor = Color.white;
        Label(new Rect(8, 88, 524, 36), DadFunLabModel.Titles[model.level], 24);
        Label(new Rect(10, 126, 520, 40), DadFunLabModel.Hints[model.level], 17);
        text.normal.textColor = Color.black;
        if (model.padEnabled)
        {
            if (padSprite != null) GUI.DrawTexture(new Rect(model.pad.x - 48, model.pad.y - 48, 96, 96), padSprite, ScaleMode.ScaleToFit);
            else Box(new Rect(model.pad.x - 32, model.pad.y - 32, 64, 64), Color.yellow);
        }
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
            Vector2 c = model.cart;
            if (cartSprite != null) GUI.DrawTexture(new Rect(c.x - 58, c.y - 80, 116, 116), cartSprite, ScaleMode.ScaleToFit);
            else Box(new Rect(c.x - 35, c.y - 40, 70, 35), new Color(.45f, .6f, .65f));
        }
        var dad = model.sim.dad;
        DadArt.Character dadArt; Texture2D idle = art.characters.TryGetValue("dad", out dadArt) ? dadArt.Frame("idle", 0) : null;
        Character(dad.flying ? flightPose : aiming ? pullPose : idle, dad.pos, dad.flying, dad.flying ? Mathf.Atan2(dad.vel.y, dad.vel.x) * Mathf.Rad2Deg : 0);
        if (dad.flying && (dashFlash > 0 || (model.CanBoost && !model.boostUsed)))
        {
            Color ring = dashFlash > 0 ? new Color(1, .75f, .1f) : new Color(.15f, .65f, .65f, .65f);
            float radius = dashFlash > 0 ? 35 + (.28f - dashFlash) * 100 : 36;
            for (int i = 0; i < 24; i++)
            { float a = i * Mathf.PI / 12, b = (i + 1) * Mathf.PI / 12; Line(dad.pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, dad.pos + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, ring, 3); }
        }
        if (model.CanBoost && dad.flying && !model.boostUsed)
            Label(new Rect(65, 790, 410, 54), model.focusTime > 0 ? "지금! 다음 상대를 탭" : "원하는 방향을 탭 → 추가 돌진", 22);
        if (feedbackTime > 0) Label(new Rect(80, 210, 380, 45), feedback, 25);
        if (aiming)
        {
            var path = model.sim.PredictShot(start - now, aimPath);
            for (int i = 1; i < path.Count; i++) Line(path[i - 1], path[i], Color.white);
        }
        Box(new Rect(0, 875, 540, 85), new Color(.95f, .9f, .72f));
        Label(new Rect(8, 878, 524, 28), "남은 " + model.sim.AliveCount + "명 · 발사 " + model.sim.shots + "/" + model.ShotBudget + " · 연쇄 " + model.sim.bestChain + (bestShots > 0 ? " · 최고 " + bestShots + "발" : ""));
        if (Button(new Rect(12, 914, 250, 34), "다시")) StartLevel(model.level);
        if (Button(new Rect(278, 914, 250, 34), paused ? "계속" : "정지")) { paused = !paused; aiming = false; finger = -1; }
        if (model.sim.Over || paused)
        {
            Box(new Rect(45, 360, 450, 200), new Color(.98f, .94f, .8f));
            bool won = model.sim.phase == SlingshotSim.Phase.Won;
            Label(new Rect(55, 380, 430, 62), paused ? "잠깐 쉬기" : won ? "대소동 해결! · " + model.sim.shots + "발" : model.sim.lossReason, 28);
            Label(new Rect(60, 445, 420, 40), paused ? "준비되면 계속하세요" : won ? "최대 연쇄 " + model.sim.bestChain + " · 카트 " + model.cartHits + "명" : DadFunLabModel.Hints[model.level], 19);
            if (Button(new Rect(60, 500, 205, 42), paused ? "계속하기" : "바로 다시")) { if (paused) paused = false; else StartLevel(model.level); }
            if (!paused && won && Button(new Rect(275, 500, 205, 42), model.level == 7 ? "처음부터" : "다음 도전 →"))
            {
                int order = System.Array.IndexOf(DadFunLabModel.PlayOrder, model.level);
                StartLevel(DadFunLabModel.PlayOrder[(order + 1) % DadFunLabModel.PlayOrder.Length]);
            }
        }
        GUI.matrix = Matrix4x4.identity;
    }
}
