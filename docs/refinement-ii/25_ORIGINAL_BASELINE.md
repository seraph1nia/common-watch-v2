# Original campaign baseline — early work for #25

This freezes the original rules before #21–#24. It establishes reproduction,
not tactical depth, fairness or final campaign calibration. The original commit
is `b4fa3f7807fcdf08040e9be2e186833b0504ab01`; later presentation work has not
changed its Core rules.

| Retained evidence | Result |
| --- | --- |
| Five policies × two seeds × baseline/adaptive | 20 campaigns, 400 battles; every original result reproduced |
| No investment, seed 1 | Four commands: Start and three Ready; actual Defeat at tick 1003 |
| Exact accepted command histories | 3,470 commands, including full preconditions and authenticated city |
| Snapshot checkpoints | 844 exact hashes after commands and encounter boundaries |
| Combat event traces | 41,512 events independently reproduced, including positions/targets/impacts |
| Frozen source integrity | All 83 original Core/Shared source/project/lock files match their Git blobs |
| Corruption check | A copied event with changed damage is rejected at sequence 1 |

The original aggregate report remains byte-identical, SHA-256
`6ef56ec5aa145a892fdd69e4c84c87ea2add5d57ed5606fe22d2b401f3505414`.
Its optimized victories and zero Keep damage are correlated policy results;
they do not establish that all players, doctrines or imperfect choices win.
These policies use the default Warden. All-doctrine comparisons remain part of
the reward/opening work and final calibration.

The generated local evidence is under `artifacts/refinement-ii/baseline/`:
`trace-archive/`, its approximately 16 MB compressed copy, `manifest.json`,
`trace-generation.log`, `trace-replay.log`, `trace-tamper.log` and
`native-timing-supplement.json`. Every encounter retains full before/after
snapshots, event and observed-position traces, and separate combat/cleanup
ticks. The snapshots retain actual stocks, equipment, research, rewards,
funding and accepted outcomes. Per-file hashes accompany the archive.

The [reproduction instructions](../../tools/baseline-archive/README.md) extract
the pinned legacy source into a disposable directory. The tool instruments a
mechanical copy of the original campaign policy and adds an independent replay
runner; original Core and policy files remain untouched. Active-checkout and
changed-source guards prevent accidental instrumentation of shipping code.
Locked dependency restoration uses the original project files and locks.
Nothing grants resources, changes health or injects authoritative snapshots.

Replay creates a fresh Match and uses the recorded commands and ticks without
calling the original policy. It compares every command result, every recorded
snapshot hash and every combat event. The original no-investment loss has the
same reproduction route. This remains available after the shipping rules change.

Headless timing declares actual rendered wall time unavailable. Combat nominal
viewing time uses the selected rate: ordinary displayed 1× has a 0.5 factor.
Corpse-cleanup ticks are separate; tick/60 is not the complete ordinary viewing
time. Eight representative native before-framing recordings supply separate
actual wall observations at both sizes, including early, mixed, W10 and W20
encounters. Their presentation baseline is `583ec3a` plus capture probes, and
all 47 original Core file hashes match. They are separate legal scenarios,
not rendered versions of the 400 headless battles. PNG/recording overhead and
the absence of independently measured cleanup wall time are retained explicitly.

Still required for #25: controlled paid decision comparisons, at least three
repeatable trade-offs, an executable elite-formation alternative, held-out seeds,
and final all-doctrine/uneven-group campaigns under the selected reward,
rhythm, opening and recovery rules. Original optimized wins do not replace that
work. No human comprehension/enjoyment, physical hardware, other-platform or
live Steam qualification is inferred from these records.
