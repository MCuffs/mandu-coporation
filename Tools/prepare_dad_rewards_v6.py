"""Turn the v6 reward delivery (Assets/DadSchoolBoss/Art/V6Rewards, originals kept untouched) into runtime copies.

Writes Assets/DadSchoolBoss/Resources/DadV6/:
  lunchbox_closed/opening/open.png   one shared crop, floor lines aligned to the closed pose (README: "바닥 기준을 맞추고")
  gear_card_common/rare/best.png     each cropped to its own frame (cards differ in canvas size; layout uses the frame)
  locker_damage_1/2/3.png            one shared crop so the three stages swap in place around the same centre
  locker_damage_1/2/3_steel.png      desaturated copies for the 2F cafeteria walls (steel counters, rice cookers, carts)
Faint halo pixels below ALPHA_FLOOR are cleared (README: "강하면 알파 임계값으로 줄인다").

Usage: python3 Tools/prepare_dad_rewards_v6.py
"""
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Assets/DadSchoolBoss/Art/V6Rewards"
OUT = ROOT / "Assets/DadSchoolBoss/Resources/DadV6"
ALPHA_FLOOR = 24          # halo pixels fainter than this are dropped
BOUNDS_ALPHA = 40         # what counts as drawn when cropping
PAD = 8
# Bottom edge of the box body, measured on the right half of each pose (the open pose's ribbon hangs lower on the left).
LUNCHBOX_FLOOR = {"lunchbox_closed": 1051, "lunchbox_opening": 1058, "lunchbox_open": 1068}


def load(name):
    im = Image.open(SRC / (name + ".png")).convert("RGBA")
    a = im.getchannel("A").point(lambda v: 0 if v < ALPHA_FLOOR else v)
    im.putalpha(a)
    return im


def bounds(im):
    return im.getchannel("A").point(lambda v: 255 if v > BOUNDS_ALPHA else 0).getbbox()


def union(boxes):
    return (min(b[0] for b in boxes), min(b[1] for b in boxes), max(b[2] for b in boxes), max(b[3] for b in boxes))


def padded(box, size):
    return (max(0, box[0] - PAD), max(0, box[1] - PAD), min(size[0], box[2] + PAD), min(size[1], box[3] + PAD))


def shared_crop(names, shifts=None):
    images = {}
    for n in names:
        im = load(n)
        dy = (shifts or {}).get(n, 0)
        if dy:
            moved = Image.new("RGBA", im.size, (0, 0, 0, 0))
            moved.alpha_composite(im, (0, -dy))
            im = moved
        images[n] = im
    box = padded(union([bounds(im) for im in images.values()]), next(iter(images.values())).size)
    return {n: im.crop(box) for n, im in images.items()}, box


def steel(im):
    gray = im.convert("L").point(lambda v: min(255, int(v * 1.12 + 10)))
    r, g, b = (gray.point(lambda v, k=k: min(255, int(v * k))) for k in (.96, .99, 1.04))
    out = Image.merge("RGBA", (r, g, b, im.getchannel("A")))
    return out


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    written = []
    base = LUNCHBOX_FLOOR["lunchbox_closed"]
    boxes, box = shared_crop(list(LUNCHBOX_FLOOR), {n: f - base for n, f in LUNCHBOX_FLOOR.items()})
    for n, im in boxes.items():
        im.save(OUT / (n + ".png")); written.append((n, im.size))
    print("lunchbox crop", box, "floor line at", base - box[1], "of", box[3] - box[1])
    for n in ("gear_card_common", "gear_card_rare", "gear_card_best"):
        im = load(n)
        im = im.crop(padded(bounds(im), im.size))
        im.save(OUT / (n + ".png")); written.append((n, im.size))
    decals, box = shared_crop(["locker_damage_1", "locker_damage_2", "locker_damage_3"])
    for n, im in decals.items():
        im.save(OUT / (n + ".png")); written.append((n, im.size))
        steel(im).save(OUT / (n + "_steel.png")); written.append((n + "_steel", im.size))
    print("decal crop", box)
    for n, size in written:
        print("  %-26s %dx%d" % (n, size[0], size[1]))


if __name__ == "__main__":
    main()
