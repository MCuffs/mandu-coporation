#!/usr/bin/env python3
"""Paste the throw game's pictures onto a 1280x720 canvas using the game's own layout numbers.

The rectangles come from the LAYOUT lines that DadThrowChecks prints into the managed-check report
(Tools/run_dad_headless_checks.py), and the people come from the stage file the game reads, so these
pictures and DadThrowPrototype use the same numbers.
This is a still mock-up for checking placement. It is NOT a Unity capture: IMGUI text metrics, texture
filtering, safe-area scaling, touch and animation timing are not reproduced. No Unity process is started.

It also measures which way each opponent picture faces and stops if the stage file says otherwise.
"""
import argparse
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / 'Assets/DadSchoolBoss/Resources'
FONTS = ROOT / 'Assets/Alley/Resources/Fonts'
INK, PAPER, GOLD, WARN = (33, 38, 41), (250, 237, 201), (255, 196, 33), (209, 64, 51)
PAPER_DOWN, WHITE = (222, 209, 173), (255, 255, 255)
W, H = 1280, 720
FINGER = (760, 300)  # where the mock-up pretends the drag started

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--report', type=Path, default=ROOT / 'Design/dad-throw-managed-checks.txt')
parser.add_argument('--out', type=Path, default=ROOT / 'Design/dad-throw-art-preview-v2')
args = parser.parse_args()

layout = {}
for line in args.report.read_text().splitlines():
    if line.startswith('LAYOUT '):
        name, x, y, w, h = line.split()[1:6]
        layout[name] = (float(x), float(y), float(w), float(h))
if 'background' not in layout:
    parser.error('No LAYOUT lines in the report. Run Tools/run_dad_headless_checks.py first.')
GROUND, GATE = layout['gate'][1], layout['gate'][0]
MUZZLE = layout['muzzle'][:2]

table = json.loads((RES / 'DadThrowStages/chapter1.json').read_text())
stages = {stage['id']: stage for stage in table['stages']}


def picture(key):
    return Image.open(RES / (key + '.png')).convert('RGBA')


def resource(folder, asset):
    manifest = json.loads((RES / folder / 'manifest.json').read_text())
    return picture(next(a['resource'] for a in manifest['assets'] if a['id'] == asset))


