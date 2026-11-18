#if UNITY_EDITOR || SLING_HEADLESS
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Window-free checks for today's challenge (DadThrowDaily): when the letter comes, what a day holds, what each
// card does to the table and the throws, and how a try runs through the flow and the save.
// The last part plays a bot through thirty days. A bot is arithmetic, not a person: it shows which days the
// numbers allow to be cleared, never whether a day is fun.
public static class DadThrowDailyChecks
{
    const DadThrowModel.Weapon Golf=DadThrowModel.Weapon.Golf,Shoe=DadThrowModel.Weapon.Shoe;
    const DadThrowFlow.Screen OnTitle=DadThrowFlow.Screen.Title,OnFight=DadThrowFlow.Screen.Playing,OnClear=DadThrowFlow.Screen.Clear,OnDefeat=DadThrowFlow.Screen.Defeat,OnLetter=DadThrowFlow.Screen.Daily;
    static readonly Vector2 Flat=new Vector2(150,50);
    static int Bits(params DadThrowDaily.Card[] cards){int bits=0;foreach(DadThrowDaily.Card card in cards)bits|=DadThrowDaily.Bit(card);return bits;}
    // A save that has cleared stages 1 to `through`.
    static DadThrowSave Veteran(int through)
    {var save=new DadThrowSave{introSeen=true,furthest=through};for(int n=1;n<=through;n++)save.cleared.Add(DadThrowCurve.Id(n));return save;}
    static int People(DadThrowStage table,Predicate<DadThrowStage.Group> which){int n=0;foreach(DadThrowStage.Group g in table.groups)if(which(g))n+=g.count;return n;}
    // A letter set by hand, so a check knows what it is fighting.
    static DadThrowSave Dealt(int through,int day,int stage,int cards)
    {DadThrowSave save=Veteran(through);save.firstDay=1;save.dailyDay=day;save.dailyStage=stage;save.dailyCards=cards;save.dailyLeft=DadThrowDaily.Tries;return save;}

