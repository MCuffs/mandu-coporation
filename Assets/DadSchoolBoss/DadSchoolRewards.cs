using System.Collections.Generic;
using UnityEngine;

// v6 reward art in play (Design/dad-reward-assets-v6.md): mom's lunchbox opening, the gear card that comes out of it,
// the six equipped slots, and dents the wall slams leave on the lockers.
public sealed partial class DadSchoolPrototype
{
    // ---- lunchbox reveal ------------------------------------------------------------------------
    const float RevealOpenTime = .55f, RevealCardTime = .25f;
    const float LunchboxFloor = 994f / 1111f;   // floor line of the shared lunchbox crop (Tools/prepare_dad_rewards_v6.py prints it)
    static readonly Color[] RarityColors = { new Color(.66f, .63f, .6f), new Color(.3f, .52f, .86f), new Color(1f, .76f, .12f) };
    // Card layout as fractions of each frame's height (header band, body, footer band), measured on the cropped cards.
    static readonly float[] CardHeader = { .104f, .105f, .133f }, CardBodyTop = { .165f, .16f, .25f }, CardBodyBottom = { .84f, .837f, .843f }, CardFooter = { .898f, .895f, .897f };
    float revealClock = -1, toastTime;
    string toast = "";
    public bool Revealing { get { return revealClock >= 0; } }
    public bool RevealShowsCard { get { return Revealing && revealClock >= RevealOpenTime; } }

    public bool BeginReveal()
    {
        if (Progress == null || Revealing) return false;
        if (Progress.HasPendingGear) { revealClock = RevealOpenTime; return true; }   // a card left open last session comes straight back
        if (!Progress.OpenLunchbox()) return false;
        PersistNow(); revealClock = 0; PlaySfx("sfx_box_common", .8f);
        return true;
    }
    public void SkipRevealToCard() { if (Revealing && revealClock < RevealOpenTime) { revealClock = RevealOpenTime; RevealSound(); } }
    public void DecideReveal(bool equip)
    {
        if (!Revealing || !Progress.HasPendingGear) { revealClock = -1; return; }
        int slot = Progress.Save.pendingSlot, rarity = Progress.Save.pendingRarity; float value = Progress.Save.pendingValue;
        double coins = equip ? Progress.EquipPending() : Progress.DismantlePending();
        toast = equip ? DadProgress.ItemName(slot, rarity) + " 장착!  전투력 +" + GearPoints(value) + (coins > 0 ? "  ·  헌 장비 코인 +" + Mathf.RoundToInt((float)coins) : "")
                      : "분해!  코인 +" + Mathf.RoundToInt((float)coins);
        toastTime = 2f; revealClock = -1; PersistNow(); PlaySfx(equip ? "sfx_skill_pick" : "sfx_coin", .7f);
    }
    void OpenAllBoxes()
    {
        int equipped; double coins; int n = Progress.OpenAllLunchboxes(out equipped, out coins);
        toast = "도시락 " + n + "개  ·  장착 " + equipped + "  ·  분해 코인 +" + Mathf.RoundToInt((float)coins); toastTime = 2.4f;
        PersistNow(); PlaySfx("sfx_box_rare", .8f);
    }
    void RevealSound() { int r = Progress.Save.pendingRarity; PlaySfx(r >= 2 ? "sfx_box_legend" : r == 1 ? "sfx_box_rare" : "sfx_coin", .85f); }
    void UpdateRewards(float dt)
    {
        toastTime = Mathf.Max(0, toastTime - dt);
        foreach (WallDent d in dents) d.pop = Mathf.Max(0, d.pop - dt);
        if (!Revealing) return;
        float before = revealClock; revealClock += dt;
        if (before < RevealOpenTime && revealClock >= RevealOpenTime) RevealSound();
    }
    // Power points an item adds right now (the card shows points; the percent is the real multiplier share).
    int GearPoints(float value) { return Mathf.Max(1, Mathf.RoundToInt(Progress.Power / Progress.GearMultiplier * value)); }

