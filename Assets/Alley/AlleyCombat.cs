using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondFistGame {
    public enum RunState { Ready, Fighting, Choosing, Won, Lost }
    public sealed class Fighter {
        public int id, kind, hp, maxHp, facing=1;
        public Vector2 pos, velocity, aim;
        public float attack, windup, stun, flash, contact, dash, wallCooldown;
        public bool boss { get { return kind==2; } }
    }
    public struct CombatEvent {
        public string text; public Vector2 pos; public int style;
        public CombatEvent(string text,Vector2 pos,int style) { this.text=text;this.pos=pos;this.style=style; }
    }
    public sealed class AlleyCombat {
        public RunState state=RunState.Ready;
        public readonly List<Fighter> enemies=new List<Fighter>();
        public readonly List<CombatEvent> events=new List<CombatEvent>();
        public Vector2 player=new Vector2(270,535), direction=Vector2.right;
        public float health=100, charge=100, elapsed, attackCooldown, invulnerable, punch, freeze, attackLead, attackAnimation;
        public Vector2 moveVelocity;
        Fighter pendingTarget;
        public int kills, hits, wallHits, chainHits, level=1, xp, nextXp=3, stage, bestChain;
        public int wallSkill, chainSkill, stunSkill;
        public bool bossSpawned, spawning=true;
        public float spawnClock=.25f, comboClock;
        public int chain;
        int nextId, pendingChoices;
        public const float Left=65,Right=475,Top=295,Bottom=745;
        public AlleyCombat(int stage=0) { this.stage=stage; }
        public void Start() { state=RunState.Fighting; }
        public Fighter AddEnemy(Vector2 pos,int kind=0) {
            int hp=kind==2?36+stage*8:kind==1?5:3;
            Fighter e=new Fighter{id=++nextId,kind=kind,hp=hp,maxHp=hp,pos=pos,attack=.7f}; enemies.Add(e); return e;
        }
        public void Choose(int skill) {
            if(state!=RunState.Choosing)return;
            if(skill==0)wallSkill++; else if(skill==1)chainSkill++; else stunSkill++;
            pendingChoices=Mathf.Max(0,pendingChoices-1);
            state=pendingChoices>0?RunState.Choosing:RunState.Fighting;
            events.Add(new CombatEvent(skill==0?"벽꿍 강화!":skill==1?"충돌 전파!":"연쇄 기절!",player,2));
        }
        public void Tick(float dt,Vector2 movement,bool special) {
            if(state!=RunState.Fighting)return;
            dt=Mathf.Min(dt,.04f);
            if(freeze>0) { freeze-=dt; return; }
            elapsed+=dt; attackCooldown-=dt; invulnerable-=dt; punch=Mathf.Max(0,punch-dt); attackAnimation=Mathf.Max(0,attackAnimation-dt); comboClock-=dt;
            if(comboClock<=0)chain=0;
            movement=Vector2.ClampMagnitude(movement,1);
            moveVelocity=Vector2.MoveTowards(moveVelocity,movement*172,dt*1350);
            player+=moveVelocity*dt;
            player=new Vector2(Mathf.Clamp(player.x,Left+12,Right-12),Mathf.Clamp(player.y,Top+12,Bottom-12));
            if(movement.sqrMagnitude>.03f&&attackAnimation<=0)direction=movement.normalized;
            if(special&&charge>=100) { attackLead=0;pendingTarget=null;Special(); }
            if(attackLead>0) {
                attackLead-=dt;
                if(attackLead<=0) {
                    if(pendingTarget!=null&&pendingTarget.hp>0&&(pendingTarget.pos-player).magnitude<145) Attack(pendingTarget);
                    else attackCooldown=.16f;
                    pendingTarget=null;
                }
            }
            if(attackCooldown<=0&&attackLead<=0) {
                Fighter nearest=null; float distance=104;
                foreach(Fighter e in enemies) { float d=(e.pos-player).magnitude; if(e.hp>0&&d<distance){nearest=e;distance=d;} }
                if(nearest!=null) { pendingTarget=nearest;attackLead=.10f;attackAnimation=.44f;attackCooldown=.52f;direction=(nearest.pos-player).normalized; }
            }
            spawnClock-=dt;
            if(spawning && spawnClock<=0 && !bossSpawned && enemies.Count<9) {
                if(kills>=18) { AddEnemy(new Vector2(270,Top+35),2); bossSpawned=true; events.Add(new CombatEvent("골목 대장 등장",new Vector2(270,390),3)); }
                else {
                    float x=UnityEngine.Random.value<.5f?Left+12:Right-12;
                    float y=UnityEngine.Random.Range(Top+45,Bottom-25);
                    AddEnemy(new Vector2(x,y),kills>5&&UnityEngine.Random.value<.28f?1:0);
                    spawnClock=Mathf.Max(.65f,1.25f-kills*.025f);
                }
            }
            foreach(Fighter e in enemies) {
                if(e.hp<=0)continue;
                e.flash=Mathf.Max(0,e.flash-dt);e.stun-=dt;e.contact-=dt;e.wallCooldown-=dt;e.attack-=dt;
                e.facing=player.x>=e.pos.x?1:-1;
                if(e.velocity.sqrMagnitude>900) {
                    e.windup=0;
                    Vector2 desired=e.pos+e.velocity*dt;
                    bool wall=desired.x<Left||desired.x>Right||desired.y<Top||desired.y>Bottom;
                    e.pos=new Vector2(Mathf.Clamp(desired.x,Left,Right),Mathf.Clamp(desired.y,Top,Bottom));
                    if(wall && e.wallCooldown<=0) {
                        e.wallCooldown=.45f; wallHits++; chain++;bestChain=Mathf.Max(bestChain,chain);comboClock=2;
                        Damage(e,1+wallSkill,"벽꿍!",1);
                        e.velocity*=-.22f;e.stun=Mathf.Max(e.stun,.35f+stunSkill*.25f);
                    }
                    e.velocity*=Mathf.Exp(-3.3f*dt);
                }
                else if(e.stun<=0) {
                    e.velocity=Vector2.zero;
                    Vector2 delta=player-e.pos;float dist=delta.magnitude;
                    if(e.dash>0) {
                        e.dash-=dt;e.pos+=e.aim*480*dt;
                        if((player-e.pos).magnitude<52) Hurt(e.boss?18:12,e.pos);
                        e.pos=new Vector2(Mathf.Clamp(e.pos.x,Left,Right),Mathf.Clamp(e.pos.y,Top,Bottom));
                    }
                    else if(e.windup>0) {
                        e.windup-=dt;
                        if(e.windup<=0) {
                            if(e.kind>=1) { e.dash=.38f;e.attack=e.boss?2.8f:2.1f; }
                            else { if(dist<83)Hurt(e.kind==1?12:8,e.pos);e.attack=1.5f; }
                        }
                    }
                    else if(e.kind>=1 && e.attack<=0 && (e.boss||dist<240)) { e.windup=e.boss?.95f:.8f;e.aim=delta.normalized; }
                    else if(dist<65 && e.attack<=0) { e.windup=e.kind==1?.85f:.65f;e.aim=delta.normalized; }
                    else if(dist>51) e.pos+=delta.normalized*(e.boss?39:e.kind==1?43:59+stage*4)*dt;
                }
            }
            for(int i=0;i<enemies.Count;i++) for(int j=i+1;j<enemies.Count;j++) {
                Fighter a=enemies[i],b=enemies[j];if(a.hp<=0||b.hp<=0)continue;
                Vector2 delta=b.pos-a.pos;float distance=delta.magnitude;
                if(distance>=43)continue;
                Vector2 normal=distance>.01f?delta/distance:Vector2.right;
                Fighter source=a.velocity.sqrMagnitude>b.velocity.sqrMagnitude?a:b;
                Fighter target=source==a?b:a;
                if(source.velocity.sqrMagnitude>180*180 && source.contact<=0 && target.contact<=0) {
                    target.contact=.28f;source.contact=.12f;chainHits++;chain++;bestChain=Mathf.Max(chain,bestChain);comboClock=2;
                    target.velocity=source.velocity.normalized*(420+chainSkill*100);source.velocity*=.62f;
                    target.stun=.4f+stunSkill*.45f;
                    Damage(target,1+chainSkill,"연쇄!",2);
                }
                else {
                    float overlap=(43-distance)*.5f;
                    if(a.velocity.sqrMagnitude<900)a.pos-=normal*overlap;
                    if(b.velocity.sqrMagnitude<900)b.pos+=normal*overlap;
                }
            }
            for(int i=enemies.Count-1;i>=0;i--) if(enemies[i].hp<=0) {
                Fighter e=enemies[i];enemies.RemoveAt(i);kills++;xp++;
                events.Add(new CombatEvent("제압",e.pos,0));
                if(e.boss) { state=RunState.Won;events.Add(new CombatEvent("BOSS DOWN",e.pos,3)); }
            }
            if(state==RunState.Fighting && xp>=nextXp && level<5) {
                xp-=nextXp;level++;nextXp=level+2;pendingChoices++;state=RunState.Choosing;
            }
            if(health<=0)state=RunState.Lost;
        }
        public void Attack(Fighter target) {
            direction=(target.pos-player).normalized;if(direction.sqrMagnitude<.01f)direction=Vector2.right;
            punch=.19f;attackCooldown=.43f;hits++;
            foreach(Fighter e in enemies) {
                Vector2 delta=e.pos-player;
                if(e.hp>0&&delta.magnitude<110&&Vector2.Dot(direction,delta.normalized)>.25f) {
                    Damage(e,1,"",0);if(e.kind==0||(e.kind==1&&e.windup<=0&&e.dash<=0)){e.velocity=direction*540;e.stun=.22f;e.windup=0;}
                }
            }
            charge=Mathf.Min(100,charge+9);freeze=.035f;
            events.Add(new CombatEvent("퍽!",player+direction*60,1));
        }
        public bool Special() {
            if(state!=RunState.Fighting||charge<100)return false;
            charge=0;punch=.3f;attackAnimation=.32f;invulnerable=.55f;
            foreach(Fighter e in enemies) {
                Vector2 delta=e.pos-player;if(e.hp<=0||delta.magnitude>185)continue;
                Vector2 dir=delta.sqrMagnitude>.01f?delta.normalized:Vector2.up;
                Damage(e,2,"",0);e.velocity=dir*(e.boss?280:880);e.stun=.8f;e.windup=0;
            }
            freeze=.09f;events.Add(new CombatEvent("전성기의 한 방",player,3));return true;
        }
        void Damage(Fighter e,int amount,string label,int style) {
            if(e.hp<=0)return;
            e.hp-=amount;e.flash=.12f;charge=Mathf.Min(100,charge+3);
            events.Add(new CombatEvent(label.Length>0?label:"−"+amount,e.pos,style));
        }
        void Hurt(int amount,Vector2 origin) {
            if(invulnerable>0)return;
            health=Mathf.Max(0,health-amount);invulnerable=.7f;
            events.Add(new CombatEvent("−"+amount,player,4));
            Vector2 push=(player-origin).normalized*15;
            player+=push; player=new Vector2(Mathf.Clamp(player.x,Left+12,Right-12),Mathf.Clamp(player.y,Top+12,Bottom-12));
        }
    }
}
