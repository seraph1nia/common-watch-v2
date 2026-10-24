#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Run the same legal economy policy against isolated, verified original rules."""
import argparse
from pathlib import Path
import subprocess

REVISION = 'b4fa3f7807fcdf08040e9be2e186833b0504ab01'
# Freeze the matched #21 observer policy before later schedule/opening APIs exist.
HARNESS = 'da3a51d49e3cafb97b67aaf7f7cf6cb3a1f3f04a'
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
policy = subprocess.check_output(['git', 'show', f'{HARNESS}:src/Game.Shared/Development/RewardEconomyReview.cs'])
(root / 'src/Game.Shared/Development/RewardEconomyReview.cs').write_bytes(policy)
current = subprocess.check_output(['git', 'show', f'{HARNESS}:tools/scenarios/Program.cs'], text=True)
start = current.index('    if (args.FirstOrDefault() is \"reward-economy-review\"')
end = current.index('    if (args.FirstOrDefault() == "cooperation-review")', start)
program.write_text(original_program.replace('try\n{', 'try\n{\n' + current[start:end], 1))
print(f'ORIGINAL_ECONOMY_SOURCE_PASS {len(files)} frozen files; frozen harness {HARNESS} copied to {root}')
