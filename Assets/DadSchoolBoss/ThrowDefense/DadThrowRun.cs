using System;
using System.Collections.Generic;
using UnityEngine;

// 연속전: one long fight instead of separate stages (2026-11-17, after the owner played the stampede and said
// "not bad, but this is about a Flash game"). Packs keep coming wave after wave, the gate does not get its
// touches back, and after every wave the player takes one of three offers that change how the ball or the shoe
// works. What is being tested: does the fight itself give something to choose, so that two runs do not play the same.
// Rules only: no drawing, no input, no files. The numbers are a first guess measured with bots
// (Design/dad-throw-run-v1.md), not with people.
public sealed class DadThrowRun
{
    public enum Pick { Pierce, Reload, Twin, Heavy, Bounce, Blast, Wide, Sticky, Flow, Repair }
    // One offer as the card shows it. `most`: how many times it may be taken. `shoe`: the card shows the shoe.
    public sealed class Card
    {
        public readonly Pick pick;public readonly string name,line;public readonly int most;public readonly bool shoe;
        public Card(Pick pick,string name,string line,int most,bool shoe){this.pick=pick;this.name=name;this.line=line;this.most=most;this.shoe=shoe;}
    }
    // In the order of Pick, so a pick is its own index.
    public static readonly Card[] Cards={
        new Card(Pick.Pierce,"묵직한 공","한 공이 여섯 명을\n더 뚫고 간다",3,false),
        new Card(Pick.Reload,"빠른 손","공을 더 빨리\n다시 던진다",3,false),
        new Card(Pick.Twin,"쌍둥이 공","공이 하나 더\n같이 나간다",2,false),
        new Card(Pick.Heavy,"돌덩이 공","공이 더 세게 때린다\n단단한 상대에게 좋다",2,false),
        new Card(Pick.Bounce,"통통 공","공이 땅에서\n한 번 더 튄다",2,false),
        new Card(Pick.Blast,"폭탄 신발","신발이 모은 무리를\n쓰러뜨린다",3,true),
        new Card(Pick.Wide,"왕발 신발","신발이 더 넓게\n밀어낸다",2,true),
        new Card(Pick.Sticky,"끈끈이 신발","맞은 무리가\n더 오래 느려진다",2,true),
        new Card(Pick.Flow,"신바람","한 공에 열 명을 맞히면\n바로 또 던진다",1,false),
        new Card(Pick.Repair,"대문 수리","대문이 한 번\n더 버틴다",int.MaxValue,false)};
    public static Card Of(Pick pick){return Cards[(int)pick];}

    public const int MaxHealth=DadThrowStageSet.MaxHealth,MaxPeople=DadThrowStageSet.MaxTotal,Offered=3;
    // What each pick does to the throws. Repair is the one pick that is not a rule: it gives the gate a touch back.
    public const int PierceStep=6,FlowChain=10;public const float ReloadStep=.8f,WideStep=1.35f,StickyTime=2,StickyStep=.7f;
    public static void Apply(DadThrowRules rules,Pick pick)
    {
        if(pick==Pick.Pierce)rules.Pierce+=PierceStep;
        else if(pick==Pick.Reload)rules.GolfReload*=ReloadStep;
        else if(pick==Pick.Twin)rules.GolfCount++;
        else if(pick==Pick.Heavy)rules.GolfDamage++;
        else if(pick==Pick.Bounce)rules.MaxBounces++;
        else if(pick==Pick.Blast)rules.ShoeBlast++;
        else if(pick==Pick.Wide)rules.ShoeRadius*=WideStep;
        else if(pick==Pick.Sticky){rules.SlowTime+=StickyTime;rules.SlowFactor*=StickyStep;}
        else if(pick==Pick.Flow)rules.ChainReload=FlowChain;
    }

    // ── how a wave grows. Wave 1 is lighter than stage 1-1 (60 people against its 100); by wave 8 the street
    // holds as many as the game allows at once, and from there pace, spacing and the tough ones go on growing.
    // First numbers (60+14, pace +9, tough 4+2 with 3 to 5 health) let a bot with an unsteady hand hold all twelve
    // waves on ball picks alone (Design/dad-throw-run-report-first-numbers.txt), so the growth was made steeper
    // and the tough ones' health has no ceiling: that is what ends a run. ──
    public static int People(int wave){return Math.Min(MaxPeople,60+16*(wave-1));}
    public static float Pace(int wave){return Math.Min(MaxPace,112+10*(wave-1));}
    public const float MaxPace=250;
    // Seconds between packs.
    public static float Gap(int wave){return Math.Max(.8f,1.6f-.09f*(wave-1));}
    // Chair dads: slow, and one ball does not put them down. They are what a plain "throw faster" build stalls on.
    public static int Tough(int wave){return wave<3?0:Math.Min(30,4+3*(wave-3));}
    public static int ToughHealth(int wave){return wave<3?3:3+(wave-3)/2;}
    // Dodge-ball dads: a small pack that outruns the pack in front of it.
    public static int Fast(int wave){return wave<2?0:Math.Min(30,6+3*(wave-2));}
    public const int PackSize=14,FastPack=6;public const float FastLead=70,ToughPace=.55f;

