using System;
using UnityEngine;
using SecondFistGame;

[Serializable]
public sealed class ArcadeProfile {
    public int coins,bestScore,selectedStyle,unlocked=1,runs,highestStage;
    public int fistLevel,bodyLevel,greedLevel;
    public int gymStationsMask,gymSafeCoins;
    public int totalKills,totalWallHits,questClaimedMask;
    public int[] stationInvested=new int[5];
    public string lastRun="";
    public const int StyleCost=120;
    public bool Owns(int style){return style>=0&&style<=3&&(unlocked&(1<<style))!=0;}
    public bool Equip(int style){if(!Owns(style))return false;selectedStyle=style;return true;}
    public bool Unlock(int style){if(style<1||style>3||Owns(style)||coins<StyleCost)return false;coins-=StyleCost;unlocked|=1<<style;return Equip(style);}
    public bool IsStationUnlocked(int id){return id>=0&&id<5&&(gymStationsMask&(1<<id))!=0;}
    public int GetStationInvested(int id){if(stationInvested==null||id<0||id>=stationInvested.Length)return 0;return stationInvested[id];}
    public void SetStationState(int id,bool isUnlocked,int invested){
        if(stationInvested==null||stationInvested.Length<5)Array.Resize(ref stationInvested,5);
        if(id<0||id>=5)return;
        if(isUnlocked)gymStationsMask|=1<<id;
        stationInvested[id]=Mathf.Max(0,invested);
    }
    public int BuiltStationCount {
        get { int c=0;for(int i=0;i<5;i++)if(IsStationUnlocked(i))c++;return c; }
    }
    public string FameTitle {
        get {
            int score=BuiltStationCount*2+highestStage*2+(fistLevel+bodyLevel+greedLevel)/3;
            if(score>=12)return "👑 전국구 전설 챔피언";
            if(score>=8)return "🔥 팔룡회 지배자";
            if(score>=5)return "⚡ 거리 챔피언";
            if(score>=2)return "🥊 골목 관장";
            return "🌱 신입 관장";
        }
    }
    public int UpgradeCost(int stat){
        int lv=stat==0?fistLevel:stat==1?bodyLevel:greedLevel;
        return 45+lv*35;
    }
    public bool UpgradeStat(int stat){
        int cost=UpgradeCost(stat);
        if(coins<cost)return false;
        if(stat==0&&fistLevel<10){coins-=cost;fistLevel++;return true;}
        if(stat==1&&bodyLevel<10){coins-=cost;bodyLevel++;return true;}
        if(stat==2&&greedLevel<10){coins-=cost;greedLevel++;return true;}
        return false;
    }
    public bool Settle(string run,int score,int reward){if(string.IsNullOrEmpty(run)||lastRun==run)return false;lastRun=run;runs++;coins+=Mathf.Max(0,reward);bestScore=Mathf.Max(bestScore,score);return true;}
    public void Normalize(){
        highestStage=Mathf.Clamp(highestStage,0,3);coins=Mathf.Max(0,coins);bestScore=Mathf.Max(0,bestScore);
        fistLevel=Mathf.Clamp(fistLevel,0,10);bodyLevel=Mathf.Clamp(bodyLevel,0,10);greedLevel=Mathf.Clamp(greedLevel,0,10);
        gymSafeCoins=Mathf.Clamp(gymSafeCoins,0,500);
        totalKills=Mathf.Max(0,totalKills);totalWallHits=Mathf.Max(0,totalWallHits);
        if(stationInvested==null||stationInvested.Length<5)Array.Resize(ref stationInvested,5);
        unlocked=(unlocked&15)|1;if(!Owns(selectedStyle))selectedStyle=0;
    }
}