    public static string Run(DadThrowLayout layout,Action<bool,string> check)
    {
        var r=new StringBuilder();DadThrowField field=layout.Field;
        DadThrowStageSet set=DadThrowStageSet.Parse(System.IO.File.ReadAllText("Assets/DadSchoolBoss/Resources/DadThrowStages/chapter1.json"),layout.Backgrounds,field.Gate);
        string street=set.stages[0].backgroundKey;
        List<DadThrowStage> game=DadThrowCurve.Build(set.kinds,street,set.stages);
        var daily=new DadThrowDaily(set.kinds,street);var tuner=new DadThrowTuner(set.kinds,street);
        DadThrowStage.Kind normal=set.kinds["normal"],guard=set.kinds["guard"],tough=set.kinds["tough"],boss=set.kinds["boss"];
        const int Tries=DadThrowDaily.Tries,Ceiling=DadThrowCurve.MaxPeople;

        // ── the day ──
        int d1=DadThrowDaily.Key(new DateTime(2026,11,18,23,59,0)),d2=DadThrowDaily.Key(new DateTime(2026,11,19,0,1,0));
        check(d2==d1+1&&DadThrowDaily.Key(new DateTime(2027,1,1))==DadThrowDaily.Key(new DateTime(2026,12,31))+1&&DadThrowDaily.Key(new DateTime(2026,12,1))==DadThrowDaily.Key(new DateTime(2026,11,30))+1&&d1>0&&d1<100000,
            "a day is a small number and tomorrow is one more, across the end of a month and of a year");

        // ── when the letter comes ──
        var fresh=new DadThrowSave();bool wrote=daily.Day(fresh,d1);
        check(wrote&&fresh.firstDay==d1&&!DadThrowDaily.Open(fresh,d1)&&fresh.dailyDay==0,"the first day played is remembered and brings no letter");
        fresh.cleared.Add("1-1");fresh.cleared.Add("1-2");
        check(!daily.Day(fresh,d1)&&fresh.dailyDay==0,"clearing stages on that first day still brings none");
        check(daily.Day(fresh,d2)&&fresh.dailyDay==d2&&fresh.dailyLeft==Tries&&fresh.dailyWon==0&&(fresh.dailyStage==1||fresh.dailyStage==2)&&DadThrowDaily.Count(fresh.dailyCards)==1,
            "the next day the letter is there: a stage already cleared, three tries, and one card on the very first one");
        int ground=fresh.dailyStage,dealt=fresh.dailyCards;fresh.cleared.Add("1-3");fresh.cleared.Add("1-4");fresh.dailyLeft=1;
        check(!daily.Day(fresh,d2)&&fresh.dailyStage==ground&&fresh.dailyCards==dealt&&fresh.dailyLeft==1,"the day's letter stays what it was dealt as, whatever is cleared afterwards");
        check(daily.Day(fresh,d2+1)&&fresh.dailyDay==d2+1&&fresh.dailyLeft==Tries&&fresh.dailyWon==0,"a new day deals a new one with all its tries");
        var idle=new DadThrowSave();daily.Day(idle,d1);
        check(!daily.Day(idle,d2)&&idle.dailyDay==0&&!DadThrowDaily.Open(idle,d2),"with nothing cleared there is no letter, on the second day or any other");
        var old=Veteran(9);
        check(daily.Day(old,d1)&&old.firstDay==1&&old.dailyDay==d1,"a save that already holds cleared stages gets its first letter today, not tomorrow");
        var bossOnly=new DadThrowSave{firstDay=1};bossOnly.cleared.Add(DadThrowCurve.Id(DadThrowCurve.PerChapter));
        check(DadThrowDaily.Grounds(bossOnly).Count==0&&!daily.Day(bossOnly,d1),"a stage that ends with a boss is never the day's ground");

        // ── sixty days in a row ──
        var steady=Veteran(9);var seenCards=new HashSet<DadThrowDaily.Card>();var seenGrounds=new HashSet<int>();bool sound=true;int singles=0;
        for(int day=d1;day<d1+60;day++)
        {
            daily.Day(steady,day);int number=steady.dailyStage,cards=steady.dailyCards,count=DadThrowDaily.Count(cards);
            // Cleared without a boss: 1 2 3 4 6 7 8 9, of which the latest six are 3 to 9.
            sound&=steady.dailyDay==day&&number>=3&&number<=9&&number%DadThrowCurve.PerChapter!=0&&count>=1&&count<=DadThrowDaily.MostCards;
            foreach(DadThrowDaily.Face a in DadThrowDaily.Faces)
            {
                if(!DadThrowDaily.Has(cards,a.card))continue;
                seenCards.Add(a.card);sound&=daily.Fits(a.card,number,steady);
                foreach(DadThrowDaily.Face b in DadThrowDaily.Faces)if(a.card!=b.card&&DadThrowDaily.Has(cards,b.card))sound&=!DadThrowDaily.Clash(a.card,b.card);
            }
            if(count==1)singles++;seenGrounds.Add(number);
            DadThrowStage table=daily.Table(number,cards);
            sound&=table.Total<=Ceiling&&table.Total>=DadThrowCurve.PackSize&&table.id==DadThrowDaily.Name&&table.health>=1;
            for(int i=0;i<table.groups.Count;i++){DadThrowStage.Group g=table.groups[i];sound&=g.count>0&&g.kind!=null&&g.firstX>field.Gate&&(i==0||g.at>=table.groups[i-1].at);}
        }
        check(sound,"sixty days in a row: a cleared stage without a boss from the latest six, one or two cards that fit the stage and each other, and a table that is sorted, starts beyond the gate and keeps to the street's ceiling");
        check(seenCards.Count>=10&&seenGrounds.Count>=4&&singles>0&&singles<60,"over those days at least ten of the twelve cards and four different stages turn up, some days with one card and some with two");
        var twice=Veteran(9);daily.Day(twice,d1+7);var again=Veteran(9);daily.Day(again,d1+7);
        check(twice.dailyStage==again.dailyStage&&twice.dailyCards==again.dailyCards,"the same day deals the same letter to the same progress");

        // ── what each card does (stage 2-3: quick, tough and tray-holding ones are all on it) ──
        const int n=8;DadThrowStage plain=DadThrowCurve.Stage(n,set.kinds,street);
        Func<DadThrowDaily.Card,DadThrowStage> with=card=>daily.Table(n,DadThrowDaily.Bit(card));
        Func<DadThrowDaily.Card,DadThrowRules> bent=card=>{var rules=new DadThrowRules{GolfCount=3};DadThrowDaily.Bend(rules,DadThrowDaily.Bit(card));return rules;};
        DadThrowStage t=with(DadThrowDaily.Card.Dodge);float quick=DadThrowCurve.Pace(n)+DadThrowCurve.FastLead;bool all=true;
        foreach(DadThrowStage.Group g in t.groups)all&=g.speed>=quick-.01f;
        check(all&&t.Total==plain.Total&&People(plain,g=>g.speed<quick-.01f)>0,"피구 대회: everyone comes at the quick ones' pace, and nobody is added");
        t=with(DadThrowDaily.Card.Trays);int packs=0;foreach(DadThrowStage.Group g in plain.groups)if(g.kind==normal&&g.count>DadThrowDaily.TrayRow*2)packs++;
        check(packs>=3&&People(t,g=>g.kind==guard)==People(plain,g=>g.kind==guard)+packs*DadThrowDaily.TrayRow&&t.Total==plain.Total,"쟁반 행렬: every ordinary pack gets a row of four trays in front, taken out of its own number");
        t=with(DadThrowDaily.Card.BallOnly);DadThrowRules only=bent(DadThrowDaily.Card.BallOnly);var noShoe=new DadThrowModel(field,t){Rules=only};
        check(People(plain,g=>g.kind==guard)>0&&People(t,g=>g.kind!=null&&g.kind.guards)==0&&t.Total==plain.Total&&only.NoShoe&&Mathf.Approximately(only.GolfReload,new DadThrowRules().GolfReload*.5f)&&!noShoe.Fire(Shoe,Flat)&&noShoe.Fire(Golf,Flat),
            "공만: the trays are put down, no shoe leaves the hand, and the ball is ready in half the time");
        DadThrowRules feast=bent(DadThrowDaily.Card.ShoeFeast);
        check(feast.ShoeReload==DadThrowDaily.FeastReload&&Mathf.Approximately(feast.GolfReload,new DadThrowRules().GolfReload*3)&&!feast.NoShoe,"신발 잔치: shoes follow each other at once and the ball takes three times as long");
        DadThrowRules wide=bent(DadThrowDaily.Card.BigBall);var bigBall=new DadThrowModel(field,plain){Rules=wide};var usual=new DadThrowModel(field,plain);
        check(Mathf.Approximately(bigBall.BallRadius,usual.BallRadius*2)&&wide.Pierce==new DadThrowRules().Pierce/2,"큰 손: the ball is twice as wide to the hit test and goes through half as many");
        DadThrowRules bouncy=bent(DadThrowDaily.Card.Bouncy),oneWay=bent(DadThrowDaily.Card.OneWay);
        check(bouncy.MaxBounces==DadThrowDaily.BouncyBounces&&bouncy.GolfCount==3&&oneWay.Pierce>=Ceiling&&oneWay.GolfCount==1,"통통: the ball bounces three times. 외길: it goes through the whole street, and the twin balls rest");
        t=with(DadThrowDaily.Card.OneGate);
        check(t.health==1&&plain.health==DadThrowCurve.MaxHealth&&new DadThrowModel(field,t).Health==1,"단판 승부: one touch on the gate ends it");
        t=with(DadThrowDaily.Card.Reverse);bool front=true;foreach(DadThrowStage.Group g in t.groups)if(g.kind==tough)front&=g.at==0&&g.firstX>=DadThrowDaily.ReverseX;
        check(front&&People(t,g=>g.kind==tough)==People(plain,g=>g.kind==tough)&&People(plain,g=>g.kind==tough)>0&&People(plain,g=>g.kind==tough&&g.at==0)==0&&t.Total==plain.Total,
            "거꾸로: the tough ones are on the street from the first moment, as many as the stage has");
        t=with(DadThrowDaily.Card.GoldRush);
        check(t.Total==Ceiling&&plain.Total<Ceiling&&DadThrowDaily.PayTimes(Bits(DadThrowDaily.Card.GoldRush))==2&&DadThrowDaily.PayTimes(0)==1,"돈벼락: the street is as full as it gets and everyone sent back pays twice");
        t=with(DadThrowDaily.Card.BossIn);DadThrowStage.Group him=t.groups.Find(g=>g.kind==boss);float lastAt=0;foreach(DadThrowStage.Group g in t.groups)lastAt=Math.Max(lastAt,g.at);
        check(People(t,g=>g.kind==boss)==1&&People(plain,g=>g.kind==boss)==0&&him.hp==DadThrowDaily.BossInHealth&&him.at>0&&him.at<lastAt&&t.Total==plain.Total+1,"대장 난입: one boss walks in halfway through, on top of the stage's own people");
        check(daily.Table(15,Bits(DadThrowDaily.Card.BossIn)).Total==Ceiling&&DadThrowCurve.Stage(15,set.kinds,street).Total==Ceiling,"on a street that is already full he takes the place of one of the crowd");
        t=with(DadThrowDaily.Card.Fog);DadThrowRules fog=bent(DadThrowDaily.Card.Fog);
        check(t.Total==plain.Total&&t.groups.Count==plain.groups.Count&&fog.Pierce==new DadThrowRules().Pierce&&!fog.NoShoe&&fog.BallScale==1,"안개 leaves the table and the throws alone: it is the screen that hides the far end of the street");
        check(DadThrowDaily.Clash(DadThrowDaily.Card.Trays,DadThrowDaily.Card.BallOnly)&&DadThrowDaily.Clash(DadThrowDaily.Card.BallOnly,DadThrowDaily.Card.Trays)&&DadThrowDaily.Clash(DadThrowDaily.Card.BossIn,DadThrowDaily.Card.BallOnly)
            &&DadThrowDaily.Clash(DadThrowDaily.Card.ShoeFeast,DadThrowDaily.Card.BallOnly)&&DadThrowDaily.Clash(DadThrowDaily.Card.BossIn,DadThrowDaily.Card.OneGate)&&DadThrowDaily.Clash(DadThrowDaily.Card.OneWay,DadThrowDaily.Card.BigBall)
            &&!DadThrowDaily.Clash(DadThrowDaily.Card.Dodge,DadThrowDaily.Card.Trays)&&!DadThrowDaily.Clash(DadThrowDaily.Card.Fog,DadThrowDaily.Card.OneGate),
            "cards that undo each other are never dealt together: no shoe against trays or a boss, a boss against a single touch, half against endless");
        check(!daily.Fits(DadThrowDaily.Card.Reverse,2,Veteran(2))&&daily.Fits(DadThrowDaily.Card.Reverse,3,Veteran(3))&&!daily.Fits(DadThrowDaily.Card.BossIn,2,Veteran(4))&&daily.Fits(DadThrowDaily.Card.BossIn,2,Veteran(5)),
            "거꾸로 needs a stage with tough ones on it, and 대장 난입 a boss the player has already sent home");
        check(DadThrowDaily.Tiers(0,20)==7&&DadThrowDaily.Tiers(1,3)==3&&DadThrowDaily.Tiers(2,DadThrowDaily.ChainGoal)==5&&DadThrowDaily.Tiers(4,0)==1&&DadThrowDaily.Worth(7)==350&&DadThrowDaily.Worth(1)==100&&DadThrowDaily.Worth(0)==0,
            "a cleared try reaches up to three tiers: cleared, the gate touched once at most, fifteen with one ball; each pays its own won");

        // ── a try through the flow ──
        var save=Veteran(4);var flow=new DadThrowFlow(game,field,null,save,null,tuner,daily);
        flow.Day(d1);
        check(!flow.DailyReady&&!flow.OpenDaily()&&save.dailyDay==0,"behind the logo no letter is dealt or opened");
        flow.Tick(2);flow.Day(d1);
        check(flow.Now==OnTitle&&flow.DailyReady&&save.dailyDay==d1&&save.dailyLeft==Tries,"on the title today's letter is dealt");
        save.dailyStage=1;save.dailyCards=Bits(DadThrowDaily.Card.Bouncy);
        check(!flow.StartDaily()&&flow.OpenDaily()&&flow.Now==OnLetter&&!flow.Start()&&!flow.OpenShop()&&!flow.OpenDaily(),"the letter opens from the title; a try starts only from the letter, and nothing else starts from inside it");
        check(flow.Back()&&flow.Now==OnTitle&&flow.OpenDaily(),"back closes the letter onto the title");
        int furthest=flow.Furthest;
        check(flow.StartDaily()&&flow.Now==OnFight&&flow.InDaily&&flow.Stage.id==DadThrowDaily.Name&&save.dailyLeft==Tries-1&&flow.Model.Rules.MaxBounces==DadThrowDaily.BouncyBounces&&flow.Model.Total==DadThrowCurve.People(1)&&flow.Ease==0&&flow.Heat==0,
            "a try is spent the moment it starts, and is fought on the day's stage with the card in the throws");
        for(int i=0;i<120*120&&flow.Now==OnFight;i++)flow.Tick(1/120f);
        check(flow.Now==OnDefeat&&!flow.Retry()&&!flow.Next()&&!flow.HasNext&&save.defeats==0&&save.tunedStage==null&&flow.Furthest==furthest&&save.coins==0,
            "a lost try cannot be tried again from its card, and neither the tuner nor the stages opened hear of it");
        check(flow.OpenShop()&&flow.CloseShop()&&flow.Now==OnDefeat&&flow.ToDaily()&&flow.Now==OnLetter&&!flow.InDaily&&flow.Model==null&&!flow.ToDaily(),"the shop opens from the result and closes back onto it; the card leads back to the letter");
        flow.StartDaily();var bot=new DadThrowBots.Run();
        for(int i=0;i<120*120&&flow.Now==OnFight;i++){DadThrowBots.Spam(flow.Model,bot);flow.Tick(1/120f);}
        int sent=flow.Model.Killed,tiers=flow.DailyTiers,paid=flow.DailyPaid;
        check(flow.Now==OnClear&&(tiers&DadThrowDaily.Cleared)!=0&&paid==DadThrowDaily.Worth(tiers)&&paid>=DadThrowDaily.ClearWon&&save.dailyWon==tiers&&save.coins==sent+paid&&flow.Coins==sent+paid&&flow.Earned==sent,
            "a cleared try pays the tiers it reached on top of the won for everyone sent back");
        check(save.cleared.Count==4&&!save.cleared.Contains(DadThrowDaily.Name)&&flow.Furthest==furthest&&save.furthest==4&&save.flawless==0&&!flow.Retry()&&!flow.Next(),"and opens no stage, counts for no streak, and offers no next stage");
        flow.ToDaily();flow.StartDaily();bot=new DadThrowBots.Run();int purse=save.coins;
        for(int i=0;i<120*120&&flow.Now==OnFight;i++){DadThrowBots.Spam(flow.Model,bot);flow.Tick(1/120f);}
        check(flow.Now==OnClear&&flow.DailyPaid==DadThrowDaily.Worth(flow.DailyTiers&~tiers)&&((flow.DailyTiers&~tiers)!=0||flow.DailyPaid==0)&&save.coins==purse+flow.Model.Killed+flow.DailyPaid&&save.dailyWon==(tiers|flow.DailyTiers),
            "a tier already paid today is not paid again; a second clear pays only what the first did not reach");
        flow.ToDaily();
        check(save.dailyLeft==0&&!flow.StartDaily()&&flow.Now==OnLetter,"after three tries the letter starts nothing more today");
        flow.CloseDaily();flow.Day(d1+1);
        check(save.dailyDay==d1+1&&save.dailyLeft==Tries&&save.dailyWon==0&&flow.DailyReady,"the next day on the title there is a new letter with all its tries");
        check(flow.Start()&&flow.Stage.id=="1-5"&&!flow.InDaily,"and the stages go on from where they were");

        DadThrowSave rich=Dealt(4,d1,1,Bits(DadThrowDaily.Card.GoldRush));var gold=new DadThrowFlow(game,field,null,rich,null,tuner,daily);
        gold.Tick(2);gold.Day(d1);gold.OpenDaily();gold.StartDaily();bot=new DadThrowBots.Run();
        for(int i=0;i<120*3&&gold.Now==OnFight;i++){DadThrowBots.Spam(gold.Model,bot);gold.Tick(1/120f);}
        int felled=gold.Model.Killed;
        check(rich.dailyCards==Bits(DadThrowDaily.Card.GoldRush)&&gold.Model.Total==Ceiling&&felled>0&&gold.Earned==felled*2&&gold.Coins==felled*2,"돈벼락 in a fight: a full street, and the purse goes up by two for each one sent back");
        check(gold.Pause()&&gold.ToTitle()&&rich.coins==felled*2&&!gold.InDaily&&rich.dailyLeft==Tries-1&&gold.OpenDaily(),"leaving a try keeps what it earned and does not give the try back");

        DadThrowSave shy=Dealt(2,d1,2,Bits(DadThrowDaily.Card.Trays));shy.met.Add("fast");var meets=new DadThrowFlow(game,field,null,shy,null,tuner,daily);
        meets.Tick(2);meets.Day(d1);meets.OpenDaily();
        check(guard.entrance!=null&&meets.StartDaily()&&meets.Entering==guard,"a card can bring on someone the player has not met, and he makes his entrance as he would on a stage");
        meets.Tick(DadThrowFlow.EnterTime+.1f);
        check(meets.Entering==null&&shy.met.Contains("guard"),"and is remembered as met");

        // ── the save file ──
        var far=new DadThrowSave{introSeen=true,firstDay=d1-1,dailyDay=d1+1,dailyStage=3,dailyCards=(1<<DadThrowDaily.Faces.Length)-1,dailyLeft=2,dailyWon=5};DadThrowSave back=DadThrowSave.Parse(far.ToJson());
        check(back.firstDay==d1-1&&back.dailyDay==d1+1&&back.dailyStage==3&&back.dailyCards==far.dailyCards&&back.dailyLeft==2&&back.dailyWon==5,"the letter comes back from the save file whole: the days, the stage, the cards, the tries left and what was paid");
        DadThrowSave before=DadThrowSave.Parse("{\"schemaVersion\":1,\"introSeen\":true,\"furthest\":1,\"cleared\":[\"1-1\"]}");
        check(before.firstDay==0&&before.dailyDay==0&&before.dailyLeft==0&&before.dailyCards==0,"a save written before the letter existed reads as never having had one");
        DadThrowSave odd=DadThrowSave.Parse("{\"schemaVersion\":1,\"introSeen\":true,\"furthest\":1,\"cleared\":[\"1-1\"],\"daily\":{\"first\":3,\"day\":9000,\"stage\":0,\"cards\":99999,\"left\":40,\"won\":64}}");
        check(odd.dailyDay==0&&odd.dailyLeft==Tries&&odd.dailyWon==0&&odd.dailyCards<(1<<DadThrowDaily.Faces.Length),"a letter the file holds without a stage, or with more tries and cards than exist, is held to what the game has");

        // ── where the letter stands on the screen ──
        Rect letter=DadThrowLayout.DailyCard;bool inside=true,apart=true;
        Rect[] parts={DadThrowLayout.DailyHeading,DadThrowLayout.DailyStage,DadThrowLayout.DailyRule(0),DadThrowLayout.DailyRule(1),DadThrowLayout.DailyTier(0),DadThrowLayout.DailyTier(1),DadThrowLayout.DailyTier(2),
            DadThrowLayout.DailyTries,DadThrowLayout.DailyGo,DadThrowLayout.DailyClose};
        foreach(Rect part in parts)inside&=letter.xMin<=part.xMin&&part.xMax<=letter.xMax&&letter.yMin<=part.yMin&&part.yMax<=letter.yMax;
        for(int i=0;i<parts.Length;i++)for(int j=i+1;j<parts.Length;j++)apart&=!parts[i].Overlaps(parts[j]);
        for(int i=0;i<Tries;i++){Rect cell=DadThrowLayout.DailyTry(i);inside&=DadThrowLayout.DailyTries.xMin<=cell.xMin&&cell.xMax<=DadThrowLayout.DailyTries.xMax&&DadThrowLayout.DailyTries.yMin<=cell.yMin&&cell.yMax<=DadThrowLayout.DailyTries.yMax;}
        check(inside&&apart,"the letter's heading, two cards, three tiers, tries and two buttons sit inside it and none covers another");
        Rect whole=new Rect(0,0,DadThrowLayout.W,DadThrowLayout.H);
        check(!DadThrowLayout.DailyButton.Overlaps(DadThrowLayout.StartButton)&&!DadThrowLayout.DailyButton.Overlaps(DadThrowLayout.ShopButton)&&!DadThrowLayout.DailyButton.Overlaps(DadThrowLayout.ShopPurse)
            &&whole.xMin<=DadThrowLayout.DailyButton.xMin&&DadThrowLayout.DailyButton.yMax<=whole.yMax&&DadThrowLayout.DailyButton.height>=48,
            "on the title the letter's button stands clear of start, the shop and the purse, and is tall enough for a thumb");
        check(DadThrowLayout.DailyChips.yMin>=DadThrowLayout.TopBar&&!DadThrowLayout.DailyChips.Overlaps(DadThrowLayout.BossName)&&!DadThrowLayout.DailyChips.Overlaps(DadThrowLayout.BossBar)&&!DadThrowLayout.DailyChips.Overlaps(DadThrowLayout.Hint)
            &&DadThrowLayout.FogFrom>DadThrowLayout.W/2&&DadThrowLayout.FogFrom<DadThrowLayout.W,
            "in a fight the day's cards are named under the top bar, clear of a boss's name and the hint; the fog covers only the far half of the street");

        // ── thirty days with a bot ──
        r.Append(Days(daily,field,d1));
        return r.ToString();
    }

