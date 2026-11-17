using System;
using System.Collections.Generic;
using UnityEngine;

// One hand-made stage: who walks in, when, from where (plan §13, §17.2). The stage owns its head count,
// speeds and health, so nothing here is added to the endless-wave formula of the first prototype.
public sealed class DadThrowStage
{
    // An opponent type as the stage file declares it: which frames to draw and how they sit on their canvas.
    public sealed class Kind
    {
        public string id,resource;public int frames;public bool facesLeft;
        // guards: walks in holding a tray (see DadThrowModel.Enemy.guard). bare: the frames to draw once the tray is gone.
        public bool guards;public string bare;
        public string BareFrame(int index){return bare+"_"+index;}
        // The big picture, the line he shouts and the advice shown the first time this opponent comes on
        // (DadThrowFlow.Entering). The picture has a canvas and a feet point of its own: the large ones drawn for
        // the entrance are 2048 square with the soles at (1024, 1900). A stage file that gives neither is taken to
        // use the walking frames' canvas and feet line, as the first entrances did.
        // shoe: the advice is about the shoe (the card shows it); otherwise it is about the ball.
        public sealed class Entrance { public string resource,name,shout,hint;public float pivotX,pivotY,stand,canvas;public bool shoe; }
        public Entrance entrance;
        // A chapter's boss (DadThrowModel.BossStep): the pictures for what he does besides walking, how wide he is
        // to a thrown thing, and what sending him back pays on top of the one won everyone pays.
        public sealed class Boss { public string brace,dash,hit,dazed,roar,fly;public float half;public int bounty; }
        public Boss boss;
        // Whole-canvas rectangle that stands the entrance picture's body `height` tall with its feet on (x, ground).
        public Rect EntrancePlace(float x,float ground,float height)
        {float k=height/entrance.stand;return new Rect(x-entrance.pivotX*k,ground-entrance.pivotY*k,entrance.canvas*k,entrance.canvas*k);}
        public float canvas,pivotX,pivotY,stand;public readonly List<float> heights=new List<float>();
        public string Frame(int index){return resource+"_"+index;}
        // Sizes differ a little so a crowd reads as people, while every one keeps the same feet line and hit circle.
        public float Height(int id){return heights[((id%heights.Count)+heights.Count)%heights.Count];}
        // Whole-canvas rectangle that puts the picture's feet on (x, ground).
        public Rect Place(float x,float ground,float height){float k=height/stand;return new Rect(x-pivotX*k,ground-pivotY*k,canvas*k,canvas*k);}
        // Opponents only ever walk toward the house on the left; a picture drawn facing right has to be mirrored.
        public bool Mirror {get{return !facesLeft;}}
    }
    // `count` people appear together at `at` seconds: the first at firstX, each next one `spacing` further right.
    public sealed class Group { public float at,firstX,spacing,speed;public int count,hp;public Kind kind; }

    // One line for the clear card, shown the first time the stage is cleared (story §5).
    public sealed class Line { public string name,line; }
    public string id,backgroundKey,hint;public int health,baseReward;
    public Line after;
    // The chapter's ending plays after this stage the first time it is cleared.
    public bool ending;
    // The opponent who makes an entrance when this stage is started for the first time (DadThrowFlow.Entering).
    public Kind entrance;
    public readonly List<Group> groups=new List<Group>();
    public int Total {get{int n=0;foreach(Group g in groups)n+=g.count;return n;}}
    // Leaves nobody on the schedule: used to stand a crowd up by hand in checks.
    public static DadThrowStage Empty(int health=5){return new DadThrowStage{id="empty",health=health};}
}

// The stage file as a whole. Parse refuses anything that would start a broken fight (plan §17.2).
public sealed class DadThrowStageSet
{
    // Technical ceiling carried over from the wave prototype; the readable target is far lower (plan §8.1).
    public const int MaxTotal=160,MaxHealth=5;
    public static readonly string[] Hints={"pull","shoe"};
    public readonly Dictionary<string,DadThrowStage.Kind> kinds=new Dictionary<string,DadThrowStage.Kind>();
    public readonly List<DadThrowStage> stages=new List<DadThrowStage>();

