using System.Collections.Generic;
using UnityEngine;

// Prototype B, "아빠가 직접 날아간다" (Design/dad-war-slingshot-v1.md): pull dad back and let go; he flies, bounces off the
// lockers and bowls the other dads over, who carry the hit on into walls and into each other. Meanwhile more dads walk in
// from the door toward wherever dad landed. Pure simulation (no input, no drawing) so editor checks and the balance bot
// can run it headless. Body sizes, the tray's front guard and the boss's "only a thrown dad opens him up" rule follow
// AlleyCombat so the existing art and lessons carry over.
public sealed class SlingshotSim
{
    public enum Phase { Ready, Flying, Won, Lost }
    public enum Kind { Gum, Chair, Tray, Boss, Dodge }

    public sealed class Dad
    {
        public Vector2 pos, vel;
        public float health = 100, maxHealth = 100, flightTime, hurt, landed;
        public bool flying, rage;   // rage: this shot is the reward for a 5-chain
        public int bounces;         // locker bounces so far this shot (bank-shot bonus, up to 3)
        public int bounceSide = -1; // the side wall he last bounced off (0 left, 1 right), for the wall-bounce pose
        public float bounceClock;   // how long ago that was, counting down from the pose time
    }
    public sealed class Foe
    {
        public int id, hp, maxHp, trayHits, facing = -1, lastShot = -1, heavyHits;
        public Kind kind;
        public bool shield, trayBroken, enraged, elite;   // elite: a big-dad, too heavy to send flying with one hit
        public Vector2 pos, vel, aim, target, face = Vector2.down;   // face: the side the tray covers, kept turned toward dad
        public float stun, flash, wallCooldown, contact, windup, dash, cooldown, groggy, air, walked, attackPose, heavyClock, scale = 1;
        public bool Boss { get { return kind == Kind.Boss; } }
        public bool Flying { get { return vel.sqrMagnitude > MinFlySpeed * MinFlySpeed; } }
        public float Radius { get { return (kind == Kind.Boss ? 38 : kind == Kind.Chair ? 30 : kind == Kind.Tray ? 27 : 25) * (elite ? 1.3f : 1) * scale; } }
        public float Lift { get { return Flying ? 14 * scale * Mathf.Clamp01(air / .12f) : 0; } }   // airborne bodies ride a little above their shadow
    }
    public sealed class Spawn { public float at, x; public Kind kind; public bool elite; }
    // A dodgeball in the air: thrown at where dad stood, or batted back by a flying dad into the crowd.
    public sealed class Ball { public Vector2 pos, vel; public float life = 2.6f; public bool returned; public int hits, lastFoe = -1; }
    public sealed class Stage
    {
        public string label, title, lesson;
        public int par;                       // shots for the third star
        public float speedScale = 1;
        public bool bossEscorts;              // the boss keeps calling more dads in while he stands
        public int floor = 1;                 // which floor's boss art and lines (main game: 1 corridor, 2 cafeteria, 3 gym)
        public int bossHp = 12, eliteHp = 8;  // tougher on higher floors in the main game
        public string bossBanner = "학부모회장 아빠 등장!  단톡방 방장이다", bossEnrage = "회장님이 단톡방을 나갔습니다";
        public string eliteBanner = "지호 큰아빠 등장!  무거워요, 두 번 들이받으세요";
        public readonly List<Spawn> spawns = new List<Spawn>();
    }
    public sealed class Event
    {
        public string text, sound; public Vector2 pos; public int style;   // style 0 small, 1 medium, 2 big, 3 banner
        public int dentSide = -1;                                          // a side-wall slam: 0 left, 1 right
    }
    public sealed class Corpse { public Kind kind; public bool elite; public Vector2 pos, vel; public float life = .6f; public int facing; }

    public const float DadRadiusBase = 26, MinFlySpeed = 60, PullDeadZone = 24, PullMax = 150;
    public const float LaunchMin = 420, LaunchMax = 1150, DadDrag = 1.3f, DadBounce = .88f, StopSpeed = 80, MaxFlight = 2.4f;
    const float FoeDrag = 3.0f, BossDrag = 1.8f, FoeWallBounce = .35f, ChainSpeed = 230, BossGroggy = 2.6f;

    public readonly Rect arena;
    public readonly float scale;   // body size against the art: 1 = the battle size of the main prototype (design: 0.72 wide view)
    public float DadRadius { get { return DadRadiusBase * scale; } }
    public readonly Stage stage;
    public readonly Dad dad = new Dad();
    public readonly List<Foe> foes = new List<Foe>();
    public readonly List<Event> events = new List<Event>();
    public readonly List<Corpse> corpses = new List<Corpse>();
    public readonly List<Ball> balls = new List<Ball>();
    public Phase phase = Phase.Ready;
    public bool ragePending;   // earned by a 5-chain: the next shot is a rage shot
    // Growth from the main game (change request C-08 draft): 들이받기 = how hard dad's hit sends a dad, 도움닫기 = launch
    // speed, 멀리 날리기 = how far a hit dad flies and extra wall damage. Health comes in through dad.maxHealth.
    public float knockBonus = 1, launchBonus = 1, throwBonus = 1;
    public int wallBonus;
    public void SetHealth(float max) { dad.maxHealth = dad.health = Mathf.Max(1, max); }
    public int shots, kills, wallSlams, shotChain, bestChain;
    public float elapsed, freeze;
    readonly System.Random rng;
    int nextSpawn, nextId, escortsCalled;
    float escortClock;

