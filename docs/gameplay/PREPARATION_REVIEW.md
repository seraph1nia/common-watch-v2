# Preparation review — issue #3

The first pass retains all three authoritative settlements and explains each
transition in the production HUD. The inspected city's threat, next production,
participating army, stored soldiers, field sit-outs and Food payment share one
overview. Early forecasts explicitly use stock now; the final one includes the
last income payment. The overview yields to selected-plot management so all nine
clearings remain accessible. Ready/Unready always acts on the authenticated city.

## Local pacing observations

The accepted verification method for this roadmap is local tests and actual
screenshots. These are automated, accelerated Warden policy runs on Linux
Xvfb/Mesa, not human reaction times or evidence of balance/fun. The solo route
opens the production Single Player and doctrine controls. Cooperative observations
come from two real Godot processes and the existing legal seven-wave replay.
Both use legal commands and normal authoritative production/economy rules.

Non-readiness decisions observed over the first three waves:

| Wave / preparation | Solo (research policy) | Host (research) | Guest (mixed) |
| --- | ---: | ---: | ---: |
| 1 / 1 | 4 | 4 | 4 |
| 1 / 2 | 0 | 0 | 0 |
| 1 / 3 | 8 | 8 | 8 |
| 2 / 1 | 6 | 7 | 6 |
| 2 / 2 | 1 | 1 | 1 |
| 2 / 3 | 4 | 3 | 3 |
| 3 / 1 | 1 | 3 | 2 |
| 3 / 2 | 1 | 2 | 1 |
| 3 / 3 | 0 | 0 | 0 |

All nine stages per participant were observed from their boundaries, with zero
unobserved command sequences. Total sampled preparation/waiting seconds were
**1.724 / 1.033** solo, **7.673 / 3.597** host and **7.648 / 1.347** guest.
Cooperative stage 1 deliberately exercises Ready/Unready, pause, a held authority,
pending acknowledgment and captures at two sizes. Those delays are test inputs,
not normal cooperative waiting estimates. Ready waiting excludes sampled paused
and disconnected time. Frame sampling and accelerated playback limit precision.

Two of nine stages had no purchase/army/research decisions in these policies.
Wave 1 preparation 2 still earns resources used by the eight stage-3 actions.
Wave 3 preparation 3 still collects final production and determines Food payment
and battle participation. Seven other stages contain choices. This evidence does
not justify replacing the rhythm with one triple-income payment: purchases affect
later income, each boundary advances research/eligible recovery, and final income
leads directly into combat. No stage-count or economy adjustment is made.

Completion receipts: `artifacts/observations/preparation/issue-03-completion/solo-player-1.json`
and `forest-player-{1,2}.json` in that directory;
`artifacts/test-logs/roadmap-03-{solo,coop}.log`.
The earlier native captures remain under `artifacts/captures/gameplay/forest/`,
its `1920/` subdirectory and `network-forest/`.

## Reproduce or observe an interactive session

```sh
COMMON_WATCH_PREPARATION_SOLO=1 COMMON_WATCH_GAMEPLAY_ART=illustrated mise exec -- bash tools/graphical.sh test-gameplay-network
COMMON_WATCH_GAMEPLAY_ART=illustrated mise exec -- bash tools/graphical.sh test-gameplay-network
mise run play -- --preparation-observations artifacts/observations/preparation/interactive.json
```

Recording is opt-in, local and bounded to the last 96 completed stages plus the
current stage. It stores counters and durations, never commands, snapshots,
credentials or names. It marks sessions first observed mid-preparation as partial
and reports skipped command sequences rather than claiming full coverage. Retries
with the same sequence do not add actions. Replacing/leaving a session closes the
unfinished observation without marking its boundary complete.

Build, 46 Core tests, 88 Shared tests, three focused observation contracts and the
native solo/cooperative workers passed. Existing 720p/1080p preparation, shortage,
waiting, allied, pending/rejected/disconnected captures, three session tests, 14
art captures and the five-size nine-clearing picking/purchase suite verify the
presentation. Core/Shared gameplay and dependency/art/music pins remain unchanged.
Further human reviews can refine pacing hypotheses without blocking this verified
local implementation. Live Steam, physical hardware and native Windows/macOS are
separate unexecuted qualifications; the known native .NET/JIT risk remains.
