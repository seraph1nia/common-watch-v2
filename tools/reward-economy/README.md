# Matched reward economy review

`mise run scenario -- reward-economy-review OUTPUT.json` runs every doctrine
through its fixed reference opening (three waves), and frontline, ranged, mixed,
tower and research policies (up to twenty), each with seeds 1 and 20261018.
The fixed ordinal offer-choice policy is deliberately not an optimizer.
Commands retain exact before/after stocks and Research points, observed Keep and
soldier healing, costs/trades/production/bonus as separate action categories.
Battle records retain base payout separately, funded/participating/unfunded
members, casualties, wounds, Keep damage, choices and technologies. Shortages
are affordable-recruit constraints at actual policy stopping points; they are
not measurements of player frustration or intended purchases.

For the original comparison, extract b4fa3f7807fcdf08040e9be2e186833b0504ab01
with the same file selection as [the original archive](../baseline-archive/README.md)
into a fresh `artifacts/refinement-ii/r7/original-source` directory. Then run:

```sh
python3 tools/reward-economy/prepare-original.py \
  --source artifacts/refinement-ii/r7/original-source
timeout --kill-after=5s 1800s mise exec -- dotnet run \
  --project artifacts/refinement-ii/r7/original-source/tools/scenarios/Scenarios.csproj \
  -- reward-economy-review artifacts/refinement-ii/r7/economy-original-reproduced.json
```

The wrapper verifies all original Core/Shared files and refuses the active tree.
It copies only the matched #21 development policy and reward CLI dispatch from
immutable commit da3a51d49e3cafb97b67aaf7f7cf6cb3a1f3f04a. Current development
probes now use later schedule/opening APIs, so they are deliberately not copied
into the original simulation. This preserves the original comparison rather than
claiming that an evolved policy still describes the old one. No simulation,
lock, project, resources, units or snapshots are changed. Compare policy hashes
and exact reports before attributing differences to the new reward rules.
Headless ticks are not rendered wall time. These correlated scripted policies
expose economic changes; wins do not establish fairness or human experience.

Compare two reports with:

```sh
python3 tools/reward-economy/compare.py --before BEFORE.json --after AFTER.json \
  --output COMPARISON.json
```

The original 72-run report contains 1,050 actual battles. Ten full-campaign
policies lose early: Merchant's four nonranged families at W1 and Grovekeeper's
ranged family at W3, both seeds. These outcomes remain in the matched report;
they are evidence for opening work rather than hidden or replaced with grants.
