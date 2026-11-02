using UnityEngine;

namespace SecondFistGame {
    public sealed class StageInfo {
        public readonly string name,tagline;public readonly int target;public readonly float heavyChance;
        public StageInfo(string name,string tagline,int target,float heavyChance){this.name=name;this.tagline=tagline;this.target=target;this.heavyChance=heavyChance;}
    }
    public static class StageCatalog {
        public static readonly StageInfo[] All={
            new StageInfo("학교 뒷골목","복서의 주먹을 피하고 벽으로 날려라",12,.15f),
            new StageInfo("네온 야시장","긴 리치의 킥복서가 거리를 좁힌다",16,.22f),
            new StageInfo("노을 옥상","빠른 복서와 돌진을 동시에 상대하라",20,.30f),
            new StageInfo("항만 물류창고","중량급의 돌진을 피해 마지막 보스를 제압하라",24,.45f)
        };
        public static StageInfo Get(int index){return All[Mathf.Clamp(index,0,All.Length-1)];}
    }
}

public partial class AlleyGame {
    bool home=true,stageSelect;Texture2D stageBackgrounds,rosterAtlas;PivotSheet rosterPivots;
    void LoadStages(){stageBackgrounds=Resources.Load<Texture2D>("StageArenas");rosterAtlas=Resources.Load<Texture2D>("StreetRoster");var data=Resources.Load<TextAsset>("StreetRosterPivots");if(data!=null)rosterPivots=JsonUtility.FromJson<PivotSheet>(data.text);}
    void ReturnHome(){home=true;stageSelect=false;paused=false;dragging=false;stick=Vector2.zero;styleMenu=false;}
    void StartStage(int index){stage=Mathf.Clamp(index,0,SecondFistGame.StageCatalog.All.Length-1);home=false;stageSelect=false;NewRun();}
    void StageBackdrop(int index,Rect rect){
        if(stageBackgrounds==null){GUI.DrawTexture(rect,background,ScaleMode.ScaleAndCrop);return;}
        float cellHeight=index<2?478f/1024f:546f/1024f;
        float cellAspect=(stageBackgrounds.width*.5f)/(stageBackgrounds.height*cellHeight),targetAspect=rect.width/rect.height;
        float w=.5f,h=cellHeight;if(targetAspect>cellAspect)h*=cellAspect/targetAspect;else w*=targetAspect/cellAspect;
        float u=(index%2)*.5f+(.5f-w)*.5f,v=(index<2?546f/1024f:0)+(cellHeight-h)*.5f;
        GUI.DrawTextureWithTexCoords(rect,stageBackgrounds,new Rect(u,v,w,h));
    }
    void HomeScreen(){
        StageBackdrop(stage,new Rect(0,0,ViewWidth,ViewHeight));Box(0,0,ViewWidth,ViewHeight,new Color(.025f,.04f,.065f,.77f));Centered(DrawHomeContent);
    }
    void DrawHomeContent(){
        if(styleMenu){StylePanel();return;}
        if(stageSelect){DrawStageSelect();return;}
        Text("SECOND FIST",24,125,492,30,18,Gold,TextAnchor.MiddleCenter,true);
        Title("다시, 한 방",24,170,492,85,61,Cream,TextAnchor.MiddleCenter);
        Text("벽으로 날리고 · 연쇄로 끝내라",24,269,492,37,21,Cream,TextAnchor.MiddleCenter);
        MotionSprite(heroMotionAtlas,heroPivots,new Vector2(265,579),8,false,260,Color.white,-3,Vector2.one);
        Surface(24,603,492,82,Slate,Gold);Text("BEST  "+profile.bestScore.ToString("N0")+"     COIN  "+profile.coins,24,625,492,35,20,Gold,TextAnchor.MiddleCenter,true);
        if(Button("게임 시작",24,716,492,70,Coral))StartStage(Mathf.Min(profile.highestStage,3));
        if(Button("스테이지 선택",24,808,237,55,Cream))stageSelect=true;
        if(Button("시작 기술",279,808,237,55,Cream))styleMenu=true;
        Text("WASD / 드래그 이동 · 자동 공격 · SPACE 필살기",24,900,492,30,14,Cream,TextAnchor.MiddleCenter);
        Text("v0.7  ·  PC 아케이드",24,941,492,19,12,Muted,TextAnchor.MiddleCenter);
    }
    void DrawStageSelect(){
        Title("골목을 골라라",24,103,492,64,39,Cream,TextAnchor.MiddleCenter);
        for(int i=0;i<SecondFistGame.StageCatalog.All.Length;i++){
            var info=SecondFistGame.StageCatalog.Get(i);float y=199+i*149;bool unlocked=i<=profile.highestStage;
            Surface(24,y,492,133,Slate,unlocked?Gold:Muted);StageBackdrop(i,new Rect(29,y+5,141,123));
            Title((i+1)+". "+info.name,184,y+11,318,40,23,unlocked?Cream:Muted);
            Text(info.target+"명 제압 후 보스",184,y+57,300,25,14,Cream);
            GUI.enabled=unlocked;if(Button(unlocked?"도전":"이전 스테이지 클리어",184,y+88,316,33,unlocked?Coral:Muted))StartStage(i);GUI.enabled=true;
        }
        if(Button("시작 화면으로",24,850,492,59,Cream))stageSelect=false;
    }
}
