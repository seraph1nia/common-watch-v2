# Legal campaign consequence experiments

Use pinned tools from the repository root. The CLI project has an explicit build;
never rely on an old copied Shared assembly after only building another project.

```sh
mise run build
mise exec -- dotnet build tools/scenarios --no-restore
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review cases artifacts/refinement-ii/r3/decisions
python3 tools/campaign-review/report.py artifacts/refinement-ii/r3/decisions
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review majors artifacts/refinement-ii/r3/major-alpha
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review majors-heavy artifacts/refinement-ii/r3/major-heavy
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review campaign-matrix artifacts/refinement-ii/r3/campaign-matrix
```

Use a fresh output directory when changing policy/rules. The matrix can resume
existing individual JSONs only after independently replaying their exact commands
and final state. A resumed file is historical evidence for its recorded policy,
not proof that a changed policy emits the same commands. The four frontline rows
from the first matrix attempt precede a fix to the unrelated ranged conversion;
they remain valid and are individually replayed. The failed ranged intention was
rejected by Core; the corrected policy explicitly builds the empty trainer before
recruiting. No validation was weakened.

Every command comes from ordinary accepted state. The recorder stores exact
preconditions, sequence, authenticated owner, results, snapshot hashes, per-tick
combat events and positions including statuses. `verify` independently replays a
single recorder directory without executing the policy:

```sh
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review verify artifacts/refinement-ii/r3/decisions/placement-1-protected
```

The full240-run matrix uses six doctrines, five families, seeds1/20261018 plus
held-outs42/98765, and the original boss versus heavyW10 campaign. It stores the
complete initial rules/roster, ordinary commands, exact per-command/final hashes,
resources/research/rewards and W5/10/20 city/army snapshots when reached. Every row
is independently replayed. The normal Wood opening precedes an explicit paid
Barracks→ArcheryRange conversion for the ranged continuation. Grove's constrained
second Metal producer is deliberately retained in this main matrix. No defeat is
removed by silently applying the separate Stone investment choice.

Both experimental formation definitions and the frozen41cc004 single-boss
campaign remain executable independently of any later shipping defaults. They
are valid nonboss waves with ordinary profiles; the single-boss validator is
never bypassed. Power is an index using actual resolved profiles, not a strength
prediction. `budgets` / `budgets-heavy` print exact definitions, profiles, rewards
and power values.

Actual native normal-speed comparisons, recordings, forecast snapshots and three
viewport stills per fight:

```sh
COMMON_WATCH_MAJOR_OUTPUT="$PWD/artifacts/captures/refinement-ii/r3/major-alpha" mise run capture-major-encounters
COMMON_WATCH_MAJOR_OUTPUT="$PWD/artifacts/captures/refinement-ii/r3/major-heavy" mise run capture-major-encounters -- --major-formation heavy
```

Run native tasks sequentially. They own displays, temporary settings and bounded
cleanup, use automated Dummy audio, and install no packages. Do not rebuild the
application while native workers are alive. Set a distinct output directory for
each revision and copy evidence before reusing working directories. Normal1x is
factor0.5. Headless actual presented wall time is null; native reports separate
observed authority resolution, presentation resolution and corpse cleanup, with
PNG/geometry/recording overhead disclosed. No human or physical GPU/controller/
audio qualification is inferred.
