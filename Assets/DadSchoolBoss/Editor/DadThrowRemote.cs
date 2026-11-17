using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Remote control for the throw prototype while the editor is open, so the scene can be run and photographed
// without touching the editor window. Nothing here runs unless a command is written.
// Write one command to Library/DadThrowCommand.txt (write a temp file, then rename it into place):
//   play            open Assets/DadThrowDefense.unity in a 1920x1080 Game view and enter Play Mode
//   pump            keep Play Mode drawing while the editor is in the background (needed for captures)
//   size:<w>x<h>    switch the Game view to a fixed resolution
//   capture:<png>   save the Game view
//   audit:<folder>  walk every screen with scripted input and save a picture of each (needs play, then pump)
//   shopaudit:<folder>  the stages and the shop: title, a stage paying out, the shop, trays stopping a ball, a shoe, defeat
//   stop            leave Play Mode
//   refresh         pick up changed files (the editor does not look while it is in the background)
// What happened is appended to Library/DadThrowRemote.log, errors and exceptions of the running game included.
[InitializeOnLoad]
public static class DadThrowRemote
{
    const string CommandPath="Library/DadThrowCommand.txt",LogPath="Library/DadThrowRemote.log";
    static bool pump;static DadThrowBots.Policy bot;static DadThrowBots.Run botRun;
    static readonly Queue<Wait> steps=new Queue<Wait>();static double nextStep;
    static DadThrowRemote()
    {
        EditorApplication.update+=Poll;Application.logMessageReceived+=Heard;
        Note("loaded; playing="+EditorApplication.isPlayingOrWillChangePlaymode);
    }
    static void Note(string line)
    {
        try{File.AppendAllText(LogPath,DateTime.Now.ToString("HH:mm:ss")+" "+line+"\n");}catch(IOException){}
    }
    static void Heard(string message,string stack,LogType type)
    {
        if(type==LogType.Log)return;
        Note(type+": "+message+(type==LogType.Exception||type==LogType.Error?"\n    "+stack.Trim().Replace("\n","\n    "):""));
    }
    static DadThrowPrototype Game {get{return UnityEngine.Object.FindObjectOfType<DadThrowPrototype>();}}

