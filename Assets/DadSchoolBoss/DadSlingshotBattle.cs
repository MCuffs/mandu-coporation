using UnityEngine;

// The main game's battle is prototype B's slingshot fight (Design/dad-war-pm.md D-008): home, stage choice, rewards, saves
// and the result card stay here; DadSlingshotPrototype runs the fight on a child object (its own audio sources) and hands
// the finished simulation back. Upgrades and gear follow change request C-08 / C-10 (draft numbers, see the slingshot doc).
public sealed partial class DadSchoolPrototype
{
    // Editor audits of the old auto-punch screens switch this off for their run.
    public static bool UseSlingshot = true;
    DadSlingshotPrototype sling;
    int requestedRematch,previousShotRecord;
    public bool NewShotRecord {get;private set;}
    public bool SlingBattle { get; private set; }
    public SlingshotSim Sling { get { return SlingBattle && sling != null ? sling.Sim : null; } }
    public SlingshotSim SlingResult { get; private set; }
    public DadSlingshotPrototype SlingView { get { EnsureSling(); return sling; } }

    void EnsureSling()
    {
        if (sling == null)
        {
            Transform child = transform.Find("Slingshot");
            if (child == null) { child = new GameObject("Slingshot").transform; child.SetParent(transform, false); }
            sling = child.GetComponent<DadSlingshotPrototype>();
            if (sling == null) sling = child.gameObject.AddComponent<DadSlingshotPrototype>();
        }
        sling.OutfitProgress=Progress;
        sling.Embedded = true; sling.Finished = OnSlingFinished; sling.Quit = GoHome;   // delegates do not survive a script reload
    }

    public bool StartRematch(int stage, int rank)
    {
        if (!Progress.CanRematch(stage, rank)) return false;
        requestedRematch = rank; StartStage(stage); return true;
    }
    void StartSlingStage(int stage)
    {
        EnsureSling();
        previousShotRecord=Progress.BestShots(stage);NewShotRecord=false;
        DadEconomy eco = Progress.Eco;
        SlingshotSim.Stage st = SlingshotSim.ForMain(eco.Floor(stage), eco.Number(stage), DadSlingshotPrototype.Arena);
        if (requestedRematch > 0) st = SlingshotSim.Rematch(eco.Floor(stage), eco.Number(stage), requestedRematch, DadSlingshotPrototype.Arena);
        requestedRematch = 0;
        int[] up = Progress.Save.upgrades; float assist = st.challengeLevel > 0 ? 1 : Progress.StuckAssist(stage); int gear = Gear;
        sling.StartEmbedded(st, sim => { ApplyGrowth(sim, up, gear, assist); ApplyLunchboxGear(sim, Progress); });
        SlingBattle = true; SlingResult = null; Home = false; Paused = false; Combat = null; Director = null;
        popups.Clear(); resultReason = "";
    }
    // Upgrades in the fight, per level and capped (the 강화 tab reads the same numbers): 들이받기 = accumulated direct-hit damage, 체력 = dad's health (no cap), 도움닫기 = launch speed, 멀리 날리기 = how far a hit dad flies, +1 wall damage
    // from Lv.10. Special launches: 벽꿍형 adds wall damage, 연쇄형 sends hit dads further, 폭주형 starts with a rage shot.
    // 엄마의 응원 (losing streak) adds health.
    public static readonly float[] GrowthStep = { .03f, .08f, .015f, .02f };
    static readonly float[] GrowthCap = { .6f, float.MaxValue, .3f, .5f };
    public const int FarThrowWallLevel = 10;
    public const float ChainLaunchThrow = .15f;
    public static float GrowthBonus(int stat, int level) { return Mathf.Min(GrowthCap[stat], GrowthStep[stat] * level); }
    public static bool GrowthCapped(int stat, int level) { return GrowthStep[stat] * level >= GrowthCap[stat] - 1e-4f; }   // .03f * 20 lands just under .6f
    public static int AssistShots(float assist) { return Mathf.Clamp(Mathf.FloorToInt((assist-1)*20 + .001f),0,5); }
    public static void ApplyGrowth(SlingshotSim sim, int[] up, int gear, float assist)
    {
        if(sim.stage.challengeLevel==0 && sim.stage.shotLimit>0) sim.stage.shotLimit += AssistShots(assist);
        sim.knockBonus = 1; sim.impactBonus = GrowthBonus(0, up[0]);
        sim.SetHealth(100 * (1 + GrowthBonus(1, up[1])) * assist);
        sim.launchBonus = 1 + GrowthBonus(2, up[2]);
        sim.throwBonus = (1 + GrowthBonus(3, up[3])) * (gear == 1 ? 1 + ChainLaunchThrow : 1);
        sim.wallBonus = (up[3] >= FarThrowWallLevel ? 1 : 0) + (gear == 0 ? 1 : 0);
        if (gear == 2) sim.ragePending = true;
    }

