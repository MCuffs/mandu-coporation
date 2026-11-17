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
    // The picture an item is shown with, by its own name: Resources/DadThrowShopV1/shop_pierce and so on.
    public const string ArtFolder="DadThrowShopV1/";
    public static string IconKey(Item item){return ArtFolder+"shop_"+item.ToString().ToLowerInvariant();}
    public const string CoinKey=ArtFolder+"icon_won",ShopKey=ArtFolder+"icon_shop",SoldOutKey=ArtFolder+"mark_soldout";

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
// tray-holding ones join. The first chapter climbs steeply, because that is where the player learns the three
// of them and has a whole shop still to buy. From the second chapter on every number grows at a little over
// half that slope: a bot that had bought the shop out stood still on stages 9 to 12 of the steep curve, with
// nothing left to buy its way past them. The street is full (160) from stage 15; after that it is pace, the
// tough ones' health and the trays that go on growing.
public static class DadThrowCurve
{
    public const int Stages=30,PerChapter=5,MaxPeople=DadThrowStageSet.MaxTotal,MaxHealth=DadThrowStageSet.MaxHealth;
    public const float MaxPace=250;
    // "2-3" is the third stage of the second chapter.
    public static string Id(int number){return ((number-1)/PerChapter+1)+"-"+((number-1)%PerChapter+1);}
    // The last stage of the steep climb. Up to here a stage is what it was before the curve was laid flatter.
    public const int Knee=PerChapter;
    // Steep steps taken since stage `from`, and gentle steps taken since the knee.
    static int Steep(int stage,int from){return Math.Max(0,Math.Min(stage,Knee)-from);}
    static int Gentle(int stage){return Math.Max(0,stage-Knee);}
    public static int People(int stage){return Math.Min(MaxPeople,60+10*Steep(stage,1)+6*Gentle(stage));}
    public static float Pace(int stage){return Math.Min(MaxPace,112+6*Steep(stage,1)+4*Gentle(stage));}
    // Seconds between packs.
    public static float Gap(int stage){return Math.Max(.8f,1.6f-.05f*Steep(stage,1)-.03f*Gentle(stage));}
    // Chair dads: slow, and one plain ball does not put them down.
    public static int Tough(int stage){return stage<3?0:Math.Min(30,3+2*Steep(stage,3)+Gentle(stage));}
    public static int ToughHealth(int stage){return stage<3?3:3+(stage-3)/4;}
    // Dodge-ball dads: a small pack that outruns the pack in front of it.
    public static int Fast(int stage){return stage<2?0:Math.Min(30,4+2*Steep(stage,2)+Gentle(stage));}
    // Tray dads run at the front of a pack. A ball stops on the tray and reaches nobody behind it, however much
    // the ball has been upgraded; a shoe knocks the tray out of every hand it pushes. They are why buying balls
    // alone does not carry a player through.
    public static int Guards(int stage){return stage<4?0:Math.Min(24,2*Steep(stage,3)+Gentle(stage));}
    public const int PackSize=14,FastPack=6;public const float FastLead=70,ToughPace=.55f,TraySpacing=38;

    static DadThrowStage.Group Pack(float at,int count,float firstX,float spacing,float speed,int hp,DadThrowStage.Kind kind)
    {return new DadThrowStage.Group{at=at,count=count,firstX=firstX,spacing=spacing,speed=speed,hp=hp,kind=kind};}
    // Stage `number` as a table in the form the stage file uses, so the fight and the picture need nothing new.
    // kinds: the opponents the stage file declares. "normal" is needed; a missing "tough", "fast" or "guard"
    // is played by the normal one (and without a guard kind there are no trays).
    // crowd, tempo: 1 for the stage as the curve has it. The tuner bends them a little (DadThrowTuner): every
    // kind of opponent grows or shrinks by the same share, so the stage stays the same stage.
    public static DadThrowStage Stage(int number,IDictionary<string,DadThrowStage.Kind> kinds,string background,float crowd=1,float tempo=1)
    {
        DadThrowStage.Kind normal,tough,fast,guard;
        if(kinds==null||!kinds.TryGetValue("normal",out normal))throw new ArgumentException("the stage curve needs the normal opponent");
        if(!kinds.TryGetValue("tough",out tough))tough=normal;if(!kinds.TryGetValue("fast",out fast))fast=normal;
        if(!kinds.TryGetValue("guard",out guard)||!guard.guards)guard=null;
        var stage=new DadThrowStage{id=Id(number),backgroundKey=background,health=MaxHealth,baseReward=People(number)};
        int people=Math.Min(MaxPeople,Math.Max(PackSize,Mathf.RoundToInt(People(number)*crowd)));
        int hard=Mathf.RoundToInt(Tough(number)*crowd),quick=Mathf.RoundToInt(Fast(number)*crowd),trays=guard!=null?Mathf.RoundToInt(Guards(number)*crowd):0,plain=people-hard-quick-trays;
        float pace=Math.Min(MaxPace,Pace(number)*tempo),gap=Gap(number);
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
        var stages=new List<DadThrowStage>(Stages);var met=new HashSet<DadThrowStage.Kind>();
        for(int number=1;number<=Stages;number++)
        {
            DadThrowStage stage=Stage(number,kinds,background);
            if(written!=null)foreach(DadThrowStage old in written)if(old.id==stage.id)stage.after=old.after;
            stage.hint=number==1?"pull":Guards(number)>0&&Guards(number-1)==0&&stage.groups.Exists(g=>g.kind!=null&&g.kind.guards)?"shoe":null;
            stage.ending=number==PerChapter;
            // An opponent the stage file gives an entrance to makes it on the first stage he is in.
            foreach(DadThrowStage.Group g in stage.groups)if(met.Add(g.kind)&&g.kind.entrance!=null&&stage.entrance==null)stage.entrance=g.kind;
            stages.Add(stage);
        }
        return stages;
    }
}

