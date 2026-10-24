# Matched reward economy review

`mise run scenario -- reward-economy-review OUTPUT.json` runs every doctrine
through its normal paid opening (three waves), and frontline, ranged, mixed,
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
It copies only the same development policy and its CLI dispatch. No simulation,
lock, project, resources, units or snapshots are changed. Compare policy hashes
and exact reports before attributing differences to the new reward rules.
Headless ticks are not rendered wall time. These correlated scripted policies
expose economic changes; wins do not establish fairness or human experience.
