using System.Collections.Generic;
using UnityEngine;

// Art and sound from the v5 delivery (Design/dad-asset-request-v5.md), loaded from Resources/DadV5, plus the v6 reward
// art (Design/dad-reward-assets-v6.md) in Resources/DadV6, prepared from Art/V6Rewards by Tools/prepare_dad_rewards_v6.py,
// and the v7 motion poses (Design/dad-motion-assets-v7.md) in Resources/DadV7, normalized by Tools/prepare_dad_motion_v7.py.
// Every character frame is a 768x768 canvas with the feet at (384, 720); dad faces right, everyone else faces left.
public sealed class DadArt
{
    public const float Canvas = 768, PivotX = 384, PivotY = 720;
    public static readonly string[] CharacterIds = { "dad", "gum", "chair", "tray", "boss1", "boss2", "boss3", "dodge" };

    public sealed class Character
    {
        public string id;
        public float standHeight;                     // standing height in canvas pixels (the spec: 560 regular, 640 boss)
        public readonly Dictionary<string, Texture2D[]> actions = new Dictionary<string, Texture2D[]>();
        public bool Has(string action) { return actions.ContainsKey(action); }
        // Missing actions fall back to walking so a partial delivery still renders.
        public Texture2D Frame(string action, int index)
        {
            Texture2D[] frames;
            if (!actions.TryGetValue(action, out frames) && !actions.TryGetValue("walk", out frames)) return null;
            return frames[((index % frames.Length) + frames.Length) % frames.Length];
        }
        public int Count(string action) { Texture2D[] f; return actions.TryGetValue(action, out f) ? f.Length : 0; }
    }

    public readonly Dictionary<string, Character> characters = new Dictionary<string, Character>();
    public readonly Dictionary<string, Texture2D> props = new Dictionary<string, Texture2D>();
    public readonly Dictionary<string, Texture2D> ui = new Dictionary<string, Texture2D>();
    public readonly Dictionary<string, Texture2D> gear = new Dictionary<string, Texture2D>();
    public readonly Dictionary<string, Texture2D> backgrounds = new Dictionary<string, Texture2D>();
    public readonly Dictionary<string, AudioClip> audio = new Dictionary<string, AudioClip>();
    public readonly Dictionary<string, Texture2D> rewards = new Dictionary<string, Texture2D>();
    public bool HasCharacters { get { return characters.ContainsKey("dad") && characters.ContainsKey("gum"); } }

    public static DadArt Load()
    {
        var art = new DadArt();
        Texture2D[] motion = Resources.LoadAll<Texture2D>("DadV7");   // extra named actions on the same canvas (movestart, pass, wallhit...)
        foreach (string id in CharacterIds)
        {
            var c = new Character { id = id, standHeight = id.StartsWith("boss") ? 640 : 560 };
            var sorted = new Dictionary<string, SortedDictionary<int, Texture2D>>();
            var frames = new List<Texture2D>(Resources.LoadAll<Texture2D>("DadV5/chars/" + id));
            foreach (Texture2D t in motion) if (t.name.StartsWith(id + "_")) frames.Add(t);
            foreach (Texture2D t in frames)
            {
                // <id>_<action>_<n>; actions may contain underscores (tray_notray_walk_0). Anything else is a prop.
                if (!t.name.StartsWith(id + "_")) { art.props[t.name] = t; continue; }
                string rest = t.name.Substring(id.Length + 1);
                int cut = rest.LastIndexOf('_'), index;
                if (cut <= 0 || !int.TryParse(rest.Substring(cut + 1), out index)) continue;
                string action = rest.Substring(0, cut);
                if (!sorted.ContainsKey(action)) sorted[action] = new SortedDictionary<int, Texture2D>();
                sorted[action][index] = t;
            }
            foreach (var kv in sorted) c.actions[kv.Key] = new List<Texture2D>(kv.Value.Values).ToArray();
            if (c.actions.Count > 0) art.characters[id] = c;
        }
        foreach (Texture2D t in Resources.LoadAll<Texture2D>("DadV5/ui")) art.ui[t.name] = t;
        foreach (Texture2D t in Resources.LoadAll<Texture2D>("DadV5/gear")) art.gear[t.name] = t;
        foreach (Texture2D t in Resources.LoadAll<Texture2D>("DadV5/bg")) art.backgrounds[t.name] = t;
        foreach (AudioClip a in Resources.LoadAll<AudioClip>("DadV5/audio")) art.audio[a.name] = a;
        foreach (Texture2D t in Resources.LoadAll<Texture2D>("DadV6")) art.rewards[t.name] = t;
        return art;
    }

    public static readonly string[] FloorBackgrounds = { "bg_floor1_corridor", "bg_floor2_cafeteria", "bg_floor3_gym" };
    public Texture2D Background(int floor)
    {
        Texture2D t;
        return backgrounds.TryGetValue(FloorBackgrounds[Mathf.Clamp(floor - 1, 0, FloorBackgrounds.Length - 1)], out t) ? t : null;
    }
    public Texture2D Icon(string name) { Texture2D t; return ui.TryGetValue(name, out t) ? t : null; }
    public Texture2D Gear(string name) { Texture2D t; return gear.TryGetValue(name, out t) ? t : null; }
    public Texture2D Reward(string name) { Texture2D t; return rewards.TryGetValue(name, out t) ? t : null; }
    public AudioClip Clip(string name) { AudioClip a; return audio.TryGetValue(name, out a) ? a : null; }
}
