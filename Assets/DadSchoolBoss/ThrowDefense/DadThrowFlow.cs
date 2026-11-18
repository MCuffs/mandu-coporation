using System;
using System.Collections.Generic;
using UnityEngine;

// Which screen is up and what each button may do (plan §5, story v2 §4-6). No drawing and no input devices:
// the screen asks, this answers, so the whole loop from the logo to a retry runs in the window-free checks.
//   Boot -> Title -> (first time: Intro) -> Playing <-> Paused -> Clear or Defeat
//   Clear -> next stage / same stage again / Title        Defeat -> same stage again / Title
//   The chapter's last stage, first time cleared: Playing -> Ending -> Clear
//   Title, Clear or Defeat -> Shop -> back to where it was opened from. What is bought there is in the throws
//   of the next stage that starts. Every challenger sent back is one won, banked when a stage ends or is left.
//   Title -> Daily (the letter of the day) -> Playing -> Clear or Defeat -> Daily. A try of the day opens no
//   stage, is not counted by the tuner, and cannot be retried from its result: the letter holds the tries.
// A request that does not belong to the current screen is refused, which is also what stops a double tap
// from starting two stages.
public sealed class DadThrowFlow
{
    public enum Screen { Boot, Title, Intro, Playing, Paused, Clear, Defeat, Ending, Shop, Daily }
    // Logo: 0.2 in, 0.8 held, 0.2 out (plan §6.1).
    public const float BootTime=1.2f,BootFade=.2f;
    public Screen Now {get;private set;}
    public DadThrowModel Model {get;private set;}
    public int StageIndex {get;private set;}
    // The furthest stage opened so far. Only a clear moves it, and a defeat never moves it back.
    public int Furthest {get;private set;}
    // Counts every fresh start of a stage, so the screen knows when to forget the last attempt's leftovers.
    public int Runs {get;private set;}
    public readonly DadThrowStory Story;public readonly DadThrowSave Save;
    // Seconds into the opening or the ending.
    public float StoryClock {get;private set;}
    // After the opening the street holds still, first challenger in view, until the player's first pull.
    public bool WaitingForAim {get;private set;}
    // The line for the clear card: only the first time a stage is cleared, and not when the ending just played.
    public DadThrowStage.Line Word {get;private set;}
    // Why the last save did not reach the disk, if it did not. Play goes on from memory.
    public string SaveProblem {get;private set;}
    readonly IList<DadThrowStage> stages;readonly DadThrowField field;readonly string savePath;float boot;bool primed;
    // A new kind of opponent makes an entrance the first time a stage with him in it starts: the street holds
    // still, his first people already in view, while a big picture of him comes on with a line and what to do
    // about him. It ends by itself after EnterTime, or on a tap once EnterSkip has passed, and is not shown again.
    // A boss makes his entrance in the middle of the fight, at the moment he steps this far onto the street:
    // everything stops where it is, balls in the air included, and goes on when the picture is gone.
    public const float EnterTime=3.2f,EnterSkip=.6f,BossShown=40;
    public DadThrowStage.Kind Entering {get;private set;} public float EnterClock {get;private set;}
    void EndEntrance(){if(!Save.met.Contains(Entering.id))Save.met.Add(Entering.id);Entering=null;Persist();}
    public bool SkipEntrance(){if(Now!=Screen.Playing||Entering==null||EnterClock<EnterSkip)return false;EndEntrance();return true;}
    // Whether the stage in hand has paid out, and the screen the shop was opened from.
    bool banked;Screen shopFrom;
    // The purse as the player sees it: what is saved, plus those sent back in the fight still going on.
    public int Coins {get{return Save.coins+(Model!=null&&!banked?Earned:0);}}
    // What the stage in hand has earned so far (it stays readable on the result card).
    public int Earned {get{return Model!=null?(DadThrowTuner.Pay(Model.Killed,Heat)+Model.Bounty)*(InDaily?DadThrowDaily.PayTimes(Save.dailyCards):1):0;}}
    // The difficulty tuner, when the game has one, and how far the stage in hand was eased or heated when it
    // started (0 when nothing was owed). The player is never shown these.
    readonly DadThrowTuner tuner;
    public int Ease {get;private set;} public int Heat {get;private set;}
    // Balls keep stopping on trays and hardly a shoe has been thrown: the screen keeps pointing at the shoe.
    public bool ShoeShy {get{return tuner!=null&&DadThrowTuner.ShoeShy(Save);}}
    // Today's challenge, when the game has one (DadThrowDaily). InDaily: the fight or the result in hand is a try
    // of the day. DailyTiers: what that try reached once it was cleared; DailyPaid: what of it had not been paid
    // before today and was added to the purse.
    readonly DadThrowDaily daily;int today;DadThrowStage dailyTable;
    public bool InDaily {get;private set;} public int DailyTiers {get;private set;} public int DailyPaid {get;private set;}
    // The letter is there to open: the game has one, the day is known, and today's has been dealt.
    public bool DailyReady {get{return daily!=null&&today>0&&Save.dailyDay==today&&DadThrowDaily.Open(Save,today);}}

