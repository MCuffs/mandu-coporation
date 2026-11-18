#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

// Window-free checks for what was added on 2026-11-17 after the owner's play: a body that is hit from the shins
// to the head, tray-holders that stop a ball until a shoe has been there, stages off a curve, one won for every
// challenger sent back, and the shop. Report() plays a bot through the stages and the shop, stage after stage;
// Tools/run_dad_throw_growth_report.py writes it out.
// The bots are arithmetic, not people: they show what the numbers allow, never whether it is fun.
public static class DadThrowGrowthChecks
{
    const DadThrowModel.Weapon Golf=DadThrowModel.Weapon.Golf,Shoe=DadThrowModel.Weapon.Shoe;
    const DadThrowModel.Phase Playing=DadThrowModel.Phase.Playing,Cleared=DadThrowModel.Phase.WaveClear;
    const DadThrowFlow.Screen OnTitle=DadThrowFlow.Screen.Title,OnFight=DadThrowFlow.Screen.Playing,OnClear=DadThrowFlow.Screen.Clear,OnDefeat=DadThrowFlow.Screen.Defeat,OnShop=DadThrowFlow.Screen.Shop;
    static readonly Vector2 Flat=new Vector2(150,50);
    static DadThrowStageSet Set(DadThrowLayout layout)
    {return DadThrowStageSet.Parse(System.IO.File.ReadAllText("Assets/DadSchoolBoss/Resources/DadThrowStages/chapter1.json"),layout.Backgrounds,layout.Field.Gate);}
    // A street nobody is due on for hours: a rule check stands its own people up, and the fight neither ends nor adds anyone.
    static DadThrowModel Quiet(DadThrowField field)
    {var stage=new DadThrowStage{id="quiet",health=5};stage.groups.Add(new DadThrowStage.Group{at=9999,count=1,firstX=1300,speed=1,hp=1});return new DadThrowModel(field,stage);}

    // A stand-in for a person who knows what a tray is: takes `think` seconds to aim, aims well `good` of the
    // time, and with `shoes` throws a shoe first when the nearest challenger holds a tray. It is still a bot.
    public static DadThrowBots.Policy Hand(int seed,bool shoes,float think=.45f,float good=.65f)
    {
        var random=new System.Random(seed);
        return (m,run)=>
        {
            if(!m.CanFire){run.think=-1;return;}
            if(run.think<0)run.think=(int)(think*120);
            if(run.think>0){run.think--;return;}
            DadThrowModel.Enemy lead=DadThrowBots.Lead(m);if(lead==null)return;
            bool aimed=random.NextDouble()<good;
            // A braced or dashing boss is a tray too, wherever on the street he is.
            DadThrowModel.Enemy chief=m.Chief,mark=lead.guard?lead:chief!=null&&chief.guard&&chief.x<=DadThrowModel.Width?chief:null;
            if(shoes&&aimed&&mark!=null)
            {
                DadThrowBots.Aim shoe=DadThrowBots.BestShoe(m,mark);
                if(shoe.any&&m.Fire(Shoe,shoe.pull)){run.shoe++;run.think=-1;return;}
            }
            Vector2 pull=DadThrowBots.Pulls[random.Next(DadThrowBots.Pulls.Length)];
            if(aimed)
            {
                DadThrowBots.Aim most,onLead;DadThrowBots.BestGolf(m,lead,out most,out onLead);
                if(most.any)pull=lead.x-m.Field.Gate<DadThrowBots.Danger&&onLead.any?onLead.pull:most.pull;
            }
            if(m.Fire(Golf,pull)){run.golf++;run.think=-1;}
        };
    }
    // One stage fought to its end with what the save has bought.
    public static DadThrowModel Fight(DadThrowStage stage,DadThrowField field,DadThrowSave save,DadThrowBots.Policy policy)
    {
        var m=new DadThrowModel(field,stage){Rules=DadThrowShop.Rules(save)};var run=new DadThrowBots.Run{end=m};
        for(int i=0;i<120*300&&m.phase==Playing;i++){policy(m,run);m.Tick(1/120f);}
        return m;
    }
    public delegate void Buyer(DadThrowSave save);
    // Buys the first thing on the list it can afford, again and again; with nothing on the list in reach, the
    // cheapest thing in the shop that is.
    public static Buyer Shopper(params DadThrowShop.Item[] order)
    {
        return save=>
        {
            for(int guard=0;guard<40;guard++)
            {
                bool bought=false;
                foreach(DadThrowShop.Item want in order)if(DadThrowShop.Buy(save,want)){bought=true;break;}
                if(bought)continue;
                DadThrowShop.Card cheapest=null;
                foreach(DadThrowShop.Card card in DadThrowShop.Cards)
                    if(DadThrowShop.CanBuy(save,card.item)&&(cheapest==null||DadThrowShop.Price(save,card.item)<DadThrowShop.Price(save,cheapest.item)))cheapest=card;
                if(cheapest==null||!DadThrowShop.Buy(save,cheapest.item))return;
            }
        };
    }