    // The wave being fought, counted from 1, and the touches the gate has left for the rest of the run.
    public int Wave {get;private set;} public int Health {get;private set;}
    // People knocked down in the waves that are over (the wave that ended the run included).
    public int Felled {get;private set;}
    public bool Over {get;private set;}
    public readonly DadThrowRules Rules=new DadThrowRules();
    public DadThrowStage Stage {get;private set;}
    // What may be taken now: up to three, empty while a wave is being fought.
    public readonly List<Pick> Offers=new List<Pick>(Offered);
    public readonly int Seed;
    readonly int[] levels=new int[Cards.Length];readonly System.Random random;
    readonly IDictionary<string,DadThrowStage.Kind> kinds;readonly DadThrowStage.Kind normal,tough,fast;readonly string background;

    // kinds: the opponents the stage file declares. "normal" is needed; "tough" and "fast" fall back to it, so a
    // file without them still gives a run (with one face). background: the street every wave is fought on.
    public DadThrowRun(IDictionary<string,DadThrowStage.Kind> kinds,string background,int seed)
    {
        if(kinds==null||!kinds.TryGetValue("normal",out normal))throw new ArgumentException("a run needs the normal opponent");
        if(!kinds.TryGetValue("tough",out tough))tough=normal;if(!kinds.TryGetValue("fast",out fast))fast=normal;
        this.kinds=kinds;this.background=background;Seed=seed;random=new System.Random(seed);
        Health=MaxHealth;Wave=1;Stage=Build(1);
    }
    // A new run on the same street: wave 1, a whole gate, plain throws.
    public DadThrowRun Again(int seed){return new DadThrowRun(kinds,background,seed);}
    public int Level(Pick pick){return levels[(int)pick];}
    public int Picked {get{int n=0;foreach(int level in levels)n+=level;return n;}}

    // The wave on the street was cleared: the gate keeps what it has left, and three offers are laid out.
    public void Cleared(DadThrowModel wave)
    {
        if(Over||Offers.Count>0)return;
        Health=wave.Health;Felled+=wave.Killed;
        // Whatever can still be taken, in a shuffled order, the first three. Repair is never offered to a whole gate.
        var pool=new List<Pick>(Cards.Length);
        foreach(Card card in Cards)if(levels[(int)card.pick]<card.most&&(card.pick!=Pick.Repair||Health<MaxHealth))pool.Add(card.pick);
        for(int i=0;i<Offered&&pool.Count>0;i++){int at=random.Next(pool.Count);Offers.Add(pool[at]);pool.RemoveAt(at);}
        // Nothing left to offer: the next wave simply comes.
        if(Offers.Count==0)Advance();
    }
    // The gate was touched for the last time.
    public void Lost(DadThrowModel wave){if(Over)return;Felled+=wave.Killed;Health=0;Offers.Clear();Over=true;}
    // Takes one of the offers and brings on the next wave. Anything else is refused.
    public bool Take(int index)
    {
        if(Over||index<0||index>=Offers.Count)return false;
        Pick pick=Offers[index];levels[(int)pick]++;
        if(pick==Pick.Repair)Health=Math.Min(MaxHealth,Health+1);else Apply(Rules,pick);
        Offers.Clear();Advance();return true;
    }
    void Advance(){Wave++;Stage=Build(Wave);}

    DadThrowStage.Group Pack(float at,int count,float firstX,float spacing,float speed,int hp,DadThrowStage.Kind kind)
    {return new DadThrowStage.Group{at=at,count=count,firstX=firstX,spacing=spacing,speed=speed,hp=hp,kind=kind};}
    // One wave as a stage table, in the form the stage file uses, so the fight and the picture need nothing new.
    DadThrowStage Build(int wave)
    {
        var stage=new DadThrowStage{id=wave+"조",backgroundKey=background,health=MaxHealth};
        int hard=Tough(wave),quick=Fast(wave),plain=People(wave)-hard-quick;float pace=Pace(wave),gap=Gap(wave);
        // Ordinary runners, about fourteen to a pack: one pack already close, one at the far end of the street,
        // the rest from off screen, a pack every `gap` seconds. Later packs run a little faster and closer together.
        int packs=Math.Max(2,(plain+PackSize-1)/PackSize),left=plain;float at=0;
        for(int i=0;i<packs;i++)
        {
            int count=i==packs-1?left:plain/packs;left-=count;if(i>=2)at+=gap;
            stage.groups.Add(Pack(at,count,i==0?700:i==1?1080:1300,i==0?44:i==1?32:Math.Max(22,30-wave),pace+i%3*8,1,normal));
        }
        // Quick packs come between the ordinary ones and catch up with the pack ahead.
        for(int i=0;quick>0;i++){int count=Math.Min(FastPack,quick);quick-=count;stage.groups.Add(Pack(gap*(1.5f+2*i),count,1300,40,pace+FastLead,1,fast));}
        // The tough ones walk in as a wall behind the first packs; a large number comes as two walls.
        if(hard>0)
        {
            int first=hard>8?hard/2:hard;
            stage.groups.Add(Pack(gap*2,first,1300,46,pace*ToughPace,ToughHealth(wave),tough));
            if(hard>first)stage.groups.Add(Pack(gap*5,hard-first,1300,46,pace*ToughPace,ToughHealth(wave),tough));
        }
        // Arrival order is what the fight walks through; equal times keep the order they were added in.
        for(int i=1;i<stage.groups.Count;i++)
            for(int j=i;j>0&&stage.groups[j].at<stage.groups[j-1].at;j--)
            {DadThrowStage.Group swap=stage.groups[j];stage.groups[j]=stage.groups[j-1];stage.groups[j-1]=swap;}
        return stage;
    }
}
