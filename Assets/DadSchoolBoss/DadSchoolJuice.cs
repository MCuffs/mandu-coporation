using System.Collections.Generic;
using UnityEngine;
using SecondFistGame;

public sealed partial class DadSchoolPrototype
{
    sealed class Debris { public Vector2 p, v; public float life, total; public Color color; }
    sealed class Corpse { public Vector2 p, v; public int frame; public bool flip, wall, right; public float size, stand, spin, lift, life = .55f; public string id; }
    readonly List<Debris> debris = new List<Debris>();
    readonly List<Corpse> corpses = new List<Corpse>();
    readonly Dictionary<int, Fighter> remembered = new Dictionary<int, Fighter>();
    readonly int[] lockerDamage = new int[6];
    // "아빠 전쟁": every opponent is some other kid's dad (Design/dad-war-plan.md); the first of each kind per run wears a name tag.
    readonly HashSet<int> introducedKinds = new HashSet<int>();
    readonly Dictionary<int, float> nameTags = new Dictionary<int, float>();
    static int ProfileKind(Fighter e) { return e.boss ? 3 : e.kind == 1 ? 1 : e.shield || e.trayBroken ? 2 : 0; }
    static readonly string[] ProfileNames = { "지호 아빠 (34세)", "서연이 아빠 (41세)", "민준이 아빠 (47세)", "학부모회장 아빠 (42세)" };
    static readonly string[] ProfileLines = { "육아휴직 3개월 차", "운동회 자리 맡기 전문", "급식 봉사 12년 차", "학부모 단톡방 방장" };
    // Mid-bosses are the bigger brother of a regular: 큰아빠 (the kid's uncle, literally "big dad").
    static readonly string[] EliteTagNames = { "지호 큰아빠 (39세)", "서연이 큰아빠 (46세)", "민준이 큰아빠 (52세)" };
    static readonly string[] EliteTagLines = { "동생 대신 출동", "의자도 두 개", "쟁반도 두 배" };
    static readonly string[] BossTagNames = { "학부모회장 아빠 (42세)", "급식 봉사 반장 (52세)", "체육 선생님 (51세)" };
    static readonly string[] BossTagLines = { "학부모 단톡방 방장", "세쌍둥이 아빠", "두 아이 아빠 · 계주 10연패" };
    static int TagKey(Fighter e) { return ProfileKind(e) + (e.elite ? 10 : 0); }
    static float BodySize(Fighter e) { return (e.boss ? 205 : e.kind == 1 ? 165 : 142) * (e.elite ? 1.45f : 1); }
    float chainBadge;
    int displayedChain;
    // The three loadouts are the special launches (C-10; their old item names clashed with lunchbox gear, PM R8).
    public static readonly string[] LaunchNames = { "벽꿍형", "연쇄형", "폭주형" };
    string GearName { get { return LaunchNames[Mathf.Clamp(Gear, 0, 2)]; } }
    string NextGoal
    {
        get
        {
            if (!ChainGearUnlocked) return "다음 특수 발사: 연쇄형\n최고 " + BestChain + " / 3연쇄 또는 첫 승리";
            if (!PhoneGearUnlocked) return "다음 특수 발사: 폭주형\n누적 벽꿍 " + TotalWallHits + " / " + PhoneWallGoal + "회  (" + (PhoneWallGoal - TotalWallHits) + "회 남음)";
            return "특수 발사 3종 해금 완료!\n한 발 최고 " + BestChain + "연쇄, 더 늘려 보세요";
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
        Texture2D source=tex; if(id=="dad"&&outfit!=null)tex=outfit.Body(source,Progress);
        float scale = standHeight / c.standHeight, size = DadArt.Canvas * scale;
        Matrix4x4 old = GUI.matrix; Vector2 shape = stretch ?? Vector2.one;
        GUI.matrix = old * Matrix4x4.TRS(new Vector3(feet.x, feet.y, 0), Quaternion.Euler(0, 0, tilt), new Vector3((flip ? -1 : 1) * shape.x, shape.y, 1));
        // Hit flash: a light warm tint (the old orange multiply turned the whole body into a red silhouette, v4 M4).
        GUI.color = flash ? new Color(1, .86f, .74f, opacity) : new Color(1, 1, 1, opacity);
        GUI.DrawTexture(new Rect(-DadArt.PivotX * scale, -DadArt.PivotY * scale, size, size), tex);
        if(id=="dad"&&outfit!=null)outfit.Layers(source.name,new Rect(-DadArt.PivotX*scale,-DadArt.PivotY*scale,size,size),Progress);
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
        if (Combat.moveVelocity.magnitude > 15)
        {
            if (dadStartPose > 0 && HasPose("dad", "movestart")) return "movestart";
            index = Mathf.FloorToInt(walkPhase); return "walk";
        }
        if (dadStopPose > 0 && HasPose("dad", "movestop")) return "movestop";
        index = Mathf.FloorToInt(clock / .6f); return "idle";
    }
    // ---- v7 motion links (Design/dad-motion-assets-v7.md) ------------------------------------------
    const float GumStride = 20;           // px walked per frame of the gum cycle walk_0 > pass_0 > walk_1 > pass_1
    const float WallBackOffset = 150;     // canvas px from the pivot to the back of the wall poses (Tools/prepare_dad_motion_v7.py)
    const float WallPress = 12;           // how far the back may sink into the wall line on impact
    const float WallHitTime = .07f, CrumpleTime = .23f;
    readonly Dictionary<int, float> strides = new Dictionary<int, float>();
    bool dadWasMoving;
    float dadStateTime, dadStartPose, dadStopPose;
    bool HasPose(string id, string action) { DadArt.Character c; return art != null && art.characters.TryGetValue(id, out c) && c.Has(action); }
    bool AtSideWall(Fighter e) { return Mathf.Abs(e.pos.x - PlayArea.xMin) < 1.5f || Mathf.Abs(e.pos.x - PlayArea.xMax) < 1.5f; }
    // Only a real side-wall contact in the last few frames counts; being airborne alone never plays the wall pose.
    bool WallHitNow(Fighter e) { return e.wallCooldown > .30f - WallHitTime && AtSideWall(e); }
    float WallPoseX(bool right, float stand, float feetX, bool impact)
    {
        WallFace face = CurrentWall; float line = right ? face.backRight : face.backLeft, off = WallBackOffset * stand / 560f;
        if (!impact) return right ? line - off : line + off;                       // slumped with the back on the wall
        return right ? Mathf.Min(feetX, line + WallPress - off) : Mathf.Max(feetX, line - WallPress + off);
    }
    void UpdateMotion(float dt)
    {
        // Physics already moved; these poses only dress the transition, and a quick jiggle gets neither.
        bool moving = Combat.moveVelocity.magnitude > 15;
        if (moving != dadWasMoving)
        {
            if (moving) { dadStopPose = 0; if (dadStateTime > .18f) { dadStartPose = .08f; walkPhase = 0; } }
            else { dadStartPose = 0; if (dadStateTime > .25f) dadStopPose = .1f; }
            dadWasMoving = moving; dadStateTime = 0;
        }
        dadStateTime += dt; dadStartPose = Mathf.Max(0, dadStartPose - dt); dadStopPose = Mathf.Max(0, dadStopPose - dt);
        foreach (Fighter e in Combat.enemies)
        {
            float walked; strides.TryGetValue(e.id, out walked);
            if (e.velocity.sqrMagnitude <= 28 * 28 && e.lift <= 0) walked += e.locomotion.magnitude * dt;   // knockback and flight never advance the walk
            strides[e.id] = walked;
        }
    }
    string EnemyAction(Fighter e, out int index)
    {
        index = 0;
        bool gum = !e.boss && e.kind != 1 && !e.shield && !e.trayBroken;
        if (gum && WallHitNow(e) && HasPose("gum", "wallhit")) return "wallhit";
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
            if (HasPose("gum", "pass"))
            {
                float walked; strides.TryGetValue(e.id, out walked);
                int phase = Mathf.FloorToInt(walked / GumStride) % 4;
                if (e.locomotion.magnitude <= 20) { index = (phase + 1) / 2 % 2; return "walk"; }   // standing still: nearest contact pose, frozen
                index = phase / 2; return phase % 2 == 1 ? "pass" : "walk";
            }
        }
        index = e.locomotion.magnitude > 20 ? step : 0; return "walk";
    }

