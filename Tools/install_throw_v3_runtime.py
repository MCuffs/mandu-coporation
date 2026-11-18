#!/usr/bin/env python3
"""Copy the approved v3 throw art from Art/ into Resources/, where the game loads it from.

Art/ holds the originals with their manifests, prompts and sources; nothing there is read by the game.
This copies only the PNG files the game uses and writes a .meta beside each copy with the original's import
settings and a GUID of its own (made from the copy's path, so running this again changes nothing and the
copy never shares a GUID with its original). It never opens Unity.
"""
import hashlib
from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/DadSchoolBoss/Art'
RESOURCES = ROOT / 'Assets/DadSchoolBoss/Resources'
FOLDERS = {'ThrowEntranceV1': 'DadThrowEntranceV1', 'ThrowShopV1': 'DadThrowShopV1', 'ThrowFxV2': 'DadThrowFxV2'}
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
    return hashlib.md5(('throw-v3-runtime:' + path.relative_to(ROOT).as_posix()).encode('utf-8')).hexdigest()


copied = 0
for source_name, target_name in FOLDERS.items():
    source, target = ART / source_name, RESOURCES / target_name
    target.mkdir(parents=True, exist_ok=True)
    folder_meta = target.with_name(target.name + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text(FOLDER_META.format(guid=guid_for(target)), encoding='utf-8', newline='\n')
    for picture in sorted(source.glob('*.png')):
        original_meta = picture.with_name(picture.name + '.meta')
        if not original_meta.exists():
            raise SystemExit(f'{picture} has no .meta to take import settings from')
        copy = target / picture.name
        shutil.copyfile(picture, copy)
        settings, changed = re.subn(r'^guid: [0-9a-f]{32}$', 'guid: ' + guid_for(copy), original_meta.read_text(encoding='utf-8'), count=1, flags=re.M)
        if changed != 1:
            raise SystemExit(f'{original_meta} has no guid line')
        copy.with_name(copy.name + '.meta').write_text(settings, encoding='utf-8', newline='\n')
        copied += 1
print(f'Copied {copied} pictures into Resources with their own GUIDs.')
