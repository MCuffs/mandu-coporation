using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondFistGame {
    public enum RunState { Ready, Fighting, Choosing, Won, Lost }
    public sealed class CoinDrop {
        public Vector2 pos,velocity;
        public int value=1;
        public float age,height,verticalVelocity;
        public bool magnetized;
    }
    public sealed class Shockwave {
        public Vector2 pos;
        public float radius,maxRadius,life,maxLife;
        public Color color;
    }
    public sealed class AlleyHazard {
        public int id,kind,hp=1;
        public Vector2 pos;
    }
    public sealed class Fighter {
        public int id,kind,hp,maxHp,appearance,facing=1,burstQueue;
        public Vector2 pos,velocity,aim,locomotion;
        public float attack,windup,stun,flash,contact,dash,wallCooldown,recovery,reaction;
        public float airElapsed,airDuration,airPeak;
        public float groggy;
        // School tray enemy: blocks hits that arrive at its front, turns slowly, loses the tray to side or rear hits.
        public bool shield,trayBroken;
        public int trayHits; // frontal hits absorbed; the tray gives way at TrayDurability
        public float turnDelay;
        // School stages: an elite is a bigger, tougher version of a regular enemy (mid-boss); power scales the hits it lands.
        public bool elite;
        public float power=1;
        public bool boss { get { return kind==2; } }
        public bool enraged;
        public float lift { get { if(airDuration<=0)return 0;float t=Mathf.Clamp01(airElapsed/airDuration);return airPeak*4*t*(1-t); } }
    }
    public struct CombatEvent {
        public string text;public Vector2 pos;public int style;
        public CombatEvent(string text,Vector2 pos,int style){this.text=text;this.pos=pos;this.style=style;}
    }
    public sealed class AlleyCombat {
        public RunState state=RunState.Ready;
        public readonly List<Fighter> enemies=new List<Fighter>();
        public readonly List<CoinDrop> coins=new List<CoinDrop>();
        public readonly List<Shockwave> shockwaves=new List<Shockwave>();
        public readonly List<AlleyHazard> hazards=new List<AlleyHazard>();
        public readonly List<CombatEvent> events=new List<CombatEvent>();
        public Vector2 player=new Vector2(270,535),direction=Vector2.right,moveVelocity,playerImpulse;
        public bool hasPartner;
        public int partnerDamage=1;
        public Vector2 partnerPos=new Vector2(225,555),partnerVelocity,partnerDirection=Vector2.right;
        public float partnerCooldown,partnerPunch;
        public float health=100,maxHealth=100,charge=100,elapsed,attackCooldown,invulnerable,punch,freeze,attackLead,attackAnimation,playerReaction;
        public float feverClock,feverDuration=5.5f,hazardSpawnClock=3.5f;
        public bool isFever { get { return feverClock>0; } }
        public int baseDamage=1,collectedCoins,comboStep,revivesUsed;
        // Opt-in equipment rules used by the school prototype; legacy combat retains defaults.
        public float punchKnockbackMultiplier=1;
        public float chargeGainMultiplier=1;
        public bool phoneSpecial;
        public float phoneHeal;
        // School rules: longer run, fewer level-ups, and a boss that only opens up after a body collision.
        public int killTargetOverride,xpCurveScale=1;
        public bool groggyBoss;
        public const int TrayDurability=4;
        // Stage scaling (school): recommended power vs dad's power stretches enemy HP and the damage dad takes.
        public float enemyHpScale=1,incomingDamageScale=1,hasteBonus,eliteKnockback=.6f;
        public int kills,hits,wallHits,chainHits,level=1,xp,nextXp=3,stage,bestChain;
        public int wallSkill,chainSkill,stunSkill,fistSkill,vampSkill,magnetSkill;
        public readonly int[] currentChoices={0,1,2};
        public int targetKills { get { return killTargetOverride>0?killTargetOverride:StageCatalog.Get(stage).target; } }
        public bool bossSpawned,spawning=true;
        public float spawnClock=.25f,comboClock;
        public int chain;
        Fighter pendingTarget,trackedTarget;
        Vector2 strikeDirection=Vector2.right;
        int nextId,pendingChoices;
        public const float Left=65,Right=475,Top=295,Bottom=745;
        public Rect arena=new Rect(Left,Top,Right-Left,Bottom-Top);
        public AlleyCombat(int stage=0){this.stage=stage;RollChoices();}
        public void SetArena(Rect next){
            if(next.width<160||next.height<200)throw new ArgumentException("Arena is too small");
            Rect previous=arena;
            player=Remap(player,previous,next);
            partnerPos=Remap(partnerPos,previous,next);
            foreach(Fighter e in enemies)e.pos=Remap(e.pos,previous,next);
            foreach(CoinDrop c in coins)c.pos=Remap(c.pos,previous,next);
            foreach(AlleyHazard h in hazards)h.pos=Remap(h.pos,previous,next);
            arena=next;ConstrainPlayer();
        }
        static Vector2 Remap(Vector2 p,Rect from,Rect to){return new Vector2(to.xMin+(p.x-from.xMin)/from.width*to.width,to.yMin+(p.y-from.yMin)/from.height*to.height);}
        public void Start(){state=RunState.Fighting;partnerPos=player+new Vector2(-42,18);}
        public Fighter AddEnemy(Vector2 pos,int kind=0){
            int hp=Mathf.Max(1,Mathf.RoundToInt((kind==2?18+stage*4:kind==1?5+stage/2:3)*enemyHpScale));
            Fighter e=new Fighter{id=++nextId,kind=kind,hp=hp,maxHp=hp,appearance=kind==2?(stage==0?1:stage==1?0:stage==2?2:3):kind==1?1:stage==0?2:stage==1?0:(nextId%2==0?0:2),pos=pos,attack=.6f+(nextId%4)*.12f};enemies.Add(e);return e;
        }
        public AlleyHazard AddHazard(Vector2 pos,int kind=0){
            AlleyHazard h=new AlleyHazard{id=++nextId,kind=kind,hp=1,pos=ClampEnemy(pos)};
            hazards.Add(h);
            return h;
        }
        public void TriggerHazard(AlleyHazard h){
            if(h==null||h.hp<=0)return;
            h.hp=0;
            if(h.kind==0){
                AddShockwave(h.pos,155f,new Color(1f,.38f,.12f),.32f);
                SpawnCoins(h.pos,3,1+magnetSkill);
                events.Add(new CombatEvent("드럼통 폭발!!",h.pos,3));
                freeze=Mathf.Max(freeze,.025f);
                foreach(Fighter e in enemies){
                    if(e.hp<=0)continue;
                    Vector2 delta=e.pos-h.pos;
                    if(delta.magnitude<=148f){
                        Vector2 dir=delta.sqrMagnitude>.01f?delta.normalized:Vector2.up;
                        Damage(e,2+wallSkill,"폭발!",2);
                        if(!e.boss||isFever)Launch(e,dir*660f,.34f,16f);
                    }
                }
            }else{
                AddShockwave(h.pos,125f,new Color(1f,.82f,.22f),.28f);
                SpawnCoins(h.pos,5,1+magnetSkill);
                health=Mathf.Min(maxHealth,health+12);
                charge=Mathf.Min(100,charge+20);
                events.Add(new CombatEvent("보급품 대박!!",h.pos,2));
            }
        }
        public int GetSkillRank(int skillId){
            switch(skillId){
                case 0: return wallSkill;
                case 1: return chainSkill;
                case 2: return stunSkill;
                case 3: return fistSkill;
                case 4: return vampSkill;
                default: return magnetSkill;
            }
        }
        void RollChoices(){
            if(level<=2){currentChoices[0]=0;currentChoices[1]=1;currentChoices[2]=2;return;}
            List<int> pool=new List<int>{0,1,2,3,4,5};
            for(int i=0;i<3;i++){
                int pick=UnityEngine.Random.Range(0,pool.Count);
                currentChoices[i]=pool[pick];
                pool.RemoveAt(pick);
            }
        }
        public void Choose(int choiceOrSkill){
            if(state!=RunState.Choosing)return;
            int skill=choiceOrSkill;
            if(choiceOrSkill>=0&&choiceOrSkill<3&&level>2)skill=currentChoices[choiceOrSkill];
            ApplySkill(skill);
            pendingChoices=Mathf.Max(0,pendingChoices-1);
            if(pendingChoices>0){RollChoices();state=RunState.Choosing;}
            else state=RunState.Fighting;
        }
        public void ChooseAll(){
            if(state!=RunState.Choosing)return;
            for(int i=0;i<3;i++)ApplySkill(level<=2?i:currentChoices[i]);
            pendingChoices=Mathf.Max(0,pendingChoices-1);
            if(pendingChoices>0){RollChoices();state=RunState.Choosing;}
            else state=RunState.Fighting;
            events.Add(new CombatEvent("전부 각성!!",player,3));
        }
        void ApplySkill(int skill){
            if(skill==0){wallSkill++;events.Add(new CombatEvent(wallSkill>=3?"초월: 팔룡 폭파벽!":"벽꿍 강화!",player,wallSkill>=3?3:2));}
            else if(skill==1){chainSkill++;events.Add(new CombatEvent(chainSkill>=3?"초월: 인간 핀볼!":"충돌 전파!",player,chainSkill>=3?3:2));}
            else if(skill==2){stunSkill++;events.Add(new CombatEvent(stunSkill>=3?"초월: 절대 기절!":"연쇄 기절!",player,stunSkill>=3?3:2));}
            else if(skill==3){fistSkill++;events.Add(new CombatEvent(fistSkill>=3?"초월: 팔룡 핵주먹!":"폭풍 권법!",player,fistSkill>=3?3:2));}
            else if(skill==4){vampSkill++;health=Mathf.Min(maxHealth,health+15);events.Add(new CombatEvent("혈기 흡수!",player,2));}
            else{magnetSkill++;events.Add(new CombatEvent("황금 손!",player,2));}
        }
        public bool Revive(){
            if(state!=RunState.Lost)return false;
            revivesUsed++;
            health=maxHealth;charge=100;invulnerable=2.5f;feverClock=4.5f;state=RunState.Fighting;
            AddShockwave(player,220,new Color(1,.78f,.22f));
            foreach(Fighter e in enemies){
                if(e.hp<=0)continue;
                Vector2 delta=e.pos-player;Vector2 aim=delta.sqrMagnitude>.01f?delta.normalized:Vector2.up;
                Damage(e,2+baseDamage,"",0);
                Launch(e,aim*750,.38f,20);
            }
            events.Add(new CombatEvent("부활! 폭주 개시!",player,3));
            return true;
        }
        public void AddShockwave(Vector2 pos,float maxRadius,Color color,float duration=.32f){
            shockwaves.Add(new Shockwave{pos=pos,radius=14,maxRadius=maxRadius,life=duration,maxLife=duration,color=color});
        }
        void SpawnCoins(Vector2 pos,int count,int valueEach=1){
            for(int i=0;i<count;i++){
                float a=UnityEngine.Random.Range(0f,Mathf.PI*2f);
                float speed=UnityEngine.Random.Range(65f,175f);
                coins.Add(new CoinDrop{pos=pos,velocity=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*speed,value=valueEach,height=8,verticalVelocity=UnityEngine.Random.Range(110f,180f)});
            }
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
            if(feverClock>0){feverClock=Mathf.Max(0,feverClock-dt);invulnerable=Mathf.Max(invulnerable,.1f);}
            punch=Mathf.Max(0,punch-dt);attackAnimation=Mathf.Max(0,attackAnimation-dt);comboClock-=dt;if(comboClock<=0){chain=0;comboStep=0;}
            for(int i=shockwaves.Count-1;i>=0;i--){
                Shockwave s=shockwaves[i];s.life-=dt;
                s.radius=Mathf.Lerp(s.maxRadius,14,Mathf.Clamp01(s.life/s.maxLife));
                if(s.life<=0)shockwaves.RemoveAt(i);
            }
            UpdateCoins(dt);
            movement=Vector2.ClampMagnitude(movement,1);if(movement.magnitude<.10f)movement=Vector2.zero;
            // Retreat cancels preparation instead of forcing a backwards turn and a skating punch.
            if(attackLead>0&&movement.sqrMagnitude>.16f&&Vector2.Dot(movement.normalized,strikeDirection)<-.35f){attackLead=0;pendingTarget=null;attackAnimation=Mathf.Min(attackAnimation,.12f);attackCooldown=.14f;}
            float brace=attackLead>0?.62f:attackAnimation>.22f?.28f:1;
            float speedMul=isFever?1.25f:1f;
            float acceleration=movement.sqrMagnitude>0?1250:1900;
            moveVelocity=Vector2.MoveTowards(moveVelocity,movement*(172*brace*speedMul),acceleration*dt);
            player+=(moveVelocity+playerImpulse)*dt;playerImpulse*=Mathf.Exp(-12*dt);ConstrainPlayer();
            if(movement.sqrMagnitude>.03f&&attackAnimation<=0)direction=movement.normalized;
            if(special&&charge>=100)Special();
            if(attackLead>0){
                attackLead-=dt;
                if(attackLead<=0){if(pendingTarget!=null&&pendingTarget.hp>0)ResolvePunch(strikeDirection);pendingTarget=null;}
            }
            if(attackCooldown<=0&&attackLead<=0&&attackAnimation<=.08f&&playerReaction<=0){
                Fighter target=AcquireTarget(movement);
                if(target!=null){
                    trackedTarget=pendingTarget=target;
                    float haste=1f/(1f+fistSkill*.16f+(isFever?.38f:0)+hasteBonus);
                    attackLead=.10f*haste;attackAnimation=.44f*haste;attackCooldown=.52f*haste;
                    strikeDirection=(target.pos-player).normalized;if(strikeDirection.sqrMagnitude<.01f)strikeDirection=direction;direction=strikeDirection;
                }
            }
            if(hasPartner)SimulatePartner(dt);
            Spawn(dt);
            foreach(Fighter e in enemies)if(e.hp>0)MoveEnemy(e,dt);
            ResolveContacts(dt);
            ResolveHazards();
            for(int i=enemies.Count-1;i>=0;i--)if(enemies[i].hp<=0){
                Fighter e=enemies[i];enemies.RemoveAt(i);kills++;xp+=1+(magnetSkill>0&&kills%3==0?1:0);
                if(vampSkill>0)health=Mathf.Min(maxHealth,health+vampSkill*2.5f);
                SpawnCoins(e.pos,e.boss?12:e.kind==1?3:1,1+magnetSkill);
                events.Add(new CombatEvent("제압",e.pos,0));
                if(e.boss){
                    foreach(CoinDrop c in coins){collectedCoins+=c.value;}coins.Clear();
                    state=RunState.Won;events.Add(new CombatEvent("BOSS DOWN",e.pos,3));
                }
            }
            if(state==RunState.Fighting&&xp>=nextXp&&level<7){xp-=nextXp;level++;nextXp=(level+2)*xpCurveScale;pendingChoices++;RollChoices();state=RunState.Choosing;}
            if(health<=0)state=RunState.Lost;
        }
        void SimulatePartner(float dt){
            partnerCooldown-=dt;partnerPunch=Mathf.Max(0,partnerPunch-dt);
            Fighter nearest=null;float bestDist=240f;
            foreach(Fighter e in enemies){
                if(e.hp<=0)continue;
                float d=(e.pos-partnerPos).magnitude;
                if(d<bestDist){bestDist=d;nearest=e;}
            }
            Vector2 goal=nearest!=null?nearest.pos-(nearest.pos-partnerPos).normalized*56f:player+new Vector2(-46,20);
            Vector2 delta=goal-partnerPos;
            Vector2 desired=delta.magnitude>12f?delta.normalized*155f:Vector2.zero;
            partnerVelocity=Vector2.MoveTowards(partnerVelocity,desired,950f*dt);
            partnerPos=ClampEnemy(partnerPos+partnerVelocity*dt);
            if(partnerVelocity.sqrMagnitude>20f)partnerDirection=partnerVelocity.normalized;
            if(nearest!=null&&(nearest.pos-partnerPos).magnitude<92f&&partnerCooldown<=0){
                partnerCooldown=.78f;partnerPunch=.18f;
                Vector2 aim=(nearest.pos-partnerPos).normalized;if(aim.sqrMagnitude<.01f)aim=Vector2.right;
                partnerDirection=aim;
                Damage(nearest,partnerDamage,"동료 지원!",1);
                if(!nearest.boss)Launch(nearest,aim*540f,.26f,10f);
            }
        }
        void ResolveHazards(){
            for(int i=hazards.Count-1;i>=0;i--){
                AlleyHazard h=hazards[i];
                if(h.hp<=0){hazards.RemoveAt(i);continue;}
                foreach(Fighter e in enemies){
                    if(e.hp<=0||e.velocity.sqrMagnitude<130*130)continue;
                    if((e.pos-h.pos).magnitude<46f){
                        TriggerHazard(h);
                        break;
                    }
                }
                if(h.hp<=0)hazards.RemoveAt(i);
            }
        }
        void UpdateCoins(float dt){
            float magnetRange=115+magnetSkill*65+(isFever?180:0);
            for(int i=coins.Count-1;i>=0;i--){
                CoinDrop c=coins[i];c.age+=dt;
                if(c.height>0||c.verticalVelocity>0){
                    c.height+=c.verticalVelocity*dt;c.verticalVelocity-=520*dt;
                    if(c.height<=0){c.height=0;c.verticalVelocity=0;}
                }
                Vector2 toPlayer=player-c.pos;float dist=toPlayer.magnitude;
                if(c.age>.18f&&dist<magnetRange)c.magnetized=true;
                if(c.magnetized){
                    Vector2 dir=dist>.01f?toPlayer/dist:Vector2.zero;
                    c.velocity=Vector2.MoveTowards(c.velocity,dir*580,2200*dt);
                    c.pos+=c.velocity*dt;
                    if(dist<28){
                        collectedCoins+=c.value;
                        charge=Mathf.Min(100,charge+(1.5f+magnetSkill*.8f)*chargeGainMultiplier);
                        events.Add(new CombatEvent("+"+c.value,player+new Vector2(0,-22),0));
                        coins.RemoveAt(i);continue;
                    }
                }else{
                    c.pos=ClampEnemy(c.pos+c.velocity*dt);
                    c.velocity*=Mathf.Exp(-7*dt);
                }
            }
        }
        Fighter AcquireTarget(Vector2 movement){
            Fighter best=null;float score=112+(isFever?28:0);
            foreach(Fighter e in enemies){
                if(e.hp<=0)continue;Vector2 delta=e.pos-player;float distance=delta.magnitude;if(distance>104+(isFever?24:0))continue;
                if(movement.sqrMagnitude>.09f&&distance>52&&Vector2.Dot(movement.normalized,delta.normalized)<-.1f)continue;
                float candidate=distance*(e==trackedTarget?.80f:1);if(candidate<score){best=e;score=candidate;}
            }
            return best;
        }
        void Spawn(float dt){
            if(spawning&&hazards.Count<2){
                hazardSpawnClock-=dt;
                if(hazardSpawnClock<=0){
                    hazardSpawnClock=6.5f;
                    float hx=UnityEngine.Random.Range(arena.xMin+48f,arena.xMax-48f);
                    float hy=UnityEngine.Random.Range(arena.yMin+55f,arena.yMax-55f);
                    if((new Vector2(hx,hy)-player).magnitude>85f){
                        AddHazard(new Vector2(hx,hy),UnityEngine.Random.value<.35f?1:0);
                    }
                }
            }
            spawnClock-=dt;if(!spawning||spawnClock>0||bossSpawned||enemies.Count>=9)return;
            if(kills>=targetKills){AddEnemy(new Vector2(arena.center.x,arena.yMin+35),2);bossSpawned=true;events.Add(new CombatEvent("골목 대장 등장",new Vector2(arena.center.x,arena.yMin+105),3));return;}
            float side=UnityEngine.Random.value<.5f?-1:1;
            float x=Mathf.Clamp(player.x+side*Mathf.Min(310,arena.width*.5f),arena.xMin+12,arena.xMax-12);
            float y=Mathf.Clamp(player.y+UnityEngine.Random.Range(-220f,220f),arena.yMin+35,arena.yMax-25);
            AddEnemy(new Vector2(x,y),kills>3&&UnityEngine.Random.value<StageCatalog.Get(stage).heavyChance?1:0);spawnClock=Mathf.Max(.65f,1.25f-kills*.025f);
        }
        void MoveEnemy(Fighter e,float dt){
            e.flash=Mathf.Max(0,e.flash-dt);e.stun-=dt;e.contact-=dt;e.wallCooldown-=dt;e.attack-=dt;e.recovery=Mathf.Max(0,e.recovery-dt);e.reaction=Mathf.Max(0,e.reaction-dt);e.groggy=Mathf.Max(0,e.groggy-dt);
            if(e.boss&&!e.enraged&&e.hp<=e.maxHp/2){
                e.enraged=true;e.flash=.3f;
                AddShockwave(e.pos,180,new Color(1,.25f,.15f));
                events.Add(new CombatEvent("대장 광폭화!!",e.pos,3));
            }
            if(e.airDuration>0)e.airElapsed=Mathf.Min(e.airDuration,e.airElapsed+dt);
            if(e.windup<=0&&e.dash<=0){
                int want=player.x-e.pos.x>18?1:player.x-e.pos.x<-18?-1:e.facing;
                if(e.shield&&want!=e.facing){e.turnDelay+=dt;if(e.turnDelay>=.55f){e.facing=want;e.turnDelay=0;}}
                else{e.facing=want;e.turnDelay=0;}
            }
            if(e.velocity.sqrMagnitude>28*28){
                e.windup=0;e.locomotion=Vector2.zero;
                Vector2 desired=e.pos+e.velocity*dt;bool hitX=desired.x<arena.xMin||desired.x>arena.xMax,hitY=desired.y<arena.yMin||desired.y>arena.yMax;
                e.pos=ClampEnemy(desired);
                if((hitX||hitY)&&e.wallCooldown<=0){
                    bool bossSlam=groggyBoss&&e.boss&&e.groggy>0; // the payoff for opening the boss up; it also ends the opening
                    e.wallCooldown=.30f;wallHits++;CountChain();
                    if(bossSlam){e.groggy=0;Damage(e,2+wallSkill,"짱 벽꿍!!",3);}else Damage(e,1+wallSkill,wallSkill>=3?"폭파벽!!":"벽꿍!",1);
                    AddShockwave(e.pos,75+wallSkill*18,wallSkill>=3?new Color(1,.32f,.12f):new Color(1,.65f,.22f),.24f);
                    if(wallSkill>0)SpawnCoins(e.pos,wallSkill>=3?2:1,1);
                    if(wallSkill>=3){
                        foreach(Fighter other in enemies){
                            if(other!=e&&other.hp>0&&(other.pos-e.pos).magnitude<115f){
                                Damage(other,1+wallSkill/2,"폭발 파편!",2);
                                other.stun=Mathf.Max(other.stun,.35f);
                            }
                        }
                    }
                    if(hitX)e.velocity.x*=-.16f;if(hitY)e.velocity.y*=-.16f;
                    e.stun=Mathf.Max(e.stun,bossSlam?.7f:.26f+stunSkill*.25f);e.reaction=Mathf.Max(e.reaction,.22f);freeze=Mathf.Max(freeze,bossSlam?.05f:.014f);
                }
                float drag=e.airElapsed<e.airDuration?3.6f:8.5f;e.velocity*=Mathf.Exp(-drag*dt);return;
            }
            e.velocity=Vector2.zero;
            if(e.stun>0||e.groggy>0||e.reaction>.10f){e.locomotion=Vector2.MoveTowards(e.locomotion,Vector2.zero,600*dt);return;}
            Vector2 delta=player-e.pos;float distance=delta.magnitude;
            if(e.dash>0){
                float started=.38f-e.dash;float ramp=Mathf.Min(Mathf.Clamp01(started/.055f),Mathf.Clamp01(e.dash/.10f));
                e.locomotion=e.aim*(480*(e.enraged?1.22f:1f)*ramp);e.pos=ClampEnemy(e.pos+e.locomotion*dt);e.dash-=dt;
                if((player-e.pos).magnitude<52)Hurt(Scaled(e,e.boss?18:12),e.pos);
                if(e.dash<=0){
                    if(e.boss&&e.burstQueue>0){
                        e.burstQueue--;e.windup=.32f;e.aim=(player-e.pos).normalized;if(e.aim.sqrMagnitude<.01f)e.aim=Vector2.right;e.facing=e.aim.x>=0?1:-1;
                        return;
                    }
                    if(e.boss&&stage>=2){
                        AddShockwave(e.pos,115,new Color(1,.3f,.18f),.28f);
                        if((player-e.pos).magnitude<105)Hurt(Scaled(e,12),e.pos);
                    }
                    e.recovery=.32f;e.locomotion=e.aim*90;
                }
                return;
            }
            if(e.windup>0){
                e.locomotion=Vector2.MoveTowards(e.locomotion,Vector2.zero,900*dt);e.pos=ClampEnemy(e.pos+e.locomotion*dt);e.windup-=dt;
                if(e.windup<=0){
                    if(e.kind>=1){e.dash=.38f;e.attack=e.boss?(e.enraged?1.8f:2.5f):2.1f;}
                    else{if(distance<(e.appearance==0?111:83))Hurt(Scaled(e,e.appearance==0?10:8),e.pos);e.attack=e.appearance==0?1.8f:1.5f;e.recovery=.22f;}
                }
                return;
            }
            if(e.recovery>0){e.locomotion=Vector2.MoveTowards(e.locomotion,Vector2.zero,750*dt);e.pos=ClampEnemy(e.pos+e.locomotion*dt);return;}
            if(e.kind>=1&&e.attack<=0&&(e.boss||distance<240)&&ChargeSlotAvailable(e)){
                e.windup=e.boss?(e.enraged?.68f:.92f):.8f;
                e.burstQueue=e.boss&&(stage>=1||e.enraged)?1:0;
                e.aim=delta.normalized;e.facing=e.aim.x>=0?1:-1;return;
            }
            if(e.kind==0&&distance<(e.appearance==0?104:76)&&e.attack<=0&&MeleeSlotsAvailable(e)){e.windup=.56f;e.aim=delta.normalized;return;}
            Vector2 desiredVelocity=Vector2.zero;
            if(distance>54){
                float a=e.id*2.399963f;float laneRadius=e.boss?119:e.kind==1?94:e.appearance==0?98:70;Vector2 lane=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*laneRadius;
                Vector2 target=player+lane*Mathf.Clamp01((190-distance)/100f);
                Vector2 steering=(target-e.pos).normalized;
                foreach(Fighter other in enemies){if(other==e||other.hp<=0)continue;Vector2 away=e.pos-other.pos;float d=away.magnitude;float clearance=BodyRadius(e)+BodyRadius(other)+24;if(d>.01f&&d<clearance)steering+=away/d*(1-d/clearance)*1.15f;}
                float speed=e.boss?(e.enraged?62:45):e.kind==1?55:(72+stage*4)*(e.shield?.85f:1);
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
                    if(Guarded(target,source.velocity.normalized)&&++target.trayHits<TrayDurability){
                        // A body thrown into the tray face bounces off without a chain.
                        source.velocity=Vector2.Reflect(source.velocity,normal)*.45f;source.contact=.14f;target.contact=.30f;
                        target.reaction=Mathf.Max(target.reaction,.15f);events.Add(new CombatEvent("팅!",target.pos,1));continue;
                    }
                    bool trayBreak=target.shield;if(trayBreak)BreakTray(target);
                    source.contact=.14f;target.contact=.30f;chainHits++;CountChain();
                    bool bossBreak=groggyBoss&&target.boss;
                    Vector2 impulse=source.velocity.normalized*(420+chainSkill*110)*(bossBreak?.55f:1);Launch(target,impulse,.27f,9);target.contact=.30f;target.stun=.34f+stunSkill*.45f;
                    source.velocity*=chainSkill>=3?.88f:.64f;
                    if(bossBreak){target.groggy=1.8f+stunSkill*.3f;target.stun=Mathf.Max(target.stun,target.groggy);Damage(target,2+chainSkill,"그로기!!",3);}
                    else Damage(target,1+chainSkill+(trayBreak?1:0),chainSkill>=3?"핀볼 폭격!":"연쇄!",2);
                    AddShockwave(target.pos,65+chainSkill*18,new Color(.32f,.85f,.66f),.22f);
                }else{
                    float settle=(required-distance)*.5f*(1-Mathf.Exp(-20*dt));
                    if(a.velocity.sqrMagnitude<900&&a.dash<=0)a.pos=ClampEnemy(a.pos-normal*settle);
                    if(b.velocity.sqrMagnitude<900&&b.dash<=0)b.pos=ClampEnemy(b.pos+normal*settle);
                }
            }
        }
        void CountChain(){chain++;bestChain=Mathf.Max(bestChain,chain);comboClock=2.3f;}
        Vector2 ClampEnemy(Vector2 p){return new Vector2(Mathf.Clamp(p.x,arena.xMin,arena.xMax),Mathf.Clamp(p.y,arena.yMin,arena.yMax));}
        void ConstrainPlayer(){player=new Vector2(Mathf.Clamp(player.x,arena.xMin+12,arena.xMax-12),Mathf.Clamp(player.y,arena.yMin+12,arena.yMax-12));if(player.x<=arena.xMin+12||player.x>=arena.xMax-12)moveVelocity.x=0;if(player.y<=arena.yMin+12||player.y>=arena.yMax-12)moveVelocity.y=0;}
        static void Launch(Fighter e,Vector2 impulse,float duration,float peak){e.velocity=impulse;e.locomotion=Vector2.zero;e.airElapsed=0;e.airDuration=duration;e.airPeak=peak;e.reaction=duration+.14f;e.stun=duration+.10f;e.windup=0;e.dash=0;e.recovery=0;e.burstQueue=0;}
        public void Attack(Fighter target){Vector2 aim=(target.pos-player).normalized;ResolvePunch(aim.sqrMagnitude>.01f?aim:direction);}
        void ResolvePunch(Vector2 aim){
            direction=aim;punch=.19f;
            float haste=1f/(1f+fistSkill*.16f+(isFever?.38f:0)+hasteBonus);
            attackCooldown=.43f*haste;hits++;comboStep=(comboStep+1)%3;
            bool finisher=comboStep==0||(isFever&&hits%2==0);
            if(attackAnimation<=0)attackAnimation=.34f*haste;
            bool dragonFist=finisher&&fistSkill>=3;
            float reach=110+fistSkill*12+(finisher?24:0)+(dragonFist?28:0)+(isFever?20:0);
            int dmg=baseDamage+(finisher&&fistSkill>0?1:0)+(dragonFist?1:0)+(isFever?1:0);
            float knockback=(650+fistSkill*55+(finisher?190:0)+(isFever?160:0))*punchKnockbackMultiplier;
            if(finisher){
                AddShockwave(player+aim*52,88+fistSkill*16,isFever||dragonFist?new Color(1,.76f,.22f):new Color(1,.42f,.18f),.22f);
                if(dragonFist)AddShockwave(player+aim*108,118,new Color(1,.32f,.12f),.26f);
            }
            foreach(Fighter e in enemies){
                Vector2 delta=e.pos-player;if(e.hp<=0||delta.magnitude>=reach||Vector2.Dot(aim,delta.normalized)<=(finisher?.05f:.25f))continue;
                if(Guarded(e,aim)&&++e.trayHits<TrayDurability){e.pos=ClampEnemy(e.pos+aim*(finisher?18:10));e.flash=.06f;e.reaction=Mathf.Max(e.reaction,.12f);events.Add(new CombatEvent("쟁반 막기!",e.pos,1));continue;}
                bool trayBreak=e.shield;if(trayBreak)BreakTray(e);
                int hitDamage=dmg+(trayBreak?1:0);if(groggyBoss&&e.boss)hitDamage=e.groggy>0?dmg+1:finisher?1:0; // a fresh school boss only takes chip damage
                Damage(e,hitDamage,dragonFist?"팔룡 핵주먹!":finisher?"강타!":"",finisher?1:0);
                bool bossOpen=e.boss&&(groggyBoss?e.groggy>0:finisher&&(isFever||dragonFist));
                // Elites have super armor: only the combo finisher sends them flying; other punches land without interrupting.
                bool canLaunch=e.elite?finisher:(e.kind==0||(e.kind==1&&(e.windup<=0&&e.dash<=0||finisher))||bossOpen);
                if(canLaunch)Launch(e,aim*knockback*(e.boss&&groggyBoss?.75f:1)*(e.elite?eliteKnockback:1),finisher?.36f:.30f,finisher?18:12);
                else if(e.boss&&groggyBoss)e.pos=ClampEnemy(e.pos+aim*(finisher?22:12)); // only a short shove until a collision opens it up
                else if(e.elite)e.pos=ClampEnemy(e.pos+aim*10);
            }
            for(int i=hazards.Count-1;i>=0;i--){
                AlleyHazard h=hazards[i];
                if(h.hp>0&&(h.pos-player).magnitude<reach&&Vector2.Dot(aim,(h.pos-player).normalized)>0f){
                    TriggerHazard(h);
                }
            }
            charge=Mathf.Min(100,charge+(9+vampSkill*2)*chargeGainMultiplier);freeze=Mathf.Max(freeze,finisher?.018f:.009f);
            events.Add(new CombatEvent(finisher?"크리티컬!":"퍽!",player+aim*60,1));
        }
        public bool Special(){
            if(state!=RunState.Fighting||charge<100)return false;
            if(phoneSpecial){
                charge=0;attackLead=0;pendingTarget=null;invulnerable=.8f;health=Mathf.Min(maxHealth,health+phoneHeal); // mom's pep talk: no fever, a little sustain
                AddShockwave(player,290,new Color(.45f,.85f,.65f),.5f);
                foreach(Fighter e in enemies){
                    Vector2 delta=e.pos-player;if(e.hp<=0||delta.magnitude>280)continue;
                    Vector2 away=delta.sqrMagnitude>.01f?delta.normalized:Vector2.right;
                    if(e.shield)BreakTray(e);
                    Launch(e,away*(e.boss?250:720),.38f,e.boss?5:18);
                    e.stun=Mathf.Max(e.stun,e.boss?.65f:1.6f);
                    if(groggyBoss&&e.boss){e.groggy=Mathf.Max(e.groggy,1.4f);e.stun=Mathf.Max(e.stun,e.groggy);events.Add(new CombatEvent("짱 겁먹음!",e.pos,3));}
                }
                freeze=Mathf.Max(freeze,.035f);events.Add(new CombatEvent("엄마한테 전화한다!",player,3));return true;
            }
            charge=0;attackLead=0;pendingTarget=null;attackCooldown=Mathf.Max(attackCooldown,.36f);punch=.25f;attackAnimation=.32f;invulnerable=1.2f;
            feverClock=feverDuration+vampSkill*.8f;
            AddShockwave(player,210,new Color(1,.78f,.24f),.38f);
            foreach(Fighter e in enemies){Vector2 delta=e.pos-player;if(e.hp<=0||delta.magnitude>195)continue;Vector2 aim=delta.sqrMagnitude>.01f?delta.normalized:Vector2.up;if(e.shield)BreakTray(e);Damage(e,2+baseDamage,"",0);Launch(e,aim*(e.boss?320:880),e.boss?.26f:.44f,e.boss?8:26);}
            freeze=Mathf.Max(freeze,.065f);events.Add(new CombatEvent("전성기의 한 방",player,3));return true;
        }
        // incoming = travel direction of the hit. The tray covers about 60 degrees either side of the facing direction.
        static bool Guarded(Fighter e,Vector2 incoming){return e.shield&&e.velocity.sqrMagnitude<120*120&&incoming.x*e.facing<-.5f;}
        void BreakTray(Fighter e){e.shield=false;e.trayBroken=true;e.windup=0;e.stun=Mathf.Max(e.stun,.6f);AddShockwave(e.pos,70,new Color(.85f,.88f,.9f),.22f);freeze=Mathf.Max(freeze,.03f);events.Add(new CombatEvent("쟁반 박살!",e.pos,2));}
        void Damage(Fighter e,int amount,string label,int style){if(e.hp<=0)return;e.hp-=amount;e.flash=.10f;charge=Mathf.Min(100,charge+(3+vampSkill)*chargeGainMultiplier);events.Add(new CombatEvent(label.Length>0?label:"−"+amount,e.pos,style));}
        int Scaled(Fighter e,int amount){return Mathf.Max(1,Mathf.RoundToInt(amount*e.power*incomingDamageScale));}
        void Hurt(int amount,Vector2 origin){
            if(invulnerable>0||isFever)return;health=Mathf.Max(0,health-amount);invulnerable=.7f;playerReaction=.18f;
            playerImpulse=(player-origin).normalized*185;attackLead=0;pendingTarget=null;attackAnimation=Mathf.Min(attackAnimation,.12f);attackCooldown=Mathf.Max(attackCooldown,.20f);
            events.Add(new CombatEvent("−"+amount,player,4));
        }
    }
}
