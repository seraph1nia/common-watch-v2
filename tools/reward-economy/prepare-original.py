#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Run the same legal economy policy against isolated, verified original rules."""
import argparse
from pathlib import Path
import shutil
import subprocess

REVISION = 'b4fa3f7807fcdf08040e9be2e186833b0504ab01'
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--source', type=Path, required=True)
a = p.parse_args()
root = a.source.resolve()
repository = Path(subprocess.check_output(['git', 'rev-parse', '--show-toplevel'], text=True).strip())
if root == repository:
    raise SystemExit('Refusing the active checkout.')
files = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', REVISION, 'src/Game.Core', 'src/Game.Shared'], text=True).splitlines()
for name in files:
    original = subprocess.check_output(['git', 'show', f'{REVISION}:{name}'])
    if (root / name).read_bytes() != original:
        raise SystemExit('Original source mismatch: ' + name)
program = root / 'tools/scenarios/Program.cs'
original_program = subprocess.check_output(['git', 'show', f'{REVISION}:tools/scenarios/Program.cs'], text=True)
if program.read_text() != original_program:
    raise SystemExit('Expected untouched original CLI.')
shutil.copyfile(repository / 'src/Game.Shared/Development/RewardEconomyReview.cs', root / 'src/Game.Shared/Development/RewardEconomyReview.cs')
current = (repository / 'tools/scenarios/Program.cs').read_text()
start = current.index('    if (args.FirstOrDefault() == "reward-economy-review")')
end = current.index('    if (args.FirstOrDefault() == "cooperation-review")', start)
program.write_text(original_program.replace('try\n{', 'try\n{\n' + current[start:end], 1))
print(f'ORIGINAL_ECONOMY_SOURCE_PASS {len(files)} frozen files; observed harness copied to {root}')
