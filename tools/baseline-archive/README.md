# Original campaign archive

These CLI-only files instrument the frozen `b4fa3f7807fcdf08040e9be2e186833b0504ab01`
policy in a disposable source directory. They add observation calls to a copy of
CampaignReview; original Core and policy source remain intact. No active checkout,
Godot DLL, lock file, simulation rule, resource, unit or snapshot is rewritten.

Extract the pinned source into a fresh directory, preserving existing evidence:

```sh
mkdir -p artifacts/refinement-ii/baseline/trace-source
git archive b4fa3f7807fcdf08040e9be2e186833b0504ab01 \
  Directory.Build.props LICENSE NuGet.Config global.json src/Game.Core \
  src/Game.Shared tools/scenarios tools/nuget-locks \
  | tar -x -C artifacts/refinement-ii/baseline/trace-source
python3 tools/baseline-archive/prepare-archive.py \
  --source artifacts/refinement-ii/baseline/trace-source
timeout --kill-after=5s 1800s mise exec -- dotnet run \
  --project artifacts/refinement-ii/baseline/trace-source/tools/scenarios/Scenarios.csproj \
  -- archive-baseline artifacts/refinement-ii/baseline/trace-archive
```

The preparation tool validates exact original source anchors and refuses the
active checkout. Use a fresh disposable source for each generation. Core's NuGet
lock and the original three-preparation rules remain pinned. The 20 original
family/seed/adaptation runs execute serially, then a no-investment defeat executes.
Each run is replayed by a fresh Match using only recorded authenticated commands
and ticks, with exact snapshot hashes at every command and encounter checkpoint. Every
recorded combat event is compared to independent replay as well.

To verify an existing archive without running its policy:

```sh
timeout --kill-after=5s 1800s mise exec -- dotnet run \
  --project artifacts/refinement-ii/baseline/trace-source/tools/scenarios/Scenarios.csproj \
  -- verify-baseline artifacts/refinement-ii/baseline/trace-archive
```

Each run records `joined.json`, `history.ndjson`, `final.json`, `replay.json`, and
per-encounter before/after manifests, complete monotonically checked combat events,
observed positions/targets/structures and timing. Stocks, research, rewards,
funding and exact command preconditions are retained in real accepted snapshots.
The aggregate campaign report must match the pre-existing original report.

Headless ticks and corpse-cleanup ticks are separate. Nominal viewing time divides
combat ticks by 60 and by the displayed rate factor; at ordinary 1x that factor is
0.5. Core corpse cleanup outside Combat uses rate 1; its ticks and nominal time
are separate. Neither conversion is actual rendered wall time or human pacing
evidence.
Native measurements must identify their actual presentation revision separately.

This is the early baseline work for #25, not tactical-depth evidence or final
selected-economy calibration. Generated archives are bounded local artifacts;
source history, instrumentation, hashes and verification commands supply the
reproduction route. GPL-3.0-only, like the original game.