    public static void ApplyLunchboxGear(SlingshotSim sim, DadProgress progress)
    {
        sim.bounceRetention = SlingshotSim.DadBounce + progress.GearBonus(0);
        sim.knockBonus *= 1 + progress.GearBonus(1);
        sim.SetHealth(sim.dad.maxHealth * (1 + progress.GearBonus(2)));
        sim.aimSlow = DadSlingshotPrototype.AimSlow - progress.GearBonus(3);
        sim.hurtGrace = .6f + progress.GearBonus(4);
        sim.throwBonus *= 1 + progress.GearBonus(5);
    }

    // ---- the 출발 card: what the next fight holds -------------------------------------------------------------------------
    public static readonly string[] KindNames = { "껌 아빠", "의자 아빠", "쟁반 아빠", "보스", "피구 아빠" };   // by SlingshotSim.Kind
    SlingshotSim.Stage cardStage; int cardStageNo; string cardFoe, cardTag, cardName; bool cardElite;
    public static SlingshotSim.Stage MainStage(DadEconomy eco, int stage) { return SlingshotSim.ForMain(eco.Floor(stage), eco.Number(stage), DadSlingshotPrototype.Arena); }
    // A role no earlier stage had (the first chair, tray and dodgeball dad), or -1. Plain dads and bosses are not roles.
    public static int NewKind(DadEconomy eco, int stage)
    {
        var seen = new System.Collections.Generic.HashSet<SlingshotSim.Kind> { SlingshotSim.Kind.Gum, SlingshotSim.Kind.Boss };
        for (int s = 1; s < stage; s++) foreach (SlingshotSim.Spawn sp in MainStage(eco, s).spawns) seen.Add(sp.kind);
        foreach (SlingshotSim.Spawn sp in MainStage(eco, stage).spawns) if (!seen.Contains(sp.kind)) return (int)sp.kind;
        return -1;
    }
    // Cached per stage: the stage, and the dad to show on the card (floor boss, big-dad or a first appearance).
    SlingshotSim.Stage CardStage(int stage)
    {
        if (cardStage != null && cardStageNo == stage) return cardStage;
        DadEconomy eco = Progress.Eco; int floorIndex = Mathf.Clamp(eco.Floor(stage) - 1, 0, 2), kind = NewKind(eco, stage);
        cardStage = MainStage(eco, stage); cardStageNo = stage; cardFoe = cardTag = cardName = null; cardElite = false;
        SlingshotSim.Spawn elite = cardStage.spawns.Find(sp => sp.elite);
        if (cardStage.spawns.Exists(sp => sp.kind == SlingshotSim.Kind.Boss)) { cardTag = "층 보스"; cardFoe = "boss" + (floorIndex + 1); cardName = BossNames[floorIndex]; }
        else if (elite != null) { cardTag = "큰아빠"; cardFoe = DadSlingshotPrototype.FoeId(elite.kind); cardName = EliteNames[floorIndex]; cardElite = true; }
        else if (kind >= 0) { cardTag = "첫 등장"; cardFoe = DadSlingshotPrototype.FoeId((SlingshotSim.Kind)kind); cardName = KindNames[kind]; }
        return cardStage;
    }

