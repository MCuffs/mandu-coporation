#!/usr/bin/env python3
"""Read-only Android release preflight. This does not certify a built AAB or Play policy compliance."""
import argparse
import json
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--editor', type=Path, default=Path('/Applications/Unity/Hub/Editor/2021.3.2f1/Unity.app/Contents'))
parser.add_argument('--report', type=Path, default=root / 'Design/dad-android-preflight-v18.json')
parser.add_argument('--native-report', type=Path, help='Fresh DadReleaseAudit output containing actual Unity Android settings')
args = parser.parse_args()
version = re.search(r'm_EditorVersion: (\S+)', (root / 'ProjectSettings/ProjectVersion.txt').read_text()).group(1)
major, minor, patch = map(int, re.match(r'(\d+)\.(\d+)\.(\d+)', version).groups())
supports_16k = (major == 6000 and (minor >= 1 or patch >= 38)) or (major == 2022 and minor == 3 and patch >= 56) or (major == 2021 and minor == 3 and patch >= 48)
settings = (root / 'ProjectSettings/ProjectSettings.asset').read_text()
def number(key):
    match = re.search(r'^  ' + re.escape(key) + r': (\d+)', settings, re.M)
    return int(match.group(1)) if match else None
architecture = number('AndroidTargetArchitectures') or 0
target = number('AndroidTargetSdkVersion')
platforms = args.editor / 'PlaybackEngines/AndroidPlayer/SDK/platforms'
installed = sorted(int(p.name.split('-')[1]) for p in platforms.glob('android-*') if p.name.split('-')[1].isdigit())
checks = [
    dict(name='Engine includes known 16 KB support', passed=supports_16k, value=version),
    dict(name='Android build module installed', passed=(args.editor / 'PlaybackEngines/AndroidPlayer').is_dir()),
    dict(name='ARM64 build architecture enabled', passed=bool(architecture & 2), value=architecture),
    dict(name='Explicit target SDK 36 or later', passed=target is not None and target >= 36, value='Automatic; installed SDK and final AAB must be inspected' if target == 0 else target),
    dict(name='Installed SDK includes API 36 or later', passed=bool(installed and max(installed) >= 36), value=installed),
]
if args.native_report:
    native = args.native_report.read_text(encoding='utf-8')
    match = re.search(r'^Android application ID: (.*)$', native, re.M)
    identifier = match.group(1).strip() if match else ''
    checks.append(dict(name='Native Android application ID has valid segments', passed=bool(re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+', identifier)), value=identifier))
report = dict(as_of='2026-11-09', status='PREFLIGHT_PASS' if all(c['passed'] for c in checks) else 'BLOCKED', checks=checks,
              limits='Configuration checks only. Android build, native library alignment, signing, device performance and Play Console validation are still required.',
              sources=['https://developer.android.com/google/play/requirements/target-sdk',
                       'https://discussions.unity.com/t/android-unity-engine-support-for-16-kb-memory-page-sizes-android-15/1589588'])
output = args.report
output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps(report, ensure_ascii=False, indent=2))