    public SlingshotSim(Stage stage, Rect arena, int seed = 1, float scale = 1)
    {
        this.stage = stage; this.arena = arena; this.scale = scale; rng = new System.Random(seed);
        dad.pos = new Vector2(arena.center.x, arena.yMax - 40);
        stage.spawns.Sort((a, b) => a.at.CompareTo(b.at));
    }
    public bool Over { get { return phase == Phase.Won || phase == Phase.Lost; } }
    public int AliveCount { get { int n = 0; foreach (Foe f in foes) if (f.hp > 0) n++; return n; } }
    public int Remaining { get { return AliveCount + stage.spawns.Count - nextSpawn; } }
    public Foe Boss { get { foreach (Foe f in foes) if (f.Boss && f.hp > 0) return f; return null; } }
    public int Stars { get { return phase != Phase.Won ? 0 : 1 + (dad.health >= dad.maxHealth * .5f ? 1 : 0) + (shots <= stage.par ? 1 : 0); } }

    // ---- input ----------------------------------------------------------------------------------------------------
    public static Vector2 LaunchVelocity(Vector2 pull)
    {
        float power = Mathf.Clamp01(pull.magnitude / PullMax);
        return pull.normalized * Mathf.Lerp(LaunchMin, LaunchMax, power);
    }
    // pull = direction and length the player dragged (already pointing where dad should fly).
    public bool Launch(Vector2 pull)
    {
        if (phase != Phase.Ready || pull.magnitude < PullDeadZone) return false;
        dad.rage = ragePending; ragePending = false; dad.bounces = 0;
        dad.vel = LaunchVelocity(pull) * (dad.rage ? 1.15f : 1) * launchBonus; dad.flying = true; dad.flightTime = 0;
        shots++; shotChain = 0; phase = Phase.Flying;
        events.Add(new Event { text = dad.rage ? "분노 발사!" : "", style = 2, sound = dad.rage ? "sfx_boss_enrage" : "sfx_launch_whoosh", pos = dad.pos });
        return true;
    }
    // The aiming guide: straight to the first wall, then a short stretch after the bounce. Bodies are not shown; reading
    // the crowd is the player's job.
    public static List<Vector2> Predict(Vector2 from, Vector2 pull, Rect arena, float dadRadius)
    {
        var points = new List<Vector2> { from };
        if (pull.magnitude < PullDeadZone) return points;
        Vector2 v = LaunchVelocity(pull), dir = v.normalized, p = from;
        float budget = v.magnitude / DadDrag * .9f, after = -1;
        Rect inner = new Rect(arena.xMin + dadRadius, arena.yMin + dadRadius, arena.width - 2 * dadRadius, arena.height - 2 * dadRadius);
        for (float travelled = 0; travelled < budget; travelled += 6)
        {
            p += dir * 6;
            bool hitX = p.x < inner.xMin || p.x > inner.xMax, hitY = p.y < inner.yMin || p.y > inner.yMax;
            if (hitX || hitY)
            {
                p = new Vector2(Mathf.Clamp(p.x, inner.xMin, inner.xMax), Mathf.Clamp(p.y, inner.yMin, inner.yMax));
                points.Add(p);
                if (after >= 0) break;
                if (hitX) dir.x = -dir.x; if (hitY) dir.y = -dir.y;
                after = 0;
            }
            if (after >= 0 && (after += 6) > 110) break;
        }
        points.Add(p);
        return points;
    }