public partial class AlleyGame {
    const string ProfileKey="SecondFist.Arcade.v6";
    ArcadeProfile profile;string runId;bool settled,styleMenu,newRecord,rouletteUsed;int runScore,runCoins,runStars,rouletteMul=1;
    void LoadArcade(){
        try{profile=JsonUtility.FromJson<ArcadeProfile>(PlayerPrefs.GetString(ProfileKey,"{}"));}catch{profile=null;}
        if(profile==null)profile=new ArcadeProfile();
        profile.Normalize();
        if(profile.runs==0&&profile.coins==0&&profile.gymStationsMask==0){
            profile.coins=35;
        }
    }
    void SaveArcade(){PlayerPrefs.SetString(ProfileKey,JsonUtility.ToJson(profile));PlayerPrefs.Save();}
    void PrepareArcade(){
        runId=Guid.NewGuid().ToString("N");settled=styleMenu=rouletteUsed=false;runScore=runCoins=runStars=0;rouletteMul=1;
        if(profile.selectedStyle==1)combat.wallSkill=1;
        if(profile.selectedStyle==2)combat.chainSkill=1;
        if(profile.selectedStyle==3)combat.stunSkill=1;
        combat.baseDamage=1+profile.fistLevel/2+(profile.IsStationUnlocked(0)?1:0);
        combat.fistSkill+=profile.fistLevel%2+(profile.IsStationUnlocked(1)?1:0);
        combat.hasPartner=profile.IsStationUnlocked(2);
        combat.maxHealth=100+profile.bodyLevel*20+(profile.IsStationUnlocked(3)?40:0);
        if(profile.IsStationUnlocked(3))combat.wallSkill+=1;
        if(profile.IsStationUnlocked(4)){combat.vampSkill+=1;combat.magnetSkill+=1;}
        combat.health=combat.maxHealth;
        combat.magnetSkill+=profile.greedLevel/2;
    }
    public static int ArcadeScore(AlleyCombat c){return c.kills*100+c.wallHits*45+c.chainHits*65+c.bestChain*100+(c.state==RunState.Won?500+Mathf.Max(0,Mathf.CeilToInt(60-c.elapsed))*10:0);}
    public static int ArcadeReward(AlleyCombat c){return Mathf.Min(160,c.kills*3+c.wallHits*2+c.chainHits*3+(c.state==RunState.Won?35:0));}
    void SettleArcade(){
        if(settled||(combat.state!=RunState.Won&&combat.state!=RunState.Lost))return;
        if(combat.state==RunState.Lost&&combat.revivesUsed==0)return;
        settled=true;runScore=ArcadeScore(combat);
        int bonusCoins=Mathf.RoundToInt((ArcadeReward(combat)+combat.collectedCoins)*(1f+profile.greedLevel*.12f));
        runCoins=bonusCoins;
        runStars=(combat.state==RunState.Won?1:0)+(combat.wallHits>=3?1:0)+(combat.bestChain>=3?1:0);
        newRecord=runScore>profile.bestScore;
        profile.totalKills+=combat.kills;
        profile.totalWallHits+=combat.wallHits;
        if(combat.state==RunState.Won)profile.highestStage=Mathf.Max(profile.highestStage,Mathf.Min(3,stage+1));
        if(profile.Settle(runId,runScore,runCoins))SaveArcade();
    }
    public bool CheckQuestProgress(out string questText,out int reward,out bool claimable,out int questIndex){
        string[] titles={
            "첫 체육관 시설 [헤비 샌드백] 건설하기",
            "골목 일진 누적 10명 제압하기 ("+Mathf.Min(10,profile.totalKills)+"/10)",
            "[챔피언 스파링 링] 짓고 동료 파이터 영입하기",
            "벽꿍 누적 10회 달성하기 ("+Mathf.Min(10,profile.totalWallHits)+"/10)",
            "체육관 5대 모든 시설 완성하기 ("+profile.BuiltStationCount+"/5)"
        };
        int[] rewards={40,55,80,100,180};
        bool[] done={
            profile.IsStationUnlocked(0),
            profile.totalKills>=10,
            profile.IsStationUnlocked(2),
            profile.totalWallHits>=10,
            profile.BuiltStationCount>=5
        };
        for(int i=0;i<titles.Length;i++){
            if((profile.questClaimedMask&(1<<i))==0){
                questText=titles[i];reward=rewards[i];claimable=done[i];questIndex=i;return true;
            }
        }
        questText="모든 체육관 마스터 업적 달성 완료!";reward=0;claimable=false;questIndex=-1;return false;
    }
    public void ClaimQuest(int index,int reward){
        if(index<0||index>=10||(profile.questClaimedMask&(1<<index))!=0)return;
        profile.questClaimedMask|=1<<index;
        profile.coins+=reward;
        SaveArcade();
        choiceFlash=.25f;shake=3.5f;
        source.PlayOneShot(specialSound,.85f);
    }
    int CurrentSlotMultiplier(){
        float t=Mathf.PingPong(pulse*2.6f,1f);
        if(t>.38f&&t<.62f)return 5;
        if(t>.20f&&t<.80f)return 3;
        return 2;
    }
    void ArcadeResult(){
        if(combat.state==RunState.Lost&&combat.revivesUsed==0&&!settled){
            ReviveModal();
            return;
        }
        SettleArcade();Box(-(ViewWidth-540)*.5f,-(ViewHeight-960)*.5f,ViewWidth,ViewHeight,new Color(.025f,.04f,.065f,.95f));
        if(styleMenu){StylePanel();return;}
        bool win=combat.state==RunState.Won;
        Text("TURF "+(stage+1)+"  /  "+(win?"TURF RECLAIMED!":"RETRY"),24,122,492,28,15,Gold,TextAnchor.MiddleCenter,true);
        Title(win?"골목 구역 평정 완료!":"정비하고 다시 쳐들어가자!",24,154,492,58,38,Cream,TextAnchor.MiddleCenter);
        Text(newRecord?"★ NEW BEST RECORD ★":"FINAL SCORE",24,222,492,24,14,newRecord?Gold:Muted,TextAnchor.MiddleCenter,true);
        Title(runScore.ToString("N0"),24,248,492,64,50,Cream,TextAnchor.MiddleCenter);
        string[] goals={"구역 보스 제압","벽꿍 3회","3연쇄 달성"};bool[] done={win,combat.wallHits>=3,combat.bestChain>=3};
        for(int i=0;i<3;i++){
            float x=24+i*166;Surface(x,326,160,68,Slate,done[i]?Gold:Muted);
            Text(done[i]?"★ 달성":"도전",x,333,160,24,14,done[i]?Gold:Muted,TextAnchor.MiddleCenter,true);
            Text(goals[i],x,362,160,24,13,Cream,TextAnchor.MiddleCenter);
        }
        Surface(24,410,492,216,Slate,Gold);
        Title("+ $"+(runCoins*rouletteMul)+" 정산금"+(rouletteMul>1?" (x"+rouletteMul+" 대박!)":""),40,422,460,42,29,Gold,TextAnchor.MiddleCenter);
        Text("보유 자금 $"+profile.coins+"   ·   골목 회수금 +$"+combat.collectedCoins,40,466,460,26,15,Cream,TextAnchor.MiddleCenter);
        float barX=52,barY=504,barW=436,barH=30;
        Panel(barX,barY,barW,barH,new Color(.08f,.11f,.15f));
        Box(barX+4,barY+4,barW*.20f,barH-8,new Color(.28f,.58f,.46f));
        Box(barX+barW*.20f,barY+4,barW*.18f,barH-8,new Color(.85f,.52f,.18f));
        Box(barX+barW*.38f,barY+4,barW*.24f,barH-8,new Color(1f,.28f,.18f));
        Box(barX+barW*.62f,barY+4,barW*.18f,barH-8,new Color(.85f,.52f,.18f));
        Box(barX+barW*.80f,barY+4,barW*.20f-4,barH-8,new Color(.28f,.58f,.46f));
        Text("x2",barX,barY,barW*.20f,barH,14,Cream,TextAnchor.MiddleCenter,true);
        Text("x3",barX+barW*.20f,barY,barW*.18f,barH,14,Cream,TextAnchor.MiddleCenter,true);
        Text("x5 JACKPOT",barX+barW*.38f,barY,barW*.24f,barH,14,Gold,TextAnchor.MiddleCenter,true);
        Text("x3",barX+barW*.62f,barY,barW*.18f,barH,14,Cream,TextAnchor.MiddleCenter,true);
        Text("x2",barX+barW*.80f,barY,barW*.20f,barH,14,Cream,TextAnchor.MiddleCenter,true);
        float needleT=rouletteUsed?.5f:Mathf.PingPong(pulse*2.6f,1f);
        Box(barX+needleT*(barW-8),barY-5,8,barH+10,Cream);
        int liveMul=CurrentSlotMultiplier();
        GUI.enabled=!rouletteUsed&&runCoins>0;
        if(Button(rouletteUsed?"보상 "+rouletteMul+"배 수령 완료!":"▶ 룰렛 스톱! 정산금 x"+liveMul+"배 받기",52,550,436,58,rouletteUsed?Muted:Gold)){
            rouletteUsed=true;rouletteMul=liveMul;
            int extra=runCoins*(rouletteMul-1);
            profile.coins+=extra;SaveArcade();
            source.PlayOneShot(specialSound,.75f);
        }
        GUI.enabled=true;
        if(Button("🏋 체육관으로 복귀 & 시설 확장 ($"+profile.coins+")",24,646,492,68,Mint)){
            if(win)stage=Mathf.Min(3,stage+1);
            EnterGymHub();
        }
        if(Button(win?(stage<3?"다음 골목 즉시 진격 ▶":"최종 구역 재도전 ▶"):"이 골목 즉시 재도전 ▶",24,728,237,54,Coral)){if(win)stage=Mathf.Min(3,stage+1);NewRun();}
        if(Button("영구 능력치 강화",279,728,237,54,Cream))styleMenu=true;
        if(Button("메인 타이틀",24,796,492,48,Cream))ReturnHome();
    }
    void ReviveModal(){
        Box(-(ViewWidth-540)*.5f,-(ViewHeight-960)*.5f,ViewWidth,ViewHeight,new Color(.02f,.025f,.04f,.94f));
        Text("SECOND CHANCE  ·  팔룡회의 집념",24,205,492,28,15,Coral,TextAnchor.MiddleCenter,true);
        Title("여기서 쓰러질 순 없다!",24,244,492,64,40,Cream,TextAnchor.MiddleCenter);
        Text("부활 시 체력 100% 회복 + 주변 적 폭발 + 4.5초 무적 폭주!",24,318,492,32,16,Gold,TextAnchor.MiddleCenter,true);
        Surface(42,376,456,178,Slate,Gold);
        MotionSprite(heroMotionAtlas,heroPivots,new Vector2(270,520),12,false,165,Color.white,-4,Vector2.one);
        if(Button("⚡ 각성 부활 (체력 100% + 폭주)",36,588,468,72,Gold)){
            combat.Revive();source.PlayOneShot(specialSound,.85f);
        }
        if(Button("체육관으로 후퇴 & 정산하기",36,682,468,54,Cream)){
            combat.revivesUsed=1;SettleArcade();
        }
    }
    void StylePanel(){
        Title("도장 · 영구 단련 & 비기",24,120,492,54,34,Cream,TextAnchor.MiddleCenter);
        Text("보유 자금 $"+profile.coins+"  ·  단련할수록 매 판 더 강해집니다",24,178,492,28,15,Gold,TextAnchor.MiddleCenter,true);
        string[] statNames={"강철 주먹","금강불괴","황금 손"};
        string[] statDescs={
            "기본 공격력 및 공속 강화 (LV."+profile.fistLevel+")",
            "최대 체력 +"+(profile.bodyLevel*20)+" 증가 (LV."+profile.bodyLevel+")",
            "정산금 +"+(profile.greedLevel*12)+"% & 자석 (LV."+profile.greedLevel+")"
        };
        int[] statLevels={profile.fistLevel,profile.bodyLevel,profile.greedLevel};
        for(int i=0;i<3;i++){
            float x=24+i*166;int cost=profile.UpgradeCost(i);bool maxed=statLevels[i]>=10;bool canBuy=!maxed&&profile.coins>=cost;
            Surface(x,218,160,152,Slate,canBuy?Gold:Muted);
            Text(statNames[i],x+8,228,144,26,17,Gold,TextAnchor.MiddleCenter,true);
            Text(statDescs[i],x+8,258,144,44,12,Cream,TextAnchor.MiddleCenter);
            GUI.enabled=canBuy;
            if(Button(maxed?"MAX":"$"+cost,x+12,312,136,44,canBuy?Coral:Muted)){
                if(profile.UpgradeStat(i)){SaveArcade();source.PlayOneShot(specialSound,.45f);}
            }
            GUI.enabled=true;
        }
        Text("시작 기술 장착 (한 가지 선택)",24,388,492,26,15,Gold,TextAnchor.MiddleCenter,true);
        string[] names={"기본 권법","벽꿍 +1","충돌 전파 +1","연쇄 기절 +1"};
        for(int i=0;i<4;i++){
            float y=422+i*96;Surface(24,y,492,84,Slate,profile.selectedStyle==i?Gold:Muted);
            if(i>0)AbilityIcon(new Rect(34,y+6,72,72),i-1);
            Text(names[i],i>0?118:44,y+12,220,30,21,Cream,TextAnchor.MiddleLeft,true);
            Text(profile.selectedStyle==i?"현재 장착 중":"시작 시 즉시 LV.1 적용",i>0?118:44,y+46,220,24,13,Muted);
            bool own=profile.Owns(i);bool affordable=own||profile.coins>=ArcadeProfile.StyleCost;GUI.enabled=affordable&&profile.selectedStyle!=i;
            if(Button(profile.selectedStyle==i?"장착 중":own?"장착":"$"+ArcadeProfile.StyleCost,367,y+16,132,50,affordable?Gold:Muted)){
                if(own)profile.Equip(i);else profile.Unlock(i);SaveArcade();
            }
            GUI.enabled=true;
        }
        if(Button("돌아가기",24,824,492,58,Cream))styleMenu=false;
    }
}