    // story: null plays no opening or ending. savePath: null keeps progress in memory only (checks, captures).
    // tuner: null plays every stage exactly as it is listed (checks, captures of a fixed stage).
    // daily: null plays without the letter of the day.
    public DadThrowFlow(IList<DadThrowStage> stages,DadThrowField field,DadThrowStory story=null,DadThrowSave save=null,string savePath=null,DadThrowTuner tuner=null,DadThrowDaily daily=null)
    {
        this.tuner=tuner;this.daily=daily;
        if(stages==null||stages.Count==0)throw new ArgumentException("the flow needs at least one stage");
        if(field==null)throw new ArgumentNullException("field");
        this.stages=stages;this.field=field;this.savePath=savePath;Story=story;Save=save??new DadThrowSave();
        // A save from a longer build may point past the stages this one has.
        Furthest=Mathf.Clamp(Save.furthest,0,stages.Count-1);Now=Screen.Boot;
    }
    public DadThrowStage Stage {get{return InDaily?dailyTable:stages[StageIndex];}}
    public bool HasNext {get{return !InDaily&&StageIndex+1<stages.Count;}}
    // The title offers 계속하기 once a later stage has been opened.
    public bool Continuing {get{return Furthest>0;}}
    public float BootAlpha {get{return Mathf.Clamp01(Mathf.Min(boot,BootTime-boot)/BootFade);}}
    public DadThrowStory.Cut Cut
    {get{return Story==null?null:Now==Screen.Intro?DadThrowStory.At(Story.opening,StoryClock):Now==Screen.Ending?DadThrowStory.At(Story.ending,StoryClock):null;}}

