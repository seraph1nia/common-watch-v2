#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Compare actual old/new legal policy reports; no fairness inference from wins."""
import argparse
import collections
import hashlib
import json
from pathlib import Path

RESOURCES = ('Gold', 'Wood', 'Food', 'Stone', 'Metal', 'Cloth')

def movements(commands):
    groups = {}
    for c in commands:
        assert c['Result']['Accepted'], c
        action = c['Command']['Action']
        category = 'ChosenBonus' if action.startswith('reward:') else action
        row = groups.setdefault(category, {r: 0 for r in RESOURCES})
        for r in RESOURCES:
            row[r] += c['After'][r] - c['Before'][r]
    return groups

def metrics(run):
    battles, commands = run['Battles'], run['Commands']
    bonus = [c for c in commands if c['Command']['Action'].startswith('reward:')]
    healing = [c for c in bonus if c['Command']['Action'] in ('reward:F04', 'reward:loot-recovery')]
    return dict(battles=len(battles), result=battles[-1]['Result'], combatTicks=sum(b['CombatTicks'] for b in battles),
                cleanupTicks=sum(b['CleanupTicks'] for b in battles), food=sum(b['FoodPaid'] for b in battles),
                casualties=sum(b['Casualties'] for b in battles), keepDamage=sum(b['KeepDamage'] for b in battles),
                bonusKeepHealing=sum(c['KeepHealing'] for c in bonus), bonusSoldierHealing=sum(c['SoldierHealing'] for c in bonus),
                bonusHealedSoldiers=sum(c['HealedSoldiers'] for c in bonus),
                ordinaryRecovery=sum(c['SoldierHealing'] for c in commands if c['Command']['Action'] == 'ready'),
                recoveryNoOps=sum(c['KeepHealing'] == 0 and c['SoldierHealing'] == 0 for c in healing),
                resourceMovements=movements(commands),
                clearResources={r: sum(b['ClearGrant'][r] for b in battles) for r in RESOURCES},
                clearResearch=sum(b['ClearResearch'] for b in battles),
                choices=dict(collections.Counter(b['Choice'] for b in battles if b.get('Choice'))),
                shortages=len(run['Shortages']),
                perWave=[{k: b[k] for k in ('Wave', 'Result', 'Funded', 'Participating', 'Unfunded', 'Casualties', 'Wounded', 'KeepDamage', 'Technologies')} for b in battles])

def load(path):
    runs = json.loads(path.read_text())
    keyed = {(r['Doctrine'], r['Family'], r['Seed']): r for r in runs}
    assert len(keyed) == len(runs) == 72
    assert len({r['Doctrine'] for r in runs}) == 6
    return keyed

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--before', type=Path, required=True)
    p.add_argument('--after', type=Path, required=True)
    p.add_argument('--output', type=Path, required=True)
    a = p.parse_args()
    before, after = load(a.before), load(a.after)
    assert before.keys() == after.keys()
    rows = []
    for key, old in before.items():
        new = after[key]
        assert old['MatchId'] == new['MatchId'] and old['Starting'] == new['Starting']
        bonus = [c for c in new['Commands'] if c['Command']['Action'].startswith('reward:')]
        offered = [b for b in new['Battles'] if b['Offers']]
        assert len(bonus) == len(offered)
        for battle in new['Battles']:
            assert len(battle['Offers']) in (0, 3) and len(set(battle['Offers'])) == len(battle['Offers'])
            if battle['Offers']:
                assert battle['Choice'] in battle['Offers'] and battle['Result'] == 'RewardDraft'
        for command in bonus:
            assert command['ResearchPointsAfter'] == command['ResearchPointsBefore']
            if command['Command']['Action'] == 'reward:loot-recovery':
                assert command['KeepHealing'] > 0 or command['SoldierHealing'] > 0
            else:
                assert all(command['After'][r] >= command['Before'][r] for r in RESOURCES)
                assert any(command['After'][r] > command['Before'][r] for r in RESOURCES)
        rows.append(dict(doctrine=key[0], family=key[1], seed=key[2], before=metrics(old), after=metrics(new)))
    report = dict(cases=rows, beforeSha256=hashlib.sha256(a.before.read_bytes()).hexdigest(),
                  afterSha256=hashlib.sha256(a.after.read_bytes()).hexdigest(),
                  limitations=['Identical ordinal-choice paid policies; reward effects change subsequent affordable actions, so command streams diverge deliberately.',
                               'Ready resource movement is net production minus final Food. Add recorded battle Food to recover gross production; clear payout and chosen bonuses are separate.',
                               'Shortages are constraints at scripted stopping points, not observed human intent. Headless ticks are not rendered wall time.',
                               'Correlated policy wins/losses quantify changes, not fairness, enjoyment or a population estimate.'])
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(json.dumps(report, indent=2) + '\n')
    print(f'REWARD_ECONOMY_COMPARISON_PASS {len(rows)} matched cases')

if __name__ == '__main__':
    main()
