#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// Prototype B rule checks and pacing bot. Pure simulation, so the editor menu (DadSlingshotSetup) and an out-of-editor
// harness can both run it; not part of player builds.
public static class SlingshotChecks
{
    // ---- rule checks ---------------------------------------------------------------------------------------
    static readonly Rect Arena = DadSlingshotPrototype.Arena;
    static SlingshotSim Sim()
    {
        var stage = new SlingshotSim.Stage { label = "T", title = "check", lesson = "", par = 3 };
        return new SlingshotSim(stage, Arena, 1);
    }
    static void Run(SlingshotSim s, float seconds) { for (float t = 0; t < seconds; t += 1 / 120f) s.Tick(1 / 120f); }
    static void Expect(bool ok, string message, StringBuilder report) { if (!ok) throw new Exception("SLING_CHECK_FAILED: " + message); report.AppendLine("PASS " + message); }
    static SlingshotSim.Foe Parked(SlingshotSim s, SlingshotSim.Kind kind, float x, float y) { var f = s.Add(kind, x, y); f.cooldown = 99; return f; }

    public static string RunChecks()
    {
        var report = new StringBuilder("Prototype B (slingshot dad) rule checks\n");
        // Compare predictions with actual empty-arena flight, including growth and corner bounces.
        foreach (float size in new[] { .72f, 1f })
        foreach (float bonus in new[] { 1f, 1.35f })
        foreach (float retention in new[] { .88f, .979f })
        foreach (bool rage in new[] { false, true })
        foreach (Vector2 pull in new[] { new Vector2(24, 0), new Vector2(150, -40), new Vector2(-100, -150) })
        {
            var stage = new SlingshotSim.Stage();
            stage.spawns.Add(new SlingshotSim.Spawn { at = 100, kind = SlingshotSim.Kind.Gum });
            var flight = new SlingshotSim(stage, Arena, 1, size);
            flight.launchBonus = bonus; flight.bounceRetention = retention; flight.ragePending = rage;
            var predicted = SlingshotSim.Predict(flight.dad.pos, pull, Arena, flight.DadRadius, null,
                bonus * (rage ? 1.15f : 1), retention, float.PositiveInfinity);
            Expect(flight.Launch(pull), "prediction fixture launches", report);
            for (int step = 0; step < 400 && flight.dad.flying; step++) flight.Tick(1 / 120f);
            Expect(Vector2.Distance(predicted[predicted.Count - 1], flight.dad.pos) < .1f,
                "predicted landing matches flight size=" + size + " boost=" + bonus + " retention=" + retention + " rage=" + rage + " pull=" + pull, report);
        }
        var preview = Sim(); preview.launchBonus = 1.35f; preview.ragePending = true;
        var buffer = new System.Collections.Generic.List<Vector2>();
        var guide = preview.PredictShot(new Vector2(150, -40), buffer);
        Expect(object.ReferenceEquals(guide, buffer) && preview.ragePending && preview.shots == 0,
            "aim prediction reuses buffer without consuming rage or launching", report);
        Expect(preview.PredictShot(new Vector2(0, 10), buffer).Count == 1,
            "dead-zone preview has no flight segment", report);
        var s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30);   // keeps each check from ending in a win
        Expect(!s.Launch(new Vector2(0, -10)) && s.phase == SlingshotSim.Phase.Ready, "a pull inside the dead zone does not launch", report);
        Expect(s.Launch(new Vector2(0, -150)) && s.dad.flying && Mathf.Abs(s.dad.vel.y + SlingshotSim.LaunchMax) < 1 && s.shots == 1, "letting go flies dad along the pull at full speed for a full pull", report);
        var half = SlingshotSim.LaunchVelocity(new Vector2(75, 0)).magnitude;
        Expect(half > SlingshotSim.LaunchMin && half < SlingshotSim.LaunchMax, "a half pull flies slower than a full one", report);

        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30);
        s.dad.pos = new Vector2(Arena.center.x, 600); s.Launch(new Vector2(150, 0)); Run(s, .35f);
        Expect(s.dad.vel.x < 0 && s.dad.flying, "dad bounces off the side lockers and keeps flying", report);
        Run(s, 3);
        Expect(!s.dad.flying && s.phase == SlingshotSim.Phase.Ready, "dad comes to rest on his own and the next shot is ready", report);

        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30);
        var gum = Parked(s, SlingshotSim.Kind.Gum, Arena.center.x, 560); s.dad.pos = new Vector2(Arena.center.x, 700);
        s.Launch(new Vector2(0, -150));
        for (int i = 0; i < 120 && !gum.Flying; i++) s.Tick(1 / 120f);
        Expect(gum.Flying && gum.vel.y < 0 && gum.hp == gum.maxHp - 1 && s.dad.flying && s.dad.vel.y < 0, "flying dad knocks a dad away and ploughs on", report);

        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30);
        gum = Parked(s, SlingshotSim.Kind.Gum, Arena.xMax - 70, 520); gum.vel = new Vector2(900, 0); Run(s, .25f);
        Expect(s.wallSlams == 1 && s.kills == 1, "a knocked dad slams the lockers for two damage (a gum dad goes down)", report);
        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30);
        gum = Parked(s, SlingshotSim.Kind.Gum, Arena.xMax - 70, 520); gum.vel = new Vector2(900, 0);
        bool dented = false; for (int i = 0; i < 60; i++) { s.Tick(1 / 120f); foreach (var e in s.events) if (e.dentSide == 1) dented = true; s.events.Clear(); }
        Expect(dented, "a side-wall slam reports which locker side to dent", report);

        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30);
        var a = Parked(s, SlingshotSim.Kind.Gum, 160, 500); var b = Parked(s, SlingshotSim.Kind.Gum, 230, 500); a.vel = new Vector2(800, 0);
        for (int i = 0; i < 60 && !b.Flying; i++) s.Tick(1 / 120f);
        Expect(b.Flying && b.vel.x > 200 && b.hp == b.maxHp - 1 && s.bestChain >= 1, "a knocked dad bowls over the next one (chain)", report);

        s = Sim(); var tray = Parked(s, SlingshotSim.Kind.Tray, Arena.center.x, 560); s.dad.pos = new Vector2(Arena.center.x, 700); Run(s, .05f);
        s.Launch(new Vector2(0, -150)); for (int i = 0; i < 120 && tray.trayHits == 0; i++) s.Tick(1 / 120f);
        Expect(tray.hp == tray.maxHp && tray.trayHits == 1 && s.dad.vel.y > 0, "a straight shot into the tray bounces dad back without damage", report);
        s = Sim(); tray = Parked(s, SlingshotSim.Kind.Tray, Arena.center.x, 560); s.dad.pos = new Vector2(Arena.xMin + 40, 560);
        s.Launch(new Vector2(150, 0)); tray.face = Vector2.down;   // braced toward where dad stood before; he comes round the side
        for (int i = 0; i < 60 && !tray.Flying; i++) s.Tick(1 / 120f);
        Expect(tray.trayBroken && tray.Flying, "coming in from the side gets past the tray and knocks it out of his hands", report);

        s = Sim(); var boss = Parked(s, SlingshotSim.Kind.Boss, Arena.center.x, 470); s.dad.pos = new Vector2(Arena.center.x, 700);
        s.Launch(new Vector2(0, -150)); for (int i = 0; i < 120 && boss.hp == boss.maxHp; i++) s.Tick(1 / 120f);
        Expect(!boss.Flying && boss.groggy <= 0 && boss.hp == boss.maxHp - 1 && s.dad.vel.y > 0, "a fresh boss bounces dad off for a chip of damage", report);
        Run(s, 3); var thrown = Parked(s, SlingshotSim.Kind.Gum, Arena.center.x, 600); thrown.vel = new Vector2(0, -800);
        for (int i = 0; i < 60 && boss.groggy <= 0; i++) s.Tick(1 / 120f);
        Expect(boss.groggy > 0, "a dad thrown into the boss makes him groggy", report);
        thrown.hp = 0; s.Tick(1 / 120f);   // the thrown dad is out of the way; this checks dad's own hit on the opened boss
        int before = boss.hp; s.dad.pos = boss.pos + new Vector2(0, 240); s.Launch(new Vector2(0, -150));
        bool bossFlew = false; for (int i = 0; i < 240 && boss.hp > before - 4; i++) { s.Tick(1 / 120f); bossFlew |= boss.Flying; }
        Expect(bossFlew && boss.hp <= before - 4, "while groggy, dad sends the boss into the wall for a big hit", report);

        s = Sim(); gum = s.Add(SlingshotSim.Kind.Gum, Arena.center.x, 650); gum.cooldown = 0; s.dad.pos = new Vector2(Arena.center.x, 700); Run(s, .8f);
        Expect(s.dad.health < s.dad.maxHealth, "a dad who reaches grounded dad swings and hurts him", report);
        s = Sim(); gum = Parked(s, SlingshotSim.Kind.Gum, Arena.xMax - 50, 520); gum.vel = new Vector2(900, 0); Run(s, .5f);
        Expect(s.phase == SlingshotSim.Phase.Won && s.Stars == 3, "clearing every dad wins, three stars with full health and few shots", report);
        s.shots = 5;
        Expect(s.Stars == 2, "too many shots drops the third star", report);
        s = Sim(); gum = s.Add(SlingshotSim.Kind.Gum, Arena.center.x, 650); gum.cooldown = 0; s.dad.health = 1; s.dad.pos = new Vector2(Arena.center.x, 700); Run(s, .8f);
        Expect(s.phase == SlingshotSim.Phase.Lost, "losing all health loses", report);
        var path = SlingshotSim.Predict(new Vector2(Arena.center.x, 700), new Vector2(150, -40), Arena, SlingshotSim.DadRadiusBase);
        Expect(path.Count >= 3 && Mathf.Abs(path[1].x - (Arena.xMax - SlingshotSim.DadRadiusBase)) < 1, "the aiming guide bends at the first wall", report);
        var small = new SlingshotSim(new SlingshotSim.Stage { label = "T", title = "check", lesson = "", par = 3 }, Arena, 1, DadSlingshotPrototype.DefaultScale);
        var smallGum = small.Add(SlingshotSim.Kind.Gum, Arena.center.x, 400);
        Expect(Mathf.Abs(small.DadRadius - 26 * DadSlingshotPrototype.DefaultScale) < .01f && Mathf.Abs(smallGum.Radius - 25 * DadSlingshotPrototype.DefaultScale) < .01f && small.dad.pos.y > Arena.yMax - 60,
            "wide view: bodies shrink with the art and dad starts at the bottom of the corridor", report);
        small.dad.pos = new Vector2(Arena.center.x, 600); small.Launch(new Vector2(-150, 0));
        for (int i = 0; i < 120 && small.dad.vel.x < 0; i++) small.Tick(1 / 120f);
        Expect(small.dad.pos.x - small.DadRadius <= Arena.xMin + 1, "dad touches the painted floor edge before bouncing", report);
        // ---- fun pass: bank shots, rage shot, dodgeball dad, heavy big-dad ----
        float[] launched = new float[2]; int[] left = new int[2];
        for (int k = 0; k < 2; k++)
        {
            var bs = Sim(); Parked(bs, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30);
            var g = Parked(bs, SlingshotSim.Kind.Gum, Arena.center.x, 560); bs.dad.pos = new Vector2(Arena.center.x, 700);
            bs.Launch(new Vector2(0, -150)); bs.dad.bounces = k * 2;   // as if he had come off two lockers
            for (int i = 0; i < 120 && !g.Flying; i++) bs.Tick(1 / 120f);
            launched[k] = g.vel.magnitude; left[k] = g.hp;
        }
        Expect(launched[1] > launched[0] * 1.25f && left[1] < left[0], "a bank shot off two lockers hits harder and for more damage", report);
        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30);
        s.dad.pos = new Vector2(Arena.center.x, 700); s.Launch(new Vector2(150, -20)); s.shotChain = 5; Run(s, 3);
        Expect(s.ragePending && s.phase == SlingshotSim.Phase.Ready, "a five-chain shot earns the next shot as a rage shot", report);
        s.Launch(new Vector2(0, -150));
        Expect(s.dad.rage && !s.ragePending && s.dad.vel.magnitude > SlingshotSim.LaunchMax, "the rage shot flies faster and is used up", report);
        s = Sim(); tray = Parked(s, SlingshotSim.Kind.Tray, Arena.center.x, 560); s.dad.pos = new Vector2(Arena.center.x, 700); Run(s, .05f);
        s.ragePending = true; s.Launch(new Vector2(0, -150)); for (int i = 0; i < 120 && !tray.Flying; i++) s.Tick(1 / 120f);
        Expect(tray.trayBroken && tray.Flying, "a rage shot goes straight through a tray's front", report);
        s = Sim(); var dodge = s.Add(SlingshotSim.Kind.Dodge, Arena.center.x, 500); dodge.cooldown = 0; s.dad.pos = new Vector2(Arena.center.x, 760); Run(s, 2.5f);
        Expect(s.dad.health < s.dad.maxHealth, "the dodgeball dad keeps his distance and hits a dad who stands still", report);
        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30);
        var ball = new SlingshotSim.Ball { pos = new Vector2(Arena.center.x, 600), vel = Vector2.zero }; s.balls.Add(ball);
        s.dad.pos = new Vector2(Arena.center.x, 760); s.Launch(new Vector2(0, -150)); for (int i = 0; i < 60 && !ball.returned; i++) s.Tick(1 / 120f);
        Expect(ball.returned && ball.vel.y < -600, "a flying dad bats the ball back the way he is going", report);
        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30); gum = Parked(s, SlingshotSim.Kind.Gum, Arena.center.x, 420);
        s.balls.Add(new SlingshotSim.Ball { pos = new Vector2(Arena.center.x, 560), vel = new Vector2(0, -760), returned = true });
        for (int i = 0; i < 60 && !gum.Flying; i++) s.Tick(1 / 120f);
        Expect(gum.Flying && gum.hp == gum.maxHp - 1, "a returned ball knocks a dad over", report);
        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30); var big = s.Add(SlingshotSim.Kind.Gum, Arena.center.x, 520, true); big.cooldown = 99;
        s.dad.pos = new Vector2(Arena.center.x, 760); s.Launch(new Vector2(0, -150)); for (int i = 0; i < 120 && big.heavyHits == 0; i++) s.Tick(1 / 120f);
        bool firstHitHeld = !big.Flying && big.heavyHits == 1;
        Run(s, 2.4f); s.dad.pos = big.pos + new Vector2(0, 200); s.Launch(new Vector2(0, -150)); for (int i = 0; i < 120 && !big.Flying; i++) s.Tick(1 / 120f);
        Expect(firstHitHeld && big.Flying, "a big-dad only staggers on the first hit and flies on a second hit within three seconds", report);
        // ---- main game: 3 floors x 20 generated from SlingshotSim.ForMain, growth modifiers ----
        bool allFilled = true, bigDads = true, bosses = true;
        for (int fl = 1; fl <= 3; fl++) for (int n = 1; n <= 20; n++)
        {
            var ms = SlingshotSim.ForMain(fl, n, Arena);
            if (ms.spawns.Count == 0 || ms.par <= 0 || ms.floor != fl) allFilled = false;
            if (n == 10 && !ms.spawns.Exists(sp => sp.elite)) bigDads = false;
            if (n == 20 && !(ms.bossEscorts && ms.spawns.Exists(sp => sp.kind == SlingshotSim.Kind.Boss))) bosses = false;
        }
        Expect(allFilled && bigDads && bosses, "main game: all 60 stages have dads and a par, each floor's 10th has a big-dad and its 20th the boss with escorts", report);
        var m4 = SlingshotSim.ForMain(1, 4, Arena); var m7 = SlingshotSim.ForMain(1, 7, Arena); var m9 = SlingshotSim.ForMain(1, 9, Arena);
        Expect(SlingshotSim.ForMain(1, 1, Arena).spawns.TrueForAll(sp => sp.kind == SlingshotSim.Kind.Gum) && m4.spawns.Exists(sp => sp.kind == SlingshotSim.Kind.Chair)
            && m7.spawns.Exists(sp => sp.kind == SlingshotSim.Kind.Tray) && m9.spawns.Exists(sp => sp.kind == SlingshotSim.Kind.Dodge), "main game 1-1..1-10 follow the first-ten order: pull, chair at 1-4, tray at 1-7, dodgeball at 1-9", report);
        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30); s.launchBonus = 1.2f; s.SetHealth(140); s.Launch(new Vector2(0, -150));
        Expect(s.dad.vel.magnitude > SlingshotSim.LaunchMax * 1.19f && s.dad.maxHealth == 140 && s.dad.health == 140, "growth: 도움닫기 speeds the launch and 체력 sets dad's health", report);
        s = Sim(); Parked(s, SlingshotSim.Kind.Tray, Arena.xMin + 40, Arena.yMin + 30); s.wallBonus = 1; var t3 = Parked(s, SlingshotSim.Kind.Tray, Arena.xMax - 70, 520); t3.vel = new Vector2(900, 0); Run(s, .25f);
        Expect(t3.hp <= 0, "growth: 멀리 날리기 adds wall damage (a tray dad goes down in one slam)", report);
        var stages = SlingshotSim.PrototypeStages(Arena);
        Expect(stages.Length == 6 && stages[5].bossEscorts && stages.All(st => st.spawns.Count > 0), "six prototype stages, the last one a boss with escorts", report);
        return report.ToString();
    }

    // ---- main game pacing: every stage of the school, a few runs each ----------------------------------------------
    // wallBonus 1 = the starting loadout (벽꿍형 gear, no upgrades).
    public static string RunMainBot(int runs = 20, int wallBonus = 0)
    {
        var report = new StringBuilder("Main game pacing bot (beginner: 20 deg, 1.5-2.5 s aim, 50-100% pull; " + runs + " runs per stage, " + (wallBonus > 0 ? "starting gear: wall damage +" + wallBonus : "no growth") + ")\n");
        for (int fl = 1; fl <= 3; fl++)
        {
            report.AppendLine();
            for (int n = 1; n <= 20; n++)
            {
                int won = 0; var times = new List<float>(); var health = new List<float>();
                for (int run = 0; run < runs; run++)
                {
                    var s = new SlingshotSim(SlingshotSim.ForMain(fl, n, Arena), Arena, 500 + run, DadSlingshotPrototype.DefaultScale) { wallBonus = wallBonus };
                    Play(s, new System.Random(run * 13 + n), 20, 1.5f, 2.5f, .5f);
                    if (s.phase == SlingshotSim.Phase.Won) { won++; times.Add(s.elapsed); health.Add(s.dad.health); }
                }
                times.Sort(); health.Sort();
                report.AppendLine(fl + "-" + n + ": win " + won + "/" + runs + (won > 0 ? " | " + times[times.Count / 2].ToString("0") + "s, health " + health[health.Count / 2].ToString("0") : ""));
            }
        }
        return report.ToString();
    }
    // Repeatable policy comparison, not a human difficulty or retention estimate. Same seeds and starting wall loadout.
    public static string RunDifficultyBot(bool rematches, int runs=12)
    {
        var report=new StringBuilder("Difficulty policy comparison: no upgrades or lunchbox passives, wall damage +1, "+runs+" seeds per row\n");
        foreach(int n in new[]{1,5,8,10,15,20})
        for(int rank=rematches?1:0;rank<=(rematches?3:0);rank++)
        for(int policy=0;policy<3;policy++)
        {
            int wins=0;float hp=0,stars=0;
            for(int seed=0;seed<runs;seed++)
            {
                var st=rank==0?SlingshotSim.ForMain(1,n,Arena):SlingshotSim.Rematch(1,n,rank,Arena);
                var sim=new SlingshotSim(st,Arena,seed,DadSlingshotPrototype.DefaultScale){wallBonus=1};
                Play(sim,new System.Random(seed*137),policy==0?60:policy==1?20:5,policy==0?.05f:policy==1?1.5f:.6f,policy==0?.1f:policy==1?2.5f:1,policy==0?.3f:.5f);
                if(sim.phase==SlingshotSim.Phase.Won){wins++;hp+=sim.dad.health;stars+=sim.Stars;}
            }
            report.AppendLine("1-"+n+" rank "+rank+" "+(policy==0?"rapid noisy":policy==1?"novice":"precise")+": "+wins+"/"+runs+(wins>0?", HP "+(hp/wins).ToString("F0")+", stars "+(stars/wins).ToString("F1"):""));
        }
        return report.ToString();
    }
    // One bot game: aim at the nearest dad (a groggy boss first) with error, taking a moment to aim at the aiming slow-down.
    static void Play(SlingshotSim s, System.Random rng, float error, float aimMin, float aimMax, float weakest)
    {
        float t = 0;
        while (!s.Over && t < 180)
        {
            if (s.phase == SlingshotSim.Phase.Ready && s.AliveCount > 0)
            {
                float aimTime = aimMin + (float)rng.NextDouble() * (aimMax - aimMin);
                for (float a = 0; a < aimTime && !s.Over; a += 1 / 60f) s.Tick(1 / 60f * DadSlingshotPrototype.AimSlow);
                if (s.Over) break;
                SlingshotSim.Foe target = null; float best = float.MaxValue;
                foreach (var f in s.foes) { if (f.hp <= 0) continue; float d = (f.pos - s.dad.pos).sqrMagnitude * (f.Boss && f.groggy <= 0 ? 4 : 1) * (f.Boss && f.groggy > 0 ? .1f : 1); if (d < best) { best = d; target = f; } }
                if (target != null)
                {
                    Vector2 dir = (target.pos - s.dad.pos).normalized; float noise = (float)(rng.NextDouble() * 2 - 1) * error * Mathf.Deg2Rad;
                    dir = new Vector2(dir.x * Mathf.Cos(noise) - dir.y * Mathf.Sin(noise), dir.x * Mathf.Sin(noise) + dir.y * Mathf.Cos(noise));
                    s.Launch(dir * SlingshotSim.PullMax * (weakest + (1 - weakest) * (float)rng.NextDouble()));
                }
            }
            s.Tick(1 / 60f); s.events.Clear(); t += 1 / 60f;
        }
    }

    // ---- pacing bot -------------------------------------------------------------------------------------------------
    // Aims at the nearest dad (or a groggy boss) with some error and takes a human-ish moment to aim, during which the
    // walkers run at the aiming slow-down. Not a player model; it shows whether stages are clearable and how long they run.
    public static string RunBot()
    {
        var report = new StringBuilder("Prototype B pacing bot (60 runs per stage per profile; wide view, bodies at " + Mathf.RoundToInt(DadSlingshotPrototype.DefaultScale * 100) + "%)\n");
        // aim error in degrees, seconds spent aiming (min, max), weakest pull share
        var profiles = new[] { new[] { 5f, .8f, 1.4f, .8f }, new[] { 15f, .8f, 1.4f, .8f }, new[] { 20f, 1.5f, 2.5f, .5f } };
        string[] names = { "careful: 5 deg, 0.8-1.4 s aim", "sloppy: 15 deg, 0.8-1.4 s aim", "beginner: 20 deg, 1.5-2.5 s aim, 50-100% pull" };
        for (int pi = 0; pi < profiles.Length; pi++)
        {
            float error = profiles[pi][0], aimMin = profiles[pi][1], aimMax = profiles[pi][2], weakest = profiles[pi][3];
            report.AppendLine("\n[" + names[pi] + "]");
            for (int si = 0; si < SlingshotSim.PrototypeStages(Arena).Length; si++)
            {
                var wins = new List<SlingshotSim>(); int lost = 0, timeout = 0, stars3 = 0;
                for (int run = 0; run < 60; run++)
                {
                    var stages = SlingshotSim.PrototypeStages(Arena); var s = new SlingshotSim(stages[si], Arena, 100 + run, DadSlingshotPrototype.DefaultScale);
                    var rng = new System.Random(run * 31 + si * 7); float t = 0;
                    while (!s.Over && t < 180)
                    {
                        if (s.phase == SlingshotSim.Phase.Ready && s.AliveCount > 0)
                        {
                            float aimTime = aimMin + (float)rng.NextDouble() * (aimMax - aimMin);
                            for (float a = 0; a < aimTime && !s.Over; a += 1 / 60f) s.Tick(1 / 60f * DadSlingshotPrototype.AimSlow);
                            if (s.Over) break;
                            SlingshotSim.Foe target = null; float best = float.MaxValue;
                            foreach (var f in s.foes) { if (f.hp <= 0) continue; float d = (f.pos - s.dad.pos).sqrMagnitude * (f.Boss && f.groggy <= 0 ? 4 : 1) * (f.Boss && f.groggy > 0 ? .1f : 1); if (d < best) { best = d; target = f; } }
                            if (target != null)
                            {
                                Vector2 dir = (target.pos - s.dad.pos).normalized; float noise = (float)(rng.NextDouble() * 2 - 1) * error * Mathf.Deg2Rad;
                                dir = new Vector2(dir.x * Mathf.Cos(noise) - dir.y * Mathf.Sin(noise), dir.x * Mathf.Sin(noise) + dir.y * Mathf.Cos(noise));
                                s.Launch(dir * SlingshotSim.PullMax * (weakest + (1 - weakest) * (float)rng.NextDouble()));
                            }
                        }
                        s.Tick(1 / 60f); s.events.Clear(); t += 1 / 60f;
                    }
                    if (s.phase == SlingshotSim.Phase.Won) { wins.Add(s); if (s.Stars == 3) stars3++; } else if (s.phase == SlingshotSim.Phase.Lost) lost++; else timeout++;
                }
                Func<Func<SlingshotSim, float>, string> median = sel => { if (wins.Count == 0) return "-"; var v = wins.Select(sel).OrderBy(x => x).ToList(); return v[v.Count / 2].ToString("0.#"); };
                var st = SlingshotSim.PrototypeStages(Arena)[si];
                report.AppendLine(st.label + " " + st.title + ": win " + wins.Count + "/60, lost " + lost + ", timeout " + timeout
                    + " | median shots " + median(x => x.shots) + " (par " + st.par + "), time " + median(x => x.elapsed) + "s, health " + median(x => x.dad.health)
                    + ", best chain " + median(x => x.bestChain) + " | 3 stars " + stars3);
            }
        }
        return report.ToString();
    }
}
#endif
