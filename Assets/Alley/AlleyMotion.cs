using System;
using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public partial class AlleyGame {
    [Serializable] public class FramePivot { public float x=.5f,y=.12f,u,v,w=.25f,h=.25f; }
    [Serializable] public class PivotSheet { public FramePivot[] frames; }
    sealed class ActorMotion {
        public Vector2 rendered,last;public float phase,spawn,death=-1,tilt,speed;
        public Fighter fighter;public bool left;
    }
    sealed class Spark { public Vector2 pos,velocity;public float life,total;public Color color;public float size; }
    readonly Dictionary<int,ActorMotion> actorMotion=new Dictionary<int,ActorMotion>();
    readonly List<Spark> sparks=new List<Spark>();
    Texture2D heroMotionAtlas,enemyMotionAtlas,abilityAtlas;
    PivotSheet heroPivots,enemyPivots;
    Vector2 heroRendered,heroLast;float heroPhase,heroSpeed,heroTilt,modalAge,menuAge,specialFlash,bossBanner;
    float shownHealth=100,healthTrail=100,shownCharge=100,shownXp,choiceFlash;
    bool heroLeft;RunState shownState;bool readyPresentation;
    void LoadPresentation(){
        heroMotionAtlas=Resources.Load<Texture2D>("HeroMotion");enemyMotionAtlas=Resources.Load<Texture2D>("EnemyMotion");abilityAtlas=Resources.Load<Texture2D>("AbilityIcons");
        TextAsset pivots=Resources.Load<TextAsset>("HeroMotionPivots");if(pivots!=null)heroPivots=JsonUtility.FromJson<PivotSheet>(pivots.text);
        pivots=Resources.Load<TextAsset>("EnemyMotionPivots");if(pivots!=null)enemyPivots=JsonUtility.FromJson<PivotSheet>(pivots.text);
    }
    void ResetMotion(){
        actorMotion.Clear();sparks.Clear();heroRendered=heroLast=new Vector2(270,535);heroSpeed=heroPhase=heroTilt=0;heroLeft=false;
        shownHealth=healthTrail=100;shownCharge=100;shownXp=0;menuAge=modalAge=0;shownState=RunState.Ready;readyPresentation=true;
    }
    void AdvancePresentation(float dt){
        dt=Mathf.Min(dt,.06f);menuAge+=dt;
        if(shownState!=combat.state){shownState=combat.state;modalAge=0;if(shownState==RunState.Choosing)choiceFlash=.20f;}
        modalAge+=dt;specialFlash=Mathf.Max(0,specialFlash-dt);choiceFlash=Mathf.Max(0,choiceFlash-dt);bossBanner=Mathf.Max(0,bossBanner-dt);
        shownHealth=Mathf.Lerp(shownHealth,combat.health,1-Mathf.Exp(-dt*16));
        healthTrail=Mathf.MoveTowards(healthTrail,shownHealth,dt*23);
        shownCharge=Mathf.Lerp(shownCharge,combat.charge,1-Mathf.Exp(-dt*11));
        shownXp=Mathf.Lerp(shownXp,combat.xp/(float)combat.nextXp,1-Mathf.Exp(-dt*10));
        for(int i=sparks.Count-1;i>=0;i--){Spark s=sparks[i];s.life-=dt;s.pos+=s.velocity*dt;s.velocity*=Mathf.Exp(-dt*5);s.velocity.y+=dt*80;if(s.life<=0)sparks.RemoveAt(i);}
    }
    void UpdateMotion(float dt,bool playing){
        dt=Mathf.Min(dt,.06f);if(!readyPresentation)return;
        float smooth=1-Mathf.Exp(-dt*26);
        float distance=(combat.player-heroLast).magnitude;heroLast=combat.player;
        heroRendered=Vector2.Lerp(heroRendered,combat.player,smooth);
        heroSpeed=Mathf.Lerp(heroSpeed,playing?distance/Mathf.Max(.001f,dt):0,1-Mathf.Exp(-dt*15));
        if(playing&&combat.freeze<=0)heroPhase+=distance/88f*Mathf.PI*2;
        float targetTilt=playing?Mathf.Clamp(combat.moveVelocity.x/172f,-1,1)*-4:0;
        heroTilt=Mathf.Lerp(heroTilt,targetTilt,smooth);
        if(combat.direction.x>.18f)heroLeft=false;else if(combat.direction.x<-.18f)heroLeft=true;
        foreach(Fighter e in combat.enemies){
            ActorMotion m;
            if(!actorMotion.TryGetValue(e.id,out m)){m=new ActorMotion{rendered=e.pos,last=e.pos,fighter=e,left=e.facing<0};actorMotion.Add(e.id,m);}
            float travel=(e.pos-m.last).magnitude;m.last=e.pos;m.fighter=e;
            m.rendered=Vector2.Lerp(m.rendered,e.pos,smooth);m.spawn=Mathf.Min(1,m.spawn+dt*5);
            m.speed=Mathf.Lerp(m.speed,playing?travel/Mathf.Max(.001f,dt):0,smooth);
            if(playing&&combat.freeze<=0&&e.stun<=0&&e.velocity.sqrMagnitude<1000)m.phase+=travel/72f*Mathf.PI*2;
            float lean=e.velocity.sqrMagnitude>4000?Mathf.Clamp(e.velocity.x*.028f,-24,24):e.dash>0?e.aim.x*-13:e.windup>0?-e.facing*8:e.facing*-Mathf.Min(4,m.speed*.045f);
            m.tilt=Mathf.Lerp(m.tilt,lean,1-Mathf.Exp(-dt*18));
            if(e.windup<=0&&e.dash<=0){if(combat.player.x-e.pos.x>14)m.left=false;else if(combat.player.x-e.pos.x<-14)m.left=true;}
        }
        foreach(ActorMotion m in actorMotion.Values){if(m.fighter.hp<=0){if(m.death<0)m.death=0;m.death+=dt;}}
    }
    void AddImpact(CombatEvent e){
        if(e.style==3&&e.text=="전성기의 한 방")specialFlash=.18f;
        if(e.text=="골목 대장 등장")bossBanner=2.3f;
        if(e.style==0&&e.text=="제압")return;
        if(e.text.StartsWith("벽꿍")||e.text.StartsWith("연쇄")||e.text=="퍽!"||e.style==4){
            Color c=e.style==2?Mint:e.style==4?Coral:new Color(1,.76f,.33f);
            int count=e.text=="퍽!"?6:10;
            for(int i=0;i<count;i++){float a=UnityEngine.Random.value*Mathf.PI*2;float speed=UnityEngine.Random.Range(45,150);sparks.Add(new Spark{pos=e.pos-new Vector2(0,45),velocity=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*speed,life=.30f,total=.30f,color=c,size=UnityEngine.Random.Range(2,5)});}
        }
    }
    float EaseOut(float t){t=Mathf.Clamp01(t);return 1-Mathf.Pow(1-t,3);}
    float PopScale(float t){t=Mathf.Clamp01(t);return 1+Mathf.Sin(t*Mathf.PI)*.09f;}
    void Ellipse(Vector2 p,float width,float height,Color c){GUI.color=c;GUI.DrawTexture(new Rect(p.x-width*.5f,p.y-height*.5f,width,height),disc);GUI.color=Color.white;}
    void MotionSprite(Texture2D sheet,PivotSheet pivots,Vector2 feet,int frame,bool left,float height,Color tint,float tilt,Vector2 stretch){
        if(sheet==null){DrawSprite(feet,0,left,height,tint,tilt);return;}
        frame=Mathf.Clamp(frame,0,15);float px=.5f,py=.12f;Rect uv=new Rect(frame%4*.25f,(3-frame/4)*.25f,.25f,.25f);
        if(pivots!=null&&pivots.frames!=null&&frame<pivots.frames.Length){FramePivot pivot=pivots.frames[frame];px=pivot.x;py=pivot.y;uv=new Rect(pivot.u,pivot.v,pivot.w,pivot.h);}
        float drawW=height*uv.width*4,drawH=height*uv.height*4;
        Matrix4x4 old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(new Vector3(feet.x,feet.y,0),Quaternion.Euler(0,0,tilt),new Vector3((left?-1:1)*stretch.x,stretch.y,1));
        GUI.color=tint;GUI.DrawTextureWithTexCoords(new Rect(-px*drawW,-(1-py)*drawH,drawW,drawH),sheet,uv);GUI.color=Color.white;GUI.matrix=old;
    }
    int AttackFrame(){
        float t=.44f-combat.attackAnimation;
        if(t<.025f)return 8;if(t<.058f)return 9;if(t<.083f)return 10;if(t<.103f)return 11;
        if(t<.16f)return 12;if(t<.235f)return 13;if(t<.33f)return 14;return 15;
    }
    void Hero(){
        Vector2 feet=heroRendered;bool moving=heroSpeed>14;
        float bob=moving?Mathf.Sin(heroPhase*2)*1.65f:Mathf.Sin(pulse*2.8f)*.7f;
        Ellipse(feet+new Vector2(0,2),moving?49:51,13,new Color(0,0,0,.22f));
        Ring(feet+new Vector2(0,1),25,1.6f,new Color(.30f,.93f,.75f,.42f));
        int frame=combat.attackAnimation>0?AttackFrame():moving?(Mathf.FloorToInt(heroPhase/(Mathf.PI*2)*8)%8):8;
        float attackT=.44f-combat.attackAnimation;float thrust=combat.attackAnimation>0?Mathf.Sin(Mathf.Clamp01((attackT-.06f)/.22f)*Mathf.PI)*6:0;
        feet+=combat.direction*thrust-new Vector2(0,bob);
        Color tint=combat.invulnerable>0&&Mathf.Sin(pulse*40)>0?new Color(1,.73f,.65f,.8f):Color.white;
        Vector2 stretch=moving?new Vector2(1+Mathf.Sin(heroPhase*2)*.012f,1-Mathf.Sin(heroPhase*2)*.018f):new Vector2(1,1+Mathf.Sin(pulse*2.8f)*.006f);
        if(combat.attackLead>0)stretch=new Vector2(1.03f,.97f);
        MotionSprite(heroMotionAtlas,heroPivots,feet,frame,heroLeft,138,tint,heroTilt,stretch);
        if(combat.punch>0){
            float progress=Mathf.Clamp01(1-combat.punch/.19f);float a=Mathf.Atan2(combat.direction.y,combat.direction.x);
            float radius=60+progress*23;Vector2 center=heroRendered-new Vector2(0,43);
            for(int i=0;i<17;i++){float p=i/16f;float angle=a-1.05f+p*2.1f;float next=angle+.13f;
                Vector2 p1=center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;
                Vector2 p2=center+new Vector2(Mathf.Cos(next),Mathf.Sin(next))*radius;
                float taper=Mathf.Sin(p*Mathf.PI)*(1-progress);
                Line(p1,p2,11*taper+1,new Color(1,.36f,.12f,(1-progress)*.9f));Line(p1-new Vector2(0,2),p2-new Vector2(0,2),3*taper,new Color(1,.96f,.74f,1-progress));
            }
        }
    }
    void Enemy(Fighter e){
        ActorMotion m;bool cached=actorMotion.TryGetValue(e.id,out m);Vector2 feet=cached?m.rendered:e.pos;
        float phase=cached?m.phase:0,spawn=cached?EaseOut(m.spawn):1;
        bool knocked=e.velocity.sqrMagnitude>4000;bool left=cached?m.left:e.facing<0;float tilt=cached?m.tilt:0;
        float height=e.boss?181:e.kind==1?146:131;
        float lift=knocked?Mathf.Min(15,e.velocity.magnitude*.022f):0;
        float bob=knocked||e.windup>0?0:Mathf.Sin(phase*2)*1.5f;
        Ellipse(feet+new Vector2(0,2),e.boss?69:48,knocked?9:12,new Color(0,0,0,.20f*spawn));
        Color tint=e.flash>0?new Color(1,.64f,.46f,spawn):new Color(1,1,1,spawn);
        float squash=e.windup>0?Mathf.Sin(Mathf.Clamp01(e.windup/.8f)*Mathf.PI)*.055f:0;
        if(knocked||e.stun>0&&e.flash>0){
            int cell=e.kind==0?5:7;
            Matrix4x4 old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(new Vector3(feet.x,feet.y,0),Quaternion.identity,new Vector3(1.035f,.97f,1));
            DrawSprite(new Vector2(0,-lift),cell,left,height,tint,tilt);GUI.matrix=old;
        }else if(enemyMotionAtlas!=null){
            int cycle=Mathf.FloorToInt(phase/(Mathf.PI*2)*8)%8;
            int[] bruiserFrames={8,9,10,11,13,15,10,9};int frame=e.kind==0?cycle:bruiserFrames[cycle];
            MotionSprite(enemyMotionAtlas,enemyPivots,feet-new Vector2(0,bob),frame,left,height,tint,tilt,new Vector2((1+squash)*(.7f+.3f*spawn),(1-squash)*(.7f+.3f*spawn)));
        }else DrawSprite(feet-new Vector2(0,bob),e.kind==0?4:6,left,height,tint,tilt);
        if(e.hp<e.maxHp||e.boss){float w=e.boss?84:42;float y=feet.y-height*.86f;Panel(feet.x-w*.5f-1,y-1,w+2,8,Ink);Panel(feet.x-w*.5f,y,w*Mathf.Clamp01(e.hp/(float)e.maxHp),6,e.boss?Coral:Mint);}
        if(e.windup>0){CircleAt(feet-new Vector2(0,height+5),13,Coral);Text("!",feet.x-16,feet.y-height-20,32,32,20,Cream,TextAnchor.MiddleCenter,true);}
    }
    void DrawAfterimagesAndDust(){
        foreach(ActorMotion m in actorMotion.Values){
            if(m.death<0||m.death>.55f)continue;
            float progress=m.death/.55f;Vector2 feet=m.rendered;int cell=m.fighter.kind==0?5:7;
            DrawSprite(feet+new Vector2((m.left?-1:1)*progress*14,progress*8),cell,m.left,m.fighter.boss?171:129,new Color(1,1,1,1-EaseOut(progress)),(m.left?1:-1)*EaseOut(progress)*56);
        }
        foreach(Spark s in sparks){float a=s.life/s.total;Line(s.pos,s.pos-s.velocity.normalized*s.size*2,s.size*a,new Color(s.color.r,s.color.g,s.color.b,a));}
    }
    void DrawCombatText(){
        int index=0;
        foreach(Pop p in pops){
            if(p.text=="제압"||p.text=="골목 대장 등장"||p.text=="전성기의 한 방"||p.text.Length==0)continue;
            float t=1-p.life;float a=Mathf.Clamp01(p.life*4);int size=p.style==4?22:p.style==0?17:25;
            Color c=p.style==4?Coral:p.style==2?Mint:p.style==0?Cream:new Color(1,.83f,.36f);c.a=a;
            Vector2 pos=p.pos+new Vector2((index++%3-1)*9,-78);
            float scale=1+Mathf.Sin(Mathf.Clamp01(t*4)*Mathf.PI)*.14f;
            Matrix4x4 old=GUI.matrix;GUI.matrix=old*Matrix4x4.TRS(new Vector3(pos.x,pos.y,0),Quaternion.Euler(0,0,p.style==0?0:-6),new Vector3(scale,scale,1));
            Outlined(p.text,-110,-24,220,48,size,c,TextAnchor.MiddleCenter,p.style!=0);GUI.matrix=old;
        }
    }
    void DrawScreenTransition(){
        if(specialFlash>0)Box(0,173,540,629,new Color(1,.90f,.65f,specialFlash*.9f));
        if(choiceFlash>0)Box(0,173,540,787,new Color(1,.92f,.70f,choiceFlash*.65f));
        if(menuAge<.35f)Box(0,0,540,960,new Color(Ink.r,Ink.g,Ink.b,1-EaseOut(menuAge/.35f)));
    }
}
