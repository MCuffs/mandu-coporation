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
            if(shoes&&aimed&&lead.guard)
            {
                DadThrowBots.Aim shoe=DadThrowBots.BestShoe(m,lead);
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
        return r.ToString();
    }

    // ── a bot through the stages and the shop ──
    static readonly CultureInfo PlainNumbers=CultureInfo.InvariantCulture;
    public const int Reach=20,Tries=36,Seeds=2;
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
                DadThrowCurve.Id(number),DadThrowCurve.People(number),DadThrowCurve.Tough(number),DadThrowCurve.ToughHealth(number),DadThrowCurve.Fast(number),DadThrowCurve.Guards(number),DadThrowCurve.Pace(number),DadThrowCurve.Gap(number)));
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
                var save=new DadThrowSave();int stage=0,fights=0,again=0;var perStage=new StringBuilder();
                while(stage<Reach&&fights<Tries)
                {
                    DadThrowModel fought=Fight(game[stage],field,save,Hand(seed*31+fights*7+p,p==0));fights++;again++;
                    save.coins+=fought.Killed*DadThrowShop.WonPerFelled;buyers[w](save);
                    if(fought.phase==Cleared){perStage.Append(again).Append(' ');stage++;again=0;}
                }
                r.AppendLine(string.Format(PlainNumbers,"  {0,-15} seed {1}: cleared {2,2} of {3} stages in {4} fights{5} | fights per stage: {6}| bought: {7} | purse {8}",
                    ways[w],seed,stage,Reach,fights,stage<Reach?", stuck on "+DadThrowCurve.Id(stage+1)+" ("+again+" tries)":"",perStage,Bought(save),save.coins));
            }
        }
        return r.ToString();
    }
}
#endif
