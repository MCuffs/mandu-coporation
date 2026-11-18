using System;
using System.Collections.Generic;
using UnityEngine;

// Today's challenge (Design/dad-throw-content-plan-30d-v1.md §5; the owner, 2026-11-18: "a player needs at least
// thirty days of things to do", and the plan "as it is"). From the second day on, the letter of the day holds
// one stage the player has already cleared, played under one or two rule cards, three tries a day.
// A card bends the stage's table or the throws and never needs a picture of its own, so every day is a fight
// the player has not had, made of what the game already has.
// Rules only: no drawing, no input, no files and no clock of its own. The screen says what day it is.
// The numbers here (three tries, what a tier pays, a boss of twenty) are a first guess. Nobody has played them.
public sealed class DadThrowDaily
{
    public enum Card { Dodge, Trays, BallOnly, ShoeFeast, BigBall, Bouncy, OneGate, Reverse, GoldRush, Fog, OneWay, BossIn }
    // A card as the letter shows it.
    public sealed class Face
    {
        public readonly Card card;public readonly string name,line;
        public Face(Card card,string name,string line){this.card=card;this.name=name;this.line=line;}
    }
    // In the order of Card, so a card is its own index.
    public static readonly Face[] Faces={
        new Face(Card.Dodge,"피구 대회","모두가 피구 아저씨처럼 빨리 온다"),
        new Face(Card.Trays,"쟁반 행렬","무리마다 앞줄이 쟁반을 든다"),
        new Face(Card.BallOnly,"공만","신발을 못 쓴다. 공은 두 배 빨리 던진다"),
        new Face(Card.ShoeFeast,"신발 잔치","신발을 쉬지 않고 던진다. 공은 느려진다"),
        new Face(Card.BigBall,"큰 손","공이 두 배 크고 절반만 뚫는다"),
        new Face(Card.Bouncy,"통통","공이 세 번 튄다"),
        new Face(Card.OneGate,"단판 승부","대문을 한 번 내주면 끝난다"),
        new Face(Card.Reverse,"거꾸로","단단한 상대가 맨 앞에 선다"),
        new Face(Card.GoldRush,"돈벼락","사람이 가득 오고 돈이 두 배다"),
        new Face(Card.Fog,"안개","멀리 있는 사람이 잘 안 보인다"),
        new Face(Card.OneWay,"외길","공이 끝까지 뚫는다. 쌍둥이 공은 쉰다"),
        new Face(Card.BossIn,"대장 난입","이긴 대장이 중간에 끼어든다")};
    public static Face Of(Card card){return Faces[(int)card];}
    // The pictures and sounds of today's challenge, as the game loads them (Tools/install_daily_runtime.py copies
    // them in; Design/dad-throw-daily-asset-request-v1.md says what each is). A rule card's picture is named after
    // the card. Chests and TierIcons are in the order of the three tiers: cleared, the gate kept, fifteen with one ball.
    public const string Art="DadThrowDailyV1/",Audio="DadThrowAudioV1/";
    public static string IconKey(Card card){return Art+"rule_"+card.ToString().ToLowerInvariant();}
    public static readonly string[] Chests={"chest_wood","chest_silver","chest_gold"},TierIcons={"tier_clear","tier_gate","tier_chain"};
    public static readonly string[] Pictures={"paper_note","rule_card_frame","rule_chip","envelope_open","icon_letter","badge_alert","stamp_done","icon_check",
        "chance_on","chance_off","fog_veil","fog_puff_0","fog_puff_1","fog_puff_2","mark_ban","fx_chest_coins_0","fx_chest_coins_1","fx_chest_coins_2"};
    public static readonly string[] Sounds={"sfx_envelope","sfx_stamp","sfx_chest_open","sfx_card_deal","sfx_try_spent","jingle_daily_clear"};
    public static int Bit(Card card){return 1<<(int)card;}
    public static bool Has(int cards,Card card){return (cards&Bit(card))!=0;}
    public static int Count(int cards){int n=0;foreach(Face face in Faces)if(Has(cards,face.card))n++;return n;}

    // Tries a day, cards at most, and how many of the latest cleared stages the day's ground is picked from.
    public const int Tries=3,MostCards=2,Recent=6;
    // What the day pays on top of the won for everyone sent back, each once a day: for clearing it, for clearing
    // it with the gate touched once at most, and for a cleared try in which one ball reached ChainGoal people.
    public const int Cleared=1,Guarded=2,Chained=4,ClearWon=100,GateWon=100,ChainWon=150,ChainGoal=15;
    // What the top bar calls the stage while the challenge is fought.
    public const string Name="오늘";
    // Trays to a pack under 쟁반 행렬, the health of the boss who walks in under 대장 난입, where the tough ones
    // stand under 거꾸로, and what the cards do to the throws.
    public const int TrayRow=4,BossInHealth=20,Endless=999;
    public const float ReverseX=600,FeastReload=.15f,FeastBall=3,OnlyBall=.5f,BigScale=2;
    public const int BouncyBounces=3;

