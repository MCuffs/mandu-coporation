using System;
using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public partial class AlleyGame {
    [Serializable] public class FramePivot { public float x=.5f,y=.12f,u,v,w=.25f,h=.25f,scale=1; }
    [Serializable] public class PivotSheet { public int columns=4,rows=4;public FramePivot[] frames; }
    sealed class ActorMotion {
        public Vector2 rendered,last,corpseVelocity;public float phase,spawn,death=-1,tilt,speed,turn;public bool targetLeft;
        public Fighter fighter;public bool left;
    }
    sealed class Spark { public Vector2 pos,velocity;public float life,total;public Color color;public float size; }
    readonly Dictionary<int,ActorMotion> actorMotion=new Dictionary<int,ActorMotion>();
    readonly List<Spark> sparks=new List<Spark>();
    Texture2D heroMotionAtlas,enemyMotionAtlas,abilityAtlas;
    PivotSheet heroPivots,enemyPivots,legacyPivots;
    readonly List<int> expiredActors=new List<int>();
    static readonly int[] BruiserFrames={8,9,10,11,13,15,10,9};
    float heroTurn,motionClock,lastModelElapsed;bool heroTargetLeft;
    Vector2 heroRendered,heroLast;float heroPhase,heroSpeed,heroTilt,modalAge,menuAge,specialFlash,bossBanner;
    float shownHealth=100,healthTrail=100,shownCharge=100,shownXp,choiceFlash;
    bool heroLeft;RunState shownState;bool readyPresentation;
    void LoadPresentation(){
        heroMotionAtlas=Resources.Load<Texture2D>("HeroMotion");enemyMotionAtlas=Resources.Load<Texture2D>("EnemyMotion");abilityAtlas=Resources.Load<Texture2D>("AbilityIcons");
        TextAsset pivots=Resources.Load<TextAsset>("HeroMotionPivots");if(pivots!=null)heroPivots=JsonUtility.FromJson<PivotSheet>(pivots.text);
        pivots=Resources.Load<TextAsset>("EnemyMotionPivots");if(pivots!=null)enemyPivots=JsonUtility.FromJson<PivotSheet>(pivots.text);
        pivots=Resources.Load<TextAsset>("FighterAtlasPivots");if(pivots!=null)legacyPivots=JsonUtility.FromJson<PivotSheet>(pivots.text);
    }
    void ResetMotion(){
        actorMotion.Clear();sparks.Clear();heroRendered=heroLast=new Vector2(270,535);heroSpeed=heroPhase=heroTilt=heroTurn=motionClock=lastModelElapsed=0;heroLeft=heroTargetLeft=false;
        shownHealth=healthTrail=combat!=null?combat.maxHealth:100;shownCharge=100;shownXp=0;menuAge=modalAge=0;shownState=RunState.Ready;readyPresentation=true;
    }
    void AdvancePresentation(float dt){
        dt=Mathf.Min(dt,.06f);menuAge+=dt;battleReveal=Mathf.Max(0,battleReveal-dt);
        if(shownState!=combat.state){shownState=combat.state;modalAge=0;if(shownState==RunState.Choosing)choiceFlash=.20f;}
        modalAge+=dt;specialFlash=Mathf.Max(0,specialFlash-dt);choiceFlash=Mathf.Max(0,choiceFlash-dt);bossBanner=Mathf.Max(0,bossBanner-dt);
        shownHealth=Mathf.Lerp(shownHealth,combat.health,1-Mathf.Exp(-dt*16));
        healthTrail=Mathf.MoveTowards(healthTrail,shownHealth,dt*23);
        shownCharge=Mathf.Lerp(shownCharge,combat.charge,1-Mathf.Exp(-dt*11));
        shownXp=Mathf.Lerp(shownXp,combat.xp/(float)combat.nextXp,1-Mathf.Exp(-dt*10));
        for(int i=sparks.Count-1;i>=0;i--){Spark s=sparks[i];s.life-=dt;s.pos+=s.velocity*dt;s.velocity*=Mathf.Exp(-dt*5);s.velocity.y+=dt*80;if(s.life<=0)sparks.RemoveAt(i);}
    }
    void UpdateMotion(float dt,bool playing){
        if(!readyPresentation||!playing)return;
        dt=Mathf.Min(dt,.10f);float smooth=1-Mathf.Exp(-dt*40);
        float simulationDt=Mathf.Clamp(combat.elapsed-lastModelElapsed,0,.12f);lastModelElapsed=combat.elapsed;motionClock+=simulationDt;
        if((combat.player-heroLast).sqrMagnitude>80*80)heroRendered=combat.player;
        heroLast=combat.player;heroRendered=Vector2.Lerp(heroRendered,combat.player,smooth);
        heroSpeed=Mathf.Lerp(heroSpeed,combat.moveVelocity.magnitude,1-Mathf.Exp(-dt*18));
        if(combat.playerReaction<=0)heroPhase+=combat.moveVelocity.magnitude*simulationDt/96f*Mathf.PI*2;
        float targetTilt=Mathf.Clamp(combat.moveVelocity.x/172f,-1,1)*-2.5f;
        if(combat.playerReaction>0)targetTilt=Mathf.Clamp(combat.playerImpulse.x*.035f,-7,7);
        heroTilt=Mathf.Lerp(heroTilt,targetTilt,1-Mathf.Exp(-dt*18));
        bool look=heroTargetLeft;if(combat.direction.x>.24f)look=false;else if(combat.direction.x<-.24f)look=true;
        Turn(ref heroLeft,ref heroTargetLeft,ref heroTurn,look,simulationDt);
        foreach(Fighter e in combat.enemies){
            ActorMotion m;
            if(!actorMotion.TryGetValue(e.id,out m)){m=new ActorMotion{rendered=e.pos,last=e.pos,fighter=e,left=e.facing<0,targetLeft=e.facing<0};actorMotion.Add(e.id,m);}
            if((e.pos-m.last).sqrMagnitude>80*80)m.rendered=e.pos;
            m.last=e.pos;m.fighter=e;m.rendered=Vector2.Lerp(m.rendered,e.pos,smooth);m.spawn=Mathf.Min(1,m.spawn+simulationDt*6);
            m.speed=Mathf.Lerp(m.speed,e.locomotion.magnitude,1-Mathf.Exp(-dt*16));
            if(e.reaction<=0&&e.windup<=0&&e.dash<=0)m.phase+=e.locomotion.magnitude*simulationDt/76f*Mathf.PI*2;
            float lean=e.reaction>0?Mathf.Clamp(e.velocity.x*.025f,-19,19):e.dash>0?e.aim.x*-10:e.windup>0?-e.facing*5:Mathf.Clamp(e.locomotion.x*.035f,-3,3);
            m.tilt=Mathf.Lerp(m.tilt,lean,1-Mathf.Exp(-dt*16));
            Turn(ref m.left,ref m.targetLeft,ref m.turn,e.facing<0,simulationDt);
        }
        expiredActors.Clear();
        foreach(var entry in actorMotion){ActorMotion m=entry.Value;if(m.fighter.hp>0)continue;
            if(m.death<0){m.death=0;m.corpseVelocity=m.fighter.velocity*.45f;}
            m.death+=simulationDt;m.rendered+=m.corpseVelocity*simulationDt;m.corpseVelocity*=Mathf.Exp(-8*simulationDt);
            m.rendered=new Vector2(Mathf.Clamp(m.rendered.x,combat.arena.xMin,combat.arena.xMax),Mathf.Clamp(m.rendered.y,combat.arena.yMin,combat.arena.yMax));
            if(m.death>.85f)expiredActors.Add(entry.Key);
        }
        foreach(int id in expiredActors)actorMotion.Remove(id);
    }
    static void Turn(ref bool facing,ref bool target,ref float remaining,bool wanted,float dt){
        if(wanted!=target){target=wanted;remaining=.09f;}
        if(remaining<=0){facing=target;return;}
        float before=remaining;remaining=Mathf.Max(0,remaining-dt);
        if(before>.045f&&remaining<=.045f)facing=target;
        if(remaining<=0)facing=target;
    }
    float TurnWidth(float remaining){return remaining<=0?1:1-Mathf.Sin((1-remaining/.09f)*Mathf.PI)*.10f;}

    void AddImpact(CombatEvent e){
        if(e.style==3&&(e.text=="전성기의 한 방"||e.text=="부활! 폭주 개시!"))specialFlash=.22f;
        if(e.text=="골목 대장 등장"||e.text=="대장 광폭화!!")bossBanner=2.3f;
        if(e.style==0&&e.text=="제압")return;
        if(e.text.StartsWith("벽꿍")||e.text.StartsWith("연쇄")||e.text=="퍽!"||e.text=="크리티컬!"||e.text=="강타!"||e.style==4){
            Color c=e.style==2?Mint:e.style==4?Coral:e.text=="크리티컬!"||e.text=="강타!"?new Color(1,.45f,.15f):new Color(1,.76f,.33f);
            int count=e.text=="퍽!"?6:e.text=="크리티컬!"?14:10;
            for(int i=0;i<count;i++){float a=UnityEngine.Random.value*Mathf.PI*2;float speed=UnityEngine.Random.Range(45,165);sparks.Add(new Spark{pos=e.pos-new Vector2(0,45),velocity=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*speed,life=.32f,total=.32f,color=c,size=UnityEngine.Random.Range(2,5.5f)});}
        }
    }
    float EaseOut(float t){t=Mathf.Clamp01(t);return 1-Mathf.Pow(1-t,3);}
    float PopScale(float t){t=Mathf.Clamp01(t);return 1+Mathf.Sin(t*Mathf.PI)*.09f;}
    void Ellipse(Vector2 p,float width,float height,Color c){GUI.color=c;GUI.DrawTexture(new Rect(p.x-width*.5f,p.y-height*.5f,width,height),disc);GUI.color=Color.white;}
    void MotionSprite(Texture2D sheet,PivotSheet pivots,Vector2 feet,int frame,bool left,float height,Color tint,float tilt,Vector2 stretch){
        if(sheet==null){DrawSprite(feet,0,left,height,tint,tilt);return;}
        int rows=pivots!=null&&pivots.rows>0?pivots.rows:4,columns=pivots!=null&&pivots.columns>0?pivots.columns:4;
        frame=Mathf.Clamp(frame,0,rows*columns-1);float px=.5f,py=.12f,normalization=1;
        Rect uv=new Rect(frame%columns/(float)columns,(rows-1-frame/columns)/(float)rows,1f/columns,1f/rows);
        if(pivots!=null&&pivots.frames!=null&&frame<pivots.frames.Length){FramePivot pivot=pivots.frames[frame];px=pivot.x;py=pivot.y;uv=new Rect(pivot.u,pivot.v,pivot.w,pivot.h);normalization=pivot.scale>0?pivot.scale:1;}
        float drawW=height*uv.width*(sheet.width/(float)sheet.height)*rows*normalization,drawH=height*uv.height*rows*normalization;
        Matrix4x4 old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(new Vector3(feet.x,feet.y,0),Quaternion.Euler(0,0,tilt),new Vector3((left?-1:1)*stretch.x,stretch.y,1));
        GUI.color=tint;GUI.DrawTextureWithTexCoords(new Rect(-px*drawW,-(1-py)*drawH,drawW,drawH),sheet,uv);GUI.color=Color.white;GUI.matrix=old;
    }
    void LegacyReaction(Vector2 feet,int frame,bool left,float height,Color tint,float tilt,Vector2 stretch){
        if(legacyPivots==null){DrawSprite(feet,frame,left,height,tint,tilt);return;}
        MotionSprite(atlas,legacyPivots,feet,frame,left,height,tint,tilt,stretch);
    }

    int AttackFrame(){
        float t=.44f-combat.attackAnimation;
        if(t<.025f)return 8;if(t<.058f)return 9;if(t<.083f)return 10;if(t<.103f)return 11;
        if(t<.16f)return 12;if(t<.235f)return 13;if(t<.33f)return 14;return 15;
    }
    void Partner(){
        if(!combat.hasPartner&&!gym.HasPartner)return;
        Vector2 feet=combat.partnerPos;
        float speed=combat.partnerVelocity.magnitude;
        bool moving=speed>12f;
        float bob=moving?Mathf.Sin(motionClock*11f)*1.4f:Mathf.Sin(motionClock*3.2f)*.5f;
        Ellipse(feet+new Vector2(0,2),44,12,new Color(0,0,0,.22f));
        Ellipse(feet+new Vector2(0,1),48,14,new Color(.20f,.85f,.95f,.22f));
        bool left=combat.partnerDirection.x<-.1f;
        int pose=combat.partnerPunch>0?2:moving?(Mathf.FloorToInt(motionClock*6f)&1):0;
        if(rosterAtlas!=null&&rosterPivots!=null){
            MotionSprite(rosterAtlas,rosterPivots,feet-new Vector2(0,bob),pose,left,124f,Color.white,0f,Vector2.one);
        }
        if(combat.partnerPunch>0){
            float progress=Mathf.Clamp01(1-combat.partnerPunch/.18f);
            float a=Mathf.Atan2(combat.partnerDirection.y,combat.partnerDirection.x);
            Vector2 center=feet-new Vector2(0,42);
            for(int i=0;i<10;i++){
                float p=i/9f;float angle=a-.8f+p*1.6f;float next=angle+.16f;
                Vector2 p1=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(48+progress*18);
                Vector2 p2=center+new Vector2(Mathf.Cos(next),Mathf.Sin(next))*(48+progress*18);
                Line(p1,p2,8f*Mathf.Sin(p*Mathf.PI)*(1-progress),new Color(.25f,.92f,1f,1-progress));
            }
        }
    }
    void Hero(){
        Vector2 feet=heroRendered;bool moving=heroSpeed>14;
        float bob=moving?Mathf.Sin(heroPhase*2)*1.35f:Mathf.Sin(motionClock*3.1f)*.55f;
        Ellipse(feet+new Vector2(0,2),moving?48:52,13,new Color(0,0,0,.26f));
        int gearTier=CurrentGearTier;
        if(combat.isFever){
            float aura=62+Mathf.Sin(pulse*18)*6;
            Ellipse(feet+new Vector2(0,1),aura,19,new Color(1,.76f,.18f,.38f));
            CircleAt(feet-new Vector2(0,55),46+Mathf.Sin(pulse*22)*4,new Color(1,.58f,.12f,.16f));
        }else if(gearTier>=2){
            Color gearAura=gearTier>=5?new Color(1f,.82f,.18f,.26f):gearTier>=4?new Color(.20f,.88f,1f,.22f):new Color(1f,.35f,.18f,.18f);
            Ellipse(feet+new Vector2(0,1),52+Mathf.Sin(pulse*8)*3,14,gearAura);
        }else{
            Ellipse(feet+new Vector2(0,1),48,12,new Color(.57f,.84f,.70f,.09f));
        }
        int frame=combat.attackAnimation>0?AttackFrame():moving?(Mathf.FloorToInt(heroPhase/(Mathf.PI*2)*8)%8):8;
        float attackT=.44f-combat.attackAnimation;
        float thrust=combat.attackAnimation>0?Mathf.Sin(Mathf.Clamp01((attackT-.05f)/.21f)*Mathf.PI)*(combat.comboStep==0?11:6):0;
        feet+=combat.direction*thrust-new Vector2(0,bob);
        Color tint=combat.playerReaction>0?new Color(1,.72f,.62f):combat.isFever?new Color(1,.96f,.78f):new Color(1,1,1,combat.invulnerable>0?.82f+.18f*Mathf.Cos(motionClock*18):1);
        Vector2 stretch=moving?new Vector2(1+Mathf.Sin(heroPhase*2)*.022f,1-Mathf.Sin(heroPhase*2)*.026f):new Vector2(1,1+Mathf.Sin(motionClock*3.1f)*.008f);
        if(combat.attackLead>0)stretch=new Vector2(1.05f,.95f);
        else if(combat.punch>.10f)stretch=new Vector2(1.06f,.96f);
        stretch.x*=TurnWidth(heroTurn);
        float height=combat.isFever?144:138;
        // Speed / Finisher Afterimage Trail
        if(combat.isFever||(combat.attackAnimation>0&&combat.comboStep==0)||heroSpeed>135f){
            Vector2 trailDir=combat.attackAnimation>0?combat.direction:(combat.moveVelocity.sqrMagnitude>1f?combat.moveVelocity.normalized:combat.direction);
            Color ghostCol=combat.isFever?new Color(1f,.78f,.22f,.24f):new Color(.32f,.85f,.95f,.20f);
            MotionSprite(heroMotionAtlas,heroPivots,feet-trailDir*14f,frame,heroLeft,height,ghostCol,heroTilt,stretch);
        }
        MotionSprite(heroMotionAtlas,heroPivots,feet,frame,heroLeft,height,tint,heroTilt,stretch);
        if(combat.punch>0){
            bool heavy=combat.comboStep==0||combat.isFever;
            float progress=Mathf.Clamp01(1-combat.punch/.19f);float a=Mathf.Atan2(combat.direction.y,combat.direction.x);
            float radius=(heavy?76:62)+progress*(heavy?34:24);Vector2 center=heroRendered-new Vector2(0,43);
            Color arcColor=heavy?new Color(1,.74f,.18f,(1-progress)*.95f):gearTier>=4?new Color(.22f,.88f,1f,(1-progress)*.9f):new Color(1,.36f,.12f,(1-progress)*.9f);
            for(int i=0;i<17;i++){float p=i/16f;float angle=a-1.05f+p*2.1f;float next=angle+.13f;
                Vector2 p1=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                Vector2 p2=center+new Vector2(Mathf.Cos(next),Mathf.Sin(next))*radius;
                float taper=Mathf.Sin(p*Mathf.PI)*(1-progress);
                Line(p1,p2,(heavy?15:11)*taper+1,arcColor);Line(p1-new Vector2(0,2),p2-new Vector2(0,2),3.5f*taper,new Color(1,.96f,.74f,1-progress));
            }
        }
    }
    void Enemy(Fighter e){
        ActorMotion m;bool cached=actorMotion.TryGetValue(e.id,out m);Vector2 feet=cached?m.rendered:e.pos;
        float phase=cached?m.phase:0,spawn=cached?EaseOut(m.spawn):1;
        bool reacting=e.reaction>0||e.velocity.sqrMagnitude>28*28;bool left=cached?m.left:e.facing<0;float tilt=cached?m.tilt:0;
        float height=e.boss?181:e.kind==1?146:131,lift=e.lift;
        float bob=reacting||e.windup>0||e.dash>0?0:Mathf.Sin(phase*2)*.9f;
        Ellipse(feet+new Vector2(0,2),(e.boss?69:48)-lift*.25f,12-lift*.10f,new Color(0,0,0,(.20f-lift*.002f)*spawn));
        if(e.enraged)Ellipse(feet+new Vector2(0,1),76,16,new Color(1,.22f,.12f,.28f));
        Color tint=e.flash>0?new Color(1,.76f,.64f,spawn):e.enraged?new Color(1,.82f,.76f,spawn):new Color(1,1,1,spawn);
        float squash=e.windup>0?Mathf.Sin(Mathf.Clamp01(e.windup/.8f)*Mathf.PI)*.035f:0;
        float turn=cached?TurnWidth(m.turn):1;
        if(rosterAtlas!=null&&rosterPivots!=null){
            int pose=reacting?3:(e.windup>0||e.dash>0)?2:cached&&m.speed>10?(Mathf.FloorToInt(phase/Mathf.PI)&1):0;
            MotionSprite(rosterAtlas,rosterPivots,feet-new Vector2(0,lift+bob),e.appearance*4+pose,left,height,tint,tilt,new Vector2(turn,1-squash));
        }else if(reacting){
            float landing=e.airDuration>0&&e.airElapsed>=e.airDuration?Mathf.Sin(Mathf.Clamp01(e.reaction/.14f)*Mathf.PI)*.045f:0;
            LegacyReaction(feet-new Vector2(0,lift),e.kind==0?5:7,left,height,tint,tilt,new Vector2((1+landing)*turn,1-landing));
        }else if(enemyMotionAtlas!=null){
            int cycle=cached&&m.speed<10?1:Mathf.FloorToInt(phase/(Mathf.PI*2)*8)%8;
            int frame=e.kind==0?cycle:BruiserFrames[cycle];
            MotionSprite(enemyMotionAtlas,enemyPivots,feet-new Vector2(0,bob),frame,left,height,tint,tilt,new Vector2((1+squash)*turn,1-squash));
        }else DrawSprite(feet-new Vector2(0,bob),e.kind==0?4:6,left,height,tint,tilt);
        if(e.hp<e.maxHp||e.boss){float w=e.boss?88:42;float y=feet.y-height*.86f;Panel(feet.x-w*.5f-1,y-1,w+2,8,Ink);Panel(feet.x-w*.5f,y,w*Mathf.Clamp01(e.hp/(float)e.maxHp),6,e.boss?Coral:Mint);}
        if(e.windup>0){CircleAt(feet-new Vector2(0,height+5),13,Coral);Text("!",feet.x-16,feet.y-height-20,32,32,20,Cream,TextAnchor.MiddleCenter,true);}
    }

    void DrawFallenActors(){
        foreach(ActorMotion m in actorMotion.Values){
            if(m.death<0||m.death>.85f)continue;
            float progress=m.death/.85f,air=0;
            if(m.fighter.airDuration>0){float t=Mathf.Clamp01((m.fighter.airElapsed+m.death)/m.fighter.airDuration);air=m.fighter.airPeak*4*t*(1-t);}
            float alpha=progress<.58f?1:1-EaseOut((progress-.58f)/.42f);
            float rotation=(m.left?1:-1)*EaseOut(Mathf.Clamp01(m.death/.48f))*53;
            if(rosterAtlas!=null&&rosterPivots!=null)MotionSprite(rosterAtlas,rosterPivots,m.rendered-new Vector2(0,air),m.fighter.appearance*4+3,m.left,m.fighter.boss?181:m.fighter.kind==1?146:131,new Color(1,1,1,alpha),rotation,Vector2.one);
            else LegacyReaction(m.rendered-new Vector2(0,air),m.fighter.kind==0?5:7,m.left,m.fighter.boss?181:m.fighter.kind==1?146:131,new Color(1,1,1,alpha),rotation,Vector2.one);
        }
    }
    void DrawAfterimagesAndDust(){
        foreach(Shockwave sw in combat.shockwaves){
            float a=Mathf.Clamp01(sw.life/sw.maxLife);
            Vector2 center=ProjectPoint(sw.pos,0.15f);
            Ring(center,sw.radius,4.5f*a+.8f,new Color(sw.color.r,sw.color.g,sw.color.b,a*.75f));
        }
        if(world3D==null){
            foreach(CoinDrop c in combat.coins){
                Vector2 p=c.pos-new Vector2(0,c.height);
                Ellipse(c.pos+new Vector2(0,2),13,5,new Color(0,0,0,.22f));
                CircleAt(p,8.5f,new Color(.95f,.58f,.08f));
                CircleAt(p-new Vector2(.8f,1f),6.2f,Gold);
                Box(p.x-1.5f,p.y-4f,2.5f,6f,Cream);
            }
        }
        foreach(Spark spark in sparks){
            float alpha=spark.life/spark.total;
            Vector2 sp=ProjectPoint(spark.pos,0.8f);
            Line(sp,sp-spark.velocity.normalized*spark.size*2,spark.size*alpha,new Color(spark.color.r,spark.color.g,spark.color.b,alpha));
        }
    }

    void DrawCombatText(){
        int index=0;
        foreach(Pop p in pops){
            if(p.text=="제압"||p.text=="퍽!"||p.text=="−1"||p.text=="+1"||p.text=="골목 대장 등장"||p.text=="전성기의 한 방"||p.text.Length==0)continue;
            float t=1-p.life;float a=Mathf.Clamp01(p.life*4);int size=p.style==4?20:p.style==0?15:p.text=="크리티컬!"?24:21;
            Color c=p.style==4?Coral:p.style==2?Mint:p.style==0?Cream:p.text=="크리티컬!"?new Color(1,.55f,.15f):new Color(1,.83f,.36f);c.a=a;
            Vector2 pos=p.pos+new Vector2((index++%3-1)*7,-54-(1-p.life)*24);
            float scale=1+Mathf.Sin(Mathf.Clamp01(t*4)*Mathf.PI)*.12f;
            Matrix4x4 old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(new Vector3(pos.x,pos.y,0),Quaternion.Euler(0,0,p.style==0?0:-4),new Vector3(scale,scale,1));
            Outlined(p.text,-110,-24,220,48,size,c,TextAnchor.MiddleCenter,p.style!=0);GUI.matrix=old;
        }
    }
    void DrawScreenTransition(){
        if(specialFlash>0)Box(0,0,ViewWidth,ViewHeight,new Color(1,.90f,.65f,specialFlash*.9f));
        if(choiceFlash>0)Box(0,0,ViewWidth,ViewHeight,new Color(1,.92f,.70f,choiceFlash*.65f));
        if(menuAge<.35f)Box(0,0,ViewWidth,ViewHeight,new Color(Ink.r,Ink.g,Ink.b,1-EaseOut(menuAge/.35f)));
    }
}
