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
    // Set for the editor session while the remote's Play Mode runs (DadThrowRemote): the save on disk is not written.
    public const string RemoteKey="DadThrowRemotePlay";
    public DadThrowModel Model {get{return Flow!=null?Flow.Model:null;}}
    DadThrowLayout layout;DadThrowStageSet stages;readonly DadThrowAim aim=new DadThrowAim();
    // The game's stages, built off the curve from the opponents the stage file declares, and the tuner that
    // bends them a little toward how the player is doing.
    List<DadThrowStage> game;DadThrowTuner tuner;
    // Today's challenge (DadThrowDaily), and its words, rebuilt only when what they say changes.
    DadThrowDaily daily;int shownDailyDay=-1,shownDailyStage=-1,shownDailyCards=-1,shownDailyRun=-1,shownTries=-1;
    string dailyStageText="",dailyChipText="",dailyResultText="",dailyTriesText="";
    static readonly string[] TierName={"깨기","대문 한 번 이하","한 공에 "+DadThrowDaily.ChainGoal+"명"},
        TierPay={"+"+DadThrowDaily.ClearWon+"원","+"+DadThrowDaily.GateWon+"원","+"+DadThrowDaily.ChainWon+"원"};
    Font body,bold,display;Texture2D dot;
    Texture2D dodgeball, sportsCar;
    bool UsesCar { get { return Model!=null && Model.Rules.SportsCar; } }
    bool UsesDodgeball { get { return Model!=null && Model.Rules.Dodgeball; } }
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
    // A boss's pictures besides his walking frames, in this order, and the words for what he was worth.
    const int Braced=0,Dashing=1,Struck=2,Stunned=3,Raging=4,Flying=5;
    readonly Dictionary<DadThrowStage.Kind,Texture2D[]> bossArt=new Dictionary<DadThrowStage.Kind,Texture2D[]>();
    readonly Dictionary<DadThrowStage.Kind,string> bountyText=new Dictionary<DadThrowStage.Kind,string>();
    // A boss sent back goes over backwards the length of the street: longer, further and higher than anyone's hop.
    const float BossOutTime=.9f,BossOutReach=520,BossOutLift=150,BossOutTurn=-300;int seenDaze;
    // What an entrance is dressed with, the shop's own pictures, and the two small effects (asset request v3: I, J, K, M).
    Texture2D bandLines,burstArt,balloonArt,coinArt,shopSign,soldOut;GUIStyle balloon;
    readonly Texture2D[] shopArt=new Texture2D[DadThrowShop.Cards.Length],clang=new Texture2D[3],coinPop=new Texture2D[3];
    const float CoinPopTime=.3f;
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
        string path=savePath;
#if UNITY_EDITOR
        // A Play Mode the remote started (captures, looking at a screen) reads the player's save and never writes it.
        // It matters since the letter of the day: the title itself now writes what it dealt.
        if(UnityEditor.SessionState.GetBool(RemoteKey,false))path=null;
