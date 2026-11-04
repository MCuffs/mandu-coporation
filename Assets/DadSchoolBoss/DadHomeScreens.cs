using UnityEngine;
using SecondFistGame;

// v9 UI direction (Design/dad-ui-direction-v9.md) carried over to the 아빠 전쟁 story: three bottom tabs (도시락 · 출발 · 강화)
// in place of the stacked menu and gear pop-up, the home scene "tired dad, over-the-top result", and the result as a job
// report. Only systems that exist are shown (no shop, map, attendance or ad buttons yet; v2 product plan section 9).
public sealed partial class DadSchoolPrototype
{
    public enum HomeTab { Lunchbox, Go, Upgrade }
    public HomeTab Tab { get; set; } = HomeTab.Go;
    static readonly string[] TabNames = { "도시락", "출발", "강화" };
    static readonly string[] TabIcons = { "tab_lunchbox", "tab_go", "tab_upgrade" };
    static readonly string[] UpgradeQuips = { "주먹 쥘 일은 없었으면 했다", "계단은 아직 쉬었다 간다", "리모컨 누르던 엄지다", "이불 털던 어깨다" };
    static readonly string[] FloorDone = { "복도 정리 끝!", "급식실 정리 끝!", "체육관 정리 끝!" };
    static readonly Color Night = new Color(.15f, .24f, .18f);   // v9 top bar and result header
    static readonly Color Muted = new Color(.45f, .44f, .4f);
    const float BarHeight = 60, TabBarTop = 872;

    void DrawHome()
    {
        Backdrop();
        if (Tab == HomeTab.Go) DrawGoTab();
        else
        {
            Box(new Rect(0, 0, Width, Height), Paper);
            if (Tab == HomeTab.Lunchbox) DrawLunchboxTab(); else DrawUpgradeTab();
        }
        DrawWallet(); DrawTabBar();
    }
    void DrawWallet()
    {
        Box(new Rect(0, 0, Width, BarHeight), Night);
        Icon("icon_coin", new Rect(20, 12, 36, 36));
        Text(new Rect(64, 10, 220, 40), Mathf.FloorToInt((float)Progress.Save.coins).ToString("N0"), 24, Paper, true);
        Icon("icon_lunchbox", new Rect(388, 10, 40, 40));
        Text(new Rect(434, 10, 90, 40), Progress.Save.lunchboxes.ToString(), 24, Paper, true);
    }
    void DrawTabBar()
    {
        Box(new Rect(0, TabBarTop, Width, Height - TabBarTop), Paper); Box(new Rect(0, TabBarTop, Width, 3), Ink);
        for (int i = 0; i < TabNames.Length; i++)
        {
            Rect r = new Rect(i * Width / 3, TabBarTop + 3, Width / 3, Height - TabBarTop - 3);
            bool active = (int)Tab == i;
            if (active) Box(new Rect(r.x + 40, r.y, r.width - 80, 5), Yellow);
            Icon(TabIcons[i], new Rect(r.center.x - 25, r.y + 9, 50, 50));
            Text(new Rect(r.x, r.y + 58, r.width, 24), TabNames[i], 18, active ? Red : Ink, true, TextAnchor.MiddleCenter);
            // Badges point at the next thing to do: boxes waiting, or an upgrade that is affordable now.
            int boxes = Progress.Save.lunchboxes + (Progress.HasPendingGear ? 1 : 0);
            if (i == 0 && boxes > 0) Badge(new Vector2(r.center.x + 26, r.y + 14), boxes.ToString());
            if (i == 2 && Progress.Save.coins >= Progress.NextUpgradeCost) Badge(new Vector2(r.center.x + 26, r.y + 14), "!");
            if (GUI.Button(r, GUIContent.none, GUIStyle.none) && !active) { Tab = (HomeTab)i; PlaySfx("sfx_button", .45f); }
        }
    }
    void Badge(Vector2 c, string s)
    {
        Ellipse(c, 34, 34, Ink); Ellipse(c, 29, 29, Red);
        Text(new Rect(c.x - 20, c.y - 14, 40, 28), s, 16, Color.white, true, TextAnchor.MiddleCenter);
    }

