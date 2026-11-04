using UnityEngine;
using SecondFistGame;

namespace SecondFistGame {
    public sealed class StageInfo {
        public readonly string name,tagline;public readonly int target;public readonly float heavyChance;
        public StageInfo(string name,string tagline,int target,float heavyChance){this.name=name;this.tagline=tagline;this.target=target;this.heavyChance=heavyChance;}
    }
    public static class StageCatalog {
        public static readonly StageInfo[] All={
            new StageInfo("학교 뒷골목","일진들을 날려버리고 체육관 앞 골목을 되찾아라!",12,.15f),
            new StageInfo("네온 야시장","상가를 점거한 킥복서 패거리를 소탕하라!",16,.22f),
            new StageInfo("노을 옥상","옥상 지부를 격파하고 스파링 챔피언을 영입하라!",20,.30f),
            new StageInfo("항만 물류창고","팔룡회 행동대장을 꺾고 모든 구역을 평정하라!",24,.45f)
        };
        public static StageInfo Get(int index){return All[Mathf.Clamp(index,0,All.Length-1)];}
    }
}

public partial class AlleyGame {
    bool home=true,stageSelect,inGym;
    Texture2D stageBackgrounds,rosterAtlas,gymBackdrop,gymEquipmentAtlas;
    PivotSheet rosterPivots;
    public GymWorld gym=new GymWorld();

