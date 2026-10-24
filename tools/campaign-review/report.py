#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-only
"""Validate matched decision receipts and print a compact measured table."""
import json
from pathlib import Path
import sys
root=Path(sys.argv[1]);runs=json.loads((root/'summary.json').read_text())
assert len(runs)==44 and all('error' not in r for r in runs)
resources=('Gold','Wood','Food','Stone','Metal','Cloth')
def manifest(units):
 return sorted(json.dumps({k:u.get(k) for k in ('Type','Level','Rank','IsBoss','Origin','Destination','Profile')},sort_keys=True) for u in units)
def identity(units):
 return sorted(json.dumps({k:u.get(k) for k in ('Id','Type','Level','Health','Profile','Capabilities','Experience','RecoveryEligible')},sort_keys=True) for u in units)
for scenario in {r['scenario'] for r in runs}:
 for seed in {r['seed'] for r in runs}:
  pair=[r for r in runs if r['scenario']==scenario and r['seed']==seed]
  assert all(r['common']==pair[0]['common'] for r in pair)
  if scenario=='placement':
   assert identity(pair[0]['chosen']['Players'][0]['Soldiers'])==identity(pair[1]['chosen']['Players'][0]['Soldiers'])
   assert pair[0]['chosen']['Players'][0]['Resources']==pair[1]['chosen']['Players'][0]['Resources']
for r in runs:
 directory=root/f"{r['scenario']}-{r['seed']}-{r['variant']}"
 assert (directory/'replay.json').is_file()
 for b in r['battles']:
  n=b['Wave'];forecast=json.loads((directory/f'forecast-{n}.json').read_text());before=json.loads((directory/f'wave-{n:02}/before.json').read_text());after=json.loads((directory/f'wave-{n:02}/after.json').read_text())
  assert manifest(forecast['NextWave'])==manifest(before['Enemies'])
  f=forecast['Players'][0]['FoodForecast'];paid=before['Players'][0]['LastUpkeep'];assert f['Participating']==paid['Participating'] and f['Paid']==paid['Paid']
  receipt=after['Players'][0]['LastBattleLoss'];assert receipt['Wave']==n and receipt['Participants']==b['Participants'] and receipt['Survivors']==b['Participants']-b['Lost']
  grant=after['Players'][0].get('LastReward')
  if grant and grant['Wave']==n:
   assert all(after['Players'][0]['Resources'][k]==before['Players'][0]['Resources'][k]+grant['Amount'][k] for k in resources)
print('All44 matched legal cases: exact common checkpoints; same placement army; independent replay; target forecasts/upkeep/frozen outcome/clear grants agree.\n')
print('| Case / seed | Variant | Immediate spent (negative = receipt) | Losses by wave | Combat ticks | Final field |')
print('| --- | --- | --- | --- | --- | --- |')
for r in runs:
 a=r['common']['Players'][0];b=r['chosen']['Players'][0];spent=', '.join(f'{a["Resources"][k]-b["Resources"][k]} {k}' for k in resources if a['Resources'][k]!=b['Resources'][k]) or '0'
 field=sum(not u['Assignment']['Stored'] for u in r['final']['Players'][0]['Soldiers'])
 print(f'| {r["scenario"]}/{r["seed"]} | {r["variant"]} | {spent} | '+','.join(str(b['Lost']) for b in r['battles'])+' | '+','.join(str(b['CombatTicks']) for b in r['battles'])+f' | {field} |')
print('\nHeadless: actual presented wall time is null; ticks are not viewing seconds. Same seed does not isolate entity/action-order RNG changes. No human claims.')