    // ---- 출발: the son's line, the title and power, the scene, mom's idle stash, the stage and the go button ----------
    void DrawGoTab()
    {
        DadSave save = Progress.Save; int stage = save.currentStage, floor = Progress.Eco.Floor(stage);
        float power = Progress.EffectivePower(stage), required = Progress.Eco.RequiredPower(stage);
        Box(new Rect(0, BarHeight, Width, 146), Paper); Box(new Rect(0, BarHeight + 146, Width, 3), Ink);
        Text(new Rect(22, 70, 340, 26), "아들: \"지호가 자기 아빠 데려왔대…\"", 17, Red, true);
        Text(new Rect(18, 94, 330, 104), Title, 64, Ink, true);
        Text(new Rect(330, 82, 188, 26), "전투력", 17, Ink, true, TextAnchor.MiddleRight);
        Text(new Rect(290, 106, 228, 56), Mathf.RoundToInt(power).ToString("N0"), 42, Ink, true, TextAnchor.MiddleRight);
        DrawHomeScene(floor);

        // Mom's stash (idle reward): always on its own row so it never covers dad's face (v9).
        bool idle = Progress.HasIdleReward;
        Panel(new Rect(20, 596, 500, 78), Paper);
        Icon("btn_idle_chest", new Rect(30, 601, 68, 68));
        Text(new Rect(106, 604, 280, 34), "엄마가 챙겨 놨다", 22, Ink, true);
        Text(new Rect(106, 636, 290, 30), idle ? "코인 " + Mathf.FloorToInt((float)save.pendingIdleCoins).ToString("N0") + " · 도시락 " + save.pendingIdleBoxes
                                              : "모으는 중 · 최대 " + Mathf.RoundToInt(Progress.Eco.offlineCapHours) + "시간", 16, idle ? Ink : Muted);
        if (Button(new Rect(404, 610, 104, 50), "받기", Yellow, idle, 22)) { Progress.ClaimIdle(); PersistNow(); PlaySfx("sfx_coin", .7f); }

        Panel(new Rect(20, 684, 500, 66), Paper);
        Text(new Rect(34, 686, 330, 34), Progress.Eco.Label(stage) + " · " + StagePlan.FloorNames[Mathf.Clamp(floor - 1, 0, 2)], 24, Ink, true);
        bool weak = power < required;
        Text(new Rect(34, 716, 360, 30), "권장 " + Mathf.RoundToInt(required).ToString("N0") + (weak ? " · 조금 더 준비하고 갈까?" : " · 준비 완료"), 17, weak ? Red : Green, true);
        float assist = Progress.StuckAssist(stage);
        if (assist > 1.001f)
        {
            Icon("mom_cheer", new Rect(392, 690, 52, 52));
            Text(new Rect(442, 688, 76, 60), "엄마 응원\n+" + Mathf.RoundToInt((assist - 1) * 100) + "%", 15, Green, true);
        }
        if (Button(new Rect(20, 764, 500, 84), Progress.Eco.Label(stage) + " 출발!", Yellow, true, 38)) StartRun();
    }
    // v9 home: dad stands there unbothered while the last 지호 아빠 is still slumped on a dented locker. Existing v5/v6/v7
    // art arranged as decoration, not a new system and not a new background.
    void DrawHomeScene(int floor)
    {
        WallFace face = WallFaces[Mathf.Clamp(floor - 1, 0, WallFaces.Length - 1)];
        const float DadStand = 270, GumStand = 252, FloorY = 568;
        float gumX = face.backRight - WallBackOffset * GumStand / 560f, chest = FloorY - 60 * GumStand / 140f;
        if (art != null && art.Reward("locker_damage_2") != null) DrawDent(face, new Vector2(face.rightX, chest), 1, 2, false, GumStand / 140f);
        Ellipse(new Vector2(gumX - 20, FloorY), 150, 24, new Color(0, 0, 0, .2f));
        DrawCharacter("gum", "crumple", 0, new Vector2(gumX, FloorY), GumStand, false);
        Ellipse(new Vector2(214, FloorY), 190, 30, new Color(0, 0, 0, .22f));
        if (NewArt) DrawCharacter("dad", "idle", Mathf.FloorToInt(clock / .6f), new Vector2(214, FloorY), DadStand, false);
        else Sprite(hero, heroFrames, 0, new Vector2(214, FloorY), 300, false);
        Rect bubble = new Rect(24, 504, 128, 50);
        Line(new Vector2(130, 548), new Vector2(158, 572), 9, Ink); Line(new Vector2(130, 548), new Vector2(156, 569), 4, Color.white);
        Panel(bubble, Color.white);
        Text(bubble, "\"어디야.\"", 22, Ink, true, TextAnchor.MiddleCenter);
    }

