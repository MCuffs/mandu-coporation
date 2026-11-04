using System.Collections.Generic;
using UnityEngine;

// Prototype B screen (Design/dad-war-slingshot-v1.md): a stage list, the slingshot fight and a result card, drawn in the
// same 540x960 IMGUI frame and from the same art as the main prototype. Kept apart from DadSchoolPrototype so the two
// builds can be compared side by side; the small drawing helpers below mirror that class on purpose.
public sealed class DadSlingshotPrototype : MonoBehaviour
{
    public const float Width = 540, Height = 960, AimSlow = .5f;
    public static readonly Rect Arena = new Rect(64, 240, 412, 520);   // the v5 corridor floor, same as DadSchoolPrototype.PlayArea
    static readonly Color Ink = new Color(.12f, .13f, .12f), Paper = new Color(.94f, .89f, .72f), Yellow = new Color(1f, .78f, .15f);
    static readonly Color Green = new Color(.32f, .48f, .36f), Red = new Color(.78f, .22f, .15f), Night = new Color(.15f, .24f, .18f);
    const string StarsKey = "DadSlingshot.Stars.";

    public SlingshotSim Sim { get; private set; }
    public int StageIndex { get; private set; } = -1;   // -1: the stage list
    public bool Aiming { get { return aiming; } }
    SlingshotSim.Stage[] stages;
    DadArt art;
    Font body, title;
    Texture2D disc;
    AudioSource sfx, music;
    bool aiming;
    int finger = -1, shownChain;
    Vector2 aimStart, aimNow;
    float clock, shake, chainBadge, bannerTime;
    string banner = "";
    bool resultSaved;
    sealed class Popup { public Vector2 pos; public string text; public float life = .75f; public int style; }
    sealed class Dent { public int side, spot, level; }
    readonly List<Popup> popups = new List<Popup>();
    readonly List<Dent> dents = new List<Dent>();
    // 1F corridor locker faces measured on bg_floor1_corridor (same numbers as DadSchoolRewards.WallFaces[0]).
    static readonly float[] LockerSpots = { 466f, 548f };
    const float LockerLeftX = 22, LockerRightX = 518, LockerWidth = 46, LockerShear = 1.3f;

