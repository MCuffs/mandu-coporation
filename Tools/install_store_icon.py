#!/usr/bin/env python3
"""Make the phone's app icon from the store's icon, so the two show the same picture.

The delivered store art (Design/store-art-v1/, Design/dad-store-art-request-v1.md) has the 512 icon the Play
listing uses and two adaptive layers. The delivered foreground layer stops short of the bottom edge, which shows
on launchers that cut icons to a rounded square, so until a redrawn layer arrives the phone's icon is made from
the store icon itself:

  icon_512.png          the store icon as it is: the plain icon older Android versions show.
  icon_adaptive_fg.png  the store icon fitted to the part of an adaptive icon a launcher shows (the middle
                        72 of 108 units), with its edge colours carried out to the canvas edge for the
                        launcher's motion effects. A launcher's circle or rounded square then cuts the store
                        icon the way the Play Store cuts it.
  icon_adaptive_bg.png  the delivered background layer; it only shows if a launcher moves the layers apart.

They go to Assets/DadSchoolBoss/Art/ThrowStoreV1/ with a .meta each: the import settings of the icon the project
already had (Art/V5/app_icon.png), uncompressed, and a GUID made from the file's path, so running this again
changes nothing. DadThrowAndroidBuild puts them into the Android build. It never opens Unity.
"""
import hashlib
from pathlib import Path
import re

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
DELIVERED = ROOT / 'Design/store-art-v1'
TARGET = ROOT / 'Assets/DadSchoolBoss/Art/ThrowStoreV1'
SETTINGS = ROOT / 'Assets/DadSchoolBoss/Art/V5/app_icon.png.meta'
CANVAS, SHOWN = 1024, 684  # 72/108 of the canvas is 683; one pixel over leaves no seam at the window's edge
FOLDER_META = '''fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
'''


def guid_for(path: Path) -> str:
    return hashlib.md5(('throw-store-icon:' + path.relative_to(ROOT).as_posix()).encode('utf-8')).hexdigest()


def meta(made: Path) -> None:
    settings, changed = re.subn(r'^guid: [0-9a-f]{32}$', 'guid: ' + guid_for(made), SETTINGS.read_text(encoding='utf-8'), count=1, flags=re.M)
    if changed != 1:
        raise SystemExit(f'{SETTINGS} has no guid line')
    settings, changed = re.subn(r'^(\s+textureCompression:) \d+$', r'\1 0', settings, flags=re.M)
    if changed < 1:
        raise SystemExit(f'{SETTINGS} has no textureCompression line')
    made.with_name(made.name + '.meta').write_text(settings, encoding='utf-8', newline='\n')


def fitted(icon: Image.Image) -> Image.Image:
    """The icon in the middle of the canvas, its outermost pixels stretched out to the canvas edge."""
    shown = icon.resize((SHOWN, SHOWN), Image.LANCZOS)
    edge = (CANVAS - SHOWN) // 2
    canvas = Image.new('RGBA', (CANVAS, CANVAS))
    canvas.paste(shown, (edge, edge))
    last = SHOWN - 1
    canvas.paste(shown.crop((0, 0, SHOWN, 1)).resize((SHOWN, edge)), (edge, 0))
    canvas.paste(shown.crop((0, last, SHOWN, SHOWN)).resize((SHOWN, edge)), (edge, edge + SHOWN))
    canvas.paste(shown.crop((0, 0, 1, SHOWN)).resize((edge, SHOWN)), (0, edge))
    canvas.paste(shown.crop((last, 0, SHOWN, SHOWN)).resize((edge, SHOWN)), (edge + SHOWN, edge))
    for x, y, corner in ((0, 0, (0, 0)), (edge + SHOWN, 0, (last, 0)), (0, edge + SHOWN, (0, last)), (edge + SHOWN, edge + SHOWN, (last, last))):
        canvas.paste(Image.new('RGBA', (edge, edge), shown.getpixel(corner)), (x, y))
    return canvas


icon = Image.open(DELIVERED / 'store_icon_512.png').convert('RGBA')
back = Image.open(DELIVERED / 'store_icon_bg.png').convert('RGB')
if icon.size != (512, 512) or icon.getchannel('A').getextrema()[0] < 255:
    raise SystemExit('store_icon_512.png must be 512x512 with no transparency')
if back.size != (CANVAS, CANVAS):
    raise SystemExit('store_icon_bg.png must be 1024x1024')
TARGET.mkdir(parents=True, exist_ok=True)
folder_meta = TARGET.with_name(TARGET.name + '.meta')
if not folder_meta.exists():
    folder_meta.write_text(FOLDER_META.format(guid=guid_for(TARGET)), encoding='utf-8', newline='\n')
for name, picture in (('icon_512.png', icon), ('icon_adaptive_fg.png', fitted(icon)), ('icon_adaptive_bg.png', back)):
    picture.save(TARGET / name, optimize=True)
    meta(TARGET / name)
print('Made icon_512, icon_adaptive_fg and icon_adaptive_bg in Art/ThrowStoreV1 from the delivered store icon.')