    // A careful stand-in (aims well two times in three, throws a shoe at a tray) plays thirty days in a row with
    // about what a player owns around 2-2, three tries a day.
    public static string Days(DadThrowDaily daily,DadThrowField field,int from)
    {
        var r=new StringBuilder();DadThrowSave player=Veteran(7);player.firstDay=1;
        player.upgrades[(int)DadThrowShop.Item.Pierce]=2;player.upgrades[(int)DadThrowShop.Item.Reload]=1;player.upgrades[(int)DadThrowShop.Item.Blast]=1;player.upgrades[(int)DadThrowShop.Item.Wide]=1;
        int cleared=0,first=0;var dealt=new int[DadThrowDaily.Faces.Length];var lost=new int[DadThrowDaily.Faces.Length];var line=new StringBuilder();
        for(int day=0;day<30;day++)
        {
            daily.Day(player,from+day);int tries=0;bool won=false;
            while(tries<DadThrowDaily.Tries&&!won)
            {
                DadThrowRules rules=DadThrowShop.Rules(player);DadThrowDaily.Bend(rules,player.dailyCards);
                var fight=new DadThrowModel(field,daily.Table(player.dailyStage,player.dailyCards)){Rules=rules};
                DadThrowBots.Policy hand=DadThrowGrowthChecks.Hand(1000+day*10+tries,true);var run=new DadThrowBots.Run();
                for(int i=0;i<120*180&&fight.phase==DadThrowModel.Phase.Playing;i++){hand(fight,run);fight.Tick(1/120f);}
                won=fight.phase==DadThrowModel.Phase.WaveClear;tries++;
            }
            if(won){cleared++;if(tries==1)first++;}
            foreach(DadThrowDaily.Face face in DadThrowDaily.Faces)if(DadThrowDaily.Has(player.dailyCards,face.card)){dealt[(int)face.card]++;if(!won)lost[(int)face.card]++;}
            line.Append(won?tries.ToString():"x");
        }
        r.AppendLine("DAILY bot (aims well 2 in 3, shoe at trays, owns Pierce 2 Reload 1 Blast 1 Wide 1, cleared to 2-2): thirty days, three tries a day");
        r.AppendLine("DAILY   cleared "+cleared+" of 30 days, "+first+" at the first try | tries per day (x = not cleared): "+line);
        var cards=new StringBuilder();
        foreach(DadThrowDaily.Face face in DadThrowDaily.Faces)cards.Append(face.card).Append(' ').Append(dealt[(int)face.card]-lost[(int)face.card]).Append('/').Append(dealt[(int)face.card]).Append("  ");
        r.AppendLine("DAILY   days cleared / days dealt, by card: "+cards);
        return r.ToString();
    }
}
#endif