    // ---- 도시락: dad between his six slots, the big lunchbox, odds in plain sight -----------------------------------------
    void DrawLunchboxTab()
    {
        DadSave save = Progress.Save;
        Text(new Rect(24, 70, 492, 54), "엄마가 뭘 넣었지?", 36, Ink, true);
        Text(new Rect(24, 120, 492, 26), "엄마: \"밥은 먹고 싸워.\"  열면 아빠 물건이 나온다", 16, Green, true);
        Panel(new Rect(20, 154, 500, 346), new Color(.84f, .86f, .78f));
        Ellipse(new Vector2(270, 474), 170, 26, new Color(0, 0, 0, .2f));
        if (NewArt) DrawCharacter("dad", "idle", Mathf.FloorToInt(clock / .6f), new Vector2(270, 474), 262, false);
        int equipped = 0;
        for (int i = 0; i < DadProgress.SlotCount; i++)
        {
            // v9 order: shoes, gloves, top down the left; head, pocket item, household tool down the right.
            Rect r = new Rect(i < 3 ? 34 : 422, 166 + (i % 3) * 110, 84, 84);
            DrawGearSlot(r, i);
            bool has = Progress.HasGear(i); if (has) equipped++;
            Text(new Rect(r.x - 12, r.yMax, r.width + 24, 22), has ? DadProgress.ItemName(i, save.gearRarity[i]) : DadProgress.SlotNames[i] + " 비어 있음", 13, has ? Ink : Muted, true, TextAnchor.MiddleCenter);
        }
        bool canOpen = save.lunchboxes > 0 || Progress.HasPendingGear;
        Rect card = new Rect(20, 512, 500, 110);
        if (Button(card, "", canOpen ? Yellow : new Color(.85f, .84f, .78f), canOpen)) BeginReveal();
        DrawLunchbox(new Vector2(card.x + 72, card.yMax - 12), 112, 0);
        Text(new Rect(150, 522, 360, 50), Progress.HasPendingGear ? "열어 둔 도시락 보기" : "도시락 열기 · " + save.lunchboxes, 30, Ink, true);
        Text(new Rect(150, 570, 360, 40), canOpen ? "더 좋은 장비는 장착, 남는 건 분해" : "스테이지를 깨면 도시락이 와요", 16, Ink);
        Panel(new Rect(20, 636, 500, 58), Paper);
        Text(new Rect(34, 638, 330, 54), "장비 전투력 +" + Mathf.RoundToInt((Progress.GearMultiplier - 1) * 100) + "%  ·  장착 " + equipped + "/" + DadProgress.SlotCount, 19, Ink, true);
        if (Button(new Rect(372, 642, 138, 44), "모두 열기", Color.white, save.lunchboxes > 1, 18)) OpenAllBoxes();
        // Odds come from the same economy data the rolls use (v2 6.2: publish the rates and the first-box rule together).
        float[] w = Progress.Eco.gearRarityWeights; float total = 0; foreach (float x in w) total += x;
        string odds = "";
        for (int i = 0; i < DadProgress.RarityCount && i < w.Length; i++) odds += (i > 0 ? " · " : "") + DadProgress.RarityNames[i] + " " + Mathf.RoundToInt(100 * w[i] / total) + "%";
        Box(new Rect(20, 708, 6, 66), Green);
        Text(new Rect(36, 708, 484, 30), "등급 확률  " + odds, 17, Ink, true);
        Text(new Rect(36, 738, 484, 36), "첫 도시락은 " + DadProgress.RarityNames[Mathf.Clamp(Progress.Eco.gearFirstRarity, 0, DadProgress.RarityCount - 1)] + " 확정 · 빈 부위부터 채워요", 15, Muted);
        Text(new Rect(24, 790, 492, 60), "\"밥 먹으려고 열었는데 효자손이 나왔다.\"", 18, Green, true, TextAnchor.MiddleCenter);
    }