    public static string Run(DadThrowLayout layout,Action<bool,string> check)
    {
        var r=new StringBuilder();DadThrowField field=DadThrowChecks.Test;DadThrowStageSet set=Set(layout);
        string street=set.stages[0].backgroundKey;var plain=new DadThrowRules();

        // ── a body is hit from the shins to the top of the head ──
        Func<float,float,bool> level=(above,tall)=>DadThrowModel.SweptBody(new Vector2(200,field.Ground-above),new Vector2(700,field.Ground-above),440,field.Ground,tall,DadThrowModel.GolfHit);
        check(level(95,107)&&level(60,107)&&level(12,107),"a ball at head height, at the belly or along the shins meets the body");
        check(!level(135,107),"a ball that clears the head misses");
        check(level(125,126)&&!level(125,100),"a taller opponent is met where a shorter one is cleared");
        check(!DadThrowModel.SweptBody(new Vector2(200,500),new Vector2(380,500),440,field.Ground,107,DadThrowModel.GolfHit),"a flight that ends short of the body does not meet it");
        var m=Quiet(field);m.AddEnemy(300);m.Fire(Golf,Flat);DadThrowChecks.Advance(m,.4f);
        check(m.Killed==1,"someone standing right in front of dad is met by a ball leaving the hand (it used to pass over the head)");

        // ── what the shop changes in a throw ──
        m=Quiet(field);m.Fire(Golf,Flat);
        check(plain.GolfDamage==1&&plain.GolfCount==1&&plain.ShoeBlast==0&&plain.ChainReload==0&&m.projectiles.Count==1,
            "with nothing bought a throw is plain: one ball, one point of hurt, a shoe that only pushes");
        m=Quiet(field);m.Rules.GolfCount=3;m.Fire(Golf,new Vector2(150,0));DadThrowChecks.Advance(m,.1f);
        check(m.projectiles.Count==3&&m.projectiles[1].pos.y<m.projectiles[0].pos.y-3&&m.projectiles[2].pos.y>m.projectiles[0].pos.y+3,
            "extra balls leave with the aimed one, one above it and one below");
        m=Quiet(field);m.Rules.GolfCount=2;m.Rules.GolfReload=0;int thrown=0;
        for(int i=0;i<40;i++)if(m.Fire(Golf,new Vector2(150,-150)))thrown++;
        check(thrown==6&&m.projectiles.Count==12,"with two balls to a throw the hand may fill the air with twelve, not six");
        m=Quiet(field);var chair=m.AddEnemy(430,3);m.Fire(Golf,Flat);DadThrowChecks.Advance(m,.6f);
        var heavy=Quiet(field);heavy.Rules.GolfDamage=3;heavy.AddEnemy(430,3);heavy.Fire(Golf,Flat);DadThrowChecks.Advance(heavy,.6f);
        check(chair.hp==2&&m.Killed==0&&heavy.Killed==1,"a plain ball takes one of a tough opponent's three; a heavy ball puts him down at once");
        m=Quiet(field);m.Rules.ShoeBlast=1;m.AddEnemy(400);m.AddEnemy(440);m.AddEnemy(470);var far=m.AddEnemy(700);var hard=m.AddEnemy(450,3);
        m.Fire(Shoe,Flat);DadThrowChecks.Advance(m,.5f);
        check(m.Killed==3&&m.enemies.Count==2&&hard.hp==2&&hard.slow>0&&hard.x>500&&far.hp==1&&far.slow==0,
            "a blasting shoe puts down the plain ones it gathers, hurts and pushes a tough one, and leaves those out of reach alone");
        m=Quiet(field);m.Rules.GolfReload=5;m.Rules.ChainReload=3;m.AddEnemy(400);m.AddEnemy(430);m.AddEnemy(460);
        m.Fire(Golf,Flat);DadThrowChecks.Advance(m,.55f);
        var slow=Quiet(field);slow.Rules.GolfReload=5;slow.AddEnemy(400);slow.AddEnemy(430);slow.AddEnemy(460);
        slow.Fire(Golf,Flat);DadThrowChecks.Advance(slow,.55f);
        check(m.Killed==3&&m.CanFire&&slow.Killed==3&&!slow.CanFire,"a ball through enough people readies the hand at once; without that the reload still has to run");

        // ── trays ──
        m=Quiet(field);var tray=m.AddEnemy(400);tray.guard=true;m.AddEnemy(440);m.AddEnemy(470);
        m.Rules.Pierce=30;m.Rules.GolfDamage=3;m.Fire(Golf,Flat);DadThrowChecks.Advance(m,.55f);
        check(m.Killed==0&&m.enemies.Count==3&&tray.hp==1&&tray.guard&&m.BlockSerial==1&&m.projectiles.Count==0,
            "a tray stops a ball dead, however far the ball has been upgraded: nobody is hurt and nobody behind is reached");
        DadThrowChecks.Advance(m,.3f);m.Fire(Shoe,Flat);DadThrowChecks.Advance(m,.5f);
        bool stripped=!tray.guard&&tray.slow>0&&tray.x>450;
        DadThrowChecks.Advance(m,.4f);m.Fire(Golf,Flat);DadThrowChecks.Advance(m,.6f);
        check(stripped&&!m.enemies.Contains(tray)&&m.Killed>=1&&m.BlockSerial==1,"a shoe knocks the tray out of the hand and pushes him back; the next ball takes him");
        // Pulled a little upward, a ball leaves the hand over the head of someone 150 in front of dad and comes down far behind him.
        m=Quiet(field);tray=m.AddEnemy(400);tray.guard=true;for(int x=730;x<=880;x+=30)m.AddEnemy(x);
        Vector2 lob=new Vector2(Mathf.Cos(-4*Mathf.Deg2Rad),Mathf.Sin(-4*Mathf.Deg2Rad))*150;
        m.Fire(Golf,lob);DadThrowChecks.Advance(m,1.6f);
        check(m.Killed>=1&&tray.guard&&m.BlockSerial==0&&m.enemies.Contains(tray),"a ball lobbed over the tray still reaches the people behind it");
        m=new DadThrowModel(field,DadThrowCurve.Stage(4,set.kinds,street));m.Tick(1/120f);int held=0,others=0;
        foreach(DadThrowModel.Enemy e in m.enemies){if(e.kind.guards&&e.guard)held++;else if(e.guard)others++;}
        check(held>0&&others==0,"on the street only the tray-holders hold trays");
        var twins=new DadThrowModel(field,DadThrowStage.Empty());var holder=twins.AddEnemy(400);holder.guard=true;twins.AddEnemy(440);twins.AddEnemy(470);
        DadThrowBots.Aim most,onLead;DadThrowBots.BestGolf(twins,holder,out most,out onLead);
        check(!onLead.any&&most.hits<=2,"a bot trying its throws sees the tray too: no ball takes the one holding it");
        check(set.kinds["guard"].guards&&set.kinds["guard"].bare!=null&&!set.kinds["normal"].guards&&set.kinds["normal"].bare==null&&set.kinds["guard"].BareFrame(1).EndsWith("_1"),
            "the stage file says who holds a tray and which pictures show him without it");
        Func<string,bool> refused=json=>{try{DadThrowStageSet.Parse(json,new[]{"bg"},215);}catch(ArgumentException){return true;}return false;};
        string kindJson="{\"enemies\":{\"normal\":{\"resource\":\"x/walk\",\"frames\":2,\"facesLeft\":true,\"canvas\":768,\"pivotX\":384,\"pivotY\":720,\"standPixels\":560,\"heights\":[102]GUARDS}},"
            +"\"stages\":[{\"id\":\"1-1\",\"backgroundKey\":\"bg\",\"health\":5,\"baseReward\":1,\"groups\":[{\"at\":0,\"count\":1,\"firstX\":800,\"enemy\":\"normal\",\"speed\":48,\"hp\":1}]}]}";
        check(!refused(kindJson.Replace("GUARDS",""))&&!refused(kindJson.Replace("GUARDS",",\"guards\":true,\"bareResource\":\"x/bare\""))&&refused(kindJson.Replace("GUARDS",",\"guards\":\"yes\"")),
            "a stage file may leave the tray out, or say it with true or false; anything else is refused");

        // ── the stage curve ──
        check(DadThrowCurve.Id(1)=="1-1"&&DadThrowCurve.Id(5)=="1-5"&&DadThrowCurve.Id(6)=="2-1"&&DadThrowCurve.Id(30)=="6-5","stages are named by chapter and place, five to a chapter");
        List<DadThrowStage> game=DadThrowCurve.Build(set.kinds,street,set.stages);
        bool sound=true,ordered=true,grows=true,led=true;int firstTough=0,firstFast=0,firstGuard=0;
        for(int number=1;number<=DadThrowCurve.Stages;number++)
        {
            DadThrowStage stage=game[number-1];int tough=0,fast=0,guards=0;
            sound&=stage.Total==DadThrowCurve.People(number)&&stage.Total<=DadThrowStageSet.MaxTotal&&stage.id==DadThrowCurve.Id(number)&&stage.groups.Count>=2&&stage.health==5&&stage.backgroundKey==street;
            for(int i=0;i<stage.groups.Count;i++)
            {
                DadThrowStage.Group g=stage.groups[i];
                sound&=g.count>=1&&g.hp>=1&&g.speed>0&&g.firstX>layout.Field.Gate&&g.firstX>field.Gate&&(g.count==1||g.spacing>0)&&g.kind!=null&&g.at>=0;
                if(i>0)ordered&=g.at>=stage.groups[i-1].at;
                if(g.kind==set.kinds["tough"])tough+=g.count;if(g.kind==set.kinds["fast"])fast+=g.count;
                if(g.kind!=set.kinds["guard"])continue;
                guards+=g.count;
                // Right behind every group of trays runs a pack of plain runners, at the same moment and the same pace.
                bool pack=false;
                foreach(DadThrowStage.Group other in stage.groups)pack|=other.kind==set.kinds["normal"]&&other.at==g.at&&other.speed==g.speed&&Mathf.Abs(other.firstX-(g.firstX+g.count*DadThrowCurve.TraySpacing))<.01f;
                led&=pack;
            }
            sound&=tough==DadThrowCurve.Tough(number)&&fast==DadThrowCurve.Fast(number)&&guards==DadThrowCurve.Guards(number);
            if(tough>0&&firstTough==0)firstTough=number;if(fast>0&&firstFast==0)firstFast=number;if(guards>0&&firstGuard==0)firstGuard=number;
            if(number>1)grows&=DadThrowCurve.People(number)>=DadThrowCurve.People(number-1)&&DadThrowCurve.Pace(number)>=DadThrowCurve.Pace(number-1)&&DadThrowCurve.Gap(number)<=DadThrowCurve.Gap(number-1)
                &&DadThrowCurve.Tough(number)>=DadThrowCurve.Tough(number-1)&&DadThrowCurve.ToughHealth(number)>=DadThrowCurve.ToughHealth(number-1)&&DadThrowCurve.Guards(number)>=DadThrowCurve.Guards(number-1);
        }
        check(game.Count==DadThrowCurve.Stages&&sound,"all thirty stage tables hold the people they promise, inside the ceiling, each pack starting in front of the gate");
        check(ordered&&grows,"packs come in the order of their times, and no stage is smaller, slower, sparser or softer than the one before");
        check(firstFast==2&&firstTough==3&&firstGuard==4&&led,"quick ones join from stage 2, tough ones from stage 3, trays from stage 4, each tray group at the front of a pack");
        check(DadThrowCurve.People(5)==100&&DadThrowCurve.Pace(5)==136&&DadThrowCurve.Tough(5)==7&&DadThrowCurve.ToughHealth(5)==3&&DadThrowCurve.Fast(5)==10&&DadThrowCurve.Guards(5)==4&&Mathf.Abs(DadThrowCurve.Gap(5)-1.4f)<.001f,
            "the first chapter is the climb it always was");
        check(DadThrowCurve.People(6)-DadThrowCurve.People(5)<DadThrowCurve.People(5)-DadThrowCurve.People(4)&&DadThrowCurve.Pace(6)-DadThrowCurve.Pace(5)<DadThrowCurve.Pace(5)-DadThrowCurve.Pace(4)
            &&DadThrowCurve.Guards(6)-DadThrowCurve.Guards(5)<DadThrowCurve.Guards(5)-DadThrowCurve.Guards(4)&&DadThrowCurve.Tough(6)-DadThrowCurve.Tough(5)<DadThrowCurve.Tough(5)-DadThrowCurve.Tough(4)
            &&DadThrowCurve.Fast(6)-DadThrowCurve.Fast(5)<DadThrowCurve.Fast(5)-DadThrowCurve.Fast(4),
            "from the second chapter every stage adds less than a stage of the first chapter did");
        check(DadThrowCurve.People(12)==142&&DadThrowCurve.Guards(12)==11&&DadThrowCurve.Tough(12)==14&&DadThrowCurve.ToughHealth(12)==5&&DadThrowCurve.People(14)<DadThrowStageSet.MaxTotal&&DadThrowCurve.People(15)==DadThrowStageSet.MaxTotal,
            "the twelfth stage is lighter than a full street, which comes at the fifteenth");
        check(game[0].Total==60&&game[0].Total<set.stages[0].Total&&DadThrowCurve.People(30)==DadThrowStageSet.MaxTotal&&DadThrowCurve.Pace(30)<=DadThrowCurve.MaxPace,
            "the first stage is lighter than the hand-made stampede; the last fills the street at a pace inside the ceiling");
        bool hints=game[0].hint=="pull"&&game[3].hint=="shoe",endings=game[4].ending;
        for(int i=0;i<game.Count;i++){if(i!=0&&i!=3)hints&=game[i].hint==null;if(i!=4)endings&=!game[i].ending;}
        check(hints&&endings&&game[0].after!=null&&game[2].after!=null&&game[1].after==null&&game[0].after==set.stages[0].after,
            "the first stage teaches the pull, the first with trays points at the shoe, the ending follows 1-5, and the lines people say come from the stage file");
        var faceless=new Dictionary<string,DadThrowStage.Kind>();faceless["normal"]=set.kinds["normal"];
        DadThrowStage bare=DadThrowCurve.Stage(12,faceless,"bg");bool trayless=true;foreach(DadThrowStage.Group g in bare.groups)trayless&=g.kind==set.kinds["normal"];
        bool nameless=false;try{DadThrowCurve.Stage(1,new Dictionary<string,DadThrowStage.Kind>(),"bg");}catch(ArgumentException){nameless=true;}
        check(bare.Total==DadThrowCurve.People(12)&&trayless&&nameless,"a stage file with only the normal face still gives every stage, with no trays; without that face it is refused by name");

        // ── the shop ──
        foreach(DadThrowShop.Card card in DadThrowShop.Cards)check(DadThrowShop.Of(card.item)==card&&card.most>=1&&card.price>0&&card.name.Length>0&&card.line.Length>0,"the "+card.item+" card is found by its item and has words and a price");
        Func<DadThrowShop.Item,DadThrowRules> after=item=>{var rules=new DadThrowRules();DadThrowShop.Apply(rules,item);return rules;};
        check(after(DadThrowShop.Item.Pierce).Pierce==plain.Pierce+6&&after(DadThrowShop.Item.Reload).GolfReload<plain.GolfReload&&after(DadThrowShop.Item.Twin).GolfCount==2
            &&after(DadThrowShop.Item.Heavy).GolfDamage==2&&after(DadThrowShop.Item.Bounce).MaxBounces==plain.MaxBounces+1,"each ball item changes the one thing its card names");
        check(after(DadThrowShop.Item.Blast).ShoeBlast==1&&after(DadThrowShop.Item.Wide).ShoeRadius>plain.ShoeRadius&&after(DadThrowShop.Item.Sticky).SlowTime>plain.SlowTime
            &&after(DadThrowShop.Item.Sticky).SlowFactor<plain.SlowFactor&&after(DadThrowShop.Item.Flow).ChainReload==DadThrowShop.FlowChain,"each shoe item and the chain item change the one thing their cards name");
        var purse=new DadThrowSave();DadThrowRules empty=DadThrowShop.Rules(purse);
        check(empty.Pierce==plain.Pierce&&empty.GolfReload==plain.GolfReload&&empty.GolfCount==1&&empty.GolfDamage==1&&empty.ShoeBlast==0&&empty.ShoeRadius==plain.ShoeRadius,"a new player throws plain throws");
        check(!DadThrowShop.CanBuy(purse,DadThrowShop.Item.Sticky)&&!DadThrowShop.Buy(purse,DadThrowShop.Item.Sticky)&&purse.coins==0,"nothing can be bought with an empty purse");
        purse.coins=5000;int first=DadThrowShop.Price(purse,DadThrowShop.Item.Blast),blasts=DadThrowShop.Of(DadThrowShop.Item.Blast).most;
        check(DadThrowShop.Buy(purse,DadThrowShop.Item.Blast)&&purse.coins==5000-first&&DadThrowShop.Level(purse,DadThrowShop.Item.Blast)==1&&DadThrowShop.Price(purse,DadThrowShop.Item.Blast)==first*2,
            "buying takes the price out of the purse and puts the item on; the next one costs twice the first");
        bool rising=true;
        for(int have=1;have<blasts;have++){rising&=DadThrowShop.Price(purse,DadThrowShop.Item.Blast)==first*(have+1)&&!DadThrowShop.Full(purse,DadThrowShop.Item.Blast)&&DadThrowShop.Buy(purse,DadThrowShop.Item.Blast);}
        check(rising&&DadThrowShop.Full(purse,DadThrowShop.Item.Blast)&&!DadThrowShop.CanBuy(purse,DadThrowShop.Item.Blast)&&!DadThrowShop.Buy(purse,DadThrowShop.Item.Blast)&&purse.coins==5000-first*blasts*(blasts+1)/2,
            "each further one costs the first price more, and an item bought as often as its card allows cannot be bought again");
        DadThrowShop.Buy(purse,DadThrowShop.Item.Twin);DadThrowShop.Buy(purse,DadThrowShop.Item.Twin);DadThrowRules bought=DadThrowShop.Rules(purse);
        check(bought.ShoeBlast==blasts&&bought.GolfCount==3&&bought.Pierce==plain.Pierce,"what has been bought is in the throws, and nothing else is");
        var greedy=new DadThrowSave();greedy.upgrades[(int)DadThrowShop.Item.Twin]=99;
        check(DadThrowShop.Rules(greedy).GolfCount==1+DadThrowShop.Of(DadThrowShop.Item.Twin).most&&!DadThrowShop.CanBuy(greedy,DadThrowShop.Item.Twin),"a level beyond what the shop sells counts as the most it sells");
        DadThrowSave back=DadThrowSave.Parse(purse.ToJson());
        DadThrowSave old=DadThrowSave.Parse("{\"schemaVersion\":1,\"introSeen\":true,\"furthest\":1,\"cleared\":[\"1-1\"]}");
        DadThrowSave odd=DadThrowSave.Parse("{\"schemaVersion\":1,\"introSeen\":true,\"furthest\":0,\"cleared\":[],\"coins\":-5,\"upgrades\":{\"Twin\":9,\"Unknown\":3,\"Blast\":1}}");
        check(back.coins==purse.coins&&DadThrowShop.Level(back,DadThrowShop.Item.Blast)==blasts&&DadThrowShop.Level(back,DadThrowShop.Item.Twin)==2&&DadThrowShop.Level(back,DadThrowShop.Item.Pierce)==0,
            "the purse and what was bought are saved and read back");
        check(old.coins==0&&DadThrowShop.Level(old,DadThrowShop.Item.Twin)==0&&old.furthest==1&&odd.coins==0&&DadThrowShop.Level(odd,DadThrowShop.Item.Twin)==2&&DadThrowShop.Level(odd,DadThrowShop.Item.Blast)==1,
            "a save from before the shop loads with an empty purse; a level too high or an item this build does not sell is held to what the shop sells");

        // ── screen flow: the purse and the shop ──
        var save=new DadThrowSave();var flow=new DadThrowFlow(game,field,null,save,null);
        check(!flow.OpenShop(),"the shop does not open behind the logo");
        flow.Tick(2);
        check(flow.Coins==0&&flow.OpenShop()&&flow.Now==OnShop&&!flow.Start()&&!flow.Buy(DadThrowShop.Item.Sticky)&&!flow.OpenShop(),"the shop opens from the title; with an empty purse nothing is bought, and nothing else starts from inside it");
        check(flow.Back()&&flow.Now==OnTitle&&!flow.CloseShop()&&!flow.Buy(DadThrowShop.Item.Sticky),"back closes the shop onto the title, and outside the shop nothing can be bought");
        check(flow.Start()&&flow.Stage.id=="1-1"&&flow.Model.Total==60&&!flow.OpenShop()&&flow.Model.Rules.GolfCount==1,"start opens stage 1-1 off the curve with plain throws; the shop stays shut during a fight");
        var bot=new DadThrowBots.Run();
        for(int i=0;i<120*3&&flow.Now==OnFight;i++){DadThrowBots.Spam(flow.Model,bot);flow.Tick(1/120f);}
        check(flow.Model.Killed>0&&flow.Coins==flow.Model.Killed&&save.coins==0&&flow.Earned==flow.Model.Killed,"during a fight the purse goes up by one won for each challenger sent back, before anything is banked");
        check(flow.Pause()&&!flow.OpenShop()&&flow.Resume(),"the shop stays shut on the pause too");
        for(int i=0;i<120*120&&flow.Now==OnFight;i++){DadThrowBots.Spam(flow.Model,bot);flow.Tick(1/120f);}
        int paid=flow.Model.Killed,sticky=DadThrowShop.Of(DadThrowShop.Item.Sticky).price;
        check(flow.Now==OnClear&&paid>=55&&save.coins==paid&&flow.Coins==paid&&flow.Earned==paid,"a cleared stage banks its won once: the purse shows what was earned, not twice that");
        check(flow.OpenShop()&&flow.Now==OnShop&&flow.Buy(DadThrowShop.Item.Sticky)&&save.coins==paid-sticky&&!flow.Buy(DadThrowShop.Item.Twin)&&!flow.Next()&&!flow.Retry(),
            "from the clear card the shop sells what the purse can pay for and refuses what it cannot; next and again wait outside");
        check(flow.CloseShop()&&flow.Now==OnClear&&flow.Next()&&flow.Stage.id=="1-2"&&flow.Model.Rules.SlowTime>plain.SlowTime&&flow.Model.Rules.Pierce==plain.Pierce&&flow.Coins==paid-sticky,
            "the shop closes back onto the card; the next stage is fought with what was bought");
        bot=new DadThrowBots.Run();
        for(int i=0;i<120*2&&flow.Now==OnFight;i++){DadThrowBots.Spam(flow.Model,bot);flow.Tick(1/120f);}
        for(int i=0;i<120*120&&flow.Now==OnFight;i++)flow.Tick(1/120f);
        int lost=flow.Model.Killed;
        check(flow.Now==OnDefeat&&lost>0&&save.coins==paid-sticky+lost&&flow.Earned==lost,"a lost stage still pays for everyone sent back");
        check(flow.OpenShop()&&flow.CloseShop()&&flow.Now==OnDefeat&&flow.Retry()&&flow.Stage.id=="1-2"&&flow.Coins==paid-sticky+lost&&flow.Earned==0,"the shop opens from the defeat card too; trying again starts the stage's count from nothing and pays nothing twice");
        bot=new DadThrowBots.Run();
        for(int i=0;i<120*2&&flow.Now==OnFight;i++){DadThrowBots.Spam(flow.Model,bot);flow.Tick(1/120f);}
        int left=flow.Model.Killed;
        check(left>0&&flow.Pause()&&flow.ToTitle()&&flow.Now==OnTitle&&save.coins==paid-sticky+lost+left&&flow.Coins==save.coins&&flow.Earned==0,"leaving a fight from the pause banks what it had earned so far");
        check(flow.Furthest==1&&flow.Start()&&flow.Stage.id=="1-2","the stage reached is kept; start goes on from it");

        // ── a bot through the first stages and the shop ──
        var career=new DadThrowSave();Buyer cheapest=Shopper();int reached=0;
        for(int tries=0;tries<8&&reached<3;tries++)
        {
            DadThrowModel fought=Fight(game[reached],field,career,DadThrowBots.Spam);career.coins+=fought.Killed*DadThrowShop.WonPerFelled;cheapest(career);
            if(fought.phase==Cleared)reached++;
        }
        int owned=0;foreach(int levelBought in career.upgrades)owned+=levelBought;
        check(reached==3&&owned>=2,"a bot that never misses clears the first three stages, buying the cheapest thing it can after each fight");

        // ── a new opponent's entrance ──
        DadThrowStage.Kind trayKind=set.kinds["guard"];
        check(trayKind.entrance!=null&&trayKind.entrance.name.Length>0&&trayKind.entrance.shout.Length>0&&trayKind.entrance.hint.Length>0&&trayKind.entrance.stand>0&&set.kinds["normal"].entrance==null,
            "the stage file gives the tray-holder an entrance: a picture, a name, a line to shout and what to do about him");
        check(!refused(kindJson.Replace("GUARDS",",\"entrance\":{\"resource\":\"x/big\",\"pivotX\":300,\"standPixels\":500,\"name\":\"n\",\"shout\":\"s\",\"hint\":\"h\"}"))
            &&refused(kindJson.Replace("GUARDS",",\"entrance\":{\"resource\":\"x/big\",\"pivotX\":300,\"standPixels\":500,\"name\":\"n\",\"shout\":\"s\"}"))
            &&refused(kindJson.Replace("GUARDS",",\"entrance\":{\"resource\":\"x/big\",\"pivotX\":300,\"standPixels\":0,\"name\":\"n\",\"shout\":\"s\",\"hint\":\"h\"}")),
            "an entrance in the stage file needs all its words and a standing height; one without is refused");
        DadThrowStage.Kind chairKind=set.kinds["tough"];
        DadThrowStage.Kind quickKind=set.kinds["fast"];
        check(chairKind.entrance!=null&&chairKind.entrance.shoe&&trayKind.entrance.shoe&&chairKind.entrance.name!=trayKind.entrance.name
            &&quickKind.entrance!=null&&!quickKind.entrance.shoe&&quickKind.entrance.name!=chairKind.entrance.name&&quickKind.entrance.canvas==2048,
            "the chair and tray entrances show a shoe while the quick challenger shows a ball");
        check(refused(kindJson.Replace("GUARDS",",\"entrance\":{\"resource\":\"x/big\",\"pivotX\":300,\"standPixels\":500,\"name\":\"n\",\"shout\":\"s\",\"hint\":\"h\",\"with\":\"car\"}")),
            "advice can be about the ball or the shoe and nothing else");
        DadThrowStage.Kind bossKind=set.kinds["boss"];
        bool onlyOne=game[1].entrance==quickKind&&game[2].entrance==chairKind&&game[3].entrance==trayKind&&game[4].entrance==bossKind;for(int i=0;i<game.Count;i++)if(i<1||i>4)onlyOne&=game[i].entrance==null;
        check(onlyOne,"the quick one makes his entrance on 1-2, the chair-holder on 1-3, the tray-holder on 1-4 and the boss on 1-5, the first stage each is in, and on no other");
        var first2=new DadThrowSave{introSeen=true,furthest=1};var quick=new DadThrowFlow(game,field,null,first2,null);quick.Tick(2);
        check(quick.Start()&&quick.Stage.id=="1-2"&&quick.Entering==quickKind,"starting 1-2 for the first time brings the quick one on");
        quick.Tick(DadThrowFlow.EnterTime+.1f);
        check(quick.Entering==null&&first2.met.Count==1&&first2.met[0]=="fast","and he is the one remembered");
        var first3=new DadThrowSave{introSeen=true,furthest=2};first3.met.Add("fast");var chairs=new DadThrowFlow(game,field,null,first3,null);chairs.Tick(2);
        check(chairs.Start()&&chairs.Stage.id=="1-3"&&chairs.Entering==chairKind,"starting 1-3 for the first time brings the chair-holder on");
        chairs.Tick(DadThrowFlow.EnterTime+.1f);
        check(chairs.Entering==null&&first3.met.Contains("tough")&&!first3.met.Contains("guard"),"and when he has been seen through he is remembered");
        var both=new DadThrowSave{introSeen=true,furthest=3};var two=new DadThrowFlow(game,field,null,both,null);two.Tick(2);two.Start();
        DadThrowStage.Kind opened=two.Entering;two.Tick(DadThrowFlow.EnterTime+.1f);for(int i=0;i<120*60&&two.Now==OnFight;i++)two.Tick(1/120f);
        two.Retry();DadThrowStage.Kind second=two.Entering;two.Tick(DadThrowFlow.EnterTime+.1f);for(int i=0;i<120*60&&two.Now==OnFight;i++)two.Tick(1/120f);
        two.Retry();DadThrowStage.Kind third=two.Entering;two.Tick(DadThrowFlow.EnterTime+.1f);for(int i=0;i<120*60&&two.Now==OnFight;i++)two.Tick(1/120f);
        check(opened==trayKind&&second==quickKind&&third==chairKind&&two.Retry()&&two.Entering==null&&both.met.Count==3,
            "a stage with three strangers in it brings on one each time it starts, in the order they arrive, never two in a row, and then nobody");
        var newcomer=new DadThrowSave{introSeen=true,furthest=3};newcomer.met.Add("tough");newcomer.met.Add("fast");var enter=new DadThrowFlow(game,field,null,newcomer,null);enter.Tick(2);
        check(enter.Start()&&enter.Stage.id=="1-4"&&enter.Entering==trayKind&&enter.EnterClock==0,"starting 1-4 for the first time brings him on");
        enter.Tick(.3f);
        check(enter.Model.enemies.Count>0&&enter.Model.Clock<.02f&&enter.Model.Health==5&&enter.EnterClock>=.29f&&!enter.SkipEntrance()&&enter.Entering!=null,
            "while he comes on the street holds still with his first people in view, and a tap in the first moment does nothing");
        enter.Tick(.4f);check(enter.Pause()&&enter.Resume()&&enter.Entering!=null,"an entrance can be paused and comes back");
        check(enter.SkipEntrance()&&enter.Entering==null&&newcomer.met.Contains("guard")&&!enter.SkipEntrance(),"a tap after that starts the fight and he is remembered as met");
        float held0=enter.Model.Clock;enter.Tick(.2f);enter.Tick(.2f);
        check(enter.Model.Clock>held0+.3f,"after the entrance the street runs");
        for(int i=0;i<120*60&&enter.Now==OnFight;i++)enter.Tick(1/120f);
        check(enter.Now==OnDefeat&&enter.Retry()&&enter.Entering==null,"trying the stage again does not bring him on a second time");
        var patient=new DadThrowSave{introSeen=true,furthest=3};patient.met.Add("tough");patient.met.Add("fast");var waits=new DadThrowFlow(game,field,null,patient,null);waits.Tick(2);waits.Start();
        waits.Tick(DadThrowFlow.EnterTime-.1f);bool still=waits.Entering!=null;waits.Tick(.2f);
        check(still&&waits.Entering==null&&patient.met.Contains("guard"),"left alone the entrance ends by itself");
        var leaver=new DadThrowSave{introSeen=true,furthest=3};leaver.met.Add("tough");leaver.met.Add("fast");var leaves=new DadThrowFlow(game,field,null,leaver,null);leaves.Tick(2);leaves.Start();leaves.Tick(.2f);
        check(leaves.Pause()&&leaves.ToTitle()&&leaves.Entering==null&&!leaver.met.Contains("guard")&&leaves.Start()&&leaves.Entering==trayKind,"leaving before he has been seen through means he comes on again next time");
        var veteran=new DadThrowSave{introSeen=true,furthest=6};veteran.met.Add("tough");veteran.met.Add("fast");var late=new DadThrowFlow(game,field,null,veteran,null);late.Tick(2);
        check(late.Start()&&late.Stage.id=="2-2"&&late.Entering==trayKind,"a player whose save is already past 1-4 meets him on the next stage he is in");
        late.Tick(DadThrowFlow.EnterTime+.1f);for(int i=0;i<120*60&&late.Now==OnFight;i++)late.Tick(1/120f);
        check(veteran.met.Contains("guard")&&late.Retry()&&late.Entering==null,"and then not again");
        DadThrowSave remembered=DadThrowSave.Parse(newcomer.ToJson());
        check(remembered.met.Count==3&&remembered.met.Contains("guard")&&remembered.met.Contains("tough")&&remembered.met.Contains("fast")&&old.met.Count==0,"who has been met is saved and read back; a save from before has met nobody");

        // ── the boss at the end of the first chapter ──
        {
            const DadThrowModel.Mood Walks=DadThrowModel.Mood.Walk,Braces=DadThrowModel.Mood.Brace,Dashes=DadThrowModel.Mood.Dash,Stunned=DadThrowModel.Mood.Dazed,Roars=DadThrowModel.Mood.Roar,GoesBack=DadThrowModel.Mood.Back;
            check(bossKind.boss!=null&&bossKind.boss.bounty==30&&bossKind.boss.half>DadThrowModel.BodyHalf&&!bossKind.guards&&bossKind.entrance!=null&&bossKind.entrance.shoe&&set.kinds["normal"].boss==null,
                "the stage file says who the boss is: his pictures, a body wider than anyone's, what he pays, and advice about the shoe");
            string cfWords=",\"boss\":{\"brace\":\"x/a\",\"dash\":\"x/b\",\"hit\":\"x/c\",\"dazed\":\"x/d\",\"roar\":\"x/e\",\"fly\":\"x/f\"BODY}";
            check(!refused(kindJson.Replace("GUARDS",cfWords.Replace("BODY","")))&&refused(kindJson.Replace("GUARDS",cfWords.Replace("BODY",",\"bodyHalf\":10")))
                &&refused(kindJson.Replace("GUARDS",cfWords.Replace("BODY","")+",\"guards\":true,\"bareResource\":\"x/bare\""))&&refused(kindJson.Replace("GUARDS",",\"boss\":{\"brace\":\"x/a\"}")),
                "a boss needs all six of his pictures, may not be narrower than anyone and may not carry a tray");
            bool cfOnly=true;for(int i=0;i<game.Count;i++)cfOnly&=game[i].groups.Exists(cfG=>cfG.kind==bossKind)==(i==DadThrowCurve.PerChapter-1);
            DadThrowStage.Group cfHim=game[4].groups.Find(cfG=>cfG.kind==bossKind);float cfLast=0;foreach(DadThrowStage.Group cfEach in game[4].groups)if(cfEach.kind!=bossKind)cfLast=Mathf.Max(cfLast,cfEach.at);
            check(cfOnly&&cfHim.count==1&&cfHim.hp==DadThrowCurve.BossHealth(5)&&cfHim.at>cfLast&&Mathf.Abs(cfHim.speed-DadThrowCurve.Pace(5)*DadThrowCurve.BossPace)<.01f&&game[4].Total==DadThrowCurve.People(5),
                "1-5 ends with its boss: one man, behind everyone else, at half the stage's pace, and one of the stage's hundred; no other stage has one yet");
            var cfTuner=new DadThrowTuner(set.kinds,street);var cfOwed=new DadThrowSave();DadThrowTuner.Lost(cfOwed,"1-5");DadThrowTuner.Lost(cfOwed,"1-5");
            DadThrowStage.Group cfSoft=cfTuner.Table(game[4],5,cfOwed).groups.Find(cfG=>cfG.kind==bossKind);
            check(cfSoft!=null&&cfSoft.count==1&&cfSoft.hp==Mathf.RoundToInt(DadThrowCurve.BossHealth(5)*.94f)&&cfSoft.hp<cfHim.hp,"an eased 1-5 still has its one boss, with a little less health");

            // Where a flat throw comes down: a boss standing a little short of it is in the way of the ball.
            var cfPath=new List<Vector2>();Quiet(field).Predict(Golf,Flat,cfPath);float cfLand=cfPath[cfPath.Count-1].x-10;
            Func<float,int,float,DadThrowModel> cfRing=(cfX,cfHealth,cfPace)=>{DadThrowModel cfStreet=Quiet(field);DadThrowModel.Enemy cfMan=cfStreet.AddEnemy(cfX,cfHealth,cfPace);cfMan.kind=bossKind;return cfStreet;};

            DadThrowModel cfA=cfRing(cfLand+240,40,60);DadThrowModel.Enemy cfBossA=cfA.Chief;DadThrowChecks.Advance(cfA,1);
            check(cfBossA.mood==Walks&&!cfBossA.guard&&Mathf.Abs(cfBossA.x-(cfLand+180))<1,"a boss walks in like anyone, at his own pace");
            DadThrowChecks.Advance(cfA,DadThrowModel.BossEvery-1+.05f);float cfStood=cfBossA.x;
            check(cfBossA.mood==Braces&&cfBossA.guard&&Mathf.Abs(cfStood-cfLand)<2,"every four seconds he stops and braces");
            cfA.Fire(Golf,Flat);DadThrowChecks.Advance(cfA,.6f);
            check(cfA.BlockSerial==1&&cfBossA.hp==40&&cfBossA.mood==Braces&&cfBossA.x==cfStood,"a ball stops on a braced boss as it does on a tray, and he does not budge");

            DadThrowModel cfB=cfRing(1100,40,60);DadThrowModel.Enemy cfBossB=cfB.Chief;DadThrowChecks.Advance(cfB,DadThrowModel.BossEvery+DadThrowModel.BossBrace+.5f);
            bool cfMid=cfBossB.mood==Dashes&&cfBossB.guard;DadThrowChecks.Advance(cfB,.6f);
            check(cfMid&&cfBossB.mood==Walks&&!cfBossB.guard&&Mathf.Abs(cfBossB.x-(1100-60*DadThrowModel.BossEvery-60*DadThrowModel.BossDashPace*DadThrowModel.BossDash))<10,
                "then he dashes for a second at five times his walk, still proof against a ball, and walks on");

            DadThrowModel cfC=cfRing(cfLand+240-plain.ShoePush*DadThrowModel.BossShove,40,60);DadThrowModel.Enemy cfBossC=cfC.Chief;DadThrowChecks.Advance(cfC,DadThrowModel.BossEvery+.05f);float cfAt=cfBossC.x;
            cfC.Fire(Shoe,Flat);DadThrowChecks.Advance(cfC,.6f);
            check(cfC.DazeSerial==1&&cfBossC.mood==Stunned&&!cfBossC.guard&&Mathf.Abs(cfBossC.x-(cfAt+plain.ShoePush*DadThrowModel.BossShove))<.01f,
                "a shoe that reaches him braced takes his feet from under him, and pushes him half as far as it pushes anyone");
            DadThrowChecks.Advance(cfC,.3f);cfC.Fire(Golf,Flat);DadThrowChecks.Advance(cfC,.6f);
            check(cfBossC.hp==40-DadThrowModel.BossDazedHurt*plain.GolfDamage&&cfBossC.mood==Stunned,"while he stands dazed a ball hurts him twice over");
            DadThrowChecks.Advance(cfC,DadThrowModel.BossDazed);
            check(cfBossC.mood==Walks&&cfBossC.x<cfAt+plain.ShoePush*DadThrowModel.BossShove,"and two and a half seconds later he is on his way again");

            DadThrowModel cfD=cfRing(cfLand-30,40,1);DadThrowModel.Enemy cfBossD=cfD.Chief,cfBeside=cfD.AddEnemy(cfLand,1,1);float cfWas=cfBossD.x,cfWasBeside=cfBeside.x;
            cfD.Fire(Shoe,Flat);DadThrowChecks.Advance(cfD,.6f);
            check(cfD.DazeSerial==0&&cfBossD.mood==Walks&&cfBossD.slow>0&&cfBossD.x-cfWas>45&&cfBossD.x-cfWas<55&&cfBeside.x-cfWasBeside>95,
                "a shoe on a walking boss only shoves and slows him, half as far as the man beside him");

            DadThrowModel cfE=cfRing(cfLand,4,1);DadThrowModel.Enemy cfBossE=cfE.Chief;
            cfE.Fire(Golf,Flat);DadThrowChecks.Advance(cfE,.5f);bool cfCalm=cfBossE.hp==3&&!cfBossE.raged&&cfBossE.mood==Walks;
            cfE.Fire(Golf,Flat);DadThrowChecks.Advance(cfE,.5f);
            check(cfCalm&&cfBossE.hp==2&&cfBossE.raged&&cfBossE.mood==Roars,"brought to half his health he loses his temper");
            DadThrowChecks.Advance(cfE,DadThrowModel.BossRoar);bool cfUp=cfBossE.mood==Walks;DadThrowChecks.Advance(cfE,DadThrowModel.BossEveryRaged+.1f);
            check(cfUp&&cfBossE.mood==Braces&&DadThrowModel.BossEveryRaged<DadThrowModel.BossEvery,"and from then on he braces sooner than he used to");

            DadThrowModel cfF=cfRing(field.Gate+6,40,60);DadThrowModel.Enemy cfBossF=cfF.Chief;DadThrowChecks.Advance(cfF,.2f);
            check(cfF.Health==4&&cfF.Escaped==0&&cfF.enemies.Contains(cfBossF)&&cfBossF.mood==GoesBack&&cfF.phase==Playing,"a boss who touches the gate costs a touch and is not out of it");
            DadThrowChecks.Advance(cfF,DadThrowModel.BossBack/(60*DadThrowModel.BossBackPace)+.1f);
            check(cfBossF.mood==Walks&&cfBossF.x>field.Gate+DadThrowModel.BossBack-30&&cfF.Health==4,"he trots back out and comes again");
            var cfThin=new DadThrowStage{id="thin",health=1};cfThin.groups.Add(new DadThrowStage.Group{at=9999,count=1,firstX=1300,speed=1,hp=1});
            var cfOne=new DadThrowModel(field,cfThin);DadThrowModel.Enemy cfBossG=cfOne.AddEnemy(field.Gate+6,40,60);cfBossG.kind=bossKind;DadThrowChecks.Advance(cfOne,.3f);
            check(cfOne.phase==DadThrowModel.Phase.Lost,"and his touch on the last of the gate loses the stage like anyone's");

            DadThrowModel cfH=cfRing(cfLand,1,1);cfH.Fire(Golf,Flat);DadThrowChecks.Advance(cfH,.5f);
            check(cfH.Killed==1&&cfH.Bounty==bossKind.boss.bounty&&cfH.Chief==null,"sending him back pays his bounty on top of the one won everyone pays");
            Vector2 cfTop=new Vector2(540,300),cfBottom=new Vector2(540,560);
            check(DadThrowModel.SweptBody(cfTop,cfBottom,500,570,160,DadThrowModel.GolfHit,bossKind.boss.half)&&!DadThrowModel.SweptBody(cfTop,cfBottom,500,570,160,DadThrowModel.GolfHit),
                "his body is wider to a ball than anyone's");

            var cfSolo=new DadThrowStage{id="solo",health=5,backgroundKey=street};cfSolo.groups.Add(new DadThrowStage.Group{at=.5f,count=1,firstX=1300,speed=120,hp=1,kind=bossKind});
            var cfSave=new DadThrowSave{introSeen=true};var cfFlow=new DadThrowFlow(new List<DadThrowStage>{cfSolo},field,null,cfSave,null);cfFlow.Tick(2);cfFlow.Start();
            bool cfQuiet=cfFlow.Now==OnFight&&cfFlow.Entering==null;
            for(int i=0;i<120*3&&cfFlow.Entering==null;i++)cfFlow.Tick(1/120f);
            DadThrowModel.Enemy cfIn=cfFlow.Model.Chief;float cfStop=cfIn!=null?cfIn.x:0;cfFlow.Tick(1);
            check(cfQuiet&&cfFlow.Entering==bossKind&&cfIn!=null&&cfStop<=DadThrowModel.Width-DadThrowFlow.BossShown&&cfStop>DadThrowModel.Width-DadThrowFlow.BossShown-5&&cfIn.x==cfStop,
                "a boss makes no entrance when the stage starts: he makes it in the middle of the fight, as he steps onto the street, and the street holds still for it");
            bool cfSkipped=cfFlow.SkipEntrance()&&cfFlow.Entering==null&&cfSave.met.Contains("boss");
            var cfThrower=new DadThrowBots.Run();for(int i=0;i<120*60&&cfFlow.Now==OnFight;i++){DadThrowBots.Spam(cfFlow.Model,cfThrower);cfFlow.Tick(1/120f);}
            check(cfSkipped&&cfFlow.Now==OnClear&&cfFlow.Entering==null&&cfFlow.Earned==DadThrowShop.WonPerFelled+bossKind.boss.bounty&&cfSave.coins==cfFlow.Earned,
                "the entrance is made once; a boss sent back through the flow pays one won and his bounty");
            cfFlow.Retry();for(int i=0;i<120*2;i++)cfFlow.Tick(1/120f);
            check(cfFlow.Now==OnFight&&cfFlow.Model.Chief!=null&&cfFlow.Entering==null,"and he does not make it again on the next try");
        }

        // ── difficulty that bends a little toward how the player is doing ──
        var tuner=new DadThrowTuner(set.kinds,street);var note=new DadThrowSave();
        check(DadThrowTuner.Ease(note,"2-1")==0&&DadThrowTuner.Heat(note)==0&&tuner.Table(game[5],6,note)==game[5],"a new player meets every stage exactly as the curve has it");
        DadThrowTuner.Lost(note,"2-1");
        check(DadThrowTuner.Ease(note,"2-1")==0&&tuner.Table(game[5],6,note)==game[5],"one defeat changes nothing");
        DadThrowTuner.Lost(note,"2-1");DadThrowStage eased=tuner.Table(game[5],6,note);
        check(DadThrowTuner.Ease(note,"2-1")==1&&eased!=game[5]&&eased.Total==Mathf.RoundToInt(game[5].Total*.94f)&&eased.id=="2-1"&&eased.health==5&&eased.entrance==game[5].entrance&&eased.hint==game[5].hint&&eased.groups.Count>=2,
            "a second defeat on the same stage takes 6% of its people away and leaves its name, its gate and its words alone");
        check(Mathf.Abs(eased.groups[0].speed-game[5].groups[0].speed*.96f)<.01f,"and 4% of its pace");
        for(int i=0;i<6;i++)DadThrowTuner.Lost(note,"2-1");
        DadThrowStage softest=tuner.Table(game[5],6,note);
        check(DadThrowTuner.Ease(note,"2-1")==DadThrowTuner.MaxEase&&softest.Total==Mathf.RoundToInt(game[5].Total*.76f)&&softest.Total*4>=game[5].Total*3,
            "however often a stage is lost it eases four steps and no further: three quarters of its people are always there");
        check(DadThrowTuner.Ease(note,"2-2")==0&&tuner.Table(game[6],7,note)==game[6],"what one stage is owed is not owed on another");
        DadThrowTuner.Cleared(note,true);
        check(DadThrowTuner.Ease(note,"2-1")==0&&note.defeats==0&&tuner.Table(game[5],6,note)==game[5],"a clear puts the stage back as it was");
        var streak=new DadThrowSave();DadThrowTuner.Cleared(streak,false);
        check(DadThrowTuner.Heat(streak)==0&&tuner.Table(game[2],3,streak)==game[2],"one flawless clear changes nothing");
        DadThrowTuner.Cleared(streak,false);DadThrowStage heated=tuner.Table(game[2],3,streak);
        check(DadThrowTuner.Heat(streak)==1&&heated.Total==Mathf.RoundToInt(game[2].Total*1.05f)&&Mathf.Abs(heated.groups[0].speed-game[2].groups[0].speed*1.03f)<.01f&&DadThrowTuner.Pay(80,1)==88&&DadThrowTuner.Pay(80,0)==80,
            "two flawless clears running heat the next stage by 5% of its people and 3% of its pace, and it pays a tenth more");
        for(int i=0;i<6;i++)DadThrowTuner.Cleared(streak,false);
        check(DadThrowTuner.Heat(streak)==DadThrowTuner.MaxHeat&&DadThrowTuner.Pay(100,3)==130,"heat stops at three steps");
        DadThrowTuner.Cleared(streak,true);
        check(DadThrowTuner.Heat(streak)==DadThrowTuner.MaxHeat-1,"a clear with a touch on the gate cools one step");
        DadThrowTuner.Lost(streak,"3-1");
        check(DadThrowTuner.Heat(streak)==0&&DadThrowTuner.Ease(streak,"3-1")==0,"a defeat cools it all, and eases nothing yet");
        DadThrowStage fullest=tuner.Table(game[29],30,new DadThrowSave{flawless=4});
        check(fullest!=game[29]&&fullest.Total==DadThrowStageSet.MaxTotal&&fullest.groups[0].speed<=DadThrowCurve.MaxPace,"heat never puts more people on the street or more pace into them than the game allows");
        var shy=new DadThrowSave();
        check(!DadThrowTuner.ShoeShy(shy),"a new player is not nagged about the shoe");
        DadThrowTuner.Watched(shy,5,1);
        check(DadThrowTuner.ShoeShy(shy),"five balls stopped on trays against one shoe thrown: the shoe button keeps asking");
        DadThrowTuner.Watched(shy,1,2);
        check(!DadThrowTuner.ShoeShy(shy),"once the shoes keep up with the trays it stops asking");
        var counted=Quiet(field);counted.Fire(Shoe,Flat);DadThrowChecks.Advance(counted,.8f);counted.Fire(Golf,Flat);
        check(counted.ShoesThrown==1,"the fight counts the shoes thrown, not the balls");
        var owing=new DadThrowSave{introSeen=true,furthest=5};owing.met.Add("fast");owing.met.Add("tough");owing.met.Add("guard");
        var bent=new DadThrowFlow(game,field,null,owing,null,tuner);bent.Tick(2);bent.Start();
        check(bent.Stage.id=="2-1"&&bent.Ease==0&&bent.Heat==0&&bent.Model.Total==game[5].Total&&!bent.ShoeShy,"through the flow a stage starts plain when nothing is owed");
        for(int i=0;i<120*60&&bent.Now==OnFight;i++)bent.Tick(1/120f);
        bool once=bent.Now==OnDefeat&&owing.defeats==1&&bent.Retry()&&bent.Ease==0&&bent.Model.Total==game[5].Total;
        for(int i=0;i<120*60&&bent.Now==OnFight;i++)bent.Tick(1/120f);
        check(once&&bent.Now==OnDefeat&&owing.defeats==2&&owing.tunedStage=="2-1"&&bent.Retry()&&bent.Ease==1&&bent.Model.Total==Mathf.RoundToInt(game[5].Total*.94f)&&bent.Stage==game[5],
            "lost twice through the flow, the third try is eased one step, and the stage the screen names is the same stage");
        DadThrowSave owed=DadThrowSave.Parse(owing.ToJson());
        check(owed.tunedStage=="2-1"&&owed.defeats==2&&owed.flawless==0&&old.defeats==0&&old.tunedStage==null&&old.flawless==0&&old.blocked==0,
            "what the tuner keeps is saved and read back; a save from before has nothing owed");
        var hot=new DadThrowSave{introSeen=true,flawless=2};var paying=new DadThrowFlow(game,field,null,hot,null,tuner);paying.Tick(2);paying.Start();
        var thrower=new DadThrowBots.Run();
        for(int i=0;i<120*120&&paying.Now==OnFight;i++){DadThrowBots.Spam(paying.Model,thrower);paying.Tick(1/120f);}
        int sent=paying.Model.Killed;
        check(paying.Heat==1&&paying.Model.Total==Mathf.RoundToInt(game[0].Total*1.05f)&&paying.Now==OnClear&&paying.Earned==DadThrowTuner.Pay(sent,1)&&hot.coins==paying.Earned&&paying.Earned>sent&&paying.Coins==hot.coins,
            "a heated stage through the flow has more people and pays its bonus once");
        var plainFlow=new DadThrowFlow(game,field,null,new DadThrowSave{introSeen=true,flawless=3,tunedStage="1-1",defeats=4},null);plainFlow.Tick(2);plainFlow.Start();
        check(plainFlow.Ease==0&&plainFlow.Heat==0&&plainFlow.Model.Total==game[0].Total&&!plainFlow.ShoeShy,"a flow without a tuner plays every stage as it is listed, whatever the save says");

        // ── the pictures the game loads for the shop, the entrances and the two small effects (asset request v3) ──
        Func<string,bool> onDisk=key=>System.IO.File.Exists("Assets/DadSchoolBoss/Resources/"+key+".png")&&System.IO.File.Exists("Assets/DadSchoolBoss/Resources/"+key+".png.meta");
        bool icons=onDisk(DadThrowShop.CoinKey)&&onDisk(DadThrowShop.ShopKey)&&onDisk(DadThrowShop.SoldOutKey);var keys=new HashSet<string>();
        foreach(DadThrowShop.Card card in DadThrowShop.Cards){icons&=onDisk(DadThrowShop.IconKey(card.item))&&keys.Add(DadThrowShop.IconKey(card.item));}
        check(icons&&DadThrowShop.IconKey(DadThrowShop.Item.Pierce)=="DadThrowShopV1/shop_pierce","every shop item has a picture of its own, named after the item, and the coin, the shop sign and the bought-out mark are there");
        bool dressed=onDisk("DadThrowEntranceV1/entrance_band_lines")&&onDisk("DadThrowEntranceV1/entrance_burst")&&onDisk("DadThrowEntranceV1/entrance_balloon");
        for(int i=0;i<3;i++)dressed&=onDisk("DadThrowFxV2/fx_tray_clang_"+i)&&onDisk("DadThrowFxV2/fx_coin_pop_"+i);
        foreach(DadThrowStage.Kind kind in set.kinds.Values)if(kind.entrance!=null)dressed&=onDisk(kind.entrance.resource);
        check(dressed,"the entrance's band lines, rays and balloon, each entrance picture the stage file names, and the frames of the two effects are there");
        DadThrowStage.Kind.Entrance big=trayKind.entrance;Rect stood=trayKind.EntrancePlace(DadThrowLayout.EntranceFeet.x,DadThrowLayout.EntranceFeet.y,DadThrowLayout.EntranceTall);float grown=DadThrowLayout.EntranceTall/big.stand;
        check(big.canvas==2048&&big.pivotX==1024&&big.pivotY==1900&&big.stand==1400&&chairKind.entrance.canvas==2048
            &&Mathf.Abs(stood.x+big.pivotX*grown-DadThrowLayout.EntranceFeet.x)<.01f&&Mathf.Abs(stood.y+big.pivotY*grown-DadThrowLayout.EntranceFeet.y)<.01f&&Mathf.Abs(stood.width-2048*grown)<.01f&&stood.width==stood.height,
            "an entrance picture is placed by its own canvas and feet point: the soles of the 2048 picture land on the entrance's feet point");
        check(big.canvas*grown<big.canvas&&DadThrowLayout.EntranceTall*2<=big.stand,"the large picture is drawn smaller than it was painted, on a phone that doubles the canvas too");
        string small="{\"enemies\":{\"normal\":{\"resource\":\"x/walk\",\"frames\":2,\"facesLeft\":true,\"canvas\":768,\"pivotX\":384,\"pivotY\":720,\"standPixels\":560,\"heights\":[102],"
            +"\"entrance\":{\"resource\":\"x/big\",\"pivotX\":300,\"standPixels\":500,\"name\":\"n\",\"shout\":\"s\",\"hint\":\"h\"}}},"
            +"\"stages\":[{\"id\":\"1-1\",\"backgroundKey\":\"bg\",\"health\":5,\"baseReward\":1,\"groups\":[{\"at\":0,\"count\":1,\"firstX\":800,\"enemy\":\"normal\",\"speed\":48,\"hp\":1}]}]}";
        DadThrowStage.Kind.Entrance plainOne=DadThrowStageSet.Parse(small,new[]{"bg"},215).kinds["normal"].entrance;
        check(plainOne.canvas==768&&plainOne.pivotY==720&&refused(small.Replace("\"standPixels\":500","\"standPixels\":500,\"canvas\":0")),
            "an entrance that names no canvas of its own uses the walking frames' canvas and feet line; a canvas of nothing is refused");

        // ── where the shop's screens sit ──
        Rect canvas=new Rect(0,0,DadThrowLayout.W,DadThrowLayout.H),resultCard=DadThrowLayout.ResultCard;
        Func<Rect,Rect,bool> inside=(inner,outer)=>inner.xMin>=outer.xMin&&inner.xMax<=outer.xMax&&inner.yMin>=outer.yMin&&inner.yMax<=outer.yMax;
        check(inside(DadThrowLayout.ShopButton,canvas)&&inside(DadThrowLayout.ShopPurse,canvas)&&!DadThrowLayout.ShopButton.Overlaps(DadThrowLayout.StartButton)
            &&!DadThrowLayout.ShopPurse.Overlaps(DadThrowLayout.ShopButton)&&DadThrowLayout.ShopButton.height>=56,"the shop button sits on the title beside start, inside the canvas, tall enough for a thumb");
        check(DadThrowLayout.CoinLabel.yMax<=DadThrowLayout.TopBar&&DadThrowLayout.CoinLabel.xMin>=DadThrowLayout.HouseHealth.xMax&&DadThrowLayout.CoinLabel.xMax<=DadThrowLayout.RemainingLabel.xMin,
            "the purse in the top bar sits between the gate's cells and the count of those still to come");
        check(inside(DadThrowLayout.DefeatShop,resultCard)&&inside(DadThrowLayout.DefeatTitle,resultCard)&&!DadThrowLayout.DefeatShop.Overlaps(DadThrowLayout.DefeatTitle)
            &&DadThrowLayout.DefeatShop.yMin>=DadThrowLayout.DefeatRetry.yMax&&DadThrowLayout.DefeatShop.height>=56&&inside(DadThrowLayout.ResultEarned,canvas)&&DadThrowLayout.ResultEarned.yMax<=resultCard.yMin
            &&DadThrowLayout.ResultEarned.yMin>=DadThrowLayout.TopBar,"the defeat card offers the shop and the title side by side under the retry, and what the stage paid stands over the card");
        bool shelf=inside(DadThrowLayout.ShopHeading,canvas)&&inside(DadThrowLayout.ShopCoins,canvas)&&inside(DadThrowLayout.ShopClose,canvas)&&!DadThrowLayout.ShopHeading.Overlaps(DadThrowLayout.ShopCoins)
            &&!DadThrowLayout.ShopCoins.Overlaps(DadThrowLayout.ShopClose)&&DadThrowLayout.ShopClose.height>=56;
        for(int i=0;i<DadThrowShop.Cards.Length;i++)
        {
            Rect card=DadThrowLayout.ShopCard(i);
            shelf&=inside(card,canvas)&&card.yMin>=DadThrowLayout.ShopClose.yMax&&card.yMin>=DadThrowLayout.ShopHeading.yMax;
            for(int j=0;j<i;j++)shelf&=!card.Overlaps(DadThrowLayout.ShopCard(j));
            shelf&=inside(DadThrowLayout.ShopName(card),card)&&inside(DadThrowLayout.ShopLine(card),card)&&inside(DadThrowLayout.ShopBuy(card),card)&&card.Contains(DadThrowLayout.ShopIcon(card))&&DadThrowLayout.ShopBuy(card).height>=48;
            shelf&=DadThrowLayout.ShopLine(card).yMin>=DadThrowLayout.ShopName(card).yMax&&DadThrowLayout.ShopBuy(card).yMin>=DadThrowLayout.ShopLine(card).yMax;
            for(int pip=0;pip<DadThrowShop.Cards[i].most;pip++)shelf&=inside(DadThrowLayout.ShopPip(card,pip),card)&&!DadThrowLayout.ShopPip(card,pip).Overlaps(DadThrowLayout.ShopBuy(card))&&DadThrowLayout.ShopPip(card,pip).yMin>=DadThrowLayout.ShopLine(card).yMax;
        }
        check(shelf,"the shop's heading, purse and way out share the top; nine cards stand three by three under them, each with its picture, name, line, cells and price in order");
        check(inside(DadThrowLayout.EntranceName,canvas)&&inside(DadThrowLayout.EntranceShout,canvas)&&inside(DadThrowLayout.EntranceHint,canvas)&&inside(DadThrowLayout.EntranceSkip,canvas)
            &&DadThrowLayout.EntranceName.yMin>=DadThrowLayout.TopBar&&DadThrowLayout.EntranceName.yMax<=DadThrowLayout.EntranceShout.yMin&&DadThrowLayout.EntranceShout.yMax<=DadThrowLayout.EntranceHint.yMin
            &&DadThrowLayout.EntranceHint.yMax<=DadThrowLayout.GolfButton.yMin&&DadThrowLayout.EntranceHint.Contains(DadThrowLayout.EntranceHintIcon)&&inside(DadThrowLayout.EntranceHintText,DadThrowLayout.EntranceHint)
            &&canvas.Contains(DadThrowLayout.EntranceFeet)&&DadThrowLayout.EntranceFeet.y-DadThrowLayout.EntranceTall>=DadThrowLayout.TopBar&&!DadThrowLayout.EntranceSkip.Overlaps(DadThrowLayout.EntranceHint),
            "an entrance puts the name, the line and the advice one under the other on the left, clear of the top bar and the buttons, and stands the man inside the canvas on the right");
        Rect words=DadThrowLayout.EntranceShoutText;float corner=DadThrowLayout.BalloonCorner*DadThrowLayout.BalloonScale;
        check(inside(words,DadThrowLayout.EntranceShout)&&words.height>=48&&words.width>=400&&corner*2<DadThrowLayout.EntranceShout.height&&corner*2<DadThrowLayout.EntranceShout.width
            &&canvas.Contains(DadThrowLayout.EntranceBurstCentre)&&DadThrowLayout.EntranceBurstCentre.y<DadThrowLayout.EntranceFeet.y,
            "the shouted line has room inside its balloon, whose corners fit without meeting, and the rays sit behind the man's body");
        bool pictured=true;
        for(int i=0;i<DadThrowShop.Cards.Length;i++)
        {
            Rect card=DadThrowLayout.ShopCard(i);Vector2 at=DadThrowLayout.ShopIcon(card);float half=DadThrowLayout.ShopIconSize/2;Rect icon=new Rect(at.x-half,at.y-half,half*2,half*2);
            pictured&=inside(icon,card)&&!icon.Overlaps(DadThrowLayout.ShopName(card))&&!icon.Overlaps(DadThrowLayout.ShopLine(card))&&!icon.Overlaps(DadThrowLayout.ShopBuy(card))&&!icon.Overlaps(DadThrowLayout.ShopPip(card,0));
        }
        check(pictured&&DadThrowLayout.ShopIconSize*2<=512,"an item's picture sits in its card clear of the name, the line, the cells and the price, and is drawn smaller than it was painted");
        return r.ToString();
    }