    void DrawLunchbox(Vector2 floor, float width, float t)
    {
        string pose = t < .2f ? "lunchbox_closed" : t < .36f ? "lunchbox_opening" : "lunchbox_open";
        Texture2D tex = art != null ? art.Reward(pose) : null;
        if (tex == null) { Icon("icon_lunchbox", new Rect(floor.x - width / 2, floor.y - width, width, width)); return; }
        // Three hand-drawn poses, not a frame-exact animation: squash, stretch up as the lid lifts, pop and settle.
        Vector2 s = Vector2.one;
        if (t > 0 && t < .2f) { float a = Mathf.Sin(t / .2f * Mathf.PI); s = new Vector2(1 + .07f * a, 1 - .08f * a); }
        else if (t >= .2f && t < .36f) { float a = (t - .2f) / .16f; s = new Vector2(.95f + .04f * a, 1.04f + .05f * a); }
        else if (t >= .36f && t < RevealOpenTime) { float a = Mathf.Clamp01((t - .36f) / (RevealOpenTime - .36f)); float o = 1 + .12f * (1 - a) * Mathf.Cos(a * Mathf.PI * 1.5f); s = new Vector2(o, o); }
        float h = width * tex.height / tex.width;
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(floor.x, floor.y, 0), Quaternion.identity, new Vector3(s.x, s.y, 1));
        GUI.DrawTexture(new Rect(-width / 2, -h * LunchboxFloor, width, h), tex);
        GUI.matrix = old;
    }
    void DrawRays(Vector2 center, float radius, Color c, float t)
    {
        c.a = .32f;
        for (int i = 0; i < 12; i++)
        {
            float angle = (i * 30 + t * 35) * Mathf.Deg2Rad; Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Line(center + dir * 50, center + dir * radius, i % 2 == 0 ? 16 : 8, c);
        }
    }
    static float EaseOutBack(float k) { const float c = 1.70158f; float x = k - 1; return 1 + (c + 1) * x * x * x + c * x * x; }

    void DrawGearCard(Vector2 center, Vector2 fit, float scale, int slot, int rarity, float value)
    {
        Texture2D card = art != null ? art.Reward(DadProgress.RarityCards[rarity]) : null;
        float aspect = card != null ? (float)card.width / card.height : .68f, h = Mathf.Min(fit.y, fit.x / aspect), w = h * aspect;
        Matrix4x4 old = GUI.matrix;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(center.x, center.y, 0), Quaternion.identity, Vector3.one * scale);
        Rect r = new Rect(-w / 2, -h / 2, w, h);
        if (card != null) GUI.DrawTexture(r, card); else { Panel(r, RarityColors[rarity]); Box(new Rect(r.x + 10, r.y + 10, w - 20, h - 20), Paper); }
        float header = r.y + h * CardHeader[rarity], top = r.y + h * CardBodyTop[rarity], body = h * (CardBodyBottom[rarity] - CardBodyTop[rarity]), footer = r.y + h * CardFooter[rarity];
        Text(new Rect(r.x, header - 18, w, 36), DadProgress.RarityNames[rarity] + "  ·  " + DadProgress.SlotNames[slot], 20, rarity == 2 ? Red : rarity == 1 ? new Color(.16f, .32f, .62f) : Ink, true, TextAnchor.MiddleCenter);
        Texture2D icon = art != null ? art.Gear(DadProgress.ItemIcon(slot, rarity)) : null;
        float iconSize = Mathf.Min(w * .66f, body * .64f);
        if (icon != null) GUI.DrawTexture(new Rect(-iconSize / 2, top + body * .07f, iconSize, iconSize), icon, ScaleMode.ScaleToFit);
        Text(new Rect(r.x + 14, top + body * .76f, w - 28, body * .22f), DadProgress.ItemName(slot, rarity), 25, Ink, true, TextAnchor.MiddleCenter);
        Text(new Rect(r.x, footer - 18, w, 36), "전투력 +" + GearPoints(value) + "  (+" + (value * 100).ToString("F1") + "%)", 19, Green, true, TextAnchor.MiddleCenter);
        GUI.matrix = old;
    }

    void DrawReveal()
    {
        if (!Revealing || Progress == null) return;
        if (!Progress.HasPendingGear) { revealClock = -1; return; }
        DadSave save = Progress.Save; int slot = save.pendingSlot, rarity = save.pendingRarity; float t = revealClock;
        Box(new Rect(0, 0, Width, Height), new Color(0, 0, 0, .82f));
        if (t < RevealOpenTime)
        {
            if (t > .36f) DrawRays(new Vector2(Width / 2, 520), 120 + 260 * (t - .36f), RarityColors[rarity], t);
            DrawLunchbox(new Vector2(Width / 2, 640), 300, t);
            Text(new Rect(20, 690, 500, 44), "엄마: \"밥은 먹고 싸워.\"", 24, Paper, true, TextAnchor.MiddleCenter);
            if (GUI.Button(new Rect(0, 0, Width, Height), GUIContent.none, GUIStyle.none)) SkipRevealToCard();
            return;
        }
        float k = Mathf.Clamp01((t - RevealOpenTime) / RevealCardTime), pop = EaseOutBack(k);
        Vector2 center = Vector2.Lerp(new Vector2(Width / 2, 560), new Vector2(Width / 2, 372), pop);
        DrawRays(center, 170 + 70 * k + 8 * Mathf.Sin(t * 4), RarityColors[rarity], t);
        if (k < 1) { GUI.color = new Color(1, 1, 1, 1 - k); DrawLunchbox(new Vector2(Width / 2, 640), 300, RevealOpenTime); GUI.color = Color.white; }
        Text(new Rect(20, 92, 500, 56), rarity == 2 ? "최고 등급!!" : rarity == 1 ? "희귀 장비!" : "새 장비", 38, RarityColors[rarity], true, TextAnchor.MiddleCenter);
        if (save.lunchboxes > 0) Text(new Rect(330, 40, 190, 34), "남은 도시락 " + save.lunchboxes, 18, Paper, true, TextAnchor.MiddleRight);
        DrawGearCard(center, new Vector2(272, 390), .35f + .65f * pop, slot, rarity, save.pendingValue);
        if (k < 1) return;
        bool upgrade = Progress.PendingIsUpgrade, has = Progress.HasGear(slot);
        int now = has ? GearPoints(save.gearValue[slot]) : 0, next = GearPoints(save.pendingValue);
        Panel(new Rect(45, 590, 450, 78), Paper);
        Text(new Rect(60, 594, 420, 34), has ? "지금: " + DadProgress.ItemName(slot, save.gearRarity[slot]) + " (" + DadProgress.RarityNames[save.gearRarity[slot]] + ")  전투력 +" + now : DadProgress.SlotNames[slot] + " 부위가 비어 있어요", 18, Ink, false, TextAnchor.MiddleCenter);
        Text(new Rect(60, 628, 420, 36), !has ? "바로 장착하면 전투력 +" + next : upgrade ? "▲ 전투력 +" + (next - now) + " 오름" : "▼ 지금 장비가 " + (now - next) + " 더 셉니다", 22, upgrade ? Green : Red, true, TextAnchor.MiddleCenter);
        if (Button(new Rect(45, 692, 205, 72), "분해\n코인 +" + Mathf.RoundToInt((float)Progress.DismantleCoins(rarity)), upgrade ? Color.white : Yellow, true, 20)) DecideReveal(false);
        if (Button(new Rect(262, 692, 233, 72), "장착!", upgrade ? Yellow : Color.white, true, 28)) DecideReveal(true);
        Text(new Rect(30, 784, 480, 30), "바꾼 헌 장비는 자동으로 분해되어 코인이 됩니다", 16, Paper, false, TextAnchor.MiddleCenter);
    }
    void DrawToast()
    {
        if (toastTime <= 0 || Revealing) return;
        float a = Mathf.Clamp01(toastTime / .35f); Rect r = new Rect(35, 66, 470, 52);   // above the result panel (185) and the gear panel (120)
        Box(r, new Color(Ink.r, Ink.g, Ink.b, a)); Box(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), new Color(Yellow.r, Yellow.g, Yellow.b, a));
        Text(r, toast, 19, new Color(Ink.r, Ink.g, Ink.b, a), true, TextAnchor.MiddleCenter);
    }
    void DrawGearRow(Rect area)
    {
        float w = (area.width - 5 * 6) / DadProgress.SlotCount;
        for (int i = 0; i < DadProgress.SlotCount; i++)
        {
            Rect r = new Rect(area.x + i * (w + 6), area.y, w, area.height);
            bool has = Progress.HasGear(i); int rarity = Progress.Save.gearRarity[i];
            Box(r, Ink); Box(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), has ? RarityColors[rarity] : new Color(.75f, .74f, .7f));
            Box(new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 12), Paper);
            Texture2D icon = has && art != null ? art.Gear(DadProgress.ItemIcon(i, rarity)) : null;
            if (icon != null) GUI.DrawTexture(new Rect(r.x + 8, r.y + 7, r.width - 16, r.height - 14), icon, ScaleMode.ScaleToFit);
            else Text(r, DadProgress.SlotNames[i], 14, new Color(.45f, .44f, .4f), false, TextAnchor.MiddleCenter);
        }
    }

    // ---- locker dents ---------------------------------------------------------------------------
    sealed class WallDent { public int side, spot, level; public bool flip; public float pop; }
    sealed class WallFace { public float[] spots; public float leftX, rightX, width, shear, snap, backLeft = 46, backRight = 494; public bool steel; }
    // Measured on the v5 backgrounds (540x960 layout): wall-face centre x per side, spot centres down the lockers, where a
    // body's back meets the wall (backLeft/backRight; 2F counters stick out further), the
    // slant of the locker faces (y grows outward), and how far a slam may sit from a spot. Doors, mats and vaulting boxes
    // are not metal, so those stretches stay clean. 2F walls are steel counters, rice cookers and carts all the way down.
    static readonly WallFace[] WallFaces =
    {
        new WallFace { spots = new[] { 466f, 548f }, leftX = 22, rightX = 518, width = 46, shear = 1.3f, snap = 46 },
        new WallFace { spots = new[] { 290f, 365f, 440f, 515f, 590f, 665f, 740f }, leftX = 30, rightX = 510, width = 50, shear = 0, snap = 40, steel = true, backLeft = 62, backRight = 478 },
        new WallFace { spots = new[] { 580f, 652f }, leftX = 22, rightX = 518, width = 46, shear = .95f, snap = 46 },
    };
    readonly List<WallDent> dents = new List<WallDent>();
    public int DentCount { get { return dents.Count; } }
    public int MaxDentLevel { get { int m = 0; foreach (WallDent d in dents) m = Mathf.Max(m, d.level); return m; } }
    WallFace CurrentWall { get { int floor = Director != null ? Director.Plan.floor : 1; return WallFaces[Mathf.Clamp(floor - 1, 0, WallFaces.Length - 1)]; } }

    // A side-wall slam dents the nearest locker spot one stage deeper (at most 3 per spot per battle).
    public void DentWall(Vector2 feet)
    {
        bool left = Mathf.Abs(feet.x - PlayArea.xMin) < 35, right = Mathf.Abs(feet.x - PlayArea.xMax) < 35;
        if (!left && !right) return;                 // top and bottom slams have no locker to dent
        WallFace face = CurrentWall; int side = right ? 1 : 0;
        float hit = feet.y - 60;                     // the body meets the wall about chest-high
        int spot = -1; float best = face.snap;
        for (int i = 0; i < face.spots.Length; i++) { float d = Mathf.Abs(face.spots[i] - hit); if (d < best) { best = d; spot = i; } }
        if (spot < 0) return;
        WallDent dent = dents.Find(x => x.side == side && x.spot == spot);
        if (dent == null) { dent = new WallDent { side = side, spot = spot, flip = (side == 1) ^ (spot % 2 == 1) }; dents.Add(dent); }
        if (dent.level >= 3) return;
        dent.level++; dent.pop = .18f;
    }
    void DrawWallDents()
    {
        if (art == null || dents.Count == 0) return;
        WallFace face = CurrentWall;
        foreach (WallDent d in dents)
        {
            Texture2D tex = art.Reward("locker_damage_" + d.level + (face.steel ? "_steel" : ""));
            if (tex == null) continue;
            float w = face.width, h = w * tex.height / tex.width, pop = 1 + d.pop * 1.6f;
            Vector2 c = new Vector2(d.side == 0 ? face.leftX : face.rightX, face.spots[d.spot]);
            // Shear so the decal lies on the slanted locker face: outward is lower, so y grows toward the outer edge.
            Matrix4x4 shear = Matrix4x4.identity; shear.m10 = d.side == 0 ? -face.shear : face.shear;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = old * Matrix4x4.Translate(new Vector3(c.x, c.y, 0)) * shear * Matrix4x4.Scale(new Vector3((d.flip ? -1 : 1) * pop, pop, 1));
            GUI.DrawTexture(new Rect(-w / 2, -h / 2, w, h), tex);
            GUI.matrix = old;
        }
    }
}