    // ---- 강화: four rows with what each level really does, then the loadout that changes how a fight plays -------------
    void DrawUpgradeTab()
    {
        double cost = Progress.NextUpgradeCost; bool afford = Progress.Save.coins >= cost;
        int step = Mathf.RoundToInt(Progress.Eco.upgradePower * 100);
        Text(new Rect(24, 70, 492, 54), "생활 체력 단련", 36, Ink, true);
        Text(new Rect(24, 120, 492, 26), "어느 강화든 전투력 +" + step + "% · 다음 비용 " + Mathf.CeilToInt((float)cost).ToString("N0") + "코인", 16, Green, true);
        for (int i = 0; i < DadProgress.StatCount; i++)
        {
            Rect row = new Rect(20, 154 + i * 98, 500, 88); Panel(row, Paper);
            Icon(UpgradeIcons[i], new Rect(30, row.y + 10, 68, 68));
            Text(new Rect(108, row.y + 4, 180, 32), DadProgress.StatNames[i] + "  Lv." + Progress.Save.upgrades[i], 23, Ink, true);
            Text(new Rect(236, row.y + 8, 150, 26), UpgradeNow(i), 14, Green, true, TextAnchor.MiddleRight);
            Text(new Rect(108, row.y + 36, 280, 24), UpgradeEffect(i, step), 15, Ink);
            Text(new Rect(108, row.y + 59, 280, 24), UpgradeQuips[i], 14, Green);
            if (Button(new Rect(394, row.y + 16, 114, 56), "강화\n" + Mathf.CeilToInt((float)cost).ToString("N0"), afford ? Yellow : Color.white, afford, 19) && Progress.TryUpgrade(i))
            { PersistNow(); PlaySfx("sfx_skill_pick", .6f); }
        }
        Text(new Rect(24, 552, 492, 34), "출동 준비물  ·  싸우는 방식이 바뀝니다", 21, Ink, true);
        string[] names = { "고무 슬리퍼", "튼튼한 도시락", "폴더폰" };
        string[] effects = { "날리는 힘 +25%\n벽 피해 +1", ChainGearUnlocked ? "처음부터\n충돌 전달 강화" : "[잠김]\n3연쇄 또는 첫 승리", PhoneGearUnlocked ? "필살기가 공포 파동\n체력 +6 · 폭주 없음" : "[잠김]\n누적 벽꿍 " + TotalWallHits + "/" + PhoneWallGoal };
        bool[] open = { true, ChainGearUnlocked, PhoneGearUnlocked };
        for (int i = 0; i < 3; i++)
        {
            Rect c = new Rect(20 + i * 170, 590, 160, 150);
            if (Button(c, "", Gear == i ? Yellow : Color.white, open[i])) { SelectGear(i); }
            Text(new Rect(c.x + 6, c.y + 10, c.width - 12, 34), names[i], 20, Ink, true, TextAnchor.MiddleCenter);
            Text(new Rect(c.x + 8, c.y + 48, c.width - 16, 70), effects[i], 15, open[i] ? Ink : Muted, false, TextAnchor.UpperCenter);
            if (Gear == i) Text(new Rect(c.x, c.y + 116, c.width, 26), "장착 중", 16, Red, true, TextAnchor.MiddleCenter);
        }
        Text(new Rect(24, 754, 492, 56), NextGoal, 16, Green, false, TextAnchor.MiddleCenter);
    }
    // What a level really changes today: every level raises the shared power; combo and launch also feed combat directly
    // (ApplyPower). Fist and stamina are not split yet, so no separate number is shown for them (v9 강화).
    static string UpgradeEffect(int stat, int step)
    {
        return "전투력 +" + step + "%" + (stat == 2 ? " · 공격 간격 -1%" : stat == 3 ? " · 날리는 힘 +2%" : "");
    }
    // The combat bonus reached so far, with the caps ApplyPower uses (combo 30%, launch 60%).
    string UpgradeNow(int stat)
    {
        int lv = Progress.Save.upgrades[stat];
        return stat == 2 ? "지금 간격 -" + Mathf.Min(30, lv) + "%" : stat == 3 ? "지금 힘 +" + Mathf.Min(60, lv * 2) + "%" : "";
    }

