using System;
using System.Collections.Generic;
using UnityEngine;

// The numbers the fight and the picture share: the line feet stand on, the line where the house is lost,
// and the hand things leave from. DadThrowLayout builds this from the art manifests, so the rules below
// hold no screen coordinates of their own and cannot drift away from the picture.
public sealed class DadThrowField
{
    public readonly float Ground,Gate;public readonly Vector2 Muzzle;
    public DadThrowField(float ground,float gate,Vector2 muzzle){Ground=ground;Gate=gate;Muzzle=muzzle;}
}

// The throw numbers the plan lists with a range to compare (§8.1). The defaults are what the game plays with;
// comparison runs change a copy and never the game.
// Since 2026-11-17 (Design/dad-throw-number-compare-2026-11-17-before.txt): a golf ball ends at its first
// landing, so it only reaches people standing together, and a shoe pushes without hurting, so the people it
// gathers are still there to be hit. With two bounces and a hurting shoe the shoe never changed anything.
public sealed class DadThrowRules
{
    // A ball went through five and reloads took 0.65 and 0.9 seconds until the stampede (2026-11-17: "much harder,
    // hectic from 1-1", "they should just come running in a mad herd"). Against a hundred runners a ball has to
    // bowl through a whole pack and the hand has to be ready again at once.
    public int Pierce=12,MaxBounces=0,ShoeDamage=0;
    public float GolfReload=.3f,ShoeReload=.7f,ShoeRadius=85,ShoePush=105,SlowTime=2.5f,SlowFactor=.35f;
    // How fast the street runs while the player is pulling back. 1 is full speed. The first prototype slowed to
    // 0.35, which kept the whole fight in slow motion for anyone who throws without pause.
    public float AimPace=1;
    // What a pick in a 연속전 run can change (DadThrowRun). A stage plays with these defaults, which leave every
    // throw as it was: one ball, one point of hurt, a shoe that only pushes, a hand that always waits its reload.
    // GolfCount: balls leaving the hand together. ShoeBlast: hurt done to everyone a shoe gathers.
    // ChainReload: a ball that reaches this many people readies the hand at once (0 = never).
    public int GolfDamage=1,GolfCount=1,ShoeBlast=0,ChainReload=0;
    public DadThrowRules Copy(){return (DadThrowRules)MemberwiseClone();}
}

// Isolated landscape experiment; no campaign, PlayerPrefs, ads or native physics dependency.
// Two ways to fill the street: a hand-made DadThrowStage (the game), or the endless wave formula of the
// first prototype (kept for its checks). A stage never mixes with the wave formula.
public sealed class DadThrowModel
{
    public const float Width=1280,Height=720;
    // Radius of the thrown thing itself in the hit test (a person adds 25).
    public const float GolfHit=11,ShoeHit=22;
    // Degrees between the balls of a fan when more than one leaves the hand (run pick).
    public const float FanDegrees=7;
    public enum Weapon { Golf, Shoe }
    public enum Phase { Playing, WaveClear, Lost }
    public sealed class Enemy { public int id,hp; public float x,speed,slow,flash,along; public DadThrowStage.Kind kind; }
    public sealed class Projectile
    {
        public Weapon weapon;public Vector2 pos,vel;public int remaining,bounces;public float life;
        public readonly HashSet<int> hit=new HashSet<int>();
    }
    public readonly DadThrowField Field;public readonly DadThrowStage Stage;
    public readonly List<Enemy> enemies=new List<Enemy>();
    public readonly List<Projectile> projectiles=new List<Projectile>();
    public Phase phase=Phase.Playing;
    public int Wave {get;private set;} public int Health {get;private set;}
    public int Coins {get;private set;} public int Level {get;private set;}
    public int Spawned {get;private set;} public int Killed {get;private set;} public int Escaped {get;private set;}
    // Everyone still to deal with: not yet walked in, plus those on the street.
    public int Remaining {get{return Total-Killed-Escaped;}}
    public int BestChain {get;private set;} public float Reload {get;private set;} public float ReloadTotal {get;private set;}
    public float Clock {get;private set;}
    // The latest golf ball to reach three or more people, for the small "×3" mark. Serial changes on every new count.
    public int ChainSerial {get;private set;} public int ChainCount {get;private set;} public Vector2 ChainAt {get;private set;}
    public int Total {get{return Stage!=null?Stage.Total:12+Math.Min(18,Wave-1)*4;}}
    public float Interval {get{return Math.Max(.22f,.8f-(Wave-1)*.045f);}}
    public int UpgradeCost {get{return 30+Level*25;}}
    public bool CanFire {get{return phase==Phase.Playing&&Reload<=0&&projectiles.Count<6*Math.Max(1,Rules.GolfCount);}}
    public DadThrowRules Rules=new DadThrowRules();
    float spawnClock;int nextId,nextGroup;
    readonly List<Enemy> touched=new List<Enemy>();
    static readonly Comparison<Enemy> NearestFirst=(a,b)=>{int c=a.along.CompareTo(b.along);return c!=0?c:a.id.CompareTo(b.id);};

