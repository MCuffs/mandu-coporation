using System;
using System.Text;
using UnityEngine;
class DadFunLabRunner
{
    static Vector2 Direction(float degrees) { float r = degrees * Mathf.Deg2Rad; return new Vector2(Mathf.Cos(r), Mathf.Sin(r)); }
    static DadFunLabModel Play(int level, float angle, float power, bool cart, float boostAt = -1, float boostAngle = 0)
    {
        var m = new DadFunLabModel(level, cart); m.Launch(Direction(angle) * power);
        bool tried = false;
        for (int i = 0; i < 1440 && !m.sim.Over; i++)
        {
            if (!tried && boostAt >= 0 && i / 120f >= boostAt) { m.Boost(Direction(boostAngle)); tried = true; }
            m.Tick(1 / 120f); m.sim.events.Clear();
        }
        return m;
    }
    static bool Won(DadFunLabModel m) { return m.sim.phase == SlingshotSim.Phase.Won; }
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
    static void Main()
    {
        Console.Write(SlingshotChecks.RunChecks()); Console.Write(DadDifficultyChecks.Run());
        for (int level = 0; level < 3; level++)
        {
            int wins = 0, total = 0; string first = "none"; int best = 99;
            for (int angle = 0; angle < 360; angle += 2)
            foreach (float power in new[] { 60f, 100f, 150f })
            {
                var m = Play(level, angle, power, true); total++;
                best = Math.Min(best, m.sim.AliveCount);
                if (Won(m)) { wins++; if (first == "none") first = "angle=" + angle + " power=" + power + " cartHits=" + m.cartHits; }
            }
            Console.WriteLine("PLAY level=" + level + " noBoost wins=" + wins + "/" + total + " bestRemaining=" + best + " first=" + first);
        }
        int exclusiveCart = 0; string cartExample = "none";
        for (int angle = 220; angle <= 320; angle += 2)
        {
            var yes = Play(1, angle, 150, true); var no = Play(1, angle, 150, false);
            if (Won(yes) && !Won(no)) { exclusiveCart++; cartExample = "angle=" + angle + " cartHits=" + yes.cartHits; }
        }
        Console.WriteLine("PLAY cart-exclusive solutions=" + exclusiveCart + " example=" + cartExample);
        int exclusiveBoost = 0; string boostExample = "none";
        for (int angle = 200; angle < 340; angle += 10)
        foreach (float at in new[] { .2f, .4f, .6f, .8f, 1f })
        for (int turn = 0; turn < 360; turn += 15)
        {
            var m = Play(2, angle, 150, true, at, turn);
            if (Won(m) && !Won(Play(2, angle, 150, true))) { exclusiveBoost++; if (boostExample == "none") boostExample = "launch=" + angle + " at=" + at + " turn=" + turn; }
        }
        Console.WriteLine("PLAY boost-exclusive solutions=" + exclusiveBoost + " first=" + boostExample);
        int aimedBoostWins = 0, aimedBoostTrials = 0;
        for (int angle = 230; angle <= 260; angle += 2)
        foreach (float at in new[] { .2f, .3f, .4f, .5f, .6f, .8f })
        {
            var m = new DadFunLabModel(2); m.Launch(Direction(angle) * 150); bool tried = false;
            for (int i = 0; i < 1440 && !m.sim.Over; i++)
            {
                if (!tried && i / 120f >= at)
                {
                    SlingshotSim.Foe target = null; float distance = float.MaxValue;
                    foreach (var foe in m.sim.foes) if (foe.hp > 0)
                    { float d = (foe.pos - m.sim.dad.pos).sqrMagnitude; if (d < distance) { target = foe; distance = d; } }
                    if (target != null) m.Boost(target.pos - m.sim.dad.pos); tried = true;
                }
                m.Tick(1 / 120f); m.sim.events.Clear();
            }
            aimedBoostTrials++; if (Won(m)) aimedBoostWins++;
        }
        Console.WriteLine("PLAY tap-nearest-target boost wins=" + aimedBoostWins + "/" + aimedBoostTrials);
        Check(exclusiveCart > 0, "cart changes a failing shot into a win");
        Check(exclusiveBoost > 0, "manual redirect changes a failing shot into a win");
        var test = new DadFunLabModel(2);
        Check(!test.Boost(Vector2.up), "cannot boost before flight");
        Check(!test.Launch(Vector2.zero), "short pull does not consume shot");
        Check(test.Launch(Vector2.up * 150) && !test.Launch(Vector2.up * 150), "one launch only");
        Check(test.Boost(Vector2.right) && !test.Boost(Vector2.left), "redirect is usable once");
        var reset = new DadFunLabModel(2); Check(!reset.boostUsed && reset.sim.shots == 0, "retry resets all experiment state");
    }
}