art = {name: resource('DadThrowV1', name) for name in ('dad_ready', 'dad_windup', 'dad_release', 'projectile_golf', 'projectile_shoe')}
art.update({name: resource('DadThrowScreensV2', name) for name in ('street_wide', 'title_wide')})
art.update({name: resource('DadThrowScreensV1', name) for name in ('dad_clear', 'dad_retry')})
kinds = {}
for name, kind in table['enemies'].items():
    frames = [picture(f"{kind['resource']}_{i}") for i in range(kind['frames'])]
    # Which way is the picture facing? A walker's head leads the body, so compare where the head and the
    # whole figure sit sideways. A rule of thumb for these side-view walkers, not a general detector.
    lead = []
    for frame in frames:
        alpha = frame.getchannel('A').point(lambda v: 255 if v > 128 else 0)
        left, top, right, bottom = alpha.getbbox()

        def centre(box):
            columns = alpha.crop(box).resize((box[2] - box[0], 1), Image.BOX).getdata()
            total = sum(columns)
            return box[0] + sum(i * v for i, v in enumerate(columns)) / total

        lead.append(centre((left, top, right, bottom)) - centre((left, top, right, top + (bottom - top) * 35 // 100)))
    ahead = sum(lead) / len(lead)
    measured = ahead > 0
    if abs(ahead) < 8 or measured != kind['facesLeft']:
        parser.error(f"{name}: stage file says facesLeft={kind['facesLeft']}, picture measures head {ahead:+.0f}px ahead toward the left")
    print(f"{name}: head is {ahead:.0f}px ahead of the body toward the left -> faces left = {measured}, as the stage file says")
    kinds[name] = (kind, frames)


def font(kind, size):
    path = FONTS / {'display': 'BlackHanSans-Regular.ttf', 'bold': 'GothicA1-Bold.ttf', 'body': 'GothicA1-Regular.ttf'}[kind]
    return ImageFont.truetype(str(path), size)


def paste(canvas, image, rect, degrees=0, tint=None):
    x, y, w, h = rect
    scaled = image.resize((max(1, round(w)), max(1, round(h))), Image.LANCZOS)
    if degrees:
        # Turn around the picture's own centre and keep the corners (the game turns it with a matrix, uncropped).
        turned = scaled.rotate(-degrees, resample=Image.BICUBIC, expand=True)
        x, y = x + (scaled.width - turned.width) / 2, y + (scaled.height - turned.height) / 2
        scaled = turned
    px, py = round(x), round(y)
    box = (max(0, -px), max(0, -py), min(scaled.width, canvas.width - px), min(scaled.height, canvas.height - py))
    if box[2] > box[0] and box[3] > box[1]:
        canvas.alpha_composite(scaled.crop(box), (max(0, px), max(0, py)))


def text(draw, rect, value, size, color=INK, kind='bold', edge=None, width=0, align='center'):
    x, y, w, h = rect
    f = font(kind, size)
    box = draw.textbbox((0, 0), value, font=f)
    tw, th = box[2] - box[0], box[3] - box[1]
    px = {'center': x + (w - tw) / 2, 'left': x, 'right': x + w - tw}[align] - box[0]
    py = y + (h - th) / 2 - box[1]
    if edge:
        for i in range(8):
            a = i * math.pi / 4
            draw.text((px + math.cos(a) * width, py + math.sin(a) * width), value, font=f, fill=edge)
    draw.text((px, py), value, font=f, fill=color)


def panel(draw, rect, fill, radius=10):
    x, y, w, h = rect
    draw.rounded_rectangle([x, y, x + w, y + h], radius=radius, fill=INK)
    draw.rounded_rectangle([x + 1.5, y + 1.5, x + w - 1.5, y + h - 1.5], radius=radius - 1.5, fill=fill)


def veil(canvas, color):
    canvas.alpha_composite(Image.new('RGBA', (W, H), color))


def predict(pull, radius):
    """Same steps as DadThrowModel.Predict: from the hand to the first landing."""
    length = math.hypot(*pull)
    k = min(length, 150) / length if length else 0
    vx, vy = max(180, pull[0] * k * 6), max(-720, min(260, pull[1] * k * 6))
    px, py = MUZZLE
    path = []
    for i in range(240):
        if i % 3 == 0:
            path.append((px, py))
        vy += 760 / 120
        px += vx / 120
        py += vy / 120
        if py >= GROUND - radius:
            path.append((px, GROUND - radius))
            break
        if px > W:
            break
    return path


def people(canvas, stage, clock):
    """Everyone the stage table has put on the street by `clock`, walked forward, nobody hit."""
    standing, number = [], 0
    for group in sorted(stages[stage]['groups'], key=lambda g: g['at']):
        for i in range(group['count']):
            number += 1
            if group['at'] <= clock:
                standing.append((group['firstX'] + group['spacing'] * i - group['speed'] * (clock - group['at']), number, group['enemy']))
    for x, number, name in sorted(standing):  # left first, as the game draws them
        kind, frames = kinds[name]
        if x <= GATE:
            continue
        height = kind['heights'][number % len(kind['heights'])]
        k = height / kind['standPixels']
        frame = frames[number % len(frames)]
        if not kind['facesLeft']:
            frame = frame.transpose(Image.FLIP_LEFT_RIGHT)
        paste(canvas, frame, (x - kind['pivotX'] * k, GROUND - kind['pivotY'] * k, kind['canvas'] * k, kind['canvas'] * k))
    total = sum(g['count'] for g in stages[stage]['groups'])
    return total - sum(1 for x, _, _ in standing if x <= GATE)


def hud(canvas, draw, stage, health, remaining, selected, reload=0, hint=None, nudge=False, pressed=None):
    x, y, w, h = layout['top_bar']
    draw.rectangle([x, y, x + w, y + h], fill=PAPER)
    draw.rectangle([x, y + h - 3, x + w, y + h], fill=INK)
    text(draw, layout['stage_label'], stage, 34, kind='display')
    text(draw, layout['health_label'], '대문', 26)
    for i in range(5):
        panel(draw, layout[f'health_cell_{i}'], (GOLD if health > 1 else WARN) if i < health else PAPER_DOWN, 8)
    text(draw, layout['remaining_label'], f'남은 도전자 {remaining}', 24, align='right')
    panel(draw, layout['pause_button'], PAPER)
    for i in range(2):
        bx, by, bw, bh = layout[f'pause_bar_{i}']
        draw.rounded_rectangle([bx, by, bx + bw, by + bh], radius=3, fill=INK)
    for key, label in (('golf', '골프공'), ('shoe', '신발')):
        button = layout[key + '_button']
        if nudge and key == 'shoe':
            bx, by, bw, bh = button
            draw.rounded_rectangle([bx - 5, by - 5, bx + bw + 5, by + bh + 5], radius=12, fill=GOLD)
        panel(draw, button, GOLD if selected == key else PAPER)
        paste(canvas, art['projectile_' + key], layout[key + '_icon'])
        text(draw, layout[key + '_label'], label, 26)
        if reload:
            cover = Image.new('RGBA', (W, H), (0, 0, 0, 0))
            bx, by, bw, bh = button
            ImageDraw.Draw(cover).rounded_rectangle([bx, by, bx + bw * reload, by + bh], radius=10, fill=INK + (115,))
            canvas.alpha_composite(cover)
    if hint:
        text(draw, layout['hint'], hint, 30, PAPER, 'display', INK, 3)


def battle(stage, clock, pose='ready', selected='golf', pull=None, flying=(), health=5, reload=0, hint=None, nudge=False, card=None, mark=None):
    canvas = Image.new('RGBA', (W, H), INK + (255,))
    paste(canvas, art['street_wide'], layout['background'])
    draw = ImageDraw.Draw(canvas)
    result = card in ('clear', 'last', 'defeat')
    # A clear only happens on an empty street with nobody left to come.
    remaining = 0 if card in ('clear', 'last') else people(canvas, stage, clock)
    if not result:
        paste(canvas, art['dad_' + pose], layout['dad_' + pose])
        if pose == 'windup':
            paste(canvas, art['projectile_' + selected], layout[selected + '_in_hand'], -35 if selected == 'shoe' else 0)
        for kind, x, y, degrees in flying:
            rx, ry, rw, rh = layout[kind + '_at_muzzle']
            paste(canvas, art['projectile_' + kind], (rx + x - MUZZLE[0], ry + y - MUZZLE[1], rw, rh), degrees)
        draw = ImageDraw.Draw(canvas)
        if mark:
            text(draw, (mark[0] - 50, mark[1] - 74, 100, 40), f'×{mark[2]}', 30, GOLD, 'display', INK, 2)
        if pull:
            path = predict(pull, 11 if selected == 'golf' else 22)
            for px, py in path[3:-1:3]:
                draw.ellipse([px - 5, py - 5, px + 5, py + 5], fill=INK)
                draw.ellipse([px - 3, py - 3, px + 3, py + 3], fill=WHITE)
            px, py = path[-1]
            draw.ellipse([px - 8, py - 8, px + 8, py + 8], fill=INK)
            draw.ellipse([px - 5, py - 5, px + 5, py + 5], fill=WHITE)
            # The pull line sits under the finger. The mock-up assumes the drag began at FINGER.
            length = math.hypot(*pull)
            k = min(length, 150) / length
            ex, ey = FINGER[0] - pull[0] * k, FINGER[1] - pull[1] * k
            draw.line([FINGER[0], FINGER[1], ex, ey], fill=INK, width=5)
            draw.ellipse([ex - 9, ey - 9, ex + 9, ey + 9], fill=INK)
    draw = ImageDraw.Draw(canvas)
    hud(canvas, draw, stage, health, remaining, selected, reload, hint, nudge)
    if card == 'pause':
        veil(canvas, (0, 0, 0, 153))
        draw = ImageDraw.Draw(canvas)
        panel(draw, layout['pause_card'], PAPER)
        text(draw, layout['pause_heading'], '잠깐만!', 46, kind='display')
        panel(draw, layout['pause_resume'], GOLD)
        text(draw, layout['pause_resume'], '계속하기', 28)
        panel(draw, layout['pause_title'], PAPER)
        text(draw, layout['pause_title'], '타이틀', 28)
    elif result:
        veil(canvas, (23, 37, 27, 212))
        paste(canvas, art['dad_retry' if card == 'defeat' else 'dad_clear'], layout['hero_retry' if card == 'defeat' else 'hero_clear'])
        draw = ImageDraw.Draw(canvas)
        panel(draw, layout['result_card'], PAPER)
        if card == 'defeat':
            text(draw, layout['defeat_heading'], '다시 해볼까?', 46, kind='display')
            text(draw, layout['defeat_line'], '대문을 다섯 번 만졌어요', 24, kind='body')
            panel(draw, layout['defeat_retry'], GOLD)
            text(draw, layout['defeat_retry'], '다시 도전', 30)
            panel(draw, layout['defeat_title'], PAPER)
            text(draw, layout['defeat_title'], '타이틀', 24)
        else:
            text(draw, layout['clear_heading'], '이번 조 끝!' if card == 'clear' else '여기까지 막았다!', 46, kind='display')
            panel(draw, layout['clear_left'], PAPER)
            text(draw, layout['clear_left'], '타이틀', 28)
            panel(draw, layout['clear_right'], GOLD)
            text(draw, layout['clear_right'], '다음' if card == 'clear' else '다시 하기', 28)
    return canvas


def title():
    canvas = Image.new('RGBA', (W, H), INK + (255,))
    paste(canvas, art['title_wide'], layout['title_art'])
    draw = ImageDraw.Draw(canvas)
    text(draw, layout['title_text'], '아빠 전쟁', 94, GOLD, 'display', INK, 4)
    panel(draw, layout['start_button'], GOLD)
    text(draw, layout['start_button'], '게임 시작', 30)
    return canvas


def logo():
    canvas = Image.new('RGBA', (W, H), (0, 0, 0, 255))
    text(ImageDraw.Draw(canvas), (0, 300, W, 120), 'Bluemushroom studio', 54, WHITE, 'display')
    return canvas


def guides(canvas):
    """Same frame with the rule lines drawn in, to compare the art with the fight's numbers."""
    marked = canvas.copy()
    draw = ImageDraw.Draw(marked)
    draw.line([0, GROUND, W, GROUND], fill=(255, 0, 0), width=2)
    draw.line([GATE, 86, GATE, H], fill=(0, 90, 255), width=2)
    painted = layout['painted_gate'][0]
    draw.line([painted, 86, painted, H], fill=(0, 200, 200), width=2)
    for name in ('road_edge', 'sand_top'):
        draw.line([0, layout[name][1], W, layout[name][1]], fill=(255, 140, 0), width=1)
    x, y, w, h = layout['aim_area']
    draw.rectangle([x, y, x + w, y + h], outline=(255, 255, 255), width=2)
    for name, color in (('muzzle', (255, 0, 255)), ('windup_hand', (0, 200, 0))):
        x, y = layout[name][:2]
        draw.ellipse([x - 7, y - 7, x + 7, y + 7], outline=color, width=3)
    text(draw, (140, 108, 1000, 28), '빨강: 발 선 · 파랑: 대문 터치로 세는 선 · 청록: 그림 속 대문 기둥 · 주황: 모래 위/도로 경계 · 분홍: 발사점 · 초록: 당긴 손 · 흰 테두리: 조준 시작 영역', 15, WHITE, 'body', INK, 2)
    return marked


args.out.mkdir(parents=True, exist_ok=True)
frames = {
    '01-logo': logo(),
    '02-title': title(),
    '03-stage-1-1-start': battle('1-1', 0.5, hint='당겨서 놓으세요'),
    '04-stage-1-3-aiming-golf': battle('1-3', 5.0, 'windup', 'golf', pull=(150, 28)),
    '05-stage-1-3-release-golf': battle('1-3', 5.2, 'release', 'golf', flying=[('golf', 470, 500, 40)], reload=0.8),
    '06-stage-1-3-aiming-shoe': battle('1-3', 5.0, 'windup', 'shoe', pull=(120, -20)),
    '07-stage-1-3-shoe-nudge': battle('1-3', 1.0, nudge=True),
    '08-pause': battle('1-3', 6.0, card='pause'),
    '09-clear': battle('1-1', 3.0, card='clear'),
    '10-clear-last-stage': battle('1-3', 16.5, card='last'),
    '11-defeat': battle('1-3', 21.0, health=0, card='defeat'),
    '12-last-health': battle('1-3', 17.5, health=1),
}
frames['13-guides'] = guides(frames['04-stage-1-3-aiming-golf'])
for name, frame in frames.items():
    frame.convert('RGB').save(args.out / (name + '.png'))
sheet = Image.new('RGB', (W, H + 14), (20, 20, 22))
for i, name in enumerate(('02-title', '04-stage-1-3-aiming-golf', '09-clear', '11-defeat')):
    small = frames[name].convert('RGB').resize((W // 2 - 5, H // 2 - 3))
    sheet.paste(small, ((i % 2) * (W // 2 + 5), (i // 2) * (H // 2 + 10)))
sheet.save(args.out / 'overview.png')
print(f'Wrote {len(frames)} mock-up frames and overview.png to {args.out.relative_to(ROOT)}. Not Unity captures.')
