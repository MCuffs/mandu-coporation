using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Everything that persists between runs: wallet, lunchboxes, upgrades, stage progress, stars and idle time.
[Serializable]
public sealed class DadSave
{
    public int version = 2;                                    // 2: real lunchbox gear (v1 saves get a starter set)
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
    public int[] gearRarity = { -1, -1, -1, -1, -1, -1 };    // per slot (DadProgress.SlotIds), -1 = empty
    public float[] gearValue = new float[DadProgress.SlotCount]; // power share each equipped item adds (0.05 = +5%)
    public int pendingSlot = -1, pendingRarity;                 // a lunchbox roll waiting for equip / dismantle
    public float pendingValue;
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
        if (Save.gearRarity == null || Save.gearRarity.Length != SlotCount)
        {
            int[] old = Save.gearRarity ?? new int[0];
            Save.gearRarity = new int[SlotCount];
            for (int i = 0; i < SlotCount; i++) Save.gearRarity[i] = i < old.Length ? old[i] : -1;
        }
        if (Save.gearValue == null || Save.gearValue.Length != SlotCount) Array.Resize(ref Save.gearValue, SlotCount);
        if (Save.version < 2)
        {
            // v1 counted opened lunchboxes as a log-curve bonus. Hand the same total back as a common item in every slot.
            bool empty = true; foreach (int r in Save.gearRarity) if (r >= 0) empty = false;
            if (empty && Save.boxesOpened > 0) for (int i = 0; i < SlotCount; i++) { Save.gearRarity[i] = 0; Save.gearValue[i] = GearValueAt(Save.boxesOpened); }
            Save.pendingSlot = -1; Save.version = 2;
        }
        if (Save.pendingSlot >= SlotCount || Save.pendingValue <= 0 || Save.pendingRarity < 0 || Save.pendingRarity >= RarityCount) Save.pendingSlot = -1;
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
    public float GearMultiplier { get { float m = 1; for (int i = 0; i < SlotCount; i++) if (HasGear(i)) m += Save.gearValue[i]; return m; } }
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