    // The Game view has no public size API; this reaches the same list the size menu shows.
    static void Size(int width,int height)
    {
        try
        {
            Assembly editor=typeof(Editor).Assembly;BindingFlags any=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
            Type sizesType=editor.GetType("UnityEditor.GameViewSizes"),viewType=editor.GetType("UnityEditor.GameView"),sizeType=editor.GetType("UnityEditor.GameViewSize"),kind=editor.GetType("UnityEditor.GameViewSizeType");
            object sizes=typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance",any).GetValue(null,null);
            object group=sizesType.GetProperty("currentGroup",any).GetValue(sizes,null);
            string label="Dad Throw "+width+"x"+height;
            var texts=(string[])group.GetType().GetMethod("GetDisplayTexts",any).Invoke(group,null);
            int index=Array.FindIndex(texts,text=>text.StartsWith(label));
            if(index<0)
            {
                object size=sizeType.GetConstructor(new[]{kind,typeof(int),typeof(int),typeof(string)}).Invoke(new object[]{Enum.Parse(kind,"FixedResolution"),width,height,label});
                group.GetType().GetMethod("AddCustomSize",any).Invoke(group,new[]{size});
                index=((string[])group.GetType().GetMethod("GetDisplayTexts",any).Invoke(group,null)).Length-1;
            }
            EditorWindow view=EditorWindow.GetWindow(viewType,false,null,false);
            viewType.GetProperty("selectedSizeIndex",any).SetValue(view,index,null);view.Repaint();
            Note("game view "+label+" (entry "+index+")");
        }
        catch(Exception error){Note("could not set the game view size: "+error);}
    }
    static void Shot(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));ScreenCapture.CaptureScreenshot(path);
        DadThrowPrototype g=Game;
        Note("capture "+path+" | screen "+Screen.width+"x"+Screen.height+(g!=null&&g.Flow!=null?" | "+g.Flow.Now+(g.Model!=null?" stage "+g.Flow.Stage.id+" clock "+g.Model.Clock.ToString("0.0")+" house "+g.Model.Health+" left "+g.Model.Remaining+" on street "+g.Model.enemies.Count:""):" | no flow: "+(g!=null?g.DebugMissing:"no prototype in the scene")));
    }
    // A step runs `seconds` after the one before it. With `ready`, it runs as soon as that is true, or after `seconds` at the latest.
    sealed class Wait { public float seconds;public Func<bool> ready;public Action action; }
    static void Step(float seconds,Action action){steps.Enqueue(new Wait{seconds=seconds,action=action});}
    static void Until(Func<bool> ready,float atMost,Action action=null){steps.Enqueue(new Wait{seconds=atMost,ready=ready,action=action??(()=>{})});}
    static void Play(DadThrowBots.Policy policy){bot=policy;botRun=new DadThrowBots.Run();}

    // Every screen once, in the order a player meets them. While it runs the game ignores the real mouse and keys,
    // so a click that strays into the Game view cannot change what is being photographed.
    static void Audit(string folder)
    {
        steps.Clear();bot=null;DadThrowPrototype g=Game;
        if(g==null){Note("audit: no DadThrowPrototype in the open scene");return;}
        Action<string> shot=name=>Shot(Path.Combine(folder,name+".png"));
        Func<DadThrowFlow> flow=()=>g.Flow;Func<DadThrowModel> model=()=>g.Model;
        Func<DadThrowFlow.Screen,Func<bool>> on=screen=>()=>g.Flow.Now==screen;
        // A new player in memory: the real save is neither read nor written while this runs.
        Step(0,()=>{Size(1920,1080);g.DebugScripted(true);g.DebugRestart();});
        Step(.6f,()=>shot("01-logo"));
        Until(on(DadThrowFlow.Screen.Title),3);
        Step(.3f,()=>shot("02-title"));
        Step(.2f,()=>flow().Start());
        // The opening: one picture from the middle of each cut.
        float[] middles={2,5.5f,9,13,17,20.5f,23};float told=0;
        for(int i=0;i<middles.Length;i++){int cut=i+1;Step(middles[i]-told,()=>shot("03-opening-"+cut));told=middles[i];}
        Until(on(DadThrowFlow.Screen.Playing),3);
        Step(.8f,()=>shot("04-1-1-waiting-for-first-pull"));
        Step(.2f,()=>g.DebugAim(new Vector2(760,300),new Vector2(615,290)));
        Step(.4f,()=>shot("05-1-1-aiming-golf"));
        Step(.1f,()=>g.DebugRelease());
        Step(.12f,()=>shot("06-1-1-just-thrown"));
        // The stampede itself, every two seconds, with a stand-in for a person throwing: it takes 0.45s to aim
        // and aims well two times in three. If it loses, the bot that never misses plays the stage again.
        Step(.3f,()=>Play(DadThrowBots.Sloppy(1,false)));
        foreach(int second in new[]{2,4,6,8,10}){int at=second;Until(()=>model().Clock>=at||g.Flow.Now!=DadThrowFlow.Screen.Playing,4,()=>shot("06-1-1-stampede-"+at.ToString("00")+"s"));}
        Until(()=>g.Flow.Now!=DadThrowFlow.Screen.Playing,40,()=>{Note("1-1 with the stand-in player: "+g.Flow.Now+", gate touched "+(5-model().Health)+", clock "+model().Clock.ToString("0.0"));if(g.Flow.Now==DadThrowFlow.Screen.Defeat&&flow().Retry())Play(DadThrowBots.Spam);});
        Until(on(DadThrowFlow.Screen.Clear),40,()=>bot=null);
        Step(.5f,()=>shot("07-1-1-clear-with-line"));
        Step(.3f,()=>flow().Next());
        // 1-3 runs from the moment it opens: there is no time to stand and look, in the script either.
        Step(.2f,()=>{shot("08-1-3-start-shoe-nudge");g.DebugSelect(DadThrowModel.Weapon.Shoe);g.DebugAim(new Vector2(760,300),new Vector2(640,330));});
        Step(.3f,()=>shot("09-1-3-aiming-shoe"));
        Step(.05f,()=>{g.DebugRelease();g.DebugSelect(DadThrowModel.Weapon.Golf);Play(DadThrowBots.Spam);});
        Until(()=>model().enemies.Exists(e=>e.slow>0),2,()=>shot("10-1-3-after-shoe"));
        Until(()=>model().ChainCount>=5,10,()=>shot("11-1-3-ball-through-pack"));
        Until(()=>model().Clock>=4||g.Flow.Now!=DadThrowFlow.Screen.Playing,6,()=>shot("11-1-3-stampede-04s"));
        Step(.2f,()=>{bot=null;flow().Pause();});
        Step(.4f,()=>shot("12-pause"));
        Step(.3f,()=>flow().Resume());
        // Nobody throws from here: the rest of the group reaches the gate.
        Until(()=>model().Health<5,40,()=>shot("13-gate-touched"));
        Until(()=>model().Health<=1,40);
        Step(.3f,()=>shot("14-one-touch-left"));
        Until(on(DadThrowFlow.Screen.Defeat),40);
        Step(.5f,()=>shot("15-defeat"));
        Step(.4f,()=>flow().Retry());
        Step(.2f,()=>Play(DadThrowBots.Spam));
        // The chapter's last stage, cleared for the first time: the ending, then the card.
        Until(on(DadThrowFlow.Screen.Ending),60,()=>bot=null);
        float[] closing={1.5f,4.5f,7.5f,10.5f};told=0;
        for(int i=0;i<closing.Length;i++){int cut=i+1;Step(closing[i]-told,()=>shot("16-ending-"+cut));told=closing[i];}
        Until(on(DadThrowFlow.Screen.Clear),4);
        Step(.5f,()=>shot("17-clear-last-stage"));
        // The same fight on a longer phone screen: the canvas must stay whole with bars at the sides.
        Step(.4f,()=>{flow().Retry();Size(2400,1080);});
        Step(3,()=>shot("18-long-screen-2400x1080"));
        // Hand the game back as the player left it: their own save, from the logo.
        Step(.6f,()=>{Size(1920,1080);g.DebugScripted(false);g.DebugReal();Note("audit done: "+folder+" | player save: "+g.DebugSavePath+(File.Exists(g.DebugSavePath)?" (exists)":" (none yet)"));});
        nextStep=EditorApplication.timeSinceStartup;
        Note("audit started: "+folder);
    }
    // The stages and the shop once through: the purse on the title, a stage paying out, the shop before and after
    // a purchase, a later stage where trays stop the balls until a shoe has been there, and the defeat card.
    // Nothing here touches the player's own save.
    static void ShopAudit(string folder)
    {
        steps.Clear();bot=null;DadThrowPrototype g=Game;
        if(g==null){Note("shop audit: no DadThrowPrototype in the open scene");return;}
        Action<string> shot=name=>Shot(Path.Combine(folder,name+".png"));
        Func<DadThrowFlow.Screen,Func<bool>> on=screen=>()=>g.Flow.Now==screen;
        Func<bool> fighting=()=>g.Flow.Now==DadThrowFlow.Screen.Playing&&g.Model!=null;
        // A flat throw every time the hand is ready: it goes at head height, so the nearest tray stops it.
        DadThrowBots.Policy flat=(m,run)=>{if(m.CanFire)m.Fire(DadThrowModel.Weapon.Golf,new Vector2(150,0));};
        Step(0,()=>{Size(1920,1080);g.DebugScripted(true);g.DebugStartAt(0,0);});
        Until(on(DadThrowFlow.Screen.Title),3);
        Step(.3f,()=>shot("shop-01-title-empty-purse"));
        Step(.2f,()=>{Note("start: "+g.Flow.Start());Play(DadThrowBots.Spam);});
        Until(()=>!fighting()||g.Model.Clock>=3,8,()=>shot("shop-02-stage-1-1-purse-counting"));
        Until(on(DadThrowFlow.Screen.Clear),60,()=>bot=null);
        Step(.5f,()=>shot("shop-03-clear-what-it-paid"));
        Step(.3f,()=>Note("shop: "+g.Flow.OpenShop()));
        Step(.4f,()=>shot("shop-04-shop-after-1-1"));
        Step(.3f,()=>Note("bought Sticky: "+g.Flow.Buy(DadThrowShop.Item.Sticky)));
        Step(.4f,()=>shot("shop-05-shop-after-buying"));
        // Further on, with a purse: stage 1-4 is the first with trays.
        Step(.3f,()=>g.DebugStartAt(3,300));
        Until(on(DadThrowFlow.Screen.Title),3);
        Step(.3f,()=>{Note("start: "+g.Flow.Start()+" "+g.Flow.Stage.id);Play(flat);});
        Until(()=>!fighting()||g.Model.BlockSerial>=2,14,()=>shot("shop-06-stage-1-4-tray-stops-the-ball"));
        // The hand has to be ready again before the shoe can leave it.
        Step(.05f,()=>bot=null);
        Until(()=>!fighting()||g.Model.CanFire,2,()=>
        {
            if(!fighting())return;
            // The shoe goes where the nearest tray is.
            DadThrowModel m=g.Model;DadThrowModel.Enemy holder=null;
            foreach(DadThrowModel.Enemy e in m.enemies)if(e.guard&&e.x<=DadThrowModel.Width&&(holder==null||e.x<holder.x))holder=e;
            if(holder==null){Note("no tray in view");return;}
            var path=new List<Vector2>();Vector2 best=DadThrowBots.Pulls[0];float off=float.MaxValue;
            foreach(Vector2 pull in DadThrowBots.Pulls){m.Predict(DadThrowModel.Weapon.Shoe,pull,path);float miss=Mathf.Abs(path[path.Count-1].x-holder.x);if(miss<off){off=miss;best=pull;}}
            g.DebugSelect(DadThrowModel.Weapon.Shoe);Note("shoe at the tray: "+m.Fire(DadThrowModel.Weapon.Shoe,best));
        });
        Until(()=>!fighting()||g.Model.enemies.Exists(e=>e.kind!=null&&e.kind.guards&&!e.guard),3,()=>shot("shop-07-shoe-knocks-the-tray-away"));
        // Nobody throws from here: the stage is lost, and the card offers the shop.
        Until(on(DadThrowFlow.Screen.Defeat),60);
        Step(.5f,()=>shot("shop-08-defeat-with-shop"));
        Step(.6f,()=>{g.DebugScripted(false);g.DebugReal();Note("shop audit done: "+folder);});
        nextStep=EditorApplication.timeSinceStartup;
        Note("shop audit started: "+folder);
    }
    static void Poll()
    {
        if(EditorApplication.isPlaying)
        {
            if(pump){EditorApplication.QueuePlayerLoopUpdate();UnityEditorInternal.InternalEditorUtility.RepaintAllViews();}
            DadThrowPrototype g=bot!=null?Game:null;
            if(g!=null&&g.Flow!=null&&g.Flow.Now==DadThrowFlow.Screen.Playing)
                try{bot(g.Model,botRun);}catch(Exception error){Note("bot: "+error);bot=null;}
            if(steps.Count>0)
            {
                Wait wait=steps.Peek();bool late=EditorApplication.timeSinceStartup>=nextStep+wait.seconds,ready=false;
                try{ready=wait.ready!=null&&wait.ready();}catch(Exception error){Note("wait failed: "+error);late=true;}
                if(ready||late)
                {
                    if(wait.ready!=null&&!ready)Note("waited "+wait.seconds+"s without the expected moment");
                    steps.Dequeue();nextStep=EditorApplication.timeSinceStartup;
                    try{wait.action();}catch(Exception error){Note("step failed: "+error);}
                }
            }
        }
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(CommandPath))return;
        string command=File.ReadAllText(CommandPath).Trim();
        if(command.Length==0)return;   // the writer has not finished yet
        File.Delete(CommandPath);Note("command: "+command);
        try
        {
            if(command=="play")
            {
                if(EditorApplication.isPlaying){Note("already playing");return;}
                EditorSceneManager.OpenScene(DadThrowBuild.ScenePath);Size(1920,1080);EditorApplication.isPlaying=true;
            }
            else if(command=="stop"){pump=false;bot=null;steps.Clear();EditorApplication.isPlaying=false;}
            else if(command=="refresh")AssetDatabase.Refresh();
            else if(!EditorApplication.isPlaying)Note("ignored: not in Play Mode");
            else if(command=="pump"){pump=true;Application.runInBackground=true;}
            else if(command.StartsWith("size:")){string[] wh=command.Substring(5).Split('x');Size(int.Parse(wh[0]),int.Parse(wh[1]));}
            else if(command.StartsWith("capture:"))Shot(command.Substring(8));
            else if(command.StartsWith("audit:"))Audit(command.Substring(6));
            else if(command.StartsWith("shopaudit:"))ShopAudit(command.Substring(10));
            else Note("unknown command");
        }
        catch(Exception error){Note("command failed: "+error);}
    }
}
