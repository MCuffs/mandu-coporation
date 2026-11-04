using UnityEngine;
using SecondFistGame;

// What one stage contains. Built deterministically from (floor, number) so the same stage always plays the same way.
public sealed class StagePlan
{
    public static readonly string[] FloorNames = { "1층 복도", "2층 급식실", "3층 체육관" };
    public int stage, floor, number, lesson;           // lesson 1..3 on the first three stages (punch, wall, chain)
    public bool midBoss, floorBoss;
    public int[] waves;                                // enemies per wave (escorts already on the field for boss stages)
    public int maxAlive, maxTrays, eliteKind, bossHp;
    public float chargerShare, trayShare, timeLimit, required;
    public string Label { get { return floor + "-" + number; } }
    public string FloorName { get { return FloorNames[Mathf.Clamp(floor - 1, 0, FloorNames.Length - 1)]; } }
    public int WaveTotal { get { int n = 0; foreach (int w in waves) n += w; return n; } }

    public static StagePlan For(int stage, DadEconomy eco)
    {
        var p = new StagePlan { stage = stage, floor = eco.Floor(stage), number = eco.Number(stage), required = eco.RequiredPower(stage) };
        p.midBoss = eco.IsMidBoss(stage); p.floorBoss = eco.IsFloorBoss(stage);
        p.lesson = stage <= 3 ? stage : 0;
        int f = p.floor, n = p.number;
        p.chargerShare = stage < 4 ? 0 : .2f + .05f * (f - 1);
        p.trayShare = f == 1 && n < 12 ? 0 : .15f + .07f * (f - 1);
        p.maxTrays = 2;
        p.maxAlive = stage <= 3 ? 3 : Mathf.Min(6, (f == 1 && n < 10 ? 4 : 5) + (f >= 2 ? 1 : 0));
        p.eliteKind = f == 1 ? 0 : f == 2 ? 3 : 1;     // mid-boss: big gum guy, big tray guy (3 = tray), big chair guy
        p.bossHp = 28;
        if (p.floorBoss || p.midBoss) { p.waves = new[] { 2 }; p.timeLimit = p.floorBoss ? 90 : 75; return p; }
        int total = stage == 1 ? 5 : stage <= 3 ? 8 : Mathf.Min(26, 9 + (3 * n) / 4 + 3 * (f - 1));
        int count = stage <= 3 || n < 5 ? 2 : 3;
        p.waves = new int[count];
        for (int i = 0; i < count; i++) p.waves[i] = total / count + (i < total % count ? 1 : 0);
        p.timeLimit = 60;
        return p;
    }
}

// Runs one stage from its plan: teaching beats on 1-1..1-3, waves that walk in from the doors, an elite on stage 10
// and the floor boss on stage 20 with a trickle of escorts the player throws into it.
public sealed class SchoolRunDirector
{
    public const int MaxEscorts = 3, XpCurveScale = 2;
    public const float EscortInterval = 4.5f, SpawnInterval = .8f, WavePause = 1.5f, MinSpawnDistance = 170;
    public StagePlan Plan { get; private set; }
    public int Lesson { get; private set; }
    public bool BossReleased { get; private set; }
    public bool BossGroggy { get; private set; }
    public bool TrayOnField { get; private set; }
    public int Wave { get; private set; }
    public Fighter Key { get; private set; }            // the elite or floor boss whose defeat ends a boss stage
    public int Defeated { get; private set; }
    bool trayIntroduced, keySpawned;
    int spawnedInWave;
    float nextSpawn, nextEscort;

    public int PlannedTotal { get { return Plan.WaveTotal + (Plan.midBoss || Plan.floorBoss ? 1 : 0); } }
    public float Progress { get { return Plan.midBoss || Plan.floorBoss ? (Key == null ? 0 : 1 - Mathf.Clamp01(Key.hp / (float)Key.maxHp)) : Mathf.Clamp01(Defeated / (float)Mathf.Max(1, Plan.WaveTotal)); } }
    public string Hint
    {
        get
        {
            if (Plan.floorBoss) return BossGroggy ? "그로기!  지금 벽으로 날리세요" : "다른 아빠를 보스에게 날리면 그로기!";
            if (Plan.midBoss) return "큰아빠는 무거워요 · 벽꿍과 연쇄로 몰아붙이세요";
            if (Lesson == 0) return "① 드래그로 다가가면 아빠가 자동으로 펀치!";
            if (Lesson == 1) return "② 적의 뒤에 벽이 오도록 자리를 잡으세요";
            if (Lesson == 2) return "③ 적 둘을 일렬로 세워 한 번에 날리세요";
            if (TrayOnField) return "쟁반은 정면만 막아요 · 위아래나 뒤에서!";
            return "벽꿍과 연쇄로 정리!  ·  남은 적 " + Mathf.Max(0, Plan.WaveTotal - Defeated) + "명";
        }
    }