    void Persist()
    {
        if(savePath==null)return;
        try{Save.Write(savePath);SaveProblem=null;}
        catch(Exception error){if(!(error is System.IO.IOException||error is UnauthorizedAccessException||error is ArgumentException))throw;SaveProblem=error.Message;}
    }
    // Every start is a clean stage: full gate touches, nobody on the street, nothing in the air, no reload.
    // This is the one way a stage begins, whether the opening ran to its end, was skipped, or never played.
    // The throws are the plain ones with everything bought in the shop added.
    void Load(int index,bool wait=false)
    {
        InDaily=false;StageIndex=index;DadThrowStage plain=stages[index];
        Ease=tuner!=null?DadThrowTuner.Ease(Save,plain.id):0;Heat=tuner!=null?DadThrowTuner.Heat(Save):0;
        Model=new DadThrowModel(field,tuner!=null?tuner.Table(plain,index+1,Save):plain){Rules=DadThrowShop.Rules(Save)};
        Runs++;Word=null;WaitingForAim=wait;primed=false;banked=false;Now=Screen.Playing;
        // Whoever in this stage has an entrance to make and has not made it: on the first stage he is in, and
        // for a save that is already past that stage, on the next stage he turns up in.
        Entering=null;EnterClock=0;
        foreach(DadThrowStage.Group g in stages[index].groups)
            if(g.kind!=null&&g.kind.entrance!=null&&g.kind.boss==null&&!Save.met.Contains(g.kind.id)){Entering=g.kind;break;}
    }
    // One won for each challenger sent back, once per stage start, whether the stage was won, lost or left.
    void Bank(bool persist=true)
    {
        if(Model==null||banked)return;
        int pay=Earned;banked=true;
        if(tuner!=null&&!InDaily)DadThrowTuner.Watched(Save,Model.BlockSerial,Model.ShoesThrown);
        Save.coins+=pay;if(persist)Persist();
    }
    // The shop opens from the title or from a result, and closes back onto the same screen.
    public bool OpenShop(){if(Now!=Screen.Title&&Now!=Screen.Clear&&Now!=Screen.Defeat)return false;shopFrom=Now;Now=Screen.Shop;return true;}
    public bool CloseShop(){if(Now!=Screen.Shop)return false;Now=shopFrom;return true;}
    public bool Buy(DadThrowShop.Item item){if(Now!=Screen.Shop||!DadThrowShop.Buy(Save,item))return false;Persist();return true;}
    // The screen says what day it is; the rules keep no clock. On the title a new day deals a new letter.
    public void Day(int key){today=key;if(daily!=null&&Now==Screen.Title&&daily.Day(Save,key))Persist();}
    public bool OpenDaily(){if(Now!=Screen.Title||!DailyReady)return false;Now=Screen.Daily;return true;}
    public bool CloseDaily(){if(Now!=Screen.Daily)return false;Now=Screen.Title;return true;}
    // A try of the day: it is spent the moment it starts, so leaving a fight that goes badly does not give it back.
    // The throws are the bought ones with the day's cards on top; the tuner neither eases nor heats it.
    public bool StartDaily()
    {
        if(Now!=Screen.Daily||!DailyReady||Save.dailyLeft<=0)return false;
        Save.dailyLeft--;Persist();
        dailyTable=daily.Table(Save.dailyStage,Save.dailyCards);InDaily=true;DailyTiers=DailyPaid=0;Ease=Heat=0;
        StageIndex=Mathf.Clamp(Save.dailyStage-1,0,stages.Count-1);
        DadThrowRules rules=DadThrowShop.Rules(Save);DadThrowDaily.Bend(rules,Save.dailyCards);
        Model=new DadThrowModel(field,dailyTable){Rules=rules};
        Runs++;Word=null;WaitingForAim=false;primed=false;banked=false;Now=Screen.Playing;
        // A card can bring on someone the player has not met yet; he makes his entrance here as he would on a stage.
        Entering=null;EnterClock=0;
        foreach(DadThrowStage.Group g in dailyTable.groups)
            if(g.kind!=null&&g.kind.entrance!=null&&g.kind.boss==null&&!Save.met.Contains(g.kind.id)){Entering=g.kind;break;}
        return true;
    }
    // From the result of a try back to the letter.
    public bool ToDaily(){if(!InDaily||Now!=Screen.Clear&&Now!=Screen.Defeat)return false;Bank();Model=null;Entering=null;InDaily=false;Now=Screen.Daily;return true;}
    // A cleared try pays each tier it reached once a day, on top of the won for everyone sent back.
    void DailyCleared()
    {
        Bank(false);
        DailyTiers=DadThrowDaily.Tiers(dailyTable.health-Model.Health,Model.BestChain);
        DailyPaid=DadThrowDaily.Worth(DailyTiers&~Save.dailyWon);Save.dailyWon|=DailyTiers;Save.coins+=DailyPaid;
        Persist();Now=Screen.Clear;
    }
    public bool Start()
    {
        if(Now!=Screen.Title)return false;
        if(Story!=null&&!Save.introSeen){StageIndex=0;StoryClock=0;Now=Screen.Intro;return true;}
        Load(Furthest);return true;
    }
    bool EndIntro(){if(Now!=Screen.Intro)return false;Save.introSeen=true;Persist();Load(0,true);return true;}
    bool EndEnding(){if(Now!=Screen.Ending)return false;Now=Screen.Clear;return true;}
    // The skip button, on the opening or the ending.
    public bool Skip(){return Now==Screen.Intro?EndIntro():EndEnding();}
    // The first pull lets the street move.
    public void AimStarted(){WaitingForAim=false;}
    public bool Pause(){if(Now!=Screen.Playing)return false;Now=Screen.Paused;return true;}
    public bool Resume(){if(Now!=Screen.Paused)return false;Now=Screen.Playing;return true;}
    public bool Retry(){if(InDaily||Now!=Screen.Defeat&&Now!=Screen.Clear)return false;Load(StageIndex);return true;}
    // There is no stage after the last one to move to.
    public bool Next(){if(Now!=Screen.Clear||!HasNext)return false;Load(StageIndex+1);return true;}
    // Leaving drops the stage in progress; what was opened stays opened.
    public bool ToTitle(){if(Now!=Screen.Paused&&Now!=Screen.Clear&&Now!=Screen.Defeat)return false;Bank();Model=null;Entering=null;InDaily=false;Now=Screen.Title;return true;}
    // Android back: pause a fight; skip a story; close the shop or the letter; from the pause or a result, go to the title.
    public bool Back(){return Now==Screen.Playing?Pause():Now==Screen.Intro||Now==Screen.Ending?Skip():Now==Screen.Shop?CloseShop():Now==Screen.Daily?CloseDaily():ToTitle();}
    // The app went to the background or lost focus. Only a live fight changes; a result never turns back into a fight.
    public void Interrupted(){Pause();}
    void Cleared()
    {
        DadThrowStage stage=Stage;bool first=!Save.cleared.Contains(stage.id);
        Bank(false);
        if(tuner!=null)DadThrowTuner.Cleared(Save,Model.Health<stage.health);
        if(first)Save.cleared.Add(stage.id);
        if(HasNext)Furthest=Math.Max(Furthest,StageIndex+1);
        Save.furthest=Furthest;Persist();
        bool closes=first&&stage.ending&&Story!=null&&Story.ending.Count>0;
        Word=first&&!closes?stage.after:null;
        if(closes){StoryClock=0;Now=Screen.Ending;}else Now=Screen.Clear;
    }
    public void Tick(float dt)
    {
        if(Now==Screen.Boot){boot+=dt;if(boot>=BootTime)Now=Screen.Title;return;}
        if(Now==Screen.Intro){StoryClock+=dt;if(StoryClock>=Story.OpeningSeconds)EndIntro();return;}
        if(Now==Screen.Ending){StoryClock+=dt;if(StoryClock>=Story.EndingSeconds)EndEnding();return;}
        if(Now!=Screen.Playing)return;
        if(Entering!=null)
        {
            // One step puts the stage's first group on the street behind the picture; then nothing moves.
            if(!primed){Model.Tick(1/120f);primed=true;}
            EnterClock+=dt;if(EnterClock>=EnterTime)EndEntrance();
            return;
        }
        if(WaitingForAim)
        {
            // One step puts the stage's first group on the street; then nothing moves until the first pull.
            if(!primed){Model.Tick(1/120f);primed=true;}
            return;
        }
        Model.Tick(dt);
        DadThrowModel.Enemy chief=Model.phase==DadThrowModel.Phase.Playing?Model.Chief:null;
        if(chief!=null&&chief.kind.entrance!=null&&chief.x<=DadThrowModel.Width-BossShown&&!Save.met.Contains(chief.kind.id)){Entering=chief.kind;EnterClock=0;primed=true;return;}
        if(Model.phase==DadThrowModel.Phase.Lost){if(tuner!=null&&!InDaily)DadThrowTuner.Lost(Save,Stage.id);Bank();Now=Screen.Defeat;}
        else if(Model.phase==DadThrowModel.Phase.WaveClear){if(InDaily)DailyCleared();else Cleared();}
    }
}

