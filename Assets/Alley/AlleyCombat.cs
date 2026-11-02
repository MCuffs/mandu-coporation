using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondFistGame {
    public enum RunState { Ready, Fighting, Choosing, Won, Lost }
    public sealed class Fighter {
        public int id,kind,hp,maxHp,appearance,facing=1;
        public Vector2 pos,velocity,aim,locomotion;
        public float attack,windup,stun,flash,contact,dash,wallCooldown,recovery,reaction;
        public float airElapsed,airDuration,airPeak;
        public bool boss { get { return kind==2; } }
        public float lift { get { if(airDuration<=0)return 0;float t=Mathf.Clamp01(airElapsed/airDuration);return airPeak*4*t*(1-t); } }
    }
    public struct CombatEvent {
        public string text;public Vector2 pos;public int style;
        public CombatEvent(string text,Vector2 pos,int style){this.text=text;this.pos=pos;this.style=style;}
    }
    public sealed class AlleyCombat {
        public RunState state=RunState.Ready;
        public readonly List<Fighter> enemies=new List<Fighter>();
        public readonly List<CombatEvent> events=new List<CombatEvent>();
        public Vector2 player=new Vector2(270,535),direction=Vector2.right,moveVelocity,playerImpulse;
        public float health=100,charge=100,elapsed,attackCooldown,invulnerable,punch,freeze,attackLead,attackAnimation,playerReaction;
        public int kills,hits,wallHits,chainHits,level=1,xp,nextXp=3,stage,bestChain,wallSkill,chainSkill,stunSkill;
        public int targetKills { get { return StageCatalog.Get(stage).target; } }
        public bool bossSpawned,spawning=true;
        public float spawnClock=.25f,comboClock;
        public int chain;
        Fighter pendingTarget,trackedTarget;
        Vector2 strikeDirection=Vector2.right;
        int nextId,pendingChoices;
        public const float Left=65,Right=475,Top=295,Bottom=745;
        public Rect arena=new Rect(Left,Top,Right-Left,Bottom-Top);
        public AlleyCombat(int stage=0){this.stage=stage;}
        public void SetArena(Rect next){
            if(next.width<160||next.height<200)throw new ArgumentException("Arena is too small");
            Rect previous=arena;
            player=Remap(player,previous,next);
            foreach(Fighter e in enemies)e.pos=Remap(e.pos,previous,next);
            arena=next;ConstrainPlayer();
        }
        static Vector2 Remap(Vector2 p,Rect from,Rect to){return new Vector2(to.xMin+(p.x-from.xMin)/from.width*to.width,to.yMin+(p.y-from.yMin)/from.height*to.height);}
        public void Start(){state=RunState.Fighting;}
        public Fighter AddEnemy(Vector2 pos,int kind=0){
            int hp=kind==2?18+stage*3:kind==1?5:3;
            Fighter e=new Fighter{id=++nextId,kind=kind,hp=hp,maxHp=hp,appearance=kind==2?(stage==0?1:stage==1?0:stage==2?2:3):kind==1?1:stage==0?2:stage==1?0:(nextId%2==0?0:2),pos=pos,attack=.6f+(nextId%4)*.12f};enemies.Add(e);return e;
        }
        public void Choose(int skill){
            if(state!=RunState.Choosing)return;
            if(skill==0)wallSkill++;else if(skill==1)chainSkill++;else stunSkill++;
            pendingChoices=Mathf.Max(0,pendingChoices-1);state=pendingChoices>0?RunState.Choosing:RunState.Fighting;
            events.Add(new CombatEvent(skill==0?"벽꿍 강화!":skill==1?"충돌 전파!":"연쇄 기절!",player,2));
        }
        public void Tick(float dt,Vector2 movement,bool special){
            if(state!=RunState.Fighting||dt<=0)return;
            float remaining=Mathf.Min(dt,.12f);bool trigger=special;
            // Small simulation steps keep steering, collision and hit-stop consistent at 20/30/60+ FPS.
            while(remaining>.000001f&&state==RunState.Fighting){
                if(freeze>0){float held=Mathf.Min(freeze,remaining);freeze-=held;remaining-=held;continue;}
                float step=Mathf.Min(remaining,1/120f);Simulate(step,movement,trigger);trigger=false;remaining-=step;
            }
        }
        void Simulate(float dt,Vector2 movement,bool special){
            elapsed+=dt;attackCooldown-=dt;invulnerable-=dt;playerReaction=Mathf.Max(0,playerReaction-dt);
            punch=Mathf.Max(0,punch-dt);attackAnimation=Mathf.Max(0,attackAnimation-dt);comboClock-=dt;if(comboClock<=0)chain=0;
            movement=Vector2.ClampMagnitude(movement,1);if(movement.magnitude<.10f)movement=Vector2.zero;
            // Retreat cancels preparation instead of forcing a backwards turn and a skating punch.
            if(attackLead>0&&movement.sqrMagnitude>.16f&&Vector2.Dot(movement.normalized,strikeDirection)<-.35f){attackLead=0;pendingTarget=null;attackAnimation=Mathf.Min(attackAnimation,.12f);attackCooldown=.14f;}
            float brace=attackLead>0?.62f:attackAnimation>.22f?.28f:1;
            float acceleration=movement.sqrMagnitude>0?1250:1900;
            moveVelocity=Vector2.MoveTowards(moveVelocity,movement*(172*brace),acceleration*dt);
            player+=(moveVelocity+playerImpulse)*dt;playerImpulse*=Mathf.Exp(-12*dt);ConstrainPlayer();
            if(movement.sqrMagnitude>.03f&&attackAnimation<=0)direction=movement.normalized;
            if(special&&charge>=100)Special();
            if(attackLead>0){
                attackLead-=dt;
                if(attackLead<=0){if(pendingTarget!=null&&pendingTarget.hp>0)ResolvePunch(strikeDirection);pendingTarget=null;}
            }
            if(attackCooldown<=0&&attackLead<=0&&attackAnimation<=.08f&&playerReaction<=0){
                Fighter target=AcquireTarget(movement);
                if(target!=null){trackedTarget=pendingTarget=target;attackLead=.10f;attackAnimation=.44f;attackCooldown=.52f;strikeDirection=(target.pos-player).normalized;if(strikeDirection.sqrMagnitude<.01f)strikeDirection=direction;direction=strikeDirection;}
            }
            Spawn(dt);
            foreach(Fighter e in enemies)if(e.hp>0)MoveEnemy(e,dt);
            ResolveContacts(dt);
            for(int i=enemies.Count-1;i>=0;i--)if(enemies[i].hp<=0){
                Fighter e=enemies[i];enemies.RemoveAt(i);kills++;xp++;events.Add(new CombatEvent("제압",e.pos,0));
                if(e.boss){state=RunState.Won;events.Add(new CombatEvent("BOSS DOWN",e.pos,3));}
            }
            if(state==RunState.Fighting&&xp>=nextXp&&level<5){xp-=nextXp;level++;nextXp=level+2;pendingChoices++;state=RunState.Choosing;}
            if(health<=0)state=RunState.Lost;
        }
        Fighter AcquireTarget(Vector2 movement){
            Fighter best=null;float score=112;
            foreach(Fighter e in enemies){
                if(e.hp<=0)continue;Vector2 delta=e.pos-player;float distance=delta.magnitude;if(distance>104)continue;
                if(movement.sqrMagnitude>.09f&&distance>52&&Vector2.Dot(movement.normalized,delta.normalized)<-.1f)continue;
                float candidate=distance*(e==trackedTarget?.80f:1);if(candidate<score){best=e;score=candidate;}
            }
            return best;
        }
        void Spawn(float dt){
            spawnClock-=dt;if(!spawning||spawnClock>0||bossSpawned||enemies.Count>=9)return;
            if(kills>=targetKills){AddEnemy(new Vector2(arena.center.x,arena.yMin+35),2);bossSpawned=true;events.Add(new CombatEvent("골목 대장 등장",new Vector2(arena.center.x,arena.yMin+105),3));return;}
            float side=UnityEngine.Random.value<.5f?-1:1;
            float x=Mathf.Clamp(player.x+side*Mathf.Min(310,arena.width*.5f),arena.xMin+12,arena.xMax-12);
            float y=Mathf.Clamp(player.y+UnityEngine.Random.Range(-220f,220f),arena.yMin+35,arena.yMax-25);
            AddEnemy(new Vector2(x,y),kills>3&&UnityEngine.Random.value<StageCatalog.Get(stage).heavyChance?1:0);spawnClock=Mathf.Max(.65f,1.25f-kills*.025f);
        }
        void MoveEnemy(Fighter e,float dt){
            e.flash=Mathf.Max(0,e.flash-dt);e.stun-=dt;e.contact-=dt;e.wallCooldown-=dt;e.attack-=dt;e.recovery=Mathf.Max(0,e.recovery-dt);e.reaction=Mathf.Max(0,e.reaction-dt);
            if(e.airDuration>0)e.airElapsed=Mathf.Min(e.airDuration,e.airElapsed+dt);
            if(e.windup<=0&&e.dash<=0){if(player.x-e.pos.x>18)e.facing=1;else if(player.x-e.pos.x<-18)e.facing=-1;}
            if(e.velocity.sqrMagnitude>28*28){
                e.windup=0;e.locomotion=Vector2.zero;
                Vector2 desired=e.pos+e.velocity*dt;bool hitX=desired.x<arena.xMin||desired.x>arena.xMax,hitY=desired.y<arena.yMin||desired.y>arena.yMax;
                e.pos=ClampEnemy(desired);
                if((hitX||hitY)&&e.wallCooldown<=0){
                    e.wallCooldown=.30f;wallHits++;CountChain();Damage(e,1+wallSkill,"벽꿍!",1);
                    if(hitX)e.velocity.x*=-.16f;if(hitY)e.velocity.y*=-.16f;
                    e.stun=Mathf.Max(e.stun,.26f+stunSkill*.25f);e.reaction=Mathf.Max(e.reaction,.22f);freeze=Mathf.Max(freeze,.014f);
                }
                float drag=e.airElapsed<e.airDuration?3.6f:8.5f;e.velocity*=Mathf.Exp(-drag*dt);return;
            }
            e.velocity=Vector2.zero;
            if(e.stun>0||e.reaction>.10f){e.locomotion=Vector2.MoveTowards(e.locomotion,Vector2.zero,600*dt);return;}
            Vector2 delta=player-e.pos;float distance=delta.magnitude;
            if(e.dash>0){
                float started=.38f-e.dash;float ramp=Mathf.Min(Mathf.Clamp01(started/.055f),Mathf.Clamp01(e.dash/.10f));
                e.locomotion=e.aim*(480*ramp);e.pos=ClampEnemy(e.pos+e.locomotion*dt);e.dash-=dt;
                if((player-e.pos).magnitude<52)Hurt(e.boss?18:12,e.pos);
                if(e.dash<=0){e.recovery=.32f;e.locomotion=e.aim*90;}return;
            }
            if(e.windup>0){
                e.locomotion=Vector2.MoveTowards(e.locomotion,Vector2.zero,900*dt);e.pos=ClampEnemy(e.pos+e.locomotion*dt);e.windup-=dt;
                if(e.windup<=0){if(e.kind>=1){e.dash=.38f;e.attack=e.boss?2.8f:2.1f;}else{if(distance<(e.appearance==0?111:83))Hurt(e.appearance==0?10:8,e.pos);e.attack=e.appearance==0?1.8f:1.5f;e.recovery=.22f;}}
                return;
            }
            if(e.recovery>0){e.locomotion=Vector2.MoveTowards(e.locomotion,Vector2.zero,750*dt);e.pos=ClampEnemy(e.pos+e.locomotion*dt);return;}
            if(e.kind>=1&&e.attack<=0&&(e.boss||distance<240)&&ChargeSlotAvailable(e)){e.windup=e.boss?.95f:.8f;e.aim=delta.normalized;e.facing=e.aim.x>=0?1:-1;return;}
            if(e.kind==0&&distance<(e.appearance==0?104:76)&&e.attack<=0&&MeleeSlotsAvailable(e)){e.windup=.56f;e.aim=delta.normalized;return;}
            Vector2 desiredVelocity=Vector2.zero;
            if(distance>54){
                float a=e.id*2.399963f;float laneRadius=e.boss?119:e.kind==1?94:e.appearance==0?98:70;Vector2 lane=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*laneRadius;
                Vector2 target=player+lane*Mathf.Clamp01((190-distance)/100f);
                Vector2 steering=(target-e.pos).normalized;
                foreach(Fighter other in enemies){if(other==e||other.hp<=0)continue;Vector2 away=e.pos-other.pos;float d=away.magnitude;float clearance=BodyRadius(e)+BodyRadius(other)+24;if(d>.01f&&d<clearance)steering+=away/d*(1-d/clearance)*1.15f;}
                float speed=e.boss?45:e.kind==1?55:72+stage*4;
                desiredVelocity=Vector2.ClampMagnitude(steering,1)*speed;
            }
            e.locomotion=Vector2.MoveTowards(e.locomotion,desiredVelocity,360*dt);e.pos=ClampEnemy(e.pos+e.locomotion*dt);
        }
        static float BodyRadius(Fighter e){return e.boss?38:e.kind==1?30:25;}
        bool ChargeSlotAvailable(Fighter current){foreach(Fighter other in enemies)if(other!=current&&other.kind>=1&&(other.windup>0||other.dash>0))return false;return true;}
        bool MeleeSlotsAvailable(Fighter current){int count=0;foreach(Fighter other in enemies)if(other!=current&&other.kind==0&&other.windup>0)count++;return count<2;}
        void ResolveContacts(float dt){
            for(int i=0;i<enemies.Count;i++)for(int j=i+1;j<enemies.Count;j++){
                Fighter a=enemies[i],b=enemies[j];if(a.hp<=0||b.hp<=0)continue;
                Vector2 delta=b.pos-a.pos;float distance=delta.magnitude;float required=BodyRadius(a)+BodyRadius(b);if(distance>=required)continue;
                Vector2 normal=distance>.01f?delta/distance:Vector2.right;
                Fighter source=a.velocity.sqrMagnitude>b.velocity.sqrMagnitude?a:b;Fighter target=source==a?b:a;
                if(source.velocity.sqrMagnitude>180*180&&source.contact<=0&&target.contact<=0){
                    source.contact=.14f;target.contact=.30f;chainHits++;CountChain();
                    Vector2 impulse=source.velocity.normalized*(420+chainSkill*100);Launch(target,impulse,.27f,9);target.contact=.30f;target.stun=.34f+stunSkill*.45f;source.velocity*=.64f;
                    Damage(target,1+chainSkill,"연쇄!",2);
                }else{
                    float settle=(required-distance)*.5f*(1-Mathf.Exp(-20*dt));
                    if(a.velocity.sqrMagnitude<900&&a.dash<=0)a.pos=ClampEnemy(a.pos-normal*settle);
                    if(b.velocity.sqrMagnitude<900&&b.dash<=0)b.pos=ClampEnemy(b.pos+normal*settle);
                }
            }
        }
        void CountChain(){chain++;bestChain=Mathf.Max(bestChain,chain);comboClock=2;}
        Vector2 ClampEnemy(Vector2 p){return new Vector2(Mathf.Clamp(p.x,arena.xMin,arena.xMax),Mathf.Clamp(p.y,arena.yMin,arena.yMax));}
        void ConstrainPlayer(){player=new Vector2(Mathf.Clamp(player.x,arena.xMin+12,arena.xMax-12),Mathf.Clamp(player.y,arena.yMin+12,arena.yMax-12));if(player.x<=arena.xMin+12||player.x>=arena.xMax-12)moveVelocity.x=0;if(player.y<=arena.yMin+12||player.y>=arena.yMax-12)moveVelocity.y=0;}
        static void Launch(Fighter e,Vector2 impulse,float duration,float peak){e.velocity=impulse;e.locomotion=Vector2.zero;e.airElapsed=0;e.airDuration=duration;e.airPeak=peak;e.reaction=duration+.14f;e.stun=duration+.10f;e.windup=0;e.dash=0;e.recovery=0;}
        public void Attack(Fighter target){Vector2 aim=(target.pos-player).normalized;ResolvePunch(aim.sqrMagnitude>.01f?aim:direction);}
        void ResolvePunch(Vector2 aim){
            direction=aim;punch=.19f;attackCooldown=.43f;hits++;if(attackAnimation<=0)attackAnimation=.34f;
            foreach(Fighter e in enemies){Vector2 delta=e.pos-player;if(e.hp<=0||delta.magnitude>=110||Vector2.Dot(aim,delta.normalized)<=.25f)continue;
                Damage(e,1,"",0);if(e.kind==0||(e.kind==1&&e.windup<=0&&e.dash<=0))Launch(e,aim*650,.30f,12);
            }
            charge=Mathf.Min(100,charge+9);freeze=Mathf.Max(freeze,.009f);events.Add(new CombatEvent("퍽!",player+aim*60,1));
        }
        public bool Special(){
            if(state!=RunState.Fighting||charge<100)return false;
            charge=0;attackLead=0;pendingTarget=null;attackCooldown=Mathf.Max(attackCooldown,.36f);punch=.25f;attackAnimation=.32f;invulnerable=.55f;
            foreach(Fighter e in enemies){Vector2 delta=e.pos-player;if(e.hp<=0||delta.magnitude>185)continue;Vector2 aim=delta.sqrMagnitude>.01f?delta.normalized:Vector2.up;Damage(e,2,"",0);Launch(e,aim*(e.boss?280:860),e.boss?.24f:.42f,e.boss?5:25);}
            freeze=Mathf.Max(freeze,.065f);events.Add(new CombatEvent("전성기의 한 방",player,3));return true;
        }
        void Damage(Fighter e,int amount,string label,int style){if(e.hp<=0)return;e.hp-=amount;e.flash=.10f;charge=Mathf.Min(100,charge+3);events.Add(new CombatEvent(label.Length>0?label:"−"+amount,e.pos,style));}
        void Hurt(int amount,Vector2 origin){
            if(invulnerable>0)return;health=Mathf.Max(0,health-amount);invulnerable=.7f;playerReaction=.18f;
            playerImpulse=(player-origin).normalized*185;attackLead=0;pendingTarget=null;attackAnimation=Mathf.Min(attackAnimation,.12f);attackCooldown=Mathf.Max(attackCooldown,.20f);
            events.Add(new CombatEvent("−"+amount,player,4));
        }
    }
}
