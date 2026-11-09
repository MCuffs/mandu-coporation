using System;
using UnityEngine;
class DadContentRunner
{
    static Vector2 Dir(float a) { a *= Mathf.Deg2Rad; return new Vector2(Mathf.Cos(a), Mathf.Sin(a)); }
    static Vector2 Target(DadFunLabModel m)
    {
        if (m.cartEnabled && !m.cartHit) return m.cart - m.sim.dad.pos;
        Vector2 result = Vector2.up; float nearest = float.MaxValue;
        foreach (var f in m.sim.foes) if (f.hp > 0)
        { Vector2 v = f.pos - m.sim.dad.pos; if (v.sqrMagnitude < nearest) { nearest = v.sqrMagnitude; result = v; } }
        return result;
    }
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
    static void Main()
    {
        for (int level = 0; level < DadFunLabModel.Titles.Length; level++)
        {
            int wins = 0, bestLeft = 99, pad = 0; string example = "none";
            for (int seed = 0; seed < 180; seed++)
            {
                var m = new DadFunLabModel(level); int shot = 0; float firedAt = 0;
                string route = "";
                for (int tick = 0; tick < 3000 && !m.sim.Over; tick++)
                {
                    if (m.CanLaunch)
                    {
                        Vector2 pull = shot == 0 ? Dir(180 + seed * 2) : Target(m);
                        if (shot > 0)
                        { float angle = Mathf.Atan2(pull.y, pull.x) * Mathf.Rad2Deg + (seed % 7 - 3) * 10; pull = Dir(angle); }
                        if (m.Launch(pull.normalized * (seed % 3 == 0 ? 100 : 150)))
                        { route += " shot" + m.sim.shots + "=" + (Mathf.Atan2(pull.y, pull.x) * Mathf.Rad2Deg).ToString("0"); shot++; firedAt = tick / 120f; }
                    }
                    if (m.CanBoost && !m.boostUsed && m.sim.dad.flying && tick / 120f - firedAt > .2f + (seed % 5) * .1f)
                        m.Boost(Target(m));
                    m.Tick(1 / 120f); m.sim.events.Clear();
                }
                if (m.sim.shots > m.ShotBudget) throw new Exception("budget exceeded");
                bestLeft = Math.Min(bestLeft, m.sim.AliveCount); pad += m.padHits;
                if (m.sim.phase == SlingshotSim.Phase.Won) { wins++; if (example == "none") example = route + " cart=" + m.cartHits + " pad=" + m.padHits; }
            }
            Console.WriteLine("CONTENT level=" + (level + 1) + " wins=" + wins + "/180 bestRemaining=" + bestLeft + " padContacts=" + pad + " example=" + example);
            Check(wins > 0, "stage " + (level + 1) + " has a reproducible winning route");
        }
        var reset = new DadFunLabModel(4);
        Check(reset.padEnabled && reset.padHits == 0 && !reset.boostUsed, "pad stage resets cleanly");
        reset.sim.dad.pos = reset.pad + new Vector2(0, 100);
        reset.Launch(new Vector2(0, -150));
        for (int i = 0; i < 30 && reset.padHits == 0; i++) reset.Tick(1 / 120f);
        Check(reset.padHits > 0 && reset.sim.dad.vel.y < 0, "mat launches upward as its painted arrow promises");
        var multi = new DadFunLabModel(2); multi.Launch(Vector2.down * 150); multi.Boost(Vector2.down);
        for (int i = 0; i < 600 && !multi.CanLaunch && !multi.sim.Over; i++) multi.Tick(1 / 120f);
        Check(multi.CanLaunch && multi.Launch(Vector2.up * 150) && !multi.boostUsed, "next shot restores one redirect");
        Check(!multi.Launch(Vector2.up * 150), "cannot fire again during flight");
        for (int i = 0; i < 1800 && !multi.sim.Over; i++) multi.Tick(1 / 120f);
        Check(!multi.Launch(new Vector2(150, 0)), "spent budget cannot launch again");
        Check(new DadFunLabModel(-5).level == 0 && new DadFunLabModel(999).level == 7, "invalid stage index is clamped");
        var sequence = new System.Collections.Generic.HashSet<int>(DadFunLabModel.PlayOrder);
        Check(sequence.Count == 8 && DadFunLabModel.PlayOrder[0] == 2, "play order includes every stage and teaches redirect first");
        var original = new DadFunLabModel(1); original.Launch(Vector2.up * 150);
        original.Tick(-1); Check(original.sim.shots == 1, "negative time does not mutate shot count");
    }
}