    // ── a bot through the stages and the shop ──
    static readonly CultureInfo PlainNumbers=CultureInfo.InvariantCulture;
    public const int Reach=20,Tries=36,Seeds=2;
    static int Owned(DadThrowSave save){int sum=0;foreach(DadThrowShop.Card card in DadThrowShop.Cards)sum+=DadThrowShop.Level(save,card.item);return sum;}
    static int Shelf(){int sum=0;foreach(DadThrowShop.Card card in DadThrowShop.Cards)sum+=card.most;return sum;}
    static string Bought(DadThrowSave save)
    {
        var text=new StringBuilder();
        foreach(DadThrowShop.Card card in DadThrowShop.Cards){int level=DadThrowShop.Level(save,card.item);if(level>0)text.Append(text.Length>0?", ":"").Append(card.item).Append(level>1?"x"+level:"");}
        return text.Length>0?text.ToString():"nothing";
    }
    public static string Report()
    {
        var r=new StringBuilder();DadThrowLayout layout=DadThrowChecks.Layout();DadThrowStageSet set=Set(layout);DadThrowField field=layout.Field;
        List<DadThrowStage> game=DadThrowCurve.Build(set.kinds,set.stages[0].backgroundKey,set.stages);
        r.AppendLine("THROW STAGES AND SHOP REPORT");
        r.AppendLine("Bots, not people. A row shows what the numbers allow under one way of spending; it says nothing about fun.");
        r.AppendLine("A bot plays the stages in order, banks one won for each challenger sent back (win or lose), spends after every fight,");
        r.AppendLine("and tries a lost stage again. It stops after "+Tries+" fights or on reaching stage "+Reach+".");
        r.AppendLine();
        r.AppendLine("Stage tables");
        for(int number=1;number<=Reach;number++)
            r.AppendLine(string.Format(PlainNumbers,"  {0,-4} {1,3} people ({2,2} tough x{3} health, {4,2} quick, {5,2} trays), pace {6:0}, a pack every {7:0.00}s",
                DadThrowCurve.Id(number),DadThrowCurve.People(number),DadThrowCurve.Tough(number),DadThrowCurve.ToughHealth(number),DadThrowCurve.Fast(number),DadThrowCurve.Guards(number),DadThrowCurve.Pace(number),DadThrowCurve.Gap(number))
                +(DadThrowCurve.BossHealth(number)>0?", and last the boss with "+DadThrowCurve.BossHealth(number)+" health":""));
        r.AppendLine();
        r.AppendLine("Shop");
        foreach(DadThrowShop.Card card in DadThrowShop.Cards)r.AppendLine(string.Format(PlainNumbers,"  {0,-7} first {1,3} won, up to {2} times",card.item,card.price,card.most));
        string[] ways={"cheapest first","balls first","shoes first","a bit of both"};
        Buyer[] buyers={Shopper(),
            Shopper(DadThrowShop.Item.Reload,DadThrowShop.Item.Pierce,DadThrowShop.Item.Twin,DadThrowShop.Item.Heavy,DadThrowShop.Item.Flow,DadThrowShop.Item.Bounce),
            Shopper(DadThrowShop.Item.Blast,DadThrowShop.Item.Wide,DadThrowShop.Item.Sticky),
            Shopper(DadThrowShop.Item.Pierce,DadThrowShop.Item.Blast,DadThrowShop.Item.Reload,DadThrowShop.Item.Wide,DadThrowShop.Item.Heavy,DadThrowShop.Item.Twin)};
        string[] players={"unsteady hand, a shoe for every tray","unsteady hand, balls only"};
        for(int p=0;p<players.Length;p++)
        {
            r.AppendLine();r.AppendLine("Player: "+players[p]);
            for(int w=0;w<ways.Length;w++)for(int seed=1;seed<=Seeds;seed++)
            {
                var save=new DadThrowSave();int stage=0,fights=0,again=0,lastBuy=0;var perStage=new StringBuilder();
                while(stage<Reach&&fights<Tries)
                {
                    DadThrowModel fought=Fight(game[stage],field,save,Hand(seed*31+fights*7+p,p==0));fights++;again++;
                    int had=Owned(save);save.coins+=fought.Killed*DadThrowShop.WonPerFelled;buyers[w](save);if(Owned(save)>had)lastBuy=stage+1;
                    if(fought.phase==Cleared){perStage.Append(again).Append(' ');stage++;again=0;}
                }
                r.AppendLine(string.Format(PlainNumbers,"  {0,-15} seed {1}: cleared {2,2} of {3} stages in {4} fights{5} | fights per stage: {6}| bought: {7} | {9} of {10} bought, the last after a fight on {11} | purse {8}",
                    ways[w],seed,stage,Reach,fights,stage<Reach?", stuck on "+DadThrowCurve.Id(stage+1)+" ("+again+" tries)":"",perStage,Bought(save),save.coins,Owned(save),Shelf(),lastBuy>0?DadThrowCurve.Id(lastBuy):"-"));
            }
        }
        // The same player and the same ways of spending, with the tuner bending the stages.
        var tuner=new DadThrowTuner(set.kinds,set.stages[0].backgroundKey);
        r.AppendLine();r.AppendLine("Player: "+players[0]+", WITH the difficulty tuner (compare with the first block)");
        for(int w=0;w<ways.Length;w++)for(int seed=1;seed<=Seeds;seed++)
        {
            var save=new DadThrowSave();int stage=0,fights=0,again=0,easedFights=0,heatedFights=0,worst=0,lastBuy=0;var perStage=new StringBuilder();
            while(stage<Reach&&fights<Tries)
            {
                DadThrowStage listed=game[stage];int ease=DadThrowTuner.Ease(save,listed.id),heat=DadThrowTuner.Heat(save),on=stage+1,had=Owned(save);
                if(ease>0)easedFights++;if(heat>0)heatedFights++;
                DadThrowModel fought=Fight(tuner.Table(listed,stage+1,save),field,save,Hand(seed*31+fights*7,true));fights++;again++;
                save.coins+=DadThrowTuner.Pay(fought.Killed,heat);DadThrowTuner.Watched(save,fought.BlockSerial,fought.ShoesThrown);
                if(fought.phase==Cleared){DadThrowTuner.Cleared(save,fought.Health<listed.health);perStage.Append(again).Append(' ');worst=Math.Max(worst,again);stage++;again=0;}
                else DadThrowTuner.Lost(save,listed.id);
                buyers[w](save);if(Owned(save)>had)lastBuy=on;
            }
            r.AppendLine(string.Format(PlainNumbers,"  {0,-15} seed {1}: cleared {2,2} of {3} stages in {4} fights{5} | fights per stage: {6}| most on one stage {7} | eased fights {8}, heated fights {9} | {11} of {12} bought, the last after a fight on {13} | purse {10}",
                ways[w],seed,stage,Reach,fights,stage<Reach?", stuck on "+DadThrowCurve.Id(stage+1)+" ("+again+" tries)":"",perStage,Math.Max(worst,again),easedFights,heatedFights,save.coins,Owned(save),Shelf(),lastBuy>0?DadThrowCurve.Id(lastBuy):"-"));
        }
        return r.ToString();
    }
}
#endif
