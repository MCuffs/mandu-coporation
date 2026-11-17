using System.Collections.Generic;
using UnityEngine;

// Open DadThrowDefense.unity explicitly. This never replaces the current game's startup scene.
// Screens follow DadThrowFlow, placement follows DadThrowLayout, the fight follows DadThrowModel and the
// people who walk in come from Resources/DadThrowStages. The opening and the ending come from DadThrowStory.
// The stages themselves come off DadThrowCurve (harder one by one) and the shop's shelf from DadThrowShop.
// This class only reads pointers and draws.
// A missing picture or data file stops on a screen that names it; nothing is replaced by stand-in blocks.
public sealed class DadThrowPrototype:MonoBehaviour
{
    public DadThrowFlow Flow {get;private set;}
    public DadThrowModel Model {get{return Flow!=null?Flow.Model:null;}}
    DadThrowLayout layout;DadThrowStageSet stages;readonly DadThrowAim aim=new DadThrowAim();
    // The game's stages, built off the curve from the opponents the stage file declares.
    List<DadThrowStage> game;
    Font body,bold,display;Texture2D dot;
    Texture2D street,titleArt,golf,shoe,clearArt,retryArt;readonly Texture2D[] dad=new Texture2D[3];
    DadThrowStory story;Texture2D actorSheet,rivalArt,propArt;readonly Dictionary<string,Texture2D> backdropArt=new Dictionary<string,Texture2D>();
    string savePath;
    // Who was on the street last frame, to tell who has just left it, and those walking off after being hit.
    // bare: a tray-holder whose tray is gone.
    struct Seen { public int id;public float x;public DadThrowStage.Kind kind;public bool bare; }
    sealed class Leaving { public int id;public float x,age;public DadThrowStage.Kind kind;public bool bare; }
    readonly List<Seen> seen=new List<Seen>(96);readonly List<Leaving> leaving=new List<Leaving>(MaxLeaving);
    const float LeaveTime=.5f,PalmTime=.6f,KnockBack=150,KnockLift=46,ShakeTime=.3f,ShakeReach=7,ShakeGrow=.024f;const int MaxLeaving=48;float palm,shake;
    readonly Dictionary<DadThrowStage.Kind,Texture2D[]> people=new Dictionary<DadThrowStage.Kind,Texture2D[]>();
    // The same people once a shoe has taken the tray out of their hands.
    readonly Dictionary<DadThrowStage.Kind,Texture2D[]> bare=new Dictionary<DadThrowStage.Kind,Texture2D[]>();
    // The big picture an opponent comes on with.
    readonly Dictionary<DadThrowStage.Kind,Texture2D> entranceArt=new Dictionary<DadThrowStage.Kind,Texture2D>();
    // A ball stopped by a tray: a clang where it stopped, and the shoe button asks to be used for a few seconds.
    const float BlockTime=.3f,NudgeTime=4;float blockClock,shoeNudge;int seenBlock;Vector2 blockAt;
    // Words with a number in them, rebuilt only when the number changes.
    int shownCoins=-1,shownEarned=-1;string coinText="",earnedText="";
    readonly int[] shownPrice=new int[DadThrowShop.Cards.Length];readonly string[] priceText=new string[DadThrowShop.Cards.Length];
    readonly List<string> missing=new List<string>();string missingText;
    DadThrowModel.Weapon selected;DadThrowLayout.Fit fit;
    float clock,throwPose,chainClock,hintClock;
    int seenRun=-1,seenHealth,seenChain,shownRemaining=-1,screenWidth,screenHeight;
    bool thrown,shoeHintDone;string remainingText="";ScreenOrientation seenOrientation;
    readonly List<Vector2> path=new List<Vector2>(96);
    readonly List<DadThrowModel.Enemy> order=new List<DadThrowModel.Enemy>(96);
    // A crowd is a mob, not a column: each person keeps to one of five lanes across the street, a few steps up
    // or down from the feet line. This is for the eye only; every throw is still judged on the one feet line.
    static float Lane(int id){return (((id*7)%5+5)%5-2)*6f;}
    // Those in the far lanes are drawn first, and within a lane those further left, so each follower's face
    // shows over the back in front of it. The id breaks ties: two people on one spot never swap from frame to frame.
    static readonly System.Comparison<DadThrowModel.Enemy> LeftFirst=(a,b)=>{int c=Lane(a.id).CompareTo(Lane(b.id));if(c==0)c=a.x.CompareTo(b.x);return c!=0?c:a.id.CompareTo(b.id);};
    static readonly string[] ChainText=Marks(40);
    static string[] Marks(int most){var marks=new string[most+1];for(int i=0;i<=most;i++)marks[i]=i<3?"":"×"+i;return marks;}
    // From this pace up someone is running, not walking: the body tips forward and bounces with each stride.
    const float RunPace=140,RunLean=9,RunBounce=5,RunSway=5;
    static readonly Color Dust=new Color(.74f,.62f,.45f);
    ScreenOrientation oldOrientation;bool oldLeft,oldRight,oldPortrait,oldUpside;
    static readonly Color Ink=new Color(.13f,.15f,.16f),Paper=new Color(.98f,.93f,.79f),Gold=new Color(1,.77f,.13f),
        PaperDown=new Color(.87f,.82f,.68f),GoldDown=new Color(.9f,.66f,.08f),Dim=new Color(.65f,.65f,.6f),Warn=new Color(.82f,.25f,.2f),
        ResultVeil=new Color(.09f,.145f,.105f,.83f),PauseVeil=new Color(0,0,0,.6f),Olive=new Color(.42f,.39f,.21f);
    static readonly string[] Times={"","한","두","세","네","다섯"};

