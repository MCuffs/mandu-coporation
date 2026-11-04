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
        int before = boss.hp; s.dad.pos = new Vector2(Arena.center.x, 700); s.Launch(new Vector2(0, -150));
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
        var path = SlingshotSim.Predict(new Vector2(Arena.center.x, 700), new Vector2(150, -40), Arena);
        Expect(path.Count >= 3 && Mathf.Abs(path[1].x - (Arena.xMax - SlingshotSim.DadRadius)) < 1, "the aiming guide bends at the first wall", report);
        var stages = SlingshotSim.PrototypeStages(Arena);
        Expect(stages.Length == 4 && stages[3].bossEscorts && stages.All(st => st.spawns.Count > 0), "four prototype stages, the last one a boss with escorts", report);
        return report.ToString();
    }

    // ---- pacing bot -------------------------------------------------------------------------------------------------
    // Aims at the nearest dad (or a groggy boss) with some error and takes a human-ish moment to aim, during which the
    // walkers run at the aiming slow-down. Not a player model; it shows whether stages are clearable and how long they run.
    public static string RunBot()
    {
        var report = new StringBuilder("Prototype B pacing bot (60 runs per stage per profile)\n");
        // aim error in degrees, seconds spent aiming (min, max), weakest pull share
        var profiles = new[] { new[] { 5f, .8f, 1.4f, .8f }, new[] { 15f, .8f, 1.4f, .8f }, new[] { 20f, 1.5f, 2.5f, .5f } };
        string[] names = { "careful: 5 deg, 0.8-1.4 s aim", "sloppy: 15 deg, 0.8-1.4 s aim", "beginner: 20 deg, 1.5-2.5 s aim, 50-100% pull" };
        for (int pi = 0; pi < profiles.Length; pi++)
        {
            float error = profiles[pi][0], aimMin = profiles[pi][1], aimMax = profiles[pi][2], weakest = profiles[pi][3];
            report.AppendLine("\n[" + names[pi] + "]");
            for (int si = 0; si < 4; si++)
            {
                var wins = new List<SlingshotSim>(); int lost = 0, timeout = 0, stars3 = 0;
                for (int run = 0; run < 60; run++)
                {
                    var stages = SlingshotSim.PrototypeStages(Arena); var s = new SlingshotSim(stages[si], Arena, 100 + run);
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
