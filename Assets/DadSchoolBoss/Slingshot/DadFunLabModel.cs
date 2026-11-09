using UnityEngine;

// Isolated experiment: no progression, save, ads or campaign changes.
public sealed class DadFunLabModel
{
    public static readonly Rect Arena = new Rect(52, 180, 436, 690);
    public static readonly string[] Titles = { "벽 뒤의 빈틈", "급식 출발!", "날다가 한 번 더", "카트 우회 작전", "매트 타고 대소동", "양쪽을 한 번에", "움직이는 표적", "마지막 급식 전쟁" };
    // Introduce the active redirect first; reserve the precision shield puzzle for a later optional challenge.
    public static readonly int[] PlayOrder = { 2, 1, 5, 4, 3, 6, 0, 7 };
    public static readonly string[] Hints = {
        "쟁반 정면 대신 벽을 거쳐 옆을 노려요", "카트를 밀면 모인 상대를 쓸어버려요",
        "발사 후 원하는 위치를 탭해 한 번 더 돌진", "카트와 아빠, 어느 쪽으로 먼저 보낼까요?",
        "노란 매트는 화살표 방향으로 강하게 쏘아 올려요", "한쪽을 맞힌 뒤 반대쪽을 탭해 돌진",
        "움직임을 보고 발사 시점을 골라요", "카트 · 매트 · 돌진을 함께 써보세요" };
    public int ShotBudget { get { return level == 4 || level >= 6 ? 3 : 2; } }
    public bool CanBoost { get { return level >= 2; } }
    public bool CanLaunch { get { return !sim.Over && sim.phase == SlingshotSim.Phase.Ready && sim.dad.landed <= .01f && sim.shots < ShotBudget; } }
    public readonly SlingshotSim sim;
    public readonly int level;
    public Vector2 cart, cartVelocity;
    public bool cartEnabled, cartHit, boostUsed;
    public int cartHits;
    public bool padEnabled;
    public Vector2 pad;
    public int padHits;
    public float focusTime;
    float padCooldown;
    float settled;
    readonly System.Collections.Generic.HashSet<int> cartVictims = new System.Collections.Generic.HashSet<int>();
    public DadFunLabModel(int index, bool enableCart = true)
    {
        level = Mathf.Clamp(index, 0, Titles.Length - 1);
        sim = new SlingshotSim(new SlingshotSim.Stage { label = "실험 " + (level + 1), par = 1, speedScale = 0 }, Arena, 1, .72f);
        sim.dad.pos = new Vector2(270, 790);
        if (level == 0)
        {
            Target(160, 545, true); Target(160, 490); Target(160, 435);
        }
        else if (level == 1)
        {
            cartEnabled = enableCart; cart = new Vector2(270, 600);
            Target(230, 430); Target(270, 390); Target(310, 350); Target(270, 300);
        }
        else if (level == 2) { Target(150, 550); Target(365, 450); Target(365, 350); }
        else if (level == 3)
        {
            cartEnabled = enableCart; cart = new Vector2(190, 590);
            Target(135, 410); Target(175, 360); Target(220, 320); Target(390, 510, true);
        }
        else if (level == 4)
        {
            padEnabled = true; pad = new Vector2(270, 550);
            Target(125, 395); Target(150, 325); Target(390, 395); Target(365, 325);
        }
        else if (level == 5)
        {
            Target(145, 560); Target(145, 495); Target(395, 400); Target(395, 335);
        }
        else if (level == 6)
        {
            Target(130, 420); Target(210, 370); Target(330, 370); Target(410, 420);
            sim.stage.speedScale = .45f;
            foreach (var foe in sim.foes) { foe.stun = 0; foe.cooldown = 1.5f; }
        }
        else
        {
            cartEnabled = enableCart; cart = new Vector2(185, 595);
            padEnabled = true; pad = new Vector2(365, 570);
            Target(140, 415); Target(185, 350); Target(230, 295); Target(390, 400, true); Target(365, 310);
        }
    }
    void Target(float x, float y, bool shield = false)
    {
        var f = sim.Add(shield ? SlingshotSim.Kind.Tray : SlingshotSim.Kind.Gum, x, y);
        f.hp = f.maxHp = 1; f.stun = 999; f.cooldown = 999; f.face = Vector2.down;
    }
    public bool Launch(Vector2 pull)
    {
        if (!CanLaunch || !sim.Launch(pull)) return false;
        boostUsed = false; settled = focusTime = 0; return true;
    }
    public bool Boost(Vector2 towards)
    {
        if (!CanBoost || boostUsed || !sim.dad.flying || sim.Over || towards.sqrMagnitude < 1) return false;
        boostUsed = true; focusTime = 0; sim.dad.vel = towards.normalized * 1000;
        sim.dad.flightTime = 0; // One explicit extra flight, never an unlimited reset.
        sim.events.Add(new SlingshotSim.Event { text = "한 번 더!", pos = sim.dad.pos, style = 2, sound = "sfx_launch_whoosh" });
        return true;
    }
    public void Tick(float dt)
    {
        // Keep cart collisions swept at the same small step as the body simulation.
        float remaining = Mathf.Min(dt, .1f);
        while (remaining > .000001f && !sim.Over)
        {
            float step = Mathf.Min(remaining, 1 / 120f); remaining -= step;
            Vector2 oldDad = sim.dad.pos; int previousKills = sim.kills;
            sim.Tick(step);
            if (sim.kills > previousKills && CanBoost && !boostUsed) focusTime = .45f;
            else focusTime = Mathf.Max(0, focusTime - step);
            padCooldown = Mathf.Max(0, padCooldown - step);
            if (padEnabled && sim.dad.flying && padCooldown <= 0 && DistanceToSegment(pad, oldDad, sim.dad.pos) < sim.DadRadius + 32)
            {
                // The painted chevron is a promise: the mat always launches upward.
                sim.dad.vel = new Vector2(sim.dad.vel.x * .25f, -Mathf.Max(1050, sim.dad.vel.magnitude));
                sim.dad.pos = pad + new Vector2(0, -sim.DadRadius - 34);
                padCooldown = .18f; padHits++;
                sim.events.Add(new SlingshotSim.Event { text = "매트 반사!", pos = pad, style = 2, sound = "sfx_wall_boing" });
            }
            if (cartEnabled)
            {
                if (!cartHit && sim.dad.flying && DistanceToSegment(cart, oldDad, sim.dad.pos) < 40)
                {
                    cartHit = true; cartVelocity = sim.dad.vel.normalized * 1000;
                    sim.dad.vel *= .65f;
                    sim.events.Add(new SlingshotSim.Event { text = "급식 출발!", pos = cart, style = 2, sound = "sfx_wall_boing" });
                }
                Vector2 old = cart; cart += cartVelocity * step;
                if (cart.x < Arena.xMin + 30 || cart.x > Arena.xMax - 30) { cart.x = Mathf.Clamp(cart.x, Arena.xMin + 30, Arena.xMax - 30); cartVelocity.x *= -.8f; }
                if (cart.y < Arena.yMin + 25 || cart.y > Arena.yMax - 25) { cart.y = Mathf.Clamp(cart.y, Arena.yMin + 25, Arena.yMax - 25); cartVelocity.y *= -.8f; }
                foreach (var f in sim.foes)
                    if (f.hp > 0 && cartVelocity.magnitude > 100 && !cartVictims.Contains(f.id) && DistanceToSegment(f.pos, old, cart) < f.Radius + 35)
                    {
                        cartVictims.Add(f.id); cartHits++; f.hp--; f.vel = cartVelocity * .7f;
                        sim.shotChain++; sim.bestChain = Mathf.Max(sim.bestChain, sim.shotChain);
                        sim.events.Add(new SlingshotSim.Event { text = "카트 연쇄!", pos = f.pos, style = 1, sound = "sfx_punch_3" });
                    }
                cartVelocity *= Mathf.Exp(-1.1f * step);
            }
            bool moving = sim.dad.flying || cartVelocity.magnitude > 80;
            foreach (var f in sim.foes) if (f.hp > 0 && f.Flying) moving = true;
            settled = sim.shots > 0 && !moving ? settled + step : 0;
            if (!sim.Over && settled > .6f && sim.shots >= ShotBudget) { sim.phase = SlingshotSim.Phase.Lost; sim.lossReason = "발사 끝 · " + sim.AliveCount + "명 남음"; }
        }
    }
    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    { Vector2 v = b - a; return Vector2.Distance(p, a + v * (v.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, v) / v.sqrMagnitude))); }
}
