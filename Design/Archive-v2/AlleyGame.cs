using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public class AlleyGame : MonoBehaviour {
    sealed class Pop { public string text;public Vector2 pos;public float life=1;public int style; }
    readonly List<Pop> pops=new List<Pop>();
    readonly List<Fighter> drawOrder=new List<Fighter>();
    public AlleyCombat combat;
    Texture2D atlas,background,white,disc,round;
    Font font;GUIStyle panelStyle;AudioSource source;AudioClip hitSound,wallSound,specialSound;
    Vector2 stickOrigin,stick; bool dragging,requestSpecial;int dragFinger=-1;
    float shake,pulse,ring,walk; int stage;bool map;
    float Scale { get { return Mathf.Min(Screen.width/540f,Screen.height/960f); } }
    Vector2 Pointer(Vector2 screen) { float s=Scale;return new Vector2((screen.x-(Screen.width-540*s)*.5f)/s,(Screen.height-screen.y-(Screen.height-960*s)*.5f)/s); }
    void Awake() {
        Application.targetFrameRate=60;
        font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},24);
        atlas=Resources.Load<Texture2D>("FighterAtlas");background=Resources.Load<Texture2D>("AlleyBackground");
        white=Texture2D.whiteTexture;disc=Circle();round=Rounded();panelStyle=new GUIStyle{border=new RectOffset(8,8,8,8),normal={background=round}};
        source=gameObject.AddComponent<AudioSource>();hitSound=Sound(155,.055f);wallSound=Sound(80,.10f);specialSound=Sound(67,.20f);
        NewRun();
    }
    void NewRun() {
        combat=new AlleyCombat(stage);pops.Clear();map=false;ring=shake=0;dragging=false;
        combat.AddEnemy(new Vector2(176,475));combat.AddEnemy(new Vector2(110,465));combat.AddEnemy(new Vector2(400,380));combat.AddEnemy(new Vector2(405,665),1);
    }
    void OnApplicationFocus(bool focused) { if(!focused){dragging=false;stick=Vector2.zero;} }
    void Update() {
        float dt=Time.unscaledDeltaTime;pulse+=dt;walk+=dt;shake=Mathf.Max(0,shake-dt*22);ring=Mathf.Max(0,ring-dt*2);
        for(int i=pops.Count-1;i>=0;i--){pops[i].life-=dt;pops[i].pos.y-=dt*29;if(pops[i].life<=0)pops.RemoveAt(i);}
        if(map||combat.state!=RunState.Fighting){dragging=false;stick=Vector2.zero;return;}
        Vector2 movement=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0));
        bool special=requestSpecial||Input.GetKeyDown(KeyCode.Space);requestSpecial=false;
        // Mouse emulation is used only when there are no real touches: avoid duplicate input on phones.
        if(Input.touchCount>0) {
            for(int i=0;i<Input.touchCount;i++) {
                Touch t=Input.GetTouch(i);Vector2 p=Pointer(t.position);
                if(t.phase==TouchPhase.Began) {
                    if((p-new Vector2(452,864)).magnitude<62)special=true;
                    else if(!dragging&&p.y>220&&p.x>=0&&p.x<=540){dragging=true;dragFinger=t.fingerId;stickOrigin=p;}
                }
                if(dragging&&t.fingerId==dragFinger) {
                    if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled){dragging=false;stick=Vector2.zero;}
                    else stick=Vector2.ClampMagnitude((p-stickOrigin)/48,1);
                }
            }
        } else {
            Vector2 p=Pointer(Input.mousePosition);
            if(Input.GetMouseButtonDown(0)) {
                if((p-new Vector2(452,864)).magnitude<62)special=true;
                else if(p.y>220&&p.y<940&&p.x>=0&&p.x<=540){dragging=true;stickOrigin=p;}
            }
            if(dragging&&Input.GetMouseButton(0))stick=Vector2.ClampMagnitude((p-stickOrigin)/48,1);
            if(Input.GetMouseButtonUp(0)){dragging=false;stick=Vector2.zero;}
        }
        movement+=stick;
        combat.Tick(Time.deltaTime,movement,special);
        foreach(CombatEvent e in combat.events) {
            pops.Add(new Pop{text=e.text,pos=e.pos,style=e.style});
            if(e.style==3){shake=9;ring=1;source.PlayOneShot(specialSound,.6f);}
            else if(e.style==1||e.style==2){shake=Mathf.Max(shake,4);source.PlayOneShot(wallSound,.45f);}
            else if(e.style==4){shake=6;source.PlayOneShot(hitSound,.7f);}
            else if(e.text=="−1")source.PlayOneShot(hitSound,.3f);
        }
        combat.events.Clear();
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
    void Text(string value,float x,float y,float w,float h,int size,Color color,TextAnchor anchor=TextAnchor.UpperLeft,bool bold=true){
        GUI.Label(new Rect(x,y,w,h),value,new GUIStyle{font=font,fontSize=size,fontStyle=bold?FontStyle.Bold:FontStyle.Normal,normal={textColor=color},alignment=anchor,wordWrap=true});
    }
    bool Button(string value,float x,float y,float w,float h,Color color) {
        Panel(x,y+4,w,h,Ink);Panel(x,y,w,h,color);Text(value,x,y,w,h,23,color==Cream?Ink:Color.white,TextAnchor.MiddleCenter);
        return GUI.Button(new Rect(x,y,w,h),GUIContent.none,GUIStyle.none);
    }
    void Line(Vector2 a,Vector2 b,float width,Color c){Matrix4x4 m=GUI.matrix;Vector2 d=b-a;GUI.matrix=m*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg),Vector3.one);Box(0,-width*.5f,d.magnitude,width,c);GUI.matrix=m;}
    void Ring(Vector2 p,float radius,float width,Color c){for(int i=0;i<36;i++){float a=i*Mathf.PI/18,b=(i+1)*Mathf.PI/18;Line(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,p+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,width,c);}}
    void DrawSprite(Vector2 feet,int cell,bool faceLeft,float height,Color color,float angle=0){
        if(atlas==null){CircleAt(feet-new Vector2(0,35),27,color);return;}
        float uvWidth=cell==1?.28125f:.25f;float width=height*uvWidth*3f;float offset=cell==1?height*.084f:0;Rect target=new Rect(-width*.5f+offset,-height*.87f,width,height);
        Matrix4x4 m=GUI.matrix;GUI.matrix=m*Matrix4x4.TRS(new Vector3(feet.x,feet.y,0),Quaternion.Euler(0,0,angle),new Vector3(faceLeft?-1:1,1,1));
        GUI.color=color;GUI.DrawTextureWithTexCoords(target,atlas,new Rect((cell%4)*.25f,cell<4?.5f:0,uvWidth,.5f));GUI.color=Color.white;GUI.matrix=m;
    }
    void Floor(){
        if(background!=null){GUI.color=Color.white;GUI.DrawTexture(new Rect(0,170,540,650),background,ScaleMode.StretchToFill);}
        else {
            Box(0,170,540,650,new Color(.72f,.67f,.55f));
            for(int y=200;y<820;y+=32)for(int x=-25;x<540;x+=66){Box(x+((y/32)%2)*33,y,64,30,new Color(.76f,.72f,.62f));}
            Box(0,220,45,560,new Color(.37f,.33f,.29f));Box(495,220,45,560,new Color(.43f,.30f,.25f));
        }
        // Collision bounds read clearly as alley edges; dark curb lines also frame the playable floor.
        Line(new Vector2(47,280),new Vector2(47,760),4,new Color(.17f,.22f,.22f,.65f));
        Line(new Vector2(493,280),new Vector2(493,760),4,new Color(.17f,.22f,.22f,.65f));
    }
    void World(){
        Matrix4x4 old=GUI.matrix;Vector2 jitter=shake>0?new Vector2(Mathf.Sin(pulse*177),Mathf.Cos(pulse*141))*shake:Vector2.zero;
        GUI.matrix=old*Matrix4x4.Translate(jitter);Floor();
        foreach(Fighter e in combat.enemies) {
            if(e.windup>0) {
                Color warning=new Color(1,.23f,.12f,.24f+Mathf.Sin(pulse*20)*.10f);
                if(e.kind>=1){Line(e.pos,e.pos+e.aim*240,49,warning);Line(e.pos,e.pos+e.aim*240,2,Coral);}
                else CircleAt(e.pos,77,warning);
            }
            if(e.stun>0&&e.velocity.sqrMagnitude<1000)Text("✦",e.pos.x-30,e.pos.y-125,60,30,25,new Color(1,.79f,.18f),TextAnchor.MiddleCenter);
            if(e.velocity.sqrMagnitude>20000)for(int j=1;j<4;j++)CircleAt(e.pos-e.velocity.normalized*j*15,24-j*5,new Color(1,.65f,.27f,.14f));
        }
        if(ring>0){Ring(combat.player,185*(1-ring)+15,9*ring,new Color(1,.53f,.18f,ring));Ring(combat.player,125*(1-ring)+30,3,new Color(1,.95f,.76f,ring));}
        drawOrder.Clear();drawOrder.AddRange(combat.enemies);drawOrder.Sort((a,b)=>a.pos.y.CompareTo(b.pos.y));
        bool heroDrawn=false;
        foreach(Fighter e in drawOrder){if(!heroDrawn&&e.pos.y>combat.player.y){Hero();heroDrawn=true;}Enemy(e);}
        if(!heroDrawn)Hero();
        foreach(Pop p in pops){if(p.text.Length==0)continue;Color c=p.style==4?Coral:p.style==2?Mint:p.style==0?Cream:new Color(1,.82f,.24f);c.a=Mathf.Min(1,p.life*3);int size=p.style==3?32:p.style==1||p.style==2?24:17;Text(p.text,p.pos.x-120+2,p.pos.y-92+2,240,65,size,new Color(Ink.r,Ink.g,Ink.b,c.a),TextAnchor.MiddleCenter);Text(p.text,p.pos.x-120,p.pos.y-92,240,65,size,c,TextAnchor.MiddleCenter);}
        GUI.matrix=old;
    }
    void Hero(){
        Vector2 p=combat.player;CircleAt(p+new Vector2(0,-3),26,new Color(0,0,0,.2f));Ring(p,27,2,new Color(.28f,.88f,.70f,.7f));
        bool moving=stick.sqrMagnitude>.05f||Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.UpArrow)||Input.GetKey(KeyCode.DownArrow)||Input.GetKey(KeyCode.LeftArrow)||Input.GetKey(KeyCode.RightArrow);
        int cell=combat.punch>0?1:moving?2+(Mathf.FloorToInt(walk*9)%2):0;
        Color tint=combat.invulnerable>0&&Mathf.Sin(pulse*45)>0?new Color(1,.52f,.42f):Color.white;
        DrawSprite(p,cell,combat.direction.x<0,130,tint);
        if(combat.punch>0){float angle=Mathf.Atan2(combat.direction.y,combat.direction.x);float radius=72;Vector2 previous=p+new Vector2(Mathf.Cos(angle-.95f),Mathf.Sin(angle-.95f))*radius-new Vector2(0,38);for(int i=1;i<=12;i++){float a=angle-.95f+i*1.9f/12;Vector2 point=p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius-new Vector2(0,38);Line(previous,point,12,new Color(1,.38f,.15f,.9f));Line(previous-new Vector2(0,3),point-new Vector2(0,3),3,Cream);previous=point;}}
    }
    void Enemy(Fighter e){
        Vector2 p=e.pos;CircleAt(p-new Vector2(0,2),e.boss?37:25,new Color(0,0,0,.23f));
        int cell=e.kind==0?4:6;if(e.flash>0||e.velocity.sqrMagnitude>4000)cell++;
        Color tint=e.flash>0?new Color(1,.6f,.43f):e.boss?new Color(1,.82f,.77f):Color.white;
        DrawSprite(p,cell,e.facing<0,e.boss?177:e.kind==1?144:125,tint,e.velocity.sqrMagnitude>4000?Mathf.Clamp(e.velocity.x*.018f,-18,18):0);
        if(e.hp<e.maxHp||e.boss){float w=e.boss?78:43;Panel(p.x-w/2,p.y-(e.boss?166:119),w,7,Ink);Panel(p.x-w/2,p.y-(e.boss?166:119),w*Mathf.Max(0,e.hp)/(float)e.maxHp,7,e.boss?Coral:Mint);}
        if(e.windup>0)Text("!",p.x-20,p.y-(e.boss?202:153),40,40,29,Coral,TextAnchor.MiddleCenter);
    }
    void Header(){
        Box(0,0,540,173,Ink);Text("SECOND FIST  /  EP. 01",24,19,490,27,14,Mint);
        Text("두 번째 주먹",22,48,345,54,35,Cream);Panel(407,49,109,40,Coral);Text("골목 액션",407,49,109,40,17,Color.white,TextAnchor.MiddleCenter);
        Text(stage==0?"학교 뒷골목":stage==1?"편의점 앞": "황제 당구장",24,112,280,27,17,Cream);
        Text(combat.state==RunState.Ready?"첫 싸움":combat.elapsed.ToString("00.0")+" s",360,111,156,27,18,Mint,TextAnchor.UpperRight);
        Panel(24,145,492,11,new Color(.3f,.24f,.25f));Panel(24,145,492*combat.health/100,11,Coral);
    }
    void HUD(){
        Panel(15,190,155,37,Ink);Text("제압 "+combat.kills+" / 18",15,190,155,37,17,Cream,TextAnchor.MiddleCenter);
        if(combat.bossSpawned){Panel(300,190,225,37,Coral);Text("골목 대장을 쓰러뜨려라",300,190,225,37,15,Color.white,TextAnchor.MiddleCenter);}
        else {Panel(340,190,185,37,Ink);Text("LV."+combat.level+"  전성기의 기억",340,190,185,37,14,Mint,TextAnchor.MiddleCenter);}
        if(combat.chain>0&&combat.comboClock>0){Text(combat.chain+" 연쇄!",140,238,260,48,32,Cream,TextAnchor.MiddleCenter);}
        Box(0,802,540,158,Ink);Box(0,802,540*combat.xp/(float)combat.nextXp,5,Mint);
        Vector2 basePos=dragging?new Vector2(Mathf.Clamp(stickOrigin.x,66,350),Mathf.Clamp(stickOrigin.y,350,875)):new Vector2(88,870);
        CircleAt(basePos,53,new Color(.30f,.36f,.40f,.6f));Ring(basePos,52,2,new Color(.70f,.78f,.76f,.6f));CircleAt(basePos+stick*31,25,new Color(.80f,.85f,.82f,.85f));
        Text("이동",30,930,118,25,13,Muted,TextAnchor.MiddleCenter);
        CircleAt(new Vector2(452,864),61,new Color(.06f,.08f,.12f));CircleAt(new Vector2(452,864),54,combat.charge>=100?Coral:new Color(.29f,.27f,.29f));Ring(new Vector2(452,864),57,3,combat.charge>=100?Cream:Muted);
        // A readable hand silhouette built from knuckles and a palm; avoids platform emoji fonts.
        for(int i=0;i<4;i++)Panel(428+i*11,839+(i==0?4:0),10,26,Cream);Panel(429,857,42,26,Cream);Panel(420,858,13,21,Cream);
        Text(combat.charge>=100?"전성기의 한 방":"충전 "+Mathf.FloorToInt(combat.charge)+"%",362,931,178,24,14,combat.charge>=100?Cream:Muted,TextAnchor.MiddleCenter);
        Text("자동 공격",164,845,180,30,17,Cream,TextAnchor.MiddleCenter);Text("몰아넣고, 날려버려!",150,881,204,28,13,Mint,TextAnchor.MiddleCenter);
        if(combat.elapsed<7) {Panel(52,735,436,43,new Color(.10f,.14f,.20f,.92f));Text("WASD / 드래그 이동   ·   SPACE 필살기",52,735,436,43,16,Cream,TextAnchor.MiddleCenter);}
    }
    void Intro(){
        Box(0,173,540,629,new Color(.07f,.09f,.13f,.38f));
        Panel(25,222,490,172,Cream);Text("몸은 아들,\n주먹은 전국구.",46,242,450,117,37,Ink);Text("한 손으로 몰아붙이는 골목 액션",47,357,447,28,16,new Color(.51f,.32f,.23f));
        DrawSprite(new Vector2(280,652),1,false,277,Color.white);
        Panel(25,683,490,109,Cream);Text("01  움직이면 가까운 적을 자동 공격\n02  벽과 적에게 날려 연쇄 제압\n03  필살기와 기술 조합으로 보스에 도전",44,698,460,90,17,Ink);
        Box(0,802,540,158,Ink);
        if(Button("첫 싸움 시작  →",24,825,492,70,Coral))combat.Start();
        Text("WASD / 방향키 / 드래그 이동  ·  SPACE 필살기",20,921,500,25,14,Muted,TextAnchor.MiddleCenter);
    }
    void Skills(){
        Box(0,173,540,787,new Color(.055f,.07f,.10f,.87f));
        Text("전성기의 기억",25,203,490,65,36,Cream,TextAnchor.MiddleCenter);Text("이번 싸움에서 되찾을 기술을 선택하세요",25,272,490,32,17,Mint,TextAnchor.MiddleCenter);
        string[] names={"벽꿍","충돌 전파","연쇄 기절"};string[] desc={"벽에 부딪힌 적에게\n추가 피해 +1","날아간 적이 주변 적에게\n주는 충돌 피해 +1","충돌한 적을 더 오래 기절시켜\n포위망을 무너뜨린다"};int[] ranks={combat.wallSkill,combat.chainSkill,combat.stunSkill};
        for(int i=0;i<3;i++) {
            float y=334+i*153;Panel(24,y+4,492,133,Ink);Panel(24,y,492,133,Cream);
            Panel(38,y+15,95,103,i==0?Coral:i==1?new Color(.18f,.59f,.52f):new Color(.50f,.39f,.66f));
            if(i==0){Box(112,y+36,6,56,Cream);CircleAt(new Vector2(71,y+66),15,Cream);Line(new Vector2(88,y+66),new Vector2(106,y+66),5,Cream);}
            else if(i==1){for(int n=0;n<3;n++){CircleAt(new Vector2(58+n*25,y+66),11,Cream);if(n<2)Line(new Vector2(70+n*25,y+66),new Vector2(73+n*25,y+66),4,Cream);}}
            else{CircleAt(new Vector2(84,y+68),22,Cream);Text("Z",58,y+43,52,45,26,Ink,TextAnchor.MiddleCenter);}
            Text(names[i],151,y+15,278,40,27,Ink);Text("LV."+(ranks[i]+1),418,y+22,81,25,15,Coral,TextAnchor.MiddleRight);Text(desc[i],152,y+65,342,60,17,Ink);
            if(GUI.Button(new Rect(24,y,492,133),GUIContent.none,GUIStyle.none)){combat.Choose(i);dragging=false;stick=Vector2.zero;}
        }
        Text("벽꿍  →  충돌 전파  →  연쇄 기절",25,837,490,40,20,Mint,TextAnchor.MiddleCenter);
        Text("싸움이 잠시 멈췄습니다",25,914,490,25,14,Muted,TextAnchor.MiddleCenter);
    }
    void Result(){
        bool win=combat.state==RunState.Won;Box(0,173,540,787,new Color(.055f,.07f,.10f,.94f));
        Text(win?"골목을 되찾았다":"아직, 끝난 게 아니다",25,226,490,65,34,Cream,TextAnchor.MiddleCenter);
        Text(win?"“그만해. 내 몸으로 뭐 하는 거야.”\n\n아이의 목소리가 들렸다.\n내 아들은 아직 여기에 있다.":"벽 가까이에서 적을 날려보자.\n포위되면 필살기로 길을 만들 수 있다.",40,330,460,180,23,Cream,TextAnchor.MiddleCenter);
        Panel(24,566,492,157,new Color(.16f,.21f,.26f));
        Text("제압  "+combat.kills+"     벽꿍  "+combat.wallHits,35,591,470,35,22,Mint,TextAnchor.MiddleCenter);
        Text("충돌  "+combat.chainHits+"     최고 연쇄  "+combat.bestChain,35,639,470,35,22,Cream,TextAnchor.MiddleCenter);
        if(Button(win?"골목 지도로  →":"다시 도전  →",24,813,492,70,Coral)){if(win)map=true;else{NewRun();combat.Start();}}
        if(Button("처음으로",24,903,492,40,Cream))NewRun();
    }
    void Map(){
        Box(0,173,540,787,Cream);Text("아들의 동네",28,207,484,60,34,Ink);Text("싸움 끝에서, 아들의 삶을 마주한다.",28,274,484,35,17,new Color(.46f,.41f,.35f));
        string[] names={"우리 학교","학교 뒷골목","편의점 앞","황제 당구장","지역 조직"};
        for(int i=0;i<5;i++){
            float y=712-i*86;float x= i%2==0?172:352;bool available=i<=stage+2;
            if(i<4){float nx=(i+1)%2==0?172:352;Line(new Vector2(x,y),new Vector2(nx,y-86),7,new Color(.75f,.70f,.59f));}
            CircleAt(new Vector2(x,y),27,available?(i<=stage+1?Mint:Coral):Muted);Text(i<=stage+1?"✓":(i+1).ToString(),x-27,y-27,54,54,24,Color.white,TextAnchor.MiddleCenter);
            Panel(x-87,y+34,174,33,Ink);Text(names[i],x-87,y+34,174,33,15,Cream,TextAnchor.MiddleCenter);
        }
        if(Button(stage<2?"다음 골목으로  →":"다시 도전  →",24,841,492,71,Coral)){stage=Mathf.Min(2,stage+1);NewRun();combat.Start();}
    }
    void OnGUI(){
        float scale=Scale;GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-540*scale)*.5f,(Screen.height-960*scale)*.5f,0),Quaternion.identity,new Vector3(scale,scale,1));
        Box(0,0,540,960,Ink);
        if(!map){World();Header();if(combat.state==RunState.Ready)Intro();else {HUD();if(combat.state==RunState.Choosing)Skills();else if(combat.state==RunState.Won||combat.state==RunState.Lost)Result();}}
        else{Header();Map();}
        GUI.color=Color.white;
    }
    void OnDestroy(){Destroy(disc);Destroy(round);Destroy(hitSound);Destroy(wallSound);Destroy(specialSound);}
}
