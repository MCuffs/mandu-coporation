using System;
using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

namespace SecondFistGame {
    public sealed class GymStation {
        public readonly int id,unlockStage,cost,incomePerCycle;
        public readonly string name,subtitle,benefit;
        public readonly Vector2 offset; // offset relative to arena center
        public int invested;
        public bool unlocked;
        public float workTimer,popScale;
        public GymStation(int id,string name,string subtitle,string benefit,int unlockStage,int cost,int income,Vector2 offset){
            this.id=id;this.name=name;this.subtitle=subtitle;this.benefit=benefit;
            this.unlockStage=unlockStage;this.cost=cost;this.incomePerCycle=income;this.offset=offset;
        }
    }

    public sealed class GymTrainee {
        public int stationId,appearance;
        public Vector2 pos;
        public float phase,punchClock;
        public bool facingLeft;
    }

    public sealed class GymWorld {
        public readonly List<GymStation> stations=new List<GymStation>();
        public readonly List<GymTrainee> trainees=new List<GymTrainee>();
        public int cashDeskPile,maxDeskPile=180;
        public float drainClock,deskFlipClock;
        public Vector2 cashDeskOffset=new Vector2(0,95);
        public Vector2 turfGateOffset=new Vector2(0,-195);

        public GymWorld(){
            stations.Add(new GymStation(0,"헤비 샌드백","펀치력 +1 · 수련생 모집","주먹 파괴력 증가",0,30,6,new Vector2(-128,-55)));
            stations.Add(new GymStation(1,"스텝 훈련 매트","공격속도 +20% · 수련생 모집","연타 속도 증가",0,55,10,new Vector2(128,-55)));
            stations.Add(new GymStation(2,"챔피언 스파링 링","든든한 동료 파이터 합류!","전투 시 동료 동행",0,90,16,new Vector2(-128,45)));
            stations.Add(new GymStation(3,"파워 벤치프레스","최대 체력 +40 · 벽꿍 강화","체력 및 벽꿍 증가",1,140,24,new Vector2(128,45)));
            stations.Add(new GymStation(4,"팔룡 비전 제단","필살기 폭주 & 코인 자석 강화","폭주 시간·자석 증가",2,210,36,new Vector2(0,-5)));
        }

        public Vector2 StationPos(Rect arena,GymStation s){
            return new Vector2(arena.center.x+s.offset.x*(arena.width/410f),arena.center.y+s.offset.y*(arena.height/450f));
        }
        public Vector2 CashDeskPos(Rect arena){
            return new Vector2(arena.center.x+cashDeskOffset.x,arena.center.y+cashDeskOffset.y*(arena.height/450f));
        }
        public Vector2 TurfGatePos(Rect arena){
            return new Vector2(arena.center.x,arena.yMin+28);
        }

        public bool HasPartner { get { return stations[2].unlocked; } }
        public int BuiltCount {
            get { int c=0;foreach(var s in stations)if(s.unlocked)c++;return c; }
        }

        public void SyncFromProfile(ArcadeProfile profile){
            for(int i=0;i<stations.Count;i++){
                stations[i].unlocked=profile.IsStationUnlocked(i);
                stations[i].invested=stations[i].unlocked?stations[i].cost:profile.GetStationInvested(i);
            }
            cashDeskPile=Mathf.Clamp(profile.gymSafeCoins,0,maxDeskPile);
            RebuildTrainees();
        }

        public void SyncToProfile(ArcadeProfile profile){
            for(int i=0;i<stations.Count;i++){
                profile.SetStationState(i,stations[i].unlocked,stations[i].invested);
            }
            profile.gymSafeCoins=cashDeskPile;
        }

        public void RebuildTrainees(){
            trainees.Clear();
            for(int i=0;i<stations.Count;i++){
                if(!stations[i].unlocked)continue;
                int count=i==2?2:1;
                for(int k=0;k<count;k++){
                    trainees.Add(new GymTrainee{
                        stationId=i,
                        appearance=(i+k)%3,
                        phase=(i*1.7f+k*2.3f),
                        facingLeft=(k%2==1)
                    });
                }
            }
        }

        public bool Tick(float dt,Vector2 playerPos,Rect arena,ArcadeProfile profile,List<CombatEvent> events,out bool builtAny,out int collectedCash){
            builtAny=false;collectedCash=0;
            bool dirty=false;
            for(int i=0;i<stations.Count;i++){
                GymStation s=stations[i];
                s.popScale=Mathf.Max(0,s.popScale-dt*3.2f);
                if(s.unlocked){
                    s.workTimer+=dt;
                    if(s.workTimer>=2.4f){
                        s.workTimer-=2.4f;
                        if(cashDeskPile<maxDeskPile){
                            cashDeskPile=Mathf.Min(maxDeskPile,cashDeskPile+s.incomePerCycle);
                            dirty=true;
                        }
                    }
                    continue;
                }
                if(profile.highestStage<s.unlockStage)continue;
                Vector2 pos=StationPos(arena,s);
                if((playerPos-pos).magnitude<56f&&profile.coins>0){
                    drainClock+=dt;
                    float stepInterval=.045f;
                    while(drainClock>=stepInterval&&profile.coins>0&&!s.unlocked){
                        drainClock-=stepInterval;
                        int stepAmount=Mathf.Min(profile.coins,Mathf.Max(1,Mathf.CeilToInt(s.cost/18f)));
                        int needed=s.cost-s.invested;
                        int pay=Mathf.Min(stepAmount,needed);
                        profile.coins-=pay;
                        s.invested+=pay;
                        s.popScale=.35f;
                        dirty=true;
                        if(s.invested>=s.cost){
                            s.unlocked=true;s.popScale=1f;builtAny=true;
                            RebuildTrainees();
                            events.Add(new CombatEvent(s.name+" 완성!!",pos,3));
                        }
                    }
                }
            }
            for(int i=0;i<trainees.Count;i++){
                GymTrainee t=trainees[i];
                t.phase+=dt*4.5f;
                t.punchClock-=dt;
                if(t.punchClock<=0)t.punchClock=UnityEngine.Random.Range(.45f,.95f);
            }
            Vector2 desk=CashDeskPos(arena);
            if(cashDeskPile>0&&(playerPos-desk).magnitude<62f){
                deskFlipClock+=dt;
                if(deskFlipClock>=.04f){
                    deskFlipClock=0;
                    int take=Mathf.Min(cashDeskPile,Mathf.Max(4,Mathf.CeilToInt(cashDeskPile*.35f)));
                    cashDeskPile-=take;
                    profile.coins+=take;
                    collectedCash+=take;
                    dirty=true;
                }
            }
            if(dirty)SyncToProfile(profile);
            return dirty;
        }
    }
}
