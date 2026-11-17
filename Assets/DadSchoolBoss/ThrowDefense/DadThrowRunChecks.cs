#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Window-free checks for 연속전 (DadThrowRun): what each pick does to a throw, how the waves grow, and the
// screen flow from the title through a pick to the end of a run. Report() plays whole runs with bots under
// different ways of picking; Tools/run_dad_throw_run_report.py writes it out.
// The bots are arithmetic, not people: they show what the numbers allow, never whether a run is fun.
public static class DadThrowRunChecks
{
    const DadThrowModel.Weapon Golf=DadThrowModel.Weapon.Golf,Shoe=DadThrowModel.Weapon.Shoe;
    const DadThrowFlow.Screen Playing=DadThrowFlow.Screen.Playing,Picking=DadThrowFlow.Screen.Pick,Title=DadThrowFlow.Screen.Title,Defeat=DadThrowFlow.Screen.Defeat;
    static readonly Vector2 Flat=new Vector2(150,50);
    static DadThrowStageSet Set(DadThrowLayout layout)
    {return DadThrowStageSet.Parse(System.IO.File.ReadAllText("Assets/DadSchoolBoss/Resources/DadThrowStages/chapter1.json"),layout.Backgrounds,layout.Field.Gate);}
    // A wave of the run that is over the moment it is asked, with `health` touches left: stands in for a fight.
    static DadThrowModel Fought(DadThrowRun run,DadThrowField field,int health){return new DadThrowModel(field,DadThrowStage.Empty(),health,run.Rules);}

    // A street nobody is due on for hours: a rule check stands its own people up, and the fight neither ends nor adds anyone.
    static DadThrowStage Quiet(){var stage=new DadThrowStage{id="quiet",health=5};stage.groups.Add(new DadThrowStage.Group{at=9999,count=1,firstX=1300,speed=1,hp=1});return stage;}
    public delegate int Chooser(DadThrowRun run);
    // Takes the first offer that is on the list, in the list's order; with none of them on the table, the first offer.
    public static Chooser Prefer(params DadThrowRun.Pick[] order)
    {return run=>{foreach(DadThrowRun.Pick want in order){int at=run.Offers.IndexOf(want);if(at>=0)return at;}return 0;};}
    // A whole run through the flow, the way the screen plays it: the policy throws, the chooser takes an offer.
    // Stops at `waves` cleared, so a bot that never loses still ends.
    public static DadThrowRun Play(DadThrowStageSet set,DadThrowField field,int seed,Func<DadThrowBots.Policy> player,Chooser choose,int waves)
    {
        var flow=new DadThrowFlow(set.stages,field,null,new DadThrowSave(),null,set.kinds);flow.Tick(2);flow.StartRun(seed);
        var bot=new DadThrowBots.Run();DadThrowBots.Policy policy=player();
        for(int i=0;i<120*60*45;i++)
        {
            if(flow.Now==Picking){if(flow.Run.Wave>=waves)break;flow.Choose(choose(flow.Run));bot=new DadThrowBots.Run();continue;}
            if(flow.Now!=Playing)break;
            policy(flow.Model,bot);flow.Tick(1/120f);
        }
        return flow.Run;
    }

