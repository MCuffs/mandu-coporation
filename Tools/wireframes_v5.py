"""Draws the v5 screen wireframes (540x960 layout units, same grid as DadSchoolPrototype).

Run: python3 -I Tools/wireframes_v5.py  -> Design/dad-v5-wireframes.png
Wireframes show layout and information only; final art and copy come later.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FONTS = ROOT / "Assets/Alley/Resources/Fonts"
W, H = 540, 960
PAPER, INK, YELLOW, GREEN, RED, GRAY, WHITE, DARK = (240, 227, 184), (31, 33, 31), (255, 199, 38), (82, 122, 92), (199, 56, 42), (168, 168, 160), (255, 255, 255), (70, 72, 70)
QUALITY = [(150, 150, 150), (90, 170, 90), (70, 130, 210), (150, 90, 200), (235, 140, 40)]


def font(size, display=False):
    return ImageFont.truetype(str(FONTS / ("BlackHanSans-Regular.ttf" if display else "GothicA1-Bold.ttf")), size)


class Screen:
    def __init__(self, title):
        self.im = Image.new("RGB", (W, H), PAPER)
        self.d = ImageDraw.Draw(self.im)
        self.title = title

    def box(self, x, y, w, h, fill=WHITE, outline=INK, width=3, r=10, dashed=False):
        if dashed:
            self.d.rounded_rectangle([x, y, x + w, y + h], r, fill=fill)
            for i in range(int(x), int(x + w), 14):
                self.d.line([i, y, min(i + 7, x + w), y], fill=outline, width=2); self.d.line([i, y + h, min(i + 7, x + w), y + h], fill=outline, width=2)
            for j in range(int(y), int(y + h), 14):
                self.d.line([x, j, x, min(j + 7, y + h)], fill=outline, width=2); self.d.line([x + w, j, x + w, min(j + 7, y + h)], fill=outline, width=2)
        else:
            self.d.rounded_rectangle([x, y, x + w, y + h], r, fill=fill, outline=outline, width=width)

    def text(self, x, y, s, size=20, color=INK, display=False, anchor="la"):
        self.d.text((x, y), s, font=font(size, display), fill=color, anchor=anchor)

    def button(self, x, y, w, h, label, fill=YELLOW, size=24, ad=False, color=INK):
        self.box(x, y + 5, w, h, fill=INK)
        self.box(x, y, w, h, fill=fill)
        self.text(x + w / 2 + (14 if ad else 0), y + h / 2, label, size, color, True, "mm")
        if ad:
            self.ad_badge(x + 10, y + h / 2 - 13)

    def ad_badge(self, x, y):
        self.box(x, y, 52, 26, fill=DARK, outline=DARK, r=6)
        self.text(x + 26, y + 13, "▶광고", 13, YELLOW, False, "mm")

    def topbar(self, left="아빠 Lv.12 · 전투력 1,234"):
        self.box(0, -10, W, 78, fill=PAPER)
        self.text(16, 22, left, 19)
        self.box(330, 16, 96, 32, fill=WHITE, r=16); self.text(378, 32, "코인 12.3K", 15, anchor="mm")
        self.box(432, 16, 96, 32, fill=WHITE, r=16); self.text(480, 32, "도시락 23", 15, anchor="mm")

    def tabbar(self, active):
        tabs = ["상점", "도시락", "출발", "강화", "학교"]
        self.box(0, 872, W, 100, fill=WHITE, r=0)
        for i, t in enumerate(tabs):
            x = i * 108
            if t == "출발":
                self.box(x + 8, 852, 92, 92, fill=YELLOW, r=46)
                self.text(x + 54, 898, t, 22, INK, True, "mm")
            else:
                self.box(x + 30, 884, 48, 40, fill=YELLOW if t == active else PAPER, r=8)
                self.text(x + 54, 940, t, 16, INK, False, "mm")

    def dad(self, cx, feet, h=300):
        # simple stand-in silhouette: head, body, legs, slippers
        s = h / 300
        self.d.ellipse([cx - 120 * s, feet - 14 * s, cx + 120 * s, feet + 14 * s], fill=(200, 190, 160))
        self.d.rounded_rectangle([cx - 55 * s, feet - 200 * s, cx + 55 * s, feet - 70 * s], 30 * s, fill=(90, 120, 90), outline=INK, width=3)
        self.d.rectangle([cx - 45 * s, feet - 75 * s, cx + 45 * s, feet - 10 * s], fill=(50, 60, 90), outline=INK, width=3)
        self.d.ellipse([cx - 42 * s, feet - 290 * s, cx + 42 * s, feet - 200 * s], fill=(235, 190, 150), outline=INK, width=3)
        self.text(cx, feet - 245 * s, "아빠", int(20 * s), INK, True, "mm")

    def note(self, s):
        self.text(W / 2, H - 2, s, 14, DARK, False, "md")


def home():
    sc = Screen("홈")
    sc.topbar()
    for i, (label, sub) in enumerate([("무료 도시락", "3/5"), ("아빠 각성", "20분"), ("출석", "4일차")]):
        y = 96 + i * 96
        sc.box(12, y, 96, 84, fill=WHITE, dashed=(label == "출석"))
        if label != "출석":
            sc.ad_badge(34, y + 6)
        sc.text(60, y + 50, label, 15, anchor="mm"); sc.text(60, y + 70, sub, 13, DARK, anchor="mm")
    sc.dad(W / 2, 560, 330)
    sc.box(340, 420, 180, 120, fill=WHITE)
    sc.text(430, 444, "방치 보상", 18, INK, True, "mm"); sc.text(430, 472, "2시간 13분", 15, DARK, anchor="mm")
    sc.text(430, 496, "코인 8.2K · 도시락 4", 14, anchor="mm")
    sc.button(356, 512, 148, 20, "받기", GREEN, 14, color=WHITE)
    sc.box(30, 610, 480, 60, fill=WHITE)
    sc.text(46, 626, "2층 급식실 · 2-7", 20, INK, True); sc.text(46, 652, "권장 전투력 1,350", 15, RED)
    sc.text(494, 640, "부족! 강화 또는 각성", 15, RED, anchor="rm")
    sc.button(30, 690, 480, 84, "2-7 출발!", YELLOW, 34)
    sc.text(W / 2, 806, "적 미리보기: 껌 짱 · 의자 짱 · 쟁반 짱", 15, DARK, anchor="mm")
    sc.tabbar("출발")
    return sc


def school_map():
    sc = Screen("학교 지도")
    sc.topbar()
    sc.box(0, 70, W, 64, fill=WHITE, r=0)
    sc.text(20, 88, "2층 급식실", 26, INK, True); sc.text(520, 100, "★ 23 / 60", 18, anchor="rm")
    for i, (name, state) in enumerate([("1층 복도", "완료"), ("2층 급식실", "진행 중"), ("3층 체육관", "잠김")]):
        x = 16 + i * 172
        sc.box(x, 146, 160, 44, fill=YELLOW if state == "진행 중" else (WHITE if state == "완료" else GRAY))
        sc.text(x + 80, 168, name + (" 완료" if state == "완료" else " (잠김)" if state == "잠김" else ""), 15, anchor="mm")
    # boss at top, path snakes down to stage 1
    sc.box(150, 206, 240, 120, fill=WHITE)
    sc.text(270, 232, "층 보스 2-20", 18, RED, True, "mm"); sc.text(270, 262, "급식실 실세 (52세)", 17, anchor="mm")
    sc.text(270, 288, "30년째 급식 조리 중", 14, DARK, anchor="mm"); sc.text(270, 310, "권장 7,589", 14, RED, anchor="mm")
    pts = []
    for k in range(19):
        row, col = divmod(k, 5)
        x = 90 + (col if row % 2 == 0 else 4 - col) * 90
        y = 768 - row * 118
        pts.append((x, y))
    sc.d.line(pts, fill=INK, width=4)
    for k, (x, y) in enumerate(pts):
        n = k + 1
        mid = n == 10
        done = n < 7
        cur = n == 7
        r = 34 if mid else 26
        fill = YELLOW if cur else (GREEN if done else (GRAY if n > 7 else WHITE))
        sc.d.ellipse([x - r, y - r, x + r, y + r], fill=fill, outline=INK, width=3)
        sc.text(x, y, str(n), 18 if not mid else 16, WHITE if done else INK, True, "mm")
        if done:
            sc.text(x, y + r + 12, "★★★" if n < 5 else "★★", 12, (200, 150, 0), anchor="mm")
        if mid:
            sc.text(x, y - r - 12, "중간 보스", 13, RED, anchor="mm")
    sc.d.line([pts[-1], (270, 326)], fill=INK, width=4)
    sc.button(20, 818, 150, 24, "별 10개 상자", GREEN, 14, color=WHITE)
    sc.button(190, 818, 150, 24, "별 20개 상자", WHITE, 14)
    sc.button(360, 818, 150, 24, "별 30개 상자", WHITE, 14)
    sc.tabbar("학교")
    return sc


def shop():
    sc = Screen("상점")
    sc.topbar()
    sc.text(20, 92, "무료", 24, INK, True)
    sc.box(20, 128, 240, 150, fill=WHITE); sc.ad_badge(32, 140); sc.text(140, 196, "도시락 5개", 22, INK, True, "mm"); sc.text(140, 230, "오늘 3 / 5회", 15, DARK, anchor="mm")
    sc.box(280, 128, 240, 150, fill=WHITE); sc.ad_badge(292, 140); sc.text(400, 196, "코인 주머니", 22, INK, True, "mm"); sc.text(400, 230, "오늘 1 / 3회", 15, DARK, anchor="mm")
    sc.text(20, 300, "도시락 (코인)", 24, INK, True)
    sc.button(20, 338, 240, 70, "1개 · 2.0K", WHITE, 22)
    sc.button(280, 338, 240, 70, "10+1개 · 18K", YELLOW, 22)
    sc.text(20, 432, "오늘의 특가", 24, INK, True); sc.text(520, 446, "갱신까지 05:12:40", 14, DARK, anchor="rm")
    for i in range(3):
        x = 20 + i * 170
        sc.box(x, 470, 160, 180, fill=WHITE, outline=QUALITY[2 + (i % 3 == 1)], width=6)
        sc.text(x + 80, 520, ["희귀 목장갑", "영웅 효자손", "희귀 조끼"][i], 17, INK, True, "mm")
        sc.text(x + 80, 556, "공격 +142", 14, DARK, anchor="mm")
        sc.button(x + 16, 600, 128, 34, ["코인 9K", "코인 24K", "코인 9K"][i], YELLOW, 15)
    sc.button(150, 672, 240, 46, "목록 새로고침", WHITE, 17, ad=True)
    sc.text(20, 742, "결제 (출시 후 검토)", 20, DARK, True)
    sc.box(20, 776, 240, 70, fill=PAPER, dashed=True); sc.text(140, 811, "광고 제거 (전면만)", 16, DARK, anchor="mm")
    sc.box(280, 776, 240, 70, fill=PAPER, dashed=True); sc.text(400, 811, "월정액: 매일 도시락", 16, DARK, anchor="mm")
    sc.tabbar("상점")
    return sc


def upgrade():
    sc = Screen("강화")
    sc.topbar()
    sc.box(20, 86, 500, 70, fill=WHITE)
    sc.text(40, 104, "전투력 1,234", 26, INK, True); sc.text(500, 121, "다음 강화 후 1,296 (+5%)", 15, GREEN, anchor="rm")
    rows = [("주먹", "Lv.24", "공격력 312 → 327"), ("체력", "Lv.20", "체력 1,840 → 1,930"), ("연타", "Lv.12", "공격 간격 −0.4%"), ("날리기", "Lv.8", "넉백 +16% → +18%")]
    for i, (name, lv, eff) in enumerate(rows):
        y = 176 + i * 104
        sc.box(20, y, 500, 92, fill=WHITE)
        sc.box(32, y + 14, 64, 64, fill=PAPER); sc.text(64, y + 46, name[0], 26, INK, True, "mm")
        sc.text(110, y + 22, name + "  " + lv, 21, INK, True); sc.text(110, y + 54, eff, 15, DARK)
        sc.button(376, y + 20, 130, 52, "코인 1.2K", YELLOW if i < 3 else GRAY, 17)
    sc.text(370, 600, "길게 누르면 연속 강화", 13, DARK)
    sc.text(20, 618, "도시락 레벨 Lv.4", 24, INK, True)
    sc.box(20, 654, 500, 112, fill=WHITE)
    for i, (q, a, b) in enumerate([("일반", 52, 48), ("고급", 33, 34), ("희귀", 12, 14), ("영웅", 3, 3.6), ("전설", 0, 0.4)]):
        x = 34 + i * 96
        sc.box(x, 666, 86, 26, fill=QUALITY[i], outline=QUALITY[i], r=6); sc.text(x + 43, 679, q, 14, WHITE, anchor="mm")
        sc.text(x + 43, 712, "%s%%" % a, 16, anchor="mm"); sc.text(x + 43, 740, "→ %s%%" % b, 14, GREEN, anchor="mm")
    sc.button(20, 784, 280, 60, "레벨업 · 코인 30K", YELLOW, 18)
    sc.button(316, 784, 204, 60, "대기 2시간", WHITE, 16)
    sc.text(418, 862, "▶광고로 30분 단축", 13, DARK, anchor="mm")
    sc.tabbar("강화")
    return sc


def lunchbox():
    sc = Screen("도시락")
    sc.topbar()
    sc.dad(W / 2, 520, 300)
    slots = [("신발", 1), ("손", 2), ("상의", 0), ("머리", 3), ("소지품", 1), ("생활용품", 2)]
    for i, (name, q) in enumerate(slots):
        x = 30 if i % 2 == 0 else 410
        y = 150 + (i // 2) * 130
        sc.box(x, y, 100, 100, fill=WHITE, outline=QUALITY[q], width=7)
        sc.text(x + 50, y + 40, name, 16, INK, True, "mm"); sc.text(x + 50, y + 70, "Lv.%d" % (14 + i), 14, DARK, anchor="mm")
    sc.button(70, 600, 400, 86, "도시락 열기 (23)", YELLOW, 32)
    sc.box(70, 704, 190, 44, fill=PAPER, dashed=True); sc.text(165, 726, "자동 열기 (Lv.5)", 15, DARK, anchor="mm")
    sc.box(280, 704, 190, 44, fill=WHITE); sc.text(375, 726, "등급 확률 보기", 15, anchor="mm")
    sc.box(40, 764, 460, 96, fill=WHITE, outline=QUALITY[2], width=5)
    sc.text(56, 776, "NEW 희귀 효자손 Lv.18", 18, INK, True); sc.text(56, 806, "공격 +142  (지금보다 +38)", 15, GREEN)
    sc.button(300, 796, 90, 44, "분해", WHITE, 16); sc.button(398, 796, 90, 44, "장착", YELLOW, 16)
    sc.tabbar("도시락")
    return sc


def result():
    sc = Screen("결과 / 부활")
    sc.box(0, 0, W, H, fill=(120, 120, 115), r=0)
    # success card
    sc.box(24, 30, 492, 410, fill=PAPER)
    sc.text(W / 2, 74, "2-7 클리어!", 34, GREEN, True, "mm"); sc.text(W / 2, 112, "최대 비거리 312m  /  9연쇄", 17, anchor="mm")
    sc.box(60, 140, 200, 80, fill=WHITE); sc.text(160, 180, "코인 1,240", 20, INK, True, "mm")
    sc.box(280, 140, 200, 80, fill=WHITE); sc.text(380, 180, "도시락 1", 20, INK, True, "mm")
    sc.button(60, 244, 420, 76, "2배로 받기", YELLOW, 28, ad=True)
    sc.button(60, 340, 420, 60, "그냥 받고 다음 스테이지", WHITE, 20)
    sc.text(W / 2, 428, "전면 광고는 이 창을 닫을 때만 (5스테이지 이후 · 3분 간격)", 13, DARK, anchor="mm")
    # fail card
    sc.box(24, 470, 492, 280, fill=PAPER)
    sc.text(W / 2, 506, "아빠도 가끔은 진다", 28, RED, True, "mm"); sc.text(W / 2, 540, "학교 짱 체력 18% 남음 · 아깝다!", 16, anchor="mm")
    sc.button(44, 566, 140, 64, "강화 +12%", WHITE, 16); sc.button(200, 566, 140, 64, "도시락 4개", WHITE, 16); sc.button(356, 566, 140, 64, "", WHITE, 15)
    sc.ad_badge(400, 572); sc.text(426, 612, "각성 20분", 15, INK, True, "mm")
    sc.button(60, 656, 420, 64, "다시 도전", YELLOW, 24)
    # revive popup (in battle)
    sc.box(24, 774, 492, 170, fill=WHITE, outline=RED, width=5)
    sc.text(W / 2, 806, "엄마 전화 찬스!  (5초)", 24, RED, True, "mm"); sc.text(W / 2, 838, "체력 50%로 부활 · 스테이지당 1번", 15, anchor="mm")
    sc.button(60, 862, 250, 60, "부활", YELLOW, 24, ad=True); sc.button(326, 862, 154, 60, "괜찮아요", WHITE, 18)
    return sc


def title():
    sc = Screen("시작")
    sc.box(0, 0, W, H, fill=(250, 214, 90), r=0)
    sc.text(W / 2, 150, "아빠가", 64, INK, True, "mm"); sc.text(W / 2, 230, "학교 짱을", 64, INK, True, "mm"); sc.text(W / 2, 310, "너무 잘 잡음", 64, RED, True, "mm")
    sc.dad(W / 2, 700, 340)
    sc.text(W / 2, 780, "화면을 터치하세요", 26, INK, True, "mm")
    sc.text(W / 2, 830, "첫 실행: 3컷 만화 → 바로 1-1 전투 (홈은 1-3 클리어 후 열림)", 14, DARK, anchor="mm")
    sc.text(20, 940, "v0.1.0", 13, DARK, anchor="ls"); sc.text(520, 940, "개인정보처리방침", 13, DARK, anchor="rs")
    return sc


def main():
    screens = [title(), home(), school_map(), lunchbox(), upgrade(), shop(), result()]
    scale = 0.6
    tw, th = int(W * scale), int(H * scale)
    cols = 4
    rows = (len(screens) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * (tw + 20) + 20, rows * (th + 56) + 20), (45, 45, 45))
    d = ImageDraw.Draw(sheet)
    for i, sc in enumerate(screens):
        x = 20 + (i % cols) * (tw + 20)
        y = 20 + (i // cols) * (th + 56)
        d.text((x, y), "%d. %s" % (i + 1, sc.title), font=font(22, True), fill=(255, 220, 80))
        sheet.paste(sc.im.resize((tw, th), Image.LANCZOS), (x, y + 32))
    out = ROOT / "Design" / "dad-v5-wireframes.png"
    sheet.save(out)
    print(out, sheet.size)


if __name__ == "__main__":
    main()
