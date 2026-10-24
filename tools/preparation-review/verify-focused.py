#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Independently check actual focused preparation receipts, without replaying policy."""
import argparse
import hashlib
import json
from pathlib import Path

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('directory', type=Path)
a = parser.parse_args()
report = {}
for stages in (3, 2):
    filename = a.directory / f'focused-{stages}.json'
    counts = dict(payments=0, storedObservations=0, woundedObservations=0, healingHundredths=0, researchProgress=0)
    for run in json.loads(filename.read_text()):
        towers = {}
        for entry in run['Commands']:
            command = entry['Command']
            assert entry['Result']['Accepted']
            action, slot = command['Action'], command['Slot']
            if action == 'build' and command['Building'] == 'ResearchTower':
                towers[slot] = 1
            elif action == 'upgrade' and slot in towers:
                towers[slot] += 1
            elif action == 'sell':
                towers.pop(slot, None)
            payment = entry.get('Preparation')
            if payment is None:
                continue
            assert action == 'ready'
            assert payment['ProductionsAfter'] == payment['ProductionsBefore'] + 1
            assert payment['ResourceMultiplier'] == (2 if stages == 2 and payment['Turn'] == 1 else 1)
            assert 1 <= payment['Turn'] <= stages
            assert payment['FoodPaid'] == 0 or payment['Turn'] == stages
            for resource in ('Gold', 'Wood', 'Food', 'Stone', 'Metal', 'Cloth'):
                before = entry['Before'][resource]
                assert entry['After'][resource] == before + payment['Quote'][resource] - (payment['FoodPaid'] if resource == 'Food' else 0)
            points = lambda value: value['Points'] * 3 + value['Progress']
            progress = points(payment['ResearchAfter']) - points(payment['ResearchBefore'])
            assert progress == sum(towers.values())
            counts['researchProgress'] += progress
            actual_heal = 0
            for member in payment['Stored']:
                amount = (member['Maximum'] * member['HealingPercent'] + 99) // 100 if member['Eligible'] else 0
                assert member['After'] == min(member['Maximum'], member['Before'] + amount)
                actual_heal += member['After'] - member['Before']
                counts['storedObservations'] += 1
                counts['woundedObservations'] += member['Before'] < member['Maximum']
            assert actual_heal == entry['SoldierHealing']
            counts['healingHundredths'] += actual_heal
            counts['payments'] += 1
    counts['sha256'] = hashlib.sha256(filename.read_bytes()).hexdigest()
    report[str(stages)] = counts
output = a.directory / 'verified-payments.json'
output.write_text(json.dumps(report, indent=2) + '\n')
print('PREPARATION_PAYMENTS_PASS', json.dumps(report))
