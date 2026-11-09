#!/usr/bin/env python3
"""Compile and run managed battle rules with Unity's bundled Mono, without launching Unity or any windows."""
import argparse
import os
from pathlib import Path
import re
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
version = re.search(r'm_EditorVersion: (\S+)', (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text()).group(1)
parser.add_argument('--editor-contents', type=Path, default=Path('/Applications/Unity/Hub/Editor') / version / 'Unity.app/Contents')
parser.add_argument('--report', type=Path, default=ROOT / 'Design/dad-headless-checks-v19.txt')
args = parser.parse_args()
mono = args.editor_contents / 'MonoBleedingEdge/bin/mono'
mcs = args.editor_contents / 'MonoBleedingEdge/lib/mono/4.5/mcs.exe'
managed = args.editor_contents / 'Managed/UnityEngine'
if not mono.is_file() or not mcs.is_file() or not managed.is_dir():
    parser.error('Bundled Mono/compiler not found. Supply --editor-contents; this script never opens or installs Unity.')

RUNNER = '''using System;
class DadHeadlessRunner {
    static int Main() {
        try { Console.Write(SlingshotChecks.RunChecks()); Console.Write(DadDifficultyChecks.Run()); return 0; }
        catch(Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}'''

def run(command, **options):
    result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, timeout=180, **options)
    if result.returncode:
        raise RuntimeError(result.stdout + result.stderr)
    return result

try:
    with tempfile.TemporaryDirectory(prefix='dad-headless-') as folder:
        temp = Path(folder)
        library = temp / 'DadRules.dll'
        references = ['-r:' + str(path) for path in sorted(managed.glob('*.dll'))]
        sources = [str(path) for path in sorted((ROOT / 'Assets').rglob('*.cs')) if 'Editor' not in path.relative_to(ROOT / 'Assets').parts]
        common = [str(mono), str(mcs), '-langversion:latest', '-define:SLING_HEADLESS']
        compiled = run(common + ['-target:library', '-out:' + str(library)] + references + sources)
        (temp / 'Runner.cs').write_text(RUNNER)
        run(common + ['-out:' + str(temp / 'Runner.exe'), '-r:' + str(library)] + references + [str(temp / 'Runner.cs')])
        environment = dict(os.environ)
        environment['MONO_PATH'] = os.pathsep.join([str(managed), str(temp)])
        checked = run([str(mono), str(temp / 'Runner.exe')], env=environment)
        count = sum(line.startswith('PASS ') for line in checked.stdout.splitlines())
        if not count:
            raise RuntimeError('Runner returned no test results')
        report = 'Window-free managed simulation checks\nUnity Editor and standalone player were not launched.\nThese checks do not validate rendering, touch gestures, native serialization or Android devices.\n\n' + checked.stdout + f'\nTOTAL PASS: {count}\n'
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(report)
        print(f'PASS: {count} simulation checks; no Unity process or window launched.')
        print(f'Report: {args.report}')
except (RuntimeError, subprocess.TimeoutExpired) as error:
    parser.exit(1, str(error) + '\n')
