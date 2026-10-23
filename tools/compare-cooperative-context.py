#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Compare actual before/after native panel geometry at matching boundaries."""
import argparse
import json
from pathlib import Path


def compare(root):
    rows = []
    for players in (2, 4, 6):
        for width in (1280, 1920):
            for boundary in ('preparing', 'ready-waiting', 'local-ready-waiting'):
                name = f'{players}p-{boundary}-{width}.json'
                before, after = [json.loads((root / layout / name).read_text()) for layout in ('before', 'after')]
                for field in ('player', 'players', 'phase', 'wave', 'turn', 'paused', 'width', 'height', 'hudMoment'):
                    assert before[field] == after[field], (name, field)
                assert len(before['teamRows']) == len(after['teamRows']) == players
                old, new = before['teamRect'], after['teamRect']
                assert old['width'] == new['width'] and old['height'] > new['height'], (name, old, new)
                rows.append(dict(players=players, width=width, boundary=boundary,
                                 beforeHeight=old['height'], afterHeight=new['height'],
                                 areaReductionPercent=round(100 * (1 - new['height'] / old['height']), 2),
                                 beforeRows=before['teamRows'], afterRows=after['teamRows'],
                                 afterWarnings=after.get('teamWarnings', '')))
    return dict(cases=rows, limitation='Matching scripted native boundaries, not human decision time, social experience or balance evidence.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path('artifacts/captures/cooperation-review'))
    args = parser.parse_args()
    report = compare(args.root)
    (args.root / 'comparison.json').write_text(json.dumps(report, indent=2) + '\n')
    print('GODOT_COOP_COMPARISON_PASS cases=' + str(len(report['cases'])) + ' retained-players exact-boundaries smaller-default-panels')
