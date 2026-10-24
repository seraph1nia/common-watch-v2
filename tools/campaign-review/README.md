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

## Final selected-rule receipts

```sh
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review verify-matrix artifacts/refinement-ii/r3/campaign-candidates --shipping
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review marginal-coop artifacts/refinement-ii/r3/marginal-cooperative
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review verify-cooperation artifacts/refinement-ii/r3/marginal-cooperative
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review archive-shipping artifacts/refinement-ii/r3/shipping-baseline
mise exec -- dotnet run --project tools/scenarios --no-build -- consequence-review legacy-selected artifacts/refinement-ii/r3/legacy-selected.json
```

`--shipping` starts fresh default authorities, checks the exact recorded initial
rules/roster and replays all120 selected rows. Every accepted final Ready checks
Core's `BattleDeployment.Preview` against actual profiles, group slots, deployment/
queue lifecycle and entry cells. Food funded/unfunded/participating IDs and paid
amounts must agree. Every immediate reward checks the frozen quote against actual
resources, Keep and individual HP, with unchanged profile/equipment/service/
recovery eligibility. Recorded encounter outcomes check frozen participants,
survivors, Keep damage and clear grants. No parallel simulation is implemented.

The additional36 headless groups use1–6original cities, seeds1/42 and invested,
capped last-city or idle last-city policies. Twelve separate doctrine/marginal
runs cap homes3 and equipment2; they are deliberate legal underinvestment, not
injected damage. Duplicated/correlated policies are not independent human trials.
The final10 trace archives use normal openings and current policies. The20
legacy-policy reports separately rerun the original five-policy baseline/adaptive
comparisons under current rules, not as a claim that the old economy survived.

Only compress finished recordings after all workers stop:

```sh
python3 tools/campaign-review/compress.py artifacts/refinement-ii/r3/marginal-cooperative
```

The helper verifies exact decoded bytes before deleting each uncompressed
original. `compression-manifest.json` retains original and gzip SHA-256/size;
`LegalRecorder.Verify` reads either representation transparently. Summary/replay
JSON stays directly readable. Compression is storage only, not replay migration.

For actual six-client W10 normal-speed observations, use a distinct label:

```sh
COMMON_WATCH_COMBAT_REVIEW_LABEL=refinement-ii/r3/shipping-wave10 mise run capture-combat-cooperation -- --wave 10
```

These concurrent ENet histories can allocate different IDs across resolutions.
Record their actual starting armies/ticks and do not label them matched width
experiments. All clients leave accelerated setup before the measured encounter.


## Evidence isolation and final audit

Final current-revision native destinations can be isolated without changing the
normal task defaults:

```sh
COMMON_WATCH_JOURNEY_OUTPUT="$PWD/artifacts/captures/refinement-ii/r3/shipping-journey" mise run capture-journey
COMMON_WATCH_JOURNEY_NETWORK_OUTPUT="$PWD/artifacts/captures/refinement-ii/r3/shipping-journey-network" mise exec -- bash tools/journey-matrix.sh --cooperative-focus --opening-order normal-wood
COMMON_WATCH_RECOVERY_NETWORK_OUTPUT=artifacts/captures/refinement-ii/r3/shipping-recovery-network mise run test-recovery-network
```

The network opening option is `--opening-order normal-wood`, not
`--normal-opening`. The initial rejected invocation is retained as a failed CLI
attempt; it is not a runtime defect. The corrected matrix completed seven
profiles before an invalid status JSON read stopped the last6p1080profile. The
reader now retries at most5reads/4×10ms and still rejects persistent invalid
JSON. The separately logged6p1080rerun completes that matrix; the earlier
production source is identical. No lower-level filesystem cause is established.
See `docs/refinement-ii/COMPLETION_AUDIT.md` for all ten original issue scopes and
qualification limits. Source archives, copied captures, original failed and final
passing logs, and export hashes are indexed in the local generated
`artifacts/refinement-ii/r3/shipping-manifest.json`. Its CI entry records the exact
final commit/run after completion; the PR exposes the same public CI result.

On this host, final headless editor startup repeated a FreeType empty-font error
in both this repository and an empty project, including fresh editor settings.
The bounded empty-project `--text-driver Dummy` diagnostic passed. An explicit
editor-only override allows the export tooling to run:

```sh
COMMON_WATCH_EXPORT_TEXT_DRIVER=Dummy mise run build-linux
COMMON_WATCH_EXPORT_TEXT_DRIVER=Dummy mise run build-windows
```

Default export commands retain their normal text driver. Import/export subprocesses
are bounded120/360seconds. The override is never passed to exported gameplay or
native graphical checks; actual Linux export smoke must render with its ordinary
text driver. No fonts, packages, game resources or user editor settings change.
This workaround qualifies local tooling and is not a claimed FreeType fix.