// The difficulty a player meets bends a little toward how they are doing (2026-11-18, the owner: "difficulty
// that changes little by little with how the player plays"). Nothing here is shown to the player.
//  - Lose the same stage twice and it eases: from the second defeat each one takes 6% of the people and 4% of
//    the pace away, four steps at most. The first defeat changes nothing; a clear puts the stage back.
//  - Clear two stages running without the gate being touched and the next one heats up: 5% more people and
//    3% more pace a step, three steps at most, and each step pays a tenth more won. A clear with a touch on
//    the gate cools one step; a defeat cools it all.
//  - Someone whose balls keep stopping on trays and who hardly throws a shoe is not given fewer trays: the
//    shoe button asks for attention for as long as a tray is in view.
// The bends are small on purpose: a lost stage still pays, and a stage that fell away under the player would
// leave the shop with nothing to sell.
public sealed class DadThrowTuner
{
    public const int MaxEase=4,MaxHeat=3,ShyBlocks=4,ShyShare=3;
    public const float EasePeople=.06f,EasePace=.04f,HeatPeople=.05f,HeatPace=.03f;
    // A heated stage pays this many won more for every ten sent back, per step.
    public const int HeatPayPerTen=1;
    readonly IDictionary<string,DadThrowStage.Kind> kinds;readonly string background;
    public DadThrowTuner(IDictionary<string,DadThrowStage.Kind> kinds,string background)
    {if(kinds==null)throw new ArgumentNullException("kinds");this.kinds=kinds;this.background=background;}

    // Steps of easing stage `id` is owed: the defeats on it beyond the first.
    public static int Ease(DadThrowSave save,string id){return save.tunedStage==id?Mathf.Clamp(save.defeats-1,0,MaxEase):0;}
    // Steps of heat the next stage starts with: flawless clears in a row beyond the first.
    public static int Heat(DadThrowSave save){return Mathf.Clamp(save.flawless-1,0,MaxHeat);}
    public static float Crowd(int ease,int heat){return 1-EasePeople*ease+HeatPeople*heat;}
    public static float Tempo(int ease,int heat){return 1-EasePace*ease+HeatPace*heat;}
    // What a fight pays: one won for each sent back, and on a heated stage a tenth more for each step.
    public static int Pay(int felled,int heat){return felled*DadThrowShop.WonPerFelled+felled*heat*HeatPayPerTen/10;}
    // A defeat on stage `id`: counted on that stage, and whatever streak there was is over.
    public static void Lost(DadThrowSave save,string id)
    {if(save.tunedStage!=id){save.tunedStage=id;save.defeats=0;}save.defeats++;save.flawless=0;}
    // A clear: the stage owes nothing more. An untouched gate adds to the streak; a touched one takes one off.
    public static void Cleared(DadThrowSave save,bool touched)
    {save.tunedStage=null;save.defeats=0;save.flawless=touched?Math.Max(0,save.flawless-1):Math.Min(MaxHeat+1,save.flawless+1);}
    // Balls stopped on trays and shoes thrown, added up over the fights so far.
    public static void Watched(DadThrowSave save,int blocked,int shoes){save.blocked+=blocked;save.shoes+=shoes;}
    public static bool ShoeShy(DadThrowSave save){return save.blocked>=ShyBlocks&&save.shoes*ShyShare<save.blocked;}

    // The table stage `number` is fought on: the plain one when nothing is owed, otherwise the curve's own table
    // with the crowd and the pace bent. What the stage says and shows (its name, hint, lines, entrance) stays.
    public DadThrowStage Table(DadThrowStage plain,int number,DadThrowSave save)
    {
        int ease=Ease(save,plain.id),heat=Heat(save);
        if(ease==0&&heat==0)return plain;
        DadThrowStage bent=DadThrowCurve.Stage(number,kinds,background,Crowd(ease,heat),Tempo(ease,heat));
        bent.hint=plain.hint;bent.after=plain.after;bent.ending=plain.ending;bent.entrance=plain.entrance;
        return bent;
    }
}