    int eliteHp = 20; float elitePower = 2;
    public void Begin(AlleyCombat combat, StagePlan plan)
    {
        var eco = DadEconomy.Load();
        eliteHp = eco.eliteHpMultiplier; elitePower = eco.elitePower; combat.eliteKnockback = eco.eliteKnockback;
        Plan = plan; Lesson = plan.lesson > 0 ? plan.lesson - 1 : 3;
        BossReleased = BossGroggy = TrayOnField = trayIntroduced = keySpawned = false; Key = null;
        Wave = 0; spawnedInWave = 0; Defeated = 0; nextSpawn = 1; nextEscort = 0;
        combat.xpCurveScale = XpCurveScale; combat.nextXp = 3 * combat.xpCurveScale; combat.groggyBoss = true;
        combat.spawning = false; combat.enemies.Clear();
        combat.charge = 0;   // the special is earned inside the stage; a full gauge at the start would end short stages in one press
        if (plan.lesson == 1)
        {
            // First punch of the game: one dazed target right in front of dad.
            Fighter first = combat.AddEnemy(new Vector2(combat.player.x + 132, combat.player.y));
            first.stun = 2.5f; first.attack = 2.5f; spawnedInWave = 1;
        }
        if (plan.midBoss || plan.floorBoss)
        {
            for (int i = 0; i < plan.waves[0]; i++) Walk(combat, combat.AddEnemy(Entrance(combat, i)));
            spawnedInWave = plan.waves[0]; nextSpawn = 1.2f;
        }
    }

    public void Tick(AlleyCombat combat)
    {
        if (combat.state != RunState.Fighting) return;
        if (Lesson == 0 && combat.hits > 0) Lesson = 1;
        if (Lesson == 1 && combat.wallHits > 0) Lesson = 2;
        if (Lesson == 2 && combat.chainHits > 0) Lesson = 3;
        CountDefeated(combat);
        TrayOnField = combat.enemies.Exists(e => e.shield);
        if (Plan.midBoss || Plan.floorBoss) { TickBossStage(combat); return; }
        if (Wave >= Plan.waves.Length) { if (combat.enemies.Count == 0) combat.state = RunState.Won; return; }
        if (spawnedInWave >= Plan.waves[Wave])
        {
            // The next wave starts once this one is cleared, after a short breather.
            if (combat.enemies.Count > 0) return;
            Wave++; spawnedInWave = 0; nextSpawn = combat.elapsed + WavePause;
            if (Wave < Plan.waves.Length) combat.events.Add(new CombatEvent("다음 무리 " + (Wave + 1) + "/" + Plan.waves.Length, combat.arena.center, 0));
            return;
        }
        if (combat.elapsed < nextSpawn || combat.enemies.Count >= Plan.maxAlive) return;
        nextSpawn = combat.elapsed + SpawnInterval;
        Spawn(combat);
    }

    void CountDefeated(AlleyCombat combat)
    {
        // AlleyCombat removes defeated enemies from its list, so defeated = spawned so far - still alive.
        Defeated = Mathf.Max(Defeated, SpawnedSoFar() - combat.enemies.Count);
    }
    int SpawnedSoFar()
    {
        if (Plan.midBoss || Plan.floorBoss) return 0;
        int n = 0; for (int i = 0; i < Wave && i < Plan.waves.Length; i++) n += Plan.waves[i];
        return n + spawnedInWave;
    }

