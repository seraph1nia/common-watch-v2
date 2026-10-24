# Ordinary opening comparison (#23)

The selected three-stage rules and all prices/allocations remain unchanged.
Normal policies refuse Gold recovery; the separate overspending task deliberately
uses its real four-Gold safety valve after a rejected normal purchase.

```sh
mise run build
mise exec -- dotnet run --no-build --project tools/scenarios -- normal-openings artifacts/refinement-ii/r9/reproduced/openings
mise exec -- dotnet run --no-build --project tools/scenarios -- opening-tasks artifacts/refinement-ii/r9/reproduced/tasks.json
mise exec -- dotnet run --no-build --project tools/scenarios -- normal-opening-horizons artifacts/refinement-ii/r9/reproduced/horizons
python3 tools/preparation-review/compare.py --before artifacts/refinement-ii/r9/reproduced/horizons/reference.json --after artifacts/refinement-ii/r9/reproduced/horizons/normal.json --before-stages 3 --after-stages 3 --output artifacts/refinement-ii/r9/reproduced/horizons/comparison.json
mise run capture-opening
mise exec -- bash tools/graphical.sh test-journey --review-match-id normal-opening-solo --gameplay-captures /tmp/common-watch-opening-solo
mise exec -- bash tools/graphical.sh test-journey-network --players 2 --width 1280 --opening-order normal-wood
```

Native checks own bounded displays/private settings and use pointer/wheel inputs,
including actual doctrine selection, construction, recruitment and Ready. Capture
opening runs six doctrines through three waves at720/1080. `--opening-order`
also accepts canonical, army-first, economy-first, normal-frontline and normal-role;
the first three remain historical comparison policies. Do not rebuild native DLLs
while clients are alive. Run display tasks sequentially.

The ordinary Wood-first route clears three waves for all six doctrines/two seeds.
Merchant's frontline-first alternative fails wave one; retain it. Advanced early
Mage/archer routes are distinct paid histories, not free specialists or the basic
recommended sequence. The archer route sells temporary buildings and loses income.

Full comparisons cover four compatible continuation families: frontline, mixed,
towers and research. Ranged uses a different trainer/slot layout; #22 already
preserves its reference/response comparison. Do not splice a Barracks opening
into that fixed ranged layout and call an invalid trainer command a game loss.
The temporary Merchant second Lumbermill is lawfully sold once the current
Stonecutter catalog price is affordable. Original and first-attempt reports remain
separate; deterministic default receipts are not human comprehension evidence.

Reports record each accepted command's stocks, research and healing; five task
traces also record rejected intentions and unchanged state. W5/10/20 snapshots
are actual city/army checkpoints before personal choice, absent when not reached.
Three Grovekeeper continuation losses after former Victories remain visible for
#25; the policy is not silently optimized until all cases win. Different entity
creation/action order can change RNG consumption despite matched seeds.

The frozen original reward comparison has a separate pinned observer and83-file
source guard: see `../reward-economy/README.md`. It reproduces the historical
72-run report rather than copying current APIs into original Core.
