using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public partial class AlleyGame {
    readonly Dictionary<int,GUIStyle> textStyles=new Dictionary<int,GUIStyle>();
    static readonly Color Gold=new Color(1,.76f,.30f),Paper=new Color(.97f,.94f,.86f),Slate=new Color(.16f,.21f,.27f);
    bool paused;
    GUIStyle LabelStyle(int size,TextAnchor anchor,bool bold,bool display=false){
        int key=size*100+(int)anchor*4+(display?2:bold?1:0);GUIStyle style;
        if(!textStyles.TryGetValue(key,out style)){
            style=new GUIStyle{font=display?fontDisplay:bold?fontBold:font,fontSize=size,fontStyle=FontStyle.Normal,alignment=anchor,wordWrap=true,clipping=TextClipping.Clip};textStyles.Add(key,style);
        }
        return style;
    }
    void Text(string value,float x,float y,float w,float h,int size,Color color,TextAnchor anchor=TextAnchor.UpperLeft,bool bold=false){
        GUIStyle style=LabelStyle(size,anchor,bold);style.normal.textColor=color;GUI.Label(new Rect(x,y,w,h),value,style);
    }
    void Title(string value,float x,float y,float w,float h,int size,Color color,TextAnchor anchor=TextAnchor.UpperLeft){
        GUIStyle style=LabelStyle(size,anchor,false,true);style.normal.textColor=color;GUI.Label(new Rect(x,y,w,h),value,style);
    }
    void Outlined(string value,float x,float y,float w,float h,int size,Color color,TextAnchor anchor,bool display=false){
        Color shadow=new Color(Ink.r,Ink.g,Ink.b,color.a*.9f);
        GUIStyle style=LabelStyle(size,anchor,!display,display);style.normal.textColor=shadow;
        GUI.Label(new Rect(x-1.4f,y,w,h),value,style);GUI.Label(new Rect(x+1.4f,y+1,w,h),value,style);GUI.Label(new Rect(x,y+2,w,h),value,style);
        style.normal.textColor=color;GUI.Label(new Rect(x,y,w,h),value,style);
    }
    void Surface(float x,float y,float w,float h,Color fill,Color edge,bool elevated=true){
        if(elevated)Panel(x,y+5,w,h,new Color(.025f,.04f,.06f,.32f));
        Panel(x,y,w,h,edge);Panel(x+1.5f,y+1.5f,w-3,h-3,fill);
        Box(x+10,y+2,w-20,1,new Color(1,1,1,.15f));
    }
    bool Button(string value,float x,float y,float w,float h,Color color){
        Rect rect=new Rect(x,y,w,h);bool hover=rect.Contains(Pointer(Input.mousePosition));bool down=hover&&Input.GetMouseButton(0);
        bool clicked=GUI.Button(rect,GUIContent.none,GUIStyle.none);float offset=down?3:0;
        Panel(x,y+5,w,h,new Color(.025f,.04f,.06f,.5f));
        Color fill=hover?Color.Lerp(color,Color.white,.09f):color;
        Surface(x,y+offset,w,h,fill,color==Cream?new Color(.78f,.74f,.66f):Color.Lerp(color,Color.white,.24f),false);
        Box(x+14,y+offset+3,w-28,2,new Color(1,1,1,.17f));
        Title(value,x,y+offset+2,w,h,22,color==Cream?Ink:Cream,TextAnchor.MiddleCenter);
        if(clicked){choiceFlash=.045f;source.PlayOneShot(hitSound,.18f);}return clicked;
    }
    void AbilityIcon(Rect rect,int index,float alpha=1){
        if(abilityAtlas==null){CircleAt(rect.center,rect.width*.3f,Gold);return;}
        GUI.color=new Color(1,1,1,alpha);GUI.DrawTextureWithTexCoords(rect,abilityAtlas,new Rect(index%2*.5f,index<2?.5f:0,.5f,.5f));GUI.color=Color.white;
    }
    void Portrait(float x,float y,float size){
        Surface(x,y,size,size,Slate,Gold,false);
        if(atlas!=null){GUI.BeginGroup(new Rect(x+2,y+2,size-4,size-4));GUI.DrawTextureWithTexCoords(new Rect(0,0,size-4,size-4),atlas,new Rect(65/1536f,1-249/1024f,235/1536f,220/1024f));GUI.EndGroup();}
        Box(x+size-13,y+size-13,10,10,Mint);
    }
    void Meter(float x,float y,float w,float h,float fill,Color color,float trail=-1){
        Panel(x-1.5f,y-1.5f,w+3,h+3,new Color(.025f,.04f,.065f));Panel(x,y,w,h,new Color(.27f,.29f,.30f));
        if(trail>0)Panel(x,y,w*Mathf.Clamp01(trail),h,Gold);
        if(fill>0){Panel(x,y,w*Mathf.Clamp01(fill),h,color);Box(x+4,y+2,Mathf.Max(0,w*Mathf.Clamp01(fill)-8),2,new Color(1,1,1,.21f));}
        for(int i=1;i<5;i++)Box(x+w*i/5f,y,1,h,new Color(.04f,.06f,.08f,.22f));
    }
    void Header(){
        Outlined("정민수",23,16,235,37,24,Cream,TextAnchor.MiddleLeft,true);
        Meter(24,58,182,9,shownHealth/100,Coral,healthTrail/100);
        Meter(24,73,182,3,shownXp,Mint);
        Outlined("LV. "+combat.level+"  ·  "+StageCatalog.Get(stage).name,24,86,350,23,13,Cream,TextAnchor.MiddleLeft);
        float cx=ViewWidth*.5f;int seconds=Mathf.FloorToInt(combat.elapsed);
        Outlined((seconds/60).ToString("00")+":"+(seconds%60).ToString("00"),cx-66,19,132,30,17,Cream,TextAnchor.MiddleCenter);
        CircleAt(new Vector2(ViewWidth-34,34),19,new Color(.04f,.06f,.08f,.37f));
        Box(ViewWidth-41,26,4,16,Cream);Box(ViewWidth-31,26,4,16,Cream);
        if(GUI.Button(new Rect(ViewWidth-59,9,50,50),GUIContent.none,GUIStyle.none)){paused=!paused;dragging=false;stick=Vector2.zero;}
    }

    void HUD(){
        Outlined(combat.bossSpawned?"골목 대장":("일진 "+Mathf.Min(combat.targetKills,combat.kills)+" / "+combat.targetKills),ViewWidth-221,76,195,29,16,combat.bossSpawned?Gold:Cream,TextAnchor.MiddleRight,true);
        if(combat.chain>0&&combat.comboClock>0){float a=Mathf.Clamp01(combat.comboClock*1.8f);Outlined(combat.chain+" 연쇄!",ViewWidth*.5f-140,113,280,52,34,new Color(1,.83f,.36f,a),TextAnchor.MiddleCenter,true);}
        if(bossBanner>0){float w=Mathf.Min(580,ViewWidth-40);float a=Mathf.Clamp01(bossBanner*2);Outlined("골목 대장이 나타났다",(ViewWidth-w)*.5f,ViewHeight*.28f,w,62,30,new Color(1,.83f,.5f,a),TextAnchor.MiddleCenter,true);}
        if(dragging){
            Vector2 basePos=new Vector2(Mathf.Clamp(stickOrigin.x,55,ViewWidth-55),Mathf.Clamp(stickOrigin.y,116,ViewHeight-55));
            CircleAt(basePos,48,new Color(.04f,.065f,.10f,.18f));Ring(basePos,47,1.5f,new Color(.85f,.9f,.83f,.44f));
            CircleAt(basePos+stick*27,23,new Color(.85f,.9f,.83f,.55f));
        }
        Vector2 special=SpecialPosition;bool full=combat.charge>=100;
        if(full)CircleAt(special,53+Mathf.Sin(pulse*4)*2,new Color(1,.53f,.16f,.13f));
        CircleAt(special,47,new Color(.04f,.055f,.07f,.40f));
        for(int i=0;i<48;i++){float a=-Mathf.PI*.5f+i*Mathf.PI/24;float b=a+Mathf.PI/25;bool lit=i/48f<shownCharge/100;Line(special+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*48,special+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*48,3,lit?(full?Gold:Mint):new Color(.7f,.73f,.7f,.28f));}
        AbilityIcon(new Rect(special.x-38,special.y-38,76,76),3,full?1:.5f);
        Outlined(full?"SPACE":"충전 "+Mathf.RoundToInt(shownCharge)+"%",special.x-64,special.y+53,128,22,12,Cream,TextAnchor.MiddleCenter);
        if(combat.elapsed<5){float a=Mathf.Clamp01(5-combat.elapsed);float w=Mathf.Min(ViewWidth-160,600);Outlined("WASD / 드래그 이동  ·  SPACE 필살기",(ViewWidth-w)*.5f,ViewHeight-61,w,36,16,new Color(1,.97f,.87f,a),TextAnchor.MiddleCenter);}
    }

    void Intro(){DrawPrologue();}

    void Skills(){
        Box(-(ViewWidth-540)*.5f,-(ViewHeight-960)*.5f,ViewWidth,ViewHeight,new Color(.025f,.04f,.065f,.89f));
        Matrix4x4 old=GUI.matrix;float lift=(1-EaseOut(modalAge/.28f))*38;GUI.matrix=old*Matrix4x4.Translate(new Vector3(0,lift,0));
        Text("LEVEL UP  /  "+combat.level,24,205,492,28,14,Gold,TextAnchor.MiddleCenter,true);
        Title("한 방 강화",24,248,492,60,39,Cream,TextAnchor.MiddleCenter);
        Text("이번 판에 사용할 기술을 선택하세요",24,311,492,31,17,new Color(.72f,.79f,.80f),TextAnchor.MiddleCenter);
        string[] names={"벽꿍","충돌 전파","연쇄 기절"};string[] tags={"강한 한 방","연쇄 공격","군중 제어"};
        string[] descriptions={"벽 충돌 피해가 1 증가합니다.","적끼리 충돌할 때 피해가 1 증가합니다.","충돌한 적의 기절 시간이 늘어납니다."};int[] ranks={combat.wallSkill,combat.chainSkill,combat.stunSkill};
        for(int i=0;i<3;i++){
            float y=365+i*142;float delay=i*.045f;float slide=(1-EaseOut(Mathf.Max(0,modalAge-delay)/.26f))*70;
            Rect rect=new Rect(22+slide,y,496,126);bool hover=rect.Contains(Pointer(Input.mousePosition));
            Color accent=i==0?Coral:i==1?new Color(.10f,.60f,.48f):new Color(.53f,.37f,.68f);
            Surface(rect.x,rect.y,rect.width,rect.height,hover?Color.white:Paper,hover?Gold:new Color(.74f,.70f,.60f));
            Box(rect.x+1.5f,rect.y+12,4,rect.height-24,accent);
            Panel(rect.x+12,rect.y+14,94,96,Color.Lerp(accent,Paper,.85f));AbilityIcon(new Rect(rect.x+10,rect.y+8,101,101),i);
            Text(tags[i],rect.x+124,rect.y+12,210,23,12,accent,TextAnchor.MiddleLeft,true);
            Title(names[i],rect.x+122,rect.y+37,295,38,26,Ink);
            Text(descriptions[i],rect.x+124,rect.y+84,350,29,15,new Color(.33f,.35f,.33f),TextAnchor.UpperLeft,false);
            Surface(rect.x+398,rect.y+17,78,27,Ink,Ink,false);Text("LV. "+(ranks[i]+1),rect.x+398,rect.y+17,78,27,12,Gold,TextAnchor.MiddleCenter,true);
            if(hover){Line(new Vector2(rect.x+458,rect.y+60),new Vector2(rect.x+465,rect.y+67),3,accent);Line(new Vector2(rect.x+465,rect.y+67),new Vector2(rect.x+458,rect.y+74),3,accent);}
            if(modalAge>.15f&&GUI.Button(rect,GUIContent.none,GUIStyle.none)){combat.Choose(i);dragging=false;stick=Vector2.zero;source.PlayOneShot(specialSound,.35f);}
        }
        Text("벽꿍  +  충돌 전파  +  연쇄 기절",24,832,492,32,18,Gold,TextAnchor.MiddleCenter,true);
        Text("기술을 조합해 나만의 싸움법을 완성하세요",24,876,492,30,15,new Color(.64f,.73f,.76f),TextAnchor.MiddleCenter);
        Text("선택하는 동안 전투가 멈춥니다",24,926,492,22,12,new Color(.44f,.55f,.60f),TextAnchor.MiddleCenter);
        GUI.matrix=old;
    }
    void PausePanel(){
        Box(-(ViewWidth-540)*.5f,-(ViewHeight-960)*.5f,ViewWidth,ViewHeight,new Color(.025f,.04f,.065f,.92f));Text("TAKE A BREATH",25,292,490,28,13,Gold,TextAnchor.MiddleCenter,true);
        Title("잠깐, 숨 고르기",25,348,490,62,37,Cream,TextAnchor.MiddleCenter);
        Text("다음 한 방을 준비하자.",25,434,490,35,20,new Color(.72f,.79f,.80f),TextAnchor.MiddleCenter);
        if(Button("계속 싸운다",36,550,468,70,Coral))paused=false;
        if(Button(source.mute?"효과음 꺼짐":"효과음 켜짐",36,647,468,53,Cream))source.mute=!source.mute;
        if(Button("시작 화면으로",36,727,468,53,Cream))ReturnHome();
    }
}