    void Spawn(AlleyCombat combat)
    {
        bool lessonPair = Plan.lesson == 3 && spawnedInWave + 1 < Plan.waves[Wave];
        int index = SpawnedSoFar();
        // Shares become a fixed pattern so a stage never rolls an unlucky all-charger wave.
        bool charger = Plan.chargerShare > 0 && Mathf.FloorToInt((index + 1) * Plan.chargerShare) > Mathf.FloorToInt(index * Plan.chargerShare);
        bool tray = !charger && Plan.trayShare > 0 && Mathf.FloorToInt((index + 2) * Plan.trayShare) > Mathf.FloorToInt((index + 1) * Plan.trayShare)
            && combat.enemies.FindAll(e => e.shield).Count < Plan.maxTrays;
        Fighter f = combat.AddEnemy(Entrance(combat, index), charger ? 1 : 0);
        if (tray) MakeTray(combat, f);
        Walk(combat, f); spawnedInWave++;
        if (lessonPair)
        {
            // Chain lesson: a second body right behind the first so one punch can send both flying.
            Fighter mate = combat.AddEnemy(f.pos + new Vector2(0, f.pos.y < combat.arena.center.y ? -44 : 44));
            Walk(combat, mate); spawnedInWave++;
        }
    }

    // Enemies walk in from the corridor doors (top corners and side doors) instead of appearing next to dad.
    Vector2 Entrance(AlleyCombat combat, int index)
    {
        Rect a = combat.arena;
        Vector2[] doors =
        {
            new Vector2(a.xMin + 30, a.yMin + 20), new Vector2(a.xMax - 30, a.yMin + 20),
            new Vector2(a.xMin + 10, a.center.y - 60), new Vector2(a.xMax - 10, a.center.y - 60),
            new Vector2(a.xMin + 10, a.yMax - 40), new Vector2(a.xMax - 10, a.yMax - 40),
        };
        Vector2 jitter = new Vector2(Random.Range(-18f, 18f), Random.Range(-18f, 18f));   // no two waves enter in lockstep
        for (int k = 0; k < doors.Length; k++)
        {
            Vector2 p = doors[(index + k) % doors.Length] + jitter;
            if ((p - combat.player).magnitude >= MinSpawnDistance) return Clamp(combat, p);
        }
        return Clamp(combat, doors[index % doors.Length] + jitter);
    }
    static Vector2 Clamp(AlleyCombat c, Vector2 p) { return new Vector2(Mathf.Clamp(p.x, c.arena.xMin, c.arena.xMax), Mathf.Clamp(p.y, c.arena.yMin, c.arena.yMax)); }
    static void Walk(AlleyCombat combat, Fighter f) { f.attack = Mathf.Max(f.attack, 1.1f + Random.value * .5f); }   // no swing while still walking in

    void MakeTray(AlleyCombat combat, Fighter f)
    {
        f.shield = true; f.hp = f.maxHp = Mathf.Max(1, Mathf.RoundToInt(4 * combat.enemyHpScale)); f.facing = combat.player.x >= f.pos.x ? 1 : -1;
        if (!trayIntroduced) { trayIntroduced = true; combat.events.Add(new CombatEvent("쟁반 짱 등장", f.pos, 3)); }
        TrayOnField = true;
    }

    void TickBossStage(AlleyCombat combat)
    {
        if (!keySpawned && combat.elapsed >= nextSpawn)
        {
            keySpawned = true; BossReleased = true; nextEscort = combat.elapsed + EscortInterval;
            Vector2 at = new Vector2(combat.arena.center.x, combat.arena.yMin + 45);
            if (Plan.floorBoss)
            {
                Key = combat.AddEnemy(at, 2);
                Key.hp = Key.maxHp = Mathf.Max(1, Mathf.RoundToInt(Plan.bossHp * combat.enemyHpScale));
                combat.bossSpawned = true;
                combat.events.Add(new CombatEvent("골목 대장 등장", combat.arena.center, 3));
            }
            else
            {
                Key = combat.AddEnemy(at, Plan.eliteKind == 3 ? 0 : Plan.eliteKind);
                if (Plan.eliteKind == 3) MakeTray(combat, Key);
                Key.elite = true; Key.power = elitePower;
                Key.hp = Key.maxHp = Key.hp * eliteHp;
                combat.events.Add(new CombatEvent("왕 짱 등장", combat.arena.center, 3));
            }
            return;
        }
        if (!keySpawned) return;
        if (Plan.midBoss && (Key == null || Key.hp <= 0 || !combat.enemies.Contains(Key))) { combat.state = RunState.Won; return; }
        BossGroggy = Key != null && Key.groggy > 0;
        int escorts = 0;
        foreach (Fighter e in combat.enemies) if (e != Key) escorts++;
        if (combat.elapsed < nextEscort || escorts >= MaxEscorts) return;
        nextEscort = combat.elapsed + EscortInterval;
        Walk(combat, combat.AddEnemy(Entrance(combat, escorts + 1)));
    }
}