    void LoadStages(){
        stageBackgrounds=Resources.Load<Texture2D>("StageArenas");
        rosterAtlas=Resources.Load<Texture2D>("StreetRoster");
        gymBackdrop=Resources.Load<Texture2D>("GymBackdrop");
        gymEquipmentAtlas=Resources.Load<Texture2D>("GymEquipment");
        var data=Resources.Load<TextAsset>("StreetRosterPivots");
        if(data!=null)rosterPivots=JsonUtility.FromJson<PivotSheet>(data.text);
        gym.SyncFromProfile(profile);
    }
    void ReturnHome(){home=true;inGym=false;stageSelect=false;paused=false;dragging=false;stick=Vector2.zero;styleMenu=false;}
    public void EnterGymHub(){
        home=false;inGym=true;stageSelect=false;paused=false;styleMenu=false;dragging=false;stick=Vector2.zero;
        stage=Mathf.Clamp(stage,0,StageCatalog.All.Length-1);
        combat=new AlleyCombat(stage);combat.spawning=false;combat.Start();
        PrepareArcade();ResetMotion();UpdateArenaViewport(true);
        combat.player=new Vector2(combat.arena.center.x,combat.arena.center.y+35);
        heroRendered=heroLast=combat.player;
        gym.SyncFromProfile(profile);
        pops.Clear();
    }
    void StartStage(int index){
        stage=Mathf.Clamp(index,0,StageCatalog.All.Length-1);
        home=false;inGym=false;stageSelect=false;NewRun();
    }
    void StartPrologueMode(){
        stage=Mathf.Min(profile.highestStage,3);home=false;inGym=false;stageSelect=false;
        combat=new AlleyCombat(stage);pops.Clear();map=false;ring=shake=0;dragging=false;paused=false;
        PrepareArcade();ResetMotion();UpdateArenaViewport(true);
        float cx=combat.arena.center.x,cy=combat.player.y;
        combat.AddEnemy(new Vector2(cx-94,cy-38));combat.AddEnemy(new Vector2(cx-158,cy-42));
        combat.AddEnemy(new Vector2(cx+151,cy-121));combat.AddEnemy(new Vector2(cx+137,cy+113),1);
        if(prologueAtlas==null)LoadPrologue();
        BeginPrologue();
    }
    void StageBackdrop(int index,Rect rect){
        if(index==0&&background!=null&&rect.height>400f){
            GUI.DrawTexture(rect,background,ScaleMode.ScaleAndCrop);
            return;
        }
        if(stageBackgrounds==null){
            if(background!=null)GUI.DrawTexture(rect,background,ScaleMode.ScaleAndCrop);
            return;
        }
        float cellHeight=index<2?478f/1024f:546f/1024f;
        float cellAspect=(stageBackgrounds.width*.5f)/(stageBackgrounds.height*cellHeight),targetAspect=rect.width/rect.height;
        float w=.5f,h=cellHeight;
        if(targetAspect>cellAspect)h*=cellAspect/targetAspect;
        else w=Mathf.Max(.36f,w*(targetAspect/cellAspect));
        float pan=.5f;
        if(combat!=null&&rect.height>400f){
            pan=Mathf.Clamp01((combat.player.x-combat.arena.xMin)/Mathf.Max(1f,combat.arena.width));
        }
        float u=(index%2)*.5f+(.5f-w)*pan,v=(index<2?546f/1024f:0)+(cellHeight-h)*.5f;
        GUI.DrawTextureWithTexCoords(rect,stageBackgrounds,new Rect(u,v,w,h));
    }
    void DrawEquipmentIcon(Rect rect,int index,Color tint){
        if(gymEquipmentAtlas==null){CircleAt(rect.center,rect.width*.35f,Gold);return;}
        int c=Mathf.Clamp(index,0,3);
        Rect uv=new Rect((c%2)*.5f,c<2?.5f:0,.5f,.5f);
        GUI.color=tint;GUI.DrawTextureWithTexCoords(rect,gymEquipmentAtlas,uv);GUI.color=Color.white;
    }
    void HomeScreen(){
        if(gymBackdrop!=null){GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,ViewWidth,ViewHeight),gymBackdrop,ScaleMode.ScaleAndCrop);}
        else StageBackdrop(stage,new Rect(0,0,ViewWidth,ViewHeight));
        Box(0,0,ViewWidth,ViewHeight,new Color(.03f,.045f,.08f,.78f));Centered(DrawHomeContent);
    }
    void DrawHomeContent(){
        if(styleMenu){StylePanel();return;}
        if(stageSelect){DrawStageSelect();return;}
        Surface(130,78,280,32,new Color(.14f,.19f,.28f),Gold,false);
        Text("★ 액션 타이쿤 & 스트리트 핀볼 격투 ★",130,80,280,28,13,Gold,TextAnchor.MiddleCenter,true);
        Outlined("팔룡체육관",24,118,492,82,62,Cream,TextAnchor.MiddleCenter,true);
        Outlined("전설의 보스 정민수 · 다시, 한 방",24,196,492,40,23,Gold,TextAnchor.MiddleCenter,true);
        Text("골목을 평정하고 · 도장을 증축하고 · 신화 권갑을 연성하라!",24,242,492,28,15,Cream,TextAnchor.MiddleCenter);
        // Hero & Gym Equipment showcase
        if(gymEquipmentAtlas!=null){
            DrawEquipmentIcon(new Rect(64,340,135,135),0,new Color(1,1,1,.92f));
            DrawEquipmentIcon(new Rect(342,340,135,135),3,new Color(1,1,1,.92f));
        }
        MotionSprite(heroMotionAtlas,heroPivots,new Vector2(268,518),8,false,238,Color.white,-3,Vector2.one);
        Surface(24,536,492,104,Slate,Gold);
        Title("보유 자금  $"+profile.coins+"   ·   도장 시설  "+gym.BuiltCount+"/5 (LV."+profile.TotalStationLevels+")",34,548,472,34,20,Gold,TextAnchor.MiddleCenter);
        Text("해방 구역 "+(profile.highestStage+1)+"/4   ·   장착 권갑 ["+profile.GearDisplayName+"]",34,592,472,28,14,Cream,TextAnchor.MiddleCenter);
        if(Button("🏋 팔룡체육관 입장 & 골목 출격",24,658,492,72,Mint)){stage=Mathf.Min(profile.highestStage,3);EnterGymHub();}
        if(Button("⚡ 즉시 골목 전투 (STAGE "+(Mathf.Min(profile.highestStage,3)+1)+")",24,742,237,54,Coral))StartStage(Mathf.Min(profile.highestStage,3));
        if(Button("구획 · 스테이지 선택",279,742,237,54,Gold))stageSelect=true;
        if(Button("권갑 연성 & 단련",24,808,237,48,Cream))styleMenu=true;
        if(Button("프롤로그 웹툰 보기",279,808,237,48,Cream))StartPrologueMode();
        Text("원핸드 드래그로 도장 증축 · 권갑 연성 · 골목 초토화까지 한 번에!",24,876,492,24,13,Cream,TextAnchor.MiddleCenter);
        Text("팔룡체육관 v1.0  ·  Hybrid-Casual Commercial Edition",24,908,492,19,12,Muted,TextAnchor.MiddleCenter);
    }
    void DrawStageSelect(){
        Title("탈환할 골목 구역 선택",24,96,492,58,36,Cream,TextAnchor.MiddleCenter);
        for(int i=0;i<StageCatalog.All.Length;i++){
            var info=StageCatalog.Get(i);float y=178+i*152;bool unlocked=i<=profile.highestStage;
            Surface(24,y,492,138,Slate,unlocked?Gold:Muted);StageBackdrop(i,new Rect(30,y+7,142,124));
            Title("구역 "+(i+1)+". "+info.name,186,y+10,316,36,22,unlocked?Gold:Muted);
            Text(info.tagline,186,y+46,316,24,13,Cream);
            Text("목표: 일진 "+info.target+"명 제압 후 구역 대장 격파",186,y+70,316,22,12,Mint);
            GUI.enabled=unlocked;
            if(Button(unlocked?"이 구역으로 출격 ▶":"이전 구역 해방 필요",186,y+96,314,34,unlocked?Coral:Muted))StartStage(i);
            GUI.enabled=true;
        }
        if(Button("돌아가기",24,820,492,58,Cream))stageSelect=false;
    }
}