    // A day as a number: days since the first day of 2000, so tomorrow is one more. It is kept small on purpose:
    // the save file reads numbers as floats, and a date written as 20261119 would not come back whole.
    public static int Key(DateTime day){return (int)(day.Date-new DateTime(2000,1,1)).TotalDays;}

    readonly IDictionary<string,DadThrowStage.Kind> kinds;readonly string background;
    readonly DadThrowStage.Kind normal,guard,chief,tough;
    public DadThrowDaily(IDictionary<string,DadThrowStage.Kind> kinds,string background)
    {
        if(kinds==null||!kinds.TryGetValue("normal",out normal))throw new ArgumentException("today's challenge needs the normal opponent");
        this.kinds=kinds;this.background=background;
        if(!kinds.TryGetValue("guard",out guard)||!guard.guards)guard=null;
        if(!kinds.TryGetValue("boss",out chief)||chief.boss==null)chief=null;
        if(!kinds.TryGetValue("tough",out tough)||tough==normal)tough=null;
    }

    // The stages a day may be played on: cleared ones that do not end with a boss, the latest `Recent` of them.
    public static List<int> Grounds(DadThrowSave save)
    {
        var list=new List<int>();
        for(int number=1;number<=DadThrowCurve.Stages;number++)
            if(number%DadThrowCurve.PerChapter!=0&&save.cleared.Contains(DadThrowCurve.Id(number)))list.Add(number);
        if(list.Count>Recent)list.RemoveRange(0,list.Count-Recent);
        return list;
    }
    // The letter comes from the day after the first day played, once there is a cleared stage to play it on.
    public static bool Open(DadThrowSave save,int today){return save.firstDay!=0&&today>save.firstDay&&Grounds(save).Count>0;}
    // Whether a card means anything on stage `number` for this player.
    public bool Fits(Card card,int number,DadThrowSave save)
    {
        if(card==Card.Trays)return guard!=null;
        if(card==Card.Reverse)return tough!=null&&DadThrowCurve.Tough(number)>0;
        // Only a boss the player has already sent home walks into a day.
        if(card==Card.BossIn)return chief!=null&&save.cleared.Contains(DadThrowCurve.Id(DadThrowCurve.PerChapter));
        return true;
    }
    // Two cards that must not be dealt together: a tray or a braced boss needs the shoe 공만 takes away, 공만 and
    // 신발 잔치 say opposite things, a boss who touches the gate comes again and would end 단판 승부 by himself,
    // and 큰 손 halves what 외길 makes endless.
    public static bool Clash(Card a,Card b)
    {
        if(a>b){Card swap=a;a=b;b=swap;}
        return a==Card.Trays&&b==Card.BallOnly||a==Card.BallOnly&&(b==Card.ShoeFeast||b==Card.BossIn)||a==Card.OneGate&&b==Card.BossIn||a==Card.BigBall&&b==Card.OneWay;
    }
    static uint Next(uint x){x^=x<<13;x^=x>>17;x^=x<<5;return x;}
    // Deals the day: the ground and the cards come from the date alone (and from what is cleared at that moment),
    // so the day stays the same however often the letter is opened. The very first letter carries one card.
    public void Roll(DadThrowSave save,int today)
    {
        List<int> grounds=Grounds(save);if(grounds.Count==0)throw new InvalidOperationException("no cleared stage to play today's challenge on");
        uint seed=unchecked((uint)today*2654435761u);if(seed==0)seed=1;
        seed=Next(seed);int number=grounds[(int)(seed%(uint)grounds.Count)];
        seed=Next(seed);int want=save.dailyDay==0||seed%3==0?1:MostCards;
        int cards=0;
        for(int tries=0;tries<64&&Count(cards)<want;tries++)
        {
            seed=Next(seed);Card card=(Card)(seed%(uint)Faces.Length);
            if(Has(cards,card)||!Fits(card,number,save))continue;
            bool clash=false;foreach(Face face in Faces)if(Has(cards,face.card)&&Clash(face.card,card))clash=true;
            if(!clash)cards|=Bit(card);
        }
        save.dailyDay=today;save.dailyStage=number;save.dailyCards=cards;save.dailyLeft=Tries;save.dailyWon=0;
    }
    // The screen says what day it is. True when the save changed and wants writing.
    public bool Day(DadThrowSave save,int today)
    {
        bool changed=false;
        // The first day played is remembered. A save that holds cleared stages from before the letter existed
        // is not kept waiting for tomorrow.
        if(save.firstDay==0){save.firstDay=save.cleared.Count>0?1:today;changed=true;}
        if(!Open(save,today)||save.dailyDay==today)return changed;
        Roll(save,today);return true;
    }

