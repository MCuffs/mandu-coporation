using UnityEngine;

// v11 opening (Design/dad-war-opening-assets-v11.md): black "Mandu Corporation" splash -> title key art -> 게임 시작 ->
// the six-cut 의리 테스트 story on a first play -> 1-1. The stills live in Resources/DadV11 (copied from Art/V11Opening);
// every word on screen is runtime text, never baked into the pictures.
public sealed partial class DadSchoolPrototype
{
    public enum OpeningStep { Done, Splash, Title, Story }
    public OpeningStep Opening { get; private set; }
    public bool InOpening { get { return Opening != OpeningStep.Done; } }
    public int StoryCut { get { return cut; } }
    public const string StorySeenKey = "DadWar.StorySeen";
    public bool StorySeen { get { return PlayerPrefs.GetInt(StorySeenKey, 0) == 1; } }
    // A save that already got past 1-1 predates the opening; the story is not forced on it.
    bool ReturningPlayer { get { return StorySeen || Progress.Save.bestStage > 1; } }
    const float SplashIn = .35f, SplashHold = 1f, SplashOut = .35f, CutFade = .18f;
    static readonly string[] StoryCuts = { "story_01_home", "story_02_prank", "story_03_where", "story_04_dash", "story_05_panic", "story_06_school" };
    static readonly float[] CutSeconds = { 3, 4, 2.5f, 1.5f, 2.5f, 2 };
    // Speaker / line pairs per cut (v11 table). Cut 2 names 지호 like the home subtitle; cut 6 runs into the fight wordless.
    static readonly string[][] CutLines =
    {
        new[] { "아들", "아빠… 나 오늘 싸웠는데." },
        new[] { "아들", "지호가 자기 아빠 데려왔대…\n나도 아빠 데려가야 할 것 같아." },
        new[] { "아빠", "어디야.", "아들", "학교 앞인데…" },
        new[] { "아빠", "가자." },
        new[] { "아들", "아빠?! 장난이야! 아빠!!" },
        new string[0],
    };
    float openingClock;
    int cut;
    bool replayingStory;   // a replay from the title goes back to the title instead of into 1-1
    Texture2D titleArt;
    Texture2D[] cutArt;

    public void ShowSplash() { Opening = OpeningStep.Splash; openingClock = 0; }
    public void ShowTitle() { Opening = OpeningStep.Title; openingClock = 0; }
    // 게임 시작: a new player watches the story and lands in 1-1; anyone else goes home.
    public void StartGame()
    {
        if (ReturningPlayer) GoHome();
        else PlayStory(false);
    }
    public void PlayStory(bool replay) { replayingStory = replay; Opening = OpeningStep.Story; cut = 0; openingClock = 0; }
    public void NextCut()
    {
        if (cut + 1 < StoryCuts.Length) { cut++; openingClock = 0; }
        else EndStory();
    }
    public void EndStory()
    {
        PlayerPrefs.SetInt(StorySeenKey, 1); PlayerPrefs.Save();
        if (replayingStory) ShowTitle(); else StartRun();
    }
    // Leaving the opening for good (home or a fight) also lets the stills go.
    void FinishOpening()
    {
        Opening = OpeningStep.Done;
        if (titleArt != null) { Resources.UnloadAsset(titleArt); titleArt = null; }
        if (cutArt != null) foreach (Texture2D t in cutArt) if (t != null) Resources.UnloadAsset(t);
        cutArt = null;
    }
    Texture2D TitleArt { get { if (titleArt == null) titleArt = Resources.Load<Texture2D>("DadV11/title_keyart"); return titleArt; } }
    Texture2D CutArt(int i)
    {
        if (cutArt == null) cutArt = new Texture2D[StoryCuts.Length];
        if (cutArt[i] == null) cutArt[i] = Resources.Load<Texture2D>("DadV11/" + StoryCuts[i]);
        return cutArt[i];
    }

    // True while the opening owns the frame; home and battle logic wait.
    bool UpdateOpening(float dt)
    {
        if (!InOpening) return false;
        openingClock += dt;
        bool next = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
        if (Opening == OpeningStep.Splash) { if (next || openingClock >= SplashIn + SplashHold + SplashOut) ShowTitle(); }
        else if (Opening == OpeningStep.Title) { if (next) StartGame(); }
        else if (Input.GetKeyDown(KeyCode.Escape)) EndStory();
        else if (next || openingClock >= CutSeconds[cut]) NextCut();
        return true;
    }