    // ---- lunchbox gear (6 slots x 3 rarities; card art Resources/DadV6, icons Resources/DadV5/gear) ----------------
    public const int SlotCount = 6, RarityCount = 3;
    public static readonly string[] SlotIds = { "shoes", "hand", "top", "head", "item", "tool" };
    public static readonly string[] SlotNames = { "신발", "손", "상의", "머리", "소지품", "생활용품" };
    public static readonly string[] RarityNames = { "일반", "희귀", "최고" };
    public static readonly string[] RarityCards = { "gear_card_common", "gear_card_rare", "gear_card_best" };
    static readonly string[] ItemNames =
    {
        "고무 슬리퍼", "삼선 슬리퍼", "낡은 등산화",   "목장갑", "빨간 고무장갑", "낡은 권투 글러브",
        "초록 조끼", "누런 런닝셔츠", "낚시 조끼",     "돋보기 안경", "아저씨 선글라스", "등산 모자",
        "폴더폰", "TV 리모컨", "둘둘 만 신문지",       "효자손", "장우산", "골프채",
    };
    public static string ItemName(int slot, int rarity) { return ItemNames[slot * RarityCount + rarity]; }
    public static string ItemIcon(int slot, int rarity) { return "gear_" + SlotIds[slot] + "_" + (rarity + 1); }
    public System.Random Rng = new System.Random();
    public bool HasGear(int slot) { return Save.gearRarity[slot] >= 0; }
    public float EquippedValue(int slot) { return HasGear(slot) ? Save.gearValue[slot] : 0; }
    // An item from the n-th box: the slot's share of the simulation's log curve (DadEconomy.gearLogScale).
    public float GearValueAt(int opened) { return Eco.gearLogScale * Mathf.Log(1 + opened / 10f) / SlotCount; }
    public double DismantleCoins(int rarity) { return Eco.BaseCoins(Mathf.Max(1, Save.bestStage - 1)) * Eco.gearDismantleShare[Mathf.Clamp(rarity, 0, RarityCount - 1)]; }
    public bool HasPendingGear { get { return Save.pendingSlot >= 0; } }
    public bool PendingIsUpgrade { get { return HasPendingGear && Save.pendingValue > EquippedValue(Save.pendingSlot); } }
    // Opens one box into a pending card. Empty slots fill first so the first six boxes dress dad head to toe.
    public bool OpenLunchbox()
    {
        if (Save.lunchboxes <= 0 || HasPendingGear) return false;
        Save.lunchboxes--; Save.boxesOpened++;
        int rarity = Save.boxesOpened == 1 ? Mathf.Clamp(Eco.gearFirstRarity, 0, RarityCount - 1) : Mathf.Min(Eco.RollRarity(Rng), RarityCount - 1);
        var empty = new List<int>();
        for (int i = 0; i < SlotCount; i++) if (!HasGear(i)) empty.Add(i);
        int slot = empty.Count > 0 ? empty[Rng.Next(empty.Count)] : Rng.Next(SlotCount);
        float jitter = 1 + Eco.gearJitter * (float)(Rng.NextDouble() * 2 - 1);
        Save.pendingSlot = slot; Save.pendingRarity = rarity;
        Save.pendingValue = GearValueAt(Save.boxesOpened) * Eco.gearRarityFactor[rarity] * jitter;
        return true;
    }
    // Equipping dismantles whatever the slot held; returns the coins that paid.
    public double EquipPending()
    {
        if (!HasPendingGear) return 0;
        int slot = Save.pendingSlot; double coins = HasGear(slot) ? DismantleCoins(Save.gearRarity[slot]) : 0;
        Save.gearRarity[slot] = Save.pendingRarity; Save.gearValue[slot] = Save.pendingValue;
        Save.coins += coins; Save.pendingSlot = -1;
        return coins;
    }
    public double DismantlePending()
    {
        if (!HasPendingGear) return 0;
        double coins = DismantleCoins(Save.pendingRarity);
        Save.coins += coins; Save.pendingSlot = -1;
        return coins;
    }
    public double ResolvePending() { return PendingIsUpgrade ? EquipPending() : DismantlePending(); }
    // "모두 열기": keep whatever is better, dismantle the rest.
    public int OpenAllLunchboxes(out int equipped, out double coins)
    {
        int n = 0; equipped = 0; coins = 0;
        if (HasPendingGear) { if (PendingIsUpgrade) equipped++; coins += ResolvePending(); }
        while (OpenLunchbox()) { n++; if (PendingIsUpgrade) equipped++; coins += ResolvePending(); }
        return n;
    }

    // ---- stage results --------------------------------------------------------------------------
    public int Stars(int stage) { return stage >= 1 && stage <= Save.stars.Length ? Save.stars[stage - 1] : 0; }
    public static int StarsFor(bool won, float hpRatio, int bestChain)
    {
        if (!won) return 0;
        return 1 + (hpRatio >= .5f ? 1 : 0) + (bestChain >= 6 ? 1 : 0);
    }
    public StageReward ApplyResult(int stage, bool won, float hpRatio, int bestChain) { return ApplyResult(stage, won, hpRatio, bestChain, StarsFor(won, hpRatio, bestChain)); }
    // The slingshot fight rates itself (clear, health at least half, shots within the target).
    public StageReward ApplyResult(int stage, bool won, float hpRatio, int bestChain, int stars)
    {
        var r = new StageReward { stage = stage, won = won, replay = stage < Save.bestStage || (stage == Eco.TotalStages && Stars(stage) > 0) };
        if (won)
        {
            r.firstClear = Stars(stage) == 0;
            r.coins = r.replay && !r.firstClear ? Eco.BaseCoins(stage) * Eco.replayCoinShare : Eco.ClearCoins(stage);
            r.boxes = Eco.boxesPerClear + (r.firstClear && Eco.IsFloorBoss(stage) ? Eco.floorBossFirstClearBoxes : 0);
            r.stars = Mathf.Clamp(stars, 1, 3);
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
