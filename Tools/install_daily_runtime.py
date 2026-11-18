#!/usr/bin/env python3
"""Copy the art and sounds of today's challenge from Art/ into Resources/, where the game loads them from.

Art/ holds the originals with their manifests and prompts; nothing there is read by the game. This copies only
the files the game uses (Design/dad-throw-daily-asset-request-v1.md, Design/daily-assets-handoff.md) and writes
a .meta beside each copy with the original's import settings and a GUID of its own, made from the copy's path:
running this again changes nothing, and a copy never shares a GUID with its original. It never opens Unity.

Not copied because the game does not draw them yet: rule_card_back (the card-turning opening is undecided),
envelope_closed (the mailbox of the home screen), unlock_daily (the notice for a newly opened screen).
"""
import hashlib
from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / 'Assets/DadSchoolBoss/Art'
RESOURCES = ROOT / 'Assets/DadSchoolBoss/Resources'
RULES = ['dodge', 'trays', 'ballonly', 'shoefeast', 'bigball', 'bouncy', 'onegate', 'reverse', 'goldrush', 'fog', 'oneway', 'bossin']
PICTURES = {
    'ThrowUiV2': ['paper_note', 'rule_card_frame', 'envelope_open', 'icon_letter', 'badge_alert', 'stamp_done', 'icon_check', 'chance_on', 'chance_off',
                  'tier_clear', 'tier_gate', 'tier_chain', 'chest_wood_closed', 'chest_wood_open', 'chest_silver_closed', 'chest_silver_open',
                  'chest_gold_closed', 'chest_gold_open', 'fx_chest_coins_0', 'fx_chest_coins_1', 'fx_chest_coins_2'],
    'ThrowHudV2': ['rule_' + rule for rule in RULES] + ['rule_chip', 'fog_veil', 'fog_puff_0', 'fog_puff_1', 'fog_puff_2', 'mark_ban'],
}
SOUNDS = ['sfx_envelope', 'sfx_stamp', 'sfx_chest_open', 'sfx_card_deal', 'sfx_try_spent', 'jingle_daily_clear']
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
    return hashlib.md5(('throw-daily-runtime:' + path.relative_to(ROOT).as_posix()).encode('utf-8')).hexdigest()


def folder(target: Path) -> None:
    target.mkdir(parents=True, exist_ok=True)
    meta = target.with_name(target.name + '.meta')
    if not meta.exists():
        meta.write_text(FOLDER_META.format(guid=guid_for(target)), encoding='utf-8', newline='\n')


def copy(original: Path, target: Path) -> None:
    original_meta = original.with_name(original.name + '.meta')
    if not original.exists() or not original_meta.exists():
        raise SystemExit(f'{original} or its .meta is missing')
    made = target / original.name
    shutil.copyfile(original, made)
    settings, changed = re.subn(r'^guid: [0-9a-f]{32}$', 'guid: ' + guid_for(made), original_meta.read_text(encoding='utf-8'), count=1, flags=re.M)
    if changed != 1:
        raise SystemExit(f'{original_meta} has no guid line')
    made.with_name(made.name + '.meta').write_text(settings, encoding='utf-8', newline='\n')


pictures, sounds = RESOURCES / 'DadThrowDailyV1', RESOURCES / 'DadThrowAudioV1'
folder(pictures)
folder(sounds)
count = 0
for source, names in PICTURES.items():
    for name in names:
        copy(ART / source / (name + '.png'), pictures)
        count += 1
for name in SOUNDS:
    copy(ART / 'ThrowAudioV1' / (name + '.wav'), sounds)
print(f'Copied {count} pictures and {len(SOUNDS)} sounds into Resources with their own GUIDs.')