    void DrawOpening()
    {
        Box(new Rect(0, 0, Width, Height), Color.black);
        if (Opening == OpeningStep.Splash) DrawSplash();
        else if (Opening == OpeningStep.Title) DrawTitle();
        else DrawStory();
    }
    void DrawSplash()
    {
        float t = openingClock, a = t < SplashIn ? t / SplashIn : t < SplashIn + SplashHold ? 1 : 1 - (t - SplashIn - SplashHold) / SplashOut;
        Text(new Rect(0, 440, Width, 80), "파랑버섯 스튜디오", 34, new Color(1, 1, 1, Mathf.Clamp01(a)), false, TextAnchor.MiddleCenter);
        if (GUI.Button(new Rect(0, 0, Width, Height), GUIContent.none, GUIStyle.none)) ShowTitle();
    }
    void DrawTitle()
    {
        if (TitleArt != null) GUI.DrawTexture(new Rect(0, 0, Width, Height), TitleArt, ScaleMode.ScaleAndCrop);
        Text(new Rect(0, 92, Width, 34), "아빠, 장난이라니까!", 21, Ink, true, TextAnchor.MiddleCenter);
        OutlinedText(new Rect(0, 120, Width, 120), Title, 86, Yellow, Ink, 4);
        if (Button(new Rect(90, 806, 360, 84), "대소동 바로 시작", Yellow, true, 30)) StartQuickChallenge();
        if (Button(new Rect(90, 898, 360, 48), ReturningPlayer ? "본편 계속하기" : "이야기와 본편", Paper, true, 22)) StartGame();
        if (ReturningPlayer)
        {
            Rect replay = new Rect(170, 758, 200, 38);
            Box(replay, new Color(Paper.r, Paper.g, Paper.b, .85f));
            Text(replay, "이야기 다시 보기", 17, Ink, true, TextAnchor.MiddleCenter);
            if (GUI.Button(replay, GUIContent.none, GUIStyle.none)) { PlaySfx("sfx_button", .45f); PlayStory(true); }
        }
    }
    void DrawStory()
    {
        float t = openingClock, k = Mathf.Clamp01(t / CutSeconds[cut]);
        // v11 pacing: cuts 1-3 slow push-in, 4 a fast push, 5 a jolt then a held beat, 6 a run across the frame.
        float zoom = 1, dx = 0;
        if (cut <= 2) zoom = 1 + .05f * k;
        else if (cut == 3) zoom = 1.03f + .09f * (1 - (1 - k) * (1 - k));
        else if (cut == 4) { zoom = 1.03f; if (t < .18f) dx = Mathf.Sin(t * 110) * 7; }
        else { zoom = 1.08f; dx = Mathf.Lerp(18, -18, k); }
        Texture2D still = CutArt(cut);
        if(DadPreferences.ReducedMotion){zoom=1;dx=0;}
        float w=Width*zoom,visible=1/zoom;
        // Crop only the still's UVs, keeping all text in the normal screen coordinate space.
        // A GUI group would clip the high-density glyph rectangles before their inverse scale is applied.
        if(still!=null)GUI.DrawTextureWithTexCoords(new Rect(0,0,Width,Height),still,new Rect((1-visible)/2-dx/w,(1-visible)/2,visible,visible));
        float fade = 1 - Mathf.Clamp01(t / CutFade);
        if (fade > 0) Box(new Rect(0, 0, Width, Height), new Color(0, 0, 0, fade));
        // Subtitles sit on their own band at the bottom (lines are never part of the art).
        string[] lines = CutLines[cut];
        if (lines.Length > 0)
        {
            int rows = lines.Length / 2; float rowHeight = 46;
            for (int r = 0; r < rows; r++) if (lines[r * 2 + 1].Contains("\n")) rowHeight = 70;
            float top = 904 - rows * rowHeight;   // the band hugs the lines and keeps clear of the bottom edge
            Box(new Rect(0, top - 18, Width, Height - top + 18), new Color(0, 0, 0, .62f));
            for (int r = 0; r < rows; r++)
            {
                float y = top + r * rowHeight;
                Text(new Rect(26, y, 70, 40), lines[r * 2], 21, Yellow, true);
                Text(new Rect(96, y, 420, rowHeight), lines[r * 2 + 1], 24, Color.white, true, TextAnchor.UpperLeft);
            }
        }
        for (int i = 0; i < StoryCuts.Length; i++) Ellipse(new Vector2(30 + i * 18, 44), 10, 10, i == cut ? Yellow : new Color(1, 1, 1, .45f));
        Rect skip = new Rect(400, 22, 120, 44);
        Box(skip, new Color(Paper.r, Paper.g, Paper.b, .88f));
        Text(skip, "건너뛰기 ▶", 18, Ink, true, TextAnchor.MiddleCenter);
        // The skip control comes first so it claims its own tap; anywhere else moves to the next cut.
        if (GUI.Button(skip, GUIContent.none, GUIStyle.none)) { PlaySfx("sfx_button", .45f); EndStory(); }
        else if (GUI.Button(new Rect(0, 0, Width, Height), GUIContent.none, GUIStyle.none)) NextCut();
    }
    void OutlinedText(Rect r, string s, int size, Color fill, Color outline, float thickness)
    {
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4;
            Text(new Rect(r.x + Mathf.Cos(a) * thickness, r.y + Mathf.Sin(a) * thickness + 2, r.width, r.height), s, size, outline, true, TextAnchor.MiddleCenter);
        }
        Text(r, s, size, fill, true, TextAnchor.MiddleCenter);
    }
}