    // ---- simulation -----------------------------------------------------------------------------------------------
    public void Tick(float dt)
    {
        if (dt <= 0) return;
        if (Over) { UpdateCorpses(Mathf.Min(dt, .1f)); return; }   // the last bodies keep flying under the finisher
        float remaining = Mathf.Min(dt, .1f);
        while (remaining > 1e-6f && !Over)
        {
            if (freeze > 0) { float held = Mathf.Min(freeze, remaining); freeze -= held; remaining -= held; continue; }
            float step = Mathf.Min(remaining, 1 / 120f); Step(step); remaining -= step;
        }
    }
    void Step(float dt)
    {
        elapsed += dt;
        SpawnDue(dt);
        UpdateDad(dt);
        foreach (Foe f in foes) if (f.hp > 0) UpdateFoe(f, dt);
        DadContacts();
        FoeContacts(dt);
        UpdateBalls(dt);
        UpdateCorpses(dt);
        for (int i = foes.Count - 1; i >= 0; i--)
        {
            Foe f = foes[i]; if (f.hp > 0) continue;
            foes.RemoveAt(i); kills++;
            corpses.Add(new Corpse { kind = f.kind, elite = f.elite, pos = f.pos, vel = f.vel.sqrMagnitude > 1 ? f.vel * .6f : new Vector2(f.facing * -120, -160), facing = f.facing });
            events.Add(new Event { text = "제압", pos = f.pos, style = 0 });
            if (f.Boss) { phase = Phase.Won; events.Add(new Event { text = "의리 증명 완료!", pos = f.pos, style = 3, sound = "jingle_win" }); return; }
        }
        if (dad.health <= 0) { phase = Phase.Lost; events.Add(new Event { text = "", sound = "jingle_lose", pos = dad.pos }); return; }
        if (nextSpawn >= stage.spawns.Count && AliveCount == 0 && !stage.bossEscorts) { phase = Phase.Won; events.Add(new Event { text = "", sound = "jingle_win", pos = dad.pos }); }
    }
    void UpdateCorpses(float dt)
    {
        for (int i = corpses.Count - 1; i >= 0; i--) { Corpse c = corpses[i]; c.life -= dt; c.pos += c.vel * dt; c.vel *= Mathf.Exp(-2 * dt); if (c.life <= 0) corpses.RemoveAt(i); }
    }
    void SpawnDue(float dt)
    {
        while (nextSpawn < stage.spawns.Count && stage.spawns[nextSpawn].at <= elapsed && AliveCount < 8) { Spawn s = stage.spawns[nextSpawn++]; Add(s.kind, s.x, -1, s.elite); }
        // Boss stage: while the boss stands, dads keep coming (they are the ammunition that opens him up).
        if (!stage.bossEscorts || Boss == null) return;
        escortClock -= dt;
        if (escortClock <= 0 && AliveCount < 4)
        {
            escortClock = 3.2f; int n = escortsCalled++;
            Add(n % 3 == 2 ? Kind.Dodge : Kind.Gum, arena.center.x + (n % 2 == 0 ? -46 : 46));   // every third is a dodgeball dad: his ball, batted back, opens the boss too
        }
    }
    public Foe Add(Kind kind, float x, float y = -1, bool elite = false)
    {
        var f = new Foe { id = nextId++, kind = kind, elite = elite, scale = scale, pos = new Vector2(x, y < 0 ? arena.yMin + 22 * scale : y) };
        f.maxHp = f.hp = kind == Kind.Boss ? stage.bossHp : elite ? stage.eliteHp : kind == Kind.Tray ? 3 : 2;
        f.shield = kind == Kind.Tray;
        f.cooldown = .5f + (float)rng.NextDouble() * .5f;   // a short grace so a fresh arrival does not swing at once
        foes.Add(f);
        if (kind == Kind.Boss) events.Add(new Event { text = stage.bossBanner, pos = f.pos, style = 3, sound = "sfx_boss_enrage" });
        if (elite) events.Add(new Event { text = stage.eliteBanner, pos = f.pos, style = 3, sound = "sfx_boss_enrage" });
        return f;
    }

    void UpdateDad(float dt)
    {
        dad.hurt = Mathf.Max(0, dad.hurt - dt); dad.landed = Mathf.Max(0, dad.landed - dt); dad.bounceClock = Mathf.Max(0, dad.bounceClock - dt);
        if (!dad.flying) return;
        dad.flightTime += dt;
        Vector2 p = dad.pos + dad.vel * dt; bool bumped = false;
        if (p.x < arena.xMin + DadRadius) { p.x = arena.xMin + DadRadius; dad.vel.x = Mathf.Abs(dad.vel.x) * DadBounce; bumped = true; dad.bounceSide = 0; dad.bounceClock = .09f; }
        if (p.x > arena.xMax - DadRadius) { p.x = arena.xMax - DadRadius; dad.vel.x = -Mathf.Abs(dad.vel.x) * DadBounce; bumped = true; dad.bounceSide = 1; dad.bounceClock = .09f; }
        if (p.y < arena.yMin + DadRadius) { p.y = arena.yMin + DadRadius; dad.vel.y = Mathf.Abs(dad.vel.y) * DadBounce; bumped = true; }
        if (p.y > arena.yMax - DadRadius) { p.y = arena.yMax - DadRadius; dad.vel.y = -Mathf.Abs(dad.vel.y) * DadBounce; bumped = true; }
        if (bumped)
        {
            dad.bounces = Mathf.Min(3, dad.bounces + 1);
            events.Add(new Event { text = dad.bounces == 1 ? "" : "튕김 x" + dad.bounces, pos = p, style = 1, sound = "sfx_wall_boing" });
        }
        dad.pos = p; dad.vel *= Mathf.Exp(-DadDrag * dt);
        if (dad.vel.magnitude < StopSpeed || dad.flightTime > MaxFlight) Land();
    }
    void Land()
    {
        dad.flying = false; dad.vel = Vector2.zero; dad.landed = .18f; dad.bounceClock = 0;
        events.Add(new Event { text = "", pos = dad.pos, sound = "sfx_landing_skid" });
        if (!Over) phase = Phase.Ready;
        if (shotChain >= 3) events.Add(new Event { text = shotChain + "연쇄!!", pos = dad.pos, style = 2, sound = "sfx_chain" });
        if (shotChain >= 5 && !Over) { ragePending = true; events.Add(new Event { text = "아빠 화났다!  다음 발은 분노 발사", pos = dad.pos, style = 3, sound = "sfx_boss_enrage" }); }
        dad.rage = false;
    }
    void Hurt(int amount, Vector2 from)
    {
        if (dad.flying || dad.hurt > 0) return;   // a flying dad cannot be hit; a short grace after each hit
        dad.health = Mathf.Max(0, dad.health - amount); dad.hurt = .6f;
        events.Add(new Event { text = "-" + amount, pos = dad.pos, style = 1, sound = "sfx_punch_2" });
    }

