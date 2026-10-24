# Preparation comparison (#22)

The shipping default is three explicit decisions. The bounded two-decision
alternative is development-only: resources 2× then 1×, Research and eligible
storage healing once at each actual Ready. No phantom payment or automatic Ready.

Use the pinned tools from the repository root:

```sh
mise run build
mise exec -- dotnet run --no-build --project tools/scenarios -- preparation-review artifacts/refinement-ii/r5/reproduced
mise exec -- dotnet run --no-build --project tools/scenarios -- preparation-opening-orders artifacts/refinement-ii/r5/reproduced/openings
mise exec -- dotnet run --no-build --project tools/scenarios -- preparation-focused artifacts/refinement-ii/r5/reproduced/focused
mise exec -- dotnet run --no-build --project tools/scenarios -- preparation-food artifacts/refinement-ii/r5/reproduced/focused
mise exec -- dotnet run --no-build --project tools/scenarios -- preparation-campaigns artifacts/refinement-ii/r5/reproduced/campaigns
python3 tools/preparation-review/verify-focused.py artifacts/refinement-ii/r5/reproduced/focused
python3 tools/preparation-review/compare.py --before artifacts/refinement-ii/r5/reproduced/3-stage.json --after artifacts/refinement-ii/r5/reproduced/2-stage.json --output artifacts/refinement-ii/r5/reproduced/comparison.json
mise exec -- bash tools/graphical.sh test-journey --review-preparations 2 --review-opening-order economy-first --review-match-id preparation-solo-fixed --gameplay-captures artifacts/captures/preparation-review/reproduced-solo
```

For cooperative comparisons, `tools/journey-network.py` accepts `--preparations
2|3`, `--opening-order canonical|army-first|economy-first`, `--players 2|4|6`,
`--width` and `--output`. Run it under an owned Xvfb with the same environment and
bounded supervision as `tools/graphical.sh test-journey-network`. Run displays
sequentially. Never rebuild the native DLL while clients are alive.

`stage-baseline.py` counts accepted readiness and authoritative actions. A stage
without a command may include saving, inspection or placement review. `timing.py`
assigns intervals to the previous observed client state, with explicit capture
and deliberate test-delay overrides; it does not measure human time or infer why
a person waited. Cooperative seconds are **summed client intervals**, not session
wall time. Solo native receipts separate scripted decisions, capture and boundary
probes, with synchronous authority and no separate already-ready wait.

Canonical and reordered histories stay separate. The ranged policy owns its
trainer/slot layout; the reordered continuation matrix covers the other four
families, rather than combining incompatible ranged/Barracks layouts. The first
attempt exposed that harness mismatch and was rejected, not counted as a game
loss. The forecast-response matrix separately includes ranged under both schedules.

Reports are development evidence, not player automation, balance guarantees or
human comprehension observations. Original b4fa3f7 and fresh #21 archives remain
separate from every candidate report. See `docs/refinement-ii/22_PREPARATION_RHYTHM.md`.