    // The table the day is fought on: stage `number` off the curve with the cards worked into it.
    public DadThrowStage Table(int number,int cards)
    {
        float crowd=Has(cards,Card.GoldRush)?(float)DadThrowCurve.MaxPeople/DadThrowCurve.People(number):1,pace=DadThrowCurve.Pace(number);
        DadThrowStage table=DadThrowCurve.Stage(number,kinds,background,crowd);table.id=Name;
        // No shoe, so nothing may need one: the trays are put down.
        if(Has(cards,Card.BallOnly))foreach(DadThrowStage.Group g in table.groups)if(g.kind!=null&&g.kind.guards)g.kind=normal;
        // A row of trays at the front of every ordinary pack big enough to stand behind one.
        if(Has(cards,Card.Trays)&&guard!=null)
            for(int i=0,packs=table.groups.Count;i<packs;i++)
            {
                DadThrowStage.Group g=table.groups[i];if(g.kind!=normal||g.count<=TrayRow*2)continue;
                table.groups.Add(new DadThrowStage.Group{at=g.at,count=TrayRow,firstX=g.firstX,spacing=DadThrowCurve.TraySpacing,speed=g.speed,hp=1,kind=guard});
                g.count-=TrayRow;g.firstX+=TrayRow*DadThrowCurve.TraySpacing;
            }
        if(Has(cards,Card.Dodge))
        {
            float quick=Math.Min(DadThrowCurve.MaxPace,pace+DadThrowCurve.FastLead);
            foreach(DadThrowStage.Group g in table.groups)if(g.kind==null||g.kind.boss==null)g.speed=Math.Max(g.speed,quick);
        }
        // The tough ones are on the street from the first moment, one wall behind the other.
        if(Has(cards,Card.Reverse)&&tough!=null)
        {
            float x=ReverseX;
            foreach(DadThrowStage.Group g in table.groups)if(g.kind==tough){g.at=0;g.firstX=x;x+=g.count*g.spacing;}
        }
        if(Has(cards,Card.OneGate))table.health=1;
        // A boss walks in halfway through. On a full street he takes the place of one of the crowd.
        if(Has(cards,Card.BossIn)&&chief!=null)
        {
            float last=0;DadThrowStage.Group big=null;
            foreach(DadThrowStage.Group g in table.groups){last=Math.Max(last,g.at);if(g.kind==normal&&(big==null||g.count>big.count))big=g;}
            if(table.Total>=DadThrowCurve.MaxPeople&&big!=null)big.count--;
            table.groups.Add(new DadThrowStage.Group{at=last*.5f,count=1,firstX=1300,spacing=0,speed=pace*DadThrowCurve.BossPace,hp=BossInHealth,kind=chief});
        }
        // Arrival order is what the fight walks through; equal times keep the order they were added in.
        for(int i=1;i<table.groups.Count;i++)
            for(int j=i;j>0&&table.groups[j].at<table.groups[j-1].at;j--)
            {DadThrowStage.Group swap=table.groups[j];table.groups[j]=table.groups[j-1];table.groups[j-1]=swap;}
        return table;
    }
    // What the cards do to the throws, on top of everything bought in the shop.
    public static void Bend(DadThrowRules rules,int cards)
    {
        if(Has(cards,Card.BallOnly)){rules.NoShoe=true;rules.GolfReload*=OnlyBall;}
        if(Has(cards,Card.ShoeFeast)){rules.ShoeReload=FeastReload;rules.GolfReload*=FeastBall;}
        if(Has(cards,Card.BigBall)){rules.BallScale=BigScale;rules.Pierce=Math.Max(1,rules.Pierce/2);}
        if(Has(cards,Card.Bouncy))rules.MaxBounces=Math.Max(rules.MaxBounces,BouncyBounces);
        if(Has(cards,Card.OneWay)){rules.Pierce=Endless;rules.GolfCount=1;}
    }
    // How many times over the day pays for each one sent back.
    public static int PayTimes(int cards){return Has(cards,Card.GoldRush)?2:1;}
    // What a cleared try reached: `touched` is how often the gate was touched, `chain` the most one ball reached.
    public static int Tiers(int touched,int chain){return Cleared|(touched<=1?Guarded:0)|(chain>=ChainGoal?Chained:0);}
    public static int Worth(int tiers){return ((tiers&Cleared)!=0?ClearWon:0)+((tiers&Guarded)!=0?GateWon:0)+((tiers&Chained)!=0?ChainWon:0);}
}