#endif
        aim.Cancel();seenRun=-1;Flow=new DadThrowFlow(game,layout.Field,story,save,path,tuner,daily);
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
            tuner=new DadThrowTuner(stages.kinds,stages.stages[0].backgroundKey);
            daily=new DadThrowDaily(stages.kinds,stages.stages[0].backgroundKey);
            story=DadThrowStory.Parse(tale);
        }
        catch(System.ArgumentException error){missing.Add(error.Message);return;}
        street=Need<Texture2D>(layout.Street.resource);titleArt=Need<Texture2D>(layout.Title.resource);
        sportsCar=Need<Texture2D>("DadThrowV1/projectile_coupe");
        dodgeball=Need<Texture2D>("DadV5/chars/dodge/prop_dodgeball");
        golf=Need<Texture2D>(layout.Golf.resource);shoe=Need<Texture2D>(layout.Shoe.resource);
        clearArt=Need<Texture2D>(layout.Clear.resource);retryArt=Need<Texture2D>(layout.Retry.resource);
        dad[(int)DadThrowLayout.Pose.Ready]=Need<Texture2D>(layout.Ready.resource);dad[(int)DadThrowLayout.Pose.Windup]=Need<Texture2D>(layout.Windup.resource);
        dad[(int)DadThrowLayout.Pose.Release]=Need<Texture2D>(layout.Release.resource);
        foreach(DadThrowStage.Kind kind in stages.kinds.Values)
        {
            var frames=new Texture2D[kind.frames];for(int i=0;i<frames.Length;i++)frames[i]=Need<Texture2D>(kind.Frame(i));people[kind]=frames;
            if(kind.entrance!=null)entranceArt[kind]=Need<Texture2D>(kind.entrance.resource);
            if(kind.boss!=null)
            {
                bossArt[kind]=new[]{Need<Texture2D>(kind.boss.brace),Need<Texture2D>(kind.boss.dash),Need<Texture2D>(kind.boss.hit),Need<Texture2D>(kind.boss.dazed),Need<Texture2D>(kind.boss.roar),Need<Texture2D>(kind.boss.fly)};
                bountyText[kind]="+"+kind.boss.bounty+"원";
            }
            if(kind.bare==null)continue;
            var without=new Texture2D[kind.frames];for(int i=0;i<without.Length;i++)without[i]=Need<Texture2D>(kind.BareFrame(i));bare[kind]=without;
        }
        bandLines=Need<Texture2D>("DadThrowEntranceV1/entrance_band_lines");burstArt=Need<Texture2D>("DadThrowEntranceV1/entrance_burst");balloonArt=Need<Texture2D>("DadThrowEntranceV1/entrance_balloon");
        foreach(DadThrowShop.Card card in DadThrowShop.Cards)shopArt[(int)card.item]=Need<Texture2D>(DadThrowShop.IconKey(card.item));
        coinArt=Need<Texture2D>(DadThrowShop.CoinKey);shopSign=Need<Texture2D>(DadThrowShop.ShopKey);soldOut=Need<Texture2D>(DadThrowShop.SoldOutKey);
        for(int i=0;i<3;i++){clang[i]=Need<Texture2D>("DadThrowFxV2/fx_tray_clang_"+i);coinPop[i]=Need<Texture2D>("DadThrowFxV2/fx_coin_pop_"+i);}
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
        // The title is where a new day is noticed: the rules keep no clock, so the screen tells them the date.
        if(Flow.Now!=DadThrowFlow.Screen.Playing){aim.Cancel();if(Flow.Now==DadThrowFlow.Screen.Title)Flow.Day(DadThrowDaily.Key(System.DateTime.Now));Flow.Tick(dt);return;}
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
        // Someone whose balls keep stopping on trays and who hardly throws a shoe: the button goes on asking for
        // as long as a tray is in view, without waiting for the next ball to be stopped.
        if(Flow.ShoeShy&&selected==DadThrowModel.Weapon.Golf&&shoeNudge<.5f)
            foreach(DadThrowModel.Enemy e in m.enemies)if(e.guard&&e.x<=DadThrowLayout.W){shoeNudge=.5f;break;}
        // A boss braced for his dash: the shoe is what stops him, so its button lights up the moment he braces.
        // A shoe that takes him off his feet jolts the street.
        DadThrowModel.Enemy boss=m.Chief;if(boss!=null&&boss.guard&&selected==DadThrowModel.Weapon.Golf&&shoeNudge<.5f)shoeNudge=.5f;
        if(m.DazeSerial!=seenDaze){seenDaze=m.DazeSerial;shake=Mathf.Max(shake,.6f);}
        if(Flow.Now!=DadThrowFlow.Screen.Playing)aim.Cancel();
    }
    // A fresh stage: nothing from the last attempt may leak into it. A button starts the stage while the screen
    // is being drawn, so drawing asks too, before the next Update comes round.
    void Fresh()
    {
        if(Flow.Model==null||Flow.Runs==seenRun)return;
        seenRun=Flow.Runs;seenHealth=Flow.Model.Health;seenChain=Flow.Model.ChainSerial;seenBlock=Flow.Model.BlockSerial;seenDaze=Flow.Model.DazeSerial;blockClock=shoeNudge=0;
        thrown=false;hintClock=throwPose=palm=chainClock=shake=0;shownRemaining=-1;aim.Cancel();seen.Clear();leaving.Clear();
        // 공만: there is no shoe to have been left selected.
        if(Flow.Model.Rules.NoShoe)selected=DadThrowModel.Weapon.Golf;
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
            leaving.Add(new Leaving{id=was.id,x=was.x,kind=was.kind,bare=was.bare,age=was.kind.boss!=null?LeaveTime-BossOutTime:0});
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
    // A player in memory only who has opened stage `index` (from 0), has `coins` in the purse and has already
    // seen the entrances of the opponents named in `met`.
    public void DebugStartAt(int index,int coins,params string[] met)
    {
        var save=new DadThrowSave{introSeen=true,furthest=index,coins=coins};save.met.AddRange(met);
        aim.Cancel();heldForCapture=false;seenRun=-1;Flow=new DadThrowFlow(game,layout.Field,story,save,null);
    }
    // The player's own save, held in memory only and opened at stage `index` (from 0): for playing one stage
    // without playing up to it. Nothing that happens from here is written to the save on disk.
    public void DebugPlayAt(int index)
    {
        string problem;DadThrowSave save=DadThrowSave.Load(savePath,out problem);save.furthest=Mathf.Clamp(index,0,game.Count-1);
        aim.Cancel();heldForCapture=false;seenRun=-1;Flow=new DadThrowFlow(game,layout.Field,story,save,null,tuner);
    }
    // A player in memory only who has cleared stages 1 to `through` and holds today's letter: stage `stage` under
    // `cards`, with `left` tries and the tiers in `won` already paid. Everyone's entrance has been seen, so a
    // capture meets no cut-in it did not ask for.
    public void DebugDaily(int through,int stage,int cards,int left=DadThrowDaily.Tries,int won=0)
    {
        var save=new DadThrowSave{introSeen=true,furthest=Mathf.Clamp(through,0,game.Count-1),coins=300,firstDay=1,
            dailyDay=DadThrowDaily.Key(System.DateTime.Now),dailyStage=stage,dailyCards=cards,dailyLeft=left,dailyWon=won};
        for(int number=1;number<=through;number++)save.cleared.Add(DadThrowCurve.Id(number));
        foreach(string kind in stages.kinds.Keys)save.met.Add(kind);
        aim.Cancel();heldForCapture=false;seenRun=-1;Flow=new DadThrowFlow(game,layout.Field,story,save,null,null,daily);
    }
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
    {if(selected==weapon||weapon==DadThrowModel.Weapon.Shoe&&Flow.Model!=null&&Flow.Model.Rules.NoShoe)return;selected=weapon;aim.Cancel();if(weapon==DadThrowModel.Weapon.Shoe)shoeHintDone=true;}
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
        if(picture==sportsCar)GUI.DrawTexture(DadThrowLayout.IconSlot(face),picture,ScaleMode.ScaleToFit);
        else GUI.DrawTexture(layout.ItemRect(sprite,DadThrowLayout.IconSlot(face).center,DadThrowLayout.IconSize),picture);
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
    // 큰 손 (DadThrowDaily) draws the ball as many times its size as the hit test takes it to be.
    void Thing(DadThrowModel.Weapon weapon,Vector2 center,float degrees)
    {
        float big=weapon==DadThrowModel.Weapon.Golf&&Model!=null?Model.Rules.BallScale:1;
        if(big==1){PlainThing(weapon,center,degrees);return;}
        Matrix4x4 old=GUI.matrix;
        GUI.matrix=old*Matrix4x4.TRS(new Vector3(center.x,center.y,0),Quaternion.identity,new Vector3(big,big,1))*Matrix4x4.TRS(new Vector3(-center.x,-center.y,0),Quaternion.identity,Vector3.one);
        PlainThing(weapon,center,degrees);GUI.matrix=old;
    }
    void PlainThing(DadThrowModel.Weapon weapon,Vector2 center,float degrees)
    {
        if(weapon==DadThrowModel.Weapon.Golf && UsesCar)
            Spun(sportsCar,new Rect(center.x-68,center.y-34,136,68),center,degrees*.25f);
        else if(weapon==DadThrowModel.Weapon.Golf && UsesDodgeball)
            Spun(dodgeball,new Rect(center.x-28,center.y-28,56,56),center,degrees);
        else if(weapon==DadThrowModel.Weapon.Golf)Spun(golf,layout.GolfRect(center),center,degrees);
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
            if(gone.kind.boss!=null){ChiefOut(gone,m);continue;}
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
            if(e.IsChief){Chief(e,m.Field.Ground);continue;}
            GUI.color=e.flash>0?new Color(1,.6f,.45f):e.OffBalance?new Color(.65f,.9f,1f):e.IsChair && e.hp<e.full?new Color(1,.72f,.58f):Color.white;
            Person(e.kind,e.id,e.x,m.Field.Ground,e.kind.Mirror,e.speed,false,e.kind.guards&&!e.guard);
        }
        GUI.color=Color.white;
        // One won leaves each of them: a coin pops over where the head was. The picture is only a picture; the
        // purse is paid from the fight's own count. The coin moves inside its frames, so every frame is placed
        // by the same foot point (64,108 of 128).
        foreach(Leaving gone in leaving)
        {
            if(gone.kind.boss!=null||gone.age>=CoinPopTime)continue;
            float k=DadThrowLayout.CoinPopSize/128;Vector2 head=new Vector2(gone.x,m.Field.Ground+Lane(gone.id)-gone.kind.Height(gone.id)-4);
            GUI.DrawTexture(new Rect(head.x-64*k,head.y-108*k,DadThrowLayout.CoinPopSize,DadThrowLayout.CoinPopSize),coinPop[Mathf.Min(2,(int)(gone.age/CoinPopTime*3))]);
        }
    }
    // The boss: his walking frames while he walks (turned round while he trots back out from the gate), and one
    // picture each for bracing, dashing, standing dazed, losing his temper and taking a ball.
    void Chief(DadThrowModel.Enemy e,float ground)
    {
        DadThrowStage.Kind kind=e.kind;Texture2D[] poses=bossArt[kind],steps=people[kind];float x=e.x,tall=kind.Height(e.id);ground+=Lane(e.id);
        bool back=e.mood==DadThrowModel.Mood.Back;Texture2D frame;
        switch(e.mood)
        {
            case DadThrowModel.Mood.Brace:frame=poses[Braced];x+=Mathf.Sin(clock*46)*1.5f;break;
            case DadThrowModel.Mood.Dash:frame=poses[Dashing];break;
            case DadThrowModel.Mood.Dazed:frame=poses[Stunned];x+=Mathf.Sin(clock*5)*4;break;
            case DadThrowModel.Mood.Roar:frame=poses[Raging];x+=Mathf.Sin(clock*63)*3;break;
            default:frame=e.flash>0&&!back?poses[Struck]:steps[(int)(clock*Mathf.Max(48,e.speed*(back?DadThrowModel.BossBackPace:1))/8)%steps.Length];break;
        }
        // Sand off his heels while he dashes.
        if(e.mood==DadThrowModel.Mood.Dash)
            for(int i=0;i<3;i++)
            {
                float puff=(clock*3+i/3f)%1,size=14+26*puff;GUI.color=new Color(Dust.r,Dust.g,Dust.b,.6f*(1-puff));
                GUI.DrawTexture(new Rect(x+50+puff*70-size/2,ground-6-size/2-puff*14,size,size),dot);
            }
        GUI.color=e.flash>0?new Color(1,.6f,.45f):e.raged?new Color(1,.84f,.8f):Color.white;
        Rect r=kind.Place(x,ground,tall);
        if(kind.Mirror==back)GUI.DrawTexture(r,frame);
        else
        {
            Matrix4x4 old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(new Vector3(x,ground,0),Quaternion.identity,new Vector3(-1,1,1));
            GUI.DrawTexture(new Rect(r.x-x,r.y-ground,r.width,r.height),frame);GUI.matrix=old;
        }
        GUI.color=Color.white;Vector2 head=new Vector2(x,ground-tall);
        // Braced: a warning over his head. Dazed: stars going round it.
        if(e.mood==DadThrowModel.Mood.Brace)Outlined(new Rect(head.x-40,head.y-64,80,60),"!",52,Warn,Paper,3,display);
        else if(e.mood==DadThrowModel.Mood.Dazed)
            for(int i=0;i<3;i++){Vector2 star=head+new Vector2(Mathf.Cos(clock*5+i*2.094f)*30,-10+Mathf.Sin(clock*5+i*2.094f)*8);Dot(star,13,Ink);Dot(star,8,Gold);}
    }
    // A boss sent back does not hop away: he goes over backwards down the street, turning as he goes, and what
    // he was worth rises from where he stood.
    void ChiefOut(Leaving gone,DadThrowModel m)
    {
        DadThrowStage.Kind kind=gone.kind;float t=Mathf.Clamp01(1-(LeaveTime-gone.age)/BossOutTime),tall=kind.Height(gone.id),ground=m.Field.Ground+Lane(gone.id);
        Vector2 feet=new Vector2(gone.x+BossOutReach*t,ground-Mathf.Sin(t*Mathf.PI)*BossOutLift);
        GUI.color=new Color(1,1,1,Mathf.Clamp01(3-3*t));
        Spun(bossArt[kind][Flying],kind.Place(feet.x,feet.y,tall),new Vector2(feet.x,feet.y-tall/2),t*BossOutTurn);
        GUI.color=Color.white;
        if(kind.boss.bounty>0&&t<.85f)Outlined(new Rect(gone.x-100,ground-tall-64-46*t,200,52),bountyText[kind],40,Gold,Ink,3,display);
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
    // Where a tray stopped a ball: a small star and a few dashes, three frames over BlockTime, centred on the spot.
    void DrawBlock()
    {
        if(blockClock<=0)return;
        float t=1-blockClock/BlockTime,size=DadThrowLayout.ClangSize;
        GUI.DrawTexture(new Rect(blockAt.x-size/2,blockAt.y-size/2,size,size),clang[Mathf.Min(2,(int)(t*3))]);
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
        float wide=r.height*.86f;GUI.DrawTexture(new Rect(r.x+(r.height-wide)/2,r.center.y-wide/2,wide,wide),coinArt);
        Text(new Rect(r.x+r.height+4,r.y,r.width-r.height-4,r.height),coinText,size,color,font,TextAnchor.MiddleLeft);
    }
    // What this stage paid, over the top edge of the result card.
    void DrawEarned()
    {
        if(Flow.Earned!=shownEarned){shownEarned=Flow.Earned;earnedText="+"+shownEarned+"원";}
        string line=earnedText;
        int heavy=DadThrowShop.Level(Flow.Save,DadThrowShop.Item.Heavy);
        if(heavy<2)
        {
            int needed=DadThrowShop.Price(Flow.Save,DadThrowShop.Item.Heavy)-Flow.Coins;
            string target=heavy==0?"피구공":"스포츠카";
            line+=needed>0?"  ·  "+target+"까지 "+needed+"원":"  ·  "+target+" 구매 가능";
        }
        Outlined(DadThrowLayout.ResultEarned,line,28,Gold,Ink,3,display);
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
        if(WeaponButton(DadThrowLayout.GolfButton,UsesCar?"스포츠카":UsesDodgeball?"피구공":"골프공",UsesCar?sportsCar:UsesDodgeball?dodgeball:golf,layout.Golf,chosen==DadThrowModel.Weapon.Golf,reload,false))Select(DadThrowModel.Weapon.Golf);
        if(!Flow.Model.Rules.NoShoe&&WeaponButton(DadThrowLayout.ShoeButton,"신발",shoe,layout.Shoe,chosen==DadThrowModel.Weapon.Shoe,reload,nudge))Select(DadThrowModel.Weapon.Shoe);
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
        if(Flow.InDaily&&DadThrowDaily.Has(Flow.Save.dailyCards,DadThrowDaily.Card.Fog))DrawFog();
        // On a result the big pose beside the card stands in for dad, so the small one is left out.
        if(!result){DrawDad();DrawFlight(m);DrawBlock();}
        GUI.matrix=still;
        if(live)DrawAim(m);
        // Nothing under a card can be pressed.
        bool was=GUI.enabled;GUI.enabled=was&&live;
        DrawTopBar(Flow.Stage,m.Health,m.Remaining);DrawPurse(DadThrowLayout.CoinLabel,26,Ink,bold);DrawControls(Flow.Stage,m.ReloadTotal>0?Mathf.Clamp01(m.Reload/m.ReloadTotal):0,selected);GUI.enabled=was;
        if(!result)DrawBossBar(m);
        if(Flow.InDaily&&!result){DailyWords();if(dailyChipText.Length>0)Outlined(DadThrowLayout.DailyChips,dailyChipText,24,Paper,Ink,3,display);}
        if(entering)DrawEntrance();
        if(now==DadThrowFlow.Screen.Paused)DrawPause();
        else if(now==DadThrowFlow.Screen.Clear)DrawClear();
        else if(now==DadThrowFlow.Screen.Defeat)DrawDefeat();
    }
    // A boss's name and what is left of him, from the moment he is on the street. The bar turns red once he has lost his temper.
    void DrawBossBar(DadThrowModel m)
    {
        DadThrowModel.Enemy chief=m.Chief;if(chief==null||chief.x>DadThrowLayout.W-DadThrowFlow.BossShown)return;
        Rect bar=DadThrowLayout.BossBar;DadUi.Panel(bar,PaperDown,Ink);
        float share=Mathf.Clamp01((float)chief.hp/Mathf.Max(1,chief.full));
        if(share>0)DadUi.Fill(new Rect(bar.x+4,bar.y+4,Mathf.Max(8,(bar.width-8)*share),bar.height-8),chief.raged?Warn:Gold,4);
        Outlined(DadThrowLayout.BossName,chief.kind.entrance!=null?chief.kind.entrance.name:"대장",30,Paper,Ink,3,display);
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
        Rect band=DadThrowLayout.EntranceBand,inner=new Rect(band.x,band.y+8,band.width,band.height-16);Box(band,Ink);Box(inner,Gold);
        // Speed lines run along the band, repeating end to end and never stretched.
        float repeats=inner.width/(inner.height*DadThrowLayout.EntranceLinesShape);
        GUI.DrawTextureWithTexCoords(inner,bandLines,new Rect(clock*DadThrowLayout.EntranceLinesPace,0,repeats,1));
        GUI.matrix=old;
        // Rays turn slowly behind him and come in with him.
        Vector2 rays=DadThrowLayout.EntranceBurstCentre+new Vector2((1-arrive)*DadThrowLayout.EntranceSlide,0);float spread=DadThrowLayout.EntranceBurst;
        Spun(burstArt,new Rect(rays.x-spread/2,rays.y-spread/2,spread,spread),rays,clock*DadThrowLayout.EntranceBurstTurn);
        // He lands with a small jolt: a touch too large for a moment, then his own size.
        float pop=1+.06f*Mathf.Clamp01(1-(t-DadThrowLayout.EntranceIn)/.18f)*(t>=DadThrowLayout.EntranceIn?1:0);
        GUI.DrawTexture(kind.EntrancePlace(DadThrowLayout.EntranceFeet.x+(1-arrive)*DadThrowLayout.EntranceSlide,DadThrowLayout.EntranceFeet.y,DadThrowLayout.EntranceTall*pop),entranceArt[kind]);
        if(t<DadThrowLayout.EntranceIn)return;
        Outlined(DadThrowLayout.EntranceName,words.name,76,Paper,Ink,4,display);
        // The balloon is drawn at its own scale so the corners stay the size they were painted.
        if(balloon==null){balloon=new GUIStyle();balloon.normal.background=balloonArt;int corner=(int)DadThrowLayout.BalloonCorner;balloon.border=new RectOffset(corner,corner,corner,corner);}
        if(Event.current.type==EventType.Repaint)
        {
            Rect shout=DadThrowLayout.EntranceShout;float k=DadThrowLayout.BalloonScale;
            GUI.matrix=old*Matrix4x4.TRS(new Vector3(shout.x,shout.y,0),Quaternion.identity,new Vector3(k,k,1));
            balloon.Draw(new Rect(0,0,shout.width/k,shout.height/k),GUIContent.none,false,false,false,false);
            GUI.matrix=old;
        }
        Text(DadThrowLayout.EntranceShoutText,words.shout,34,Ink,display);
        // What to do about him, with the thing to do it with.
        DadUi.Panel(DadThrowLayout.EntranceHint,Gold,Ink);
        if(words.shoe)GUI.DrawTexture(layout.ItemRect(layout.Shoe,DadThrowLayout.EntranceHintIcon,92),shoe);
        else GUI.DrawTexture(layout.ItemRect(layout.Golf,DadThrowLayout.EntranceHintIcon,60),golf);
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
        if(Flow.InDaily)
        {
            // A try of the day: what it reached and what that paid, then back to the letter.
            if(shownDailyRun!=Flow.Runs)
            {
                shownDailyRun=Flow.Runs;var said=new System.Text.StringBuilder();
                for(int i=0;i<TierName.Length;i++)if((Flow.DailyTiers&(1<<i))!=0)said.Append(said.Length>0?" · ":"").Append(TierName[i]);
                dailyResultText=said+"\n"+(Flow.DailyPaid>0?"보너스 +"+Flow.DailyPaid+"원":"이미 받은 보상이에요");
            }
            Text(DadThrowLayout.ClearHeading,"도전 성공!",46,Ink,display);
            Text(DadThrowLayout.ClearWordLine,dailyResultText,24,Ink,body,TextAnchor.UpperCenter);
            if(Button(DadThrowLayout.ClearLeft,"상점",false)){Flow.OpenShop();return;}
            if(Button(DadThrowLayout.ClearRight,"도전장",true))Flow.ToDaily();
            return;
        }
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
        if(Flow.InDaily)
        {
            // A lost try goes back to the letter, which holds the tries that are left.
            if(Flow.Save.dailyLeft!=shownTries){shownTries=Flow.Save.dailyLeft;dailyTriesText="오늘 남은 기회 "+shownTries+"번";}
            Text(DadThrowLayout.DefeatHeading,"다시 해볼까?",46,Ink,display);
            Text(DadThrowLayout.DefeatLine,dailyTriesText,24,Ink,body);
            if(Button(DadThrowLayout.DefeatRetry,"도전장",true,30)){Flow.ToDaily();return;}
            if(Button(DadThrowLayout.DefeatShop,"상점",false,24)){Flow.OpenShop();return;}
            if(Button(DadThrowLayout.DefeatTitle,"타이틀",false,24))Flow.ToTitle();
            return;
        }
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
        // The letter of the day, from the second day on. A dot says tries are left.
        if(Flow.DailyReady)
        {
            bool letter=Button(DadThrowLayout.DailyButton,"오늘의 도전조",false,24);
            if(Flow.Save.dailyLeft>0)DadUi.Fill(DadThrowLayout.DailyDot,Warn,11);
            if(letter){Flow.OpenDaily();return;}
        }
        DrawPurse(DadThrowLayout.ShopPurse,22,Paper,bold);
        bool open=Button(DadThrowLayout.ShopButton,"상점",false,26);
        Rect sign=DadThrowLayout.ShopButton;GUI.DrawTexture(new Rect(sign.x+14,sign.center.y-22,44,44),shopSign);
        if(open)Flow.OpenShop();
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
            int level=DadThrowShop.Level(Flow.Save,card.item);
            Texture2D picture=shopArt[(int)card.item];
            string name=card.name;
            string description=card.line;
            if(card.item==DadThrowShop.Item.Heavy)
            {
                picture=level==0?dodgeball:sportsCar;
                name=level==0?"피구공":level==1?"스포츠카":"스포츠카 강화";
                description=level==0?"다음 상품: 스포츠카":level==1?"한 번에 8명 더 관통":"차체 충돌 위력 +1";
            }
            float side=DadThrowLayout.ShopIconSize;
            GUI.DrawTexture(new Rect(icon.x-side/2,icon.y-side/2,side,side),picture,ScaleMode.ScaleToFit);
            Text(DadThrowLayout.ShopName(slot),name,28,Ink,display,TextAnchor.MiddleLeft);
            Text(DadThrowLayout.ShopLine(slot),description,19,Ink,body,TextAnchor.UpperLeft);
            for(int pip=0;pip<card.most;pip++)DadUi.Panel(DadThrowLayout.ShopPip(slot,pip),pip<level?Gold:PaperDown,Ink);
            if(level>=card.most)
            {
                // Bought out: the mark beside the words, so it is not told by color alone.
                Rect done=DadThrowLayout.ShopBuy(slot);GUI.DrawTexture(new Rect(done.x,done.center.y-22,44,44),soldOut);
                Text(Rect.MinMaxRect(done.x+48,done.y,done.xMax,done.yMax),"다 샀다",22,Olive,bold,TextAnchor.MiddleLeft);continue;
            }
            int price=DadThrowShop.Price(Flow.Save,card.item);
            if(price!=shownPrice[i]){shownPrice[i]=price;priceText[i]=price+"원";}
            // A price out of reach is shown greyed and cannot be pressed.
            if(Button(DadThrowLayout.ShopBuy(slot),priceText[i],true,24,DadThrowShop.CanBuy(Flow.Save,card.item)))Flow.Buy(card.item);
        }
    }
    // ── today's challenge ──
    void DailyWords()
    {
        DadThrowSave s=Flow.Save;if(s.dailyDay==shownDailyDay&&s.dailyStage==shownDailyStage&&s.dailyCards==shownDailyCards)return;
        shownDailyDay=s.dailyDay;shownDailyStage=s.dailyStage;shownDailyCards=s.dailyCards;
        dailyStageText=DadThrowCurve.Id(Mathf.Max(1,s.dailyStage))+" 판에서";
        var names=new System.Text.StringBuilder();
        foreach(DadThrowDaily.Face face in DadThrowDaily.Faces)if(DadThrowDaily.Has(s.dailyCards,face.card))names.Append(names.Length>0?" · ":"").Append(face.name);
        dailyChipText=names.ToString();
    }
    // 안개: the far half of the street stands behind a veil that thickens to the right. It is drawn over the
    // people and under dad and everything thrown, so a ball can be followed into it.
    const float FogDepth=.86f;
    void DrawFog()
    {
        float top=DadThrowLayout.TopBar,tall=DadThrowLayout.H-top,x=DadThrowLayout.FogFrom;
        // Thin strips, each a little thicker than the last: four wide ones read as stripes, not as fog.
        for(int i=1;i<=DadThrowLayout.FogSteps;i++,x+=DadThrowLayout.FogStep)Box(new Rect(x,top,DadThrowLayout.FogStep,tall),new Color(Paper.r,Paper.g,Paper.b,FogDepth*i/(DadThrowLayout.FogSteps+1)));
        Box(new Rect(x,top,DadThrowLayout.W-x,tall),new Color(Paper.r,Paper.g,Paper.b,FogDepth));
    }
    // The letter: the stage it is played on, the day's cards, what a cleared try pays and has paid, the tries left.
    void DrawDaily()
    {
        DailyWords();DadThrowSave s=Flow.Save;
        GUI.DrawTexture(layout.TitleArt(),titleArt);Veil(ResultVeil);DadUi.Panel(DadThrowLayout.DailyCard,Paper,Ink);
        Text(DadThrowLayout.DailyHeading,"오늘의 도전장",44,Ink,display,TextAnchor.MiddleLeft);
        Text(DadThrowLayout.DailyStage,dailyStageText,26,Olive,bold,TextAnchor.MiddleRight);
        int slot=0;
        foreach(DadThrowDaily.Face face in DadThrowDaily.Faces)
        {
            if(!DadThrowDaily.Has(s.dailyCards,face.card)||slot>=DadThrowDaily.MostCards)continue;
            Rect rule=DadThrowLayout.DailyRule(slot++),band=new Rect(rule.x,rule.y,rule.width,64);
            DadUi.Panel(rule,PaperDown,Ink);DadUi.Panel(band,Gold,Ink);
            Text(band,face.name,30,Ink,display);
            Text(new Rect(rule.x+14,band.yMax+14,rule.width-28,rule.height-band.height-28),face.line,22,Ink,body,TextAnchor.UpperCenter);
        }
        for(int i=0;i<TierName.Length;i++)
        {
            Rect row=DadThrowLayout.DailyTier(i);bool won=(s.dailyWon&(1<<i))!=0;DadUi.Panel(row,won?Gold:Paper,Ink);
            Text(new Rect(row.x+14,row.y+6,row.width-28,34),TierName[i],22,Ink,bold,TextAnchor.MiddleLeft);
            Text(new Rect(row.x+14,row.y+38,row.width-28,30),won?"받았다":TierPay[i],20,won?Ink:Olive,body,TextAnchor.MiddleLeft);
        }
        Rect tries=DadThrowLayout.DailyTries;Text(new Rect(tries.x,tries.y,140,tries.height),"남은 기회",24,Ink,bold,TextAnchor.MiddleLeft);
        for(int i=0;i<DadThrowDaily.Tries;i++)DadUi.Panel(DadThrowLayout.DailyTry(i),i<s.dailyLeft?Gold:PaperDown,Ink);
        if(Button(DadThrowLayout.DailyClose,"닫기",false,26)){Flow.CloseDaily();return;}
        if(Button(DadThrowLayout.DailyGo,s.dailyLeft>0?"도전":"내일 또 와요",true,s.dailyLeft>0?34:26,s.dailyLeft>0))Flow.StartDaily();
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
            else if(Flow.Now==DadThrowFlow.Screen.Daily)DrawDaily();
            else if(Flow.Now==DadThrowFlow.Screen.Intro||Flow.Now==DadThrowFlow.Screen.Ending)DrawStory(Flow.Cut);
            else DrawBattle();
            Letterbox(bars);
        }
        finally{GUI.color=tint;GUI.matrix=old;}
    }
}
