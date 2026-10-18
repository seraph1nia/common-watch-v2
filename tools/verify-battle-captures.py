#!/usr/bin/env python3
"""Validate actual frame files and their authoritative capture receipts (no image synthesis)."""
import json
import pathlib
import struct
import sys

root = pathlib.Path(sys.argv[1])
required = [f'swordsman-{phase}' for phase in ('idle', 'moving', 'anticipation', 'lunge', 'impact', 'recovery', 'damaged', 'death')]
required += ['ranged-projectile', 'magic-impact', 'status-effect', 'battle-complete', 'structure-shot', 'status-burn', 'status-chill', 'structure-damaged', 'structure-destroyed', 'tower-shot', 'structure-battle-complete']
for name in required:
    path = root / (name + '.png')
    data = path.read_bytes()
    assert data[:8] == b'\x89PNG\r\n\x1a\n', path
    width, height = struct.unpack('>II', data[16:24])
    assert width >= 1280 and height >= 720, path
    receipt = json.loads(path.with_suffix('.json').read_text())
    assert receipt['presentationTick'] <= receipt['authoritativeTick'], path
    assert receipt['eventSequence'] >= 0, path
    assert receipt['activeEventEffects'] <= 4096, path
    assert all(e['Tick'] <= receipt['presentationTick'] for e in receipt['recentEvents']), path
impact = json.loads((root / 'swordsman-impact.json').read_text())
assert any(e['type'] == 'Impact' for e in impact['recentEvents'])
status = json.loads((root / 'status-effect.json').read_text())
assert status['scenario'] == 'status'
for name, key in [('status-effect', 'poison'), ('status-burn', 'burn'), ('status-chill', 'chill')]:
    receipt = json.loads((root / (name + '.json')).read_text())
    assert any(u['statuses'][key] for u in receipt['units']), name
assert (root / 'swordsman-idle.png').read_bytes() != (root / 'swordsman-lunge.png').read_bytes()
sequence = sorted((root / 'sequence').glob('*.json'))
if sequence:
    ticks = [json.loads(p.read_text())['presentationTick'] for p in sequence]
    assert ticks == sorted(set(ticks)) and len(ticks) >= 20
print(f'ANIMATION_CAPTURE_VERIFIED {root}: {len(required)} phases, {len(sequence)} ordered frames')
