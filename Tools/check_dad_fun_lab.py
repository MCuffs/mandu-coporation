"""Compile and play experiment strategies without opening Unity or changing saves (Windows)."""
from pathlib import Path
import os
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
DATA = Path(os.environ.get('DAD_UNITY_DATA', 'C:/Program Files/Unity/Hub/Editor/2021.3.2f1/Editor/Data'))
MONO = DATA / 'MonoBleedingEdge/bin/mono.exe'
MCS = DATA / 'MonoBleedingEdge/lib/mono/4.5/mcs.exe'
MANAGED = DATA / 'Managed/UnityEngine'

def run(args, env=None):
    result = subprocess.run(args, cwd=ROOT, capture_output=True, env=env, timeout=180)
    out = result.stdout.decode('utf-8-sig', errors='replace')
    err = result.stderr.decode('utf-8-sig', errors='replace')
    if result.returncode:
        raise RuntimeError(out + err)
    return out + err

with tempfile.TemporaryDirectory(prefix='dad-fun-lab-') as folder:
    temp = Path(folder)
    refs = ['-r:' + str(p) for p in MANAGED.glob('*.dll')]
    sources = [str(p) for p in (ROOT / 'Assets').rglob('*.cs') if 'Editor' not in p.relative_to(ROOT / 'Assets').parts]
    base = [str(MONO), str(MCS), '-langversion:latest', '-define:SLING_HEADLESS']
    compilation = run(base + ['-target:library', '-out:' + str(temp / 'Rules.dll')] + refs + sources)
    run(base + ['-out:' + str(temp / 'Play.exe'), '-r:' + str(temp / 'Rules.dll')] + refs + [str(ROOT / 'Tools/DadFunLabRunner.cs')])
    env = dict(os.environ, MONO_PATH=str(MANAGED))
    report = run([str(MONO), str(temp / 'Play.exe')], env)
    (ROOT / 'Design/dad-fun-lab-playchecks.txt').write_text('Programmatic strategy play, not human fun or rendering validation.\n' + compilation + report, encoding='utf-8')
    print('\n'.join(line for line in report.splitlines() if line.startswith('PLAY ')))
    print('PASS:', sum(line.startswith('PASS ') for line in report.splitlines()))