    void Awake() { Application.targetFrameRate = 60; Load(); stages = SlingshotSim.PrototypeStages(Arena); }
    bool ResourcesLost { get { return art == null || disc == null || sfx == null || music == null || stages == null; } }
    void Load()
    {
        art = DadArt.Load();
        body = Resources.Load<Font>("Fonts/GothicA1-Bold"); title = Resources.Load<Font>("Fonts/BlackHanSans-Regular");
        if (body == null) body = Font.CreateDynamicFontFromOSFont("Arial", 20);
        if (title == null) title = body;
        disc = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[4096];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(32 - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f))));
        disc.SetPixels(pixels); disc.Apply();
        AudioSource[] sources = GetComponents<AudioSource>();
        sfx = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        music = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();
        music.loop = true; music.volume = .3f; music.playOnAwake = false;
        if (stages == null) stages = SlingshotSim.PrototypeStages(Arena);
        if (Camera.main != null) Camera.main.backgroundColor = Ink;
    }
    void OnDestroy() { if (disc != null) Destroy(disc); }

    public int BestStars(int i) { return PlayerPrefs.GetInt(StarsKey + i, 0); }
    public int StageCount { get { return stages.Length; } }
    public void StartStage(int i)
    {
        StageIndex = Mathf.Clamp(i, 0, stages.Length - 1);
        stages = SlingshotSim.PrototypeStages(Arena);   // fresh spawn lists each try
        Sim = new SlingshotSim(stages[StageIndex], Arena, 7 + StageIndex);
        aiming = false; finger = -1; popups.Clear(); dents.Clear(); shownChain = 0; chainBadge = 0; resultSaved = false;
        banner = stages[StageIndex].label + "  " + stages[StageIndex].title; bannerTime = 2f;
    }
    public void ShowMenu() { Sim = null; StageIndex = -1; aiming = false; }
    // Editor audits hold an aim on screen without a finger.
    public void DebugAim(Vector2 start, Vector2 now) { aiming = true; finger = -2; aimStart = start; aimNow = now; }
    public void DebugRelease() { if (!aiming) return; aiming = false; finger = -1; Sim.Launch(aimStart - aimNow); }

    // ---- frame ------------------------------------------------------------------------------------------------------
    float ViewScale { get { return Mathf.Min(Screen.safeArea.width / Width, Screen.safeArea.height / Height); } }
    Vector2 Offset { get { Rect s = Screen.safeArea; return new Vector2(s.x + (s.width - Width * ViewScale) / 2, Screen.height - s.yMax + (s.height - Height * ViewScale) / 2); } }
    Vector2 Pointer(Vector2 p) { return (new Vector2(p.x, Screen.height - p.y) - Offset) / ViewScale; }

    void Update()
    {
        if (ResourcesLost) Load();
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f); clock += dt;
        shake = Mathf.Max(0, shake - dt * 25); chainBadge = Mathf.Max(0, chainBadge - dt); bannerTime = Mathf.Max(0, bannerTime - dt);
        for (int i = popups.Count - 1; i >= 0; i--) { popups[i].life -= dt; popups[i].pos.y -= dt * 40; if (popups[i].life <= 0) popups.RemoveAt(i); }
        UpdateMusic();
        if (Sim == null) return;
        if (!Sim.Over)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { ShowMenu(); return; }
            ReadInput();
        }
        Sim.Tick(dt * (aiming ? AimSlow : 1));   // aiming is a calm moment: the walkers slow down while dad is pulled back
        ConsumeEvents();
        if (Sim.Over && !resultSaved)
        {
            resultSaved = true;
            if (Sim.Stars > BestStars(StageIndex)) { PlayerPrefs.SetInt(StarsKey + StageIndex, Sim.Stars); PlayerPrefs.Save(); }
        }
    }
    void ReadInput()
    {
        if (finger == -2) return;   // an editor-held aim
        bool began, held, ended; Vector2 p; int id;
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0); p = Pointer(t.position); id = t.fingerId;
            began = t.phase == TouchPhase.Began; ended = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled; held = !ended;
        }
        else { p = Pointer(Input.mousePosition); id = -1; began = Input.GetMouseButtonDown(0); held = Input.GetMouseButton(0); ended = Input.GetMouseButtonUp(0); }
        if (began && !aiming && Sim.phase == SlingshotSim.Phase.Ready && p.y > 64 && p.y < Height && !ListButton.Contains(p))
        { aiming = true; finger = id; aimStart = aimNow = p; }
        if (!aiming || finger != id) return;
        if (held) aimNow = p;
        if (ended)
        {
            aiming = false; finger = -1;
            Sim.Launch(aimStart - aimNow);   // pulled back -> flies the other way; a short tap is ignored by the dead zone
        }
    }
    void ConsumeEvents()
    {
        foreach (SlingshotSim.Event e in Sim.events)
        {
            if (!string.IsNullOrEmpty(e.sound)) PlaySfx(e.sound, e.sound.StartsWith("jingle") ? .8f : .6f, e.sound == "sfx_chain" ? 1 + Mathf.Min(.5f, Sim.shotChain * .06f) : 1);
            if (e.dentSide >= 0) AddDent(e.dentSide, e.pos.y);
            if (string.IsNullOrEmpty(e.text)) continue;
            if (e.style >= 3) { banner = e.text; bannerTime = 2.4f; shake = Mathf.Max(shake, 5); continue; }
            popups.Add(new Popup { pos = e.pos - new Vector2(0, 80), text = e.text, style = e.style });
            shake = Mathf.Max(shake, e.style >= 2 ? 4 : e.style == 1 ? 2.5f : 0);
        }
        Sim.events.Clear();
        if (Sim.shotChain >= 2 && Sim.shotChain != shownChain) { shownChain = Sim.shotChain; chainBadge = 1.1f; }
        if (Sim.shotChain < shownChain) shownChain = Sim.shotChain;
    }
    void AddDent(int side, float feetY)
    {
        float chest = feetY - 60; int spot = -1; float best = 46;
        for (int i = 0; i < LockerSpots.Length; i++) { float d = Mathf.Abs(LockerSpots[i] - chest); if (d < best) { best = d; spot = i; } }
        if (spot < 0) return;   // doors and the back wall stay clean
        Dent dent = dents.Find(x => x.side == side && x.spot == spot);
        if (dent == null) { dent = new Dent { side = side, spot = spot }; dents.Add(dent); }
        dent.level = Mathf.Min(3, dent.level + 1);
    }
    void UpdateMusic()
    {
        if (music == null || art == null) return;
        AudioClip want = Sim == null ? art.Clip("bgm_home") : Sim.Over ? null : art.Clip("bgm_battle");
        if (music.clip == want) return;
        music.clip = want; if (want != null) music.Play(); else music.Stop();
    }
    void PlaySfx(string name, float volume, float pitch = 1)
    {
        AudioClip clip = art != null ? art.Clip(name) : null;
        if (clip == null || sfx == null) return;
        sfx.pitch = pitch; sfx.PlayOneShot(clip, volume);
    }

    // ---- drawing ----------------------------------------------------------------------------------------------------
    static readonly Rect ListButton = new Rect(452, 10, 76, 42);
    void OnGUI()
    {
        if (ResourcesLost) Load();
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(Offset.x, Offset.y, 0), Quaternion.identity, Vector3.one * ViewScale);
        Box(new Rect(0, 0, Width, Height), Paper);
        if (Sim == null) DrawMenu();
        else { DrawFight(); if (Sim.Over) DrawResult(); }
        GUI.matrix = previous;
    }

    void DrawMenu()
    {
        Box(new Rect(0, 0, Width, 60), Night);
        Text(new Rect(20, 8, 500, 44), "시제품 B  ·  아빠가 직접 날아간다", 20, Paper, true);
        Text(new Rect(24, 76, 492, 90), "아빠 전쟁", 70, Ink, true);
        Ellipse(new Vector2(286, 474), 200, 30, new Color(0, 0, 0, .18f));
        DrawCharacter("dad", "special", 0, new Vector2(270, 440), 230, false, -18);
        for (int i = 0; i < 3; i++) Line(new Vector2(150 - i * 24, 330 + i * 34), new Vector2(60 - i * 24, 372 + i * 34), 6, new Color(Ink.r, Ink.g, Ink.b, .35f));
        string[] rules = { "1  화면을 누르고 뒤로 당겼다 놓으면 아빠가 날아가요", "2  날아간 아빠가 다른 아빠를 사물함에 박아 버려요", "3  그동안 아빠들이 계속 다가와요. 닿으면 아파요" };
        for (int i = 0; i < rules.Length; i++) Text(new Rect(30, 490 + i * 32, 480, 30), rules[i], 17, Ink, true);
        for (int i = 0; i < stages.Length; i++)
        {
            Rect r = new Rect(24, 600 + i * 74, 492, 62);
            if (Button(r, "", i == 3 ? new Color(.95f, .6f, .45f) : Yellow)) StartStage(i);
            Text(new Rect(r.x + 18, r.y, 300, r.height), stages[i].label + "   " + stages[i].title, 22, Ink, true);
            int stars = BestStars(i);
            Text(new Rect(r.xMax - 120, r.y, 104, r.height), new string('★', stars) + new string('☆', 3 - stars), 24, stars > 0 ? Red : Ink, true, TextAnchor.MiddleRight);
        }
    }

    void DrawFight()
    {
        Texture2D bg = art != null ? art.Background(1) : null;
        Matrix4x4 stage = GUI.matrix;
        GUI.matrix *= Matrix4x4.Translate(new Vector3(Mathf.Sin(clock * 61) * shake, Mathf.Cos(clock * 49) * shake * .5f, 0));
        if (bg != null) GUI.DrawTexture(new Rect(0, 0, Width, Height), bg, ScaleMode.StretchToFill);
        foreach (Dent d in dents) DrawDent(d);
        foreach (SlingshotSim.Corpse c in Sim.corpses)
        {
            float k = 1 - c.life / .6f;
            DrawCharacter(FoeId(c.kind), "fly", 0, c.pos - new Vector2(0, 30 + k * 40), FoeStand(c.kind), c.facing > 0, (c.facing > 0 ? 1 : -1) * k * 70, false, null, Mathf.Clamp01(c.life / .25f));
        }
        // Telegraphs under the bodies: the charge line and the swing ring.
        foreach (SlingshotSim.Foe f in Sim.foes)
        {
            if (f.windup <= 0) continue;
            if (f.kind == SlingshotSim.Kind.Chair || f.Boss) { Vector2 end = f.pos + f.aim * 240; Line(f.pos, end, 18, new Color(.9f, .2f, .1f, .28f)); Ellipse(end, 26, 14, Red); }
            else Ellipse(f.pos + f.aim * 30, 70, 26, new Color(.9f, .2f, .1f, .35f));
        }
        var order = new List<object> { Sim.dad }; foreach (SlingshotSim.Foe f in Sim.foes) order.Add(f);
        order.Sort((a, b) => Feet(a).y.CompareTo(Feet(b).y));
        foreach (object o in order) { if (o is SlingshotSim.Dad) DrawDad(); else DrawFoe((SlingshotSim.Foe)o); }
        if (aiming && Sim.phase == SlingshotSim.Phase.Ready) DrawAim();
        else if (Sim.phase == SlingshotSim.Phase.Ready && Sim.shots == 0 && StageIndex == 0) DrawPullHint();
        foreach (Popup p in popups) Text(new Rect(p.pos.x - 130, p.pos.y, 260, 44), p.text, p.style >= 2 ? 30 : p.style == 1 ? 24 : 18, p.style >= 1 ? Red : Ink, true, TextAnchor.MiddleCenter);
        GUI.matrix = stage;
        DrawHud();
    }
    static Vector2 Feet(object o) { return o is SlingshotSim.Dad ? ((SlingshotSim.Dad)o).pos : ((SlingshotSim.Foe)o).pos; }

    void DrawDad()
    {
        SlingshotSim.Dad d = Sim.dad;
        Ellipse(d.pos, 60, 16, new Color(0, 0, 0, d.flying ? .14f : .24f));
        string action = "idle"; int index = Mathf.FloorToInt(clock / .6f); bool flip = false; float tilt = 0, lift = 0;
        if (d.flying)
        {
            action = "special"; index = 0; flip = d.vel.x < 0; lift = 22;
            tilt = Mathf.Clamp(Mathf.Atan2(d.vel.y, Mathf.Abs(d.vel.x)) * Mathf.Rad2Deg, -55, 55) * (flip ? -1 : 1);
            // Speed lines trail behind him.
            Vector2 back = -d.vel.normalized; float k = Mathf.Clamp01(d.vel.magnitude / SlingshotSim.LaunchMax);
            for (int i = 0; i < 3; i++) { Vector2 side = new Vector2(-back.y, back.x) * (i - 1) * 18; Vector2 a = d.pos - new Vector2(0, 70) + back * 60 + side; Line(a, a + back * (40 + 50 * k), 5, new Color(Ink.r, Ink.g, Ink.b, .35f)); }
        }
        else if (aiming) { action = "punch"; index = 0; flip = (aimStart - aimNow).x < 0; }
        else if (d.hurt > .35f) { action = "hurt"; index = 0; }
        else if (d.landed > 0) { action = "movestop"; index = 0; }
        bool blink = d.hurt > 0 && Mathf.Sin(clock * 35) > .3f;
        DrawCharacter("dad", action, index, d.pos - new Vector2(0, lift), 150, flip, tilt, blink);
    }
    void DrawFoe(SlingshotSim.Foe f)
    {
        string id = FoeId(f.kind); float stand = FoeStand(f.kind), lift = f.Lift;
        Ellipse(f.pos, f.Boss ? 92 : 52, 14, new Color(0, 0, 0, .22f));
        string action; int index = 0; float tilt = 0;
        bool tray = f.kind == SlingshotSim.Kind.Tray, broken = tray && f.trayBroken;
        if (f.Flying) { action = "fly"; tilt = Mathf.Clamp(-f.vel.x * .03f, -25, 25); }
        else if (f.Boss && f.groggy > 0) action = "groggy";
        else if (f.dash > 0) action = "dash";
        else if (f.windup > 0) action = f.kind == SlingshotSim.Kind.Chair || f.Boss ? "windup" : "attack";
        else if (f.attackPose > 0) { action = "attack"; index = 1; }
        else if (f.stun > 0) action = broken ? "notray_hit" : "hit";
        else if (f.kind == SlingshotSim.Kind.Gum)
        {
            int step = Mathf.FloorToInt(f.walked / 20) % 4;   // walk_0 > pass_0 > walk_1 > pass_1 (v7)
            action = step % 2 == 0 ? "walk" : "pass"; index = step / 2;
        }
        else { action = broken ? "notray_walk" : "walk"; index = Mathf.FloorToInt(f.walked / 24); }
        DrawCharacter(id, action, index, f.pos - new Vector2(0, lift), stand, f.facing > 0, tilt, f.flash > 0);
        if (f.shield)
        {
            // The tray rim marks the guarded side: the one turned toward dad.
            Vector2 c = f.pos - new Vector2(0, stand * .42f) + f.face * 26, side = new Vector2(-f.face.y, f.face.x) * 24;
            Line(c - side, c + side, 10, Ink); Line(c - side, c + side, 5, new Color(1, 1, 1, .9f));
        }
        if (f.hp < f.maxHp && !f.Boss) Bar(new Rect(f.pos.x - 26, f.pos.y - stand - 10, 52, 7), f.hp / (float)f.maxHp, Red);
        if (f.Boss && f.groggy > 0)
        {
            Vector2 head = f.pos - new Vector2(0, stand * .95f);
            for (int s = 0; s < 3; s++) { float t = clock * 7 + s * 2.094f; Vector2 p = head + new Vector2(Mathf.Cos(t) * 40, Mathf.Sin(t) * 10); Ellipse(p, 18, 18, Ink); Ellipse(p, 12, 12, Yellow); }
        }
    }
    void DrawAim()
    {
        Vector2 pull = aimStart - aimNow; SlingshotSim.Dad d = Sim.dad;
        float power = Mathf.Clamp01(pull.magnitude / SlingshotSim.PullMax);
        bool live = pull.magnitude >= SlingshotSim.PullDeadZone;
        // Rubber band from dad's chest back toward the finger, then the dotted flight guide.
        Vector2 chest = d.pos - new Vector2(0, 60);
        Line(chest, chest - pull.normalized * Mathf.Min(pull.magnitude, SlingshotSim.PullMax) * .7f, 7, new Color(Ink.r, Ink.g, Ink.b, .55f));
        if (!live) return;
        List<Vector2> path = SlingshotSim.Predict(d.pos, pull, Arena);
        for (int i = 0; i + 1 < path.Count; i++)
        {
            Vector2 a = path[i], b = path[i + 1]; float len = (b - a).magnitude;
            for (float s = 0; s < len; s += 16) Ellipse(Vector2.Lerp(a, b, s / Mathf.Max(1, len)) - new Vector2(0, 30), i == 0 ? 9 : 7, i == 0 ? 9 : 7, new Color(1, 1, 1, i == 0 ? .95f : .6f));
        }
        Rect tag = new Rect(chest.x - 40, chest.y - 120, 80, 30);
        Box(tag, new Color(Ink.r, Ink.g, Ink.b, .75f)); Text(tag, Mathf.RoundToInt(power * 100) + "%", 18, power > .95f ? Yellow : Color.white, true, TextAnchor.MiddleCenter);
    }
    void DrawPullHint()
    {
        // First stage, before the first shot: a ghost finger pulls back from dad.
        float t = Mathf.Repeat(clock, 1.6f) / 1.2f; Vector2 start = Sim.dad.pos + new Vector2(0, -40), end = start + new Vector2(-30, 110);
        Vector2 p = Vector2.Lerp(start, end, Mathf.Clamp01(t));
        Ellipse(p, 46, 46, new Color(1, 1, 1, t <= 1 ? .55f : 0)); Ellipse(p, 22, 22, new Color(Ink.r, Ink.g, Ink.b, t <= 1 ? .5f : 0));
        Text(new Rect(Sim.dad.pos.x - 150, Sim.dad.pos.y + 4, 300, 34), "뒤로 당겼다 놓기!", 22, Ink, true, TextAnchor.MiddleCenter);
    }
    void DrawHud()
    {
        SlingshotSim.Stage s = Sim.stage; SlingshotSim.Dad d = Sim.dad;
        Box(new Rect(0, 0, Width, 60), Night);
        Text(new Rect(16, 4, 200, 26), s.label + "  " + s.title, 17, Paper, true);
        Bar(new Rect(16, 34, 230, 16), d.health / d.maxHealth, d.health < 30 ? Red : new Color(.45f, .75f, .45f));
        Text(new Rect(256, 6, 186, 24), "발사 " + Sim.shots + " · 목표 " + s.par, 17, Paper, true, TextAnchor.MiddleRight);
        Text(new Rect(256, 30, 186, 24), s.bossEscorts ? "보스를 쓰러뜨리세요" : "남은 아빠 " + Sim.Remaining, 15, Paper, false, TextAnchor.MiddleRight);
        if (Button(ListButton, "목록", Color.white, true, 17)) ShowMenu();
        SlingshotSim.Foe boss = Sim.Boss;
        if (boss != null)
        {
            Box(new Rect(0, 60, Width, 34), new Color(Night.r, Night.g, Night.b, .85f));
            Text(new Rect(14, 62, 110, 30), "회장 아빠", 16, Paper, true);
            Bar(new Rect(116, 68, 400, 18), boss.hp / (float)boss.maxHp, boss.groggy > 0 ? Yellow : Red);
            if (boss.groggy > 0) Text(new Rect(116, 66, 400, 22), "그로기!  아빠로 들이받아요", 14, Ink, true, TextAnchor.MiddleCenter);
        }
        float top = boss != null ? 100 : 68;
        if (bannerTime > 0) { Panel(new Rect(24, top, 492, 52), Yellow); Text(new Rect(30, top, 480, 52), banner, 21, Ink, true, TextAnchor.MiddleCenter); }
        else if (Sim.shots < 2 && !Sim.Over)
        {
            Box(new Rect(24, top + 6, 492, 34), new Color(Paper.r, Paper.g, Paper.b, .9f));
            Text(new Rect(24, top + 6, 492, 34), s.lesson, 16, Ink, true, TextAnchor.MiddleCenter);
        }
        if (chainBadge > 0 && Sim.shotChain >= 2)
        {
            Rect badge = new Rect(160, top + 64, 220, 50); Panel(badge, Yellow);
            Text(badge, Sim.shotChain + "연쇄!!", 32, Red, true, TextAnchor.MiddleCenter);
        }
        if (aiming) Text(new Rect(0, 900, Width, 40), "조준 중 · 시간이 느려져요", 16, Paper, true, TextAnchor.MiddleCenter);
    }
    void DrawResult()
    {
        bool won = Sim.phase == SlingshotSim.Phase.Won;
        Box(new Rect(0, 0, Width, Height), new Color(0, 0, 0, .78f));
        Panel(new Rect(30, 170, 480, 620), Paper);
        Box(new Rect(33, 173, 474, 50), Night);
        Text(new Rect(30, 173, 480, 50), Sim.stage.label + " · " + Sim.stage.title, 21, Paper, true, TextAnchor.MiddleCenter);
        Text(new Rect(30, 236, 480, 64), won ? "복도 정리 끝!" : "아빠도 가끔은 진다", 44, won ? Ink : Red, true, TextAnchor.MiddleCenter);
        int stars = Sim.Stars;
        Text(new Rect(30, 300, 480, 50), new string('★', stars) + new string('☆', 3 - stars), 40, Red, true, TextAnchor.MiddleCenter);
        string[] labels = { "발사", "최대 연쇄", "남은 체력" };
        string[] values = { Sim.shots + " / " + Sim.stage.par, Sim.bestChain.ToString(), Mathf.CeilToInt(Sim.dad.health).ToString() };
        for (int i = 0; i < 3; i++)
        {
            Rect r = new Rect(48 + i * 152, 364, 140, 86); Panel(r, Color.white);
            Text(new Rect(r.x, r.y + 6, r.width, 24), labels[i], 15, Ink, true, TextAnchor.MiddleCenter);
            Text(new Rect(r.x, r.y + 32, r.width, 46), values[i], 30, Red, true, TextAnchor.MiddleCenter);
        }
        Text(new Rect(50, 462, 440, 50), won ? (stars < 3 ? "별 3개: 체력 절반 이상, 발사 " + Sim.stage.par + "번 이하" : "완벽해요!") : "다가오는 아빠를 먼저 날려 보세요", 17, Green, true, TextAnchor.MiddleCenter);
        bool next = won && StageIndex + 1 < stages.Length;
        if (Button(new Rect(60, 530, 420, 72), next ? "다음 판" : "다시 하기", Yellow, true, 30)) StartStage(next ? StageIndex + 1 : StageIndex);
        if (next && Button(new Rect(60, 618, 420, 56), "다시 하기", Color.white, true, 21)) StartStage(StageIndex);
        if (Button(new Rect(60, next ? 690 : 618, 420, 56), "판 목록", Color.white, true, 21)) ShowMenu();
    }

    // ---- helpers (mirroring DadSchoolPrototype) ----------------------------------------------------------------------
    static string FoeId(SlingshotSim.Kind k) { return k == SlingshotSim.Kind.Boss ? "boss1" : k == SlingshotSim.Kind.Chair ? "chair" : k == SlingshotSim.Kind.Tray ? "tray" : "gum"; }
    static float FoeStand(SlingshotSim.Kind k) { return k == SlingshotSim.Kind.Boss ? 200 : k == SlingshotSim.Kind.Chair ? 150 : 140; }
    void DrawDent(Dent d)
    {
        Texture2D tex = art != null ? art.Reward("locker_damage_" + d.level) : null;
        if (tex == null) return;
        float w = LockerWidth, h = w * tex.height / tex.width;
        Vector2 c = new Vector2(d.side == 0 ? LockerLeftX : LockerRightX, LockerSpots[d.spot]);
        Matrix4x4 shear = Matrix4x4.identity; shear.m10 = d.side == 0 ? -LockerShear : LockerShear;
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.Translate(new Vector3(c.x, c.y, 0)) * shear * Matrix4x4.Scale(new Vector3((d.side == 1) ^ (d.spot % 2 == 1) ? -1 : 1, 1, 1));
        GUI.DrawTexture(new Rect(-w / 2, -h / 2, w, h), tex);
        GUI.matrix = old;
    }
    void DrawCharacter(string id, string action, int index, Vector2 feet, float standHeight, bool flip, float tilt = 0, bool flash = false, Vector2? stretch = null, float opacity = 1)
    {
        DadArt.Character c;
        if (art == null || !art.characters.TryGetValue(id, out c)) return;
        Texture2D tex = c.Frame(action, index);
        if (tex == null) return;
        float scale = standHeight / c.standHeight, size = DadArt.Canvas * scale;
        Matrix4x4 old = GUI.matrix; Vector2 shape = stretch ?? Vector2.one;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(feet.x, feet.y, 0), Quaternion.Euler(0, 0, tilt), new Vector3((flip ? -1 : 1) * shape.x, shape.y, 1));
        GUI.color = flash ? new Color(1, .86f, .74f, opacity) : new Color(1, 1, 1, opacity);
        GUI.DrawTexture(new Rect(-DadArt.PivotX * scale, -DadArt.PivotY * scale, size, size), tex);
        GUI.color = Color.white; GUI.matrix = old;
    }
    void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
    void Panel(Rect r, Color c) { Box(r, Ink); Box(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), c); }
    void Text(Rect r, string s, int size = 20, Color? color = null, bool display = false, TextAnchor align = TextAnchor.MiddleLeft)
    { GUI.Label(r, s, new GUIStyle { font = display ? title : body, fontSize = size, alignment = align, wordWrap = true, normal = { textColor = color ?? Ink } }); }
    bool Button(Rect r, string s, Color c, bool enabled = true, int size = 23)
    {
        Panel(new Rect(r.x, r.y + 5, r.width, r.height), Ink); Panel(r, enabled ? c : new Color(.6f, .6f, .55f));
        if (s.Length > 0) Text(r, s, size, Ink, true, TextAnchor.MiddleCenter);
        bool previous = GUI.enabled; GUI.enabled = previous && enabled;
        bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none); GUI.enabled = previous;
        if (clicked) PlaySfx("sfx_button", .45f);
        return clicked;
    }
    void Bar(Rect r, float value, Color c) { Box(r, Ink); Box(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(value), r.height - 4), c); }
    void Ellipse(Vector2 center, float w, float h, Color c) { GUI.color = c; GUI.DrawTexture(new Rect(center.x - w / 2, center.y - h / 2, w, h), disc); GUI.color = Color.white; }
    void Line(Vector2 a, Vector2 b, float width, Color c)
    {
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(a.x, a.y, 0), Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg), Vector3.one);
        Box(new Rect(0, -width / 2, (b - a).magnitude, width), c); GUI.matrix = old;
    }
}