    void UpdateFoe(Foe f, float dt)
    {
        f.flash = Mathf.Max(0, f.flash - dt); f.stun -= dt; f.wallCooldown -= dt; f.contact -= dt; f.cooldown -= dt; f.attackPose = Mathf.Max(0, f.attackPose - dt);
        f.groggy = Mathf.Max(0, f.groggy - dt); f.heavyClock = Mathf.Max(0, f.heavyClock - dt); if (f.heavyClock <= 0) f.heavyHits = 0;
        if (f.Boss && !f.enraged && f.hp <= f.maxHp / 2)
        {
            f.enraged = true; f.flash = .3f;
            events.Add(new Event { text = stage.bossEnrage, pos = f.pos, style = 3, sound = "sfx_boss_enrage" });
        }
        if (f.Flying) { f.air += dt; FlyFoe(f, dt); return; }
        f.vel = Vector2.zero; f.air = 0;
        if (f.stun > 0 || f.groggy > 0) return;
        Vector2 delta = dad.pos - f.pos; float distance = delta.magnitude;
        if (f.windup <= 0 && f.dash <= 0) f.facing = delta.x > 12 ? 1 : delta.x < -12 ? -1 : f.facing;
        if (!dad.flying && distance > 1) f.face = delta / distance;   // braced toward where dad stood; a bank shot can get round it
        if (f.dash > 0)
        {
            f.pos = Clamp(f.pos + f.aim * (f.Boss ? 380 : 430) * dt, f.Radius); f.dash -= dt;
            if ((dad.pos - f.pos).magnitude < f.Radius + DadRadius) Hurt(f.Boss ? 15 : 14, f.pos);   // the first boss should not wall off beginners
            if (f.dash <= 0) f.cooldown = f.Boss ? (f.enraged ? 2.2f : 3f) : 2.6f;
            return;
        }
        if (f.windup > 0)
        {
            f.windup -= dt;
            if (f.windup > 0) return;
            if (f.kind == Kind.Dodge)
            {
                // The throw goes where dad stood at the wind-up; moving (flying) is how you dodge it.
                Vector2 hand = f.pos - new Vector2(0, 60 * scale), dir = (f.target - f.pos).normalized;
                balls.Add(new Ball { pos = hand, vel = (dir.sqrMagnitude > .01f ? dir : Vector2.down) * 480 });
                f.attackPose = .3f; f.cooldown = 2.4f;
                events.Add(new Event { text = "", pos = f.pos, sound = "sfx_skill_pick" });
                return;
            }
            if (f.kind == Kind.Chair || f.Boss) { f.dash = .5f; events.Add(new Event { text = "", pos = f.pos, sound = "sfx_chair_dash" }); }
            else
            {
                f.attackPose = .25f; f.cooldown = 1.5f;
                if (distance < f.Radius + DadRadius + 22 * scale) Hurt(f.elite ? 14 : f.kind == Kind.Tray ? 10 : 8, f.pos);
            }
            return;
        }
        if (f.kind == Kind.Dodge)
        {
            // Keeps his distance: walks in to throwing range, winds up (the target mark shows), throws.
            if (f.cooldown <= 0 && distance < 520 && !dad.flying) { f.windup = .8f; f.target = dad.pos; f.facing = delta.x >= 0 ? 1 : -1; return; }
            if (distance < 300) return;
        }
        bool charger = f.kind == Kind.Chair || f.Boss;
        if (charger && f.cooldown <= 0 && distance < (f.Boss ? 340 : 280) && !dad.flying && NoOtherChargerWinding(f))
        { f.windup = f.Boss ? (f.enraged ? .65f : .85f) : .9f; f.aim = delta.normalized; f.facing = f.aim.x >= 0 ? 1 : -1; return; }
        if (!charger && f.kind != Kind.Dodge && f.cooldown <= 0 && distance < f.Radius + DadRadius + 14 * scale && !dad.flying)
        { f.windup = .4f; f.aim = delta.normalized; return; }
        if (distance < f.Radius + DadRadius + 4 * scale) return;
        // Walk toward dad, spreading out so a crowd stays readable and leaves angles to shoot through.
        Vector2 steer = delta / Mathf.Max(1, distance);
        foreach (Foe o in foes)
        {
            if (o == f || o.hp <= 0) continue;
            Vector2 away = f.pos - o.pos; float d = away.magnitude, clear = f.Radius + o.Radius + 20 * scale;
            if (d > .01f && d < clear) steer += away / d * (1 - d / clear) * 1.2f;
        }
        float speed = (f.Boss ? (f.enraged ? 42 : 32) : f.elite ? 34 : f.kind == Kind.Chair ? 42 : f.kind == Kind.Tray || f.kind == Kind.Dodge ? 40 : 50) * stage.speedScale;
        Vector2 step = Vector2.ClampMagnitude(steer, 1) * speed * dt;
        f.pos = Clamp(f.pos + step, f.Radius); f.walked += step.magnitude;
    }
    bool NoOtherChargerWinding(Foe self)
    {
        foreach (Foe o in foes) if (o != self && o.hp > 0 && (o.kind == Kind.Chair || o.Boss) && (o.windup > 0 || o.dash > 0)) return false;
        return true;
    }
    void FlyFoe(Foe f, float dt)
    {
        Vector2 p = f.pos + f.vel * dt; int side = -1; bool hitX = false, hitY = false;
        if (p.x < arena.xMin + f.Radius * .6f) { hitX = true; side = 0; }
        if (p.x > arena.xMax - f.Radius * .6f) { hitX = true; side = 1; }
        if (p.y < arena.yMin + 8 || p.y > arena.yMax - 8) hitY = true;
        f.pos = Clamp(p, f.Radius * .6f);
        if ((hitX || hitY) && f.wallCooldown <= 0)
        {
            f.wallCooldown = .3f; wallSlams++; shotChain++; bestChain = Mathf.Max(bestChain, shotChain);
            bool bossSlam = f.Boss && f.groggy > 0;
            int damage = f.Boss ? (bossSlam ? 3 + wallBonus : 0) : 2 + wallBonus;
            if (damage > 0) Damage(f, damage);
            if (bossSlam) f.groggy = 0;
            events.Add(new Event { text = bossSlam ? "보스 벽꿍!!" : "벽꿍!", pos = f.pos, style = bossSlam ? 3 : 1, sound = "sfx_wall_" + (1 + wallSlams % 2), dentSide = hitX ? side : -1 });
            freeze = Mathf.Max(freeze, bossSlam ? .07f : .035f);
            f.stun = Mathf.Max(f.stun, bossSlam ? .9f : .5f);
            if (hitX) f.vel.x *= -FoeWallBounce; if (hitY) f.vel.y *= -FoeWallBounce;
        }
        f.vel *= Mathf.Exp(-(f.Boss ? BossDrag : FoeDrag) * dt);   // an opened-up boss slides far: reaching a wall is the payoff
        if (!f.Flying) { f.vel = Vector2.zero; f.stun = Mathf.Max(f.stun, .35f); }
    }
    void Damage(Foe f, int amount) { if (f.hp <= 0) return; f.hp -= amount; f.flash = .12f; }
    // The tray covers the side turned toward dad, so a straight shot bounces off; bank off a locker or come from behind.
    static bool Guarded(Foe f, Vector2 incoming) { return f.shield && !f.Flying && Vector2.Dot(incoming.normalized, f.face) < -.5f; }
    void BreakTray(Foe f)
    {
        f.shield = false; f.trayBroken = true; f.stun = Mathf.Max(f.stun, .6f);
        events.Add(new Event { text = "쟁반 박살!", pos = f.pos, style = 2, sound = "sfx_tray_break" });
    }

