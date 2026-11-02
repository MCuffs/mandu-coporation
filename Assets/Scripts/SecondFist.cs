using UnityEngine;
using System.Collections.Generic;

public class SecondFist : MonoBehaviour
{
    enum Phase { Intro, Fight, Won, Lost }
    class Rival { public int side, hp; public float clock, windup; public bool heavy; }
    readonly List<Rival> rivals = new List<Rival>();
    Phase phase; Font font; Texture2D pixel; AudioSource audioSource;
    float hp=100, stamina=100, elapsed, spawn, lockout, flash, punch, noticeTime;
    int defeated, combo, best, facing=1; string notice=""; int page;
    const int Goal=12;
    readonly Color ink=new Color(.045f,.055f,.09f), mint=new Color(.54f,1,.73f), red=new Color(1,.33f,.35f);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name!="FirstFight") return; if (FindObjectOfType<SecondFist>()==null) new GameObject("SecondFist").AddComponent<SecondFist>(); }
    void Awake() {
        Application.targetFrameRate=60;
        font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},24);
        pixel=Texture2D.whiteTexture; audioSource=gameObject.AddComponent<AudioSource>();
        phase=Phase.Intro;
    }
    void Begin() { phase=Phase.Fight; rivals.Clear(); hp=stamina=100; elapsed=0; spawn=.7f; defeated=combo=best=0; lockout=flash=punch=0; Tell("다가오는 주먹을 읽어라."); }
    void Tell(string s) { notice=s; noticeTime=1.2f; }
    void Update() {
        float dt=Time.deltaTime; flash=Mathf.Max(0,flash-dt); punch=Mathf.Max(0,punch-dt); noticeTime-=dt;
        if(phase!=Phase.Fight) return;
        elapsed+=dt; lockout-=dt; stamina=Mathf.Min(100,stamina+22*dt); spawn-=dt;
        if(spawn<=0 && rivals.Count<2 && defeated+rivals.Count<Goal) {
            int side=rivals.Count==0?(Random.value<.5f?-1:1):-rivals[0].side;
            bool heavy=defeated>=4 && Random.value<.35f;
            float windup=heavy?1.45f:Mathf.Max(.85f,1.3f-defeated*.025f);
            rivals.Add(new Rival{side=side,hp=heavy?3:2,clock=windup+.6f,windup=windup,heavy=heavy}); spawn=1.6f;
        }
        for(int i=rivals.Count-1;i>=0;i--) {
            Rival r=rivals[i]; r.clock-=dt;
            if(r.clock<=0) { hp-=r.heavy?24:16; combo=0; flash=.18f; r.clock=r.windup+.55f; Tell("맞았다! 주황색 예고 뒤 초록색 순간에 반격"); Tone(100,.12f); }
        }
        bool left=Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.LeftArrow);
        bool right=Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.RightArrow);
        if(Input.GetMouseButtonDown(0)) {
            float scale=Mathf.Min(Screen.width/540f,Screen.height/960f);
            Vector2 p=new Vector2((Input.mousePosition.x-(Screen.width-540*scale)/2)/scale,(Screen.height-Input.mousePosition.y-(Screen.height-960*scale)/2)/scale);
            if(p.x>=0 && p.x<=540 && p.y>245 && p.y<930) { left=p.x<270; right=!left; }
        }
        if(left) Strike(-1); else if(right) Strike(1);
        if(hp<=0) { hp=0; phase=Phase.Lost; }
        else if(defeated>=Goal) phase=Phase.Won;
    }
    void Strike(int side) {
        if(lockout>0) return;
        if(stamina<18) { Tell("숨을 골라라 — 연타하면 지친다"); return; }
        lockout=.23f; stamina-=18; facing=side; punch=.16f;
        Rival r=rivals.Find(x=>x.side==side);
        if(r==null || r.clock>r.windup) { combo=0; Tell("거리가 멀다. 가까워질 때까지 기다려라"); return; }
        bool counter=r.clock<=.32f;
        r.hp-=counter?3:1;
        if(counter) { stamina=Mathf.Min(100,stamina+27); combo++; best=Mathf.Max(best,combo); Tell("카운터!  /  "+combo+" 연속"); Tone(740,.09f); }
        else { combo=0; Tell("명중 — 초록색 순간에는 한 방에 제압!"); Tone(240,.06f); }
        if(r.hp<=0) { rivals.Remove(r); defeated++; spawn=Mathf.Min(spawn,.55f); }
        else r.clock=r.windup+.25f;
    }
    void Tone(float hz,float duration) {
        int n=(int)(22050*duration); float[] samples=new float[n];
        for(int i=0;i<n;i++) samples[i]=Mathf.Sin(i*hz*2*Mathf.PI/22050)*.09f*(1-i/(float)n);
        AudioClip clip=AudioClip.Create("hit",n,1,22050,false); clip.SetData(samples,0); audioSource.PlayOneShot(clip); Destroy(clip,duration+1);
    }
    void Box(float x,float y,float w,float h,Color c) { GUI.color=c; GUI.DrawTexture(new Rect(x,y,w,h),pixel); GUI.color=Color.white; }
    void Text(string s,float x,float y,float w,float h,int size,Color c,TextAnchor anchor=TextAnchor.UpperLeft) {
        GUI.Label(new Rect(x,y,w,h),s,new GUIStyle{font=font,fontSize=size,normal={textColor=c},alignment=anchor,wordWrap=true,fontStyle=FontStyle.Bold});
    }
    bool Button(string s,float x,float y,float w,float h) {
        Box(x,y,w,h,mint); Text(s,x,y,w,h,22,ink,TextAnchor.MiddleCenter);
        return GUI.Button(new Rect(x,y,w,h),GUIContent.none,GUIStyle.none);
    }
    void Limb(float x,float y,float length,float angle,float width,Color c) {
        Matrix4x4 old=GUI.matrix; GUI.matrix=old*Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.Euler(0,0,angle),Vector3.one); Box(0,0,width,length,c); GUI.matrix=old;
    }
    void Fighter(float x,float y,bool hero,int dir,bool attack,Color color,float size=1) {
        Matrix4x4 old=GUI.matrix; GUI.matrix=old*Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.identity,new Vector3(size,size,1));
        Box(-31,103,69,8,new Color(0,0,0,.3f));
        Color skin=new Color(.94f,.72f,.53f); Color pants=new Color(.08f,.10f,.16f);
        Limb(-18,51,52,7,15,pants); Limb(8,51,52,-9,15,pants);
        Box(-25,97,25,9,ink); Box(8,97,27,9,ink);
        Box(-26,-9,52,68,color); Box(-4,-8,8,55,hero?Color.white:ink);
        Box(-19,-49,38,39,skin); Box(-22,-55,44,14,ink); Box(-21,-43,9,13,ink);
        Box(dir>0?9:-15,-33,7,4,ink);
        Limb(-29,0,44,dir<0&&attack?85:20,13,color); Limb(19,0,44,dir>0&&attack?-85:-20,13,color);
        if(attack) { Box(dir>0?55:-70,-10,19,20,skin); Box(dir>0?80:-98,-24,18,5,mint); }
        GUI.matrix=old;
    }
    void Background() {
        Box(0,0,540,960,ink); Box(0,225,540,440,new Color(.10f,.14f,.20f));
        for(int i=0;i<7;i++) { Box(i*89-15,255-(i%3)*22,72,290,new Color(.065f,.085f,.13f)); for(int j=0;j<4;j++) Box(i*89+1,290+j*51,9,18,new Color(.27f,.31f,.28f)); }
        Box(0,545,540,210,new Color(.15f,.17f,.22f));
        for(int i=0;i<7;i++) Box(0,560+i*26,540,1,new Color(.21f,.23f,.27f));
        Box(24,346,9,194,new Color(.35f,.39f,.40f)); Box(22,342,79,7,mint);
        Text("골목 끝",39,364,100,40,18,mint); Box(440,450,70,85,new Color(.25f,.21f,.23f));
    }
    void OnGUI() {
        float s=Mathf.Min(Screen.width/540f,Screen.height/960f); GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-540*s)/2,(Screen.height-960*s)/2,0),Quaternion.identity,new Vector3(s,s,1));
        Background();
        Text("SECOND FIST",28,26,400,28,17,mint); Text("두 번째 주먹",26,60,480,60,40,Color.white);
        Text("EP. 01   아들의 자리",28,125,480,32,18,new Color(.65f,.70f,.78f));
        if(phase==Phase.Intro) {
            Fighter(272,514,true,1,false,mint,1.35f);
            Box(24,193,492,227,new Color(.055f,.07f,.11f,.97f));
            Text(page==0?"죽었어야 할 내가,\n아들의 몸에서 눈을 떴다.":"머리는 기억한다.\n하지만 이 몸은 너무 약하다.",45,218,450,100,28,Color.white);
            Text(page==0?"날 배신한 놈들보다 먼저,\n이 아이를 괴롭힌 놈들이 눈앞에 있다.":"좌우 터치 또는 A / D로 공격.\n적의 예고 표시가 초록색일 때 반격!\n연타하면 체력이 소모된다.",45,330,445,80,17,new Color(.72f,.77f,.83f));
            Text("1분 전투 · 12명 제압 · 타이밍 액션",30,746,480,45,18,Color.white,TextAnchor.MiddleCenter);
            if(Button(page==0?"눈을 뜬다  →":"첫 싸움 시작  →",30,817,480,72)) { if(page==0)page=1;else Begin(); }
            return;
        }
        Text("제압  "+defeated+" / "+Goal,28,178,260,35,24,Color.white); Text(elapsed.ToString("0.0")+"s",380,180,135,35,22,mint,TextAnchor.UpperRight);
        Box(28,222,484,9,new Color(.25f,.12f,.16f)); Box(28,222,484*hp/100,9,red);
        Fighter(270,535,true,facing,punch>0,mint);
        foreach(Rival r in rivals) {
            bool near=r.clock<=r.windup; float x=r.side<0?105:435; if(!near)x+=r.side*35;
            Fighter(x,537,false,-r.side,false,r.heavy?new Color(.64f,.40f,.78f):new Color(.89f,.46f,.32f),r.heavy?1.15f:1);
            if(near) {
                Color cue=r.clock<=.32f?mint:new Color(1,.66f,.29f);
                Box(x-42,439,84,7,new Color(.22f,.24f,.29f)); Box(x-42,439,84*Mathf.Clamp01(r.clock/r.windup),7,cue);
                Text(r.clock<=.32f?"반격!":r.heavy?"강타 예고":"공격 예고",x-65,405,130,32,20,cue,TextAnchor.MiddleCenter);
            }
            Text(new string('●',r.hp),x-50,665,100,30,13,red,TextAnchor.MiddleCenter);
        }
        Text(noticeTime>0?notice:"주황색은 준비 / 초록색은 반격",26,715,488,59,19,noticeTime>0?mint:Color.white,TextAnchor.MiddleCenter);
        Box(28,786,484,6,new Color(.20f,.25f,.29f)); Box(28,786,484*stamina/100,6,mint);
        Text("기력 "+Mathf.CeilToInt(stamina),28,798,480,22,13,new Color(.64f,.7f,.77f));
        Box(28,837,232,76,new Color(.14f,.20f,.25f)); Box(280,837,232,76,new Color(.14f,.20f,.25f));
        Text("← 왼쪽  [ A ]",28,837,232,76,22,mint,TextAnchor.MiddleCenter); Text("오른쪽  [ D ] →",280,837,232,76,22,mint,TextAnchor.MiddleCenter);
        if(flash>0) Box(0,0,540,960,new Color(1,.12f,.15f,.2f));
        if(phase==Phase.Won || phase==Phase.Lost) {
            Box(0,164,540,796,new Color(.03f,.04f,.07f,.96f));
            Text(phase==Phase.Won?"몸의 주인은… 누구지?":"이 몸은, 아직 약하다.",32,225,476,90,32,Color.white,TextAnchor.MiddleCenter);
            Text(phase==Phase.Won?"“그만해. 내 몸으로 뭐 하는 거야.”\n\n머릿속에서 아이의 목소리가 들렸다.\n내 아들은 아직 여기에 있다.":"싸움의 기억만으로는 부족하다.\n주먹이 다가올 때까지 기다리고,\n초록색 순간에 반격해 보자.",44,357,452,210,22,new Color(.77f,.81f,.87f),TextAnchor.MiddleCenter);
            Text("제압 "+defeated+" / "+Goal+"     최고 연속 반격 "+best,35,636,470,60,20,mint,TextAnchor.MiddleCenter);
            if(Button("다시 싸운다  →",30,817,480,72)) Begin();
        }
    }
}
