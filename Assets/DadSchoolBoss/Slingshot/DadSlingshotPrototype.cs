using System.Collections.Generic;
using UnityEngine;

// Prototype B screen (Design/dad-war-slingshot-v1.md): a stage list, the slingshot fight and a result card, drawn in the
// same 540x960 IMGUI frame and from the same art as the main prototype. Kept apart from DadSchoolPrototype so the two
// builds can be compared side by side; the small drawing helpers below mirror that class on purpose.
// Wide view (Design/dad-war-b-changes-v1.md C-01..C-03): bodies drawn at 72% (or 80%) of the battle size, the fight uses
// the whole corridor floor, one thin bar on top, and the chain count stays out of the crowd's way.
public sealed class DadSlingshotPrototype : MonoBehaviour
{
    public const float Width = 540, Height = 960, AimSlow = .5f, DefaultScale = .72f;
    // The V13 long corridor (Art/V13Slingshot/arena-guide.json, 9:16 crop [0,210,1080,1920] at half scale): the wall fronts
    // are straight lines at pixel x 103 / 976 and the floor starts at pixel y 532, so the bounce walls sit at x 51.5 / 488
    // and the fight starts at the foot of the door, y 161.
    public static readonly Rect Arena = new Rect(51.5f, 161, 436.5f, 769);
    // Locker stretches of the long corridor in screen y (pixel intervals from the arena guide); windows in between stay clean.
    static readonly Vector2[] LockerRuns = { new Vector2(179, 305), new Vector2(409.5f, 533.5f), new Vector2(639, 762.5f), new Vector2(862.5f, 989) };
    static readonly Color Ink = new Color(.12f, .13f, .12f), Paper = new Color(.94f, .89f, .72f), Yellow = new Color(1f, .78f, .15f);
    static readonly Color Green = new Color(.32f, .48f, .36f), Red = new Color(.78f, .22f, .15f), Night = new Color(.15f, .24f, .18f);
    const string StarsKey = "DadSlingshot.Stars.", ScaleKey = "DadSlingshot.Scale";
    const float BarHeight = 44;

    public SlingshotSim Sim { get; private set; }
    public int StageIndex { get; private set; } = -1;   // -1: the stage list
    public bool Aiming { get { return aiming; } }
    // Character size against the art, kept per device so 72% and 80% can be compared (decision in the change request).
    public static float BodyScale { get { return PlayerPrefs.GetInt(ScaleKey, Mathf.RoundToInt(DefaultScale * 100)) / 100f; } }
    public static void SetBodyScale(float scale) { PlayerPrefs.SetInt(ScaleKey, Mathf.RoundToInt(scale * 100)); PlayerPrefs.Save(); }
    float S { get { return Sim != null ? Sim.scale : BodyScale; } }
    SlingshotSim.Stage[] stages;
    DadArt art;
    Font body, title;
    Texture2D disc;
    AudioSource sfx, music, pullLoop;
    // V13 pack (Design/dad-b-assets-v13.md): poses, effects, hand, icons and sounds by file name; missing ones fall back.
    readonly Dictionary<string, Texture2D> v13 = new Dictionary<string, Texture2D>();
    readonly Dictionary<string, AudioClip> v13Audio = new Dictionary<string, AudioClip>();
    Texture2D V(string name) { Texture2D t; return v13.TryGetValue(name, out t) ? t : null; }
    bool aiming;
    int finger = -1, shownChain;
    Vector2 aimStart, aimNow;
    float clock, shake, chainChip, chainBig, bannerTime, endClock;
    Vector2 flightDir = Vector2.up;   // kept while dad slows down so the pose does not spin at low speed
    public static bool DebugWalls;    // editor audits draw the bounce lines over the painting to check they meet the wall fronts
    string banner = "";
    bool resultSaved;
    sealed class Popup { public Vector2 pos; public string text; public float life = .75f; public int style; }
    sealed class Dent { public int side, spot, level; public float y; }
    readonly List<Popup> popups = new List<Popup>();
    readonly List<Dent> dents = new List<Dent>();
    // 1F corridor locker faces measured on bg_floor1_corridor (same numbers as DadSchoolRewards.WallFaces[0]).
    static readonly float[] LockerSpots = { 466f, 548f };
    const float LockerLeftX = 22, LockerRightX = 518, LockerWidth = 46, LockerShear = 1.3f;