    void DadContacts()
    {
        foreach (Foe f in foes)
        {
            if (f.hp <= 0) continue;
            Vector2 delta = f.pos - dad.pos; float distance = delta.magnitude, required = f.Radius + DadRadius;
            if (distance >= required) continue;
            Vector2 n = distance > .01f ? delta / distance : Vector2.up;
            if (!dad.flying)
            {
                if (!f.Flying) f.pos = Clamp(f.pos + n * (required - distance), f.Radius);   // a standing dad is not walked through
                continue;
            }
            if (f.lastShot == shots) continue;   // one hit per body per shot
            f.lastShot = shots;
            float speed = dad.vel.magnitude;
            dad.pos = f.pos - n * (required + 1);
            bool rage = dad.rage;
            if (Guarded(f, dad.vel) && !rage)
            {
                dad.vel = Vector2.Reflect(dad.vel, n) * .7f;
                if (++f.trayHits >= 2) BreakTray(f);
                else events.Add(new Event { text = "팅!", pos = f.pos, style = 1, sound = "sfx_tray_block" });
                continue;
            }
            if (f.Boss && f.groggy <= 0)
            {
                // A fresh boss is a wall: dad bounces off with a chip of damage. A thrown dad has to open him up first;
                // a rage shot knocks the wind out of him for a moment instead.
                dad.vel = Vector2.Reflect(dad.vel, n) * .65f; Damage(f, rage ? 2 : 1); f.pos = Clamp(f.pos + n * 10, f.Radius);
                if (rage) f.groggy = 1.2f;
                events.Add(new Event { text = rage ? "분노의 들이받기!" : "퍽!", pos = f.pos, style = rage ? 2 : 0, sound = "sfx_punch_1" });
                continue;
            }
            if (f.elite && !rage && f.heavyHits == 0)
            {
                // Too heavy for one hit: he staggers and dad bounces off. A second hit within three seconds sends him.
                f.heavyHits = 1; f.heavyClock = 3; f.stun = Mathf.Max(f.stun, .5f); f.windup = 0; Damage(f, 1);
                f.pos = Clamp(f.pos + n * 24 * scale, f.Radius); dad.vel = Vector2.Reflect(dad.vel, n) * .6f;
                events.Add(new Event { text = "무거워!  한 번 더", pos = f.pos, style = 1, sound = "sfx_tray_block" });
                continue;
            }
            if (f.shield) BreakTray(f);
            // Bank shot: every locker bounce before the hit adds 15% (and the second one adds a point of damage).
            float bank = 1 + .15f * dad.bounces;
            Vector2 dir = (dad.vel.normalized * .55f + n * .45f).normalized;
            f.vel = dir * Mathf.Max(380, speed * .95f) * bank * (f.elite ? .8f : 1) * knockBonus * throwBonus;
            f.air = 0; f.windup = 0; f.dash = 0; f.stun = .6f; f.contact = .15f; f.heavyHits = 0;
            Damage(f, 1 + (dad.bounces >= 2 ? 1 : 0) + (rage ? 1 : 0)); shotChain++; bestChain = Mathf.Max(bestChain, shotChain);
            // Dad ploughs on at 80% speed (95% in a rage): straight through a head-on hit, glancing off a side hit.
            Vector2 heading = Vector2.Lerp(dad.vel.normalized, Vector2.Reflect(dad.vel, n).normalized, rage ? .1f : .35f);
            dad.vel = (heading.sqrMagnitude > .01f ? heading.normalized : dad.vel.normalized) * speed * (rage ? .95f : .8f);
            bool big = speed * bank > 820 || rage;
            events.Add(new Event { text = rage ? "분노!" : dad.bounces > 0 ? "튕김 강타!" : big ? "크리티컬!" : "퍽!", pos = f.pos, style = big || dad.bounces > 0 ? 1 : 0, sound = big ? "sfx_punch_3" : "sfx_punch_1" });
        }
    }
    void FoeContacts(float dt)
    {
        for (int i = 0; i < foes.Count; i++) for (int j = i + 1; j < foes.Count; j++)
        {
            Foe a = foes[i], b = foes[j]; if (a.hp <= 0 || b.hp <= 0) continue;
            Vector2 delta = b.pos - a.pos; float distance = delta.magnitude, required = a.Radius + b.Radius;
            if (distance >= required) continue;
            Vector2 n = distance > .01f ? delta / distance : Vector2.right;
            Foe source = a.vel.sqrMagnitude > b.vel.sqrMagnitude ? a : b, target = source == a ? b : a;
            if (source.vel.magnitude > ChainSpeed && source.contact <= 0 && target.contact <= 0)
            {
                source.contact = .15f; target.contact = .25f;
                if (Guarded(target, source.vel))
                {
                    source.vel = Vector2.Reflect(source.vel, n) * .45f;
                    if (++target.trayHits >= 2) BreakTray(target);
                    else events.Add(new Event { text = "팅!", pos = target.pos, style = 1, sound = "sfx_tray_block" });
                    continue;
                }
                shotChain++; bestChain = Mathf.Max(bestChain, shotChain);
                if (target.Boss && target.groggy <= 0)
                {
                    target.groggy = BossGroggy; Damage(target, 1); source.vel *= .4f;
                    events.Add(new Event { text = "그로기!!  지금 아빠로 들이받아요", pos = target.pos, style = 3, sound = "sfx_chain" });
                    continue;
                }
                if (target.elite && target.heavyHits == 0)
                {
                    target.heavyHits = 1; target.heavyClock = 3; target.stun = Mathf.Max(target.stun, .5f); Damage(target, 1); source.vel *= .4f;
                    events.Add(new Event { text = "무거워!", pos = target.pos, style = 1, sound = "sfx_tray_block" });
                    continue;
                }
                if (target.shield) BreakTray(target);
                target.heavyHits = 0;
                target.vel = source.vel.normalized * Mathf.Max(300, source.vel.magnitude * .8f);
                target.air = 0; target.windup = 0; target.dash = 0; target.stun = .6f;
                Damage(target, 1); source.vel *= source.Boss ? .85f : .55f;   // a heavy boss ploughs through the small fry
                events.Add(new Event { text = "연쇄!", pos = target.pos, style = 1, sound = "sfx_chain" });
            }
            else
            {
                float settle = (required - distance) * .5f * (1 - Mathf.Exp(-20 * dt));
                if (!a.Flying && a.dash <= 0) a.pos = Clamp(a.pos - n * settle, a.Radius);
                if (!b.Flying && b.dash <= 0) b.pos = Clamp(b.pos + n * settle, b.Radius);
            }
        }
    }
    void UpdateBalls(float dt)
    {
        for (int i = balls.Count - 1; i >= 0; i--)
        {
            Ball b = balls[i]; b.life -= dt; b.pos += b.vel * dt;
            bool gone = b.life <= 0 || !arena.Contains(b.pos);
            if (!gone && !b.returned)
            {
                float d = (b.pos - (dad.pos - new Vector2(0, 40 * scale))).magnitude;
                if (dad.flying && d < DadRadius + 16 * scale)
                {
                    // A flying dad bats the ball back the way he is going, much faster.
                    b.returned = true; b.vel = dad.vel.normalized * 760; b.life = 1.6f; shotChain++; bestChain = Mathf.Max(bestChain, shotChain);
                    events.Add(new Event { text = "되받아치기!", pos = b.pos, style = 2, sound = "sfx_punch_3" });
                }
                else if (!dad.flying && d < DadRadius + 10 * scale) { Hurt(12, b.pos); gone = true; }
            }
            if (!gone && b.returned)
            {
                foreach (Foe f in foes)
                {
                    if (f.hp <= 0 || f.id == b.lastFoe || (f.pos - new Vector2(0, 40 * scale) - b.pos).magnitude > f.Radius + 10 * scale) continue;
                    b.lastFoe = f.id; b.hits++; shotChain++; bestChain = Mathf.Max(bestChain, shotChain);
                    if (f.Boss && f.groggy <= 0) { f.groggy = BossGroggy; Damage(f, 1); events.Add(new Event { text = "그로기!!  지금 아빠로 들이받아요", pos = f.pos, style = 3, sound = "sfx_chain" }); }
                    else
                    {
                        if (f.shield) BreakTray(f);
                        if (!f.elite || f.heavyHits > 0) { f.vel = b.vel.normalized * 520; f.air = 0; f.windup = 0; f.dash = 0; f.stun = .6f; f.heavyHits = 0; }
                        else { f.heavyHits = 1; f.heavyClock = 3; f.stun = Mathf.Max(f.stun, .5f); }
                        Damage(f, 1); events.Add(new Event { text = "명중!", pos = f.pos, style = 1, sound = "sfx_chain" });
                    }
                    b.vel *= .8f;
                    if (b.hits >= 3) gone = true;
                    break;
                }
            }
            if (gone) balls.RemoveAt(i);
        }
    }
    Vector2 Clamp(Vector2 p, float margin)
    {
        return new Vector2(Mathf.Clamp(p.x, arena.xMin + margin, arena.xMax - margin), Mathf.Clamp(p.y, arena.yMin + 8, arena.yMax - 8));
    }

