using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public sealed partial class DadSchoolPrototype
{
    sealed class Debris { public Vector2 p, v; public float life, total; public Color color; }
    sealed class Corpse { public Vector2 p, v; public int frame; public bool flip; public float size, life = .55f; }
    readonly List<Debris> debris = new List<Debris>();
    readonly List<Corpse> corpses = new List<Corpse>();
    readonly Dictionary<int, Fighter> remembered = new Dictionary<int, Fighter>();
    readonly int[] lockerDamage = new int[6];
    float chainBadge;
    int displayedChain;
    string GearName { get { return Gear == 0 ? "고무 슬리퍼" : Gear == 1 ? "튼튼한 도시락" : "폴더폰"; } }
    string NextGoal
    {
        get
        {
            if (!ChainGearUnlocked) return "다음 준비물: 튼튼한 도시락\n최고 " + BestChain + " / 3연쇄 또는 첫 배달 성공";
            if (!PhoneGearUnlocked) return "다음 준비물: 폴더폰\n누적 벽꿍 " + TotalWallHits + " / " + PhoneWallGoal + "회  (" + (PhoneWallGoal - TotalWallHits) + "회 남음)";
            return "준비물 3종 해금 완료!\n다른 장비로 최고 " + BestChain + "연쇄에 도전하세요";
        }
    }
    // Atlas frames: 0 gum guy (basic), 1 chair charger, 2 tray guy, 3 school boss. The tray look is reserved for the tray role.
    static int EnemyFrame(Fighter e) { return e.boss ? 3 : e.kind == 1 ? 1 : e.shield || e.trayBroken ? 2 : 0; }
    void ResetJuice()
    {
        debris.Clear(); corpses.Clear(); remembered.Clear(); chainBadge = 0; displayedChain = 0;
        for (int i = 0; i < lockerDamage.Length; i++) lockerDamage[i] = 0;
    }
    void RememberEnemies()
    {
        remembered.Clear();
        foreach (Fighter e in Combat.enemies) remembered[e.id] = e;
    }
    void UpdateJuice(float dt)
    {
        foreach (Fighter e in remembered.Values)
        {
            if (e.hp > 0) continue;
            corpses.Add(new Corpse { p = e.pos, v = e.velocity * .25f, frame = EnemyFrame(e), flip = e.facing > 0, size = e.boss ? 205 : e.kind == 1 ? 165 : 142 });
        }
        for (int i = corpses.Count - 1; i >= 0; i--)
        {
            Corpse c = corpses[i]; c.life -= dt; c.p += c.v * dt; c.v *= Mathf.Exp(-8 * dt);
            c.p.x = Mathf.Clamp(c.p.x, PlayArea.xMin, PlayArea.xMax); c.p.y = Mathf.Clamp(c.p.y, PlayArea.yMin, PlayArea.yMax);
            if (c.life <= 0) corpses.RemoveAt(i);
        }
        for (int i = debris.Count - 1; i >= 0; i--)
        {
            Debris d = debris[i]; d.life -= dt; d.p += d.v * dt; d.v.y += 220 * dt;
            if (d.life <= 0) debris.RemoveAt(i);
        }
        chainBadge = Mathf.Max(0, chainBadge - dt);
    }
    void ImpactJuice(CombatEvent e)
    {
        bool wall = e.text.StartsWith("벽꿍") || e.text.StartsWith("폭파벽");
        bool tray = e.text == "쟁반 박살!", blocked = e.text == "쟁반 막기!" || e.text == "팅!";
        bool hit = wall || tray || blocked || e.text == "퍽!" || e.text == "크리티컬!" || e.text.StartsWith("연쇄");
        if (!hit) return;
        Vector2 center = e.pos - new Vector2(0, 65);
        int count = wall ? 14 : tray ? 12 : blocked ? 5 : 7;
        if (wall)
        {
            bool right = e.pos.x > PlayArea.center.x;
            int row = Mathf.Clamp(Mathf.RoundToInt((e.pos.y - 340) / 160), 0, 2);
            if (Mathf.Min(Mathf.Abs(e.pos.x - PlayArea.xMin), Mathf.Abs(e.pos.x - PlayArea.xMax)) < 35)
                lockerDamage[(right ? 3 : 0) + row] = Mathf.Min(3, lockerDamage[(right ? 3 : 0) + row] + 1);
            Combat.freeze = Mathf.Max(Combat.freeze, .035f);
        }
        if (Combat.chain >= 2) { displayedChain = Combat.chain; chainBadge = .9f; }
        for (int i = 0; i < count && debris.Count < 100; i++)
        {
            float angle = i * Mathf.PI * 2 / count;
            debris.Add(new Debris { p = center, v = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (wall ? 150 : tray ? 170 : 95), total = .3f, life = .3f, color = wall ? Yellow : tray ? new Color(.72f, .76f, .78f) : Color.white });
        }
    }
    void DrawLockers()
    {
        for (int side = 0; side < 2; side++) for (int row = 0; row < 3; row++)
        {
            float x = side == 0 ? 30 : 476, y = 290 + row * 160;
            int damage = lockerDamage[side * 3 + row];
            Color steel = damage >= 2 ? new Color(.43f, .49f, .47f) : new Color(.48f, .58f, .59f);
            Panel(new Rect(x, y, 34, 95), steel);
            for (int n = 0; n < 3; n++) Line(new Vector2(x + 8, y + 14 + n * 6), new Vector2(x + 27, y + 14 + n * 6), 2, Ink);
            if (damage > 0)
            {
                Vector2 dent = new Vector2(x + 17, y + 55);
                Line(dent - new Vector2(9, 13), dent + new Vector2(6, -2), 3, Ink);
                Line(dent + new Vector2(6, -2), dent - new Vector2(7, -12), 3, Ink);
                if (damage >= 2) { Line(dent - new Vector2(7, -12), dent + new Vector2(12, 22), 3, Ink); Text(new Rect(x - 2, y + 65, 40, 25), "쾅", 15, Ink, true, TextAnchor.MiddleCenter); }
            }
            else Box(new Rect(x + 23, y + 52, 4, 12), Ink);
        }
    }
    void DrawCorpses()
    {
        foreach (Corpse c in corpses)
        {
            float progress = 1 - c.life / .55f;
            Sprite(enemies, enemyFrames, c.frame, c.p, c.size, c.flip, (c.flip ? 1 : -1) * (20 + progress * 60), false, new Vector2(1 + progress * .2f, 1 - progress * .6f), Mathf.Clamp01(c.life / .22f));
        }
    }
    void DrawImpactJuice()
    {
        foreach (Debris d in debris)
        {
            Color c = d.color; c.a = d.life / d.total;
            Line(d.p, d.p - d.v.normalized * (8 + d.life * 20), 3, c);
        }
        if (chainBadge > 0)
        {
            Rect badge = new Rect(150, 211, 240, 48); Panel(badge, Yellow);
            Text(badge, displayedChain + "연쇄!!", 31, Red, true, TextAnchor.MiddleCenter);
        }
    }
}
