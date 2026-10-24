#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Summarize recorded authoritative commands, not inferred human decisions."""
import argparse
import json
from pathlib import Path

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--input', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
runs = json.loads(a.input.read_text())
rows = []
for run in runs:
    wave, stage = 1, 1
    actions = []
    for item in run['Commands']:
        command = item['Command']
        if command['ExpectedPhase'] not in ('Building', 'Preparation'):
            continue
        assert item['Result']['Accepted']
        if command['Action'] != 'ready':
            actions.append(item)
            continue
        rows.append(dict(doctrine=run['Doctrine'], family=run['Family'], seed=run['Seed'], wave=wave, stage=stage,
                         actions=[c['Command']['Action'] for c in actions], nonReadinessCommands=len(actions),
                         resourceChangingCommands=sum(c['Before'] != c['After'] for c in actions),
                         placementCommands=sum(c['Command']['Action'] in ('deploy', 'deploy-squad', 'move', 'regroup', 'store', 'send') for c in actions),
                         noCommandStage=not actions, readyBefore=item['Before'], readyAfter=item['After'],
                         researchPointsBefore=item['ResearchPointsBefore'], researchPointsAfter=item['ResearchPointsAfter'],
                         ordinaryHealing=item['SoldierHealing']))
        actions = []
        if command['ExpectedPhase'] == 'Preparation':
            wave, stage = wave + 1, 1
        else:
            stage += 1
    assert not actions, 'Only complete accepted preparation boundaries are compared.'
report = dict(runs=len(runs), barriers=len(rows), noCommandStages=sum(r['noCommandStage'] for r in rows),
              stages=rows, limitations=['A stage with no recorded command can still contain deliberate saving, placement review or inspection.',
                                       'Resource-changing counts include actual paid actions/trades. UI inspection is absent from command history.',
                                       'Headless commands do not measure human time, pacing, comprehension or synchronization waiting.'])
a.output.parent.mkdir(parents=True, exist_ok=True)
a.output.write_text(json.dumps(report, indent=2) + '\n')
print(f'PREPARATION_BASELINE_PASS {len(rows)} accepted barriers; {report["noCommandStages"]} no-command stages')