    void OnSlingFinished(SlingshotSim sim)
    {
        if (!SlingBattle || SlingResult != null) return;
        bool won = sim.phase == SlingshotSim.Phase.Won;
        SlingResult = sim;
        BestChain = Mathf.Max(BestChain, sim.bestChain); TotalWallHits += sim.wallSlams; if (won) Wins++;
        PlayerPrefs.SetInt("DadV2.BestChain", BestChain); PlayerPrefs.SetInt("DadV2.Wins", Wins);
        PlayerPrefs.SetInt("DadV2.TotalWallHits", TotalWallHits); PlayerPrefs.Save();
        LastReward = sim.stage.challengeLevel > 0
            ? Progress.ApplyRematch(PlayingStage, sim.stage.challengeLevel, won)
            : Progress.ApplyResult(PlayingStage, won, sim.dad.health / sim.dad.maxHealth, sim.bestChain, sim.Stars);
        NewShotRecord=won&&sim.stage.challengeLevel==0&&previousShotRecord>0&&sim.shots<previousShotRecord;
        if (sim.stage.challengeLevel == 0) Progress.RecordShots(PlayingStage, sim.shots, won);
        PlaySfx(won ? "jingle_win" : "jingle_lose", .8f);
        PersistNow();
    }
    // Editor checks: end the current fight at once with the given outcome.
    public void FinishSlingBattleNow(bool won)
    {
        SlingshotSim sim = Sling; if (sim == null) return;
        sim.phase = won ? SlingshotSim.Phase.Won : SlingshotSim.Phase.Lost;
        if (!won) sim.dad.health = 0;
        OnSlingFinished(sim);
    }
    void StopSling() { if (sling != null) sling.StopEmbedded(); SlingBattle = false; SlingResult = null; }

    // The result card over the frozen fight: shots against the target, best chain and wall slams (C-12).
    void DrawSlingResult()
    {
        SlingshotSim sim = SlingResult;
        bool won = sim.phase == SlingshotSim.Phase.Won;
        int floorIndex = Mathf.Clamp(sim.stage.floor - 1, 0, 2);
        SlingshotSim.Foe boss = null;   // a boss or big-dad still standing
        foreach (SlingshotSim.Foe f in sim.foes) if ((f.Boss || f.elite) && f.hp > 0) { boss = f; break; }
        string reason = !string.IsNullOrEmpty(sim.lossReason) ? sim.lossReason + " · 튕김과 연쇄로 발사를 아껴보세요" : !won && boss != null ? (boss.Boss ? BossNames[floorIndex] : EliteNames[floorIndex]) + " 체력 " + Mathf.CeilToInt(100f * boss.hp / Mathf.Max(1, boss.maxHp)) + "% 남음 · 아깝다!"
            : !won ? "다가오기 전에 벽으로! 튕겨서 여럿을 노려보세요"
            : sim.shots > sim.stage.par ? "목표 " + sim.stage.par + "발 안에 끝내면 별 하나 더"
            : sim.dad.health < sim.dad.maxHealth * .5f ? "체력을 반 넘게 지키면 별 하나 더" : "다음에는 더 크게 한 방!";
        if (won && sim.stage.challengeLevel > 0) reason = "발사 " + sim.shots + "/" + sim.stage.shotLimit + " · " + (sim.stage.challengeLevel < 3 ? "다음 단계는 지원군과 압박이 늘어납니다" : "재대결 최고 단계 정리 완료!");
        if(won&&sim.stage.challengeLevel==0)
        {
            reason="클리어 ★   ·   목표 "+sim.stage.par+"발 "+(sim.shots<=sim.stage.par?"★":"☆")+"   ·   체력 50% "+(sim.dad.health>=sim.dad.maxHealth*.5f?"★":"☆");
            if(NewShotRecord)reason="최소 발사 기록 "+previousShotRecord+" → "+sim.shots+"발!\n"+reason;
        }
        // The stage's boss or big-dad stars in the scene (flying on a win, still standing on a loss); otherwise a plain dad.
        SlingshotSim.Spawn key = sim.stage.spawns.Find(sp => sp.kind == SlingshotSim.Kind.Boss) ?? sim.stage.spawns.Find(sp => sp.elite);
        string foe = key == null ? "gum" : key.kind == SlingshotSim.Kind.Boss ? "boss" + (floorIndex + 1) : DadSlingshotPrototype.FoeId(key.kind);
        float stand = key == null ? 160 : Mathf.Min(230, DadSlingshotPrototype.FoeStand(key.kind, key.elite) * 1.1f);
        DrawReport(won, floorIndex, sim.stage.label + " · " + (sim.stage.challengeLevel > 0 ? sim.stage.title : StagePlan.FloorNames[floorIndex]),
            new[] { sim.stage.shotLimit > 0 ? "발사 / 한도" : "발사 / 목표", "최대 연쇄", "벽꿍" }, new[] { sim.shots + " / " + (sim.stage.shotLimit > 0 ? sim.stage.shotLimit : sim.stage.par), sim.bestChain.ToString(), sim.wallSlams.ToString() },
            reason, won && sim.stage.challengeLevel == 0 && Progress.Eco.IsFloorBoss(PlayingStage), () => DrawResultScene(won, foe, stand, key == null || boss != null));
    }
}
