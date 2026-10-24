#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Compare native figure geometry, without claiming paired network outcomes."""
import argparse
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--before', type=Path, default=Path('artifacts/captures/combat-review/r10-before'))
parser.add_argument('--after', type=Path, default=Path('artifacts/captures/combat-review/r10-final'))
parser.add_argument('--output', type=Path, default=Path('artifacts/captures/cooperation-review/framing-comparison.json'))
args = parser.parse_args()
rows = []
for width in (1280, 1920):
    old, new = [json.loads((root / f'network-6p-{width}/sequence.json').read_text()) for root in (args.before, args.after)]
    for run in (old, new):
        assert run['players'] == 6 and run['width'] == width and run['wave'] == 6
        assert run['speed'] == '1x' and run['factor'] == .5
        assert len(run['initial']['Players']) == 6 and run['initial']['Phase'] == 'Combat'
        assert run['initial']['Paused'] and run['initial']['CombatSpeed'] == 'One'
    geometry = [next(sample['data']['geometry'] for sample in run['samples'] if sample['name'] == 'local-start') for run in (old, new)]
    before, after = geometry
    # Gate A's unresolved framing case is maximum-group 720p. Require a
    # substantial gain there; retain 1080p readability rather than demanding
    # the same percentage from a viewport already close to its vertical fit.
    minimum_ratio = 1.15 if width == 1280 else 1.0
    assert after['medianActorHeight'] > before['medianActorHeight'] * minimum_ratio, (width, 'insufficient maximum-group figure gain')
    assert after['zoom'] > before['zoom'], (width, 'saved default zoom did not improve')
    assert after['labelActorOverlaps'] <= before['labelActorOverlaps'], (width, 'initial label collisions grew')
    assert after['labelLabelOverlaps'] <= before['labelLabelOverlaps'], (width, 'initial labels collide')
    assert any(sample['data']['cameraMode'] == 'Overview' for sample in new['samples']), 'explicit overview coverage missing'
    rows.append(dict(width=width, beforeActorHeight=before['medianActorHeight'], afterActorHeight=after['medianActorHeight'],
                     sizeRatio=after['medianActorHeight']/before['medianActorHeight'], beforeActors=before['actorCount'], afterActors=after['actorCount'],
                     beforeLabelActorOverlaps=before['labelActorOverlaps'], afterLabelActorOverlaps=after['labelActorOverlaps'],
                     beforeWallSeconds=old['wallSeconds'], afterWallSeconds=new['wallSeconds']))
args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text(json.dumps(dict(cases=rows, limitation='Real six-client normal 1x encounters after paid accelerated setup. Network interleaving changes IDs/tie breaks; outcomes are unpaired. Native PNG/geometry, half-scale optional video proxies; no human/hardware findings.'), indent=2) + '\n')
print('GODOT_COOP_FRAMING_PASS maximum-group both sizes larger figures initial-label-collisions-not-increased explicit-overview')
