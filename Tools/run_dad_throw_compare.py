#!/usr/bin/env python3
"""Compare throw-game numbers with bots, using Unity's bundled Mono, without launching Unity or any windows.

Compiles the same sources as Tools/run_dad_headless_checks.py and runs DadThrowCompare.Run(), which plays
copies of the stage tables under different rule numbers. The game's own numbers and files are not changed.
The bots aim perfectly: the report shows what the numbers allow, not what people will do.
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
parser.add_argument('--report', type=Path, default=ROOT / 'Design/dad-throw-number-compare.txt')
args = parser.parse_args()
mono = args.editor_contents / 'MonoBleedingEdge/bin/mono'
mcs = args.editor_contents / 'MonoBleedingEdge/lib/mono/4.5/mcs.exe'
managed = args.editor_contents / 'Managed/UnityEngine'
if not mono.is_file() or not mcs.is_file() or not managed.is_dir():
    parser.error('Bundled Mono/compiler not found. Supply --editor-contents; this script never opens or installs Unity.')

RUNNER = '''using System;
class DadThrowCompareRunner {
    static int Main() {
        try { Console.Write(DadThrowCompare.Run()); return 0; }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}'''


def run(command, limit, **options):
    result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, timeout=limit, **options)
    if result.returncode:
        raise RuntimeError(result.stdout + result.stderr)
    return result


try:
    with tempfile.TemporaryDirectory(prefix='dad-compare-') as folder:
        temp = Path(folder)
        library = temp / 'DadRules.dll'
        references = ['-r:' + str(path) for path in sorted(managed.glob('*.dll'))]
        sources = [str(path) for path in sorted((ROOT / 'Assets').rglob('*.cs')) if 'Editor' not in path.relative_to(ROOT / 'Assets').parts]
        common = [str(mono), str(mcs), '-langversion:latest', '-define:SLING_HEADLESS']
        run(common + ['-target:library', '-out:' + str(library)] + references + sources, 180)
        (temp / 'Runner.cs').write_text(RUNNER)
        run(common + ['-out:' + str(temp / 'Runner.exe'), '-r:' + str(library)] + references + [str(temp / 'Runner.cs')], 180)
        environment = dict(os.environ)
        environment['MONO_PATH'] = os.pathsep.join([str(managed), str(temp)])
        started = time.time()
        compared = run([str(mono), str(temp / 'Runner.exe')], 1500, env=environment)
        if 'THROW NUMBER COMPARISON' not in compared.stdout:
            raise RuntimeError('Runner returned no comparison')
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(compared.stdout)
        print(f'Compared in {time.time() - started:.0f}s; no Unity process or window launched.')
        print(f'Report: {args.report}')
except (RuntimeError, subprocess.TimeoutExpired) as error:
    parser.exit(1, str(error) + '\n')
