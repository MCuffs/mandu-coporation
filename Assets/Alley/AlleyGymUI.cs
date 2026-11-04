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

    void DrawGymStation2D(GymStation s,Vector2 pos,bool stageUnlocked){
        float pop=1f+Mathf.Sin(Mathf.Clamp01(s.popScale)*Mathf.PI)*.18f;
        int equipIdx=s.id==0?0:s.id==1?1:s.id==2?3:2;
        if(s.unlocked){
            Ellipse(pos+new Vector2(0,6),116,36,new Color(0,0,0,.26f));
            Ellipse(pos+new Vector2(0,3),108,32,new Color(1f,.78f,.24f,.18f));
            float size=(s.id==2?126f:108f)*pop;
            float sway=s.id==0?Mathf.Sin(pulse*7f)*3.5f:0f;
            Rect eqRect=new Rect(pos.x-size*.5f+sway,pos.y-size*.82f,size,size);
            if(s.id==4){
                Ellipse(pos,92,28,new Color(.18f,.22f,.30f,.9f));
                AbilityIcon(new Rect(pos.x-44*pop,pos.y-82*pop,88*pop,88*pop),3,1f);
            }else{
                DrawEquipmentIcon(eqRect,equipIdx,Color.white);
            }
            // Draw Trainees practicing at this station
            for(int i=0;i<gym.trainees.Count;i++){
                GymTrainee gt=gym.trainees[i];
                if(gt.stationId!=s.id)continue;
                Vector2 tFeet=pos+new Vector2(gt.facingLeft?44f:-44f,10f);
                float bob=Mathf.Sin(gt.phase)*1.6f;
                Ellipse(tFeet+new Vector2(0,2),38,10,new Color(0,0,0,.22f));
                int pose=gt.punchClock<.22f?2:(Mathf.FloorToInt(gt.phase/Mathf.PI)&1);
                if(rosterAtlas!=null&&rosterPivots!=null){
                    MotionSprite(rosterAtlas,rosterPivots,tFeet-new Vector2(0,bob),gt.appearance*4+pose,gt.facingLeft,108f,Color.white,0f,Vector2.one);
                }
            }
            // Compact 1-line badge below station
            bool nearPlayer=(combat.player-pos).magnitude<82f;
            float bw=124f,bh=!s.IsMaxed&&(nearPlayer||s.invested>0)?36f:22f;
            Surface(pos.x-bw*.5f,pos.y+8,bw,bh,new Color(.07f,.10f,.16f,.90f),Gold,false);
            if(!s.IsMaxed&&s.invested>0){
                float upRatio=Mathf.Clamp01(s.invested/(float)Mathf.Max(1,s.CurrentCost));
                Box(pos.x-bw*.5f+2,pos.y+10,(bw-4)*upRatio,bh-4,new Color(.28f,.82f,.56f,.38f));
            }
            Text(s.name+" LV."+s.level,pos.x-bw*.5f+4,pos.y+9,bw-8,18,11,Gold,TextAnchor.MiddleCenter,true);
            if(!s.IsMaxed&&(nearPlayer||s.invested>0)){
                int upRemain=Mathf.Max(0,s.CurrentCost-s.invested);
                Text("▲ 증축 $"+upRemain+" (+"+s.CurrentIncome+"/주기)",pos.x-bw*.5f+4,pos.y+24,bw-8,16,10,Mint,TextAnchor.MiddleCenter,true);
            }
        }else if(stageUnlocked){
            float ratio=Mathf.Clamp01(s.invested/(float)Mathf.Max(1,s.cost));
            int remain=Mathf.Max(0,s.cost-s.invested);
            bool canAfford=profile.coins>0;
            Ellipse(pos+new Vector2(0,4),104,34,new Color(.06f,.09f,.14f,.65f));
            Ellipse(pos+new Vector2(0,4),96,30,canAfford?new Color(1f,.78f,.22f,.22f+Mathf.Sin(pulse*6f)*.08f):new Color(1,1,1,.10f));
            if(s.id<4)DrawEquipmentIcon(new Rect(pos.x-42,pos.y-72,84,84),equipIdx,new Color(1,1,1,.28f));
            float bw=118f,bh=38f;
            Surface(pos.x-bw*.5f,pos.y-8,bw,bh,new Color(.09f,.13f,.20f,.92f),canAfford?Gold:Muted,false);
            if(ratio>0)Box(pos.x-bw*.5f+2,pos.y-6,(bw-4)*ratio,bh-4,new Color(.28f,.82f,.56f,.42f));
            Text(s.name,pos.x-bw*.5f+4,pos.y-6,bw-8,18,11,Cream,TextAnchor.MiddleCenter,true);
            Text("💵 $"+remain+" · "+s.benefit,pos.x-bw*.5f+4,pos.y+10,bw-8,16,10,canAfford?Gold:Muted,TextAnchor.MiddleCenter,true);
        }else{
            Ellipse(pos+new Vector2(0,4),92,28,new Color(0,0,0,.35f));
            Surface(pos.x-54,pos.y-10,108,28,new Color(.07f,.09f,.13f,.82f),Muted,false);
            Text("🔒 구역 "+(s.unlockStage+1)+" 해방",pos.x-50,pos.y-8,100,24,10,Muted,TextAnchor.MiddleCenter,true);
        }
    }

    public void DrawGymWorld(){
        Matrix4x4 old=GUI.matrix;Vector2 jitter=shake>0?new Vector2(Mathf.PerlinNoise(pulse*34,0)-.5f,Mathf.PerlinNoise(0,pulse*34)-.5f)*shake:Vector2.zero;
        GUI.matrix=old*Matrix4x4.Translate(jitter);
        if(gymBackdrop!=null){GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,ViewWidth,ViewHeight),gymBackdrop,ScaleMode.ScaleAndCrop);}
        else Floor();

        Rect arena=combat.arena;
        // 1. Top Turf Battle Gate Banner at open shutter door
        Vector2 gateGui=gym.TurfGatePos(arena);
        float gatePulse=Mathf.Sin(pulse*6f)*3f;
        Ellipse(gateGui+new Vector2(0,12),140,26,new Color(1f,.29f,.17f,.22f+Mathf.Sin(pulse*8f)*.08f));
        Surface(gateGui.x-112,gateGui.y-34+gatePulse,224,36,new Color(.14f,.06f,.08f,.90f),Coral,false);
        Outlined("▲ 골목 출격: "+StageCatalog.Get(stage).name+" ▲",gateGui.x-110,gateGui.y-32+gatePulse,220,20,13,Gold,TextAnchor.MiddleCenter,true);
        Text("위로 걸어가면 즉시 전투 시작",gateGui.x-110,gateGui.y-15+gatePulse,220,15,10,Cream,TextAnchor.MiddleCenter);

        // Subtle Golden Guide Line to next affordable station
        for(int i=0;i<gym.stations.Count;i++){
            GymStation s=gym.stations[i];
            if(!s.unlocked&&profile.highestStage>=s.unlockStage&&profile.coins>0){
                Vector2 targetPos=gym.StationPos(arena,s);
                Color guideCol=new Color(1f,.82f,.22f,.40f+Mathf.Sin(pulse*8f)*.18f);
                Line(combat.player,targetPos,2.5f,guideCol);
                CircleAt(targetPos,6f+Mathf.Sin(pulse*10f)*1.5f,Gold);
                break;
            }
        }

        // Cash Safe Desk on floor
        Vector2 deskGui=gym.CashDeskPos(arena);
        Ellipse(deskGui+new Vector2(0,6),112,30,new Color(.12f,.82f,.52f,.22f));
        int stackCount=Mathf.Clamp(gym.cashDeskPile/15,0,8);
        for(int b=0;b<stackCount;b++){
            Panel(deskGui.x-48+(b%2)*16,deskGui.y-18-(b/2)*5,22,10,new Color(.22f,.82f,.45f));
        }
        Surface(deskGui.x-64,deskGui.y-12,128,32,new Color(.08f,.15f,.13f,.92f),Mint,false);
        Text("💰 수련비 금고  $"+gym.cashDeskPile,deskGui.x-60,deskGui.y-10,120,28,12,gym.cashDeskPile>0?Gold:Cream,TextAnchor.MiddleCenter,true);

        // 2. Y-Sorted Stations, Partner & Hero
        List<int> stationOrder=new List<int>{0,1,4,2,3};
        stationOrder.Sort((a,b)=>gym.StationPos(arena,gym.stations[a]).y.CompareTo(gym.StationPos(arena,gym.stations[b]).y));
        bool heroDrawn=false,partnerDrawn=!gym.HasPartner;
        for(int idx=0;idx<stationOrder.Count;idx++){
            GymStation s=gym.stations[stationOrder[idx]];
            Vector2 sPos=gym.StationPos(arena,s);
            if(!partnerDrawn&&sPos.y>combat.partnerPos.y){Partner();partnerDrawn=true;}
            if(!heroDrawn&&sPos.y>combat.player.y){Hero();heroDrawn=true;}
            DrawGymStation2D(s,sPos,profile.highestStage>=s.unlockStage);
        }
        if(!partnerDrawn)Partner();
        if(!heroDrawn)Hero();

        DrawAfterimagesAndDust();
        DrawCombatText();
        GUI.matrix=old;
        DrawGymHUD();
    }

    void DrawGymHUD(){
        // Top Header with Fame Rank Badge & Money Counter
        Surface(16,12,ViewWidth-84,66,new Color(.09f,.13f,.20f,.95f),Gold,true);
        Outlined(profile.FameTitle+"  ·  팔룡체육관",28,15,286,26,16,Gold,TextAnchor.MiddleLeft,true);
        Text("시설 "+gym.BuiltCount+"/5 (LV."+profile.TotalStationLevels+") · 장비 ["+profile.GearDisplayName+"]",28,43,286,22,11,Cream,TextAnchor.MiddleLeft,true);
        Surface(ViewWidth-218,20,140,48,new Color(.16f,.22f,.18f),Gold,false);
        Title("💵 $"+profile.coins,ViewWidth-214,22,132,44,21,Gold,TextAnchor.MiddleCenter);

        // Pause Button
        CircleAt(new Vector2(ViewWidth-34,45),21,new Color(.08f,.11f,.16f,.88f));
        Box(ViewWidth-41,37,4,16,Cream);Box(ViewWidth-31,37,4,16,Cream);
        if(GUI.Button(new Rect(ViewWidth-59,20,50,50),GUIContent.none,GUIStyle.none)){paused=!paused;dragging=false;stick=Vector2.zero;}

        // Addictive Milestone Quest & Achievement Bar (One-Tap Dopamine Reward!)
        string qTitle;int qReward,qIndex;bool qClaimable;
        if(CheckQuestProgress(out qTitle,out qReward,out qClaimable,out qIndex)){
            Surface(16,84,ViewWidth-32,46,qClaimable?new Color(.24f,.18f,.06f,.96f):new Color(.12f,.16f,.24f,.92f),qClaimable?Gold:Mint,false);
            Text("🎯 목표: "+qTitle,28,88,ViewWidth-186,22,12,Cream,TextAnchor.MiddleLeft,true);
            Text("보상: +$"+qReward+" 정산금 즉시 지급",28,108,ViewWidth-186,18,11,Gold,TextAnchor.MiddleLeft);
            if(qClaimable){
                if(Button("보상 +$"+qReward+" 받기",ViewWidth-154,89,130,36,Gold,13))ClaimQuest(qIndex,qReward);
            }else{
                Surface(ViewWidth-146,92,120,30,Slate,Muted,false);
                Text("진행 중",ViewWidth-146,92,120,30,12,Muted,TextAnchor.MiddleCenter,true);
            }
        }

        // Bottom Quick Action Buttons (Battle + Gear Forge + Turf Select)
        float by=ViewHeight-68;
        if(Button("⚔ 골목 출격",16,by,182,52,Coral,18))StartStage(stage);
        bool canForge=profile.coins>=ArcadeProfile.GearRollCost;
        if(Button("🥊 권갑연성 $"+ArcadeProfile.GearRollCost,206,by,182,52,canForge?Gold:Cream,16)){
            if(canForge)TriggerGearForge();else styleMenu=true;
        }
        if(Button("구획 선택",396,by,128,52,Mint,17))stageSelect=true;

        if(dragging){
            Vector2 basePos=new Vector2(Mathf.Clamp(stickOrigin.x,55,ViewWidth-55),Mathf.Clamp(stickOrigin.y,150,ViewHeight-55));
            CircleAt(basePos,48,new Color(.04f,.065f,.10f,.18f));Ring(basePos,47,1.5f,new Color(.85f,.9f,.83f,.44f));
            CircleAt(basePos+stick*27,23,new Color(.85f,.9f,.83f,.55f));
        }
    }
}
