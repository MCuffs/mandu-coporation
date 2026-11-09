#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class DadDifficultyChecks
{
    static void Expect(bool ok, string name, StringBuilder report) { if (!ok) throw new Exception("DIFFICULTY_CHECK_FAILED: " + name); report.AppendLine("PASS " + name); }
    public static string Run()
    {
        var report = new StringBuilder("Difficulty, rematch and one-time reward checks\n");
        Rect arena= DadSlingshotPrototype.Arena;
        bool consistent=true;
        for(int floor=1;floor<=3;floor++) for(int n=1;n<=20;n++)
        {
            var basic=SlingshotSim.ForMain(floor,n,arena);
            var first=SlingshotSim.Rematch(floor,n,1,arena);
            var last=SlingshotSim.Rematch(floor,n,3,arena);
            consistent &= first.spawns.Count>basic.spawns.Count && last.spawns.Count>first.spawns.Count && last.shotLimit<first.shotLimit && last.speedScale>first.speedScale && last.bossHp==basic.bossHp && last.eliteHp==basic.eliteHp;
        }
        Expect(SlingshotSim.ForMain(1,9,arena).shotLimit==0 && SlingshotSim.ForMain(1,10,arena).shotLimit>SlingshotSim.ForMain(1,10,arena).par,"tutorial stays unrestricted; campaign budget begins at 1-10",report);
        var assistedStage=SlingshotSim.ForMain(1,20,arena);var assisted=new SlingshotSim(assistedStage,arena);int limit=assistedStage.shotLimit;
        DadSchoolPrototype.ApplyGrowth(assisted,new int[4],0,1.25f);
        Expect(assistedStage.shotLimit==limit+5,"campaign losing-streak assist also adds up to five shots",report);
        var strictStage=SlingshotSim.Rematch(1,20,3,arena);var strict=new SlingshotSim(strictStage,arena);limit=strictStage.shotLimit;
        DadSchoolPrototype.ApplyGrowth(strict,new int[4],0,1.25f);
        Expect(strictStage.shotLimit==limit,"rematch shot cap is fixed rather than silently eased",report);
        Expect(consistent,"all 60 stages have escalating rematches without inflated HP",report);
        bool noEarlyRole=true;for(int n=1;n<=3;n++) foreach(var foe in SlingshotSim.Rematch(1,n,3,arena).spawns) noEarlyRole &= foe.kind==SlingshotSim.Kind.Gum;
        Expect(noEarlyRole,"tutorial rematches never introduce unlearned roles",report);
        var st=new SlingshotSim.Stage{par=1,shotLimit=1};
        var sim=new SlingshotSim(st,arena);var survivor=sim.Add(SlingshotSim.Kind.Tray,arena.xMin+30,arena.yMin+20);survivor.cooldown=999;
        Expect(sim.Launch(Vector2.right*150),"last allowed shot launches",report);
        sim.Tick(.1f);Expect(!sim.Over && !sim.Launch(Vector2.right*150),"budget does not end a flight or permit an extra shot",report);
        for(int i=0;i<600&&!sim.Over;i++)sim.Tick(1/120f);
        Expect(sim.phase==SlingshotSim.Phase.Lost && sim.lossReason.Contains("발사 소진"),"survivors after final settling produce explicit shot-budget loss",report);
        sim=new SlingshotSim(new SlingshotSim.Stage{par=1,shotLimit=1},arena);
        sim.Add(SlingshotSim.Kind.Tray,arena.xMin+30,arena.yMin+20).cooldown=999;
        sim.shots=1; sim.balls.Add(new SlingshotSim.Ball{pos=arena.center,vel=Vector2.right*100,returned=true,life=2.6f});
        for(int i=0;i<80;i++)sim.Tick(.01f);
        Expect(!sim.Over,"budget waits for a returned dodgeball to finish its chain",report);
        sim=new SlingshotSim(new SlingshotSim.Stage{par=1,shotLimit=1},arena);sim.Add(SlingshotSim.Kind.Gum,arena.center.x,arena.center.y).cooldown=999;
        sim.shots=1;sim.foes[0].hp=0;sim.Tick(.02f);
        Expect(sim.phase==SlingshotSim.Phase.Won,"clearing on final budget shot wins before the budget loss check",report);
        sim=new SlingshotSim(new SlingshotSim.Stage{par=10},arena);sim.Add(SlingshotSim.Kind.Tray,arena.xMin+30,arena.yMin+20).cooldown=999;sim.dad.landed=.48f;
        Expect(!sim.Launch(Vector2.up*150),"landing cannot be skipped by rapid release",report);
        for(int i=0;i<60;i++)sim.Tick(.01f);
        Expect(sim.Launch(Vector2.up*150),"launch is available after landing recovery",report);
        var p=new DadProgress(new DadEconomy(),Path.Combine(Path.GetTempPath(),"dad-difficulty-"+Guid.NewGuid()+".json"));
        Expect(!p.CanRematch(1,1),"uncleared campaign stage locks rematch",report);
        p.Save.stars=new[]{2};p.Save.bestStage=2;p.Save.currentStage=2;
        Expect(p.CanRematch(1,1)&&!p.CanRematch(1,2),"only the next uncompleted rank unlocks",report);
        double coins=p.Save.coins;int boxes=p.Save.lunchboxes;
        p.ApplyRematch(1,1,false);
        Expect(p.Save.coins==coins&&p.Save.lunchboxes==boxes&&p.RematchRank(1)==0,"rematch failure pays nothing and loses no inventory",report);
        var reward=p.ApplyRematch(1,1,true);
        Expect(reward.coins>0&&reward.boxes==1&&p.RematchRank(1)==1,"first rematch win pays and unlocks next rank",report);
        coins=p.Save.coins;boxes=p.Save.lunchboxes;p.ApplyRematch(1,1,true);
        Expect(p.Save.coins==coins&&p.Save.lunchboxes==boxes,"repeat rank win cannot duplicate first-win rewards",report);
        p.ApplyRematch(1,3,true);
        Expect(p.RematchRank(1)==1&&p.Save.coins==coins,"cannot skip a rank through result processing",report);
        Expect(p.Save.bestStage==2&&p.Save.currentStage==2&&p.Stars(1)==2,"rematch never overwrites campaign stars or onward progress",report);
        var mastered=p.ApplyResult(1,true,1,9,3);double baseReplay=p.Eco.BaseCoins(1)*p.Eco.replayCoinShare;
        Expect(mastered.coins>baseReplay&&mastered.boxes==p.Eco.boxesPerClear+1&&mastered.bonusLine.Contains("별 3개"),"first three-star clear adds explicit mastery bonus",report);
        var repeat=p.ApplyResult(1,true,1,9,3);
        Expect(repeat.boxes==p.Eco.boxesPerClear&&repeat.bonusLine=="","three-star mastery bonus cannot repeat",report);
        p.ApplyRematch(1,2,true);p.ApplyRematch(1,3,true);
        Expect(p.RematchRank(1)==3&&!p.CanRematch(1,4)&&p.CanRematch(1,3),"all three ranks complete; final rank stays replayable",report);
        return report.ToString();
    }
}
#endif