    // ---- v5 sound -----------------------------------------------------------------------------------
    void PlaySfx(string name, float volume = .7f, float pitch = 1)
    {
        AudioClip clip = art != null ? art.Clip(name) : null;
        if (!DadPreferences.Effects || clip == null || audioSource == null) return;
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
        music.mute=!DadPreferences.Music;
        bool fighting = Combat != null && (Combat.state == RunState.Fighting || Combat.state == RunState.Choosing);
        // Results stay quiet for the jingle; the opening is silent on the company splash, then home music under title and story.
        AudioClip want = InOpening ? (Opening == OpeningStep.Splash ? null : art.Clip("bgm_home")) : Home ? art.Clip("bgm_home") : fighting ? art.Clip("bgm_battle") : null;
        if (music.clip == want) return;
        music.clip = want;
        if (want != null) music.Play(); else music.Stop();
    }
    void Icon(string name, Rect r) { Texture2D t = art != null ? art.Icon(name) : null; if (t != null) GUI.DrawTexture(r, t, ScaleMode.ScaleToFit); }

    void ResetJuice()
    {
        debris.Clear(); corpses.Clear(); remembered.Clear(); chainBadge = 0; displayedChain = 0; introducedKinds.Clear(); nameTags.Clear();
        for (int i = 0; i < lockerDamage.Length; i++) lockerDamage[i] = 0;
        dents.Clear(); strides.Clear(); dadWasMoving = false; dadStateTime = 0; dadStartPose = 0; dadStopPose = 0;
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
            if (EnemyId(e) == "gum" && NewArt && HasPose("gum", "crumple") && e.wallCooldown > .30f - .12f && AtSideWall(e))
            {
                // Knocked out against the wall: wall hit, then he slumps down where he hit instead of flying off.
                bool right = e.pos.x > PlayArea.center.x;
                corpses.Add(new Corpse { p = e.pos, wall = true, right = right, flip = !right, lift = e.lift, id = "gum", stand = StandHeight(e), size = BodySize(e), frame = EnemyFrame(e) });
                continue;
            }
            // The knocked-out body keeps flying out of the fight instead of fading on the spot (v4 M5).
            // Always away from dad and a little upward, even after a wall bounce sent the body back toward him.
            Vector2 fromDad = e.pos - Combat.player; if (fromDad.sqrMagnitude < 1) fromDad = new Vector2(-e.facing, 0);
            Vector2 away = fromDad.normalized * Mathf.Max(260, e.velocity.magnitude * .5f) + new Vector2(0, -90);
            corpses.Add(new Corpse { p = e.pos, v = away, frame = EnemyFrame(e), flip = e.facing > 0, size = BodySize(e), id = EnemyId(e), stand = StandHeight(e), spin = (e.facing > 0 ? 1 : -1) * Random.Range(120f, 260f) });
        }
        for (int i = corpses.Count - 1; i >= 0; i--)
        {
            Corpse c = corpses[i]; c.life -= dt;
            if (c.wall) { if (c.life <= 0) corpses.RemoveAt(i); continue; }
            c.p += c.v * dt; c.v *= Mathf.Exp((NewArt ? -2.5f : -8) * dt);
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
        float alpha = Mathf.Clamp01(t / .4f), x = Mathf.Clamp(feet.x, 122, Width - 122), y = feet.y - e.lift - size - 58;
        if (y < 220) y = feet.y + 12; // above the head would sit under the banner strip (bosses enter at the top), so hang it below the feet
        int k = ProfileKind(e), floor = Director != null ? Mathf.Clamp(Director.Plan.floor - 1, 0, 2) : 0; Rect r = new Rect(x - 118, y, 236, 48);
        string name = e.boss ? BossTagNames[floor] : e.elite && k < 3 ? EliteTagNames[k] : ProfileNames[k], line = e.boss ? BossTagLines[floor] : e.elite && k < 3 ? EliteTagLines[k] : ProfileLines[k];
        Box(r, new Color(Ink.r, Ink.g, Ink.b, alpha)); Box(new Rect(r.x + 3, r.y + 3, r.width - 6, r.height - 6), new Color(Paper.r, Paper.g, Paper.b, alpha));
        Text(new Rect(r.x, r.y + 2, r.width, 24), name, 18, new Color(Red.r, Red.g, Red.b, alpha), true, TextAnchor.MiddleCenter);
        Text(new Rect(r.x, r.y + 24, r.width, 20), line, 14, new Color(Ink.r, Ink.g, Ink.b, alpha), false, TextAnchor.MiddleCenter);
    }
    void ImpactJuice(CombatEvent e)
    {
        bool wall = e.text.StartsWith("벽꿍") || e.text.StartsWith("폭파벽");
        bool tray = e.text == "쟁반 박살!", blocked = e.text == "쟁반 막기!" || e.text == "팅!";
        bool hit = wall || tray || blocked || e.text == "퍽!" || e.text == "크리티컬!" || e.text.StartsWith("연쇄");
        if (wall || e.text.StartsWith("짱 벽꿍")) DentWall(e.pos);
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
            if (c.wall)
            {
                bool impact = .55f - c.life < WallHitTime;
                Vector2 at = new Vector2(WallPoseX(c.right, c.stand, c.p.x, impact), c.p.y - (impact ? c.lift : 0));
                DrawCharacter("gum", impact ? "wallhit" : "crumple", 0, at, c.stand, c.flip, 0, impact, null, Mathf.Clamp01(c.life / (.55f - WallHitTime - CrumpleTime)));
                continue;
            }
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