    // ---- result: today's job report (v9 03) -----------------------------------------------------------------------------
    void DrawResult()
    {
        bool won = Combat.state == RunState.Won;
        int floorIndex = Mathf.Clamp(Director.Plan.floor - 1, 0, 2);
        Box(new Rect(0, 0, Width, Height), new Color(0, 0, 0, .8f));
        Panel(new Rect(20, 50, 500, 880), Paper);
        Box(new Rect(23, 53, 494, 50), Night);
        Text(new Rect(20, 53, 500, 50), Director.Plan.Label + " · " + Director.Plan.FloorName, 22, Paper, true, TextAnchor.MiddleCenter);
        Text(new Rect(20, 110, 500, 26), won ? "아빠 출동 중 발생한 일" : "오늘은 여기까지", 16, Ink, false, TextAnchor.MiddleCenter);
        Text(new Rect(20, 132, 500, 62), won ? FloorDone[floorIndex] : "아빠도 가끔은 진다", 44, won ? Ink : Red, true, TextAnchor.MiddleCenter);
        if (won && LastReward != null) for (int s = 0; s < 3; s++) Icon(s < LastReward.stars ? "star_full" : "star_empty", new Rect(Width / 2 - 66 + s * 44, 196, 40, 40));
        DrawResultScene(won);
        string[] labels = { "최대 연쇄", "벽꿍", "제압" }; int[] values = { Combat.bestChain, Combat.wallHits, Combat.kills };
        for (int i = 0; i < 3; i++)
        {
            Rect r = new Rect(32 + i * 164, 448, 148, 82); Panel(r, Color.white);
            Text(new Rect(r.x, r.y + 6, r.width, 24), labels[i], 15, Ink, true, TextAnchor.MiddleCenter);
            Text(new Rect(r.x, r.y + 30, r.width, 48), values[i].ToString(), 36, Red, true, TextAnchor.MiddleCenter);
        }
        if (LastReward != null)
        {
            Panel(new Rect(32, 542, 476, 66), new Color(.9f, .76f, .38f));
            string coins = "코인 +" + Mathf.RoundToInt((float)LastReward.coins) + (LastReward.replay && !LastReward.firstClear && won ? "  (다시 하기)" : "");
            if (Progress.Save.lunchboxes > 0 || Progress.HasPendingGear)
            {
                // The lunchbox reward is the button: tap it to open (v6 README, connection step 1).
                Text(new Rect(40, 546, 228, 58), coins, 22, Ink, true, TextAnchor.MiddleCenter);
                Rect box = new Rect(276, 548, 224, 54);
                if (Button(box, "", Yellow, true, 18)) BeginReveal();
                DrawLunchbox(new Vector2(box.x + 34, box.yMax - 6), 54 * (1 + .05f * Mathf.Sin(clock * 6)), 0);
                Text(new Rect(box.x + 62, box.y, 160, box.height), (LastReward.boxes > 0 ? "도시락 +" + LastReward.boxes + "  " : "") + "열기 (" + Mathf.Max(1, Progress.Save.lunchboxes) + ")", 18, Ink, true, TextAnchor.MiddleCenter);
            }
            else Text(new Rect(32, 542, 476, 66), coins + (LastReward.boxes > 0 ? "      도시락 +" + LastReward.boxes : ""), 23, Ink, true, TextAnchor.MiddleCenter);
        }
        Fighter key = Director.Key;
        bool floorCleared = won && Director.Plan.floorBoss, finalCleared = floorCleared && PlayingStage == Progress.Eco.TotalStages;
        string reason = resultReason != "" ? resultReason : floorCleared ? FloorClearLines[floorIndex] : won ? "다음에는 더 크게 한 방!" : key != null && key.hp > 0 ? (key.boss ? BossNames[floorIndex] : EliteNames[floorIndex]) + " 체력 " + Mathf.CeilToInt(100f * key.hp / key.maxHp) + "% 남음 · 아깝다!" : "돌진 예고를 피하고 벽 쪽으로 몰아보세요";
        Text(new Rect(44, 614, 452, 52), reason, 18, Ink, false, TextAnchor.MiddleCenter);
        if (finalCleared) Text(new Rect(36, 666, 468, 26), EndingLine, 16, Green, true, TextAnchor.MiddleCenter);
        else if (!won && Progress.StuckAssist(PlayingStage) > 1.001f)
        {
            Icon("mom_cheer", new Rect(120, 664, 30, 30));
            Text(new Rect(154, 666, 300, 26), "엄마의 응원 +" + Mathf.RoundToInt((Progress.StuckAssist(PlayingStage) - 1) * 100) + "% 적용 중", 17, Green, true);
        }
        Text(new Rect(44, 694, 452, 46), NextGoal, 15, Green, false, TextAnchor.MiddleCenter);
        int next = Progress.Save.currentStage;
        if (won) Text(new Rect(20, 742, 500, 24), "다음: " + Progress.Eco.Label(next) + " · " + StagePlan.FloorNames[Mathf.Clamp(Progress.Eco.Floor(next) - 1, 0, 2)], 16, Ink, true, TextAnchor.MiddleCenter);
        if (Button(new Rect(40, 772, 460, 74), won ? "다음 상대 보러 가기" : "다시 도전", Yellow, true, 30)) StartRun(won ? next : PlayingStage);
        if (Button(new Rect(40, 860, 460, 54), "집으로 · 강화하기", Color.white, true, 21)) { GoHome(); Tab = HomeTab.Upgrade; }
    }
    // Dad unbothered next to whoever just left the frame, with the red 처리 완료 stamp; on a loss, dad hurt and the one who
    // is still standing.
    void DrawResultScene(bool won)
    {
        Fighter key = Director.Key;
        string foe = key != null ? EnemyId(key) : "gum";
        float foeStand = key != null ? StandHeight(key) * 1.15f : 160;
        if (won)
        {
            DrawCharacter(foe, "fly", 0, new Vector2(382, 390), foeStand, false, -18);
            Ellipse(new Vector2(182, 432), 120, 18, new Color(0, 0, 0, .2f));
            DrawCharacter("dad", "idle", Mathf.FloorToInt(clock / .6f), new Vector2(182, 432), 186, false);
            Stamp(new Vector2(286, 416), "처리 완료", -8);
        }
        else
        {
            Ellipse(new Vector2(182, 432), 120, 18, new Color(0, 0, 0, .2f));
            DrawCharacter("dad", "hurt", 0, new Vector2(182, 432), 176, false);
            if (key == null || key.hp > 0) { Ellipse(new Vector2(372, 432), 110, 16, new Color(0, 0, 0, .2f)); DrawCharacter(foe, "walk", 0, new Vector2(372, 432), foeStand, false); }
        }
    }
    void Stamp(Vector2 c, string s, float angle)
    {
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(c.x, c.y, 0), Quaternion.Euler(0, 0, angle), Vector3.one);
        Color ink = new Color(Red.r, Red.g, Red.b, .92f);
        Rect r = new Rect(-74, -22, 148, 44);
        Box(new Rect(r.x, r.y, r.width, 4), ink); Box(new Rect(r.x, r.yMax - 4, r.width, 4), ink);
        Box(new Rect(r.x, r.y, 4, r.height), ink); Box(new Rect(r.xMax - 4, r.y, 4, r.height), ink);
        Text(r, s, 24, ink, true, TextAnchor.MiddleCenter);
        GUI.matrix = old;
    }
}