    // ---- stages -----------------------------------------------------------------------------------------------------
    // Six prototype stages: the pull, then walls / bank shots / chains, the chair rush and the tray, the dodgeball dad,
    // the heavy big-dad (mid-boss), then the 1-20 boss. Times are seconds from the start; x is where in the doorway the dad
    // walks in.
    public static Stage[] PrototypeStages(Rect arena)
    {
        float c = arena.center.x;
        var s1 = new Stage { label = "B-1", title = "당겨서 날아가기", lesson = "화면 아무 데나 누르고 뒤로 당겼다 놓으세요", par = 3, speedScale = .8f };
        Add(s1, 0, Kind.Gum, c); Add(s1, 2.5f, Kind.Gum, c - 60); Add(s1, 5, Kind.Gum, c + 60);
        var s2 = new Stage { label = "B-2", title = "벽꿍과 연쇄", lesson = "사물함에 튕긴 뒤 맞히면 더 세게! 날아간 아빠끼리 부딪히면 연쇄", par = 4 };
        Add(s2, 0, Kind.Gum, c - 50); Add(s2, 0, Kind.Gum, c - 10); Add(s2, .3f, Kind.Gum, c + 40);
        Add(s2, 5, Kind.Gum, c - 70); Add(s2, 5, Kind.Gum, c); Add(s2, 5.3f, Kind.Gum, c + 70);
        var s3 = new Stage { label = "B-3", title = "의자 돌진과 쟁반", lesson = "의자 아빠는 돌진 전에 먼저! 쟁반은 옆이나 뒤에서", par = 9 };
        Add(s3, 0, Kind.Gum, c - 40); Add(s3, 0, Kind.Gum, c + 40); Add(s3, 2, Kind.Chair, c);
        Add(s3, 6, Kind.Tray, c - 50); Add(s3, 6.5f, Kind.Gum, c + 50); Add(s3, 10, Kind.Chair, c + 30); Add(s3, 11, Kind.Tray, c - 30); Add(s3, 11.5f, Kind.Gum, c + 70);
        var s4 = new Stage { label = "B-4", title = "피구 아빠", lesson = "날아가는 아빠로 공을 맞히면 되받아쳐요. 서 있으면 맞아요", par = 8 };
        Add(s4, 0, Kind.Gum, c - 50); Add(s4, 0, Kind.Gum, c + 50); Add(s4, 1, Kind.Dodge, c);
        Add(s4, 6, Kind.Gum, c - 70); Add(s4, 6.3f, Kind.Dodge, c + 60); Add(s4, 6.6f, Kind.Gum, c);
        Add(s4, 11, Kind.Dodge, c - 60); Add(s4, 11.3f, Kind.Gum, c + 70); Add(s4, 11.6f, Kind.Chair, c);
        var s5 = new Stage { label = "B-5", title = "지호 큰아빠", lesson = "큰아빠는 무거워요. 3초 안에 두 번 들이받거나 분노 발사로!", par = 11 };
        Add(s5, 0, Kind.Gum, c, true); Add(s5, .5f, Kind.Gum, c - 70); Add(s5, .5f, Kind.Gum, c + 70);
        Add(s5, 5, Kind.Tray, c - 40); Add(s5, 5.3f, Kind.Gum, c + 40); Add(s5, 9, Kind.Chair, c); Add(s5, 9.3f, Kind.Dodge, c - 60);
        Add(s5, 13, Kind.Gum, c - 70); Add(s5, 13.3f, Kind.Gum, c + 70);
        var s6 = new Stage { label = "B-보스", title = "학부모회장 아빠", lesson = "아빠를 날려 회장님에게 맞히면 그로기! 그때 들이받아 벽으로", par = 15, bossEscorts = true };
        Add(s6, 0, Kind.Boss, c); Add(s6, .5f, Kind.Gum, c - 70); Add(s6, .5f, Kind.Gum, c + 70);
        return new[] { s1, s2, s3, s4, s5, s6 };
    }
    static void Add(Stage s, float at, Kind kind, float x, bool elite = false) { s.spawns.Add(new Spawn { at = at, kind = kind, x = x, elite = elite }); }

