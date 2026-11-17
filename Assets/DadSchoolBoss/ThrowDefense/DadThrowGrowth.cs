using System;
using System.Collections.Generic;
using UnityEngine;

// Stages that get harder one by one, and a shop to keep up with them (2026-11-17, the owner after playing 연속전:
// "have stages, harder and harder; a shop; one won for every challenger sent back; upgrade to beat them").
// This replaces the run with three offers between waves: its picks became the shelf of the shop, and its wave
// formula became the stage curve. Rules only: no drawing, no input, no files.
// The numbers are a first guess measured with bots (Design/dad-throw-stages-shop-v1.md), not with people.

// What the shop sells. Everything bought stays bought: the save keeps a level for each item.
public static class DadThrowShop
{
    public enum Item { Pierce, Reload, Twin, Heavy, Bounce, Blast, Wide, Sticky, Flow }
    // One item as its card shows it. `most`: how many times it can be bought. `price`: the first purchase;
    // the second costs twice that, the third three times. `shoe`: the card shows the shoe.
    public sealed class Card
    {
        public readonly Item item;public readonly string name,line;public readonly int most,price;public readonly bool shoe;
        public Card(Item item,string name,string line,int most,int price,bool shoe){this.item=item;this.name=name;this.line=line;this.most=most;this.price=price;this.shoe=shoe;}
    }
    // In the order of Item, so an item is its own index.
    // The first prices (everything for 1,890 won) were bought out by stage 10 with 1,700 won to spare, and from
    // there the stages only got harder (Design/dad-throw-growth-report-first-prices.txt). With more levels and
    // dearer ones everything costs 3,480 won, about what stages 1 to 23 pay when each is cleared at the first try.
    public static readonly Card[] Cards={
        new Card(Item.Pierce,"묵직한 공","한 공이 여섯 명을 더 뚫고 간다",4,40,false),
        new Card(Item.Reload,"빠른 손","공을 더 빨리 다시 던진다",3,60,false),
        new Card(Item.Twin,"쌍둥이 공","공이 하나 더 같이 나간다",2,200,false),
        new Card(Item.Heavy,"돌덩이 공","단단한 상대를 더 세게 때린다",4,80,false),
        new Card(Item.Bounce,"통통 공","공이 땅에서 한 번 더 튄다",2,80,false),
        new Card(Item.Blast,"폭탄 신발","신발이 모은 무리를 쓰러뜨린다",4,60,true),
        new Card(Item.Wide,"왕발 신발","신발이 더 넓게 밀어낸다",2,60,true),
        new Card(Item.Sticky,"끈끈이 신발","맞은 무리가 더 오래 느려진다",2,50,true),
        new Card(Item.Flow,"신바람","한 공에 열 명 맞히면 바로 또 던진다",1,150,false)};
    public static Card Of(Item item){return Cards[(int)item];}

    // One won for every challenger sent back.
    public const int WonPerFelled=1;
    public const int PierceStep=6,FlowChain=10;public const float ReloadStep=.8f,WideStep=1.35f,StickyTime=2,StickyStep=.7f;
    public static void Apply(DadThrowRules rules,Item item)
    {
        if(item==Item.Pierce)rules.Pierce+=PierceStep;
        else if(item==Item.Reload)rules.GolfReload*=ReloadStep;
        else if(item==Item.Twin)rules.GolfCount++;
        else if(item==Item.Heavy)rules.GolfDamage++;
        else if(item==Item.Bounce)rules.MaxBounces++;
        else if(item==Item.Blast)rules.ShoeBlast++;
        else if(item==Item.Wide)rules.ShoeRadius*=WideStep;
        else if(item==Item.Sticky){rules.SlowTime+=StickyTime;rules.SlowFactor*=StickyStep;}
        else if(item==Item.Flow)rules.ChainReload=FlowChain;
    }
    public static int Level(DadThrowSave save,Item item){return save.upgrades[(int)item];}
    public static bool Full(DadThrowSave save,Item item){return Level(save,item)>=Of(item).most;}
    // What the next purchase of this item costs.
    public static int Price(DadThrowSave save,Item item){return Of(item).price*(Level(save,item)+1);}
    public static bool CanBuy(DadThrowSave save,Item item){return !Full(save,item)&&save.coins>=Price(save,item);}
    public static bool Buy(DadThrowSave save,Item item)
    {
        if(!CanBuy(save,item))return false;
        save.coins-=Price(save,item);save.upgrades[(int)item]++;return true;
    }
    // The throws a stage is fought with: the plain ones, with everything the save has bought added.
    public static DadThrowRules Rules(DadThrowSave save)
    {
        var rules=new DadThrowRules();
        foreach(Card card in Cards)for(int i=Math.Min(card.most,Level(save,card.item));i>0;i--)Apply(rules,card.item);
        return rules;
    }
}