// Who is aiming and how far they pulled (plan §7). One pointer owns an aim from touch-down to release;
// no other finger can move it or let it go. Whether the pull is long enough to throw is the model's rule.
public sealed class DadThrowAim
{
    public const int Mouse=-100;
    public bool Active {get;private set;} public int Finger {get;private set;}
    public Vector2 Start {get;private set;} public Vector2 Now {get;private set;}
    public Vector2 Pull {get{return Start-Now;}}
    // A new aim needs a ready hand and a touch-down on the open street, not on a button.
    public bool Begin(int finger,Vector2 point,bool canFire)
    {
        if(Active||!canFire||!DadThrowLayout.AimArea.Contains(point))return false;
        Active=true;Finger=finger;Start=Now=point;return true;
    }
    public bool Move(int finger,Vector2 point){if(!Active||finger!=Finger)return false;Now=point;return true;}
    // The owner let go: hands back the pull and ends the aim. Any other finger is ignored.
    public bool End(int finger,Vector2 point,out Vector2 pull)
    {
        pull=Vector2.zero;if(!Active||finger!=Finger)return false;
        Now=point;pull=Pull;Active=false;return true;
    }
    // Pause, lost focus, rotation, a weapon switch: the aim is dropped and nothing is thrown.
    public void Cancel(){Active=false;}
}
