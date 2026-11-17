using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// The opening and the chapter ending of the gate-touch contest (Design/dad-throw-story-animation-v2.md):
// timed cuts of standing cutouts over a backdrop with one line of dialogue. Who stands where, for how long and
// what is said comes from Resources/DadThrowStoryV1/story.json. Nothing is drawn here.
public sealed class DadThrowStory
{
    public sealed class Cut
    {
        public float from,seconds;public string backdrop,name,line;
        // rival: the challenger stands on the right. prop: the toy ball is shown by dad's hand.
        // battle: the cut is the fight's own camera, with dad winding up, so the fight starts where the story ends.
        public bool rival,prop,battle;
        public readonly List<string> actors=new List<string>();
    }
    public sealed class Actor { public string id;public Rect uv;public float left; }

    public const float W=DadThrowModel.Width,H=DadThrowModel.Height;
    // Placement follows the timing preview (Design/dad-throw-story-v1/index.html): cutouts 40% of the screen wide,
    // standing 4% above the bottom edge, dad shifted 25 left outdoors, the challenger on the right.
    public const float ActorSize=512,ActorBottom=691,StreetShift=-25;
    public static readonly Rect Rival=new Rect(866,216,324,432),Prop=new Rect(314,371,51,51);
    public static readonly Rect Dialogue=new Rect(77,596,1126,102),DialogueName=new Rect(101,606,1078,26),DialogueLine=new Rect(101,634,1078,56);
    public static readonly Rect Skip=new Rect(1084,20,164,64);

    public string actorSheet,rivalResource,propResource;
    public readonly Dictionary<string,string> backdrops=new Dictionary<string,string>();
    public readonly Dictionary<string,Actor> actors=new Dictionary<string,Actor>();
    public readonly List<Cut> opening=new List<Cut>(),ending=new List<Cut>();
    public static float Length(List<Cut> cuts){return cuts.Count==0?0:cuts[cuts.Count-1].from+cuts[cuts.Count-1].seconds;}
    public float OpeningSeconds {get{return Length(opening);}}
    public float EndingSeconds {get{return Length(ending);}}
    // The cut on screen at `clock`; the last one once the time has run out.
    public static Cut At(List<Cut> cuts,float clock)
    {
        foreach(Cut cut in cuts)if(clock<cut.from+cut.seconds)return cut;
        return cuts.Count>0?cuts[cuts.Count-1]:null;
    }
    public Rect ActorRect(Actor actor,Cut cut)
    {return new Rect(actor.left*W+(cut.backdrop=="street"?StreetShift:0),ActorBottom-ActorSize,ActorSize,ActorSize);}

    public static DadThrowStory Parse(string json)
    {
        var story=new DadThrowStory();var root=DadThrowJson.Map(DadThrowJson.Parse(json),"story file");
        story.actorSheet=DadThrowJson.Text(root,"actorSheet","story file");story.rivalResource=DadThrowJson.Text(root,"rival","story file");
        story.propResource=DadThrowJson.Text(root,"prop","story file");
        foreach(var entry in DadThrowJson.Map(Need(root,"backdrops"),"backdrops"))
        {
            if(!(entry.Value is string)||((string)entry.Value).Length==0)throw new ArgumentException("backdrop "+entry.Key+" needs a picture");
            story.backdrops[entry.Key]=(string)entry.Value;
        }
        foreach(var entry in DadThrowJson.Map(Need(root,"actors"),"actors"))
        {
            string owner="actor "+entry.Key;var map=DadThrowJson.Map(entry.Value,owner);var cell=DadThrowJson.List(Need(map,"cell"),owner+" cell");
            if(cell.Count!=2||!(cell[0] is double)||!(cell[1] is double))throw new ArgumentException(owner+" needs a column and a row");
            int column=(int)(double)cell[0],row=(int)(double)cell[1];
            if(column<0||column>1||row<0||row>1)throw new ArgumentException(owner+" is outside the 2x2 sheet");
            // Sheet cells are counted from the top left; texture coordinates start at the bottom left.
            story.actors[entry.Key]=new Actor{id=entry.Key,uv=new Rect(column*.5f,1-(row+1)*.5f,.5f,.5f),left=DadThrowJson.Number(map,"left",owner)};
        }
        Read(story,root,"opening",story.opening);Read(story,root,"ending",story.ending);
        if(story.opening.Count==0)throw new ArgumentException("story file has no opening");
        return story;
    }
    static object Need(Dictionary<string,object> map,string key)
    {object value;if(!map.TryGetValue(key,out value)||value==null)throw new ArgumentException("story file needs "+key);return value;}
    static bool Flag(Dictionary<string,object> map,string key){object value;return map.TryGetValue(key,out value)&&value is bool&&(bool)value;}
    static void Read(DadThrowStory story,Dictionary<string,object> root,string key,List<Cut> cuts)
    {
        float clock=0;
        foreach(object item in DadThrowJson.List(Need(root,key),key))
        {
            string owner=key+" cut "+(cuts.Count+1);var map=DadThrowJson.Map(item,owner);
            var cut=new Cut{from=clock,seconds=DadThrowJson.Number(map,"seconds",owner),name=DadThrowJson.Text(map,"name",owner),line=DadThrowJson.Text(map,"line",owner),
                backdrop=DadThrowJson.TextOr(map,"backdrop",null,owner),rival=Flag(map,"rival"),prop=Flag(map,"prop"),battle=Flag(map,"battle")};
            if(cut.seconds<=0)throw new ArgumentException(owner+" needs a length above zero");
            if(!cut.battle&&(cut.backdrop==null||!story.backdrops.ContainsKey(cut.backdrop)))throw new ArgumentException(owner+" needs a known backdrop");
            if(DadThrowJson.Has(map,"actors"))foreach(object who in DadThrowJson.List(map["actors"],owner+" actors"))
            {
                if(!(who is string)||!story.actors.ContainsKey((string)who))throw new ArgumentException(owner+" shows someone the sheet does not have");
                cut.actors.Add((string)who);
            }
            cuts.Add(cut);clock+=cut.seconds;
        }
    }
}

