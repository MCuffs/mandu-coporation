using System;
using System.IO;
using UnityEngine;

// Everything that persists between runs: wallet, lunchboxes, upgrades, stage progress, stars and idle time.
[Serializable]
public sealed class DadSave
{
    public int version = 1;
    public double coins;
    public int lunchboxes, boxesOpened;
    public int[] upgrades = new int[DadProgress.StatCount];   // fist, hp, combo, launch
    public int bestStage = 1;                                  // highest unlocked stage, 1-based
    public int currentStage = 1;                               // stage the start button plays
    public int[] stars = new int[0];                           // best stars per stage (index = stage - 1)
    public int lossStage, lossesAtStage;                       // consecutive losses on one stage, for the stuck assist
    public long lastSeenUnix;
    public double pendingIdleCoins;
    public int pendingIdleBoxes;
    public float pendingIdleHours;
}

public sealed class StageReward
{
    public int stage, stars;
    public bool won, firstClear, replay;
    public double coins;
    public int boxes;
}

public sealed class DadProgress
{
    public const int StatCount = 4;
    public static readonly string[] StatNames = { "주먹", "체력", "연타", "날리기" };
    public readonly DadEconomy Eco;
    public DadSave Save { get; private set; }
    readonly string path;

    public static string DefaultPath { get { return Path.Combine(Application.persistentDataPath, "dad-save.json"); } }
    public DadProgress(DadEconomy eco, string savePath) { Eco = eco; path = savePath; Load(); }

    public void Load()
    {
        Save = null;
        try { if (File.Exists(path)) Save = JsonUtility.FromJson<DadSave>(File.ReadAllText(path)); }
        catch (Exception e) { Debug.LogWarning("Save unreadable, starting fresh: " + e.Message); }
        if (Save == null) Save = new DadSave();
        if (Save.upgrades == null || Save.upgrades.Length != StatCount) Array.Resize(ref Save.upgrades, StatCount);
        if (Save.stars == null) Save.stars = new int[0];
        Save.bestStage = Mathf.Clamp(Save.bestStage, 1, Eco.TotalStages);
        Save.currentStage = Mathf.Clamp(Save.currentStage, 1, Save.bestStage);
    }
    public void Persist()
    {
        // Write-then-rename so a crash mid-write never leaves a half file behind.
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(Save));
        if (File.Exists(path)) File.Delete(path);
        File.Move(tmp, path);
    }

    // ---- power ----------------------------------------------------------------------------------
    public int TotalLevels { get { int sum = 0; foreach (int l in Save.upgrades) sum += l; return sum; } }
    // Stand-in for real gear until lunchbox opening lands: best-of-many gear grows with lunchboxes opened.
    public float GearMultiplier { get { return 1 + Eco.gearLogScale * Mathf.Log(1 + Save.boxesOpened / 10f); } }
    public float Power { get { return Eco.basePower * Mathf.Pow(1 + Eco.upgradePower, TotalLevels) * GearMultiplier; } }
    public float StuckAssist(int stage)
    {
        if (stage != Save.lossStage) return 1;
        int steps = Mathf.Max(0, Save.lossesAtStage - Eco.stuckAssistAfter + 1);
        return 1 + Mathf.Min(Eco.stuckAssistCap, steps * Eco.stuckAssistStep);
    }
    public float EffectivePower(int stage) { return Power * StuckAssist(stage); }

    // ---- upgrades (one shared cost track: any stat costs the same and adds the same power) -------
    public double NextUpgradeCost { get { return Eco.UpgradeCost(TotalLevels); } }
    public bool TryUpgrade(int stat)
    {
        if (stat < 0 || stat >= StatCount || Save.coins < NextUpgradeCost) return false;
        Save.coins -= NextUpgradeCost; Save.upgrades[stat]++;
        return true;
    }
    public int OpenAllLunchboxes() { int n = Save.lunchboxes; Save.boxesOpened += n; Save.lunchboxes = 0; return n; }

    // ---- stage results --------------------------------------------------------------------------
    public int Stars(int stage) { return stage >= 1 && stage <= Save.stars.Length ? Save.stars[stage - 1] : 0; }
    public static int StarsFor(bool won, float hpRatio, int bestChain)
    {
        if (!won) return 0;
        return 1 + (hpRatio >= .5f ? 1 : 0) + (bestChain >= 6 ? 1 : 0);
    }
    public StageReward ApplyResult(int stage, bool won, float hpRatio, int bestChain)
    {
        var r = new StageReward { stage = stage, won = won, replay = stage < Save.bestStage || (stage == Eco.TotalStages && Stars(stage) > 0) };
        if (won)
        {
            r.firstClear = Stars(stage) == 0;
            r.coins = r.replay && !r.firstClear ? Eco.BaseCoins(stage) * Eco.replayCoinShare : Eco.ClearCoins(stage);
            r.boxes = Eco.boxesPerClear + (r.firstClear && Eco.IsFloorBoss(stage) ? Eco.floorBossFirstClearBoxes : 0);
            r.stars = StarsFor(true, hpRatio, bestChain);
            if (Save.stars.Length < stage) Array.Resize(ref Save.stars, stage);
            Save.stars[stage - 1] = Mathf.Max(Save.stars[stage - 1], r.stars);
            if (stage == Save.bestStage && stage < Eco.TotalStages) Save.bestStage = stage + 1;
            Save.currentStage = Mathf.Min(Save.bestStage, stage + 1);
            if (Save.lossStage == stage) { Save.lossStage = 0; Save.lossesAtStage = 0; }
        }
        else
        {
            r.coins = Eco.ClearCoins(stage) * Eco.failCoinShare;
            if (Save.lossStage != stage) { Save.lossStage = stage; Save.lossesAtStage = 0; }
            Save.lossesAtStage++;
            Save.currentStage = stage;
        }
        Save.coins += r.coins; Save.lunchboxes += r.boxes;
        return r;
    }

    // ---- idle (offline) rewards ------------------------------------------------------------------
    public static long Unix(DateTime utc) { return (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds; }
    public void AccrueIdle(DateTime utcNow)
    {
        long now = Unix(utcNow);
        if (Save.lastSeenUnix > 0 && now > Save.lastSeenUnix)
        {
            float hours = (now - Save.lastSeenUnix) / 3600f;
            if (hours * 60 >= Eco.offlineMinMinutes)
            {
                float paid = Mathf.Min(hours, Eco.offlineCapHours - Save.pendingIdleHours);
                if (paid > 0)
                {
                    int farm = Mathf.Max(1, Save.bestStage - 1);
                    Save.pendingIdleCoins += Eco.offlineClearsPerHour * paid * Eco.BaseCoins(farm);
                    Save.pendingIdleBoxes += Mathf.FloorToInt(Eco.offlineBoxesPerHour * (Save.pendingIdleHours + paid)) - Mathf.FloorToInt(Eco.offlineBoxesPerHour * Save.pendingIdleHours);
                    Save.pendingIdleHours += paid;
                }
            }
        }
        Save.lastSeenUnix = now;
    }
    public bool HasIdleReward { get { return Save.pendingIdleCoins >= 1 || Save.pendingIdleBoxes > 0; } }
    public void ClaimIdle(int multiplier = 1)
    {
        Save.coins += Save.pendingIdleCoins * multiplier; Save.lunchboxes += Save.pendingIdleBoxes * multiplier;
        Save.pendingIdleCoins = 0; Save.pendingIdleBoxes = 0; Save.pendingIdleHours = 0;
    }
    public void Touch(DateTime utcNow) { Save.lastSeenUnix = Unix(utcNow); }
}