    void Awake()
    {
        oldOrientation=Screen.orientation;oldLeft=Screen.autorotateToLandscapeLeft;oldRight=Screen.autorotateToLandscapeRight;
        oldPortrait=Screen.autorotateToPortrait;oldUpside=Screen.autorotateToPortraitUpsideDown;
        Screen.autorotateToLandscapeLeft=Screen.autorotateToLandscapeRight=true;
        Screen.autorotateToPortrait=Screen.autorotateToPortraitUpsideDown=false;Screen.orientation=ScreenOrientation.AutoRotation;
        Application.targetFrameRate=60;
        dot=new Texture2D(32,32,TextureFormat.RGBA32,false);var px=new Color[1024];
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)px[y*32+x]=new Color(1,1,1,Mathf.Clamp01(15.5f-Vector2.Distance(new Vector2(x,y),new Vector2(15.5f,15.5f))));
        dot.SetPixels(px);dot.Apply(false,true);
        Load();
        if(missing.Count>0){missingText=string.Join("\n",missing.ToArray());Debug.LogError("Throw prototype cannot start. Missing or unreadable:\n"+missingText);}
        else
        {
            // One small file beside the game's other data.
            savePath=System.IO.Path.Combine(Application.persistentDataPath,"dad-throw-save.json");Begin();
        }
        seenOrientation=Screen.orientation;screenWidth=Screen.width;screenHeight=Screen.height;
    }
    // From the logo, with what the player's save remembers. A broken save is reported, never silently replaced.
    void Begin()
    {
        string problem;DadThrowSave save=DadThrowSave.Load(savePath,out problem);
        if(problem!=null)Debug.LogWarning("Throw prototype save: "+problem);
        aim.Cancel();seenRun=-1;Flow=new DadThrowFlow(game,layout.Field,story,save,savePath);
    }
    // Only what this scene draws is loaded, by the keys the manifests and the stage file name.
    T Need<T>(string key) where T:Object{T asset=Resources.Load<T>(key);if(asset==null)missing.Add(key);return asset;}
    string NeedText(string key){TextAsset asset=Need<TextAsset>(key);return asset!=null?asset.text:null;}
    void Load()
    {
        body=Need<Font>("Fonts/GothicA1-Regular");bold=Need<Font>("Fonts/GothicA1-Bold");display=Need<Font>("Fonts/BlackHanSans-Regular");
        string throwArt=NeedText("DadThrowV1/manifest"),wide=NeedText("DadThrowScreensV2/manifest"),result=NeedText("DadThrowScreensV1/manifest"),table=NeedText("DadThrowStages/chapter1"),tale=NeedText("DadThrowStoryV1/story");
        if(missing.Count>0)return;
        try
        {
            layout=DadThrowLayout.Parse(throwArt,wide,result);
            stages=DadThrowStageSet.Parse(table,layout.Backgrounds,layout.Field.Gate);
            game=DadThrowCurve.Build(stages.kinds,stages.stages[0].backgroundKey,stages.stages);
            story=DadThrowStory.Parse(tale);
        }
        catch(System.ArgumentException error){missing.Add(error.Message);return;}
        street=Need<Texture2D>(layout.Street.resource);titleArt=Need<Texture2D>(layout.Title.resource);
        golf=Need<Texture2D>(layout.Golf.resource);shoe=Need<Texture2D>(layout.Shoe.resource);
        clearArt=Need<Texture2D>(layout.Clear.resource);retryArt=Need<Texture2D>(layout.Retry.resource);
        dad[(int)DadThrowLayout.Pose.Ready]=Need<Texture2D>(layout.Ready.resource);dad[(int)DadThrowLayout.Pose.Windup]=Need<Texture2D>(layout.Windup.resource);
        dad[(int)DadThrowLayout.Pose.Release]=Need<Texture2D>(layout.Release.resource);
        foreach(DadThrowStage.Kind kind in stages.kinds.Values)
        {
            var frames=new Texture2D[kind.frames];for(int i=0;i<frames.Length;i++)frames[i]=Need<Texture2D>(kind.Frame(i));people[kind]=frames;
            if(kind.entrance!=null)entranceArt[kind]=Need<Texture2D>(kind.entrance.resource);
            if(kind.bare==null)continue;
            var without=new Texture2D[kind.frames];for(int i=0;i<without.Length;i++)without[i]=Need<Texture2D>(kind.BareFrame(i));bare[kind]=without;
        }
        actorSheet=Need<Texture2D>(story.actorSheet);rivalArt=Need<Texture2D>(story.rivalResource);propArt=Need<Texture2D>(story.propResource);
        foreach(var backdrop in story.backdrops)backdropArt[backdrop.Key]=Need<Texture2D>(backdrop.Value);
    }
    void OnDestroy()
    {
        if(dot!=null)Destroy(dot);Screen.autorotateToLandscapeLeft=oldLeft;Screen.autorotateToLandscapeRight=oldRight;
        Screen.autorotateToPortrait=oldPortrait;Screen.autorotateToPortraitUpsideDown=oldUpside;Screen.orientation=oldOrientation;
    }
    // Leaving the app pauses a live fight and drops the aim, so coming back never throws on its own.
    void Interrupt()
    {
#if UNITY_EDITOR
        // A capture run goes on while the editor sits in the background, where focus comes and goes.
        if(scripted)return;
#endif
        aim.Cancel();if(Flow!=null)Flow.Interrupted();
    }
    void OnApplicationPause(bool value){if(value)Interrupt();}
    void OnApplicationFocus(bool value){if(!value)Interrupt();}
    bool Upright {get{return Screen.height>Screen.width;}}

    void Update()
    {
        fit=DadThrowLayout.FitTo(Screen.safeArea,Screen.height);
        if(Flow==null)return;
        float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);clock+=dt;throwPose=Mathf.Max(0,throwPose-dt);
        palm=Mathf.Max(0,palm-dt);chainClock=Mathf.Max(0,chainClock-dt);shake=Mathf.Max(0,shake-dt/ShakeTime);
        blockClock=Mathf.Max(0,blockClock-dt);shoeNudge=selected==DadThrowModel.Weapon.Shoe?0:Mathf.Max(0,shoeNudge-dt);
        for(int i=leaving.Count-1;i>=0;i--){leaving[i].age+=dt;if(leaving[i].age>=LeaveTime)leaving.RemoveAt(i);}
        // A turned or resized screen moves every point under the finger: drop the aim, and stop the fight while held upright.
        if(Screen.orientation!=seenOrientation||Screen.width!=screenWidth||Screen.height!=screenHeight)
        {seenOrientation=Screen.orientation;screenWidth=Screen.width;screenHeight=Screen.height;aim.Cancel();}
        if(Upright)Interrupt();
        bool back=Input.GetKeyDown(KeyCode.Escape);
#if UNITY_EDITOR
        back&=!scripted;
