using System.Collections.Generic;
using UnityEngine;

public sealed partial class DadSlingshotPrototype
{
    public bool UserPaused {get;private set;}
    bool pauseOptions;
    float impactPause,impactCooldown,summaryTime;
    int observedShots,shotKills,shotWalls,summaryShot,shotHits;
    int lessonKind=-1;
    public bool TutorialOpen {get{return lessonKind>=0&&Sim!=null&&!Sim.Over;}}
    sealed class Burst {public Vector2 point;public float age;public bool wall;}
    readonly List<Burst> bursts=new List<Burst>();
    public void SetPaused(bool paused)
    {
        UserPaused=paused&&Sim!=null&&!Sim.Over;pauseOptions=false;
        aiming=false;finger=-1;queuedLaunch=false;
        if(pullLoop!=null)pullLoop.Stop();
    }
    void OnApplicationPause(bool paused){if(paused)SetPaused(true);}
    void ResetPresentation(){UserPaused=false;pauseOptions=false;impactPause=impactCooldown=summaryTime=0;observedShots=summaryShot=0;lessonKind=-1;bursts.Clear();}
    void UpdatePresentation(float dt)
    {
        impactCooldown=Mathf.Max(0,impactCooldown-dt);summaryTime=Mathf.Max(0,summaryTime-dt);
        for(int i=bursts.Count-1;i>=0;i--){bursts[i].age+=dt;if(bursts[i].age>.28f)bursts.RemoveAt(i);}
    }
    void ObserveShot()
    {
        if(Sim.shots==observedShots)return;
        observedShots=Sim.shots;shotKills=Sim.kills;shotWalls=Sim.wallSlams;shotHits=0;summaryTime=0;
    }
    void CompleteShot()
    {
        if(Sim.shots==0||Sim.dad.flying||summaryShot==Sim.shots)return;
        summaryShot=Sim.shots;summaryTime=2.4f;
    }
    void PresentImpact(SlingshotSim.Event e)
    {
        if(e.text=="퍽!"||e.text=="분노의 들이받기!"||e.text=="쟁반 박살!"||e.text=="명중!")shotHits++;
        string sound=e.sound??"";
        bool wall=sound.StartsWith("sfx_wall_")&&!sound.EndsWith("boing");
        bool hit=wall||sound.StartsWith("sfx_punch_")||sound=="sfx_tray_break"||e.text=="명중!";
        if(!hit)return;
        if(bursts.Count>=12)bursts.RemoveAt(0);
        bursts.Add(new Burst{point=e.pos-new Vector2(0,35*S),wall=wall});
        if(impactCooldown<=0)
        {
            impactCooldown=.12f;
            if(!DadPreferences.ReducedMotion)impactPause=wall?0:.025f;
            if(e.style>=2||(wall&&Sim.shotChain>=3))DadPreferences.Impact();
        }
    }
    void DrawImpactBursts()
    {
        foreach(var burst in bursts)
        {
            float t=burst.age/.28f,a=(1-t)*.85f,r=(12+35*t)*S;
            Color color=burst.wall?new Color(1,.68f,.12f,a):new Color(1,.97f,.83f,a);
            for(int i=0;i<8;i++)
            {
                float angle=(i*45+17)*Mathf.Deg2Rad;Vector2 dir=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                Line(burst.point+dir*r*.58f,burst.point+dir*r,Mathf.Max(1,3*(1-t))*S,color);
            }
        }
    }
    void DrawShotFeedback()
    {
        if(Sim.Over||UserPaused)return;
        float bottom=Height+Pad;
        if(summaryTime>0)
        {
            Rect r=new Rect(14,bottom-102,340,48);DadUi.Fill(r,new Color(.12f,.20f,.16f,.94f),12);
            string text=summaryShot+"번째 발사  ·  제압 "+Mathf.Max(0,Sim.kills-shotKills)+"명  ·  벽꿍 "+Mathf.Max(0,Sim.wallSlams-shotWalls);
            if(Sim.kills==shotKills&&Sim.wallSlams==shotWalls)text=shotHits>0?"명중 "+shotHits+"회  ·  다음 발로 마무리!":"빗나갔어요  ·  옆벽으로 경로를 바꿔보세요";
            Text(r,text,17,Color.white,false,TextAnchor.MiddleCenter);
        }
        Rect goal=new Rect(14,bottom-48,325,32);DadUi.Fill(goal,new Color(.97f,.94f,.84f,.94f),9);
        Text(goal,"별 목표  "+Sim.shots+" / "+Sim.stage.par+"발  ·  체력 50% 이상",15,Sim.shots>Sim.stage.par?Red:Ink,false,TextAnchor.MiddleCenter);
    }
    void PrepareOpponentLesson()
    {
        if(!Embedded)return;int seen=PlayerPrefs.GetInt("DadWar.SeenRoles",0);
        foreach(var spawn in Sim.stage.spawns)
        {
            int kind=(int)spawn.kind;
            if(kind==1||kind==2||kind==4)if((seen&(1<<kind))==0){lessonKind=kind;break;}
        }
    }
    void DrawOpponentLesson()
    {
        Box(new Rect(0,-Pad,Width,Height+2*Pad),new Color(.04f,.08f,.06f,.86f));
        Panel(new Rect(28,160,484,650),Paper);
        string name=lessonKind==1?"의자를 든 아빠":lessonKind==2?"쟁반을 든 아빠":"피구공을 든 아빠";
        string rule=lessonKind==1?"빨간 길을 따라 돌진해요":lessonKind==2?"앞에서 맞히면 쟁반으로 막아요":"던진 공은 아빠로 받아칠 수 있어요";
        string counter=lessonKind==1?"돌진 예고가 보이면 먼저 날아가세요":lessonKind==2?"벽으로 튕겨 옆이나 뒤를 노리세요":"공을 향해 날아가 되돌려 주세요";
        Text(new Rect(52,184,436,32),"새로운 아빠 등장",19,Green);
        Text(new Rect(52,222,436,58),name,34,Ink,true);
        DrawCharacter(FoeId((SlingshotSim.Kind)lessonKind),"walk",0,new Vector2(270,534),206,false);
        Text(new Rect(52,550,436,44),rule,21,Ink,false,TextAnchor.MiddleCenter);
        Text(new Rect(52,600,436,54),counter,21,Green,false,TextAnchor.MiddleCenter);
        if(Button(new Rect(54,690,432,74),"알겠어, 가자!",Yellow,true,27))
        AcknowledgeLesson();
    }
    public void AcknowledgeLesson()
    {if(lessonKind<0)return;int seen=PlayerPrefs.GetInt("DadWar.SeenRoles",0);PlayerPrefs.SetInt("DadWar.SeenRoles",seen|(1<<lessonKind));PlayerPrefs.Save();lessonKind=-1;}
    void DrawBattlePause()
    {
        Box(new Rect(0,-Pad,Width,Height+Pad*2),new Color(.04f,.08f,.06f,.88f));
        Panel(new Rect(28,196,484,586),Paper);
        Text(new Rect(52,218,436,54),pauseOptions?"편하게 싸우는 법":"아빠, 잠깐만!",36,Ink,true);
        Text(new Rect(52,276,436,32),"전투는 멈춰 있습니다",18,Green);
        if(pauseOptions)
        {
            bool[] values={DadPreferences.Music,DadPreferences.Effects,DadPreferences.Haptics,DadPreferences.ReducedMotion};
            string[] labels={"배경 음악","효과음","진동","화면 흔들림 줄이기"};
            for(int i=0;i<4;i++)
            {
                float y=332+i*74;Text(new Rect(54,y,286,52),labels[i],22,Ink);
                if(Button(new Rect(366,y,116,52),values[i]?"켜짐":"꺼짐",values[i]?Yellow:Color.white,true,20))
                {bool v=!values[i];if(i==0)DadPreferences.Music=v;else if(i==1)DadPreferences.Effects=v;else if(i==2)DadPreferences.Haptics=v;else DadPreferences.ReducedMotion=v;}
            }
            if(Button(new Rect(54,672,430,66),"돌아가기",Yellow))pauseOptions=false;
            return;
        }
        if(Button(new Rect(54,334,430,74),"계속 싸우기",Yellow,true,27))SetPaused(false);
        if(Button(new Rect(54,426,430,64),"소리 · 진동 · 화면 설정",Color.white,true,22))pauseOptions=true;
        if(Button(new Rect(54,508,430,64),"처음부터 다시",Color.white,true,22))
        {
            if(Embedded)
            {var host=GetComponentInParent<DadSchoolPrototype>();if(host!=null){int rank=Sim.stage.challengeLevel;if(rank>0)host.StartRematch(host.PlayingStage,rank);else host.StartRun(host.PlayingStage);}}
            else StartStage(StageIndex);
        }
        if(Button(new Rect(54,590,430,64),"집으로 돌아가기",Color.white,true,22)){SetPaused(false);if(Embedded){if(Quit!=null)Quit();}else ShowMenu();}
        Text(new Rect(54,684,430,46),"집으로 가면 이번 전투의 진행은 끝납니다",17,Green,false,TextAnchor.MiddleCenter);
    }
}