    // ---- main game stages (3 floors x 20) ------------------------------------------------------------------------------
    static readonly string[] FloorTitles = { "1층 복도", "2층 급식실", "3층 체육관" };
    static readonly string[] EliteBanners = { "지호 큰아빠 등장!  무거워요, 두 번 들이받으세요", "민준이 큰아빠 등장!  쟁반도 두 배, 두 번 들이받으세요", "서연이 큰아빠 등장!  의자도 두 개, 두 번 들이받으세요" };
    static readonly string[] BossBanners = { "학부모회장 아빠 등장!  단톡방 방장이다", "급식 봉사 반장 등장!  세쌍둥이 아빠다", "체육 선생님 등장!  계주 10연패 아빠다" };
    static readonly string[] BossEnrages = { "회장님이 단톡방을 나갔습니다", "반장님이 국자를 들었습니다", "체육 선생님이 호루라기를 불었습니다" };
    // Floor 1, stages 1-10 follow the first-ten plan (dad-war-first10-v2 order, retold for the slingshot): the pull, bank shots
    // and chains, the chair, a power stage, the tray, the dodgeball dad, the big-dad. Everything else is generated: more dads,
    // more roles and a little more speed with every stage and floor; the 10th is the floor's big-dad, the 20th its boss.
    public static Stage ForMain(int floor, int number, Rect arena)
    {
        floor = Mathf.Clamp(floor, 1, 3); number = Mathf.Clamp(number, 1, 20);
        float c = arena.center.x; int f = floor - 1;
        var st = new Stage { label = floor + "-" + number, title = FloorTitles[f], floor = floor, bossBanner = BossBanners[f], bossEnrage = BossEnrages[f], eliteBanner = EliteBanners[f], bossHp = 12 + 3 * f, eliteHp = 8 + 2 * f };
        if (floor == 1 && number <= 9)
        {
            Stage[] proto = PrototypeStages(arena);
            switch (number)
            {
                case 1: Copy(proto[0], st); break;
                case 2: Copy(proto[1], st); break;
                case 3: st.lesson = "한 발로 여럿! 사물함에 튕겨 줄 선 아빠들을 쓸어요"; st.par = 5; Wave(st, 0, Kind.Gum, 4, c); Wave(st, 5, Kind.Gum, 4, c); break;
                case 4: st.lesson = "의자 아빠는 돌진 전에 먼저!"; st.par = 5; Wave(st, 0, Kind.Gum, 3, c); Add(st, 2, Kind.Chair, c); Wave(st, 7, Kind.Gum, 2, c); break;
                case 5: st.lesson = "돌진 예고선이 보이면 그 의자 아빠부터"; st.par = 7; Wave(st, 0, Kind.Gum, 3, c); Add(st, 1.5f, Kind.Chair, c - 60); Wave(st, 6, Kind.Gum, 2, c); Add(st, 8, Kind.Chair, c + 60); break;
                case 6: st.lesson = "몸풀기! 뭉친 아빠들을 한 번에"; st.par = 4; st.speedScale = .85f; Wave(st, 0, Kind.Gum, 5, c); Wave(st, 4, Kind.Gum, 5, c); break;
                case 7: st.lesson = "쟁반은 정면을 막아요. 벽에 튕겨 옆이나 뒤로"; st.par = 6; Wave(st, 0, Kind.Gum, 3, c); Add(st, 1, Kind.Tray, c); Wave(st, 7, Kind.Gum, 2, c); break;
                case 8: Copy(proto[2], st); break;
                case 9: Copy(proto[3], st); break;
            }
            st.label = floor + "-" + number; st.title = FloorTitles[f];
            return st;
        }
        if (floor == 1 && number == 10) { Copy(PrototypeStages(arena)[4], st); st.label = "1-10"; st.title = FloorTitles[f]; return st; }
        float step = (number - 1) / 19f + f;   // 0 .. 3 across the whole school
        st.speedScale = 1 + .04f * step;
        Kind[] roles = { Kind.Chair, Kind.Tray, Kind.Dodge };
        if (number == 20)
        {
            st.lesson = "아빠를 날려 보스에게 맞히면 그로기! 그때 들이받아 벽으로"; st.bossEscorts = true; st.par = 15 + 2 * f;
            Add(st, 0, Kind.Boss, c); Add(st, .5f, Kind.Gum, c - 70); Add(st, .5f, Kind.Gum, c + 70);
            return st;
        }
        if (number == 10)
        {
            Kind eliteKind = f == 1 ? Kind.Tray : f == 2 ? Kind.Chair : Kind.Gum;
            st.lesson = "큰아빠는 무거워요. 3초 안에 두 번 들이받거나 분노 발사로!"; st.par = 12 + f;
            Add(st, 0, eliteKind, c, true); Wave(st, .5f, Kind.Gum, 2, c); Add(st, 5, roles[f], c - 40); Wave(st, 5.3f, Kind.Gum, 2, c);
            Add(st, 9, Kind.Chair, c); Add(st, 9.3f, Kind.Dodge, c - 60); Wave(st, 13, Kind.Gum, 2, c);
            return st;
        }
        // Generated stage: three waves; the share of chairs, trays and dodgeball dads grows with the stage.
        int total = 7 + number / 3 + 2 * f, made = 0, wave = 0;
        float special = Mathf.Clamp01(.2f + .05f * number + .1f * f);
        var rng = new System.Random(floor * 100 + number);
        st.lesson = number % 5 == 1 ? FloorTitles[f] + "  새 판!  튕김과 연쇄로 몰아치세요" : "남은 아빠를 사물함에 박아 버려요";
        while (made < total)
        {
            int n = Mathf.Min(total - made, 3 + rng.Next(2));
            for (int i = 0; i < n; i++)
            {
                Kind kind = rng.NextDouble() < special ? roles[rng.Next(Mathf.Min(3, 1 + number / 7 + f))] : Kind.Gum;
                Add(st, wave * 5.5f + i * .3f, kind, c + (i - (n - 1) / 2f) * 60);
            }
            made += n; wave++;
        }
        st.par = Mathf.CeilToInt(total * .6f) + 2;
        return st;
    }
    static void Copy(Stage from, Stage to)
    {
        to.lesson = from.lesson; to.par = from.par; to.speedScale = from.speedScale;
        foreach (Spawn sp in from.spawns) to.spawns.Add(sp);
    }
    static void Wave(Stage s, float at, Kind kind, int count, float c)
    {
        for (int i = 0; i < count; i++) Add(s, at + i * .25f, kind, c + (i - (count - 1) / 2f) * 60);
    }
}
