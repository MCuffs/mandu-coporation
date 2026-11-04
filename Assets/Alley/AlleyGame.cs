using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public partial class AlleyGame : MonoBehaviour {
    sealed class Pop { public string text;public Vector2 pos;public float life=1;public int style; }
    readonly List<Pop> pops=new List<Pop>();
    readonly List<Fighter> drawOrder=new List<Fighter>();
    public AlleyCombat combat;
    public Alley3DWorld world3D;
    Texture2D atlas,background,white,disc,round;
    Font font,fontBold,fontDisplay;GUIStyle panelStyle;AudioSource source;AudioClip hitSound,wallSound,specialSound;
    Vector2 stickOrigin,stick; bool dragging,requestSpecial;int dragFinger=-1;
    float shake,pulse,ring,walk; int stage;bool map;
    float Scale { get { return Mathf.Min(Screen.width/540f,Screen.height/960f); } }
    float ViewWidth { get { return 540f; } }
    float ViewHeight { get { return 960f; } }
    Vector2 ViewportOffset { get { float s=Scale;return new Vector2((Screen.width-540f*s)*.5f,(Screen.height-960f*s)*.5f); } }
    Vector2 SpecialPosition { get { return new Vector2(ViewWidth-72,ViewHeight-84); } }
    Vector2 Pointer(Vector2 screen) {
        float s=Scale;Vector2 off=ViewportOffset;
        return new Vector2((screen.x-off.x)/s,(Screen.height-screen.y-off.y)/s);
    }
    public bool IsStoryActive() { return storyActive; }
    public bool IsInGym() { return inGym; }
    public bool IsHome() { return home; }
    public int CurrentStage { get { return stage; } }

    void Awake() {
        Application.targetFrameRate=60;
#if !UNITY_EDITOR
        int targetH=Mathf.Min(1080,Display.main.systemHeight-80);
        int targetW=Mathf.RoundToInt(targetH*540f/960f);
        Screen.SetResolution(targetW,targetH,FullScreenMode.Windowed);
#endif
        font=Resources.Load<Font>("Fonts/GothicA1-Regular");
        fontBold=Resources.Load<Font>("Fonts/GothicA1-Bold");fontDisplay=Resources.Load<Font>("Fonts/BlackHanSans-Regular");
        if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},24);
        if(fontBold==null)fontBold=font;if(fontDisplay==null)fontDisplay=fontBold;
        atlas=Resources.Load<Texture2D>("FighterAtlas");background=Resources.Load<Texture2D>("AlleyBackground");
        white=Texture2D.whiteTexture;disc=Circle();round=Rounded();panelStyle=new GUIStyle{border=new RectOffset(8,8,8,8),normal={background=round}};
        source=gameObject.AddComponent<AudioSource>();hitSound=Sound(155,.055f);wallSound=Sound(80,.10f);specialSound=Sound(67,.20f);
        LoadPresentation();LoadArcade();LoadStages();LoadPrologue();
        world3D=null;
        if(Camera.main!=null){
            Camera.main.orthographic=true;
            Camera.main.backgroundColor=new Color(.04f,.05f,.08f);
        }
        NewRun();EnterGymHub();
    }
    void NewRun() {
        combat=new AlleyCombat(stage);pops.Clear();map=false;ring=shake=0;dragging=false;paused=false;
        PrepareArcade();ResetMotion();
        UpdateArenaViewport(true);
        float cx=combat.arena.center.x,cy=combat.player.y;
        combat.AddEnemy(new Vector2(cx-94,cy-38));combat.AddEnemy(new Vector2(cx-158,cy-42));
        combat.AddEnemy(new Vector2(cx+151,cy-121));combat.AddEnemy(new Vector2(cx+137,cy+113),1);
        combat.AddHazard(new Vector2(cx-118,cy+84),0);
        combat.AddHazard(new Vector2(cx+116,cy-82),1);
        StartBattle();
    }
    void OnApplicationFocus(bool focused) { if(!focused){dragging=false;stick=Vector2.zero;} }
    void EnsureLoaded() {
        if(world3D!=null){Destroy(world3D);world3D=null;}
        Transform old3D=transform.Find("World3D");
        if(old3D!=null)Destroy(old3D.gameObject);
        if(white!=null&&font!=null&&heroMotionAtlas!=null&&profile!=null)return;
        font=Resources.Load<Font>("Fonts/GothicA1-Regular");
        fontBold=Resources.Load<Font>("Fonts/GothicA1-Bold");fontDisplay=Resources.Load<Font>("Fonts/BlackHanSans-Regular");
        if(font==null)font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},24);
        if(fontBold==null)fontBold=font;if(fontDisplay==null)fontDisplay=fontBold;
        atlas=Resources.Load<Texture2D>("FighterAtlas");background=Resources.Load<Texture2D>("AlleyBackground");
        white=Texture2D.whiteTexture;if(disc==null)disc=Circle();if(round==null)round=Rounded();
        panelStyle=new GUIStyle{border=new RectOffset(8,8,8,8),normal={background=round}};
        if(source==null)source=GetComponent<AudioSource>()??gameObject.AddComponent<AudioSource>();
        if(hitSound==null)hitSound=Sound(155,.055f);if(wallSound==null)wallSound=Sound(80,.10f);if(specialSound==null)specialSound=Sound(67,.20f);
        LoadPresentation();LoadArcade();LoadStages();LoadPrologue();
        if(combat==null){NewRun();EnterGymHub();}
        else UpdateArenaViewport(true);
    }
    void Update() {
        EnsureLoaded();
        float dt=Time.unscaledDeltaTime;pulse+=dt;walk+=dt;shake=Mathf.Max(0,shake-dt*22);ring=Mathf.Max(0,ring-dt*2);
        for(int i=pops.Count-1;i>=0;i--){pops[i].life-=dt;pops[i].pos.y-=dt*29;if(pops[i].life<=0)pops.RemoveAt(i);}
        if(Input.GetKeyDown(KeyCode.Escape)&&!home&&(inGym||combat.state==RunState.Fighting)&&!map)paused=!paused;
        if(Input.GetKeyDown(KeyCode.F11))ToggleFullscreen();
        UpdateArenaViewport(false);
        AdvancePresentation(dt);
        if(storyActive){UpdatePrologue(dt);UpdateMotion(dt,false);if(world3D!=null)world3D.SyncWorld(this,dt);return;}
        if(home||stageSelect||styleMenu||map||paused){dragging=false;stick=Vector2.zero;UpdateMotion(dt,false);if(world3D!=null)world3D.SyncWorld(this,dt);return;}
        if(inGym){UpdateGymHub(dt);if(world3D!=null)world3D.SyncWorld(this,dt);return;}
        if(combat.state!=RunState.Fighting){dragging=false;stick=Vector2.zero;UpdateMotion(dt,false);if(world3D!=null)world3D.SyncWorld(this,dt);return;}
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
            if(e.style==3){shake=5;if(e.text=="전성기의 한 방"||e.text=="부활! 폭주 개시!")ring=1;source.PlayOneShot(specialSound,.6f);}
            else if(e.style==1||e.style==2){shake=Mathf.Max(shake,e.text=="퍽!"?1.1f:e.text=="크리티컬!"?3.2f:2.6f);source.PlayOneShot(e.text=="퍽!"?hitSound:wallSound,e.text=="퍽!"?.24f:.42f);}
            else if(e.style==4){shake=3;source.PlayOneShot(hitSound,.7f);}
            else if(e.text.StartsWith("+"))source.PlayOneShot(hitSound,.12f);
            else if(e.text=="−1")source.PlayOneShot(hitSound,.3f);
        }
        combat.events.Clear();SettleArcade();UpdateMotion(dt,true);
        if(world3D!=null)world3D.SyncWorld(this,dt);
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
        if(world3D!=null)return;
        if(stageBackgrounds!=null){GUI.color=Color.white;StageBackdrop(stage,new Rect(0,0,ViewWidth,ViewHeight));}
        else if(background!=null){GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,ViewWidth,ViewHeight),background,ScaleMode.ScaleAndCrop);}
        else Box(0,0,ViewWidth,ViewHeight,new Color(.72f,.67f,.55f));
    }
    void DrawHazard2D(AlleyHazard h){
        Vector2 p=h.pos;
        float pulseScale=1f+Mathf.Sin(pulse*8f+h.id)*.04f;
        Ellipse(p+new Vector2(0,4),44,14,new Color(0,0,0,.28f));
        if(h.kind==0){
            // Explosive Red Drum Barrel
            float w=34*pulseScale,ht=44*pulseScale;
            Panel(p.x-w*.5f-2,p.y-ht-2,w+4,ht+4,Ink);
            Panel(p.x-w*.5f,p.y-ht,w,ht,new Color(.88f,.20f,.16f));
            Box(p.x-w*.5f,p.y-ht*.68f,w,7,Gold);
            Box(p.x-w*.5f,p.y-ht*.32f,w,5,new Color(.18f,.20f,.24f));
            Outlined("💥",p.x-18,p.y-ht-16,36,24,14,Gold,TextAnchor.MiddleCenter,true);
        }else{
            // Golden Supply Crate
            float w=38*pulseScale,ht=36*pulseScale;
            Panel(p.x-w*.5f-2,p.y-ht-2,w+4,ht+4,Ink);
            Panel(p.x-w*.5f,p.y-ht,w,ht,Gold);
            Box(p.x-w*.5f+4,p.y-ht*.55f,w-8,6,Coral);
            Box(p.x-3,p.y-ht+4,6,ht-8,Coral);
            Outlined("🎁",p.x-18,p.y-ht-16,36,24,14,Cream,TextAnchor.MiddleCenter,true);
        }
    }
    void World(){
        Matrix4x4 old=GUI.matrix;Vector2 jitter=shake>0?new Vector2(Mathf.PerlinNoise(pulse*34,0)-.5f,Mathf.PerlinNoise(0,pulse*34)-.5f)*shake:Vector2.zero;
        GUI.matrix=old*Matrix4x4.Translate(jitter);
        Floor();
        DrawFallenActors();
        foreach(Fighter e in combat.enemies) {
            if(e.windup>0) {
                Color warning=new Color(1,.23f,.12f,.22f+Mathf.Sin(pulse*20)*.08f);
                if(e.kind>=1){
                    Vector2 aimEnd=e.pos+e.aim*210;
                    Line(e.pos,aimEnd,34,warning);Line(e.pos,aimEnd,2.5f,Coral);
                }else Ellipse(e.pos,e.appearance==0?96:78,34,warning);
            }
            if(e.stun>0&&e.velocity.sqrMagnitude<1000)Text("✦",e.pos.x-30,e.pos.y-155,60,30,22,new Color(1,.79f,.18f),TextAnchor.MiddleCenter);
        }
        foreach(AlleyHazard h in combat.hazards){
            if(h.hp>0)DrawHazard2D(h);
        }
        if(ring>0){Ring(combat.player-new Vector2(0,36),185*(1-ring)+15,9*ring,new Color(1,.53f,.18f,ring));Ring(combat.player-new Vector2(0,36),125*(1-ring)+30,3,new Color(1,.95f,.76f,ring));}
        drawOrder.Clear();drawOrder.AddRange(combat.enemies);drawOrder.Sort((a,b)=>a.pos.y.CompareTo(b.pos.y));
        bool heroDrawn=false,partnerDrawn=!combat.hasPartner;
        foreach(Fighter e in drawOrder){
            if(!partnerDrawn&&e.pos.y>combat.partnerPos.y){Partner();partnerDrawn=true;}
            if(!heroDrawn&&e.pos.y>combat.player.y){Hero();heroDrawn=true;}
            Enemy(e);
        }
        if(!partnerDrawn)Partner();
        if(!heroDrawn)Hero();
        DrawAfterimagesAndDust();
        DrawCombatText();
        GUI.matrix=old;
    }

    void Centered(System.Action draw){draw();}
    void DrawPillarbox(Vector2 off){
        Matrix4x4 prev=GUI.matrix;
        GUI.matrix=Matrix4x4.identity;
        if(off.x>1f){
            GUI.color=new Color(.03f,.04f,.06f);
            GUI.DrawTexture(new Rect(0,0,off.x+2,Screen.height),white);
            GUI.DrawTexture(new Rect(Screen.width-off.x-2,0,off.x+2,Screen.height),white);
            GUI.color=new Color(1f,.76f,.30f,.22f);
            GUI.DrawTexture(new Rect(off.x-1.5f,0,1.5f,Screen.height),white);
            GUI.DrawTexture(new Rect(Screen.width-off.x,0,1.5f,Screen.height),white);
        }
        if(off.y>1f){
            GUI.color=new Color(.03f,.04f,.06f);
            GUI.DrawTexture(new Rect(0,0,Screen.width,off.y+2),white);
            GUI.DrawTexture(new Rect(0,Screen.height-off.y-2,Screen.width,off.y+2),white);
        }
        GUI.color=Color.white;
        GUI.matrix=prev;
    }
    void OnGUI(){
        EnsureLoaded();
        float scale=Scale;
        Vector2 off=ViewportOffset;
        GUI.matrix=Matrix4x4.TRS(new Vector3(off.x,off.y,0),Quaternion.identity,new Vector3(scale,scale,1));
        if(home){HomeScreen();GUI.color=Color.white;DrawPillarbox(off);return;}
        if(storyActive){DrawPrologue();GUI.color=Color.white;DrawPillarbox(off);return;}
        if(inGym){
            DrawGymWorld();
            if(stageSelect){Box(0,0,ViewWidth,ViewHeight,new Color(.02f,.03f,.05f,.92f));Centered(DrawStageSelect);}
            else if(styleMenu){Box(0,0,ViewWidth,ViewHeight,new Color(.02f,.03f,.05f,.92f));Centered(StylePanel);}
            else if(paused)Centered(PausePanel);
            GUI.color=Color.white;DrawScreenTransition();DrawPillarbox(off);return;
        }
        Floor();
        if(!map){
            World();Header();HUD();
            if(paused)Centered(PausePanel);else if(combat.state==RunState.Choosing)Centered(Skills);else if(combat.state==RunState.Won||combat.state==RunState.Lost)Centered(ArcadeResult);
        }else Centered(ArcadeResult);
        DrawBattleReveal();GUI.color=Color.white;DrawScreenTransition();
        DrawPillarbox(off);
    }
    void OnDestroy(){Destroy(disc);Destroy(round);Destroy(hitSound);Destroy(wallSound);Destroy(specialSound);DestroyPrologue();}
}
