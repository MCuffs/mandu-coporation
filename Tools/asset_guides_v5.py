"""Draws the asset spec guides: Design/asset-guide-character.png and Design/asset-guide-background.png."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
ROOT = Path(__file__).resolve().parents[1]
F = ROOT / "Assets/Alley/Resources/Fonts/GothicA1-Bold.ttf"
def font(s): return ImageFont.truetype(str(F), s)
INK, RED, BLUE, GREEN, GRAY = (30, 30, 30), (210, 50, 40), (40, 110, 210), (40, 150, 80), (200, 200, 200)

def character():
    im = Image.new("RGB", (768 * 2 + 60, 900), (250, 250, 250)); d = ImageDraw.Draw(im)
    for k, (title, h, facing) in enumerate([("일반 캐릭터 (아빠 · 적)", 560, "아빠 → 오른쪽 / 적 ← 왼쪽"), ("보스", 640, "보스 ← 왼쪽")]):
        ox, oy = 20 + k * (768 + 20), 110
        d.text((ox, 20), title, font=font(30), fill=INK)
        d.text((ox, 62), "캔버스 768×768 PNG · 투명 배경 · 발 중심 (384, 720) 고정", font=font(20), fill=INK)
        for gx in range(0, 769, 64):
            d.line([ox + gx, oy, ox + gx, oy + 768], fill=(235, 235, 235))
            d.line([ox, oy + gx, ox + 768, oy + gx], fill=(235, 235, 235))
        d.rectangle([ox, oy, ox + 768, oy + 768], outline=INK, width=3)
        d.line([ox, oy + 720, ox + 768, oy + 720], fill=RED, width=3); d.text((ox + 8, oy + 726), "발바닥 선 y=720", font=font(18), fill=RED)
        d.line([ox + 384, oy, ox + 384, oy + 768], fill=BLUE, width=2); d.text((ox + 392, oy + 8), "x=384 (몸 중심)", font=font(18), fill=BLUE)
        top = 720 - h
        d.line([ox, oy + top, ox + 768, oy + top], fill=GREEN, width=3); d.text((ox + 8, oy + top - 26), "서 있을 때 머리 끝 (키 %d±20px)" % h, font=font(18), fill=GREEN)
        # stand-in silhouette
        cx = ox + 384; w = h * 0.36
        d.ellipse([cx - w * .35, oy + top, cx + w * .35, oy + top + h * .22], outline=INK, width=4)
        d.rounded_rectangle([cx - w * .5, oy + top + h * .22, cx + w * .5, oy + top + h * .62], 20, outline=INK, width=4)
        d.line([cx - w * .25, oy + top + h * .62, cx - w * .3, oy + 720], fill=INK, width=8); d.line([cx + w * .25, oy + top + h * .62, cx + w * .3, oy + 720], fill=INK, width=8)
        d.text((ox + 760, oy + 740), "바라보는 방향: " + facing, font=font(18), fill=INK, anchor="ra")
        d.rectangle([ox + 520, oy + top + 80, ox + 760, oy + top + 220], outline=GRAY, width=2)
        d.text((ox + 528, oy + top + 88), "팔·소품은 캔버스 안이면\n어디로 뻗어도 됨\n(발 위치만 고정)", font=font(17), fill=(120, 120, 120))
    im.save(ROOT / "Design/asset-guide-character.png")

def background():
    s = 0.4; W, H = 1080, 1920
    im = Image.new("RGB", (int(W * s) + 560, int(H * s) + 40), (250, 250, 250)); d = ImageDraw.Draw(im)
    ox, oy = 20, 20
    def r(x0, y0, x1, y1, **kw): d.rectangle([ox + x0 * s, oy + y0 * s, ox + x1 * s, oy + y1 * s], **kw)
    r(0, 0, W, H, fill=(225, 215, 190), outline=INK, width=3)
    r(0, 0, W, 290, fill=(200, 200, 200)); r(0, 1600, W, H, fill=(200, 200, 200))
    r(128, 480, 952, 1520, fill=(245, 240, 225), outline=RED, width=3)
    r(0, 290, 128, 1600, fill=(180, 200, 205)); r(952, 290, W, 1600, fill=(180, 200, 205))
    r(128, 290, 952, 480, fill=(205, 215, 190))
    tx = ox + W * s + 20
    lines = [("배경 1080×1920 PNG (투명 아님)", INK), ("", INK), ("빨간 칸 (128,480)~(952,1520):", RED), ("  싸우는 바닥. 사람·물건·글자 없이 비워 둠", RED),
             ("  평평하게, 원근 약하게", RED), ("", INK), ("좌우 파란 띠 x<128, x>952:", BLUE), ("  벽·사물함·문. 적이 부딪히는 벽", BLUE), ("", INK),
             ("위쪽 (128,290)~(952,480): 뒷벽, 문 1개", GREEN), ("", INK), ("회색 띠 y<290, y>1600:", (110, 110, 110)), ("  화면 UI가 덮는 곳. 그림은 이어지게", (110, 110, 110)),
             ("  그리되 중요한 것은 두지 않음", (110, 110, 110))]
    for i, (t, c) in enumerate(lines): d.text((tx, oy + 10 + i * 30), t, font=font(19), fill=c)
    im.save(ROOT / "Design/asset-guide-background.png")

character(); background(); print("ok")