#endif
        if(back){aim.Cancel();Flow.Back();}
        Fresh();
        if(Flow.Now!=DadThrowFlow.Screen.Playing){aim.Cancel();Flow.Tick(dt);return;}
        // An entrance holds the street still: nothing is aimed, and a tap anywhere lets the fight begin.
        if(Flow.Entering!=null){aim.Cancel();if(Tapped())Flow.SkipEntrance();Flow.Tick(dt);Fresh();return;}
        Pointer();Keys();
        // After the opening the street waits for the first pull; hints count fight time only, so neither that
        // wait nor a pause uses them up.
        if(aim.Active)Flow.AimStarted();
        if(!Flow.WaitingForAim)hintClock+=dt;
        Flow.Tick(dt*(aim.Active?Flow.Model.Rules.AimPace:1));
        DadThrowModel m=Flow.Model;
        if(m.Health<seenHealth){palm=PalmTime;shake=1;}
        seenHealth=m.Health;Track(m);
        // A ball through a pack jolts the street, more the more it went through, but never as hard as a touch on
        // the gate: balls land every third of a second, and the street must not shake itself unreadable.
        if(m.ChainSerial!=seenChain){seenChain=m.ChainSerial;chainClock=.6f;shake=Mathf.Max(shake,Mathf.Min(.65f,.08f*m.ChainCount));}
        // A tray stopped a ball: no words, the shoe button lights up instead.
        if(m.BlockSerial!=seenBlock){seenBlock=m.BlockSerial;blockClock=BlockTime;blockAt=m.BlockAt;if(selected==DadThrowModel.Weapon.Golf)shoeNudge=NudgeTime;}
        if(Flow.Now!=DadThrowFlow.Screen.Playing)aim.Cancel();
    }
    // A fresh stage: nothing from the last attempt may leak into it. A button starts the stage while the screen
    // is being drawn, so drawing asks too, before the next Update comes round.
    void Fresh()
    {
        if(Flow.Model==null||Flow.Runs==seenRun)return;
        seenRun=Flow.Runs;seenHealth=Flow.Model.Health;seenChain=Flow.Model.ChainSerial;seenBlock=Flow.Model.BlockSerial;blockClock=shoeNudge=0;
        thrown=false;hintClock=throwPose=palm=chainClock=shake=0;shownRemaining=-1;aim.Cancel();seen.Clear();leaving.Clear();
    }
    // Someone who is no longer on the street either touched the gate or was hit. A hit knocks them back the way
    // they came, off their feet for a moment: out of this turn, nothing worse. A ball can clear five at once and
    // balls follow each other every half second, so many are on their way out together.
    void Track(DadThrowModel m)
    {
        foreach(Seen was in seen)
        {
            bool still=false;foreach(DadThrowModel.Enemy e in m.enemies)if(e.id==was.id){still=true;break;}
            if(still||was.kind==null||was.x<=m.Field.Gate+8)continue;
            if(leaving.Count>=MaxLeaving)leaving.RemoveAt(0);
            leaving.Add(new Leaving{id=was.id,x=was.x,kind=was.kind,bare=was.bare});
        }
        seen.Clear();foreach(DadThrowModel.Enemy e in m.enemies)seen.Add(new Seen{id=e.id,x=e.x,kind=e.kind,bare=e.kind!=null&&e.kind.guards&&!e.guard});
    }
#if UNITY_EDITOR
    // Editor captures only (DadThrowRemote): these stand in for a finger so a screen can be photographed mid-gesture.
    // While one is held, or while a capture script drives the game, real pointers, keys and button presses are not read.
    bool heldForCapture,scripted;
    public void DebugScripted(bool on){scripted=on;aim.Cancel();heldForCapture=false;}
    // A new player in memory only: captures never read or write the real save.
    public void DebugRestart(){aim.Cancel();heldForCapture=false;seenRun=-1;Flow=new DadThrowFlow(game,layout.Field,story,new DadThrowSave(),null);}
    // A player in memory only who has opened stage `index` (from 0) and has `coins` in the purse.
    public void DebugStartAt(int index,int coins)
    {aim.Cancel();heldForCapture=false;seenRun=-1;Flow=new DadThrowFlow(game,layout.Field,story,new DadThrowSave{introSeen=true,furthest=index,coins=coins},null);}
    // Back to the player's own save, from the logo, when a capture run is over.
    public void DebugReal(){heldForCapture=false;if(Flow!=null)Begin();}
    public string DebugSavePath {get{return savePath;}}
    public void DebugSelect(DadThrowModel.Weapon weapon){aim.Cancel();selected=weapon;}
    public bool DebugAim(Vector2 start,Vector2 now)
    {
        aim.Cancel();heldForCapture=Flow!=null&&Flow.Model!=null&&aim.Begin(DadThrowAim.Mouse,start,Flow.Model.CanFire);
        if(heldForCapture)aim.Move(DadThrowAim.Mouse,now);
        return heldForCapture;
    }
    public void DebugRelease(){if(heldForCapture)Release(DadThrowAim.Mouse,aim.Now);heldForCapture=false;}
    public string DebugMissing {get{return missingText;}}
