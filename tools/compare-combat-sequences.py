#!/usr/bin/env python3
"""Compare actual native recordings; these are observations, never constructed game state."""
import argparse
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument('--before', type=Path, default=Path('artifacts/captures/combat-review/before'))
parser.add_argument('--after', type=Path, default=Path('artifacts/captures/combat-review/after'))
parser.add_argument('--output', type=Path, default=Path('artifacts/captures/combat-review/comparison.json'))
parser.add_argument("--profiles", nargs="+", default=["early", "dense", "major-10", "boss-20"], help="Use boss-10 instead of major-10 for historical R4 source recordings.")
args = parser.parse_args()

def intersects(a, b):
    return a[0] < b[0] + b[2] and a[0] + a[2] > b[0] and a[1] < b[1] + b[3] and a[1] + a[3] > b[1]

rows = []
for width in (1280, 1920):
    for profile in args.profiles:
        before = json.loads((args.before / str(width) / (profile + '.json')).read_text())
        after = json.loads((args.after / str(width) / (profile + '.json')).read_text())
        assert before['outcome'] == after['outcome'], (width, profile, 'authoritative outcome changed')
        assert before['endTick'] - before['startTick'] == after['endTick'] - after['startTick'], (width, profile, 'authoritative duration changed')
        old = before['samples'][0]['data']['geometry']
        new = after['samples'][0]['data']['geometry']
        gain = new['medianActorHeight'] / old['medianActorHeight']
        assert gain > 1.05, (width, profile, 'useful figures did not grow')
        assert new['labelActorOverlaps'] < old['labelActorOverlaps'], (width, profile, 'initial label overlaps did not reduce')
        inspected = 0
        for sample in after['samples']:
            data = sample['data']
            if not data.get('inspector'):
                continue
            geometry = data['geometry']
            for actor in geometry['actors']:
                if actor['id'] in (geometry['selected'], geometry['highlightedTarget']):
                    assert not intersects(actor['bounds'], data['inspector']), (width, profile, sample['frame'], 'inspector covers subject or target')
                    inspected += 1
        assert inspected > 0, (width, profile, 'no actual selected subject was observed')
        rows.append(dict(width=width, profile=profile, ticks=after['endTick'] - after['startTick'],
                         beforeActorHeight=old['medianActorHeight'], afterActorHeight=new['medianActorHeight'], sizeRatio=gain,
                         beforeLabels=len(old['labels']), afterLabels=len(new['labels']),
                         beforeLabelActorOverlaps=old['labelActorOverlaps'], afterLabelActorOverlaps=new['labelActorOverlaps'],
                         beforeWallSeconds=before['wallSeconds'], afterWallSeconds=after['wallSeconds'],
                         inspectedBodyChecks=inspected, outcomeEqual=True))
args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text(json.dumps(dict(comparisons=rows, limitations='Native software-rendered observations. Video proxies are half-scale; PNGs and measured rectangles use native window pixels. No human comprehension or strategic finding.'), indent=2))
print('COMBAT_SEQUENCE_COMPARISON_PASS eight deterministic native fixtures; larger actors, fewer initial overlaps, unchanged outcomes/ticks, selected bodies/targets unobscured')
