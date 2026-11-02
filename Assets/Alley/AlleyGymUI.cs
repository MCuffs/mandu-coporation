using System;
using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public partial class AlleyGame {
    public void UpdateGymHub(float dt){
        Vector2 movement=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)-(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0));
        if(Input.touchCount>0){
            for(int i=0;i<Input.touchCount;i++){
                Touch t=Input.GetTouch(i);Vector2 p=Pointer(t.position);
                if(t.phase==TouchPhase.Began&&!dragging&&p.y>145&&p.y<ViewHeight-85){dragging=true;dragFinger=t.fingerId;stickOrigin=p;}
                if(dragging&&t.fingerId==dragFinger){
                    if(t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled){dragging=false;stick=Vector2.zero;}
                    else stick=Vector2.ClampMagnitude((p-stickOrigin)/48,1);
                }
            }
        }else{
            Vector2 p=Pointer(Input.mousePosition);
            if(Input.GetMouseButtonDown(0)&&p.y>145&&p.y<ViewHeight-85){dragging=true;stickOrigin=p;}
            if(dragging&&Input.GetMouseButton(0))stick=Vector2.ClampMagnitude((p-stickOrigin)/48,1);
            if(Input.GetMouseButtonUp(0)){dragging=false;stick=Vector2.zero;}
        }
        movement=Vector2.ClampMagnitude(movement+stick,1);
        if(movement.magnitude<.1f)movement=Vector2.zero;
        combat.moveVelocity=Vector2.MoveTowards(combat.moveVelocity,movement*185f,1400f*dt);
        combat.player+=combat.moveVelocity*dt;
        combat.player=new Vector2(Mathf.Clamp(combat.player.x,combat.arena.xMin+16,combat.arena.xMax-16),Mathf.Clamp(combat.player.y,combat.arena.yMin+12,combat.arena.yMax-16));
        if(movement.sqrMagnitude>.03f)combat.direction=movement.normalized;
        combat.elapsed+=dt;
        if(gym.HasPartner){
            Vector2 target=combat.player+new Vector2(-46,20);
            combat.partnerVelocity=Vector2.MoveTowards(combat.partnerVelocity,(target-combat.partnerPos)*4.5f,800f*dt);
            combat.partnerPos+=combat.partnerVelocity*dt;
        }
        bool builtAny;int collectedCash;
        if(gym.Tick(dt,combat.player,combat.arena,profile,combat.events,out builtAny,out collectedCash)){
            SaveArcade();
            if(builtAny){
                shake=4.5f;choiceFlash=.25f;
                source.PlayOneShot(specialSound,.85f);
                PrepareArcade();
            }else if(collectedCash>0){
                pops.Add(new Pop{text="+$"+collectedCash,pos=combat.player+new Vector2(0,-20),style=2});
                source.PlayOneShot(hitSound,.25f);
            }else{
                source.PlayOneShot(hitSound,.12f);
            }
        }
        foreach(CombatEvent e in combat.events){
            pops.Add(new Pop{text=e.text,pos=e.pos,style=e.style});AddImpact(e);
        }
        combat.events.Clear();
        UpdateMotion(dt,true);
        Vector2 gate=gym.TurfGatePos(combat.arena);
        if(combat.player.y<=gate.y+26f&&Mathf.Abs(combat.player.x-gate.x)<95f){
            source.PlayOneShot(specialSound,.75f);
            StartStage(stage);
        }
    }

    Vector2 ProjectPoint(Vector2 arenaPos,float yOffset3D=0f){
        if(world3D!=null){
            Vector3 w=world3D.MapTo3D(arenaPos,combat.arena)+new Vector3(0,yOffset3D,0);
            return world3D.WorldToGui(w,Scale);
        }
        return arenaPos;
    }

    public void DrawGymWorld(){
        Rect arena=combat.arena;
        // 1. Top Turf Battle Gate Floating 3D Banner
        Vector2 gateGui=ProjectPoint(gym.TurfGatePos(arena),2.3f);
        Surface(gateGui.x-115,gateGui.y-24,230,46,new Color(.18f,.06f,.08f,.92f),Coral,false);
        Outlined("▲ 골목 출격: "+StageCatalog.Get(stage).name+" ▲",gateGui.x-125,gateGui.y-21,250,24,14,Gold,TextAnchor.MiddleCenter,true);
        Text("위로 걸어가면 즉시 골목 전투 시작!",gateGui.x-125,gateGui.y+1,250,18,11,Cream,TextAnchor.MiddleCenter);

        // 2. Draw 3D-Projected Labels & Progress Badges for 5 Gym Stations
        Vector2 heroGui=ProjectPoint(combat.player,0.3f);
        Vector2 guideTargetGui=Vector2.zero;
        bool hasGuideTarget=false;

        for(int i=0;i<gym.stations.Count;i++){
            GymStation s=gym.stations[i];
            Vector2 pos2D=gym.StationPos(arena,s);
            Vector2 guiPos=ProjectPoint(pos2D,s.unlocked?1.65f:0.55f);
            bool stageUnlocked=profile.highestStage>=s.unlockStage;
            if(!s.unlocked&&stageUnlocked&&!hasGuideTarget){
                guideTargetGui=guiPos;hasGuideTarget=true;
            }
            if(s.unlocked){
                Surface(guiPos.x-66,guiPos.y-16,132,36,new Color(.08f,.12f,.18f,.88f),Gold,false);
                Text("★ "+s.name,guiPos.x-62,guiPos.y-15,124,18,11,Gold,TextAnchor.MiddleCenter,true);
                Text("+$"+s.incomePerCycle+" 수련비 · "+s.benefit,guiPos.x-62,guiPos.y+1,124,16,10,Mint,TextAnchor.MiddleCenter);
            }else if(stageUnlocked){
                float ratio=Mathf.Clamp01(s.invested/(float)Mathf.Max(1,s.cost));
                int remain=Mathf.Max(0,s.cost-s.invested);
                bool canAfford=profile.coins>0;
                Surface(guiPos.x-66,guiPos.y-30,132,62,new Color(.10f,.15f,.22f,.92f),canAfford?Gold:Cream,true);
                if(ratio>0)Box(guiPos.x-63,guiPos.y-27,126*ratio,56,new Color(.28f,.82f,.56f,.40f));
                Outlined(s.name,guiPos.x-62,guiPos.y-26,124,20,12,Cream,TextAnchor.MiddleCenter,true);
                Text(s.subtitle,guiPos.x-62,guiPos.y-7,124,16,10,Mint,TextAnchor.MiddleCenter);
                Surface(guiPos.x-44,guiPos.y+10,88,20,canAfford?Gold:Slate,Ink,false);
                Text("💵 $"+remain,guiPos.x-44,guiPos.y+10,88,20,12,canAfford?Ink:Cream,TextAnchor.MiddleCenter,true);
            }else{
                Surface(guiPos.x-58,guiPos.y-20,116,42,new Color(.08f,.10f,.14f,.82f),Muted,false);
                Text("🔒 "+s.name,guiPos.x-54,guiPos.y-17,108,18,11,Muted,TextAnchor.MiddleCenter,true);
                Text("구역 "+s.unlockStage+" 해방 필요",guiPos.x-54,guiPos.y+1,108,16,10,Cream,TextAnchor.MiddleCenter);
            }
        }

        // Supercent Golden Guide Line on the ground pointing to the next target!
        if(hasGuideTarget&&profile.coins>0){
            Color guideCol=new Color(1f,.82f,.22f,.55f+Mathf.Sin(pulse*8f)*.25f);
            Line(heroGui,guideTargetGui,3.5f,guideCol);
            CircleAt(guideTargetGui,8f+Mathf.Sin(pulse*10f)*2f,Gold);
        }

        // 3. Cash Safe Desk Floating Counter
        Vector2 deskGui=ProjectPoint(gym.CashDeskPos(arena),1.15f);
        Surface(deskGui.x-74,deskGui.y-22,148,46,new Color(.11f,.18f,.16f,.92f),Mint,true);
        Text("💰 수련비 금고 (걸어가서 수거)",deskGui.x-70,deskGui.y-19,140,18,10,Mint,TextAnchor.MiddleCenter,true);
        Title("$"+gym.cashDeskPile,deskGui.x-70,deskGui.y-2,140,24,19,Gold,TextAnchor.MiddleCenter);

        DrawCombatText();
        DrawGymHUD();
    }

    void DrawGymHUD(){
        // Top Header with Fame Rank Badge & Money Counter
        Surface(16,12,ViewWidth-84,66,new Color(.09f,.13f,.20f,.95f),Gold,true);
        Outlined(profile.FameTitle+"  ·  정민수 체육관",28,15,280,26,17,Gold,TextAnchor.MiddleLeft,true);
        Text("시설 "+gym.BuiltCount+"/5   ·   수련생 "+gym.trainees.Count+"명   ·   동료 "+(gym.HasPartner?"합류 완료":"미영입"),28,43,290,22,12,Cream,TextAnchor.MiddleLeft,true);
        Surface(ViewWidth-218,20,140,48,new Color(.16f,.22f,.18f),Gold,false);
        Title("💵 $"+profile.coins,ViewWidth-214,22,132,44,23,Gold,TextAnchor.MiddleCenter);

        // Pause Button
        CircleAt(new Vector2(ViewWidth-34,45),21,new Color(.08f,.11f,.16f,.88f));
        Box(ViewWidth-41,37,4,16,Cream);Box(ViewWidth-31,37,4,16,Cream);
        if(GUI.Button(new Rect(ViewWidth-59,20,50,50),GUIContent.none,GUIStyle.none)){paused=!paused;dragging=false;stick=Vector2.zero;}

        // Addictive Milestone Quest & Achievement Bar (One-Tap Dopamine Reward!)
        string qTitle;int qReward,qIndex;bool qClaimable;
        if(CheckQuestProgress(out qTitle,out qReward,out qClaimable,out qIndex)){
            Surface(16,84,ViewWidth-32,46,qClaimable?new Color(.24f,.18f,.06f,.96f):new Color(.12f,.16f,.24f,.92f),qClaimable?Gold:Mint,false);
            Text("🎯 목표: "+qTitle,28,88,ViewWidth-180,22,13,Cream,TextAnchor.MiddleLeft,true);
            Text("보상: +$"+qReward+" 정산금 즉시 지급",28,108,ViewWidth-180,18,11,Gold,TextAnchor.MiddleLeft);
            if(qClaimable){
                if(Button("보상 +$"+qReward+" 받기!",ViewWidth-154,89,130,36,Gold))ClaimQuest(qIndex,qReward);
            }else{
                Surface(ViewWidth-146,92,120,30,Slate,Muted,false);
                Text("진행 중",ViewWidth-146,92,120,30,12,Muted,TextAnchor.MiddleCenter,true);
            }
        }

        // Bottom Quick Action Buttons
        float by=ViewHeight-68;
        if(Button("⚔ 즉시 골목 출격 ("+StageCatalog.Get(stage).name+")",20,by,ViewWidth-186,52,Coral))StartStage(stage);
        if(Button("구획 선택",ViewWidth-156,by,136,52,Gold))stageSelect=true;

        if(dragging){
            Vector2 basePos=new Vector2(Mathf.Clamp(stickOrigin.x,55,ViewWidth-55),Mathf.Clamp(stickOrigin.y,150,ViewHeight-55));
            CircleAt(basePos,48,new Color(.04f,.065f,.10f,.18f));Ring(basePos,47,1.5f,new Color(.85f,.9f,.83f,.44f));
            CircleAt(basePos+stick*27,23,new Color(.85f,.9f,.83f,.55f));
        }
    }
}
