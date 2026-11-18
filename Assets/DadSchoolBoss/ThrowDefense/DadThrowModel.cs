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
    public bool Dodgeball, SportsCar;
    public int Pierce=12,MaxBounces=0,ShoeDamage=0;
    public float GolfReload=.3f,ShoeReload=.7f,ShoeRadius=85,ShoePush=105,SlowTime=2.5f,SlowFactor=.35f;
    // How fast the street runs while the player is pulling back. 1 is full speed. The first prototype slowed to
    // 0.35, which kept the whole fight in slow motion for anyone who throws without pause.
    public float AimPace=1;
    // What the shop can change (DadThrowShop). With nothing bought these defaults leave every throw as it was:
    // one ball, one point of hurt, a shoe that only pushes, a hand that always waits its reload.
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
    public float BallRadius { get { return Rules.SportsCar ? 26 : Rules.Dodgeball ? 20 : GolfHit; } }
    // Degrees between the balls of a fan when more than one leaves the hand.
    public const float FanDegrees=7;
    public enum Weapon { Golf, Shoe }
    public enum Phase { Playing, WaveClear, Lost }
    // What a boss is doing. Everyone else only ever walks.
    public enum Mood { Walk, Brace, Dash, Dazed, Roar, Back }
    // guard: holding a tray, or a boss braced for his dash. A ball stops on it; a shoe knocks it away.
    // full: the health he came with. mood, moodClock, raged: a boss's state (BossStep).
    public sealed class Enemy
    {
        public int id,hp,full; public float x,speed,slow,flash,along,moodClock; public DadThrowStage.Kind kind; public bool guard,raged;
        public bool IsChair { get { return kind!=null && kind.id=="tough"; } }
        public bool OffBalance { get { return IsChair && slow>0; } } public Mood mood;
        public bool IsChief {get{return kind!=null&&kind.boss!=null;}}
    }
    // A chapter's boss (2026-11-18, Design/dad-throw-content-strategy-v2.md). He walks in slowly. Every BossEvery
    // seconds he braces for BossBrace seconds and then dashes for BossDash seconds at BossDashPace times his walk;
    // from the moment he braces until the dash is over a ball stops on him as it does on a tray. A shoe that
    // reaches him then takes his feet from under him: he stands dazed for BossDazed seconds and a ball hurts him
    // BossDazedHurt times over. A shoe pushes him BossShove as far as it pushes anyone. At half his health he
    // loses his temper once (BossRoar seconds on the spot); after that he walks BossRagedPace as fast and braces
    // every BossEveryRaged seconds. Touching the gate costs a touch like anyone's, but he is not out of it: he
    // trots BossBack back out and comes again.
    public const float BossEvery=4,BossEveryRaged=2.8f,BossBrace=.9f,BossDash=1,BossDashPace=5,BossDazed=2.5f,BossRoar=.8f,
        BossRagedPace=1.25f,BossBack=460,BossBackPace=6,BossShove=.5f,BossInView=60;
    public const int BossDazedHurt=2;
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
    // The latest ball a tray stopped, for the clang on the street and the nudge toward the shoe.
    public int BlockSerial {get;private set;} public Vector2 BlockAt {get;private set;}
    // Shoes thrown in this fight, for telling a player who never reaches for one (DadThrowTuner).
    public int ShoesThrown {get;private set;}
    // Won paid on top of the head count: what the bosses sent back were worth.
    public int Bounty {get;private set;}
    // The boss on the street, if one is.
    public Enemy Chief {get{foreach(Enemy e in enemies)if(e.IsChief)return e;return null;}}
    // Counts every time a shoe takes a braced or dashing boss off his feet, and where.
    public int DazeSerial {get;private set;}
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
    // Growth between waves belongs to the wave prototype. Stage rewards are not connected yet (plan §15).
    public bool Upgrade(){if(Stage!=null||phase!=Phase.WaveClear||Level>=5||Coins<UpgradeCost)return false;Coins-=UpgradeCost;Level++;return true;}
    public bool NextWave(){if(Stage!=null||phase!=Phase.WaveClear)return false;Wave++;Spawned=Killed=Escaped=0;spawnClock=Reload=0;phase=Phase.Playing;return true;}
    public Enemy AddEnemy(float x,int hp=1,float speed=-1)
    {var e=new Enemy{id=++nextId,x=x,hp=hp,full=hp,speed=speed>0?speed:25+Math.Min(Wave-1,20)*2};enemies.Add(e);return e;}
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
        // Extra balls (bought in the shop) leave with the aimed one, fanned a little above and below it: 7 degrees up, 7 down, 14 up...
        for(int extra=1;weapon==Weapon.Golf&&extra<Rules.GolfCount;extra++)
        {
            float turn=(extra+1)/2*FanDegrees*(extra%2==1?-1:1)*Mathf.Deg2Rad,cos=Mathf.Cos(turn),sin=Mathf.Sin(turn);
            projectiles.Add(new Projectile{weapon=weapon,pos=Field.Muzzle,vel=new Vector2(launch.x*cos-launch.y*sin,launch.x*sin+launch.y*cos),remaining=Rules.Pierce+Level});
        }
        if(weapon==Weapon.Shoe)ShoesThrown++;
        Reload=ReloadTotal=weapon==Weapon.Golf?Rules.GolfReload:Rules.ShoeReload;return true;
    }
    // Half the width of a standing body, and how tall one stands when no picture says (checks, the wave formula).
    public const float BodyHalf=25,BodyTall=107;
    // Whether this step of a flight touches a standing body: three circles from the shins to the top of the head,
    // each as wide as the body. Until 2026-11-17 a body was one circle at the belly, so a ball that met the head
    // flew through (owner's play: "hit the head and they do not go down").
    // half: half the body's width; a boss is wider than the rest.
    public static bool SweptBody(Vector2 from,Vector2 to,float x,float feet,float tall,float radius,float half=BodyHalf)
    {
        float reach=radius+half,top=Mathf.Max(half,tall-half);
        if(Mathf.Max(from.x,to.x)<x-reach||Mathf.Min(from.x,to.x)>x+reach)return false;
        for(int i=0;i<3;i++)if(SweptHit(from,to,new Vector2(x,feet-Mathf.Lerp(half,top,i*.5f)),reach))return true;
        return false;
    }
    public static bool SweptHit(Vector2 from,Vector2 to,Vector2 point,float radius)
    {Vector2 d=to-from;float t=d.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector2.Dot(point-from,d)/d.sqrMagnitude);return (point-(from+d*t)).sqrMagnitude<=radius*radius;}
    // Free flight from the hand to the first landing: the same steps a thrown thing takes, and no further,
    // so the dots never promise where a bounce will go.
    public List<Vector2> Predict(Weapon weapon,Vector2 pull,List<Vector2> path)
    {
        path.Clear();Vector2 p=Field.Muzzle,v=LaunchVelocity(pull);float radius=weapon==Weapon.Golf?BallRadius:ShoeHit;
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
                for(int i=0;i<g.count;i++){Enemy e=AddEnemy(g.firstX+g.spacing*i,g.hp,g.speed);e.kind=g.kind;e.guard=g.kind!=null&&g.kind.guards;}
                Spawned+=g.count;
            }
            return;
        }
        spawnClock-=dt;
        if(Spawned<Total&&spawnClock<=0&&enemies.Count<80)
        {AddEnemy(Wave==1&&Spawned<3?750+Spawned*100:Width+20,Wave>=5&&Spawned%6==0?2:1);Spawned++;spawnClock+=Interval;}
    }
    void BossStep(Enemy e,float dt)
    {
        e.moodClock+=dt;float pace=e.speed*(e.raged?BossRagedPace:1);
        switch(e.mood)
        {
            case Mood.Walk:
                e.x-=pace*(e.slow>0?Rules.SlowFactor:1)*dt;
                // He braces only where the player can see him do it.
                if(e.moodClock>=(e.raged?BossEveryRaged:BossEvery)&&e.x<=Width-BossInView){e.mood=Mood.Brace;e.moodClock=0;e.guard=true;}
                break;
            case Mood.Brace:if(e.moodClock>=BossBrace){e.mood=Mood.Dash;e.moodClock=0;}break;
            case Mood.Dash:e.x-=pace*BossDashPace*dt;if(e.moodClock>=BossDash){e.mood=Mood.Walk;e.moodClock=0;e.guard=false;}break;
            case Mood.Dazed:if(e.moodClock>=BossDazed){e.mood=Mood.Walk;e.moodClock=0;}break;
            case Mood.Roar:if(e.moodClock>=BossRoar){e.mood=Mood.Walk;e.moodClock=0;}break;
            case Mood.Back:e.x+=e.speed*BossBackPace*dt;if(e.x>=Field.Gate+BossBack){e.mood=Mood.Walk;e.moodClock=0;}break;
        }
    }
    // Takes `amount` off someone's health; true when that sends them back. A boss brought to half his health
    // loses his temper, once.
    bool Hurt(Enemy e,int amount)
    {
        e.hp-=amount;
        if(e.hp<=0){Killed++;if(e.IsChief)Bounty+=e.kind.boss.bounty;return true;}
        if(e.IsChief&&!e.raged&&e.hp*2<=e.full){e.raged=true;if(e.mood!=Mood.Back){e.mood=Mood.Roar;e.moodClock=0;e.guard=false;}}
        return false;
    }
    void Step(float dt)
    {
        Reload=Mathf.Max(0,Reload-dt);Arrive(dt);
        for(int i=enemies.Count-1;i>=0;i--)
        {
            Enemy e=enemies[i];e.slow=Mathf.Max(0,e.slow-dt);e.flash=Mathf.Max(0,e.flash-dt);
            if(e.IsChief)BossStep(e,dt);
            else
            {
                bool charging=e.IsChair && e.hp<e.full && !e.OffBalance;
                e.x-=e.speed*(e.slow>0?Rules.SlowFactor:charging?1.5f:1f)*dt;
            }
            if(e.x>Field.Gate)continue;
            // A boss takes his touch and trots back out to come again; anyone else is done for this stage.
            if(e.IsChief){e.x=Field.Gate+.5f;e.mood=Mood.Back;e.moodClock=0;e.guard=false;}else{enemies.RemoveAt(i);Escaped++;}
            // Losing the house outranks clearing the street: the last person walking in at one health is a defeat.
            Health--;if(Health<=0){phase=Phase.Lost;projectiles.Clear();return;}
        }
        for(int i=projectiles.Count-1;i>=0;i--)
        {
            Projectile p=projectiles[i];p.life+=dt;Vector2 from=p.pos;p.vel.y+=760*dt;p.pos+=p.vel*dt;
            float radius=p.weapon==Weapon.Golf?BallRadius:ShoeHit;
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
                if(p.hit.Contains(e.id)||!SweptBody(from,p.pos,e.x,Field.Ground,e.kind!=null?e.kind.Height(e.id):BodyTall,radius,e.IsChief?e.kind.boss.half:BodyHalf))continue;
                Vector2 body=new Vector2(e.x,Field.Ground-35);
                e.along=length<.001f?0:Vector2.Dot(body-from,flight)/length;touched.Add(e);
            }
            if(touched.Count>1)touched.Sort(NearestFirst);
            foreach(Enemy e in touched)
            {
                // A tray stops a ball dead: the one holding it is not hurt and nobody behind is reached. However
                // strong the ball has been made, only a shoe gets the tray out of the hand.
                if(p.weapon==Weapon.Golf&&e.guard){p.hit.Add(e.id);p.remaining=0;e.flash=.16f;BlockSerial++;BlockAt=p.pos;break;}
                if(p.weapon==Weapon.Shoe)
                {
                    // Knock the leading group back into its followers. This changes spacing, not just damage.
                    // The spot is taken before the struck person can be removed, so the push still happens around it.
                    float center=e.x;
                    for(int k=enemies.Count-1;k>=0;k--)
                    {
                        Enemy target=enemies[k];if(Mathf.Abs(target.x-center)>=Rules.ShoeRadius)continue;
                        // A boss is heavy and goes half as far. Caught braced or dashing, his feet go from under him.
                        target.x=Mathf.Min(Width+40,target.x+Rules.ShoePush*(target.IsChief?BossShove:1));target.slow=Rules.SlowTime;target.flash=.16f;
                        if(target.IsChief&&target.guard){target.mood=Mood.Dazed;target.moodClock=0;DazeSerial++;}
                        target.guard=false;
                        // A blasting shoe (bought in the shop) also hurts everyone it gathers. The struck one is settled just below.
                        if(Rules.ShoeBlast>0&&target!=e&&Hurt(target,Rules.ShoeBlast))enemies.RemoveAt(k);
                    }
                }
                p.hit.Add(e.id);p.remaining--;e.flash=.16f;
                // A ball hurts a dazed boss several times over.
                bool followUp=e.OffBalance && p.weapon==Weapon.Golf;
                int damage=p.weapon==Weapon.Shoe?Rules.ShoeDamage+Rules.ShoeBlast:Rules.GolfDamage+Level/3;
                if(followUp)damage*=2;
                if(e.mood==Mood.Dazed && p.weapon==Weapon.Golf)damage*=BossDazedHurt;
                if(Hurt(e,damage))
                {
                    enemies.Remove(e);
                    if(followUp && Stage!=null)Bounty+=2;
                    if(Stage==null)Coins+=2;
                }
                BestChain=Math.Max(BestChain,p.hit.Count);
                // A ball through a whole pack readies the hand at once (bought in the shop): the reward for waiting for a pack.
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
