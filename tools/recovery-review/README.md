# Finite squad recovery review

Run from the repository root with pinned mise tools. The CLI project is built
explicitly; a solution build alone does not refresh its copied Shared assembly.

```sh
mise run build
mise exec -- dotnet restore tools/scenarios --locked-mode
mise exec -- dotnet build tools/scenarios --no-restore
mise exec -- dotnet run --project tools/scenarios --no-build -- squad-recovery-review artifacts/refinement-ii/r8/comparison-selected.json
mise exec -- dotnet run --project tools/scenarios --no-build -- recovery-campaigns artifacts/refinement-ii/r8/campaigns-selected.json
mise run capture-recovery
COMMON_WATCH_RECOVERY_OUTPUT="$PWD/artifacts/captures/recovery/plain" mise run capture-recovery -- --world-art debug --hud-art plain --view topdown
COMMON_WATCH_RECOVERY_OUTPUT="$PWD/artifacts/captures/recovery/missing" mise run capture-recovery -- --assets-root /tmp/common-watch-recovery-absent-art
mise run test-recovery-network
```

Use an actually absent path for the last art profile. Run graphical tasks
sequentially; each owns its virtual display and private temporary settings.
Never rebuild the Godot application while a native test is alive. These tasks
use Dummy audio only for automation and install no system packages.

The first JSON contains ten legal checkpoint comparisons, exact individual/
sequence snapshot equivalence, actual paid replacements, immediate reward then
ordinary healing, and an unfunded stored-member consequence. Checkpoints are
created by ordinary commands, never injected state. Payments-to-full assume the
current hall rate and continued eligibility; future Food is not guaranteed.

The second JSON contains 24 complete campaigns: six doctrines, seeds1/20261018,
individual and finite-sequence implementations. Each pair uses the same match
identity and policy. Its digest includes the full detached snapshot after every
accepted command and completed battle. Twelve digest pairs must match. This is
an equivalence experiment, not evidence that recovery is universally optimal.

Native output records actual member identities, no immediate heal, paid Food,
partial capacity, keyboard commit, synthetic controller cancel, explicit return,
healthy-squad absence and action counts. The two-process test holds real host
authority, cancels with one command pending, checks accepted-prefix retry,
rejoins with private credentials and checks the next real production heal.
Credentials remain in disposable private directories and never in the report.

See [the decision and qualifications](../../docs/refinement-ii/24_SQUAD_RECOVERY.md).
Generated source archives, raw receipts, captures and SHA-256 manifest remain
under gitignored `artifacts/refinement-ii/r8` and `artifacts/captures/recovery`.
