#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;
public sealed class DadHeadlessPlayerSmoke : MonoBehaviour
{
    string root;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        string[] args=Environment.GetCommandLineArgs();
        for(int i=0;i+1<args.Length;i++)if(args[i]=="--dad-headless-root")
        {var test=new GameObject("Headless smoke").AddComponent<DadHeadlessPlayerSmoke>();test.root=args[i+1];Directory.CreateDirectory(test.root);break;}
    }
    void Check(bool ok,string label)
    {
        File.AppendAllText(Path.Combine(root,"headless-report.txt"),(ok?"PASS ":"FAIL ")+label+"\n");
        if(!ok)throw new Exception(label);
    }
    IEnumerator Start()
    {
        yield return null;
        try
        {
            var game=FindObjectOfType<DadSchoolPrototype>();Check(game!=null,"main scene starts in actual Windows player");
            Check(Path.GetFullPath(DadProgress.DefaultPath).StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar),"smoke save is isolated");
            Check(Resources.Load<Font>("Fonts/GothicA1-Regular")!=null,"Korean font is included");
            Check(Resources.Load<TextAsset>("DadPrivacy")!=null,"offline privacy policy is included");
            Check(Resources.Load<Texture2D>("DadContentV2/lunch_cart")!=null,"cart artwork is included");
            game.GoHome();game.StartRun(1);game.SlingView.DebugAim(new Vector2(270,680),new Vector2(270,810));game.SlingView.SetPaused(true);
            float elapsed=game.Sling.elapsed;Check(game.SlingView.UserPaused&&!game.SlingView.Aiming,"pause cancels aim");
            game.SlingView.SetPaused(false);game.SlingView.DebugAim(new Vector2(270,680),new Vector2(270,810));game.SlingView.DebugRelease();
            Check(game.Sling.shots==1,"release fires once in compiled player");
            game.GoHome();game.StartQuickChallenge();
            var challenge=FindObjectOfType<DadFunLab>();Check(challenge!=null&&!game.enabled,"quick start enters challenge and disables campaign host");
            Check(DadFunLabModel.Titles.Length==12&&challenge.Model.level==2,"twelve stages are present and redirect is taught first");
            Check(challenge.Model.Launch(new Vector2(0,-150))&&challenge.Model.Boost(new Vector2(150,-50)),"compiled challenge launches and redirects");
            challenge.Exit();Check(game.enabled,"leaving challenge restores the campaign host");
            game.Progress.Save.coins=123;game.Progress.Persist();
            Check(new DadProgress(game.Progress.Eco,DadProgress.DefaultPath).Save.coins==123,"native JSON save round-trip succeeds");
            File.AppendAllText(Path.Combine(root,"headless-report.txt"),"LIMIT: no rendered frames or physical gestures were tested.\n");
            Application.Quit(0);
        }
        catch(Exception e){File.AppendAllText(Path.Combine(root,"headless-report.txt"),e+"\n");Application.Quit(1);}
    }
}
#endif