// What the player has done so far, kept across launches (plan §16). Only what the story needs for now:
// whether the opening was seen, how far the stages are open, and which stages were cleared once.
public sealed class DadThrowSave
{
    public const int Schema=1;
    public bool introSeen;public int furthest;public readonly List<string> cleared=new List<string>();
    // Won in the purse and how many times each shop item has been bought (in the order of DadThrowShop.Item).
    // Files written before the shop existed have neither and read as an empty purse with nothing bought.
    public int coins;public readonly int[] upgrades=new int[DadThrowShop.Cards.Length];
    public string ToJson()
    {
        var text=new StringBuilder("{\"schemaVersion\":"+Schema+",\"introSeen\":"+(introSeen?"true":"false")+",\"furthest\":"+furthest+",\"cleared\":[");
        for(int i=0;i<cleared.Count;i++)text.Append(i>0?",":"").Append('"').Append(cleared[i].Replace("\\","\\\\").Replace("\"","\\\"")).Append('"');
        text.Append("],\"coins\":"+coins+",\"upgrades\":{");
        for(int i=0;i<upgrades.Length;i++)text.Append(i>0?",":"").Append('"').Append(DadThrowShop.Cards[i].item.ToString()).Append("\":").Append(upgrades[i]);
        return text.Append("}}").ToString();
    }
    public static DadThrowSave Parse(string json)
    {
        var map=DadThrowJson.Map(DadThrowJson.Parse(json),"save");
        if(DadThrowJson.Whole(map,"schemaVersion","save")!=Schema)throw new ArgumentException("save is from another version");
        object seen;if(!map.TryGetValue("introSeen",out seen)||!(seen is bool))throw new ArgumentException("save needs introSeen");
        var save=new DadThrowSave{introSeen=(bool)seen,furthest=Math.Max(0,DadThrowJson.Whole(map,"furthest","save"))};
        if(DadThrowJson.Has(map,"coins"))save.coins=Math.Max(0,DadThrowJson.Whole(map,"coins","save"));
        if(DadThrowJson.Has(map,"upgrades"))
        {
            // A level is held to what the shop sells; an item this build does not know is left out.
            var bought=DadThrowJson.Map(map["upgrades"],"save upgrades");
            foreach(DadThrowShop.Card card in DadThrowShop.Cards)
                if(DadThrowJson.Has(bought,card.item.ToString()))save.upgrades[(int)card.item]=Mathf.Clamp(DadThrowJson.Whole(bought,card.item.ToString(),"save upgrades"),0,card.most);
        }
        object list;if(!map.TryGetValue("cleared",out list))throw new ArgumentException("save needs cleared");
        foreach(object id in DadThrowJson.List(list,"save cleared"))
        {if(!(id is string))throw new ArgumentException("save has a stage that is not a name");if(!save.cleared.Contains((string)id))save.cleared.Add((string)id);}
        return save;
    }
    // A missing file is a new player. A broken file falls back to the backup; if that is broken too the player
    // starts new and `problem` says why, so the loss is reported instead of silently overwritten.
    public static DadThrowSave Load(string path,out string problem)
    {
        problem=null;
        if(!File.Exists(path)&&!File.Exists(path+".bak"))return new DadThrowSave();
        foreach(string file in new[]{path,path+".bak"})
        {
            if(!File.Exists(file))continue;
            try{DadThrowSave save=Parse(File.ReadAllText(file));if(file!=path)problem="save was unreadable; the backup was used";return save;}
            catch(Exception error){if(!(error is ArgumentException||error is IOException))throw;problem="save and backup were unreadable; starting new ("+error.Message+")";}
        }
        return new DadThrowSave();
    }
    // Written beside the file first and read back, then swapped in; the file it replaces becomes the backup.
    public void Write(string path)
    {
        string draft=path+".tmp";File.WriteAllText(draft,ToJson());Parse(File.ReadAllText(draft));
        if(File.Exists(path))File.Copy(path,path+".bak",true);
        File.Copy(draft,path,true);File.Delete(draft);
    }
}
