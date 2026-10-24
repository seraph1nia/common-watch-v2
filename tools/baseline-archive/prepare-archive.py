#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Instrument a disposable original-rules checkout; never modify the active Core."""
import argparse
import hashlib
from pathlib import Path
import shutil
import subprocess

REVISION = 'b4fa3f7807fcdf08040e9be2e186833b0504ab01'
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--source', type=Path, required=True)
args = parser.parse_args()
root = args.source.resolve()
resources = Path(__file__).resolve().parent
repository = Path(subprocess.check_output(['git', 'rev-parse', '--show-toplevel']).decode().strip()).resolve()
if root == repository:
    raise SystemExit('Refusing to instrument the active checkout.')
files = subprocess.check_output(['git', 'ls-tree', '-r', '--name-only', REVISION,
                                 'src/Game.Core', 'src/Game.Shared'], text=True).splitlines()
for name in files:
    expected = subprocess.check_output(['git', 'show', f'{REVISION}:{name}'])
    if (root / name).read_bytes() != expected:
        raise SystemExit('Original source mismatch: ' + name)
relative = 'src/Game.Shared/Development/CampaignReview.cs'
original = subprocess.check_output(['git', 'show', f'{REVISION}:{relative}'])
if (root / relative).read_bytes() != original:
    raise SystemExit('Expected untouched original campaign policy in disposable checkout.')
program = root / 'tools/scenarios/Program.cs'
old_program = subprocess.check_output(['git', 'show', f'{REVISION}:tools/scenarios/Program.cs']).decode()
if program.read_text() != old_program:
    raise SystemExit('Expected untouched original CLI in disposable checkout.')
source = original.decode()
start, end = source.index('public sealed record EncounterResult'), source.index('/// <summary>Legal')
source = source[:start] + source[end:]

def replace(old, new):
    global source
    if source.count(old) != 1:
        raise SystemExit('Frozen instrumentation anchor changed: ' + old)
    source = source.replace(old, new)

replace('public static class CampaignReview', 'public static class ArchivedCampaignReview')
replace('Run(string family, ulong seed, bool adaptive)', 'Run(string family, ulong seed, bool adaptive, string archiveDirectory)')
replace('int player = match.Join()!.Id; long sequence = 0;',
        'int player = match.Join()!.Id; long sequence = 0;\n        using var archive = new ArchiveRecorder(archiveDirectory, match.Snapshot());')
replace('void Submit(GameplayIntent intent) => Accept(match.Apply(player, intent.Capture(match.Snapshot(), ++sequence, player)));',
        '''void Submit(GameplayIntent intent)
        {
            var command = intent.Capture(match.Snapshot(), ++sequence, player); long tick = match.Tick;
            var result = match.Apply(player, command); Accept(result);
            archive.Command(tick, player, command, result, match.Snapshot());
        }''')
replace('void Send(string action) => Accept(match.Apply(player, Command.FromSnapshot(match.Snapshot(), ++sequence, action, player)));',
        '''void Send(string action)
        {
            var command = Command.FromSnapshot(match.Snapshot(), ++sequence, action, player); long tick = match.Tick;
            var result = match.Apply(player, command); Accept(result);
            archive.Command(tick, player, command, result, match.Snapshot());
        }''')
replace('var prepared = match.Snapshot(); var before = prepared.Players.Single(p => p.Id == player);',
        'var prepared = match.Snapshot(); var before = prepared.Players.Single(p => p.Id == player); archive.BeginEncounter(prepared);')
replace('for (int tick = 0; tick < 18000 && (match.Phase == Phase.Combat || match.Snapshot().DyingBodies.Length > 0); tick++) match.Step();',
        'for (int tick = 0; tick < 18000 && (match.Phase == Phase.Combat || match.Snapshot().DyingBodies.Length > 0); tick++) { match.Step(); archive.Observe(match.Snapshot()); }')
replace('var after = match.Snapshot(); var city = after.Players.Single(p => p.Id == player);',
        'var after = match.Snapshot(); var city = after.Players.Single(p => p.Id == player); archive.EndEncounter(after);')
replace('return new(family, seed, adaptive, match.Id, results.ToArray());',
        'archive.Complete(match.Snapshot()); return new(family, seed, adaptive, match.Id, results.ToArray());')
(root / 'src/Game.Shared/Development/ArchivedCampaignReview.cs').write_text(source)
shutil.copyfile(resources / 'ArchiveRecorder.cs', root / 'src/Game.Shared/Development/ArchiveRecorder.cs')
shutil.copyfile(resources / 'ArchiveRunner.cs', root / 'tools/scenarios/ArchiveRunner.cs')
program.write_text(old_program.replace('try\n{', 'try\n{\n    if (args.FirstOrDefault() == "archive-baseline") return ArchiveRunner.Run(args[1]);\n    if (args.FirstOrDefault() == "verify-baseline") return ArchiveRunner.VerifyOnly(args[1]);', 1))
print('Original policy SHA256:', hashlib.sha256(original).hexdigest())
print('Instrumentation written only beneath:', root)