    // `backgrounds`: resource keys the layout has measured anchors for. `failLine`: nobody may start at or behind it.
    public static DadThrowStageSet Parse(string json,ICollection<string> backgrounds,float failLine)
    {
        var set=new DadThrowStageSet();
        var root=DadThrowJson.Map(DadThrowJson.Parse(json),"stage file");
        foreach(var entry in DadThrowJson.Map(Need(root,"enemies","stage file"),"enemies"))
        {
            string owner="enemy "+entry.Key;var map=DadThrowJson.Map(entry.Value,owner);
            var kind=new DadThrowStage.Kind{id=entry.Key,resource=DadThrowJson.Text(map,"resource",owner),frames=DadThrowJson.Whole(map,"frames",owner),
                canvas=DadThrowJson.Number(map,"canvas",owner),pivotX=DadThrowJson.Number(map,"pivotX",owner),pivotY=DadThrowJson.Number(map,"pivotY",owner),
                stand=DadThrowJson.Number(map,"standPixels",owner)};
            object faces;if(!map.TryGetValue("facesLeft",out faces)||!(faces is bool))throw new ArgumentException(owner+" must say facesLeft true or false");
            kind.facesLeft=(bool)faces;
            object tray;if(map.TryGetValue("guards",out tray)){if(!(tray is bool))throw new ArgumentException(owner+" must say guards true or false");kind.guards=(bool)tray;}
            kind.bare=DadThrowJson.TextOr(map,"bareResource",null,owner);
            if(DadThrowJson.Has(map,"entrance"))
            {
                string own=owner+" entrance";var word=DadThrowJson.Map(map["entrance"],own);
                kind.entrance=new DadThrowStage.Kind.Entrance{resource=DadThrowJson.Text(word,"resource",own),name=DadThrowJson.Text(word,"name",own),
                    shout=DadThrowJson.Text(word,"shout",own),hint=DadThrowJson.Text(word,"hint",own),pivotX=DadThrowJson.Number(word,"pivotX",own),stand=DadThrowJson.Number(word,"standPixels",own),
                    pivotY=DadThrowJson.NumberOr(word,"pivotY",kind.pivotY,own),canvas=DadThrowJson.NumberOr(word,"canvas",kind.canvas,own)};
                if(kind.entrance.stand<=0||kind.entrance.canvas<=0)throw new ArgumentException(own+" needs a standing height and a canvas above zero");
                string with=DadThrowJson.TextOr(word,"with","shoe",own);
                if(with!="shoe"&&with!="golf")throw new ArgumentException(own+" is done with the shoe or with golf, not with "+with);
                kind.entrance.shoe=with=="shoe";
            }
            if(DadThrowJson.Has(map,"boss"))
            {
                string own=owner+" boss";var word=DadThrowJson.Map(map["boss"],own);
                kind.boss=new DadThrowStage.Kind.Boss{brace=DadThrowJson.Text(word,"brace",own),dash=DadThrowJson.Text(word,"dash",own),hit=DadThrowJson.Text(word,"hit",own),
                    dazed=DadThrowJson.Text(word,"dazed",own),roar=DadThrowJson.Text(word,"roar",own),fly=DadThrowJson.Text(word,"fly",own),
                    half=DadThrowJson.NumberOr(word,"bodyHalf",DadThrowModel.BodyHalf,own),bounty=(int)DadThrowJson.NumberOr(word,"bounty",0,own)};
                if(kind.boss.half<DadThrowModel.BodyHalf||kind.boss.bounty<0)throw new ArgumentException(own+" needs a body at least as wide as anyone's and a bounty that is not negative");
                if(kind.guards)throw new ArgumentException(own+" cannot also carry a tray");
            }
            foreach(object value in DadThrowJson.List(Need(map,"heights",owner),owner+" heights"))
            {if(!(value is double)||(double)value<=0)throw new ArgumentException(owner+" has a height that is not above zero");kind.heights.Add((float)(double)value);}
            if(kind.frames<1||kind.canvas<=0||kind.stand<=0||kind.heights.Count==0)throw new ArgumentException(owner+" needs frames, a canvas, a standing height and at least one drawn height");
            set.kinds[kind.id]=kind;
        }
        foreach(object item in DadThrowJson.List(Need(root,"stages","stage file"),"stages"))
        {
            var map=DadThrowJson.Map(item,"a stage");string id=DadThrowJson.Text(map,"id","a stage"),owner="stage "+id;
            foreach(DadThrowStage other in set.stages)if(other.id==id)throw new ArgumentException(owner+" is listed twice");
            var stage=new DadThrowStage{id=id,backgroundKey=DadThrowJson.Text(map,"backgroundKey",owner),health=DadThrowJson.Whole(map,"health",owner),
                baseReward=DadThrowJson.Whole(map,"baseReward",owner),hint=DadThrowJson.TextOr(map,"hint",null,owner)};
            if(!backgrounds.Contains(stage.backgroundKey))throw new ArgumentException(owner+" uses a background with no measured anchors: "+stage.backgroundKey);
            if(stage.health<1||stage.health>MaxHealth)throw new ArgumentException(owner+" needs house health from 1 to "+MaxHealth);
            if(stage.baseReward<0)throw new ArgumentException(owner+" has a negative reward");
            if(stage.hint!=null&&Array.IndexOf(Hints,stage.hint)<0)throw new ArgumentException(owner+" has an unknown hint: "+stage.hint);
            if(DadThrowJson.Has(map,"after"))
            {var word=DadThrowJson.Map(map["after"],owner+" after");stage.after=new DadThrowStage.Line{name=DadThrowJson.Text(word,"name",owner+" after"),line=DadThrowJson.Text(word,"line",owner+" after")};}
            object last;stage.ending=map.TryGetValue("ending",out last)&&last is bool&&(bool)last;
            foreach(object entry in DadThrowJson.List(Need(map,"groups",owner),owner+" groups"))
            {
                var g=DadThrowJson.Map(entry,owner+" group");string name=DadThrowJson.Text(g,"enemy",owner+" group");
                DadThrowStage.Kind kind;if(!set.kinds.TryGetValue(name,out kind))throw new ArgumentException(owner+" asks for an unknown enemy: "+name);
                var group=new DadThrowStage.Group{at=DadThrowJson.Number(g,"at",owner+" group"),count=DadThrowJson.Whole(g,"count",owner+" group"),
                    firstX=DadThrowJson.Number(g,"firstX",owner+" group"),spacing=DadThrowJson.NumberOr(g,"spacing",0,owner+" group"),
                    speed=DadThrowJson.Number(g,"speed",owner+" group"),hp=DadThrowJson.Whole(g,"hp",owner+" group"),kind=kind};
                if(group.at<0)throw new ArgumentException(owner+" has a group before time zero");
                if(group.count<1)throw new ArgumentException(owner+" has a group with nobody in it");
                if(group.hp<1)throw new ArgumentException(owner+" has a group with no health");
                if(group.speed<=0)throw new ArgumentException(owner+" has a group that does not walk");
                if(group.count>1&&group.spacing<=0)throw new ArgumentException(owner+" stacks a group on one spot");
                if(group.firstX<=failLine)throw new ArgumentException(owner+" starts a group at or behind the house line");
                stage.groups.Add(group);
            }
            if(stage.groups.Count==0)throw new ArgumentException(owner+" has no groups");
            if(stage.Total>MaxTotal)throw new ArgumentException(owner+" has "+stage.Total+" people, over the ceiling of "+MaxTotal);
            // Arrival order is what the runner walks through; equal times keep the order they were written in.
            for(int i=1;i<stage.groups.Count;i++)
                for(int j=i;j>0&&stage.groups[j].at<stage.groups[j-1].at;j--)
                {DadThrowStage.Group swap=stage.groups[j];stage.groups[j]=stage.groups[j-1];stage.groups[j-1]=swap;}
            set.stages.Add(stage);
        }
        if(set.stages.Count==0)throw new ArgumentException("stage file has no stages");
        return set;
    }
    static object Need(Dictionary<string,object> map,string key,string owner)
    {object value;if(!map.TryGetValue(key,out value)||value==null)throw new ArgumentException(owner+" needs "+key);return value;}
}
