using System;
using UnityEngine;

// Balance numbers for the stage/upgrade economy. Resources/DadEconomy.json is the single source of truth:
// the game loads it here and Tools/progression_sim.py reads the same file, so the simulation and the build agree.
[Serializable]
public sealed class DadEconomy
{
    public int floors = 3, stagesPerFloor = 20;
    public float requiredBase = 72, requiredGrowth = 1.11f, midBossSpike = 1.3f, floorBossSpike = 1.8f, floorRelief = .85f, winSlope = 2.5f;
    public float basePower = 100, upgradePower = .05f, upgradeCostBase = 30, upgradeCostGrowth = 1.1f;
    public float coinBase = 40, coinGrowth = 1.12f, bossCoinMult = 3, failCoinShare = .3f, replayCoinShare = .5f;
    public int boxesPerClear = 1, floorBossFirstClearBoxes = 10;
    public float offlineClearsPerHour = 8, offlineBoxesPerHour = 2, offlineCapHours = 8, offlineMinMinutes = 1;
    public int stuckAssistAfter = 3;
    public float stuckAssistStep = .05f, stuckAssistCap = .25f;
    public float gearLogScale = .35f;
    // Lunchbox gear (6 slots x 3 rarities): an item rolled from the n-th box is worth gearLogScale*ln(1+n/10)/6 times its
    // rarity factor, so the equipped set tracks the log curve the simulation uses (Tools/progression_sim.py rolls the same way).
    public float[] gearRarityWeights = { 70, 25, 5 }, gearRarityFactor = { .8f, .95f, 1.15f }, gearDismantleShare = { .04f, .1f, .25f };
    public float gearJitter = .1f;
    public int gearFirstRarity = 1;
    public float combatHpBase = 1, combatDamageBase = 1, combatHpExponent = 1.5f, combatDamageExponent = 1.5f, combatScaleMin = .4f, combatScaleMax = 4;
    public int eliteHpMultiplier = 20;
    public float elitePower = 2, eliteKnockback = .45f;

    static DadEconomy cached;
    public static DadEconomy Load()
    {
        if (cached != null) return cached;
        var asset = Resources.Load<TextAsset>("DadEconomy");
        cached = asset != null ? JsonUtility.FromJson<DadEconomy>(asset.text) : new DadEconomy();
        return cached;
    }

    public int TotalStages { get { return floors * stagesPerFloor; } }
    public int Floor(int stage) { return (stage - 1) / stagesPerFloor + 1; }
    public int Number(int stage) { return (stage - 1) % stagesPerFloor + 1; }
    public bool IsFloorBoss(int stage) { return Number(stage) == stagesPerFloor; }
    public bool IsMidBoss(int stage) { return Number(stage) == stagesPerFloor / 2; }
    public bool IsBoss(int stage) { return IsFloorBoss(stage) || IsMidBoss(stage); }
    public string Label(int stage) { return Floor(stage) + "-" + Number(stage); }

    // Sawtooth: rises every stage, spikes at mid and floor bosses, relaxes on the first stage of a new floor.
    public float RequiredPower(int stage)
    {
        float req = requiredBase * Mathf.Pow(requiredGrowth, stage - 1);
        if (IsFloorBoss(stage)) req *= floorBossSpike;
        else if (IsMidBoss(stage)) req *= midBossSpike;
        else if (Number(stage) == 1 && stage > 1) req *= floorRelief;
        return req;
    }
    public double BaseCoins(int stage) { return coinBase * Math.Pow(coinGrowth, stage - 1); }
    public double ClearCoins(int stage) { return BaseCoins(stage) * (IsBoss(stage) ? bossCoinMult : 1); }
    public double UpgradeCost(int totalLevel) { return upgradeCostBase * Math.Pow(upgradeCostGrowth, totalLevel); }
    public int RollRarity(System.Random rng)
    {
        float total = 0; foreach (float w in gearRarityWeights) total += w;
        double roll = rng.NextDouble() * total;
        for (int r = 0; r < gearRarityWeights.Length; r++) { roll -= gearRarityWeights[r]; if (roll < 0) return r; }
        return gearRarityWeights.Length - 1;
    }
    // Design-model win chance (used by the progression simulation and as the calibration target for real combat).
    public float ModelWinChance(float power, float required) { return Mathf.Clamp(.5f + winSlope * (power / required - 1), .03f, .99f); }
}
