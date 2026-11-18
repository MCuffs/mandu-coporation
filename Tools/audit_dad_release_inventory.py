"""Repository-only release inventory. Source sizes are not APK size or runtime RAM."""
from pathlib import Path
from collections import defaultdict
import hashlib
import json
import re
import subprocess
from PIL import Image

root = Path(__file__).resolve().parents[1]
resources = [p for p in (root / 'Assets').rglob('*') if p.is_file() and 'Resources' in p.parts and p.suffix != '.meta']
groups = defaultdict(lambda: {'files': 0, 'source_bytes': 0, 'textures': 0, 'rgba_pixel_bytes': 0})
duplicates = defaultdict(list)
missing_meta = []
for p in resources:
    parts = p.relative_to(root).parts
    key = parts[parts.index('Resources') + 1]
    group = groups[key]
    group['files'] += 1
    group['source_bytes'] += p.stat().st_size
    duplicates[hashlib.sha256(p.read_bytes()).hexdigest()].append(str(p.relative_to(root)))
    if not Path(str(p) + '.meta').is_file():
        missing_meta.append(str(p.relative_to(root)))
    if p.suffix.lower() in ('.png', '.jpg', '.jpeg'):
        with Image.open(p) as im:
            group['textures'] += 1
            group['rgba_pixel_bytes'] += im.width * im.height * 4

settings = (root / 'ProjectSettings/ProjectSettings.asset').read_text(encoding='utf-8')
scene_text = (root / 'ProjectSettings/EditorBuildSettings.asset').read_text(encoding='utf-8')
report = dict(
    date='2026-11-09', head=subprocess.check_output(['git', 'rev-parse', '--short', 'HEAD'], cwd=root, text=True).strip(),
    limits='Repository inspection only. RGBA pixel bytes assume every original texture uncompressed and simultaneously resident; actual imports, compression, loading and Android measurements differ.',
    enabled_scenes=re.findall(r'enabled: 1\s+path: (.+)', scene_text),
    company=re.search(r'companyName: (.+)', settings)[1],
    identifiers=re.findall(r'    (\w+): (com\.[^\s]+)', settings),
    resources_files=len(resources), resources_source_bytes=sum(p.stat().st_size for p in resources),
    resource_groups=dict(sorted(groups.items(), key=lambda item: -item[1]['source_bytes'])),
    duplicate_resource_sets=[paths for paths in duplicates.values() if len(paths) > 1],
    missing_resource_meta=missing_meta,
    android_artifacts=[str(p.relative_to(root)) for p in root.rglob('*') if p.is_file() and p.suffix.lower() in ('.aab', '.apk')],
    fonts_license_files=[str(p.relative_to(root)) for p in (root / 'Design/Licenses').glob('*')],
)
out = root / 'Design/dad-release-inventory.json'
out.write_text(json.dumps(report, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
print(json.dumps({k: report[k] for k in ('resources_files', 'resources_source_bytes', 'enabled_scenes', 'company', 'identifiers', 'android_artifacts', 'missing_resource_meta')}, ensure_ascii=False))
print('Resource groups:', json.dumps(report['resource_groups'], ensure_ascii=False))
