"""Normalize the v7 motion delivery (Assets/DadSchoolBoss/Art/V7Motion, originals kept untouched) onto the v5 canvas.

v5 character frames are 768x768 with the feet at (384, 720) and a 560 px standing height (Design/dad-motion-assets-v7.md).
The v7 originals are 1254x1254 at their own scale, so each pose gets a scale chosen by head size against the v5 frames
(the README forbids stretching every bounding box to the same height):
  dad move start / stop  0.633  (same standing height as dad_idle_0)
  gum walk passing A / B 0.605  (heads match gum_walk_*, body a touch taller at the passing position, as it should be)
  gum wall hit           0.54   (drawn larger in the original; head matched to gum_hit_0 / gum_fly_0)
  gum crumple            0.60   (sits low: scaled by head, never stretched to standing height)
Standing poses are centred on x=384. The wall poses put their back (right edge of the drawing) at x=534 so the game can
press it against a wall: BackOffset = 150 canvas px (DadSchoolJuice.WallBackOffset).

Writes Assets/DadSchoolBoss/Resources/DadV7/<id>_<action>_<n>.png, picked up by DadArt as extra named actions.
Usage: python3 Tools/prepare_dad_motion_v7.py
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/DadSchoolBoss/Art/V7Motion"
OUT = ROOT / "Assets/DadSchoolBoss/Resources/DadV7"
CANVAS, PIVOT_X, FEET_Y, BACK_X = 768, 384, 720, 534
ALPHA_FLOOR, BOUNDS_ALPHA = 24, 40

POSES = [
    # source, output name, scale, horizontal anchor ("centre" or "back")
    ("dad_move_start", "dad_movestart_0", .633, "centre"),
    ("dad_move_stop", "dad_movestop_0", .633, "centre"),
    ("gum_walk_pass_a", "gum_pass_0", .605, "centre"),
    ("gum_walk_pass_b", "gum_pass_1", .605, "centre"),
    ("gum_wall_hit", "gum_wallhit_0", .54, "back"),
    ("gum_crumple", "gum_crumple_0", .60, "back"),
]


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for src, name, scale, anchor in POSES:
        im = Image.open(SRC / (src + ".png")).convert("RGBA")
        im.putalpha(im.getchannel("A").point(lambda v: 0 if v < ALPHA_FLOOR else v))
        box = im.getchannel("A").point(lambda v: 255 if v > BOUNDS_ALPHA else 0).getbbox()
        art = im.crop(box)
        art = art.resize((round(art.width * scale), round(art.height * scale)), Image.LANCZOS)
        x = PIVOT_X - art.width // 2 if anchor == "centre" else BACK_X - art.width
        y = FEET_Y - art.height
        canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
        canvas.alpha_composite(art, (x, y))
        assert x >= 0 and y >= 0 and x + art.width <= CANVAS, (name, x, y, art.size)
        canvas.save(OUT / (name + ".png"))
        print("  %-18s from %-16s scale %.3f  drawn %dx%d at (%d,%d)" % (name, src, scale, art.width, art.height, x, y))


if __name__ == "__main__":
    main()
