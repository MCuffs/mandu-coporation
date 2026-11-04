using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

// A self-contained presentation for the school prototype. Combat stays in the shared simulation.
public sealed partial class DadSchoolPrototype : MonoBehaviour
{
    public const float Width = 540, Height = 960;
    public const float RunLimit = 180;
    public AlleyCombat Combat { get; private set; }
    public bool Home { get; private set; } = true;
    public bool Paused { get; private set; }
    public int BestChain { get; private set; }
    public int Wins { get; private set; }
    public int Gear { get; private set; }
    public int TotalWallHits { get; private set; }
    public bool ChainGearUnlocked { get { return BestChain >= 3 || Wins > 0; } }
    public const int PhoneWallGoal = 60; // about two full runs of wall slams
    public bool PhoneGearUnlocked { get { return TotalWallHits >= PhoneWallGoal; } }
    public SchoolRunDirector Director { get; private set; }

    struct Frame { public Texture2D texture; public float aspect, relativeHeight; public bool owned; }
    sealed class Island { public int label, count, left, right, bottom, top; }
    sealed class Popup { public Vector2 position; public string text; public float life = .7f; public int style; }
    sealed class Actor { public Fighter enemy; public Vector2 feet; }
    readonly List<Popup> popups = new List<Popup>();
    readonly List<Actor> actors = new List<Actor>();
    Texture2D hero, enemies, arena, disc;
    Frame[] heroFrames, enemyFrames;
    Font body, title;
    AudioSource audioSource;
    AudioClip hitClip, wallClip;
    bool dragging, specialRequested, resultSaved, gearPanel;
    int finger = -1;
    Vector2 dragOrigin, stick;
    float clock, shake, bannerTime;
    string banner = "", resultReason = "";
    static readonly Color Ink = new Color(.12f, .13f, .12f);
    static readonly Color Paper = new Color(.94f, .89f, .72f);
    static readonly Color Green = new Color(.32f, .48f, .36f);
    static readonly Color Yellow = new Color(1f, .78f, .15f);
    static readonly Color Red = new Color(.78f, .22f, .15f);
    static readonly string[] SkillNames = { "사물함까지 가라", "친구 따라 교무실", "잠깐 반성해", "아빠 아직 빠르다", "도시락 한 입", "잔돈도 챙겨" };
    static readonly string[] SkillEffects = { "벽 충돌 피해 +1", "충돌 전달 능력 강화", "벽 충돌 후 기절 연장", "자동 공격 간격 단축", "즉시 체력 15 회복\n제압할 때 체력 회복", "코인 흡수 범위 증가\n코인 보상 증가" };
    public Rect PlayArea { get { return new Rect(64, 240, 412, 520); } }

    void Awake()
    {
        Application.targetFrameRate = 60;
        hero = LoadArt("Hero", out heroFrames);
        enemies = LoadArt("Enemies", out enemyFrames);
        arena = Resources.Load<Texture2D>("DadV2/Arena");
        body = Resources.Load<Font>("Fonts/GothicA1-Bold");
        title = Resources.Load<Font>("Fonts/BlackHanSans-Regular");
        if (body == null) body = Font.CreateDynamicFontFromOSFont("Arial", 20);
        if (title == null) title = body;
        disc = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[4096];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01(32 - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f))));
        disc.SetPixels(pixels); disc.Apply();
        audioSource = gameObject.AddComponent<AudioSource>();
        hitClip = MakeSound(150, .065f); wallClip = MakeSound(75, .13f);
        ReloadProgress();
        if (Camera.main != null) Camera.main.backgroundColor = Ink;
    }

    public void ReloadProgress()
    {
        BestChain = PlayerPrefs.GetInt("DadV2.BestChain", 0);
        Wins = PlayerPrefs.GetInt("DadV2.Wins", 0);
        TotalWallHits = PlayerPrefs.GetInt("DadV2.TotalWallHits", 0);
        int savedGear = Mathf.Clamp(PlayerPrefs.GetInt("DadV2.Gear", 0), 0, 2);
        Gear = savedGear == 2 && PhoneGearUnlocked || savedGear == 1 && ChainGearUnlocked ? savedGear : 0;
    }

    static Texture2D LoadArt(string name, out Frame[] frames)
    {
        frames = new Frame[4]; int tallest = 1; bool complete = true;
        for (int i = 0; i < frames.Length; i++)
        {
            Texture2D t = Resources.Load<Texture2D>("DadV2/Baked/" + name + "/" + i);
            if (t == null) { complete = false; break; }
            frames[i] = new Frame { texture = t, aspect = t.width / (float)t.height, relativeHeight = t.height };
            tallest = Mathf.Max(tallest, t.height);
        }
        if (complete) { for (int i = 0; i < frames.Length; i++) frames[i].relativeHeight /= tallest; return frames[0].texture; }
        Texture2D source = Resources.Load<Texture2D>("DadV2/" + name); frames = Slice(source, 4); return source;
    }
