#!/usr/bin/env python3
"""Read-only Android release preflight. This does not certify a built AAB or Play policy compliance."""
import argparse
import json
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--editor', type=Path, default=Path('/Applications/Unity/Hub/Editor/2021.3.2f1/Unity.app/Contents'))
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
checks = [
    dict(name='Engine includes known 16 KB support', passed=supports_16k, value=version),
    dict(name='Android build module installed', passed=(args.editor / 'PlaybackEngines/AndroidPlayer').is_dir()),
    dict(name='ARM64 build architecture enabled', passed=bool(architecture & 2), value=architecture),
    dict(name='Explicit target SDK 36 or later', passed=target is not None and target >= 36, value='Automatic; installed SDK and final AAB must be inspected' if target == 0 else target),
]
report = dict(as_of='2026-11-09', status='PREFLIGHT_PASS' if all(c['passed'] for c in checks) else 'BLOCKED', checks=checks,
              limits='Configuration checks only. Android build, native library alignment, signing, device performance and Play Console validation are still required.',
              sources=['https://developer.android.com/google/play/requirements/target-sdk',
                       'https://discussions.unity.com/t/android-unity-engine-support-for-16-kb-memory-page-sizes-android-15/1589588'])
output = root / 'Design/dad-android-preflight-v18.json'
output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n')
print(json.dumps(report, ensure_ascii=False, indent=2))
