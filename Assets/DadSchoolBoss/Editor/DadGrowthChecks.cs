using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Regression checks use a disposable save, never the player's save or preferences.
public static class DadGrowthChecks
{
    static StringBuilder report;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); report.AppendLine("PASS " + name); }
    public static void Batch()
    {
        report = new StringBuilder("Slingshot growth and replay regression checks\n");
        string path = Path.Combine(Path.GetTempPath(), "dad-growth-" + Guid.NewGuid() + ".json");
        int exit = 0;
        try
        {
            var p = new DadProgress(DadEconomy.Load(), path); p.Save.coins = 1000000;
            p.Save.lunchboxes = 2;
            Check(p.OpenLunchbox() && p.Save.pendingSlot == 1 && p.Save.pendingRarity == p.Eco.gearFirstRarity, "first teaching item is guaranteed rare hand gear");
            p.EquipPending();
            Check(p.OpenLunchbox() && p.Save.pendingSlot == 0, "second teaching item is shoes"); p.EquipPending();
            foreach (int i in new[] { 0, 2, 3 })
            {
                p.Save.upgrades[i] = i == 3 ? 25 : 20;
                double coins = p.Save.coins, cost = p.NextUpgradeCost; int levels = p.TotalLevels;
                Check(!p.TryUpgrade(i) && p.Save.coins == coins && p.TotalLevels == levels && p.NextUpgradeCost == cost, "cap blocks purchase without changing cost: " + i);
                p.Save.upgrades[i] = 0;
            }
            Check(!p.TryUpgrade(-1) && !p.TryUpgrade(4), "invalid stat rejected");
            var stage = SlingshotSim.ForMain(1, 3, DadSlingshotPrototype.Arena);
            var baseline = new SlingshotSim(stage, DadSlingshotPrototype.Arena, 1, .72f);
            DadSchoolPrototype.ApplyGrowth(baseline, new int[4], 0, 1);
            for (int i = 0; i < 6; i++) { p.Save.gearRarity[i] = 1; p.Save.gearValue[i] = .04f; }
            var equipped = new SlingshotSim(stage, DadSlingshotPrototype.Arena, 1, .72f);
            DadSchoolPrototype.ApplyGrowth(equipped, new int[4], 0, 1); DadSchoolPrototype.ApplyLunchboxGear(equipped, p);
            Check(equipped.bounceRetention > baseline.bounceRetention && equipped.bounceRetention < 1, "shoes retain more wall speed without acceleration");
            Check(equipped.knockBonus > baseline.knockBonus, "hands transfer more impact");
            Check(equipped.dad.maxHealth > baseline.dad.maxHealth && equipped.aimSlow < baseline.aimSlow && equipped.hurtGrace > baseline.hurtGrace && equipped.throwBonus > baseline.throwBonus, "all remaining slots affect their stated property");
            var physicsA = new SlingshotSim(stage, DadSlingshotPrototype.Arena, 1, .72f);
            var physicsB = new SlingshotSim(stage, DadSlingshotPrototype.Arena, 1, .72f);
            DadSchoolPrototype.ApplyLunchboxGear(physicsB, p);
            foreach (var sim in new[] { physicsA, physicsB }) { sim.dad.pos = new Vector2(sim.arena.xMax - sim.DadRadius - 1, 700); sim.Launch(Vector2.right * 150); sim.Tick(.02f); }
            Check(physicsB.dad.vel.magnitude > physicsA.dad.vel.magnitude, "same wall shot preserves more speed with shoes");
            for (int i=0;i<6;i++) Check(DadProgress.GearBonus(i,.08f)>DadProgress.GearBonus(i,.04f), "bulk-open order remains monotonic: " + i);
            p.Save.pendingSlot=1;p.Save.pendingValue=.08f;p.Save.pendingRarity=2;Check(p.PendingIsUpgrade,"pending compares same-slot effect");
            p.Save.bestStage=8;p.Save.currentStage=8;p.RecordShots(2,5,true);p.RecordShots(2,7,true);p.RecordShots(2,2,false);
            p.ApplyResult(2,true,1,4,3); Check(p.Save.currentStage==8 && p.BestShots(2)==5,"replay win preserves onward stage and best successful shots");
            p.ApplyResult(2,false,0,0,0);Check(p.Save.currentStage==8,"replay loss preserves onward stage");
            p.Persist();var restored=new DadProgress(DadEconomy.Load(),path);Check(restored.BestShots(2)==5 && restored.EquippedValue(1)==.04f && restored.HasPendingGear,"gear, pending card and shot record survive reload");
            File.WriteAllText(path,"{\"version\":2,\"coins\":123,\"currentStage\":1,\"bestStage\":1}");
            var old=new DadProgress(DadEconomy.Load(),path);Check(old.Save.coins==123 && old.BestShots(1)==0,"old v2 save without shot records loads");
            var damage=new SlingshotSim(stage,DadSlingshotPrototype.Arena,1,.72f);DadSchoolPrototype.ApplyGrowth(damage,new[]{1,0,0,0},0,1);
            int sum=0;for(int i=0;i<100;i++)sum+=damage.ImpactDamage(1);
            Check(sum==103 && damage.knockBonus==1,"small impact upgrade accumulates damage without duplicating throw growth");
            DadSlingshotSetup.RunChecks();DadPrototypeSetup.RunPolishChecks();
            Debug.Log("DAD_GROWTH_CHECKS_OK\n"+report);
        }
        catch(Exception e){report.AppendLine("FAIL "+e);Debug.LogException(e);exit=1;}
        finally{if(File.Exists(path))File.Delete(path);File.WriteAllText("Design/dad-growth-replay-checks.txt",report.ToString());EditorApplication.Exit(exit);}
    }
}