    public DadThrowModel(DadThrowField field,int wave=1)
    {if(field==null)throw new ArgumentNullException("field");Field=field;Wave=Math.Max(1,wave);Health=5;}
    public DadThrowModel(DadThrowField field,DadThrowStage stage)
    {
        if(field==null)throw new ArgumentNullException("field");if(stage==null)throw new ArgumentNullException("stage");
        Field=field;Stage=stage;Wave=1;Health=stage.health;
    }
    // One wave of a run (DadThrowRun): the gate keeps the touches it has left, the throws keep what was picked.
    public DadThrowModel(DadThrowField field,DadThrowStage stage,int health,DadThrowRules rules):this(field,stage)
    {if(rules==null)throw new ArgumentNullException("rules");Health=Math.Max(1,health);Rules=rules;}
    // Growth between waves belongs to the wave prototype. Stage rewards are not connected yet (plan §15).
    public bool Upgrade(){if(Stage!=null||phase!=Phase.WaveClear||Level>=5||Coins<UpgradeCost)return false;Coins-=UpgradeCost;Level++;return true;}
    public bool NextWave(){if(Stage!=null||phase!=Phase.WaveClear)return false;Wave++;Spawned=Killed=Escaped=0;spawnClock=Reload=0;phase=Phase.Playing;return true;}
    public Enemy AddEnemy(float x,int hp=1,float speed=-1)
    {var e=new Enemy{id=++nextId,x=x,hp=hp,speed=speed>0?speed:25+Math.Min(Wave-1,20)*2};enemies.Add(e);return e;}
    public static Vector2 LaunchVelocity(Vector2 pull)
    {
        // Forward throws only. Up/down dragging still changes the arc; minimum forward motion avoids accidental self-shots.
        Vector2 v=Vector2.ClampMagnitude(pull,150)*6;
        v.x=Mathf.Max(180,v.x);v.y=Mathf.Clamp(v.y,-720,260);return v;
    }
    public bool Fire(Weapon weapon,Vector2 pull)
    {
        if(!CanFire||pull.magnitude<18)return false;
        Vector2 launch=LaunchVelocity(pull);
        projectiles.Add(new Projectile{weapon=weapon,pos=Field.Muzzle,vel=launch,remaining=weapon==Weapon.Golf?Rules.Pierce+Level:1});
        // Extra balls (run pick) leave with the aimed one, fanned a little above and below it: 7 degrees up, 7 down, 14 up...
        for(int extra=1;weapon==Weapon.Golf&&extra<Rules.GolfCount;extra++)
        {
            float turn=(extra+1)/2*FanDegrees*(extra%2==1?-1:1)*Mathf.Deg2Rad,cos=Mathf.Cos(turn),sin=Mathf.Sin(turn);
            projectiles.Add(new Projectile{weapon=weapon,pos=Field.Muzzle,vel=new Vector2(launch.x*cos-launch.y*sin,launch.x*sin+launch.y*cos),remaining=Rules.Pierce+Level});
        }
        Reload=ReloadTotal=weapon==Weapon.Golf?Rules.GolfReload:Rules.ShoeReload;return true;
    }
    public static bool SweptHit(Vector2 from,Vector2 to,Vector2 point,float radius)
    {Vector2 d=to-from;float t=d.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector2.Dot(point-from,d)/d.sqrMagnitude);return (point-(from+d*t)).sqrMagnitude<=radius*radius;}
    // Free flight from the hand to the first landing: the same steps a thrown thing takes, and no further,
    // so the dots never promise where a bounce will go.
    public List<Vector2> Predict(Weapon weapon,Vector2 pull,List<Vector2> path)
    {
        path.Clear();Vector2 p=Field.Muzzle,v=LaunchVelocity(pull);float radius=weapon==Weapon.Golf?GolfHit:ShoeHit;
        for(int i=0;i<240;i++)
        {
            if(i%3==0)path.Add(p);v.y+=760*(1/120f);p+=v*(1/120f);
            if(p.y>=Field.Ground-radius){p.y=Field.Ground-radius;path.Add(p);break;}
            if(p.x>Width)break;
        }
        return path;
    }
    public void Tick(float elapsed)
    {
        if(phase!=Phase.Playing)return;
        // A bounded fixed-step integrator keeps low-FPS devices from skipping a fast golf-ball collision.
        float remaining=Mathf.Clamp(elapsed,0,.25f);
        while(remaining>0&&phase==Phase.Playing){float dt=Mathf.Min(remaining,1/120f);Step(dt);remaining-=dt;}
    }
    void Arrive(float dt)
    {
        if(Stage!=null)
        {
            // The stage table is the whole schedule: its people, its speeds, its health. Nothing escalates on its own.
            Clock+=dt;
            while(nextGroup<Stage.groups.Count&&Stage.groups[nextGroup].at<=Clock)
            {
                DadThrowStage.Group g=Stage.groups[nextGroup++];
                for(int i=0;i<g.count;i++)AddEnemy(g.firstX+g.spacing*i,g.hp,g.speed).kind=g.kind;
                Spawned+=g.count;
            }
            return;
        }
        spawnClock-=dt;
        if(Spawned<Total&&spawnClock<=0&&enemies.Count<80)
        {AddEnemy(Wave==1&&Spawned<3?750+Spawned*100:Width+20,Wave>=5&&Spawned%6==0?2:1);Spawned++;spawnClock+=Interval;}
    }
    void Step(float dt)
    {
        Reload=Mathf.Max(0,Reload-dt);Arrive(dt);
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Enemy e=enemies[i];e.slow=Mathf.Max(0,e.slow-dt);e.flash=Mathf.Max(0,e.flash-dt);
            e.x-=e.speed*(e.slow>0?Rules.SlowFactor:1)*dt;
            // Losing the house outranks clearing the street: the last person walking in at one health is a defeat.
            if(e.x<=Field.Gate){enemies.RemoveAt(i);Escaped++;Health--;if(Health<=0){phase=Phase.Lost;projectiles.Clear();return;}}
        }
        for(int i=projectiles.Count-1;i>=0;i--)
        {
            Projectile p=projectiles[i];p.life+=dt;Vector2 from=p.pos;p.vel.y+=760*dt;p.pos+=p.vel*dt;
            float radius=p.weapon==Weapon.Golf?GolfHit:ShoeHit;
            if(p.pos.y>=Field.Ground-radius)
            {
                p.pos.y=Field.Ground-radius;
                if(p.weapon==Weapon.Golf&&p.bounces++<Rules.MaxBounces)p.vel.y=-Mathf.Abs(p.vel.y)*.62f;
                else {projectiles.RemoveAt(i);continue;}
            }
            // Everyone this step's flight crosses, taken in the order the flight reaches them. Without the order a
            // ball that meets more people than it can pierce would skip the nearest and hit whoever was listed last.
            touched.Clear();Vector2 flight=p.pos-from;float length=flight.sqrMagnitude;
            foreach(Enemy e in enemies)
            {
                Vector2 body=new Vector2(e.x,Field.Ground-35);
                if(p.hit.Contains(e.id)||!SweptHit(from,p.pos,body,radius+25))continue;
                e.along=length<.001f?0:Vector2.Dot(body-from,flight)/length;touched.Add(e);
            }
            if(touched.Count>1)touched.Sort(NearestFirst);
            foreach(Enemy e in touched)
            {
                if(p.weapon==Weapon.Shoe)
                {
                    // Knock the leading group back into its followers. This changes spacing, not just damage.
                    // The spot is taken before the struck person can be removed, so the push still happens around it.
                    float center=e.x;
                    for(int k=enemies.Count-1;k>=0;k--)
                    {
                        Enemy target=enemies[k];if(Mathf.Abs(target.x-center)>=Rules.ShoeRadius)continue;
                        target.x=Mathf.Min(Width+40,target.x+Rules.ShoePush);target.slow=Rules.SlowTime;target.flash=.16f;
                        // A blasting shoe (run pick) also hurts everyone it gathers. The struck one is settled just below.
                        if(Rules.ShoeBlast>0&&target!=e){target.hp-=Rules.ShoeBlast;if(target.hp<=0){enemies.RemoveAt(k);Killed++;}}
                    }
                }
                p.hit.Add(e.id);p.remaining--;e.hp-=p.weapon==Weapon.Shoe?Rules.ShoeDamage+Rules.ShoeBlast:Rules.GolfDamage+Level/3;e.flash=.16f;
                if(e.hp<=0){enemies.Remove(e);Killed++;if(Stage==null)Coins+=2;}
                BestChain=Math.Max(BestChain,p.hit.Count);
                // A ball through a whole pack readies the hand at once (run pick): the reward for waiting for a pack.
                if(p.weapon==Weapon.Golf&&Rules.ChainReload>0&&p.hit.Count==Rules.ChainReload)Reload=0;
                if(p.weapon==Weapon.Golf&&p.hit.Count>=3){ChainSerial++;ChainCount=p.hit.Count;ChainAt=p.pos;}
                if(p.remaining<=0)break;
            }
            if(p.remaining<=0||p.life>6||p.pos.x>Width+80||p.pos.x<0)projectiles.RemoveAt(i);
        }
        // Cleared only when nobody is left on the schedule or on the street.
        if(Spawned>=Total&&enemies.Count==0){phase=Phase.WaveClear;if(Stage==null)Coins+=10+Wave*2;projectiles.Clear();}
    }
}