    void Awake() { Application.targetFrameRate = 60; Load(); stages = SlingshotSim.PrototypeStages(Arena); }
    bool ResourcesLost { get { return art == null || disc == null || sfx == null || music == null || pullLoop == null || stages == null; } }
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
        pullLoop = sources.Length > 2 ? sources[2] : gameObject.AddComponent<AudioSource>();
        music.loop = true; music.volume = .3f; music.playOnAwake = false;
        v13.Clear(); v13Audio.Clear();
        foreach (Texture2D t in Resources.LoadAll<Texture2D>("DadV13")) v13[t.name] = t;
        foreach (AudioClip a in Resources.LoadAll<AudioClip>("DadV13")) v13Audio[a.name] = a;
        foreach (string n in new[] { "corridor_floor1_long", "dad_pull_0", "dad_flight_0", "dad_flight_1", "dad_bounce_0", "dad_land_0", "wind", "swoosh", "rage_0", "rage_1", "drag_hand", "wall_launch", "chain_launch", "rage_launch" })
            if (!v13.ContainsKey(n)) Debug.LogWarning("V13 image missing, old art is used instead: " + n);
        foreach (string n in new[] { "sfx_pull_tension", "sfx_launch_whoosh", "sfx_wall_boing", "sfx_landing_skid" })
            if (!v13Audio.ContainsKey(n)) Debug.LogWarning("V13 sound missing: " + n);
        AudioClip tension; v13Audio.TryGetValue("sfx_pull_tension", out tension);
        pullLoop.clip = tension; pullLoop.loop = true; pullLoop.playOnAwake = false; pullLoop.volume = .35f;
        if (stages == null) stages = SlingshotSim.PrototypeStages(Arena);
        if (Camera.main != null) Camera.main.backgroundColor = Ink;
    }
    void OnDestroy() { if (disc != null) Destroy(disc); if (pullLoop != null) pullLoop.Stop(); }
    // Losing focus drops the pull (no launch) and silences the rubber band.
    void OnApplicationFocus(bool focused) { if (!focused) { aiming = false; finger = -1; if (pullLoop != null) pullLoop.Stop(); } }

    public int BestStars(int i) { return PlayerPrefs.GetInt(StarsKey + i, 0); }
    public int StageCount { get { return stages.Length; } }
    public void StartStage(int i)
    {
        StageIndex = Mathf.Clamp(i, 0, stages.Length - 1);
        stages = SlingshotSim.PrototypeStages(Arena);   // fresh spawn lists each try
        Sim = new SlingshotSim(stages[StageIndex], Arena, 7 + StageIndex, BodyScale);
        aiming = false; finger = -1; popups.Clear(); dents.Clear(); shownChain = 0; chainChip = chainBig = endClock = 0; resultSaved = false;
        banner = stages[StageIndex].label + "  " + stages[StageIndex].title + "  ·  상대 " + stages[StageIndex].spawns.Count + "명" + (stages[StageIndex].bossEscorts ? " +" : ""); bannerTime = 1.8f;
    }
    public void ShowMenu() { Sim = null; StageIndex = -1; aiming = false; }
    // Editor audits hold an aim on screen without a finger.
    public void DebugAim(Vector2 start, Vector2 now) { aiming = true; finger = -2; aimStart = start; aimNow = now; }
    public void DebugRelease() { if (!aiming) return; aiming = false; finger = -1; Sim.Launch(aimStart - aimNow); }

    // ---- frame ------------------------------------------------------------------------------------------------------
    float ViewScale { get { return Mathf.Min(Screen.safeArea.width / Width, Screen.safeArea.height / Height); } }
    Vector2 Offset { get { Rect s = Screen.safeArea; return new Vector2(s.x + (s.width - Width * ViewScale) / 2, Screen.height - s.yMax + (s.height - Height * ViewScale) / 2); } }
    Vector2 Pointer(Vector2 p) { return (new Vector2(p.x, Screen.height - p.y) - Offset) / ViewScale; }
    // On a phone taller than 9:16 the frame is centred with this much room (canvas units) above and below it inside the safe
    // area. The corridor painting fills it; the top bar and the bottom chips move out to the real screen edges.
    float Pad { get { return Mathf.Max(0, (Screen.safeArea.height / ViewScale - Height) / 2); } }
    Rect ListButton { get { return new Rect(482, -Pad + 6, 50, 32); } }

    void Update()
    {
        if (ResourcesLost) Load();
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f); clock += dt;
        shake = Mathf.Max(0, shake - dt * 25); chainChip = Mathf.Max(0, chainChip - dt); chainBig = Mathf.Max(0, chainBig - dt); bannerTime = Mathf.Max(0, bannerTime - dt);
        for (int i = popups.Count - 1; i >= 0; i--) { popups[i].life -= dt; popups[i].pos.y -= dt * 40; if (popups[i].life <= 0) popups.RemoveAt(i); }
        UpdateMusic();
        if (Sim == null) return;
        if (!Sim.Over)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { ShowMenu(); return; }
            ReadInput();
        }
        // Aiming is a calm moment: the walkers slow down while dad is pulled back. Once it is over, the last bodies keep
        // flying in slow motion for a moment (the finisher) before the result card.
        UpdatePullSound();
        Sim.Tick(dt * (Sim.Over ? .3f : aiming ? AimSlow : 1));
        ConsumeEvents();
        endClock = Mathf.Max(0, endClock - dt);
        if (Sim.Over && !resultSaved)
        {
            resultSaved = true; endClock = Sim.phase == SlingshotSim.Phase.Won ? 1f : .7f;
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
            if (t.phase == TouchPhase.Canceled) { if (finger == id) { aiming = false; finger = -1; } return; }   // a cancel never fires
            began = t.phase == TouchPhase.Began; ended = t.phase == TouchPhase.Ended; held = !ended;
        }
        else { p = Pointer(Input.mousePosition); id = -1; began = Input.GetMouseButtonDown(0); held = Input.GetMouseButton(0); ended = Input.GetMouseButtonUp(0); }
        if (began && !aiming && Sim.phase == SlingshotSim.Phase.Ready && p.y > BarHeight - Pad && p.y < Height + Pad && !ListButton.Contains(p))
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
            if (string.IsNullOrEmpty(e.text) || e.text.EndsWith("연쇄!!")) continue;   // the chain count has its own chip
            if (e.style >= 3) { banner = e.text; bannerTime = 2.2f; shake = Mathf.Max(shake, 5); continue; }
            popups.Add(new Popup { pos = e.pos - new Vector2(0, 80 * S), text = e.text, style = e.style });
            // Only wall slams and bigger shake the corridor; small knocks stay still so the crowd stays readable.
            shake = Mathf.Max(shake, e.style >= 2 ? 4 : e.style == 1 && e.text.StartsWith("벽꿍") ? 2.5f : 0);
        }
        Sim.events.Clear();
        if (Sim.shotChain >= 2 && Sim.shotChain != shownChain)
        {
            shownChain = Sim.shotChain; chainChip = 1.4f;
            if (shownChain >= 5) chainBig = .6f;
        }
        if (Sim.shotChain < shownChain) shownChain = Sim.shotChain;
    }
    // The rubber band creaks while dad is pulled back, higher the harder the pull (V13 audio: pitch 0.8-1.8).
    void UpdatePullSound()
    {
        if (pullLoop == null || pullLoop.clip == null) return;
        bool on = aiming && Sim != null && !Sim.Over && Sim.phase == SlingshotSim.Phase.Ready && (aimStart - aimNow).magnitude >= SlingshotSim.PullDeadZone;
        if (on)
        {
            pullLoop.pitch = Mathf.Lerp(.8f, 1.8f, Mathf.Clamp01((aimStart - aimNow).magnitude / SlingshotSim.PullMax));
            if (!pullLoop.isPlaying) pullLoop.Play();
        }
        else if (pullLoop.isPlaying) pullLoop.Stop();
    }
    void AddDent(int side, float feetY)
    {
        if (V("corridor_floor1_long") != null)
        {
            // Long corridor: dents only on the locker runs (not the windows), merged when they land close together.
            float y = feetY - 40 * S; bool locker = false;
            foreach (Vector2 run in LockerRuns) if (y > run.x + 14 && y < run.y - 14) locker = true;
            if (!locker) return;
            Dent near = dents.Find(x => x.side == side && Mathf.Abs(x.y - y) < 36);
            if (near == null) { near = new Dent { side = side, y = y }; dents.Add(near); }
            near.level = Mathf.Min(3, near.level + 1);
            return;
        }
        float chest = feetY - 60 * S; int spot = -1; float best = 46;
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
        AudioClip clip;
        if (!v13Audio.TryGetValue(name, out clip)) clip = art != null ? art.Clip(name) : null;
        if (clip == null || sfx == null) return;
        sfx.pitch = pitch; sfx.PlayOneShot(clip, volume);
    }

    // ---- drawing ----------------------------------------------------------------------------------------------------
    void OnGUI()
    {
        if (ResourcesLost) Load();
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(Offset.x, Offset.y, 0), Quaternion.identity, Vector3.one * ViewScale);
        Box(new Rect(0, -Pad, Width, Height + 2 * Pad), Paper);
        if (Sim == null) DrawMenu();
        else { DrawFight(); if (Sim.Over && endClock <= 0) DrawResult(); }
        GUI.matrix = previous;
    }

    void DrawMenu()
    {
        Box(new Rect(0, 0, Width, 60), Night);
        Text(new Rect(20, 8, 500, 44), "시제품 B  ·  아빠가 직접 날아간다", 20, Paper, true);
        Text(new Rect(24, 66, 492, 80), "아빠 전쟁", 62, Ink, true);
        Ellipse(new Vector2(300, 338), 160, 24, new Color(0, 0, 0, .18f));
        Texture2D flight = V("dad_flight_1"), wind = V("wind");
        if (flight != null)
        {
            if (wind != null) DrawFx(wind, new Vector2(170, 262), 210, 90, -20, new Color(1, 1, 1, .8f));
            DrawPose(flight, new Vector2(300, 240), new Vector2(384, 384), 230 / 560f, false, -20, false);
        }
        else
        {
            DrawCharacter("dad", "special", 0, new Vector2(290, 312), 170, false, -18);
            for (int i = 0; i < 3; i++) Line(new Vector2(190 - i * 20, 222 + i * 26), new Vector2(118 - i * 20, 254 + i * 26), 5, new Color(Ink.r, Ink.g, Ink.b, .35f));
        }
        string[] rules = { "1  화면을 누르고 뒤로 당겼다 놓으면 아빠가 날아가요", "2  날아간 아빠가 다른 아빠를 사물함에 박아 버려요", "3  그동안 아빠들이 계속 다가와요. 닿으면 아파요" };
        for (int i = 0; i < rules.Length; i++) Text(new Rect(30, 352 + i * 28, 480, 28), rules[i], 16, Ink, true);
        for (int i = 0; i < stages.Length; i++)
        {
            Rect r = new Rect(24, 448 + i * 68, 492, 56);
            bool boss = i == stages.Length - 1, mid = i == stages.Length - 2;
            if (Button(r, "", boss ? new Color(.95f, .6f, .45f) : mid ? new Color(1f, .7f, .35f) : Yellow)) StartStage(i);
            Text(new Rect(r.x + 18, r.y, 300, r.height), stages[i].label + "   " + stages[i].title, 22, Ink, true);
            int stars = BestStars(i);
            Text(new Rect(r.xMax - 120, r.y, 104, r.height), new string('★', stars) + new string('☆', 3 - stars), 24, stars > 0 ? Red : Ink, true, TextAnchor.MiddleRight);
        }
        // Size comparison for the wide view (change request, decision 2): the choice applies from the next fight.
        Text(new Rect(24, 892, 170, 44), "캐릭터 크기", 18, Ink, true);
        float[] sizes = { .72f, .8f };
        for (int i = 0; i < sizes.Length; i++)
        {
            bool on = Mathf.Abs(BodyScale - sizes[i]) < .005f;
            if (Button(new Rect(190 + i * 120, 892, 110, 44), Mathf.RoundToInt(sizes[i] * 100) + "%", on ? Yellow : Color.white, true, 20)) SetBodyScale(sizes[i]);
        }
    }

    void DrawFight()
    {
        float s = S;
        Texture2D bg = art != null ? art.Background(1) : null;
        Matrix4x4 stage = GUI.matrix;
        GUI.matrix *= Matrix4x4.Translate(new Vector3(Mathf.Sin(clock * 61) * shake, Mathf.Cos(clock * 49) * shake * .5f, 0));
        // Boss finisher: about 0.25 s of zoom (x1.15) on the boss as he goes down, then back while the card waits.
        SlingshotSim.Corpse fallen = Sim.phase == SlingshotSim.Phase.Won ? Sim.corpses.Find(c => c.kind == SlingshotSim.Kind.Boss) : null;
        if (fallen != null && endClock > .75f)
        {
            float z = 1 + .15f * Mathf.Clamp01((1f - endClock) / .1f); Vector3 c = new Vector3(fallen.pos.x, fallen.pos.y - 80, 0);
            GUI.matrix *= Matrix4x4.Translate(c) * Matrix4x4.Scale(new Vector3(z, z, 1)) * Matrix4x4.Translate(-c);
        }
        Texture2D corridor = V("corridor_floor1_long");
        // Pixel y 210 is the top of the 9:16 frame, so the full painting runs from -105 to 1065: on a 9:19.5 phone the
        // letterbox space shows more corridor (change request, proposal 2) while the fight stays inside the frame.
        if (corridor != null) GUI.DrawTexture(new Rect(0, -105, Width, 1170), corridor, ScaleMode.StretchToFill);
        else if (bg != null) GUI.DrawTexture(new Rect(0, 0, Width, Height), bg, ScaleMode.StretchToFill);
        foreach (Dent d in dents) DrawDent(d);
        foreach (SlingshotSim.Corpse c in Sim.corpses)
        {
            float k = 1 - c.life / .6f;
            DrawCharacter(FoeId(c.kind), "fly", 0, c.pos - new Vector2(0, (30 + k * 40) * s), FoeStand(c.kind, c.elite) * s, c.facing > 0, (c.facing > 0 ? 1 : -1) * k * 70, false, null, Mathf.Clamp01(c.life / .25f));
        }
        // Telegraphs under the bodies: the charge line and the swing ring.
        foreach (SlingshotSim.Foe f in Sim.foes)
        {
            if (f.windup <= 0) continue;
            if (f.kind == SlingshotSim.Kind.Dodge)
            {
                Vector2 hand = f.pos - new Vector2(0, 60 * s), dir = f.target - hand; float len = dir.magnitude;
                for (float t = 0; t < len; t += 22) Ellipse(hand + dir * (t / Mathf.Max(1, len)), 7 * s, 7 * s, new Color(.9f, .2f, .1f, .45f));
                Ellipse(f.target, 54 * s, 22 * s, new Color(.9f, .2f, .1f, .35f)); Ellipse(f.target, 30 * s, 12 * s, new Color(.9f, .2f, .1f, .5f));
            }
            else if (f.kind == SlingshotSim.Kind.Chair || f.Boss) { Vector2 end = f.pos + f.aim * 240; Line(f.pos, end, 18 * s, new Color(.9f, .2f, .1f, .28f)); Ellipse(end, 26 * s, 14 * s, Red); }
            else Ellipse(f.pos + f.aim * 30 * s, 70 * s, 26 * s, new Color(.9f, .2f, .1f, .35f));
        }
        var order = new List<object> { Sim.dad }; foreach (SlingshotSim.Foe f in Sim.foes) order.Add(f);
        order.Sort((a, b) => Feet(a).y.CompareTo(Feet(b).y));
        foreach (object o in order) { if (o is SlingshotSim.Dad) DrawDad(); else DrawFoe((SlingshotSim.Foe)o); }
        DrawBalls();
        if (DebugWalls)
        {
            Line(new Vector2(Arena.xMin, Arena.yMin), new Vector2(Arena.xMin, Arena.yMax), 3, Red); Line(new Vector2(Arena.xMax, Arena.yMin), new Vector2(Arena.xMax, Arena.yMax), 3, Red);
            Line(new Vector2(Arena.xMin, Arena.yMin), new Vector2(Arena.xMax, Arena.yMin), 3, Red);
            foreach (Vector2 run in LockerRuns) { Line(new Vector2(4, run.x), new Vector2(4, run.y), 6, Yellow); Line(new Vector2(Width - 4, run.x), new Vector2(Width - 4, run.y), 6, Yellow); }
        }
        if (aiming && Sim.phase == SlingshotSim.Phase.Ready) DrawAim();
        else if (Sim.phase == SlingshotSim.Phase.Ready && Sim.shots == 0) DrawPullHint();
        foreach (Popup p in popups) Text(new Rect(p.pos.x - 130, p.pos.y, 260, 40), p.text, p.style >= 2 ? 26 : p.style == 1 ? 20 : 16, p.style >= 1 ? Red : Ink, true, TextAnchor.MiddleCenter);
        GUI.matrix = stage;
        DrawHud();
    }
    static Vector2 Feet(object o) { return o is SlingshotSim.Dad ? ((SlingshotSim.Dad)o).pos : ((SlingshotSim.Foe)o).pos; }

    void DrawDad()
    {
        SlingshotSim.Dad d = Sim.dad; float s = S;
        Texture2D rage = V(Mathf.Repeat(clock, .2f) < .1f ? "rage_0" : "rage_1");
        if (Sim.ragePending && !d.flying)
        {
            // A 5-chain charged him up: the V13 flame ring behind him (the middle is clear) until the rage shot goes.
            float pulse = 1 + .08f * Mathf.Sin(clock * 9);
            if (rage != null) DrawFx(rage, d.pos - new Vector2(0, 50 * s), 230 * s * pulse, 230 * s * pulse, 0, Color.white);
            else { Ellipse(d.pos, 120 * s * pulse, 40 * s * pulse, new Color(Red.r, Red.g, Red.b, .35f)); Ellipse(d.pos, 90 * s, 28 * s, new Color(Yellow.r, Yellow.g, Yellow.b, .35f)); }
            Texture2D icon = V("rage_launch");
            Rect label = new Rect(d.pos.x - 74, d.pos.y - 150 * s - 30, 160, 26);
            if (icon != null) GUI.DrawTexture(new Rect(label.x - 30, label.y - 4, 32, 32), icon, ScaleMode.ScaleToFit);
            Text(label, "분노 발사 준비!", 16, Red, true, TextAnchor.MiddleCenter);
        }
        Ellipse(d.pos, 60 * s, 16 * s, new Color(0, 0, 0, d.flying ? .14f : .24f));
        string action = "idle"; int index = Mathf.FloorToInt(clock / .6f); bool flip = false; float tilt = 0, lift = 0;
        if (d.flying && DrawFlightV13(d, s, rage)) return;
        if (!d.flying && DrawGroundV13(d, s)) return;
        if (d.flying)
        {
            action = "special"; index = 0; flip = d.vel.x < 0; lift = 22 * s;
            tilt = Mathf.Clamp(Mathf.Atan2(d.vel.y, Mathf.Abs(d.vel.x)) * Mathf.Rad2Deg, -55, 55) * (flip ? -1 : 1);
            // Speed lines trail behind him.
            Vector2 back = -d.vel.normalized; float k = Mathf.Clamp01(d.vel.magnitude / SlingshotSim.LaunchMax);
            // Red in a rage, yellow and thicker with every locker bounce (the bank-shot bonus), ink otherwise.
            Color streak = d.rage ? new Color(Red.r, Red.g, Red.b, .7f) : d.bounces > 0 ? new Color(Yellow.r, Yellow.g, Yellow.b, .85f) : new Color(Ink.r, Ink.g, Ink.b, .35f);
            for (int i = 0; i < 3; i++) { Vector2 side = new Vector2(-back.y, back.x) * (i - 1) * 18 * s; Vector2 a = d.pos - new Vector2(0, 70 * s) + back * 60 * s + side; Line(a, a + back * (40 + 50 * k) * s, (5 + 2 * d.bounces) * s, streak); }
        }
        else if (aiming) { action = "punch"; index = 0; flip = (aimStart - aimNow).x < 0; }
        else if (d.hurt > .35f) { action = "hurt"; index = 0; }
        else if (d.landed > 0) { action = "movestop"; index = 0; }
        bool blink = d.hurt > 0 && Mathf.Sin(clock * 35) > .3f || d.flying && d.rage;
        DrawCharacter("dad", action, index, d.pos - new Vector2(0, lift), 150 * s, flip, tilt, blink);
    }
    // V13 flight: two flight poses alternate, turned about the body centre (pivot 384,384) to the flight direction; at a
    // side-locker contact the wall-bounce pose stands with its back contact (canvas x 205) on the wall front. Wind behind
    // him grows with speed, a swoosh joins after a locker bounce (bank shot), and the flame ring rides along in a rage.
    bool DrawFlightV13(SlingshotSim.Dad d, float s, Texture2D rage)
    {
        Texture2D a = V("dad_flight_0"), b = V("dad_flight_1"), bounce = V("dad_bounce_0");
        if (a == null || b == null) return false;
        float scale = 150 * s / 560f, speed = d.vel.magnitude, k = Mathf.Clamp01(speed / SlingshotSim.LaunchMax);
        if (speed > 120) flightDir = d.vel / speed;
        Vector2 dir = flightDir, body = d.pos - new Vector2(0, 55 * s);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Color tint = d.rage ? new Color(1, .45f, .35f, .5f + .45f * k) : d.bounces > 0 ? new Color(1, .85f, .3f, .5f + .45f * k) : new Color(1, 1, 1, .35f + .5f * k);
        Texture2D wind = V("wind"), swoosh = V("swoosh");
        if (wind != null) DrawFx(wind, body - dir * (60 + 40 * k) * s, (120 + 120 * k) * s, (70 + 30 * k) * s, angle, tint);
        if (swoosh != null && d.bounces > 0) DrawFx(swoosh, body - dir * 50 * s, 170 * s, 70 * s, angle, tint);
        if (d.rage && rage != null) DrawFx(rage, body, 210 * s, 210 * s, 0, Color.white);
        Ellipse(d.pos, 60 * s, 16 * s, new Color(0, 0, 0, .14f));
        if (d.bounceClock > 0 && d.bounceSide >= 0 && bounce != null)
        {
            bool right = d.bounceSide == 1; float wallX = right ? Arena.xMax : Arena.xMin;
            DrawPose(bounce, new Vector2(wallX + (right ? -1 : 1) * (384 - 205) * scale, d.pos.y), new Vector2(384, 720), scale, right, 0, d.rage);
        }
        else
        {
            bool flip = dir.x < 0;
            float tilt = flip ? -Mathf.Atan2(dir.y, -dir.x) * Mathf.Rad2Deg : angle;
            bool variant = d.flightTime < .12f || Mathf.Repeat(d.flightTime, .9f) < .1f;   // pose 1 mixed in briefly, never flickering
            DrawPose(variant ? b : a, body, new Vector2(384, 384), scale, flip, tilt, d.rage);
        }
        if (d.bounces > 0)
        {
            Texture2D icon = V("wall_launch");
            if (icon != null) { GUI.DrawTexture(new Rect(body.x + 34 * s, body.y - 70 * s, 34, 34), icon, ScaleMode.ScaleToFit); Text(new Rect(body.x + 34 * s + 30, body.y - 70 * s, 50, 34), "x" + d.bounces, 18, Red, true); }
        }
        return true;
    }
    // V13 ground poses: pulling back while aiming (facing the launch), and the skid when he comes to a stop.
    bool DrawGroundV13(SlingshotSim.Dad d, float s)
    {
        Texture2D pull = V("dad_pull_0"), land = V("dad_land_0");
        bool pulling = aiming && Sim.phase == SlingshotSim.Phase.Ready && (aimStart - aimNow).magnitude >= SlingshotSim.PullDeadZone;
        Texture2D pose = pulling ? pull : !aiming && d.landed > 0 && d.hurt <= .35f ? land : null;
        if (pose == null) return false;
        Ellipse(d.pos, 64 * s, 17 * s, new Color(0, 0, 0, .24f));
        bool flip = pose == pull ? (aimStart - aimNow).x < 0 : false;
        DrawPose(pose, d.pos, new Vector2(384, 720), 150 * s / 560f, flip, 0, d.hurt > 0 && Mathf.Sin(clock * 35) > .3f);
        return true;
    }
    void DrawPose(Texture2D tex, Vector2 at, Vector2 pivot, float scale, bool flip, float tilt, bool flash)
    {
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(at.x, at.y, 0), Quaternion.Euler(0, 0, tilt), new Vector3(flip ? -1 : 1, 1, 1));
        GUI.color = flash ? new Color(1, .86f, .74f, 1) : Color.white;
        GUI.DrawTexture(new Rect(-pivot.x * scale, -pivot.y * scale, 768 * scale, 768 * scale), tex);
        GUI.color = Color.white; GUI.matrix = old;
    }
    void DrawFx(Texture2D tex, Vector2 center, float w, float h, float angle, Color tint)
    {
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(center.x, center.y, 0), Quaternion.Euler(0, 0, angle), Vector3.one);
        GUI.color = tint; GUI.DrawTexture(new Rect(-w / 2, -h / 2, w, h), tex); GUI.color = Color.white; GUI.matrix = old;
    }
    void DrawFoe(SlingshotSim.Foe f)
    {
        float s = S, stand = FoeStand(f.kind, f.elite) * s, lift = f.Lift;
        string id = FoeId(f.kind);
        Ellipse(f.pos, (f.Boss ? 92 : 52) * (f.elite ? 1.4f : 1) * s, 14 * s, new Color(0, 0, 0, .22f));
        string action; int index = 0; float tilt = 0;
        bool broken = f.kind == SlingshotSim.Kind.Tray && f.trayBroken;
        if (f.Flying) { action = "fly"; tilt = Mathf.Clamp(-f.vel.x * .03f, -25, 25); }
        else if (f.Boss && f.groggy > 0) action = "groggy";
        else if (f.dash > 0) action = "dash";
        else if (f.kind == SlingshotSim.Kind.Dodge && (f.windup > 0 || f.attackPose > 0)) { action = "throw"; index = f.windup > 0 ? 0 : 1; }
        else if (f.windup > 0) action = f.kind == SlingshotSim.Kind.Chair || f.Boss ? "windup" : "attack";
        else if (f.attackPose > 0) { action = "attack"; index = 1; }
        else if (f.stun > 0) action = broken ? "notray_hit" : "hit";
        else if (f.kind == SlingshotSim.Kind.Gum)
        {
            int step = Mathf.FloorToInt(f.walked / (20 * s)) % 4;   // walk_0 > pass_0 > walk_1 > pass_1 (v7), stride scaled with the body
            action = step % 2 == 0 ? "walk" : "pass"; index = step / 2;
        }
        else { action = broken ? "notray_walk" : "walk"; index = Mathf.FloorToInt(f.walked / (24 * s)); }
        DrawCharacter(id, action, index, f.pos - new Vector2(0, lift), stand, f.facing > 0, tilt, f.flash > 0);
        if (f.shield)
        {
            // The tray rim marks the guarded side: the one turned toward dad.
            Vector2 c = f.pos - new Vector2(0, stand * .42f) + f.face * 26 * s, side = new Vector2(-f.face.y, f.face.x) * 24 * s;
            Line(c - side, c + side, 10 * s, Ink); Line(c - side, c + side, 5 * s, new Color(1, 1, 1, .9f));
        }
        if (f.elite && f.heavyHits > 0)
        {
            // The second-hit window: hit him again before the yellow bar runs out.
            Bar(new Rect(f.pos.x - 34 * s, f.pos.y - stand - 26, 68 * s, 8), f.heavyClock / 3f, Yellow);
            Text(new Rect(f.pos.x - 70, f.pos.y - stand - 50, 140, 22), "한 번 더!", 15, Red, true, TextAnchor.MiddleCenter);
        }
        if (f.hp < f.maxHp && !f.Boss) Bar(new Rect(f.pos.x - 22 * s, f.pos.y - stand - 8, 44 * s, 6), f.hp / (float)f.maxHp, Red);
        if (f.Boss && f.groggy > 0)
        {
            Vector2 head = f.pos - new Vector2(0, stand * .95f);
            for (int i = 0; i < 3; i++) { float t = clock * 7 + i * 2.094f; Vector2 p = head + new Vector2(Mathf.Cos(t) * 40 * s, Mathf.Sin(t) * 10 * s); Ellipse(p, 18 * s, 18 * s, Ink); Ellipse(p, 12 * s, 12 * s, Yellow); }
        }
    }
    void DrawBalls()
    {
        Texture2D ball = art != null && art.props.ContainsKey("prop_dodgeball") ? art.props["prop_dodgeball"] : null; float s = S;
        foreach (SlingshotSim.Ball b in Sim.balls)
        {
            Ellipse(b.pos + new Vector2(0, 58 * s), 26 * s, 9 * s, new Color(0, 0, 0, .2f));
            if (b.returned) Line(b.pos - b.vel.normalized * 70 * s, b.pos, 12 * s, new Color(Yellow.r, Yellow.g, Yellow.b, .6f));
            float size = 34 * s; Rect r = new Rect(b.pos.x - size / 2, b.pos.y - size / 2, size, size);
            if (ball != null) GUI.DrawTexture(r, ball, ScaleMode.ScaleToFit); else { Ellipse(b.pos, size, size, Ink); Ellipse(b.pos, size - 6, size - 6, Red); }
        }
    }
    void DrawAim()
    {
        Vector2 pull = aimStart - aimNow; SlingshotSim.Dad d = Sim.dad; float s = S;
        float power = Mathf.Clamp01(pull.magnitude / SlingshotSim.PullMax);
        bool live = pull.magnitude >= SlingshotSim.PullDeadZone;
        // Rubber band from dad's chest back toward the finger, then the dotted flight guide.
        Vector2 chest = d.pos - new Vector2(0, 60 * s);
        Line(chest, chest - pull.normalized * Mathf.Min(pull.magnitude, SlingshotSim.PullMax) * .7f, 7 * s, new Color(Ink.r, Ink.g, Ink.b, .55f));
        if (!live) return;
        List<Vector2> path = SlingshotSim.Predict(d.pos, pull, Arena, Sim.DadRadius);
        for (int i = 0; i + 1 < path.Count; i++)
        {
            Vector2 a = path[i], b = path[i + 1]; float len = (b - a).magnitude;
            for (float t = 0; t < len; t += 16) Ellipse(Vector2.Lerp(a, b, t / Mathf.Max(1, len)) - new Vector2(0, 30 * s), i == 0 ? 9 : 7, i == 0 ? 9 : 7, new Color(1, 1, 1, i == 0 ? .95f : .6f));
        }
        Rect tag = new Rect(chest.x - 38, chest.y - 100 * s - 30, 76, 28);
        Box(tag, new Color(Ink.r, Ink.g, Ink.b, .75f)); Text(tag, Mathf.RoundToInt(power * 100) + "%", 17, power > .95f ? Yellow : Color.white, true, TextAnchor.MiddleCenter);
    }
    void DrawPullHint()
    {
        // Before the first shot: the stage's lesson above dad, and a ghost finger pressing above him and pulling back down.
        Vector2 dad = Sim.dad.pos; float s = S;
        float t = Mathf.Repeat(clock, 1.6f) / 1.2f; Vector2 start = dad + new Vector2(0, -230), end = dad + new Vector2(26, -110 * s);
        Vector2 p = Vector2.Lerp(start, end, Mathf.Clamp01(t)); float a = t <= 1 ? 1 : 0;
        if (StageIndex == 0)
        {
            Line(start, p, 6, new Color(Ink.r, Ink.g, Ink.b, .35f * a));
            Texture2D hand = V("drag_hand");
            // The V13 hand points down-left; its fingertip (about 12%, 94% of the image) rides on the pull path.
            if (hand != null) { GUI.color = new Color(1, 1, 1, a); GUI.DrawTexture(new Rect(p.x - .12f * 72, p.y - .94f * 72, 72, 72), hand, ScaleMode.ScaleToFit); GUI.color = Color.white; }
            else { Ellipse(p, 46, 46, new Color(1, 1, 1, .55f * a)); Ellipse(p, 22, 22, new Color(Ink.r, Ink.g, Ink.b, .5f * a)); }
        }
        // Under the bar, over the back wall: the walkers enter below it, so the lesson never sits on top of them.
        Rect pill = new Rect(24, BarHeight - Pad + 6 + (Sim.Boss != null ? 28 : 0) + (bannerTime > 0 ? 32 : 0), 492, 34);
        Box(pill, new Color(Paper.r, Paper.g, Paper.b, .92f));
        Text(pill, StageIndex == 0 ? "누르고 아래로 당겼다 놓으면 위로 날아가요" : Sim.stage.lesson, 16, Ink, true, TextAnchor.MiddleCenter);
    }
    void DrawHud()
    {
        SlingshotSim.Stage st = Sim.stage; SlingshotSim.Dad d = Sim.dad;
        float pad = Pad, y0 = -pad, bottom = Height + pad;
        // One thin bar at the top of the screen: stage, health, shots against the target, and the way out.
        Box(new Rect(0, y0, Width, BarHeight), Night);
        Text(new Rect(12, y0, 196, BarHeight), st.label + "  " + st.title, 16, Paper, true);
        Bar(new Rect(210, y0 + 15, 150, 14), d.health / d.maxHealth, d.health < 30 ? Red : new Color(.45f, .75f, .45f));
        Text(new Rect(366, y0, 112, BarHeight), "발사 " + Sim.shots + " / " + st.par, 16, Sim.shots > st.par ? new Color(1, .6f, .5f) : Paper, true, TextAnchor.MiddleCenter);
        if (Button(ListButton, "목록", Color.white, true, 15)) ShowMenu();
        float top = y0 + BarHeight;
        SlingshotSim.Foe boss = Sim.Boss ?? Sim.foes.Find(x => x.elite && x.hp > 0);
        if (boss != null)
        {
            Box(new Rect(0, top, Width, 28), new Color(Night.r, Night.g, Night.b, .85f));
            Text(new Rect(12, top, 100, 28), boss.Boss ? "회장 아빠" : "지호 큰아빠", 15, Paper, true);
            Bar(new Rect(104, top + 6, 424, 16), boss.hp / (float)boss.maxHp, boss.groggy > 0 ? Yellow : Red);
            if (boss.groggy > 0) Text(new Rect(104, top + 4, 424, 20), "그로기!  아빠로 들이받아요", 13, Ink, true, TextAnchor.MiddleCenter);
            else if (boss.heavyHits > 0) Text(new Rect(104, top + 4, 424, 20), "휘청!  3초 안에 한 번 더", 13, Ink, true, TextAnchor.MiddleCenter);
            top += 28;
        }
        if (bannerTime > 0) { Box(new Rect(0, top, Width, 32), new Color(Yellow.r, Yellow.g, Yellow.b, .95f)); Text(new Rect(10, top, Width - 20, 32), banner, 17, Ink, true, TextAnchor.MiddleCenter); }
        // Bottom corners stay clear of dad (he starts in the middle): the chain chip on the left, what is left on the right.
        if (chainChip > 0 && Sim.shotChain >= 2)
        {
            string titleText = ChainTitle(Sim.shotChain); Texture2D icon = V("chain_launch");
            Rect chip = new Rect(12, bottom - 54, (titleText.Length > 0 ? 260 : 132) + (icon != null ? 30 : 0), 40); Panel(chip, Yellow);
            if (icon != null) GUI.DrawTexture(new Rect(chip.x + 6, chip.y + 4, 32, 32), icon, ScaleMode.ScaleToFit);
            Text(new Rect(chip.x + (icon != null ? 30 : 0), chip.y, chip.width - (icon != null ? 30 : 0), chip.height), Sim.shotChain + "연쇄!" + (titleText.Length > 0 ? "  " + titleText : ""), titleText.Length > 0 ? 18 : 22, Red, true, TextAnchor.MiddleCenter);
        }
        if (!st.bossEscorts)
        {
            Rect left = new Rect(Width - 132, bottom - 54, 120, 40); Box(left, new Color(Paper.r, Paper.g, Paper.b, .9f));
            Text(left, "남은 " + Sim.Remaining + "명", 17, Ink, true, TextAnchor.MiddleCenter);
        }
        if (chainBig > 0)
        {
            float a = Mathf.Clamp01(chainBig / .2f);
            Text(new Rect(0, 402, Width, 70), Sim.shotChain + "연쇄!!", 52, new Color(Ink.r, Ink.g, Ink.b, .6f * a), true, TextAnchor.MiddleCenter);
            Text(new Rect(0, 398, Width, 70), Sim.shotChain + "연쇄!!", 52, new Color(Red.r, Red.g, Red.b, a), true, TextAnchor.MiddleCenter);
            Text(new Rect(0, 462, Width, 40), ChainTitle(Sim.shotChain), 26, new Color(Ink.r, Ink.g, Ink.b, a), true, TextAnchor.MiddleCenter);
        }
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
    static string FoeId(SlingshotSim.Kind k) { return k == SlingshotSim.Kind.Boss ? "boss1" : k == SlingshotSim.Kind.Chair ? "chair" : k == SlingshotSim.Kind.Tray ? "tray" : k == SlingshotSim.Kind.Dodge ? "dodge" : "gum"; }
    // Big-dads are the regular art at 1.45x, like the main prototype's elites.
    static float FoeStand(SlingshotSim.Kind k, bool elite = false) { return (k == SlingshotSim.Kind.Boss ? 200 : k == SlingshotSim.Kind.Chair ? 150 : 140) * (elite ? 1.45f : 1); }
    // A line of 아빠 전쟁 humour for big chains (shown with the count).
    static string ChainTitle(int chain) { return chain >= 10 ? "전설의 아빠" : chain >= 7 ? "의리 폭발" : chain >= 5 ? "단톡방 긴급 소집" : chain >= 3 ? "몸풀기 끝" : ""; }
    void DrawDent(Dent d)
    {
        Texture2D tex = art != null ? art.Reward("locker_damage_" + d.level) : null;
        if (tex == null) return;
        if (V("corridor_floor1_long") != null)
        {
            // Long corridor: the lockers face the floor straight on, so the dent sits on the strip without a shear.
            float w2 = 40, h2 = w2 * tex.height / tex.width, cx = d.side == 0 ? Arena.xMin - 20 : Arena.xMax + 20;
            GUI.DrawTexture(new Rect(cx - w2 / 2, d.y - h2 / 2, w2, h2), tex);
            return;
        }
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
