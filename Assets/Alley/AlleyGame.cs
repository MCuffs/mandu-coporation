using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public partial class AlleyGame : MonoBehaviour {
    sealed class Pop { public string text;public Vector2 pos;public float life=1;public int style; }
    readonly List<Pop> pops=new List<Pop>();
    readonly List<Fighter> drawOrder=new List<Fighter>();
    public AlleyCombat combat;
    Texture2D atlas,background,white,disc,round;
    Font font,fontBold,fontDisplay;GUIStyle panelStyle;AudioSource source;AudioClip hitSound,wallSound,specialSound;
    Vector2 stickOrigin,stick; bool dragging,requestSpecial;int dragFinger=-1;
    float shake,pulse,ring,walk; int stage;bool map;
    float Scale { get { return Mathf.Min(Screen.width/540f,Screen.height/960f); } }
    float ViewWidth { get { return Screen.width/Scale; } }
    float ViewHeight { get { return Screen.height/Scale; } }
    Vector2 SpecialPosition { get { return new Vector2(ViewWidth-76,ViewHeight-82); } }
    Vector2 Pointer(Vector2 screen) { return new Vector2(screen.x/Scale,(Screen.height-screen.y)/Scale); }
    void Awake() {
        Application.targetFrameRate=60;
#if !UNITY_EDITOR
        Screen.SetResolution(Display.main.systemWidth,Display.main.systemHeight,FullScreenMode.FullScreenWindow);
#endif
        font=Resources.Load<Font>("Fonts/GothicA1-Regular");
        fontBold=Resources.Load<Font>("Fonts/GothicA1-Bold");fontDisplay=Resources.Load<Font>("Fonts/BlackHanSans-Regular");
        if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},24);
        if(fontBold==null)fontBold=font;if(fontDisplay==null)fontDisplay=fontBold;
        atlas=Resources.Load<Texture2D>("FighterAtlas");background=Resources.Load<Texture2D>("AlleyBackground");
        white=Texture2D.whiteTexture;disc=Circle();round=Rounded();panelStyle=new GUIStyle{border=new RectOffset(8,8,8,8),normal={background=round}};
        source=gameObject.AddComponent<AudioSource>();hitSound=Sound(155,.055f);wallSound=Sound(80,.10f);specialSound=Sound(67,.20f);
        LoadPresentation();LoadArcade();LoadStages();NewRun();ReturnHome();
    }
    void NewRun() {
        combat=new AlleyCombat(stage);pops.Clear();map=false;ring=shake=0;dragging=false;paused=false;ResetMotion();
        UpdateArenaViewport(true);
        float cx=combat.arena.center.x,cy=combat.player.y;
        combat.AddEnemy(new Vector2(cx-94,cy-38));combat.AddEnemy(new Vector2(cx-158,cy-42));
        combat.AddEnemy(new Vector2(cx+151,cy-121));combat.AddEnemy(new Vector2(cx+137,cy+113),1);
        PrepareArcade();StartBattle();
    }
    void OnApplicationFocus(bool focused) { if(!focused){dragging=false;stick=Vector2.zero;} }
    void Update() {
        float dt=Time.unscaledDeltaTime;pulse+=dt;walk+=dt;shake=Mathf.Max(0,shake-dt*22);ring=Mathf.Max(0,ring-dt*2);
        for(int i=pops.Count-1;i>=0;i--){pops[i].life-=dt;pops[i].pos.y-=dt*29;if(pops[i].life<=0)pops.RemoveAt(i);}
        if(Input.GetKeyDown(KeyCode.Escape)&&!home&&combat.state==RunState.Fighting&&!map)paused=!paused;
        if(Input.GetKeyDown(KeyCode.F11))ToggleFullscreen();
        UpdateArenaViewport(false);
        AdvancePresentation(dt);
        if(storyActive){UpdatePrologue(dt);UpdateMotion(dt,false);return;}
        if(home||map||paused||combat.state!=RunState.Fighting){dragging=false;stick=Vector2.zero;UpdateMotion(dt,false);return;}
        Vector2 movement=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0));
        bool special=requestSpecial||Input.GetKeyDown(KeyCode.Space);requestSpecial=false;
        // Mouse emulation is used only when there are no real touches: avoid duplicate input on phones.
        if(Input.touchCount>0) {
            for(int i=0;i<Input.touchCount;i++) {
                Touch t=Input.GetTouch(i);Vector2 p=Pointer(t.position);
                if(t.phase==TouchPhase.Began) {
                    if((p-SpecialPosition).magnitude<62)special=true;
                    else if(!dragging&&p.y>105&&p.x>=0&&p.x<=ViewWidth){dragging=true;dragFinger=t.fingerId;stickOrigin=p;}
                }
                if(dragging&&t.fingerId==dragFinger) {
                    if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled){dragging=false;stick=Vector2.zero;}
                    else stick=Vector2.ClampMagnitude((p-stickOrigin)/48,1);
                }
            }
        } else {
            Vector2 p=Pointer(Input.mousePosition);
            if(Input.GetMouseButtonDown(0)) {
                if((p-SpecialPosition).magnitude<62)special=true;
                else if(p.y>105&&p.y<ViewHeight-10&&p.x>=0&&p.x<=ViewWidth){dragging=true;stickOrigin=p;}
            }
            if(dragging&&Input.GetMouseButton(0))stick=Vector2.ClampMagnitude((p-stickOrigin)/48,1);
            if(Input.GetMouseButtonUp(0)){dragging=false;stick=Vector2.zero;}
        }
        movement+=stick;
        combat.Tick(Time.deltaTime,movement,special);
        foreach(CombatEvent e in combat.events) {
            pops.Add(new Pop{text=e.text,pos=e.pos,style=e.style});AddImpact(e);
            if(e.style==3){shake=5;if(e.text=="전성기의 한 방")ring=1;source.PlayOneShot(specialSound,.6f);}
            else if(e.style==1||e.style==2){shake=Mathf.Max(shake,e.text=="퍽!"?1.1f:2.6f);source.PlayOneShot(e.text=="퍽!"?hitSound:wallSound,e.text=="퍽!"?.24f:.38f);}
            else if(e.style==4){shake=3;source.PlayOneShot(hitSound,.7f);}
            else if(e.text=="−1")source.PlayOneShot(hitSound,.3f);
        }
        combat.events.Clear();SettleArcade();UpdateMotion(dt,true);
    }
    AudioClip Sound(float hz,float duration) {
        int n=(int)(22050*duration);float[] data=new float[n];
        for(int i=0;i<n;i++){float t=i/22050f;data[i]=(Mathf.Sin(2*Mathf.PI*(hz*t-180*t*t))*.55f+Mathf.Sin(i*1.713f)*.12f)*Mathf.Pow(1-i/(float)n,2)*.22f;}
        AudioClip c=AudioClip.Create("Impact",n,1,22050,false);c.SetData(data,0);return c;
    }
    Texture2D Circle() {
        Texture2D t=new Texture2D(64,64,TextureFormat.RGBA32,false);Color[] data=new Color[4096];
        for(int y=0;y<64;y++)for(int x=0;x<64;x++)data[y*64+x]=new Color(1,1,1,Mathf.Clamp01((31.5f-Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f)))*1.5f));
        t.SetPixels(data);t.Apply();return t;
    }
    Texture2D Rounded() {
        Texture2D t=new Texture2D(32,32,TextureFormat.RGBA32,false);Color[] data=new Color[1024];
        for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=Mathf.Max(7-x,x-24),dy=Mathf.Max(7-y,y-24);float d=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;data[y*32+x]=new Color(1,1,1,Mathf.Clamp01(8-d));}
        t.SetPixels(data);t.Apply();return t;
    }
    static readonly Color Ink=new Color(.10f,.14f,.20f),Cream=new Color(1,.96f,.86f),Coral=new Color(1,.29f,.17f),Mint=new Color(.32f,.85f,.66f),Muted=new Color(.53f,.57f,.61f);
    void Box(float x,float y,float w,float h,Color c){GUI.color=c;GUI.DrawTexture(new Rect(x,y,w,h),white);GUI.color=Color.white;}
    void Panel(float x,float y,float w,float h,Color c){GUI.color=c;GUI.Box(new Rect(x,y,w,h),GUIContent.none,panelStyle);GUI.color=Color.white;}
    void CircleAt(Vector2 p,float radius,Color c){GUI.color=c;GUI.DrawTexture(new Rect(p.x-radius,p.y-radius,radius*2,radius*2),disc);GUI.color=Color.white;}


    void Line(Vector2 a,Vector2 b,float width,Color c){Matrix4x4 m=GUI.matrix;Vector2 d=b-a;GUI.matrix=m*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);Box(0,-width*.5f,d.magnitude,width,c);GUI.matrix=m;}
    void Ring(Vector2 p,float radius,float width,Color c){for(int i=0;i<36;i++){float a=i*Mathf.PI/18,b=(i+1)*Mathf.PI/18;Line(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,c);}}
    void DrawSprite(Vector2 feet,int cell,bool faceLeft,float height,Color color,float angle=0){
        if(atlas==null){CircleAt(feet-new Vector2(0,35),27,color);return;}
        float uvWidth=cell==1?.28125f:.25f;float width=height*uvWidth*3f;float offset=cell==1?height*.084f:0;Rect target=new Rect(-width*.5f+offset,-height*.87f,width,height);
        Matrix4x4 m=GUI.matrix;GUI.matrix=m*Matrix4x4.TRS(new Vector3(feet.x,feet.y,0),Quaternion.Euler(0,0,angle),new Vector3(faceLeft?-1:1,1,1));
        GUI.color=color;GUI.DrawTextureWithTexCoords(target,atlas,new Rect((cell%4)*.25f,cell<4?.5f:0,uvWidth,.5f));GUI.color=Color.white;GUI.matrix=m;
    }
    void Floor(){
        if(stageBackgrounds!=null){GUI.color=Color.white;StageBackdrop(stage,new Rect(0,0,ViewWidth,ViewHeight));}
        else if(background!=null){GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,ViewWidth,ViewHeight),background,ScaleMode.ScaleAndCrop);}
        else Box(0,0,ViewWidth,ViewHeight,new Color(.72f,.67f,.55f));
    }
    void World(){
        Matrix4x4 old=GUI.matrix;Vector2 jitter=shake>0?new Vector2(Mathf.PerlinNoise(pulse*34,0)-.5f,Mathf.PerlinNoise(0,pulse*34)-.5f)*shake:Vector2.zero;
        GUI.matrix=old*Matrix4x4.Translate(jitter);Floor();DrawFallenActors();
        foreach(Fighter e in combat.enemies) {
            if(e.windup>0) {
                Color warning=new Color(1,.23f,.12f,.24f+Mathf.Sin(pulse*20)*.10f);
                if(e.kind>=1){Line(e.pos,e.pos+e.aim*240,49,warning);Line(e.pos,e.pos+e.aim*240,2,Coral);}
                else CircleAt(e.pos,e.appearance==0?111:83,warning);
            }
            if(e.stun>0&&e.velocity.sqrMagnitude<1000)Text("✦",e.pos.x-30,e.pos.y-125,60,30,25,new Color(1,.79f,.18f),TextAnchor.MiddleCenter);
            if(e.velocity.sqrMagnitude>20000)for(int j=1;j<4;j++)CircleAt(e.pos-e.velocity.normalized*j*15,24-j*5,new Color(1,.65f,.27f,.14f));
        }
        if(ring>0){Ring(combat.player,185*(1-ring)+15,9*ring,new Color(1,.53f,.18f,ring));Ring(combat.player,125*(1-ring)+30,3,new Color(1,.95f,.76f,ring));}
        drawOrder.Clear();drawOrder.AddRange(combat.enemies);drawOrder.Sort((a,b)=>a.pos.y.CompareTo(b.pos.y));
        bool heroDrawn=false;
        foreach(Fighter e in drawOrder){if(!heroDrawn&&e.pos.y>combat.player.y){Hero();heroDrawn=true;}Enemy(e);}
        if(!heroDrawn)Hero();
        DrawAfterimagesAndDust();
        DrawCombatText();
        GUI.matrix=old;
    }








    void Centered(System.Action draw){Matrix4x4 old=GUI.matrix;GUI.matrix=old*Matrix4x4.Translate(new Vector3((ViewWidth-540)*.5f,(ViewHeight-960)*.5f,0));draw();GUI.matrix=old;}
    void OnGUI(){
        float scale=Scale;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        if(home){HomeScreen();GUI.color=Color.white;return;}
        if(storyActive){DrawPrologue();GUI.color=Color.white;return;}
        Floor();
        if(!map){
            World();Header();HUD();
            if(paused)Centered(PausePanel);else if(combat.state==RunState.Choosing)Centered(Skills);else if(combat.state==RunState.Won||combat.state==RunState.Lost)Centered(ArcadeResult);
        }else Centered(ArcadeResult);
        DrawBattleReveal();GUI.color=Color.white;DrawScreenTransition();
    }
    void OnDestroy(){Destroy(disc);Destroy(round);Destroy(hitSound);Destroy(wallSound);Destroy(specialSound);DestroyPrologue();}
}
