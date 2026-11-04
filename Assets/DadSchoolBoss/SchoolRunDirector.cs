using UnityEngine;
using SecondFistGame;

// Explicit teaching beats instead of the legacy endless random spawner.
// Run shape (design target 150-240 s): teaching beats -> mixed waves -> boss with a trickle of escorts
// that the player throws into the boss to open it up.
public sealed class SchoolRunDirector
{
    public const int KillTarget = 45, BossHp = 28, MaxEscorts = 3, XpCurveScale = 3;
    public const float BossFloor = 80, BossDeadline = 120, EscortInterval = 4.5f;
    public int Lesson { get; private set; }
    public bool BossReleased { get; private set; }
    public bool BossGroggy { get; private set; }
    float nextWave = 5, nextEscort;
    int wave;
    public string Hint
    {
        get
        {
            if (BossReleased) return BossGroggy ? "그로기!  지금 벽으로 날리세요" : "졸개를 짱에게 날리면 그로기!";
            if (Lesson == 0) return "① 드래그로 다가가면 아빠가 자동으로 펀치!";
            if (Lesson == 1) return "② 적의 뒤에 벽이 오도록 자리를 잡으세요";
            if (Lesson == 2) return "③ 적 둘을 일렬로 세워 한 번에 날리세요";
            return "벽꿍과 연쇄로 정리!  ·  학교 짱이 오고 있습니다";
        }
    }
    public void Begin(AlleyCombat combat)
    {
        Lesson = 0; BossReleased = BossGroggy = false; wave = 0; nextWave = 5; nextEscort = 0;
        combat.killTargetOverride = KillTarget; combat.xpCurveScale = XpCurveScale; combat.nextXp = 3 * combat.xpCurveScale; combat.groggyBoss = true;
        combat.spawning = false; combat.enemies.Clear();
        Fighter first = combat.AddEnemy(new Vector2(combat.player.x + 132, combat.player.y));
        first.stun = 2.5f; first.attack = 2.5f;
    }
    public void Tick(AlleyCombat combat)
    {
        if (combat.state != RunState.Fighting) return;
        if (Lesson == 0 && combat.hits > 0) { Lesson = 1; nextWave = Mathf.Min(nextWave, combat.elapsed + 1); }
        if (Lesson == 1 && combat.wallHits > 0) { Lesson = 2; nextWave = Mathf.Min(nextWave, combat.elapsed + 1); }
        if (Lesson == 2 && combat.chainHits > 0) Lesson = 3;
        if (!BossReleased && (combat.kills >= combat.targetKills && combat.elapsed >= BossFloor || combat.elapsed >= BossDeadline))
        {
            Fighter boss = combat.AddEnemy(new Vector2(combat.arena.center.x, combat.arena.yMin + 45), 2);
            boss.hp = boss.maxHp = BossHp;
            combat.AddEnemy(new Vector2(combat.arena.xMin + 40, combat.arena.yMin + 70));
            combat.AddEnemy(new Vector2(combat.arena.xMax - 40, combat.arena.yMin + 70));
            combat.bossSpawned = BossReleased = true; nextEscort = combat.elapsed + EscortInterval;
            combat.events.Add(new CombatEvent("골목 대장 등장", combat.arena.center, 3));
            return;
        }
        if (BossReleased) { TickBossFight(combat); return; }
        if (combat.elapsed < nextWave || combat.enemies.Count >= (combat.elapsed < 24 ? 3 : combat.elapsed < 60 ? 5 : 6)) return;
        nextWave = combat.elapsed + (combat.elapsed < 24 ? 5 : combat.elapsed < 60 ? 3.8f : 3.3f); wave++;
        float side = combat.player.x < combat.arena.center.x ? 1 : -1;
        float x = Mathf.Clamp(combat.player.x + side * 115, combat.arena.xMin + 20, combat.arena.xMax - 20);
        float y = Mathf.Clamp(combat.player.y + (wave % 2 == 0 ? -65 : 50), combat.arena.yMin + 35, combat.arena.yMax - 30);
        bool charger = combat.elapsed >= 24 && wave % 3 == 0 || combat.elapsed >= 60 && wave % 2 == 0;
        combat.AddEnemy(new Vector2(x, y), charger ? 1 : 0);
        if ((Lesson >= 2 || combat.elapsed >= 15) && combat.enemies.Count < 6)
            combat.AddEnemy(new Vector2(Mathf.Clamp(x + side * 44, combat.arena.xMin + 12, combat.arena.xMax - 12), y));
    }
    // Escorts are ammunition: the boss only becomes launchable after another body hits it.
    void TickBossFight(AlleyCombat combat)
    {
        Fighter boss = combat.enemies.Find(e => e.boss);
        BossGroggy = boss != null && boss.groggy > 0;
        int escorts = 0;
        foreach (Fighter e in combat.enemies) if (!e.boss) escorts++;
        if (combat.elapsed < nextEscort || escorts >= MaxEscorts) return;
        nextEscort = combat.elapsed + EscortInterval;
        float side = combat.player.x < combat.arena.center.x ? 1 : -1;
        float x = Mathf.Clamp(combat.player.x + side * 120, combat.arena.xMin + 20, combat.arena.xMax - 20);
        combat.AddEnemy(new Vector2(x, Mathf.Clamp(combat.player.y + (escorts % 2 == 0 ? -55 : 55), combat.arena.yMin + 35, combat.arena.yMax - 30)));
    }
}
