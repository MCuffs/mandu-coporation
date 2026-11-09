using System;
using UnityEngine;

// Opt-in Windows DEVELOPMENT_BUILD demo only: no SDK, network request or real ad impression.
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
public sealed class DadAdFlowDemo : MonoBehaviour, IDadRewardedProvider
{
    Action<bool> callback; float opened; Font font;
    public bool Ready { get { return callback==null; } }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--dad-ad-demo")<0)return;
        var demo=new GameObject("Explicit ad flow demo").AddComponent<DadAdFlowDemo>();
        demo.font=Resources.Load<Font>("Fonts/GothicA1-Regular");DontDestroyOnLoad(demo.gameObject);DadRewardedAds.Bind(demo);
    }
    public void Show(Action<bool> done) { callback=done;opened=Time.realtimeSinceStartup; }
    void OnGUI()
    {
        if(callback==null)return;GUI.depth=-100;
        GUI.color=new Color(.08f,.12f,.1f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);GUI.color=Color.white;
        var style=new GUIStyle(GUI.skin.label){font=font,fontSize=24,alignment=TextAnchor.MiddleCenter,wordWrap=true};
        GUI.Label(new Rect(30,Screen.height*.25f,Screen.width-60,180),"보상 광고 흐름 테스트\n실제 광고가 아니며 수익이 발생하지 않습니다.\n완료 / 취소 동작만 확인합니다.",style);
        float y=Screen.height*.6f;GUI.enabled=Time.realtimeSinceStartup-opened>=5;
        if(GUI.Button(new Rect(40,y,Screen.width-80,60),"TEST COMPLETE (5s)")){var done=callback;callback=null;done(true);}
        GUI.enabled=true;
        if(GUI.Button(new Rect(40,y+80,Screen.width-80,60),"CANCEL")){var done=callback;callback=null;done(false);}
    }
}
#endif
