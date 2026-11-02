using System;
using UnityEngine;
using SecondFistGame;

[Serializable]
public sealed class ArcadeProfile {
    public int coins,bestScore,selectedStyle,unlocked=1,runs,highestStage;
    public string lastRun="";
    public const int StyleCost=120;
    public bool Owns(int style){return style>=0&&style<=3&&(unlocked&(1<<style))!=0;}
    public bool Equip(int style){if(!Owns(style))return false;selectedStyle=style;return true;}
    public bool Unlock(int style){if(style<1||style>3||Owns(style)||coins<StyleCost)return false;coins-=StyleCost;unlocked|=1<<style;return Equip(style);}
    public bool Settle(string run,int score,int reward){if(string.IsNullOrEmpty(run)||lastRun==run)return false;lastRun=run;runs++;coins+=Mathf.Max(0,reward);bestScore=Mathf.Max(bestScore,score);return true;}
    public void Normalize(){highestStage=Mathf.Clamp(highestStage,0,3);coins=Mathf.Max(0,coins);bestScore=Mathf.Max(0,bestScore);unlocked=(unlocked&15)|1;if(!Owns(selectedStyle))selectedStyle=0;}
}

public partial class AlleyGame {
    const string ProfileKey="SecondFist.Arcade.v6";
    ArcadeProfile profile;string runId;bool settled,styleMenu,newRecord;int runScore,runCoins,runStars;
    void LoadArcade(){try{profile=JsonUtility.FromJson<ArcadeProfile>(PlayerPrefs.GetString(ProfileKey,"{}"));}catch{profile=null;}if(profile==null)profile=new ArcadeProfile();profile.Normalize();}
    void SaveArcade(){PlayerPrefs.SetString(ProfileKey,JsonUtility.ToJson(profile));PlayerPrefs.Save();}
    void PrepareArcade(){runId=Guid.NewGuid().ToString("N");settled=styleMenu=false;runScore=runCoins=runStars=0;if(profile.selectedStyle==1)combat.wallSkill=1;if(profile.selectedStyle==2)combat.chainSkill=1;if(profile.selectedStyle==3)combat.stunSkill=1;}
    public static int ArcadeScore(AlleyCombat c){return c.kills*100+c.wallHits*45+c.chainHits*65+c.bestChain*100+(c.state==RunState.Won?500+Mathf.Max(0,Mathf.CeilToInt(60-c.elapsed))*10:0);}
    public static int ArcadeReward(AlleyCombat c){return Mathf.Min(160,c.kills*3+c.wallHits*2+c.chainHits*3+(c.state==RunState.Won?35:0));}
    void SettleArcade(){if(settled||(combat.state!=RunState.Won&&combat.state!=RunState.Lost))return;settled=true;runScore=ArcadeScore(combat);runCoins=ArcadeReward(combat);runStars=(combat.state==RunState.Won?1:0)+(combat.wallHits>=3?1:0)+(combat.bestChain>=3?1:0);newRecord=runScore>profile.bestScore;if(combat.state==RunState.Won)profile.highestStage=Mathf.Max(profile.highestStage,Mathf.Min(3,stage+1));if(profile.Settle(runId,runScore,runCoins))SaveArcade();}
    void ArcadeResult(){
        SettleArcade();Box(-(ViewWidth-540)*.5f,-(ViewHeight-960)*.5f,ViewWidth,ViewHeight,new Color(.025f,.04f,.065f,.95f));
        if(styleMenu){StylePanel();return;}
        bool win=combat.state==RunState.Won;
        Text("ROUND "+(stage+1)+"  /  "+(win?"CLEAR":"RETRY"),24,172,492,30,15,Gold,TextAnchor.MiddleCenter,true);
        Title(win?"시원하게 끝냈다!":"한 판 더?",24,218,492,65,39,Cream,TextAnchor.MiddleCenter);
        Text(newRecord?"NEW BEST":"SCORE",24,314,492,28,15,newRecord?Gold:Muted,TextAnchor.MiddleCenter,true);
        Title(runScore.ToString("N0"),24,350,492,76,56,Cream,TextAnchor.MiddleCenter);
        Text("최고 기록  "+profile.bestScore.ToString("N0"),24,437,492,28,16,Muted,TextAnchor.MiddleCenter);
        string[] goals={"라운드 클리어","벽꿍 3회","3연쇄 달성"};bool[] done={win,combat.wallHits>=3,combat.bestChain>=3};
        for(int i=0;i<3;i++){float x=24+i*166;Surface(x,495,160,78,Slate,done[i]?Gold:Muted);Text(done[i]?"달성":"도전",x,503,160,28,15,done[i]?Gold:Muted,TextAnchor.MiddleCenter,true);Text(goals[i],x,538,160,26,13,Cream,TextAnchor.MiddleCenter);}
        Surface(24,606,492,116,Slate,Gold);Title("+ "+runCoins+" 코인",40,618,460,44,29,Gold,TextAnchor.MiddleCenter);
        Text("보유 "+profile.coins+"  ·  시작 기술 해금 "+ArcadeProfile.StyleCost+" 코인",40,671,460,30,16,Cream,TextAnchor.MiddleCenter);
        if(Button(win?(stage<3?"다음 스테이지":"최종 클리어 · 다시 도전"):"바로 재도전",24,766,492,68,Coral)){if(win)stage=Mathf.Min(3,stage+1);NewRun();}
        if(Button("시작 기술",24,853,237,54,Cream))styleMenu=true;
        if(Button("시작 화면",279,853,237,54,Cream))ReturnHome();
    }
    void StylePanel(){
        Title("첫 한 방을 바꿔라",24,190,492,60,36,Cream,TextAnchor.MiddleCenter);Text("보유 "+profile.coins+" 코인 · 한 가지 기술로 시작",24,263,492,32,16,Gold,TextAnchor.MiddleCenter);
        string[] names={"기본","벽꿍 +1","충돌 전파 +1","연쇄 기절 +1"};
        for(int i=0;i<4;i++){float y=327+i*114;Surface(24,y,492,100,Slate,profile.selectedStyle==i?Gold:Muted);if(i>0)AbilityIcon(new Rect(34,y+7,86,86),i-1);Text(names[i],i>0?130:44,y+15,220,35,23,Cream,TextAnchor.MiddleLeft,true);Text(profile.selectedStyle==i?"다음 판에 적용":"시작 기술 LV.1",i>0?130:44,y+57,220,25,13,Muted);
            bool own=profile.Owns(i);bool affordable=own||profile.coins>=ArcadeProfile.StyleCost;GUI.enabled=affordable&&profile.selectedStyle!=i;
            if(Button(profile.selectedStyle==i?"장착 중":own?"장착":ArcadeProfile.StyleCost+" 코인",367,y+23,132,53,affordable?Gold:Muted)){if(own)profile.Equip(i);else profile.Unlock(i);SaveArcade();}GUI.enabled=true;}
        if(Button(home?"시작 화면으로":"결과로 돌아가기",24,829,492,64,Cream))styleMenu=false;
    }
}