    public static string Run(DadThrowLayout layout,Action<bool,string> check)
    {
        var r=new StringBuilder();DadThrowField field=DadThrowChecks.Test;DadThrowStageSet set=Set(layout);

        // ── what a pick changes in a throw ──
        var plain=new DadThrowRules();
        var m=new DadThrowModel(field,Quiet());m.Fire(Golf,Flat);
        check(plain.GolfDamage==1&&plain.GolfCount==1&&plain.ShoeBlast==0&&plain.ChainReload==0&&m.projectiles.Count==1,
            "a stage plays with plain throws: one ball, one point of hurt, a shoe that only pushes");
        m=new DadThrowModel(field,Quiet());m.Rules.GolfCount=3;m.Fire(Golf,new Vector2(150,0));DadThrowChecks.Advance(m,.1f);
        check(m.projectiles.Count==3&&m.projectiles[1].pos.y<m.projectiles[0].pos.y-3&&m.projectiles[2].pos.y>m.projectiles[0].pos.y+3,
            "extra balls leave with the aimed one, one above it and one below");
        m=new DadThrowModel(field,Quiet());m.Rules.GolfCount=2;m.Rules.GolfReload=0;int thrown=0;
        for(int i=0;i<40;i++)if(m.Fire(Golf,new Vector2(150,-150)))thrown++;
        check(thrown==6&&m.projectiles.Count==12,"with two balls to a throw the hand may fill the air with twelve, not six");
        m=new DadThrowModel(field,Quiet());var chair=m.AddEnemy(430,3);m.Fire(Golf,Flat);DadThrowChecks.Advance(m,.6f);
        var heavy=new DadThrowModel(field,Quiet());heavy.Rules.GolfDamage=3;heavy.AddEnemy(430,3);heavy.Fire(Golf,Flat);DadThrowChecks.Advance(heavy,.6f);
        check(chair.hp==2&&m.Killed==0&&heavy.Killed==1,"a plain ball takes one of a tough opponent's three; a heavy ball puts him down at once");
        m=new DadThrowModel(field,Quiet());m.Rules.ShoeBlast=1;m.AddEnemy(400);m.AddEnemy(440);m.AddEnemy(470);var far=m.AddEnemy(700);var hard=m.AddEnemy(450,3);
        m.Fire(Shoe,Flat);DadThrowChecks.Advance(m,.5f);
        check(m.Killed==3&&m.enemies.Count==2&&hard.hp==2&&hard.slow>0&&hard.x>500&&far.hp==1&&far.slow==0,
            "a blasting shoe puts down the plain ones it gathers, hurts and pushes a tough one, and leaves those out of reach alone");
        m=new DadThrowModel(field,Quiet());m.Rules.GolfReload=5;m.Rules.ChainReload=3;m.AddEnemy(400);m.AddEnemy(430);m.AddEnemy(460);
        m.Fire(Golf,Flat);DadThrowChecks.Advance(m,.55f);
        var slow=new DadThrowModel(field,Quiet());slow.Rules.GolfReload=5;slow.AddEnemy(400);slow.AddEnemy(430);slow.AddEnemy(460);
        slow.Fire(Golf,Flat);DadThrowChecks.Advance(slow,.55f);
        check(m.Killed==3&&m.CanFire&&slow.Killed==3&&!slow.CanFire,"a ball through enough people readies the hand at once; without the pick the reload still has to run");
        var carried=new DadThrowRules();m=new DadThrowModel(field,DadThrowStage.Empty(),2,carried);m.AddEnemy(field.Gate-1);m.Tick(.01f);
        check(m.Rules==carried&&m.Health==1&&m.Escaped==1,"a wave of a run starts with the touches the gate has left and the throws the run has picked");

        foreach(DadThrowRun.Card card in DadThrowRun.Cards)check(DadThrowRun.Of(card.pick)==card&&card.most>=1&&card.name.Length>0&&card.line.Length>0,"the "+card.pick+" card is found by its pick and has words");
        Func<DadThrowRun.Pick,DadThrowRules> after=pick=>{var rules=new DadThrowRules();DadThrowRun.Apply(rules,pick);return rules;};
        check(after(DadThrowRun.Pick.Pierce).Pierce==plain.Pierce+6&&after(DadThrowRun.Pick.Reload).GolfReload<plain.GolfReload&&after(DadThrowRun.Pick.Twin).GolfCount==2
            &&after(DadThrowRun.Pick.Heavy).GolfDamage==2&&after(DadThrowRun.Pick.Bounce).MaxBounces==plain.MaxBounces+1,"each ball pick changes the one thing its card names");
        check(after(DadThrowRun.Pick.Blast).ShoeBlast==1&&after(DadThrowRun.Pick.Wide).ShoeRadius>plain.ShoeRadius&&after(DadThrowRun.Pick.Sticky).SlowTime>plain.SlowTime
            &&after(DadThrowRun.Pick.Sticky).SlowFactor<plain.SlowFactor&&after(DadThrowRun.Pick.Flow).ChainReload==DadThrowRun.FlowChain,"each shoe pick and the chain pick change the one thing their cards name");
        var untouched=after(DadThrowRun.Pick.Repair);
        check(untouched.Pierce==plain.Pierce&&untouched.GolfReload==plain.GolfReload&&untouched.ShoeBlast==0&&untouched.GolfCount==1,"a repair changes no throw");

        // ── how the waves grow ──
        var run=new DadThrowRun(set.kinds,set.stages[0].backgroundKey,1);
        check(run.Wave==1&&run.Health==DadThrowRun.MaxHealth&&run.Stage.id=="1조"&&run.Stage.Total==60&&run.Offers.Count==0&&run.Picked==0&&!run.Over,
            "a run opens on wave 1 with a whole gate, sixty people and plain throws");
        check(run.Stage.Total<set.stages[0].Total,"the first wave of a run is lighter than stage 1-1");
        bool sound=true,ordered=true,grows=true;int firstTough=0,firstFast=0;
        var growing=new DadThrowRun(set.kinds,set.stages[0].backgroundKey,5);
        for(int wave=1;wave<=24;wave++)
        {
            DadThrowStage stage=growing.Stage;int tough=0,fast=0;
            sound&=stage.Total==DadThrowRun.People(wave)&&stage.Total<=DadThrowStageSet.MaxTotal&&stage.id==wave+"조"&&stage.groups.Count>=2;
            for(int i=0;i<stage.groups.Count;i++)
            {
                DadThrowStage.Group g=stage.groups[i];
                sound&=g.count>=1&&g.hp>=1&&g.speed>0&&g.firstX>layout.Field.Gate&&g.firstX>field.Gate&&(g.count==1||g.spacing>0)&&g.kind!=null&&g.at>=0;
                if(i>0)ordered&=g.at>=stage.groups[i-1].at;
                if(g.kind==set.kinds["tough"])tough+=g.count;if(g.kind==set.kinds["fast"])fast+=g.count;
            }
            sound&=tough==DadThrowRun.Tough(wave)&&fast==DadThrowRun.Fast(wave);
            if(tough>0&&firstTough==0)firstTough=wave;if(fast>0&&firstFast==0)firstFast=wave;
            if(wave>1)grows&=DadThrowRun.People(wave)>=DadThrowRun.People(wave-1)&&DadThrowRun.Pace(wave)>=DadThrowRun.Pace(wave-1)&&DadThrowRun.Gap(wave)<=DadThrowRun.Gap(wave-1);
            growing.Cleared(Fought(growing,field,5));if(growing.Offers.Count>0)growing.Take(0);
        }
        check(sound,"every wave table up to wave 24 holds the people it promises, inside the ceiling, each pack starting in front of the gate");
        check(ordered&&grows,"packs come in the order of their times, and no wave is smaller, slower or sparser than the one before");
        check(firstFast==2&&firstTough==3&&DadThrowRun.Pace(40)==DadThrowRun.MaxPace&&DadThrowRun.People(40)==DadThrowStageSet.MaxTotal&&DadThrowRun.ToughHealth(40)>DadThrowRun.ToughHealth(12),"quick ones join from wave 2, tough ones from wave 3; pace and head count stop at their ceilings while the tough ones go on getting tougher");
        var faceless=new Dictionary<string,DadThrowStage.Kind>();faceless["normal"]=set.kinds["normal"];
        var one=new DadThrowRun(faceless,"bg",1);for(int i=0;i<4;i++){one.Cleared(Fought(one,field,5));one.Take(0);}
        bool refused=false;try{new DadThrowRun(new Dictionary<string,DadThrowStage.Kind>(),"bg",1);}catch(ArgumentException){refused=true;}
        check(one.Stage.Total==DadThrowRun.People(5)&&refused,"a stage file without the tough and quick faces still gives a run with one face; without the normal one it is refused by name");

        // ── offers ──
        run.Cleared(Fought(run,field,5));
        check(run.Offers.Count==3&&run.Offers[0]!=run.Offers[1]&&run.Offers[1]!=run.Offers[2]&&run.Offers[0]!=run.Offers[2]&&run.Wave==1,
            "a cleared wave lays out three different offers and waits");
        check(!run.Take(-1)&&!run.Take(3)&&run.Wave==1&&run.Picked==0,"an offer that is not on the table cannot be taken");
        DadThrowRun.Pick took=run.Offers[1];
        check(run.Take(1)&&run.Level(took)==1&&run.Picked==1&&run.Wave==2&&run.Stage.id=="2조"&&run.Offers.Count==0&&!run.Take(0),
            "taking an offer brings on the next wave and clears the table: nothing more can be taken until that wave is cleared");
        bool neverWhole=true,everHurt=false;
        for(int seed=0;seed<60;seed++)
        {
            var whole=new DadThrowRun(set.kinds,"bg",seed);whole.Cleared(Fought(whole,field,5));neverWhole&=!whole.Offers.Contains(DadThrowRun.Pick.Repair);
            var hurt=new DadThrowRun(set.kinds,"bg",seed);hurt.Cleared(Fought(hurt,field,3));int at=hurt.Offers.IndexOf(DadThrowRun.Pick.Repair);
            if(at<0)continue;
            everHurt=true;hurt.Take(at);neverWhole&=hurt.Health==4&&hurt.Rules.Pierce==plain.Pierce&&hurt.Rules.GolfCount==1;
        }
        check(neverWhole&&everHurt,"a repair is never offered to a whole gate; taken, it gives back one touch and changes no throw");
        var a=new DadThrowRun(set.kinds,"bg",42);var b=new DadThrowRun(set.kinds,"bg",42);var c=new DadThrowRun(set.kinds,"bg",43);bool same=true,differs=false;
        for(int wave=0;wave<8;wave++)
        {
            a.Cleared(Fought(a,field,4));b.Cleared(Fought(b,field,4));c.Cleared(Fought(c,field,4));
            for(int i=0;i<a.Offers.Count;i++){same&=a.Offers.Count==b.Offers.Count&&a.Offers[i]==b.Offers[i];differs|=i>=c.Offers.Count||a.Offers[i]!=c.Offers[i];}
            a.Take(0);b.Take(0);c.Take(0);
        }
        check(same&&differs,"the same seed lays out the same offers wave after wave; another seed lays out others");
        var greedy=new DadThrowRun(set.kinds,"bg",9);bool capped=true;int dry=0;
        for(int wave=0;wave<60;wave++)
        {
            int before=greedy.Wave;greedy.Cleared(Fought(greedy,field,5));
            if(greedy.Offers.Count==0){dry++;capped&=greedy.Wave==before+1;}else greedy.Take(0);
            foreach(DadThrowRun.Card card in DadThrowRun.Cards)capped&=greedy.Level(card.pick)<=card.most;
        }
        check(capped&&dry>0&&greedy.Rules.GolfCount==3&&greedy.Rules.GolfDamage==3&&greedy.Rules.ShoeBlast==3&&greedy.Rules.Pierce==plain.Pierce+18,
            "no pick is taken more often than its card allows; once nothing is left to offer the next wave simply comes");
        var ended=new DadThrowRun(set.kinds,"bg",3);var last=Fought(ended,field,1);last.AddEnemy(field.Gate-1);last.Tick(.01f);ended.Lost(last);
        ended.Cleared(Fought(ended,field,5));
        check(ended.Over&&ended.Health==0&&ended.Offers.Count==0&&!ended.Take(0)&&ended.Wave==1,"a lost run is over: no offers, nothing to take, the wave count stays where it fell");

        // ── screen flow ──
        var bare=new DadThrowFlow(set.stages,field);bare.Tick(2);
        check(!bare.RunAvailable&&!bare.StartRun(1)&&bare.Now==Title,"without the opponent kinds the title has no run to start");
        var save=new DadThrowSave();var flow=new DadThrowFlow(set.stages,field,null,save,null,set.kinds);
        check(flow.RunAvailable&&!flow.StartRun(1)&&flow.Run==null,"a run cannot be started behind the logo");
        flow.Tick(2);
        check(flow.StartRun(7)&&flow.Now==Playing&&flow.Run!=null&&flow.Stage.id=="1조"&&flow.Model.Health==5&&flow.Runs==1&&!flow.HasNext&&!flow.WaitingForAim,
            "연속전 on the title opens wave 1 at once, on the run's own table");
        check(!flow.StartRun(8)&&!flow.Start()&&flow.Runs==1,"a second tap starts nothing more");
        var bot=new DadThrowBots.Run();
        for(int i=0;i<120*120&&flow.Now==Playing;i++){DadThrowBots.Spam(flow.Model,bot);flow.Tick(1/120f);}
        check(flow.Now==Picking&&flow.Run.Offers.Count==3&&flow.Model.projectiles.Count==0&&flow.Furthest==0&&save.cleared.Count==0,
            "clearing a wave lays out three offers; the stages and what the save has opened are not touched");
        int kept=flow.Model.Health,runs=flow.Runs;flow.Tick(5);flow.Interrupted();
        check(flow.Now==Picking&&!flow.Pause()&&!flow.Resume()&&!flow.Retry()&&!flow.Next()&&!flow.ToTitle()&&!flow.Back()&&flow.Now==Picking,
            "the offers wait: time, leaving the app and every other button leave them on the table");
        check(!flow.Choose(-1)&&!flow.Choose(3)&&flow.Now==Picking,"an offer that is not there cannot be chosen");
        // A repair, if that is what the first offer happens to be, gives one touch back.
        if(flow.Run.Offers[0]==DadThrowRun.Pick.Repair)kept++;
        check(flow.Choose(0)&&flow.Now==Playing&&flow.Stage.id=="2조"&&flow.Model.Health==kept&&flow.Runs==runs+1&&flow.Run.Picked==1&&flow.Model.Rules==flow.Run.Rules&&!flow.Choose(0),
            "choosing starts wave 2 with the gate as it was and the pick in the throws; a second tap chooses nothing");
        check(flow.Pause()&&flow.Resume()&&flow.Now==Playing,"a wave of a run pauses and resumes like any fight");
        for(int i=0;i<120*120&&flow.Now==Playing;i++)flow.Tick(1/120f);
        check(flow.Now==Defeat&&flow.Run.Over&&flow.Run.Wave==2&&flow.BestWave==2&&save.bestWave==2,"letting the gate fall ends the run on its result, and the furthest wave is remembered");
        int seedBefore=flow.Run.Seed;
        check(flow.Retry()&&flow.Now==Playing&&flow.Stage.id=="1조"&&flow.Model.Health==5&&flow.Run.Picked==0&&flow.Run.Seed!=seedBefore&&flow.Model.Rules.Pierce==plain.Pierce,
            "again is a new run: wave 1, a whole gate, plain throws, other offers");
        check(flow.Pause()&&flow.ToTitle()&&flow.Now==Title&&flow.Run==null&&flow.Model==null&&flow.BestWave==2,"leaving from the pause drops the run and keeps the best");
        check(flow.Start()&&flow.Stage.id=="1-1"&&flow.Run==null&&flow.HasNext&&flow.Model.Rules.GolfCount==1&&flow.Model.Health==set.stages[0].health,
            "after a run the stages play as before, with plain throws");
        DadThrowSave back=DadThrowSave.Parse(save.ToJson());DadThrowSave old=DadThrowSave.Parse("{\"schemaVersion\":1,\"introSeen\":true,\"furthest\":1,\"cleared\":[\"1-1\"]}");
        check(back.bestWave==2&&old.bestWave==0&&old.furthest==1&&old.cleared.Count==1,"the best wave is saved and read back; a save written before runs existed still loads");

        // ── a bot through a real run ──
        DadThrowRun played=Play(set,field,11,()=>DadThrowBots.Spam,Prefer(),4);
        check(played.Wave==4&&played.Picked==3&&!played.Over&&played.Felled>=DadThrowRun.People(1)+DadThrowRun.People(2)+DadThrowRun.People(3)+DadThrowRun.People(4)-20,
            "a bot that never misses plays four waves of a run through the flow, taking an offer after each");
        DadThrowRun fallen=Play(set,field,11,()=>DadThrowBots.Idle,Prefer(),4);
        check(fallen.Over&&fallen.Wave==1&&fallen.Health==0,"a player who throws nothing loses the run on wave 1");

        // ── where the run's screens sit ──
        Rect canvas=new Rect(0,0,DadThrowLayout.W,DadThrowLayout.H);
        Func<Rect,Rect,bool> inside=(inner,outer)=>inner.xMin>=outer.xMin&&inner.xMax<=outer.xMax&&inner.yMin>=outer.yMin&&inner.yMax<=outer.yMax;
        check(inside(DadThrowLayout.RunButton,canvas)&&inside(DadThrowLayout.RunBest,canvas)&&!DadThrowLayout.RunButton.Overlaps(DadThrowLayout.StartButton)
            &&!DadThrowLayout.RunBest.Overlaps(DadThrowLayout.RunButton)&&DadThrowLayout.RunButton.height>=56,"the run button sits on the title beside start, inside the canvas, tall enough for a thumb");
        bool placed=true;
        for(int count=1;count<=DadThrowRun.Offered;count++)for(int i=0;i<count;i++)
        {
            Rect card=DadThrowLayout.PickCard(i,count);
            placed&=inside(card,canvas)&&card.yMin>=DadThrowLayout.PickHeading.yMax&&card.yMin>DadThrowLayout.TopBar&&(i==0||!card.Overlaps(DadThrowLayout.PickCard(i-1,count)));
            placed&=inside(DadThrowLayout.PickName(card),card)&&inside(DadThrowLayout.PickLine(card),card)&&inside(DadThrowLayout.PickLevel(card),card)&&card.Contains(DadThrowLayout.PickIcon(card));
            placed&=DadThrowLayout.PickLine(card).yMin>=DadThrowLayout.PickName(card).yMax&&DadThrowLayout.PickLevel(card).yMin>=DadThrowLayout.PickLine(card).yMax;
        }
        check(placed,"one, two or three offer cards stand side by side under their heading, inside the canvas, each with its picture, name, line and level in order");
        return r.ToString();
    }