// How hard stage n is. The first stage is lighter than the hand-made stampede it replaces (60 people against
// 100); every stage adds people, pace and closer packs, and from stage 2, 3 and 4 the quick, the tough and the
// tray-holding ones join. From stage 11 the street holds as many as the game allows at once, and what goes on
// growing is pace, the tough ones' health and the trays.
public static class DadThrowCurve
{
    public const int Stages=30,PerChapter=5,MaxPeople=DadThrowStageSet.MaxTotal,MaxHealth=DadThrowStageSet.MaxHealth;
    public const float MaxPace=250;
    // "2-3" is the third stage of the second chapter.
    public static string Id(int number){return ((number-1)/PerChapter+1)+"-"+((number-1)%PerChapter+1);}
    public static int People(int stage){return Math.Min(MaxPeople,60+10*(stage-1));}
    public static float Pace(int stage){return Math.Min(MaxPace,112+6*(stage-1));}
    // Seconds between packs.
    public static float Gap(int stage){return Math.Max(.8f,1.6f-.05f*(stage-1));}
    // Chair dads: slow, and one plain ball does not put them down.
    public static int Tough(int stage){return stage<3?0:Math.Min(30,3+2*(stage-3));}
    public static int ToughHealth(int stage){return stage<3?3:3+(stage-3)/3;}
    // Dodge-ball dads: a small pack that outruns the pack in front of it.
    public static int Fast(int stage){return stage<2?0:Math.Min(30,4+2*(stage-2));}
    // Tray dads run at the front of a pack. A ball stops on the tray and reaches nobody behind it, however much
    // the ball has been upgraded; a shoe knocks the tray out of every hand it pushes. They are why buying balls
    // alone does not carry a player through.
    public static int Guards(int stage){return stage<4?0:Math.Min(24,2*(stage-3));}
    public const int PackSize=14,FastPack=6;public const float FastLead=70,ToughPace=.55f,TraySpacing=38;

    static DadThrowStage.Group Pack(float at,int count,float firstX,float spacing,float speed,int hp,DadThrowStage.Kind kind)
    {return new DadThrowStage.Group{at=at,count=count,firstX=firstX,spacing=spacing,speed=speed,hp=hp,kind=kind};}
    // Stage `number` as a table in the form the stage file uses, so the fight and the picture need nothing new.
    // kinds: the opponents the stage file declares. "normal" is needed; a missing "tough", "fast" or "guard"
    // is played by the normal one (and without a guard kind there are no trays).
    public static DadThrowStage Stage(int number,IDictionary<string,DadThrowStage.Kind> kinds,string background)
    {
        DadThrowStage.Kind normal,tough,fast,guard;
        if(kinds==null||!kinds.TryGetValue("normal",out normal))throw new ArgumentException("the stage curve needs the normal opponent");
        if(!kinds.TryGetValue("tough",out tough))tough=normal;if(!kinds.TryGetValue("fast",out fast))fast=normal;
        if(!kinds.TryGetValue("guard",out guard)||!guard.guards)guard=null;
        var stage=new DadThrowStage{id=Id(number),backgroundKey=background,health=MaxHealth,baseReward=People(number)};
        int hard=Tough(number),quick=Fast(number),trays=guard!=null?Guards(number):0,plain=People(number)-hard-quick-trays;
        float pace=Pace(number),gap=Gap(number);
        // Ordinary runners, about fourteen to a pack: one pack already close, one at the far end of the street,
        // the rest from off screen, a pack every `gap` seconds. Later packs run a little faster and closer together.
        int packs=Math.Max(2,(plain+PackSize-1)/PackSize),left=plain;float at=0;
        for(int i=0;i<packs;i++)
        {
            int count=i==packs-1?left:plain/packs;left-=count;if(i>=2)at+=gap;
            stage.groups.Add(Pack(at,count,i==0?700:i==1?1080:1300,i==0?44:i==1?32:Math.Max(22,30-number),pace+i%3*8,1,normal));
        }
        // Trays are shared out over the packs behind the first, a few at the very front of each, at the pack's own pace.
        for(int i=1,behind=packs-1;i<packs&&trays>0;i++)
        {
            int count=trays/behind+(i<=trays%behind?1:0);if(count==0)continue;
            // They take the spot the pack would have started from and the pack falls in right behind them.
            DadThrowStage.Group pack=stage.groups[i];
            stage.groups.Add(Pack(pack.at,count,pack.firstX,TraySpacing,pack.speed,1,guard));pack.firstX+=count*TraySpacing;
        }
        // Quick packs come between the ordinary ones and catch up with the pack ahead.
        for(int i=0;quick>0;i++){int count=Math.Min(FastPack,quick);quick-=count;stage.groups.Add(Pack(gap*(1.5f+2*i),count,1300,40,pace+FastLead,1,fast));}
        // The tough ones walk in as a wall behind the first packs; a large number comes as two walls.
        if(hard>0)
        {
            int first=hard>8?hard/2:hard;
            stage.groups.Add(Pack(gap*2,first,1300,46,pace*ToughPace,ToughHealth(number),tough));
            if(hard>first)stage.groups.Add(Pack(gap*5,hard-first,1300,46,pace*ToughPace,ToughHealth(number),tough));
        }
        // Arrival order is what the fight walks through; equal times keep the order they were added in.
        for(int i=1;i<stage.groups.Count;i++)
            for(int j=i;j>0&&stage.groups[j].at<stage.groups[j-1].at;j--)
            {DadThrowStage.Group swap=stage.groups[j];stage.groups[j]=stage.groups[j-1];stage.groups[j-1]=swap;}
        return stage;
    }
    // The whole game: `Stages` stages off the curve. The lines people say after a stage are taken from the stage
    // file where it has a stage of the same name. The first stage teaches the pull, the first stage with trays
    // points at the shoe, and the first chapter's last stage is followed by the ending.
    public static List<DadThrowStage> Build(IDictionary<string,DadThrowStage.Kind> kinds,string background,IList<DadThrowStage> written=null)
    {
        var stages=new List<DadThrowStage>(Stages);
        for(int number=1;number<=Stages;number++)
        {
            DadThrowStage stage=Stage(number,kinds,background);
            if(written!=null)foreach(DadThrowStage old in written)if(old.id==stage.id)stage.after=old.after;
            stage.hint=number==1?"pull":Guards(number)>0&&Guards(number-1)==0&&stage.groups.Exists(g=>g.kind!=null&&g.kind.guards)?"shoe":null;
            stage.ending=number==PerChapter;
            stages.Add(stage);
        }
        return stages;
    }
}