#if UNITY_EDITOR
    public static void BakeArtFrames(string name)
    {
        var texture = Resources.Load<Texture2D>("DadV2/" + name);
        if (texture == null) throw new System.InvalidOperationException("Missing atlas " + name);
        Frame[] frames = Slice(texture, 4, true);
        string dir = Application.dataPath + "/DadSchoolBoss/Resources/DadV2/Baked/" + name;
        System.IO.Directory.CreateDirectory(dir);
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i].texture == null) throw new System.InvalidOperationException("Missing silhouette " + i);
            System.IO.File.WriteAllBytes(dir + "/" + i + ".png", frames[i].texture.EncodeToPNG());
            DestroyImmediate(frames[i].texture);
        }
    }
#endif
    static Frame[] Slice(Texture2D texture, int count, bool keepReadable = false)
    {
        var frames = new Frame[count];
        if (texture == null) return frames;
        Color32[] pixels = texture.GetPixels32();
        int[] labels = new int[pixels.Length], queue = new int[pixels.Length];
        var islands = new List<Island>();
        int label = 0;
        for (int start = 0; start < pixels.Length; start++)
        {
            if (labels[start] != 0 || pixels[start].a < 8) continue;
            label++; var island = new Island { label = label, left = texture.width, bottom = texture.height };
            int head = 0, tail = 1; queue[0] = start; labels[start] = label;
            while (head < tail)
            {
                int p = queue[head++], x = p % texture.width, y = p / texture.width;
                island.count++; island.left = Mathf.Min(island.left, x); island.right = Mathf.Max(island.right, x);
                island.bottom = Mathf.Min(island.bottom, y); island.top = Mathf.Max(island.top, y);
                if (x > 0) Enqueue(p - 1, label, pixels, labels, queue, ref tail);
                if (x < texture.width - 1) Enqueue(p + 1, label, pixels, labels, queue, ref tail);
                if (y > 0) Enqueue(p - texture.width, label, pixels, labels, queue, ref tail);
                if (y < texture.height - 1) Enqueue(p + texture.width, label, pixels, labels, queue, ref tail);
            }
            if (island.count > 1000) islands.Add(island);
        }
        islands.Sort((a, b) => b.count.CompareTo(a.count));
        if (islands.Count > count) islands.RemoveRange(count, islands.Count - count);
        islands.Sort((a, b) => a.left.CompareTo(b.left));
        if (islands.Count != count) Debug.LogError("Expected " + count + " sprite silhouettes in " + texture.name);
        int tallest = 1;
        for (int cell = 0; cell < islands.Count; cell++)
        {
            Island island = islands[cell]; int w = island.right - island.left + 1, h = island.top - island.bottom + 1;
            var cropped = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int p = (y + island.bottom) * texture.width + x + island.left;
                if (labels[p] == island.label) cropped[y * w + x] = pixels[p];
            }
            var sprite = new Texture2D(w, h, TextureFormat.RGBA32, false); sprite.name = texture.name + " frame " + cell;
            sprite.wrapMode = TextureWrapMode.Clamp; sprite.SetPixels32(cropped); sprite.Apply(false, !keepReadable);
            frames[cell] = new Frame { texture = sprite, aspect = w / (float)h, relativeHeight = h, owned = true };
            tallest = Mathf.Max(tallest, h);
        }
        for (int i = 0; i < count; i++) frames[i].relativeHeight /= tallest;
        return frames;
    }
    static void Enqueue(int p, int label, Color32[] pixels, int[] labels, int[] queue, ref int tail)
    { if (labels[p] == 0 && pixels[p].a >= 8) { labels[p] = label; queue[tail++] = p; } }

    public void StartRun()
    {
        Combat = new AlleyCombat(0); Combat.SetArena(PlayArea);
        Combat.hazardSpawnClock = 9999; // Environmental readability first; no random explosive barrels.
        Combat.player = new Vector2(270, 570);
        ConfigureGear(Combat, Gear);
        Director = new SchoolRunDirector(); Director.Begin(Combat); ResetJuice();
        Combat.Start(); Home = false; Paused = false; resultSaved = false;
        dragging = false; finger = -1; stick = Vector2.zero; specialRequested = false;
        popups.Clear(); banner = "드래그로 다가가세요 · 펀치는 자동!"; bannerTime = 2.5f; resultReason = "";
    }

    public void SelectGear(int value)
    {
        if (value == 1 && !ChainGearUnlocked) return;
        if (value == 2 && !PhoneGearUnlocked) return;
        Gear = Mathf.Clamp(value, 0, 2); PlayerPrefs.SetInt("DadV2.Gear", Gear); PlayerPrefs.Save();
    }
    public static void ConfigureGear(AlleyCombat combat, int gear)
    {
        combat.wallSkill = gear == 0 ? 1 : 0; combat.chainSkill = gear == 1 ? 1 : 0;
        combat.punchKnockbackMultiplier = gear == 0 ? 1.25f : 1;
        combat.phoneSpecial = gear == 2; combat.phoneHeal = gear == 2 ? 6 : 0; combat.feverDuration = 2.4f; combat.chargeGainMultiplier = .55f;
    }
    public void GoHome() { Home = true; Paused = false; dragging = false; stick = Vector2.zero; }
    float ViewScale { get { return Mathf.Min(Screen.safeArea.width / Width, Screen.safeArea.height / Height); } }
    Vector2 Offset { get { Rect s = Screen.safeArea; return new Vector2(s.x + (s.width - Width * ViewScale) / 2, Screen.height - s.yMax + (s.height - Height * ViewScale) / 2); } }
    Vector2 Pointer(Vector2 p) { return (new Vector2(p.x, Screen.height - p.y) - Offset) / ViewScale; }
    static readonly Rect SpecialRect = new Rect(377, 820, 139, 106);
    void ReadPointer(Vector2 p, bool began, bool held, bool ended, int id)
    {
        if (began && p.y > 160 && p.y < 935 && p.x >= 0 && p.x <= Width && !SpecialRect.Contains(p) && !dragging)
        { dragging = true; finger = id; dragOrigin = p; }
        if (!dragging || finger != id) return;
        if (ended) { dragging = false; stick = Vector2.zero; finger = -1; }
        else if (held) stick = Vector2.ClampMagnitude((p - dragOrigin) / 48, 1);
    }
    void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f); clock += dt;
        shake = Mathf.Max(0, shake - dt * 25); bannerTime = Mathf.Max(0, bannerTime - dt);
        for (int i = popups.Count - 1; i >= 0; i--) { popups[i].life -= dt; popups[i].position.y -= dt * 38; if (popups[i].life <= 0) popups.RemoveAt(i); }
        if (Home || Combat == null) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { if (Combat.state == RunState.Fighting) Paused = !Paused; }
        if (Paused || Combat.state != RunState.Fighting) { dragging = false; stick = Vector2.zero; specialRequested = false; SaveResult(); return; }
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++) { Touch t = Input.GetTouch(i); ReadPointer(Pointer(t.position), t.phase == TouchPhase.Began, true, t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled, t.fingerId); }
        }
        else ReadPointer(Pointer(Input.mousePosition), Input.GetMouseButtonDown(0), Input.GetMouseButton(0), Input.GetMouseButtonUp(0), -1);
        Vector2 movement = stick + new Vector2(Input.GetAxisRaw("Horizontal"), -Input.GetAxisRaw("Vertical"));
        RememberEnemies();
        Combat.Tick(dt, movement, specialRequested || Input.GetKeyDown(KeyCode.Space)); specialRequested = false;
        int oldLesson = Director.Lesson;
        Director.Tick(Combat); UpdateJuice(dt);
        if (Director.Lesson > oldLesson) { banner = Director.Lesson == 1 ? "펀치 성공! 이번엔 벽을 노려보세요" : Director.Lesson == 2 ? "벽꿍 성공! 적 둘을 일렬로!" : "연쇄 성공! 이제 감 잡았죠?"; bannerTime = 2.5f; }
        if (Combat.elapsed >= RunLimit && Combat.state == RunState.Fighting) { Combat.health = 0; Combat.state = RunState.Lost; resultReason = "수업 종이 울렸습니다!"; }
        foreach (CombatEvent e in Combat.events)
        {
            ImpactJuice(e);
            if (e.text == "쟁반 짱 등장") { banner = "쟁반 짱!  위아래나 뒤에서 때리세요"; bannerTime = 2.8f; }
            if (e.text == "골목 대장 등장") { banner = "학교 짱 등장!  졸개로 맞히세요"; bannerTime = 2.5f; }
            if (e.text == "대장 광폭화!!") { banner = "학교 짱이 제대로 삐졌습니다"; bannerTime = 2; }
            if (e.style > 0)
            {
                string text = e.text == "전성기의 한 방" ? "아빠 화났다!!" : e.text == "BOSS DOWN" ? "도시락 배달 완료!" : e.text;
                popups.Add(new Popup { text = text, position = e.pos - new Vector2(0, 70), style = e.style });
                shake = Mathf.Max(shake, e.style >= 2 ? 4 : 2);
                bool wallImpact = e.text.StartsWith("벽꿍") || e.text.StartsWith("폭파벽");
                bool clang = e.text == "쟁반 막기!" || e.text == "팅!" || e.text == "쟁반 박살!";
                audioSource.pitch = clang ? 1.75f : wallImpact ? .8f : 1 + Mathf.Min(Combat.chain, 6) * .045f;
                audioSource.PlayOneShot(wallImpact || e.style >= 2 ? wallClip : hitClip, wallImpact ? .34f : .18f);
            }
        }
        Combat.events.Clear(); SaveResult();
    }
    void SaveResult()
    {
        if (resultSaved || Combat == null || (Combat.state != RunState.Won && Combat.state != RunState.Lost)) return;
        resultSaved = true; BestChain = Mathf.Max(BestChain, Combat.bestChain);
        TotalWallHits += Combat.wallHits;
        if (Combat.state == RunState.Won) Wins++;
        PlayerPrefs.SetInt("DadV2.BestChain", BestChain); PlayerPrefs.SetInt("DadV2.Wins", Wins);
        PlayerPrefs.SetInt("DadV2.TotalWallHits", TotalWallHits); PlayerPrefs.Save();
    }
    void OnApplicationFocus(bool focused) { if (!focused) { dragging = false; stick = Vector2.zero; if (!Home && Combat != null && Combat.state == RunState.Fighting) Paused = true; } }
    void OnDestroy()
    {
        if (disc != null) Destroy(disc); if (hitClip != null) Destroy(hitClip); if (wallClip != null) Destroy(wallClip);
        if (heroFrames != null) foreach (Frame f in heroFrames) if (f.owned && f.texture != null) Destroy(f.texture);
        if (enemyFrames != null) foreach (Frame f in enemyFrames) if (f.owned && f.texture != null) Destroy(f.texture);
    }
    static AudioClip MakeSound(float hz, float length)
    {
        float[] data = new float[(int)(22050 * length)];
        for (int i = 0; i < data.Length; i++) { float t = i / 22050f; data[i] = Mathf.Sin(t * hz * Mathf.PI * 2 * (1 - t * 3)) * Mathf.Pow(1 - i / (float)data.Length, 2) * .35f; }
        AudioClip clip = AudioClip.Create("Dad impact", data.Length, 1, 22050, false); clip.SetData(data, 0); return clip;
    }

    void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }
    void Panel(Rect r, Color c) { Box(r, Ink); Box(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), c); }
    void Text(Rect r, string s, int size = 20, Color? color = null, bool display = false, TextAnchor align = TextAnchor.MiddleLeft)
    { GUI.Label(r, s, new GUIStyle { font = display ? title : body, fontSize = size, alignment = align, wordWrap = true, normal = { textColor = color ?? Ink } }); }
    bool Button(Rect r, string s, Color c, bool enabled = true, int size = 23)
    {
        Panel(new Rect(r.x, r.y + 5, r.width, r.height), Ink); Panel(r, enabled ? c : new Color(.6f, .6f, .55f));
        Text(r, s, size, Ink, true, TextAnchor.MiddleCenter);
        bool previous = GUI.enabled; GUI.enabled = previous && enabled;
        bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none); GUI.enabled = previous; return clicked;
    }
    void Bar(Rect r, float value, Color c) { Box(r, Ink); Box(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(value), r.height - 4), c); }
    void Ellipse(Vector2 center, float w, float h, Color c) { GUI.color = c; GUI.DrawTexture(new Rect(center.x - w / 2, center.y - h / 2, w, h), disc); GUI.color = Color.white; }
    void Line(Vector2 a, Vector2 b, float width, Color c)
    {
        // Rotate in the current (scaled, offset, shaking) GUI space. GUIUtility.RotateAroundPivot treats the pivot as
        // screen space, which threw every non-horizontal line off position once the view was scaled.
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(a.x, a.y, 0), Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg), Vector3.one);
        Box(new Rect(0, -width / 2, (b - a).magnitude, width), c); GUI.matrix = old;
    }
    void Sprite(Texture2D atlas, Frame[] frames, int index, Vector2 feet, float height, bool flip, float tilt = 0, bool flash = false, Vector2? stretch = null, float opacity = 1)
    {
        if (atlas == null) return;
        Frame frame = frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        float h = height * frame.relativeHeight, w = h * frame.aspect;
        Matrix4x4 old = GUI.matrix;
        Vector2 shape = stretch ?? Vector2.one;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(feet.x, feet.y, 0), Quaternion.Euler(0, 0, tilt), new Vector3((flip ? -1 : 1) * shape.x, shape.y, 1));
        GUI.color = flash ? new Color(1, .48f, .35f, opacity) : new Color(1, 1, 1, opacity);
        if (frame.texture != null) GUI.DrawTexture(new Rect(-w / 2, -h, w, h), frame.texture);
        GUI.color = Color.white; GUI.matrix = old;
    }
    void OnGUI()
    {
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(new Vector3(Offset.x, Offset.y, 0), Quaternion.identity, Vector3.one * ViewScale);
        Box(new Rect(0, 0, Width, Height), Paper);
        if (Home) DrawHome(); else if (Combat != null) DrawBattle();
        GUI.matrix = previous;
    }
    void Backdrop()
    {
        if (arena != null) GUI.DrawTexture(new Rect(0, 140, Width, 660), arena, ScaleMode.StretchToFill);
        else Box(new Rect(0, 140, Width, 660), new Color(.68f, .69f, .58f));
    }
    void DrawHome()
    {
        Backdrop(); Box(new Rect(0, 0, Width, 350), Paper);
        Text(new Rect(24, 18, 492, 35), "도시락 배달 갔다가 학교를 접수했다", 18, Green);
        Text(new Rect(23, 66, 500, 250), "아빠가\n학교 짱을\n너무 잘 잡음", 55, Ink, true);
        Ellipse(new Vector2(265, 702), 220, 38, new Color(0, 0, 0, .2f));
        Sprite(hero, heroFrames, 0, new Vector2(267, 700), 375, false);
        Panel(new Rect(25, 718, 490, 57), Paper);
        Text(new Rect(37, 720, 466, 50), "최고 " + BestChain + "연쇄   /   배달 성공 " + Wins + "회", 21, Ink, false, TextAnchor.MiddleCenter);
        if (Button(new Rect(25, 795, 490, 66), "도시락 배달 출발!", Yellow, true, 29)) StartRun();
        if (Button(new Rect(25, 882, 490, 48), "아빠 장비  ·  " + GearName, Paper, true, 21)) gearPanel = true;
        if (gearPanel)
        {
            Box(new Rect(0, 0, Width, Height), new Color(0, 0, 0, .72f)); Panel(new Rect(25, 160, 490, 650), Paper);
            Text(new Rect(45, 178, 450, 70), "아빠의 준비물", 35, Ink, true);
            if (Button(new Rect(45, 270, 450, 95), "고무 슬리퍼\n날리는 힘 +25% · 벽 피해 +1", Gear == 0 ? Yellow : Color.white, true, 21)) SelectGear(0);
            if (Button(new Rect(45, 390, 450, 95), ChainGearUnlocked ? "튼튼한 도시락\n처음부터 충돌 전달 강화" : "튼튼한 도시락  [잠김]\n3연쇄 또는 첫 배달 성공", Gear == 1 ? Yellow : Color.white, ChainGearUnlocked, 21)) SelectGear(1);
            if (Button(new Rect(45, 510, 450, 110), PhoneGearUnlocked ? "폴더폰\n필살기가 공포 파동으로 변경 · 체력 +6\n넓게 밀기·돌진 취소·짱 겁먹음 / 폭주 없음" : "폴더폰  [잠김]\n누적 벽꿍 " + TotalWallHits + " / " + PhoneWallGoal + "회", Gear == 2 ? Yellow : Color.white, PhoneGearUnlocked, 20)) SelectGear(2);
            if (Button(new Rect(45, 710, 450, 60), "준비 완료", Green)) gearPanel = false;
        }
    }
    void DrawBattle()
    {
        Backdrop();
        Matrix4x4 stageMatrix = GUI.matrix;
        GUI.matrix *= Matrix4x4.Translate(new Vector3(Mathf.Sin(clock * 61) * shake, Mathf.Cos(clock * 49) * shake * .5f, 0));
        Rect a = PlayArea;
        Line(new Vector2(a.xMin, a.yMin), new Vector2(a.xMin, a.yMax), 5, Green);
        Line(new Vector2(a.xMax, a.yMin), new Vector2(a.xMax, a.yMax), 5, Green);
        Line(new Vector2(a.xMin, a.yMax), new Vector2(a.xMax, a.yMax), 3, Green);
        DrawLockers(); DrawCorpses();
        foreach (Fighter e in Combat.enemies)
        {
            if (e.windup > 0)
            {
                Vector2 end = e.pos + e.aim * (e.kind >= 1 ? 220 : 80);
                Line(e.pos, end, e.kind >= 1 ? 16 : 6, new Color(.9f, .2f, .1f, .3f));
                Ellipse(end, 22, 12, Red);
            }
        }
        foreach (CoinDrop c in Combat.coins) { Ellipse(c.pos - new Vector2(0, c.height), 12, 12, Ink); Ellipse(c.pos - new Vector2(0, c.height + 1), 8, 8, Yellow); }
        foreach (Shockwave wave in Combat.shockwaves)
        {
            Color color = wave.color; color.a = wave.life / wave.maxLife;
            for (int i = 0; i < 24; i++) { float t = i * Mathf.PI / 12; Vector2 p = wave.pos + new Vector2(Mathf.Cos(t), Mathf.Sin(t) * .55f) * wave.radius; Ellipse(p, 5, 5, color); }
        }
        actors.Clear(); actors.Add(new Actor { feet = Combat.player });
        foreach (Fighter e in Combat.enemies) actors.Add(new Actor { enemy = e, feet = e.pos });
        actors.Sort((x, y) => x.feet.y.CompareTo(y.feet.y));
        foreach (Actor actor in actors)
        {
            Fighter e = actor.enemy;
            if (e == null)
            {
                Ellipse(actor.feet, 57, 15, new Color(0, 0, 0, .22f));
                int pose = Combat.attackLead > 0 ? 0 : Combat.punch > .01f ? 2 : Combat.attackAnimation > .05f ? 3 : Combat.moveVelocity.magnitude > 15 && Mathf.Sin(clock * 12) > 0 ? 1 : 0;
                float bob = pose == 1 ? 3 : 0;
                Vector2 shape = Combat.attackLead > 0 ? new Vector2(.92f, 1.035f) : pose == 2 ? new Vector2(1.09f, .96f) : pose == 3 ? new Vector2(.97f, 1.02f) : Vector2.one;
                float lean = Combat.attackLead > 0 ? -Combat.direction.x * 9 : Combat.playerReaction > 0 ? -9 : 0;
                Sprite(hero, heroFrames, pose, actor.feet - new Vector2(0, bob), 155, Combat.direction.x < -.1f, lean, Combat.invulnerable > 0 && Mathf.Sin(clock * 35) > .3f, shape);
                if (Combat.attackLead > 0) { Vector2 end = actor.feet + Combat.direction * 80; Line(actor.feet, end, 4, Yellow); Ellipse(end, 12, 8, Yellow); }
            }
            else
            {
                float size = e.boss ? 205 : e.kind == 1 ? 165 : 142;
                Ellipse(actor.feet, e.boss ? 90 : 50, 14, new Color(0, 0, 0, .22f));
                int frame = EnemyFrame(e);
                float bob = e.locomotion.magnitude > 20 ? Mathf.Sin(clock * 11 + e.id) * 2 : 0;
                bool wallSquash = e.wallCooldown > .18f;
                Vector2 shape = wallSquash ? new Vector2(1.22f, .70f) : e.reaction > 0 ? new Vector2(.9f, 1.1f) : e.windup > 0 ? new Vector2(1.07f, .94f) : Vector2.one;
                Sprite(enemies, enemyFrames, frame, actor.feet - new Vector2(0, e.lift + bob), size, e.facing > 0, e.reaction > 0 ? Mathf.Clamp(e.velocity.x * .04f, -32, 32) : e.windup > 0 ? -e.facing * 8 : 0, e.flash > 0, shape);
                float front = actor.feet.x + e.facing * size * .21f, chest = actor.feet.y - e.lift - bob;
                if (e.shield)
                {
                    // Tray rim glint marks the guarded side; it flickers while the tray guy is turning around.
                    Color rim = e.turnDelay > 0 && Mathf.Sin(clock * 40) > 0 ? new Color(1, .78f, .15f, .9f) : new Color(1, 1, 1, .85f);
                    Vector2 rimTop = new Vector2(front, chest - size * .70f), rimBottom = new Vector2(front, chest - size * .28f);
                    Line(rimTop, rimBottom, 10, Ink); Line(rimTop, rimBottom, 5, rim);
                    for (int d = 0; d < e.trayHits; d++) Line(new Vector2(front - 6, chest - size * (.62f - d * .09f)), new Vector2(front + 6, chest - size * (.58f - d * .09f)), 3, Ink); // dents
                }
                else if (e.trayBroken)
                {
                    Vector2 dent = new Vector2(front, chest - size * .5f);
                    Line(dent - new Vector2(8, 12), dent + new Vector2(5, -2), 3, Ink); Line(dent + new Vector2(5, -2), dent - new Vector2(6, -12), 3, Ink);
                }
                if (e.hp < e.maxHp && !e.boss) Bar(new Rect(e.pos.x - 28, e.pos.y - size - 14, 56, 7), e.hp / (float)e.maxHp, Red);
                if (e.groggy > 0)
                {
                    // Dizzy stars: the boss is open to being launched into a wall.
                    Vector2 head = actor.feet - new Vector2(0, e.lift + size * .93f);
                    for (int s = 0; s < 3; s++) { float t = clock * 7 + s * 2.094f; Vector2 p = head + new Vector2(Mathf.Cos(t) * 36, Mathf.Sin(t) * 9); Ellipse(p, 17, 17, Ink); Ellipse(p, 11, 11, Yellow); }
                }
            }
        }
        DrawImpactJuice();
        foreach (Popup p in popups) Text(new Rect(p.position.x - 130, p.position.y, 260, 45), p.text, p.style >= 2 ? 28 : 21, p.style >= 2 ? Red : Ink, true, TextAnchor.MiddleCenter);
        GUI.matrix = stageMatrix;
        Panel(new Rect(0, 0, Width, 145), Paper);
        Text(new Rect(18, 8, 390, 35), "1층 복도  /  도시락 배달", 24, Ink, true);
        if (Button(new Rect(454, 13, 68, 43), "정지", Color.white, true, 18)) { Paused = true; dragging = false; stick = Vector2.zero; }
        Bar(new Rect(20, 60, 500, 23), Combat.health / Combat.maxHealth, Combat.health < 30 ? Red : Green);
        Text(new Rect(24, 59, 490, 24), "아빠 체력  " + Mathf.CeilToInt(Combat.health) + " / " + Combat.maxHealth, 14, Color.white);
        Fighter boss = Combat.enemies.Find(e => e.boss);
        if (boss != null)
        {
            bool groggy = boss.groggy > 0;
            Text(new Rect(20, 93, 90, 36), "학교 짱", 20, Red, true);
            Bar(new Rect(106, 100, 318, 22), boss.hp / (float)boss.maxHp, groggy ? Yellow : Red);
            Text(new Rect(106, 99, 318, 24), groggy ? "그로기!  벽으로 날려라" : boss.enraged ? "광폭화" : "", 14, groggy ? Ink : Color.white, true, TextAnchor.MiddleCenter);
            Text(new Rect(430, 93, 90, 36), Mathf.CeilToInt(Mathf.Max(0, RunLimit - Combat.elapsed)) + "초", 20, Ink, false, TextAnchor.MiddleRight);
        }
        else Text(new Rect(20, 93, 500, 36), "제압 " + Combat.kills + "/" + Combat.targetKills + "   벽꿍 " + Combat.wallHits + "   " + Mathf.CeilToInt(Mathf.Max(0, RunLimit - Combat.elapsed)) + "초", 20);
        Bar(new Rect(20, 137, 500, 8), Combat.xp / (float)Combat.nextXp, Yellow);
        if (bannerTime > 0) { Panel(new Rect(25, 160, 490, 54), Yellow); Text(new Rect(30, 161, 480, 50), banner, 23, Ink, true, TextAnchor.MiddleCenter); }
        else
        {
            // A paper pill keeps the hint readable when the boss stands at the top of the corridor.
            Box(new Rect(48, 163, 444, 32), new Color(Paper.r, Paper.g, Paper.b, .88f));
            Text(new Rect(30, 155, 480, 48), Director.Hint, 19, Ink, false, TextAnchor.MiddleCenter);
        }
        Panel(new Rect(0, 797, Width, 163), Paper);
        Text(new Rect(20, 810, 340, 36), "최고 " + Combat.bestChain + "연쇄  /  Lv." + Combat.level, 24, Ink, true);
        Text(new Rect(20, 854, 335, 80), "드래그 이동 · 가까우면 자동 펀치\nPC: 방향키 / WASD · 스페이스", 16);
        if (Button(SpecialRect, Combat.charge >= 100 ? (Gear == 2 ? "엄마한테\n전화한다!" : "아빠 화났다!\n필살기") : "충전 " + Mathf.FloorToInt(Combat.charge) + "%", Yellow, Combat.charge >= 100 && !Paused && Combat.state == RunState.Fighting, 23)) specialRequested = true;
        if (dragging) { Ellipse(dragOrigin, 88, 88, new Color(.12f, .13f, .12f, .2f)); Ellipse(dragOrigin + stick * 34, 32, 32, new Color(.12f, .13f, .12f, .55f)); }
        if (Combat.state == RunState.Choosing) DrawChoices();
        else if (Combat.state == RunState.Won || Combat.state == RunState.Lost) DrawResult();
        else if (Paused) DrawPause();
    }
    void DrawChoices()
    {
        Box(new Rect(0, 0, Width, Height), new Color(0, 0, 0, .77f));
        Text(new Rect(20, 170, 500, 75), "아빠의 잔기술", 40, Yellow, true, TextAnchor.MiddleCenter);
        Text(new Rect(20, 240, 500, 45), "하나 고르면 바로 전투로 돌아갑니다", 18, Color.white, false, TextAnchor.MiddleCenter);
        for (int i = 0; i < 3; i++)
        {
            int skill = Combat.currentChoices[i]; Rect r = new Rect(30, 325 + i * 155, 480, 128);
            if (Button(r, SkillNames[skill] + "   Lv." + (Combat.GetSkillRank(skill) + 1) + "\n" + SkillEffects[skill], Paper, true, 22)) { Combat.Choose(i); dragging = false; stick = Vector2.zero; }
        }
    }
    void DrawPause()
    {
        Box(new Rect(0, 0, Width, Height), new Color(0, 0, 0, .75f)); Panel(new Rect(35, 315, 470, 345), Paper);
        Text(new Rect(55, 335, 430, 80), "아빠 잠깐 숨 좀", 33, Ink, true, TextAnchor.MiddleCenter);
        if (Button(new Rect(65, 440, 410, 65), "계속 배달하기", Yellow)) Paused = false;
        if (Button(new Rect(65, 550, 410, 65), "집으로 돌아가기", Color.white)) GoHome();
    }
    void DrawResult()
    {
        bool won = Combat.state == RunState.Won;
        Box(new Rect(0, 0, Width, Height), new Color(0, 0, 0, .8f)); Panel(new Rect(25, 185, 490, 635), Paper);
        Text(new Rect(45, 210, 450, 80), won ? "도시락 배달 완료!" : "아빠도 가끔은 진다", 34, won ? Green : Red, true, TextAnchor.MiddleCenter);
        Text(new Rect(50, 305, 440, 110), "최대 " + Combat.bestChain + "연쇄\n벽꿍 " + Combat.wallHits + "회  ·  제압 " + Combat.kills + "명", 29, Ink, true, TextAnchor.MiddleCenter);
        Text(new Rect(55, 430, 430, 95), resultReason != "" ? resultReason : won ? "다음에는 더 크게 한 방!" : "돌진 예고를 피하고 벽 쪽으로 몰아보세요", 21, Ink, false, TextAnchor.MiddleCenter);
        Text(new Rect(55, 520, 430, 80), NextGoal, 18, Green, false, TextAnchor.MiddleCenter);
        if (Button(new Rect(50, 615, 440, 68), "한 판 더!", Yellow, true, 29)) StartRun();
        if (Button(new Rect(50, 715, 440, 58), "집으로 · 장비 바꾸기", Color.white)) GoHome();
    }
}