    // ── bots through whole runs ──
    static readonly CultureInfo PlainNumbers=CultureInfo.InvariantCulture;
    const DadThrowRun.Pick Pierce=DadThrowRun.Pick.Pierce,Reload=DadThrowRun.Pick.Reload,Twin=DadThrowRun.Pick.Twin,Heavy=DadThrowRun.Pick.Heavy,Bounce=DadThrowRun.Pick.Bounce,
        Blast=DadThrowRun.Pick.Blast,Wide=DadThrowRun.Pick.Wide,Sticky=DadThrowRun.Pick.Sticky,Flow=DadThrowRun.Pick.Flow,Repair=DadThrowRun.Pick.Repair;
    static string Taken(DadThrowRun run)
    {
        var text=new StringBuilder();
        foreach(DadThrowRun.Card card in DadThrowRun.Cards)if(run.Level(card.pick)>0)text.Append(text.Length>0?", ":"").Append(card.pick).Append(run.Level(card.pick)>1?"x"+run.Level(card.pick):"");
        return text.Length>0?text.ToString():"nothing";
    }
    public static string Report()
    {
        var r=new StringBuilder();DadThrowLayout layout=DadThrowChecks.Layout();DadThrowStageSet set=Set(layout);DadThrowField field=layout.Field;
        r.AppendLine("THROW RUN REPORT (연속전)");
        r.AppendLine("Bots, not people. A row shows what the numbers allow under one way of picking; it says nothing about fun.");
        r.AppendLine("A run stops at wave "+Limit+" cleared, so a bot that never loses still ends.");
        r.AppendLine();
        r.AppendLine("Wave tables");
        for(int wave=1;wave<=Limit;wave++)
            r.AppendLine(string.Format(PlainNumbers,"  wave {0,2}: {1,3} people ({2} tough x{3} health, {4} quick), pace {5:0}, a pack every {6:0.00}s",
                wave,DadThrowRun.People(wave),DadThrowRun.Tough(wave),DadThrowRun.ToughHealth(wave),DadThrowRun.Fast(wave),DadThrowRun.Pace(wave),DadThrowRun.Gap(wave)));
        string[] ways={"first offer","ball: more and faster","ball: heavy first","shoe first","repair first"};
        Chooser[] choosers={Prefer(),Prefer(Twin,Reload,Pierce,Flow,Bounce,Heavy),Prefer(Heavy,Twin,Pierce,Reload,Flow,Bounce),Prefer(Blast,Wide,Sticky,Heavy,Twin),Prefer(Repair,Twin,Heavy,Blast)};
        string[] players={"sloppy hand, shoes when crowded","sloppy hand, balls only","never misses, shoe first"};
        for(int p=0;p<players.Length;p++)
        {
            r.AppendLine();r.AppendLine("Player: "+players[p]);
            for(int w=0;w<ways.Length;w++)
            {
                var line=new StringBuilder();float sum=0;int most=0,least=int.MaxValue,cleared=0;string build="";
                for(int seed=1;seed<=Seeds;seed++)
                {
                    int bot=seed*17+p;
                    Func<DadThrowBots.Policy> player=p==0?(Func<DadThrowBots.Policy>)(()=>DadThrowBots.Sloppy(bot,true)):p==1?(Func<DadThrowBots.Policy>)(()=>DadThrowBots.Sloppy(bot,false)):()=>DadThrowBots.ShoeFirst;
                    DadThrowRun run=Play(set,field,seed*101,player,choosers[w],Limit);
                    // A run that fell on wave n held n-1 waves; one stopped at the limit held them all.
                    int held=run.Over?run.Wave-1:run.Wave;sum+=held;most=Math.Max(most,held);least=Math.Min(least,held);if(!run.Over)cleared++;
                    line.Append(held).Append(run.Over?"":"+").Append(seed<Seeds?" ":"");if(seed==1)build=Taken(run);
                }
                r.AppendLine(string.Format(PlainNumbers,"  {0,-22} waves held: {1} | average {2:0.0}, least {3}, most {4}, reached the limit {5}/{6} | seed 1 took: {7}",
                    ways[w],line,sum/Seeds,least,most,cleared,Seeds,build));
            }
        }
        return r.ToString();
    }
    public const int Limit=18,Seeds=5;
}
#endif