#endif
    // One pointer owns an aim. Touches are read by finger id; the mouse is used only when no finger is down.
    void Pointer()
    {
#if UNITY_EDITOR
        if(heldForCapture||scripted)return;
#endif
        DadThrowModel m=Flow.Model;
        if(Input.touchCount>0)
        {
            bool ownerSeen=false;
            for(int i=0;i<Input.touchCount;i++)
            {
                Touch touch=Input.GetTouch(i);Vector2 p=fit.ToCanvas(touch.position,Screen.height);
                if(aim.Active&&touch.fingerId==aim.Finger)
                {
                    ownerSeen=true;
                    if(touch.phase==TouchPhase.Canceled)aim.Cancel();
                    else if(touch.phase==TouchPhase.Ended)Release(touch.fingerId,p);
                    else aim.Move(touch.fingerId,p);
                }
                else if(!aim.Active&&touch.phase==TouchPhase.Began&&aim.Begin(touch.fingerId,p,m.CanFire))ownerSeen=true;
            }
            // The owning finger is gone without an end event: nothing is thrown.
            if(aim.Active&&!ownerSeen)aim.Cancel();
            return;
        }
        if(aim.Active&&aim.Finger!=DadThrowAim.Mouse){aim.Cancel();return;}
        Vector2 mouse=fit.ToCanvas(Input.mousePosition,Screen.height);
        if(Input.GetMouseButtonDown(0))aim.Begin(DadThrowAim.Mouse,mouse,m.CanFire);
        else if(Input.GetMouseButtonUp(0))Release(DadThrowAim.Mouse,mouse);
        else if(Input.GetMouseButton(0))aim.Move(DadThrowAim.Mouse,mouse);
        else aim.Cancel();
    }
    // A finger went down or the mouse was pressed this frame.
    bool Tapped()
    {
#if UNITY_EDITOR
        if(heldForCapture||scripted)return false;
#endif
        for(int i=0;i<Input.touchCount;i++)if(Input.GetTouch(i).phase==TouchPhase.Began)return true;
        return Input.GetMouseButtonDown(0);
    }
    void Select(DadThrowModel.Weapon weapon)
    {if(selected==weapon)return;selected=weapon;aim.Cancel();if(weapon==DadThrowModel.Weapon.Shoe)shoeHintDone=true;}
    // With a keyboard: 1 the ball, 2 the shoe, space or tab the other one. A phone has only the two buttons.
    void Keys()
    {
#if UNITY_EDITOR
        if(heldForCapture||scripted)return;
#endif
        if(Input.GetKeyDown(KeyCode.Alpha1))Select(DadThrowModel.Weapon.Golf);
        else if(Input.GetKeyDown(KeyCode.Alpha2))Select(DadThrowModel.Weapon.Shoe);
        else if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Tab))Select(selected==DadThrowModel.Weapon.Golf?DadThrowModel.Weapon.Shoe:DadThrowModel.Weapon.Golf);
    }
    void Release(int finger,Vector2 point)
    {
        Vector2 pull;if(!aim.End(finger,point,out pull))return;
        // A short tap fails here and costs no reload.
        if(Flow.Model.Fire(selected,pull)){throwPose=DadThrowLayout.ReleaseTime;thrown=true;}
    }

    // ── drawing helpers ──
    void Box(Rect r,Color c){DadUi.Fill(r,c,0);}
    void Text(Rect r,string value,int size,Color color,Font font,TextAnchor anchor=TextAnchor.MiddleCenter){DadUi.Text(r,value,font,size,color,anchor);}
    // Lettering that sits on a picture needs an edge to stay readable on sky, wall and road alike.
    void Outlined(Rect r,string value,int size,Color fill,Color edge,float width,Font font)
    {
        // A wide edge needs more copies around the letter, or its corners show as steps.
        int copies=width>=3?16:8;
        for(int i=0;i<copies;i++)
        {
            float angle=i*Mathf.PI*2/copies;Rect o=r;o.x+=Mathf.Cos(angle)*width;o.y+=Mathf.Sin(angle)*width;
            Text(o,value,size,edge,font);
        }
        Text(r,value,size,fill,font);
    }
    // The face drops a little the moment it is touched, so a press is seen before the screen changes.
    Rect Face(Rect r,bool enabled)
    {
#if UNITY_EDITOR
        if(scripted)return r;
#endif
        return enabled&&DadUi.Pressed(r)?new Rect(r.x,r.y+3,r.width,r.height):r;
    }
    bool Click(Rect r,bool enabled)
    {
#if UNITY_EDITOR
        if(scripted)return false;
#endif
        bool was=GUI.enabled;GUI.enabled=was&&enabled;bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=was;return clicked;}
    bool Button(Rect r,string label,bool primary,int size=28,bool enabled=true)
    {
        enabled&=GUI.enabled;Rect face=Face(r,enabled);bool down=face.y!=r.y;
        DadUi.Panel(face,!enabled?Dim:primary?(down?GoldDown:Gold):(down?PaperDown:Paper),Ink);Text(face,label,size,Ink,bold);
        return Click(r,enabled);
    }
    // Weapon button with the thing's own picture. The dark cover shrinks as the hand gets ready again.
    bool WeaponButton(Rect r,string label,Texture2D picture,DadThrowLayout.Sprite sprite,bool active,float reload,bool nudge)
    {
        bool enabled=GUI.enabled;Rect face=Face(r,enabled);
        if(nudge){float grow=3+2*Mathf.Sin(clock*6);DadUi.Fill(new Rect(face.x-grow,face.y-grow,face.width+grow*2,face.height+grow*2),Gold,12);}
        DadUi.Panel(face,active?Gold:Paper,Ink);
        GUI.DrawTexture(layout.ItemRect(sprite,DadThrowLayout.IconSlot(face).center,DadThrowLayout.IconSize),picture);
        Text(DadThrowLayout.LabelSlot(face),label,26,Ink,bold);
        if(reload>0)DadUi.Fill(new Rect(face.x,face.y,face.width*reload,face.height),new Color(Ink.r,Ink.g,Ink.b,.45f),10);
        return Click(r,enabled);
    }
    void Line(Vector2 a,Vector2 b,float width,Color color)
    {
        Matrix4x4 old=GUI.matrix;Vector2 d=b-a;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);
        Box(new Rect(0,-width/2,d.magnitude,width),color);GUI.matrix=old;
    }
    void Spun(Texture2D picture,Rect rect,Vector2 center,float degrees)
    {
        Matrix4x4 old=GUI.matrix;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(center.x,center.y,0),Quaternion.Euler(0,0,degrees),Vector3.one)*Matrix4x4.TRS(new Vector3(-center.x,-center.y,0),Quaternion.identity,Vector3.one);
        GUI.DrawTexture(rect,picture);GUI.matrix=old;
    }
    void Thing(DadThrowModel.Weapon weapon,Vector2 center,float degrees)
    {
        if(weapon==DadThrowModel.Weapon.Golf)Spun(golf,layout.GolfRect(center),center,degrees);
        else Spun(shoe,layout.ShoeRect(center),center,degrees);
    }
    void Dot(Vector2 p,float size,Color color){GUI.color=color;GUI.DrawTexture(new Rect(p.x-size/2,p.y-size/2,size,size),dot);GUI.color=Color.white;}
    // Whatever is not the canvas is covered last, so people still outside the right edge never show on a long screen.
    void Letterbox(Color color)
    {
        float left=-fit.offset.x/fit.scale,top=-fit.offset.y/fit.scale,width=Screen.width/fit.scale,height=Screen.height/fit.scale;
        Box(new Rect(left,top,-left,height),color);Box(new Rect(DadThrowLayout.W,top,left+width-DadThrowLayout.W,height),color);
        Box(new Rect(0,top,DadThrowLayout.W,-top),color);Box(new Rect(0,DadThrowLayout.H,DadThrowLayout.W,top+height-DadThrowLayout.H),color);
    }

    // ── battle ──
    // A touch on the gate: the door lights up and a palm print fades on it. Nothing breaks.
    void DrawGate()
    {
        if(palm<=0)return;
        float t=palm/PalmTime;Rect door=layout.GateDoor;
        Box(door,new Color(Gold.r,Gold.g,Gold.b,.45f*t));
        Vector2 c=door.center;Color edge=new Color(Ink.r,Ink.g,Ink.b,t),skin=new Color(Paper.r,Paper.g,Paper.b,t);
        // Palm, four fingers and a thumb, each a rounded block with a dark edge.
        Rect[] parts={new Rect(c.x-9,c.y-4,18,18),new Rect(c.x-9,c.y-17,4,15),new Rect(c.x-4,c.y-20,4,18),new Rect(c.x+1,c.y-19,4,17),new Rect(c.x+6,c.y-15,4,13),new Rect(c.x-15,c.y,8,5)};
        foreach(Rect part in parts)DadUi.Fill(new Rect(part.x-1.5f,part.y-1.5f,part.width+3,part.height+3),edge,3);
        foreach(Rect part in parts)DadUi.Fill(part,skin,2);
    }
    // Legs keep time with the pace: someone running at 160 steps three times as often as someone strolling at 50.
    // `toRight` is the way the person is heading: challengers come from the right, those sent back go right.
    void Person(DadThrowStage.Kind kind,int id,float x,float ground,bool mirror,float speed,bool toRight=false,bool noTray=false)
    {
        bool runs=speed>=RunPace;float stride=clock*Mathf.Max(48,speed)/8+id;
        ground+=Lane(id);
        if(runs)
        {
            // Nobody in a herd holds a line: each drifts a little, and kicks up the sand behind the heels.
            x+=Mathf.Sin(clock*2.3f+id*1.7f)*RunSway;
            if(x<DadThrowLayout.W+40)
            {
                float puff=stride-Mathf.Floor(stride);Color was=GUI.color;
                GUI.color=new Color(Dust.r,Dust.g,Dust.b,.5f*(1-puff)*was.a);float size=8+12*puff;
                GUI.DrawTexture(new Rect(x+(toRight?-26:26)*(1+puff*.6f)-size/2,ground-3-size/2-puff*6,size,size),dot);GUI.color=was;
            }
            ground-=Mathf.Abs(Mathf.Sin(stride*Mathf.PI))*RunBounce;
        }
        Rect r=kind.Place(x,ground,kind.Height(id));if(r.x>DadThrowLayout.W+60)return;
        Texture2D[] frames;if(!noTray||!bare.TryGetValue(kind,out frames))frames=people[kind];
        Texture2D frame=frames[(int)stride%frames.Length];
        if(!mirror&&!runs){GUI.DrawTexture(r,frame);return;}
        Matrix4x4 old=GUI.matrix,turn=Matrix4x4.TRS(new Vector3(x,ground,0),Quaternion.Euler(0,0,runs?(toRight?RunLean:-RunLean):0),new Vector3(mirror?-1:1,1,1));
        GUI.matrix=old*turn;GUI.DrawTexture(new Rect(r.x-x,r.y-ground,r.width,r.height),frame);GUI.matrix=old;
    }
    void DrawPeople(DadThrowModel m)
    {
        // Those sent back turn round, so their picture is mirrored the other way: one hop backwards, legs going,
        // fading over its second half. A ring of sparks marks where each was hit.
        foreach(Leaving gone in leaving)
        {
            float t=gone.age/LeaveTime;GUI.color=new Color(1,1,1,Mathf.Clamp01(2-2*t));
            Person(gone.kind,gone.id,gone.x+KnockBack*t,m.Field.Ground-Mathf.Sin(t*Mathf.PI)*KnockLift,!gone.kind.Mirror,200,true,gone.bare);
            if(t>=.36f)continue;
            GUI.color=Color.white;Vector2 chest=new Vector2(gone.x,m.Field.Ground+Lane(gone.id)-58);float reach=16+110*t;
            for(int i=0;i<6;i++){Vector2 spark=chest+new Vector2(Mathf.Cos(i*1.047f+gone.id),Mathf.Sin(i*1.047f+gone.id))*reach;Dot(spark,11,Ink);Dot(spark,7,Gold);}
        }
        order.Clear();order.AddRange(m.enemies);order.Sort(LeftFirst);
        foreach(DadThrowModel.Enemy e in order)
        {
            if(e.kind==null)continue;
            GUI.color=e.flash>0?new Color(1,.6f,.45f):Color.white;
            Person(e.kind,e.id,e.x,m.Field.Ground,e.kind.Mirror,e.speed,false,e.kind.guards&&!e.guard);
        }
        GUI.color=Color.white;
    }
    void DrawDad()
    {
        DadThrowLayout.Pose pose=DadThrowLayout.PoseFor(aim.Active,throwPose);
        GUI.DrawTexture(layout.DadRect(layout.Dad(pose)),dad[(int)pose]);
        // While pulling back, the chosen thing sits in the raised hand: the picture itself says what will fly.
        if(pose==DadThrowLayout.Pose.Windup)Thing(selected,layout.WindupHand,selected==DadThrowModel.Weapon.Shoe?-35:0);
    }
    void DrawFlight(DadThrowModel m)
    {
        foreach(DadThrowModel.Projectile p in m.projectiles)Thing(p.weapon,p.pos,p.life*(p.weapon==DadThrowModel.Weapon.Golf?540:420));
        // A ball through three or more people gets a mark, bigger the more it went through.
        if(chainClock>0&&m.ChainCount>=3)
        {
            Vector2 at=m.ChainAt;float rise=(.6f-chainClock)*60;int size=Mathf.Min(60,36+8*(m.ChainCount-3));
            Outlined(new Rect(at.x-90,at.y-96-rise,180,70),ChainText[Mathf.Min(m.ChainCount,ChainText.Length-1)],size,Gold,Ink,3,display);
        }
    }
    // Where a tray stopped a ball: a ring of pale sparks, smaller and quicker than the ones a hit throws up.
    void DrawBlock()
    {
        if(blockClock<=0)return;
        float t=1-blockClock/BlockTime;
        for(int i=0;i<8;i++){Vector2 spark=blockAt+new Vector2(Mathf.Cos(i*.785f),Mathf.Sin(i*.785f))*(10+46*t);Dot(spark,10,Ink);Dot(spark,6,Color.white);}
    }
    void DrawAim(DadThrowModel m)
    {
        if(!aim.Active)return;
        m.Predict(selected,aim.Pull,path);
        // Dots from the hand to the first landing only. Dark-edged so they stay visible on the pale wall and the sky.
        int last=path.Count-1;
        for(int i=3;i<last;i+=3){Dot(path[i],10,Ink);Dot(path[i],6,Color.white);}
        if(last>0){Dot(path[last],16,Ink);Dot(path[last],10,Color.white);}
        // The pull line is drawn under the finger, from where the drag began, so it never runs across dad's face.
        Vector2 end=aim.Start-Vector2.ClampMagnitude(aim.Pull,150);
        Line(aim.Start,end,5,Ink);Dot(end,18,Ink);
    }
    // The top bar: which group this is, the gate touches it has left, how many are still to come.
    void DrawTopBar(DadThrowStage stage,int touches,int remaining)
    {
        Box(new Rect(0,0,DadThrowLayout.W,DadThrowLayout.TopBar),Paper);Box(new Rect(0,DadThrowLayout.TopBar-3,DadThrowLayout.W,3),Ink);
        Text(DadThrowLayout.StageLabel,stage.id,34,Ink,display);
        // The cells are the gate touches this group of challengers may still make.
        Text(DadThrowLayout.HealthLabel,"대문",26,Ink,bold);
        for(int i=0;i<DadThrowLayout.HealthCells;i++)
        {
            bool full=i<touches;Color fill=!full?PaperDown:touches==1?Color.Lerp(Warn,Gold,.5f+.5f*Mathf.Sin(clock*5)):Gold;
            DadUi.Panel(DadThrowLayout.HealthCell(i),fill,Ink);
        }
        if(remaining!=shownRemaining){shownRemaining=remaining;remainingText="남은 도전자 "+shownRemaining;}
        Text(DadThrowLayout.RemainingLabel,remainingText,24,Ink,bold,TextAnchor.MiddleRight);
    }
    // The purse: a coin and the count. In a fight it goes up with every challenger sent back.
    void DrawPurse(Rect r,int size,Color color,Font font)
    {
        if(Flow.Coins!=shownCoins){shownCoins=Flow.Coins;coinText=shownCoins+"원";}
        Vector2 coin=new Vector2(r.x+r.height/2,r.center.y);float wide=r.height*.62f;
        Dot(coin,wide+6,Ink);Dot(coin,wide,Gold);
        Text(new Rect(r.x+r.height+4,r.y,r.width-r.height-4,r.height),coinText,size,color,font,TextAnchor.MiddleLeft);
    }
    // What this stage paid, over the top edge of the result card.
    void DrawEarned()
    {
        if(Flow.Earned!=shownEarned){shownEarned=Flow.Earned;earnedText="+"+shownEarned+"원";}
        Outlined(DadThrowLayout.ResultEarned,earnedText,36,Gold,Ink,3,display);
    }
    // Pause and the two things to throw. `chosen` is the thing shown as picked.
    void DrawControls(DadThrowStage stage,float reload,DadThrowModel.Weapon chosen)
    {
        Rect pause=Face(DadThrowLayout.PauseButton,GUI.enabled);float drop=pause.y-DadThrowLayout.PauseButton.y;
        DadUi.Panel(pause,drop>0?PaperDown:Paper,Ink);
        for(int i=0;i<2;i++){Rect bar=DadThrowLayout.PauseBar(i);bar.y+=drop;DadUi.Fill(bar,Ink,3);}
        if(Click(DadThrowLayout.PauseButton,GUI.enabled)){aim.Cancel();Flow.Pause();}
        bool nudge=(stage.hint=="shoe"&&!shoeHintDone&&hintClock<6||shoeNudge>0)&&GUI.enabled;
        // Switching things while aiming drops the aim and changes only the choice.
        if(WeaponButton(DadThrowLayout.GolfButton,"골프공",golf,layout.Golf,chosen==DadThrowModel.Weapon.Golf,reload,false))Select(DadThrowModel.Weapon.Golf);
        if(WeaponButton(DadThrowLayout.ShoeButton,"신발",shoe,layout.Shoe,chosen==DadThrowModel.Weapon.Shoe,reload,nudge))Select(DadThrowModel.Weapon.Shoe);
        if(stage.hint=="shoe"&&hintClock>=6)shoeHintDone=true;
        // First stage only: gone after the first real throw, or after six seconds of the fight either way.
        if(stage.hint=="pull"&&!thrown&&hintClock<6&&GUI.enabled)Outlined(DadThrowLayout.Hint,"당겨서 놓으세요",30,Paper,Ink,3,display);
    }
    void DrawBattle()
    {
        Fresh();DadThrowModel m=Flow.Model;DadThrowFlow.Screen now=Flow.Now;
        bool entering=now==DadThrowFlow.Screen.Playing&&Flow.Entering!=null;
        bool live=now==DadThrowFlow.Screen.Playing&&!entering,result=now==DadThrowFlow.Screen.Clear||now==DadThrowFlow.Screen.Defeat;
        // The street jolts on a big hit or a touch on the gate. It is drawn a touch larger while it does, so no
        // edge of the picture shows; the bar, the buttons and the aim line stay where the finger expects them.
        Matrix4x4 still=GUI.matrix;
        if(live&&shake>0)
        {
            float k=shake*shake,grow=1+ShakeGrow*k;Vector2 push=new Vector2(Mathf.Sin(clock*71),Mathf.Cos(clock*83))*ShakeReach*k;
            GUI.matrix=still*Matrix4x4.TRS(new Vector3(push.x+DadThrowLayout.W/2*(1-grow),push.y+DadThrowLayout.H/2*(1-grow),0),Quaternion.identity,new Vector3(grow,grow,1));
        }
        GUI.DrawTexture(layout.Background(),street);
        DrawGate();DrawPeople(m);
        // On a result the big pose beside the card stands in for dad, so the small one is left out.
        if(!result){DrawDad();DrawFlight(m);DrawBlock();}
        GUI.matrix=still;
        if(live)DrawAim(m);
        // Nothing under a card can be pressed.
        bool was=GUI.enabled;GUI.enabled=was&&live;
        DrawTopBar(Flow.Stage,m.Health,m.Remaining);DrawPurse(DadThrowLayout.CoinLabel,26,Ink,bold);DrawControls(Flow.Stage,m.ReloadTotal>0?Mathf.Clamp01(m.Reload/m.ReloadTotal):0,selected);GUI.enabled=was;
        if(entering)DrawEntrance();
        if(now==DadThrowFlow.Screen.Paused)DrawPause();
        else if(now==DadThrowFlow.Screen.Clear)DrawClear();
        else if(now==DadThrowFlow.Screen.Defeat)DrawDefeat();
    }
    void Veil(Color color){Box(new Rect(0,0,DadThrowLayout.W,DadThrowLayout.H),color);}
    // A new opponent comes on: the street dims, a slanted band crosses it, the man himself slides in large from
    // the right, his name and his line stand on the band, and under it one line says what to do about him.
    void DrawEntrance()
    {
        DadThrowStage.Kind kind=Flow.Entering;DadThrowStage.Kind.Entrance words=kind.entrance;float t=Flow.EnterClock;
        float arrive=1-Mathf.Pow(1-Mathf.Clamp01(t/DadThrowLayout.EntranceIn),3);
        Veil(PauseVeil);
        Matrix4x4 old=GUI.matrix;Vector3 middle=new Vector3(DadThrowLayout.W/2,DadThrowLayout.H/2,0);
        GUI.matrix=old*Matrix4x4.TRS(middle,Quaternion.Euler(0,0,DadThrowLayout.EntranceTilt),new Vector3(1,Mathf.Max(.02f,arrive),1))*Matrix4x4.TRS(-middle,Quaternion.identity,Vector3.one);
        Rect band=DadThrowLayout.EntranceBand;Box(band,Ink);Box(new Rect(band.x,band.y+8,band.width,band.height-16),Gold);
        GUI.matrix=old;
        // He lands with a small jolt: a touch too large for a moment, then his own size.
        float pop=1+.06f*Mathf.Clamp01(1-(t-DadThrowLayout.EntranceIn)/.18f)*(t>=DadThrowLayout.EntranceIn?1:0);
        GUI.DrawTexture(kind.EntrancePlace(DadThrowLayout.EntranceFeet.x+(1-arrive)*DadThrowLayout.EntranceSlide,DadThrowLayout.EntranceFeet.y,DadThrowLayout.EntranceTall*pop),entranceArt[kind]);
        if(t<DadThrowLayout.EntranceIn)return;
        Outlined(DadThrowLayout.EntranceName,words.name,76,Paper,Ink,4,display);
        DadUi.Panel(DadThrowLayout.EntranceShout,Paper,Ink);Text(DadThrowLayout.EntranceShout,words.shout,34,Ink,display);
        // What to do about him, with the thing to do it with.
        DadUi.Panel(DadThrowLayout.EntranceHint,Gold,Ink);
        GUI.DrawTexture(layout.ItemRect(layout.Shoe,DadThrowLayout.EntranceHintIcon,92),shoe);
        Text(DadThrowLayout.EntranceHintText,words.hint,29,Ink,bold,TextAnchor.MiddleLeft);
        if(t>=DadThrowFlow.EnterSkip)Outlined(DadThrowLayout.EntranceSkip,"화면을 누르면 시작",22,Paper,Ink,2,bold);
    }
    void DrawPause()
    {
        Veil(PauseVeil);DadUi.Panel(DadThrowLayout.PauseCard,Paper,Ink);
        Text(DadThrowLayout.PauseHeading,"잠깐만!",46,Ink,display);
        if(Button(DadThrowLayout.PauseResume,"계속하기",true))Flow.Resume();
        if(Button(DadThrowLayout.PauseTitle,"타이틀",false))Flow.ToTitle();
    }
    void DrawClear()
    {
        Veil(ResultVeil);GUI.DrawTexture(layout.HeroRect(layout.Clear),clearArt);DadUi.Panel(DadThrowLayout.ResultCard,Paper,Ink);DrawEarned();
        // A stage is one group of challengers. After the last one that exists there is nowhere further to go, and the card says so.
        Text(DadThrowLayout.ClearHeading,Flow.HasNext?"이번 조 끝!":"여기까지 막았다!",46,Ink,display);
        if(Flow.Word!=null)
        {
            Text(DadThrowLayout.ClearWordName,Flow.Word.name,20,Olive,display);
            Text(DadThrowLayout.ClearWordLine,Flow.Word.line,24,Ink,body,TextAnchor.UpperCenter);
        }
        // The lesser choice is the shop now; the title is reached from the pause.
        if(Button(DadThrowLayout.ClearLeft,"상점",false)){Flow.OpenShop();return;}
        if(Flow.HasNext){if(Button(DadThrowLayout.ClearRight,"다음",true))Flow.Next();}
        else if(Button(DadThrowLayout.ClearRight,"다시 하기",true))Flow.Retry();
    }
    void DrawDefeat()
    {
        Veil(ResultVeil);GUI.DrawTexture(layout.HeroRect(layout.Retry),retryArt);DadUi.Panel(DadThrowLayout.ResultCard,Paper,Ink);DrawEarned();
        Text(DadThrowLayout.DefeatHeading,"다시 해볼까?",46,Ink,display);
        int touches=Flow.Stage.health;
        Text(DadThrowLayout.DefeatLine,"대문을 "+(touches<Times.Length?Times[touches]:touches.ToString())+" 번 만졌어요",24,Ink,body);
        if(Button(DadThrowLayout.DefeatRetry,"다시 도전",true,30))Flow.Retry();
        // A lost stage still paid for everyone sent back: the shop is one tap away.
        if(Button(DadThrowLayout.DefeatShop,"상점",false,24)){Flow.OpenShop();return;}
        if(Button(DadThrowLayout.DefeatTitle,"타이틀",false,24))Flow.ToTitle();
    }

    // ── opening and ending ──
    void DrawStory(DadThrowStory.Cut cut)
    {
        if(cut.battle)
        {
            // The fight's own camera, dad already winding up with the slipper: the story ends on the picture the
            // first pull starts from, so neither the gate nor dad moves when the fight begins. The top bar shows
            // here for the first time; the buttons wait for the fight, where the dialogue box is gone.
            DadThrowStage stage=Flow.Stage;GUI.DrawTexture(layout.Background(),street);
            GUI.DrawTexture(layout.DadRect(layout.Windup),dad[(int)DadThrowLayout.Pose.Windup]);Thing(DadThrowModel.Weapon.Shoe,layout.WindupHand,-35);
            DrawTopBar(stage,stage.health,stage.Total);
        }
        else
        {
            Texture2D backdrop=backdropArt[cut.backdrop];
            GUI.DrawTexture(new Rect(0,0,DadThrowLayout.W,DadThrowLayout.W*backdrop.height/backdrop.width),backdrop);
            foreach(string who in cut.actors){DadThrowStory.Actor actor=story.actors[who];GUI.DrawTextureWithTexCoords(story.ActorRect(actor,cut),actorSheet,actor.uv);}
            if(cut.rival)GUI.DrawTexture(DadThrowStory.Rival,rivalArt);
            if(cut.prop)GUI.DrawTexture(DadThrowStory.Prop,propArt);
        }
        // Words are drawn, never painted into the pictures: a name and one line at the bottom, skip at the top.
        DadUi.Fill(DadThrowStory.Dialogue,Ink,6);Rect inner=DadThrowStory.Dialogue;
        DadUi.Fill(new Rect(inner.x+3,inner.y+3,inner.width-6,inner.height-6),new Color(Paper.r,Paper.g,Paper.b,.95f),4);
        Text(DadThrowStory.DialogueName,cut.name,20,Olive,display,TextAnchor.MiddleLeft);
        Text(DadThrowStory.DialogueLine,cut.line,27,Ink,body,TextAnchor.UpperLeft);
        if(Button(DadThrowStory.Skip,"건너뛰기",false,24)){aim.Cancel();Flow.Skip();}
    }

    // ── other screens ──
    void DrawBoot()
    {
        Box(new Rect(0,0,DadThrowLayout.W,DadThrowLayout.H),Color.black);
        Text(new Rect(0,300,DadThrowLayout.W,120),"Bluemushroom studio",54,new Color(1,1,1,Flow.BootAlpha),display);
    }
    void DrawTitle()
    {
        // The key art already holds dad and the neighbours; nothing else is placed over them.
        GUI.DrawTexture(layout.TitleArt(),titleArt);
        Outlined(DadThrowLayout.TitleText,"아빠 전쟁",94,Gold,Ink,4,display);
        if(Button(DadThrowLayout.StartButton,Flow.Continuing?"계속하기":"게임 시작",true,30)){Flow.Start();return;}
        DrawPurse(DadThrowLayout.ShopPurse,22,Paper,bold);
        if(Button(DadThrowLayout.ShopButton,"상점",false,26))Flow.OpenShop();
    }
    // The shop: nine things, each with a cell for every time it can be bought and the price of the next one.
    void DrawShop()
    {
        GUI.DrawTexture(layout.TitleArt(),titleArt);Veil(ResultVeil);
        Text(DadThrowLayout.ShopHeading,"상점",48,Paper,display,TextAnchor.MiddleLeft);
        DrawPurse(DadThrowLayout.ShopCoins,36,Gold,display);
        if(Button(DadThrowLayout.ShopClose,"닫기",true,28)){Flow.CloseShop();return;}
        for(int i=0;i<DadThrowShop.Cards.Length;i++)
        {
            DadThrowShop.Card card=DadThrowShop.Cards[i];Rect slot=DadThrowLayout.ShopCard(i);
            DadUi.Panel(slot,Paper,Ink);Vector2 icon=DadThrowLayout.ShopIcon(slot);
            if(card.shoe)GUI.DrawTexture(layout.ItemRect(layout.Shoe,icon,80),shoe);else GUI.DrawTexture(layout.ItemRect(layout.Golf,icon,56),golf);
            Text(DadThrowLayout.ShopName(slot),card.name,28,Ink,display,TextAnchor.MiddleLeft);
            Text(DadThrowLayout.ShopLine(slot),card.line,19,Ink,body,TextAnchor.UpperLeft);
            int level=DadThrowShop.Level(Flow.Save,card.item);
            for(int pip=0;pip<card.most;pip++)DadUi.Panel(DadThrowLayout.ShopPip(slot,pip),pip<level?Gold:PaperDown,Ink);
            if(level>=card.most){Text(DadThrowLayout.ShopBuy(slot),"다 샀다",22,Olive,bold);continue;}
            int price=DadThrowShop.Price(Flow.Save,card.item);
            if(price!=shownPrice[i]){shownPrice[i]=price;priceText[i]=price+"원";}
            // A price out of reach is shown greyed and cannot be pressed.
            if(Button(DadThrowLayout.ShopBuy(slot),priceText[i],true,24,DadThrowShop.CanBuy(Flow.Save,card.item)))Flow.Buy(card.item);
        }
    }
    void DrawMissing()
    {
        Box(new Rect(0,0,DadThrowLayout.W,DadThrowLayout.H),Color.black);
        Text(new Rect(100,90,1080,70),"필요한 파일을 찾지 못했습니다",36,Color.white,display);
        Text(new Rect(100,180,1080,480),missingText,22,Color.white,body,TextAnchor.UpperLeft);
    }
    void DrawTurn()
    {
        Box(new Rect(0,0,DadThrowLayout.W,DadThrowLayout.H),Color.black);
        Text(new Rect(0,300,DadThrowLayout.W,120),"화면을 가로로 돌려 주세요",64,Color.white,display);
    }
    void OnGUI()
    {
        fit=DadThrowLayout.FitTo(Screen.safeArea,Screen.height);Matrix4x4 old=GUI.matrix;Color tint=GUI.color;
        try
        {
            GUI.matrix=Matrix4x4.TRS(fit.offset,Quaternion.identity,new Vector3(fit.scale,fit.scale,1));GUI.color=Color.white;
            Color bars=Ink;
            if(Flow==null){DrawMissing();bars=Color.black;}
            else if(Upright){DrawTurn();bars=Color.black;}
            else if(Flow.Now==DadThrowFlow.Screen.Boot){DrawBoot();bars=Color.black;}
            else if(Flow.Now==DadThrowFlow.Screen.Title)DrawTitle();
            else if(Flow.Now==DadThrowFlow.Screen.Shop)DrawShop();
            else if(Flow.Now==DadThrowFlow.Screen.Intro||Flow.Now==DadThrowFlow.Screen.Ending)DrawStory(Flow.Cut);
            else DrawBattle();
            Letterbox(bars);
        }
        finally{GUI.color=tint;GUI.matrix=old;}
    }
}
