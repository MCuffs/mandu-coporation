#!/usr/bin/env python3
"""Play a bot through the throw game's stages and shop, using Unity's bundled Mono, without launching Unity or any windows.

Compiles the same sources as Tools/run_dad_headless_checks.py and runs DadThrowGrowthChecks.Report(): a bot plays
the stages in order, banks one won for each challenger sent back, spends in the shop after every fight and tries
a lost stage again. The bots are arithmetic, not people: the report shows what the numbers allow, never whether
it is fun.

Windows: pass --editor-contents "C:/Program Files/Unity/Hub/Editor/<version>/Editor/Data".
"""
import argparse
import os
from pathlib import Path
import re
import subprocess
import tempfile
import time

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
version = re.search(r'm_EditorVersion: (\S+)', (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text()).group(1)
parser.add_argument('--editor-contents', type=Path, default=Path('/Applications/Unity/Hub/Editor') / version / 'Unity.app/Contents')
parser.add_argument('--report', type=Path, default=ROOT / 'Design/dad-throw-growth-report.txt')
args = parser.parse_args()
mono = args.editor_contents / 'MonoBleedingEdge/bin/mono'
if os.name == 'nt':
    mono = mono.with_suffix('.exe')
mcs = args.editor_contents / 'MonoBleedingEdge/lib/mono/4.5/mcs.exe'
managed = args.editor_contents / 'Managed/UnityEngine'
if not mono.is_file() or not mcs.is_file() or not managed.is_dir():
    parser.error('Bundled Mono/compiler not found. Supply --editor-contents; this script never opens or installs Unity.')

RUNNER = '''using System;
class DadThrowGrowthReportRunner {
    static int Main() {
        try { Console.OutputEncoding = new System.Text.UTF8Encoding(false); Console.Write(DadThrowGrowthChecks.Report()); return 0; }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}'''


def run(command, limit, **options):
    result = subprocess.run(command, cwd=ROOT, text=True, encoding='utf-8', capture_output=True, timeout=limit, **options)
    if result.returncode:
        raise RuntimeError(result.stdout + result.stderr)
    return result


try:
    with tempfile.TemporaryDirectory(prefix='dad-growth-report-') as folder:
        temp = Path(folder)
        library = temp / 'DadRules.dll'
        references = ['-r:' + str(path) for path in sorted(managed.glob('*.dll'))]
        sources = [str(path) for path in sorted((ROOT / 'Assets').rglob('*.cs')) if 'Editor' not in path.relative_to(ROOT / 'Assets').parts]
        common = [str(mono), str(mcs), '-langversion:latest', '-define:SLING_HEADLESS']
        run(common + ['-target:library', '-out:' + str(library)] + references + sources, 180)
        (temp / 'Runner.cs').write_text(RUNNER, encoding='utf-8')
        run(common + ['-out:' + str(temp / 'Runner.exe'), '-r:' + str(library)] + references + [str(temp / 'Runner.cs')], 180)
        environment = dict(os.environ)
        environment['MONO_PATH'] = os.pathsep.join([str(managed), str(temp)])
        started = time.time()
        played = run([str(mono), str(temp / 'Runner.exe')], 3000, env=environment)
        if 'THROW STAGES AND SHOP REPORT' not in played.stdout:
            raise RuntimeError('Runner returned no report')
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(played.stdout, encoding='utf-8', newline='\n')
        print(f'Played in {time.time() - started:.0f}s; no Unity process or window launched.')
        print(f'Report: {args.report}')
except (RuntimeError, subprocess.TimeoutExpired) as error:
    parser.exit(1, str(error) + '\n')
