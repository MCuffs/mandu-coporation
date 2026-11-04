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
    public enum Kind { Gum, Chair, Tray, Boss }

    public sealed class Dad
    {
        public Vector2 pos, vel;
        public float health = 100, maxHealth = 100, flightTime, hurt, landed;
        public bool flying;
    }
    public sealed class Foe
    {
        public int id, hp, maxHp, trayHits, facing = -1, lastShot = -1;
        public Kind kind;
        public bool shield, trayBroken, enraged;
        public Vector2 pos, vel, aim, face = Vector2.down;   // face: the side the tray covers, kept turned toward dad
        public float stun, flash, wallCooldown, contact, windup, dash, cooldown, groggy, air, walked, attackPose;
        public bool Boss { get { return kind == Kind.Boss; } }
        public bool Flying { get { return vel.sqrMagnitude > MinFlySpeed * MinFlySpeed; } }
        public float Radius { get { return kind == Kind.Boss ? 38 : kind == Kind.Chair ? 30 : kind == Kind.Tray ? 27 : 25; } }
        public float Lift { get { return Flying ? 14 * Mathf.Clamp01(air / .12f) : 0; } }   // airborne bodies ride a little above their shadow
    }
    public sealed class Spawn { public float at, x; public Kind kind; }
    public sealed class Stage
    {
        public string label, title, lesson;
        public int par;                       // shots for the third star
        public float speedScale = 1;
        public bool bossEscorts;              // the boss keeps calling more dads in while he stands
        public readonly List<Spawn> spawns = new List<Spawn>();
    }
    public sealed class Event
    {
        public string text, sound; public Vector2 pos; public int style;   // style 0 small, 1 medium, 2 big, 3 banner
        public int dentSide = -1;                                          // a side-wall slam: 0 left, 1 right
    }
    public sealed class Corpse { public Kind kind; public Vector2 pos, vel; public float life = .6f; public int facing; }

    public const float DadRadius = 26, MinFlySpeed = 60, PullDeadZone = 24, PullMax = 150;
    public const float LaunchMin = 420, LaunchMax = 1150, DadDrag = 1.3f, DadBounce = .88f, StopSpeed = 80, MaxFlight = 2.4f;
    const float FoeDrag = 3.0f, BossDrag = 1.8f, FoeWallBounce = .35f, ChainSpeed = 230, BossGroggy = 2.6f;

    public readonly Rect arena;
    public readonly Stage stage;
    public readonly Dad dad = new Dad();
    public readonly List<Foe> foes = new List<Foe>();
    public readonly List<Event> events = new List<Event>();
    public readonly List<Corpse> corpses = new List<Corpse>();
    public Phase phase = Phase.Ready;
    public int shots, kills, wallSlams, shotChain, bestChain;
    public float elapsed, freeze;
    readonly System.Random rng;
    int nextSpawn, nextId, escortsCalled;
    float escortClock;

    public SlingshotSim(Stage stage, Rect arena, int seed = 1)
    {
        this.stage = stage; this.arena = arena; rng = new System.Random(seed);
        dad.pos = new Vector2(arena.center.x, arena.yMax - 60);
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
        dad.vel = LaunchVelocity(pull); dad.flying = true; dad.flightTime = 0;
        shots++; shotChain = 0; phase = Phase.Flying;
        events.Add(new Event { text = "", sound = "sfx_skill_pick", pos = dad.pos });
        return true;
    }
    // The aiming guide: straight to the first wall, then a short stretch after the bounce. Bodies are not shown; reading
    // the crowd is the player's job.
    public static List<Vector2> Predict(Vector2 from, Vector2 pull, Rect arena)
    {
        var points = new List<Vector2> { from };
        if (pull.magnitude < PullDeadZone) return points;
        Vector2 v = LaunchVelocity(pull), dir = v.normalized, p = from;
        float budget = v.magnitude / DadDrag * .9f, after = -1;
        Rect inner = new Rect(arena.xMin + DadRadius, arena.yMin + DadRadius, arena.width - 2 * DadRadius, arena.height - 2 * DadRadius);
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
        if (Over || dt <= 0) return;
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
        for (int i = corpses.Count - 1; i >= 0; i--) { Corpse c = corpses[i]; c.life -= dt; c.pos += c.vel * dt; c.vel *= Mathf.Exp(-2 * dt); if (c.life <= 0) corpses.RemoveAt(i); }
        for (int i = foes.Count - 1; i >= 0; i--)
        {
            Foe f = foes[i]; if (f.hp > 0) continue;
            foes.RemoveAt(i); kills++;
            corpses.Add(new Corpse { kind = f.kind, pos = f.pos, vel = f.vel.sqrMagnitude > 1 ? f.vel * .6f : new Vector2(f.facing * -120, -160), facing = f.facing });
            events.Add(new Event { text = "제압", pos = f.pos, style = 0 });
            if (f.Boss) { phase = Phase.Won; events.Add(new Event { text = "의리 증명 완료!", pos = f.pos, style = 3, sound = "jingle_win" }); return; }
        }
        if (dad.health <= 0) { phase = Phase.Lost; events.Add(new Event { text = "", sound = "jingle_lose", pos = dad.pos }); return; }
        if (nextSpawn >= stage.spawns.Count && AliveCount == 0 && !stage.bossEscorts) { phase = Phase.Won; events.Add(new Event { text = "", sound = "jingle_win", pos = dad.pos }); }
    }
    void SpawnDue(float dt)
    {
        while (nextSpawn < stage.spawns.Count && stage.spawns[nextSpawn].at <= elapsed && AliveCount < 8) { Spawn s = stage.spawns[nextSpawn++]; Add(s.kind, s.x); }
        // Boss stage: while the boss stands, dads keep coming (they are the ammunition that opens him up).
        if (!stage.bossEscorts || Boss == null) return;
        escortClock -= dt;
        if (escortClock <= 0 && AliveCount < 4) { escortClock = 3.2f; Add(Kind.Gum, arena.center.x + (escortsCalled++ % 2 == 0 ? -46 : 46)); }
    }
    public Foe Add(Kind kind, float x, float y = -1)
    {
        var f = new Foe { id = nextId++, kind = kind, pos = new Vector2(x, y < 0 ? arena.yMin + 22 : y) };
        f.maxHp = f.hp = kind == Kind.Boss ? 12 : kind == Kind.Tray ? 3 : 2;
        f.shield = kind == Kind.Tray;
        f.cooldown = .5f + (float)rng.NextDouble() * .5f;   // a short grace so a fresh arrival does not swing at once
        foes.Add(f);
        if (kind == Kind.Boss) events.Add(new Event { text = "학부모회장 아빠 등장!  단톡방 방장이다", pos = f.pos, style = 3, sound = "sfx_boss_enrage" });
        return f;
    }

    void UpdateDad(float dt)
    {
        dad.hurt = Mathf.Max(0, dad.hurt - dt); dad.landed = Mathf.Max(0, dad.landed - dt);
        if (!dad.flying) return;
        dad.flightTime += dt;
        Vector2 p = dad.pos + dad.vel * dt; bool bumped = false;
        if (p.x < arena.xMin + DadRadius) { p.x = arena.xMin + DadRadius; dad.vel.x = Mathf.Abs(dad.vel.x) * DadBounce; bumped = true; }
        if (p.x > arena.xMax - DadRadius) { p.x = arena.xMax - DadRadius; dad.vel.x = -Mathf.Abs(dad.vel.x) * DadBounce; bumped = true; }
        if (p.y < arena.yMin + DadRadius) { p.y = arena.yMin + DadRadius; dad.vel.y = Mathf.Abs(dad.vel.y) * DadBounce; bumped = true; }
        if (p.y > arena.yMax - DadRadius) { p.y = arena.yMax - DadRadius; dad.vel.y = -Mathf.Abs(dad.vel.y) * DadBounce; bumped = true; }
        if (bumped) events.Add(new Event { text = "쿵", pos = p, style = 0, sound = "sfx_punch_3" });
        dad.pos = p; dad.vel *= Mathf.Exp(-DadDrag * dt);
        if (dad.vel.magnitude < StopSpeed || dad.flightTime > MaxFlight) Land();
    }
    void Land()
    {
        dad.flying = false; dad.vel = Vector2.zero; dad.landed = .2f;
        if (!Over) phase = Phase.Ready;
        if (shotChain >= 3) events.Add(new Event { text = shotChain + "연쇄!!", pos = dad.pos, style = 2, sound = "sfx_chain" });
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
        f.groggy = Mathf.Max(0, f.groggy - dt);
        if (f.Boss && !f.enraged && f.hp <= f.maxHp / 2)
        {
            f.enraged = true; f.flash = .3f;
            events.Add(new Event { text = "회장님이 단톡방을 나갔습니다", pos = f.pos, style = 3, sound = "sfx_boss_enrage" });
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
            if ((dad.pos - f.pos).magnitude < f.Radius + DadRadius) Hurt(f.Boss ? 18 : 14, f.pos);
            if (f.dash <= 0) f.cooldown = f.Boss ? (f.enraged ? 2.2f : 3f) : 2.6f;
            return;
        }
        if (f.windup > 0)
        {
            f.windup -= dt;
            if (f.windup > 0) return;
            if (f.kind == Kind.Chair || f.Boss) { f.dash = .5f; events.Add(new Event { text = "", pos = f.pos, sound = "sfx_chair_dash" }); }
            else
            {
                f.attackPose = .25f; f.cooldown = 1.5f;
                if (distance < f.Radius + DadRadius + 22) Hurt(f.kind == Kind.Tray ? 10 : 8, f.pos);
            }
            return;
        }
        bool charger = f.kind == Kind.Chair || f.Boss;
        if (charger && f.cooldown <= 0 && distance < (f.Boss ? 340 : 280) && !dad.flying && NoOtherChargerWinding(f))
        { f.windup = f.Boss ? (f.enraged ? .65f : .85f) : .9f; f.aim = delta.normalized; f.facing = f.aim.x >= 0 ? 1 : -1; return; }
        if (!charger && f.cooldown <= 0 && distance < f.Radius + DadRadius + 14 && !dad.flying)
        { f.windup = .4f; f.aim = delta.normalized; return; }
        if (distance < f.Radius + DadRadius + 4) return;
        // Walk toward dad, spreading out so a crowd stays readable and leaves angles to shoot through.
        Vector2 steer = delta / Mathf.Max(1, distance);
        foreach (Foe o in foes)
        {
            if (o == f || o.hp <= 0) continue;
            Vector2 away = f.pos - o.pos; float d = away.magnitude, clear = f.Radius + o.Radius + 20;
            if (d > .01f && d < clear) steer += away / d * (1 - d / clear) * 1.2f;
        }
        float speed = (f.Boss ? (f.enraged ? 42 : 32) : f.kind == Kind.Chair ? 42 : f.kind == Kind.Tray ? 40 : 50) * stage.speedScale;
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
            int damage = f.Boss ? (bossSlam ? 3 : 0) : 2;
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
            if (Guarded(f, dad.vel))
            {
                dad.vel = Vector2.Reflect(dad.vel, n) * .7f;
                if (++f.trayHits >= 2) BreakTray(f);
                else events.Add(new Event { text = "팅!", pos = f.pos, style = 1, sound = "sfx_tray_block" });
                continue;
            }
            if (f.Boss && f.groggy <= 0)
            {
                // A fresh boss is a wall: dad bounces off with a chip of damage. A thrown dad has to open him up first.
                dad.vel = Vector2.Reflect(dad.vel, n) * .65f; Damage(f, 1); f.pos = Clamp(f.pos + n * 10, f.Radius);
                events.Add(new Event { text = "퍽!", pos = f.pos, style = 0, sound = "sfx_punch_1" });
                continue;
            }
            if (f.shield) BreakTray(f);
            Vector2 dir = (dad.vel.normalized * .55f + n * .45f).normalized;
            f.vel = dir * Mathf.Max(380, speed * .95f);
            f.air = 0; f.windup = 0; f.dash = 0; f.stun = .6f; f.contact = .15f;
            Damage(f, 1); shotChain++; bestChain = Mathf.Max(bestChain, shotChain);
            // Dad ploughs on at 80% speed: straight through a head-on hit, glancing off a side hit.
            Vector2 heading = Vector2.Lerp(dad.vel.normalized, Vector2.Reflect(dad.vel, n).normalized, .35f);
            dad.vel = (heading.sqrMagnitude > .01f ? heading.normalized : dad.vel.normalized) * speed * .8f;
            events.Add(new Event { text = speed > 820 ? "크리티컬!" : "퍽!", pos = f.pos, style = speed > 820 ? 1 : 0, sound = speed > 820 ? "sfx_punch_3" : "sfx_punch_1" });
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
                if (target.shield) BreakTray(target);
                target.vel = source.vel.normalized * Mathf.Max(300, source.vel.magnitude * .8f);
                target.air = 0; target.windup = 0; target.dash = 0; target.stun = .6f;
                Damage(target, 1); source.vel *= .55f;
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
    Vector2 Clamp(Vector2 p, float margin)
    {
        return new Vector2(Mathf.Clamp(p.x, arena.xMin + margin, arena.xMax - margin), Mathf.Clamp(p.y, arena.yMin + 8, arena.yMax - 8));
    }

    // ---- stages -----------------------------------------------------------------------------------------------------
    // Four prototype stages: the first fight teaches the pull, then walls and chains, then the chair rush and the tray,
    // then the 1-20 boss. Times are seconds from the start; x is where in the doorway the dad walks in.
    public static Stage[] PrototypeStages(Rect arena)
    {
        float c = arena.center.x;
        var s1 = new Stage { label = "B-1", title = "당겨서 날아가기", lesson = "화면 아무 데나 누르고 뒤로 당겼다 놓으세요", par = 3, speedScale = .8f };
        Add(s1, 0, Kind.Gum, c); Add(s1, 2.5f, Kind.Gum, c - 60); Add(s1, 5, Kind.Gum, c + 60);
        var s2 = new Stage { label = "B-2", title = "벽꿍과 연쇄", lesson = "맞은 아빠가 사물함이나 다른 아빠에게 부딪히면 연쇄!", par = 4 };
        Add(s2, 0, Kind.Gum, c - 50); Add(s2, 0, Kind.Gum, c - 10); Add(s2, .3f, Kind.Gum, c + 40);
        Add(s2, 5, Kind.Gum, c - 70); Add(s2, 5, Kind.Gum, c); Add(s2, 5.3f, Kind.Gum, c + 70);
        var s3 = new Stage { label = "B-3", title = "의자 돌진과 쟁반", lesson = "의자 아빠는 돌진 전에 먼저! 쟁반은 옆이나 뒤에서", par = 8 };
        Add(s3, 0, Kind.Gum, c - 40); Add(s3, 0, Kind.Gum, c + 40); Add(s3, 2, Kind.Chair, c);
        Add(s3, 6, Kind.Tray, c - 50); Add(s3, 6.5f, Kind.Gum, c + 50); Add(s3, 10, Kind.Chair, c + 30); Add(s3, 11, Kind.Tray, c - 30); Add(s3, 11.5f, Kind.Gum, c + 70);
        var s4 = new Stage { label = "B-보스", title = "학부모회장 아빠", lesson = "아빠를 날려 회장님에게 맞히면 그로기! 그때 들이받아 벽으로", par = 12, bossEscorts = true };
        Add(s4, 0, Kind.Boss, c); Add(s4, .5f, Kind.Gum, c - 70); Add(s4, .5f, Kind.Gum, c + 70);
        return new[] { s1, s2, s3, s4 };
    }
    static void Add(Stage s, float at, Kind kind, float x) { s.spawns.Add(new Spawn { at = at, kind = kind, x = x }); }
}
