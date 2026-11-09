using UnityEngine;

// Isolated experiment: no progression, save, ads or campaign changes.
public sealed class DadFunLabModel
{
    public static readonly Rect Arena = new Rect(52, 180, 436, 690);
    public static readonly string[] Titles = { "한 발로 전원 제압", "급식 카트로 싹쓸이", "날다가 한 번 더 꺾기" };
    public readonly SlingshotSim sim;
    public readonly int level;
    public Vector2 cart, cartVelocity;
    public bool cartEnabled, cartHit, boostUsed;
    public int cartHits;
    float settled;
    readonly System.Collections.Generic.HashSet<int> cartVictims = new System.Collections.Generic.HashSet<int>();
    public DadFunLabModel(int index, bool enableCart = true)
    {
        level = Mathf.Clamp(index, 0, 2);
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
        else { Target(150, 550); Target(365, 450); Target(365, 350); }
    }
    void Target(float x, float y, bool shield = false)
    {
        var f = sim.Add(shield ? SlingshotSim.Kind.Tray : SlingshotSim.Kind.Gum, x, y);
        f.hp = f.maxHp = 1; f.stun = 999; f.cooldown = 999; f.face = Vector2.down;
    }
    public bool Launch(Vector2 pull) { return sim.shots == 0 && sim.Launch(pull); }
    public bool Boost(Vector2 towards)
    {
        if (level != 2 || boostUsed || !sim.dad.flying || sim.Over || towards.sqrMagnitude < 1) return false;
        boostUsed = true; sim.dad.vel = towards.normalized * 1000;
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
            Vector2 oldDad = sim.dad.pos;
            sim.Tick(step);
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
            if (!sim.Over && settled > .6f) { sim.phase = SlingshotSim.Phase.Lost; sim.lossReason = "한 발 끝 · " + sim.AliveCount + "명 남음"; }
        }
    }
    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    { Vector2 v = b - a; return Vector2.Distance(p, a + v * (v.sqrMagnitude < .001f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, v) / v.sqrMagnitude))); }
}
