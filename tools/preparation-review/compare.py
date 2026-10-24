#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Compare accepted preparation policies with exposure and timing limits explicit."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--before', type=Path, required=True)
p.add_argument('--after', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
p.add_argument('--before-stages', type=int, choices=(2, 3), default=3)
p.add_argument('--after-stages', type=int, choices=(2, 3), default=2)
a = p.parse_args()

def load(filename):
    return {(r['Doctrine'], r['Family'], r['Seed']): r for r in json.loads(filename.read_text())}

def metrics(run):
    commands, battles = run['Commands'], run['Battles']
    payments = [c for c in commands if c['Command']['Action'] == 'ready']
    return dict(battles=len(battles), result=battles[-1]['Result'], ready=len(payments),
                actions=dict(Counter(c['Command']['Action'] for c in commands)),
                food=sum(b['FoodPaid'] for b in battles), casualties=sum(b['Casualties'] for b in battles),
                keepDamage=sum(b['KeepDamage'] for b in battles), ordinaryHealing=sum(c['SoldierHealing'] for c in payments),
                researchPointsOnReady=sum(c['ResearchPointsAfter'] - c['ResearchPointsBefore'] for c in payments),
                shortages=run['Shortages'], perWave=[{k: b[k] for k in ('Wave', 'Result', 'Participating', 'Funded', 'Unfunded', 'FoodPaid', 'Casualties', 'Wounded', 'KeepDamage', 'Technologies')} for b in battles],
                finalStock=battles[-1]['EndStock'])

old, new = load(a.before), load(a.after)
assert old.keys() == new.keys()
rows = []
for key in old:
    x, y = old[key], new[key]
    assert x['MatchId'] == y['MatchId'] and x['Starting'] == y['Starting']
    for stages, run in ((a.before_stages, x), (a.after_stages, y)):
        assert all(c['Result']['Accepted'] for c in run['Commands'])
        assert sum(c['Command']['Action'] == 'ready' for c in run['Commands']) == len(run['Battles']) * stages
        assert all(len(b['Offers']) in (0, 3) for b in run['Battles'])
    rows.append(dict(doctrine=key[0], family=key[1], seed=key[2], before=metrics(x), after=metrics(y)))
report = dict(cases=rows, beforeSha256=hashlib.sha256(a.before.read_bytes()).hexdigest(), afterSha256=hashlib.sha256(a.after.read_bytes()).hexdigest(),
              limitations=['Both policies use actual commands and explicit schedules. Different survival changes exposure; compare common waves as well as totals.',
                           'No-command stages can include saving or inspection. Scripted victories do not measure decision quality, fairness or human pacing.',
                           'Headless ticks do not measure rendered wall time. Native interval categories separately include capture/test delays.'])
a.output.parent.mkdir(parents=True, exist_ok=True)
a.output.write_text(json.dumps(report, indent=2) + '\n')
print(f'PREPARATION_COMPARISON_PASS {len(rows)} matched runs; exact actual barrier counts')
