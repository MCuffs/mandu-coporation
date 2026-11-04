using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public sealed partial class DadSchoolPrototype
{
    sealed class Debris { public Vector2 p, v; public float life, total; public Color color; }
    sealed class Corpse { public Vector2 p, v; public int frame; public bool flip; public float size, stand, spin, life = .55f; public string id; }
    readonly List<Debris> debris = new List<Debris>();
    readonly List<Corpse> corpses = new List<Corpse>();
    readonly Dictionary<int, Fighter> remembered = new Dictionary<int, Fighter>();
    readonly int[] lockerDamage = new int[6];
    // Every opponent is a grown man who never left school; the first of each kind per run wears a name tag saying so.
    readonly HashSet<int> introducedKinds = new HashSet<int>();
    readonly Dictionary<int, float> nameTags = new Dictionary<int, float>();
    static int ProfileKind(Fighter e) { return e.boss ? 3 : e.kind == 1 ? 1 : e.shield || e.trayBroken ? 2 : 0; }
    static readonly string[] ProfileNames = { "껌 짱 (34세)", "의자 짱 (41세)", "쟁반 짱 (47세)", "학교 짱 (42세)" };
    static readonly string[] ProfileLines = { "12년째 휴학 중", "동창회 난동 전문", "급식실 무단 점거", "15년째 유급 중" };
    static readonly string[] BossTagNames = { "학교 짱 (42세)", "급식실 실세 (52세)", "체육 선생님 (51세)" };
    static readonly string[] BossTagLines = { "15년째 유급 중", "30년째 급식 조리 중", "만년 기간제" };
    static int TagKey(Fighter e) { return ProfileKind(e) + (e.elite ? 10 : 0); }
    static float BodySize(Fighter e) { return (e.boss ? 205 : e.kind == 1 ? 165 : 142) * (e.elite ? 1.45f : 1); }
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
    // ---- v5 character art ------------------------------------------------------------------------
    const float DadHeight = 150;
    static float StandHeight(Fighter e) { return (e.boss ? 200 : e.kind == 1 ? 150 : 140) * (e.elite ? 1.45f : 1); }
    string EnemyId(Fighter e)
    {
        int floor = Director != null ? Director.Plan.floor : 1;
        return e.boss ? "boss" + Mathf.Clamp(floor, 1, 3) : e.kind == 1 ? "chair" : e.shield || e.trayBroken ? "tray" : "gum";
    }
    void DrawCharacter(string id, string action, int index, Vector2 feet, float standHeight, bool flip, float tilt = 0, bool flash = false, Vector2? stretch = null, float opacity = 1)
    {
        DadArt.Character c;
        if (art == null || !art.characters.TryGetValue(id, out c)) return;
        Texture2D tex = c.Frame(action, index);
        if (tex == null) return;
        float scale = standHeight / c.standHeight, size = DadArt.Canvas * scale;
        Matrix4x4 old = GUI.matrix; Vector2 shape = stretch ?? Vector2.one;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(feet.x, feet.y, 0), Quaternion.Euler(0, 0, tilt), new Vector3((flip ? -1 : 1) * shape.x, shape.y, 1));
        // Hit flash: a light warm tint (the old orange multiply turned the whole body into a red silhouette, v4 M4).
        GUI.color = flash ? new Color(1, .86f, .74f, opacity) : new Color(1, 1, 1, opacity);
        GUI.DrawTexture(new Rect(-DadArt.PivotX * scale, -DadArt.PivotY * scale, size, size), tex);
        GUI.color = Color.white; GUI.matrix = old;
    }
    string DadAction(out int index)
    {
        index = 0;
        if (specialPose > 0) return "special";
        if (Combat.playerReaction > 0) return "hurt";
        if (Combat.attackLead > 0) return "punch";
        if (Combat.punch > .01f) { index = 1; return "punch"; }
        if (Combat.attackAnimation > .05f) { index = 2; return "punch"; }
        if (Combat.moveVelocity.magnitude > 15) { index = Mathf.FloorToInt(walkPhase); return "walk"; }
        index = Mathf.FloorToInt(clock / .6f); return "idle";
    }
    string EnemyAction(Fighter e, out int index)
    {
        index = 0;
        bool airborne = e.airDuration > 0 && e.airElapsed < e.airDuration || e.velocity.sqrMagnitude > 220 * 220;
        if (airborne) return "fly";
        int step = Mathf.FloorToInt(clock * 4 + e.id);
        if (e.boss)
        {
            if (e.groggy > 0) return "groggy";
            if (e.enraged && e.flash > .12f) return "enrage";
            if (e.dash > 0) return "dash";
            if (e.windup > 0) return "windup";
            if (e.reaction > 0 || e.flash > 0) return "hit";
        }
        else if (e.kind == 1)
        {
            if (e.dash > 0) return "dash";
            if (e.windup > 0) return "windup";
            if (e.reaction > 0 || e.flash > 0) return "hit";
        }
        else if (e.shield || e.trayBroken)
        {
            if (e.shield && e.reaction > 0) return "block";
            if (e.shield && (e.windup > 0 || e.recovery > 0)) return "attack";
            string bare = e.trayBroken ? "notray_" : "";
            if (e.reaction > 0 || e.flash > 0) return bare + "hit";
            index = e.locomotion.magnitude > 20 ? step : 0; return bare + "walk";
        }
        else
        {
            if (e.windup > 0) return "attack";
            if (e.recovery > 0) { index = 1; return "attack"; }
            if (e.reaction > 0 || e.flash > 0) return "hit";
        }
        index = e.locomotion.magnitude > 20 ? step : 0; return "walk";
    }

    // ---- v5 sound -----------------------------------------------------------------------------------
    void PlaySfx(string name, float volume = .7f, float pitch = 1)
    {
        AudioClip clip = art != null ? art.Clip(name) : null;
        if (clip == null || audioSource == null) return;
        audioSource.pitch = pitch; audioSource.PlayOneShot(clip, volume);
    }
    void PlayEventSound(CombatEvent e, bool wall, bool clang)
    {
        if (art == null || art.audio.Count == 0)
        {
            audioSource.pitch = clang ? 1.75f : wall ? .8f : 1 + Mathf.Min(Combat.chain, 6) * .045f;
            audioSource.PlayOneShot(wall || e.style >= 2 ? wallClip : hitClip, wall ? .34f : .18f);
            return;
        }
        if (e.text == "쟁반 박살!") PlaySfx("sfx_tray_break", .75f);
        else if (clang) PlaySfx("sfx_tray_block", .65f);
        else if (wall || e.text.StartsWith("짱 벽꿍")) PlaySfx(Random.value < .5f ? "sfx_wall_1" : "sfx_wall_2", .8f);
        else if (e.text == "대장 광폭화!!") PlaySfx("sfx_boss_enrage", .8f);
        else if (e.text.StartsWith("연쇄") || e.text.StartsWith("핀볼") || e.text.StartsWith("그로기")) PlaySfx("sfx_chain", .75f, 1 + Mathf.Min(Combat.chain, 6) * .04f);
        else PlaySfx("sfx_punch_" + Random.Range(1, 4), .6f, Random.Range(.94f, 1.06f));
    }
    void UpdateMusic()
    {
        if (music == null || art == null) return;
        bool fighting = Combat != null && (Combat.state == RunState.Fighting || Combat.state == RunState.Choosing);
        AudioClip want = Home ? art.Clip("bgm_home") : fighting ? art.Clip("bgm_battle") : null;   // results stay quiet for the jingle
        if (music.clip == want) return;
        music.clip = want;
        if (want != null) music.Play(); else music.Stop();
    }
    void Icon(string name, Rect r) { Texture2D t = art != null ? art.Icon(name) : null; if (t != null) GUI.DrawTexture(r, t, ScaleMode.ScaleToFit); }

    void ResetJuice()
    {
        debris.Clear(); corpses.Clear(); remembered.Clear(); chainBadge = 0; displayedChain = 0; introducedKinds.Clear(); nameTags.Clear();
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
            // The knocked-out body keeps flying out of the fight instead of fading on the spot (v4 M5).
            // Always away from dad and a little upward, even after a wall bounce sent the body back toward him.
            Vector2 fromDad = e.pos - Combat.player; if (fromDad.sqrMagnitude < 1) fromDad = new Vector2(-e.facing, 0);
            Vector2 away = fromDad.normalized * Mathf.Max(260, e.velocity.magnitude * .5f) + new Vector2(0, -90);
            corpses.Add(new Corpse { p = e.pos, v = away, frame = EnemyFrame(e), flip = e.facing > 0, size = BodySize(e), id = EnemyId(e), stand = StandHeight(e), spin = (e.facing > 0 ? 1 : -1) * Random.Range(120f, 260f) });
        }
        for (int i = corpses.Count - 1; i >= 0; i--)
        {
            Corpse c = corpses[i]; c.life -= dt; c.p += c.v * dt; c.v *= Mathf.Exp((NewArt ? -2.5f : -8) * dt);
            c.p.x = Mathf.Clamp(c.p.x, PlayArea.xMin, PlayArea.xMax); c.p.y = Mathf.Clamp(c.p.y, PlayArea.yMin, PlayArea.yMax);
            if (c.life <= 0) corpses.RemoveAt(i);
        }
        for (int i = debris.Count - 1; i >= 0; i--)
        {
            Debris d = debris[i]; d.life -= dt; d.p += d.v * dt; d.v.y += 220 * dt;
            if (d.life <= 0) debris.RemoveAt(i);
        }
        chainBadge = Mathf.Max(0, chainBadge - dt);
        foreach (Fighter e in Combat.enemies) if (introducedKinds.Add(TagKey(e))) nameTags[e.id] = 3f;
        var tagIds = new List<int>(nameTags.Keys);
        foreach (int id in tagIds) { nameTags[id] -= dt; if (nameTags[id] <= 0 || !Combat.enemies.Exists(e => e.id == id)) nameTags.Remove(id); }
    }
    void DrawNameTag(Fighter e, Vector2 feet, float size)
    {
        float t; if (!nameTags.TryGetValue(e.id, out t)) return;
        float alpha = Mathf.Clamp01(t / .4f), x = Mathf.Clamp(feet.x, 112, Width - 112), y = feet.y - e.lift - size - 58;
        if (y < 220) y = feet.y + 12; // above the head would sit under the banner strip (bosses enter at the top), so hang it below the feet
        int k = ProfileKind(e), floor = Director != null ? Mathf.Clamp(Director.Plan.floor - 1, 0, 2) : 0; Rect r = new Rect(x - 100, y, 200, 48);
        string name = e.boss ? BossTagNames[floor] : (e.elite ? "왕 " : "") + ProfileNames[k], line = e.boss ? BossTagLines[floor] : ProfileLines[k];
        Box(r, new Color(Ink.r, Ink.g, Ink.b, alpha)); Box(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), new Color(Paper.r, Paper.g, Paper.b, alpha));
        Text(new Rect(r.x, r.y + 2, r.width, 24), name, 18, new Color(Red.r, Red.g, Red.b, alpha), true, TextAnchor.MiddleCenter);
        Text(new Rect(r.x, r.y + 24, r.width, 20), line, 14, new Color(Ink.r, Ink.g, Ink.b, alpha), false, TextAnchor.MiddleCenter);
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
            if (NewArt && c.id != null) DrawCharacter(c.id, "fly", 0, c.p - new Vector2(0, 30 + progress * 40), c.stand, c.flip, c.spin * progress * .55f, false, null, Mathf.Clamp01(c.life / .25f));
            else Sprite(enemies, enemyFrames, c.frame, c.p, c.size, c.flip, (c.flip ? 1 : -1) * (20 + progress * 60), false, new Vector2(1 + progress * .2f, 1 - progress * .6f), Mathf.Clamp01(c.life / .22f));
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
