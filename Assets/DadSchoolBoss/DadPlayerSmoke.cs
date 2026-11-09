#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using UnityEngine;

// Development player only. Runs against an isolated save when explicitly launched with --dad-smoke-root.
public sealed class DadPlayerSmoke : MonoBehaviour
{
    string root;float started;bool complete;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        string[] args=Environment.GetCommandLineArgs();
        for(int i=0;i+1<args.Length;i++)if(args[i]=="--dad-smoke-root")
        {Application.runInBackground=true;var test=new GameObject("Native player smoke").AddComponent<DadPlayerSmoke>();test.root=args[i+1];test.started=-1;Directory.CreateDirectory(test.root);File.WriteAllText(Path.Combine(test.root,"report.txt"),"Native macOS player smoke\n");break;}
    }
    bool Check(bool success,string name)
    {
        File.AppendAllText(Path.Combine(root,"report.txt"),(success?"PASS ":"FAIL ")+name+"\n");
        if(!success){complete=true;Application.Quit(1);}return success;
    }
    IEnumerator Start()
    {
        started=Time.realtimeSinceStartup;yield return new WaitForSecondsRealtime(1);
        var game=FindObjectOfType<DadSchoolPrototype>();if(!Check(game!=null,"main scene loads the game"))yield break;
        if(!Check(game.Progress!=null&&DadProgress.DefaultPath.StartsWith(root),"save is isolated from the player's progress"))yield break;
        if(!Check(Resources.Load<Font>("Fonts/GothicA1-Regular")!=null&&Resources.Load<Font>("Fonts/BlackHanSans-Regular")!=null,"Korean font resources exist in the player"))yield break;
        game.GoHome();yield return new WaitForSecondsRealtime(.4f);yield return Capture("01-home.png");
        game.StartRun(1);game.SlingView.DebugAim(new Vector2(270,680),new Vector2(270,810));yield return new WaitForSecondsRealtime(.3f);
        if(!Check(game.Sling!=null&&game.SlingView.Aiming,"battle and pull input initialize in the standalone player"))yield break;
        game.SlingView.SetPaused(true);float time=game.Sling.elapsed;Vector2 position=game.Sling.dad.pos;
        yield return new WaitForSecondsRealtime(.4f);
        if(!Check(game.SlingView.UserPaused&&game.Sling.elapsed==time&&game.Sling.dad.pos==position,"pause freezes simulation in the real player"))yield break;
        yield return Capture("02-pause.png");
        game.SlingView.SetPaused(false);game.SlingView.DebugAim(new Vector2(270,680),new Vector2(270,810));game.SlingView.DebugRelease();
        yield return new WaitForSecondsRealtime(.25f);
        if(!Check(game.Sling.shots==1,"resuming and releasing fires exactly one shot"))yield break;
        yield return Capture("03-flight.png");yield return new WaitForSecondsRealtime(.4f);
        if(!Check(File.Exists(Path.Combine(root,"01-home.png"))&&File.Exists(Path.Combine(root,"02-pause.png")),"native player rendered home and pause captures"))yield break;
        complete=true;Application.Quit(0);
    }
    IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
        texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();
        File.WriteAllBytes(Path.Combine(root,name),texture.EncodeToPNG());Destroy(texture);
    }
    void Update(){if(!complete&&started>=0&&Time.realtimeSinceStartup-started>20){Check(false,"smoke run completed before timeout");}}
}
#endif
