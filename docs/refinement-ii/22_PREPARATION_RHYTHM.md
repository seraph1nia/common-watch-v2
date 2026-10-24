# Preparation rhythm — issue #22 experiment design

This design precedes any preparation-rule edit. #21 is locally verified on
`da3a51d`; final CI75 is pending. No stage-count change occurs until that gate.
The fresh immediate-reward three-stage source is preserved as
`artifacts/refinement-ii/r7/three-stage-source.tar` (SHA-256
`3a68f3ad6358d29a8a9fae7efaeae782338693b22ce9e936ce7d3277133959b7`).
The original b4fa3f7 command/event archive remains separate.

## Baselines and evaluated tasks

Original reward matrix: 72 runs / 1,050 battles. Fresh #21 matrix: 72 runs /
1,037 battles, 3,111 accepted preparation barriers, 1,068 stages with no recorded
non-readiness command. Different survival changes exposure. These counts do not
say a player had no meaningful choice: deliberate saving and forecast/ally
inspection need no authoritative command. The stage recorder preserves actual
actions, resource changes, placement commands, Ready stock movements, Research
points and ordinary healing separately. It never infers human time.

Before comparing outcomes, evaluate:

- Every doctrine-specific normal paid opening, seeds 1 and 20261018, plus explicit
  Farm/Metal/Barracks, army-first and economy-first orders. Preserve failed paths.
- Frontline/ranged/mixed/tower/research campaigns with reference and forecast
  response policies; no changed enemy profiles or composition.
- Producer construction/upgrades between decisions, Gold recovery at zero Wood,
  Research purchases/progress, eligible stored wounds, partial Food funding and
  final-window recruitment. Record actual prebattle spending opportunities.
- Native complete three-wave solo and 2/4/6-client journeys under both schedules,
  using the same Controls and input paths. Record readiness/command counts and
  waiting by observed cause, including deliberate script/capture delays.
- Retry, unready, pause, disconnect, elimination and reconnect at both nonfinal
  and final boundaries; no duplicate production, healing, Research or Food.

Report no-command counts alongside purchases, placements, resources, field
participation, casualties, Keep damage, Research timing and veteran recovery.
Scripted no-purchase or already-ready time is not human idle time. Losses and
shortages remain visible; an optimized win count cannot select the schedule.

## One bounded alternative

The three-stage baseline has three Ready events. Each produces ordinary resources,
advances Research once and heals eligible stored members once. The third payment
immediately funds Food and starts battle; its income cannot be spent first.
After a nonfinal clear, immediate loot applies before next-wave planning.

Prototype two actual Ready decisions. Frontload resource delivery, while Research
and ordinary healing each occur once per actual Ready. No entry payment, hidden
turn, automatic Ready, spending heuristic or forced countdown.

| Event | Three stages | Two-stage candidate |
| --- | --- | --- |
| First Ready resources | 1× current full production | 2× current full production |
| First Ready Research / eligible storage heal | once / once | once / once |
| Middle Ready | 1× production, once Research/heal; another purchase window | absent |
| Final Ready | 1× production, once Research/heal, Food, combat | same order, actual second decision |
| Unchanged city's resource total | 3× | 3× |
| Research/healing opportunities per wave | 3 | 2 |
| Twenty-wave readiness barriers | 60 | 40 |
| Nonfinal default reward drafts | 19 | 19 |

Resource scaling includes Base Gold, every producer's current output, purchased
Worldtree production and once-per-city doctrine production. Checked multiplication
happens before any commit. It does not repeat Research, healing, merchant sale
bonuses, turn counters or lifecycle effects. ProductionCount counts actual Ready
payments (two), not three phantom batches.

A producer built before the first decision earns three base outputs under both
schedules. A producer built after first Ready earns only the final output in the
candidate; in the baseline it can earn a middle output that is spendable before
battle and another final output. Frontloading existing producers therefore does
not preserve reinvestment chains. A new/upgrade producer's exact delivered output
must be tabulated, per resource. Reduced Research and healing are explicit
opportunity-cost changes, not silently compensated.

All prices, starting stock, immediate loot, enemy/player profiles and encounter
composition remain fixed during this comparison. Any later economy adjustment
belongs to #23 or final #25 and must not be folded into this experiment.

## Authority, presentation and compatibility

Use one Core preparation-schedule interface for stage count and resource delivery.
Snapshot income must quote the *next actual delivery*. Final Food forecasts include
only its final delivery. UI tracks, phase labels, guidance, legal checkpoints,
network policies and tests must read the authoritative schedule; audit every `/3`,
Turn==3 and three-iteration loop. No client guesses the host's timing.

Before Core implementation, declare battle-flow version 4 / protocol 19 and an
updated rules fingerprint containing the schedule. Keep combat8/reward1. Historical
three-stage protocol18 sessions are explicitly rejected, not migrated. Supported
experiment configurations are 2/3; ordinary play retains baseline3 until the
measured decision selects a shipping default. A development-only launch option
may choose the isolated alternative for paired native checks; every peer receives
the same frozen rules and uses the same UI.

Select two only if normal paid paths remain viable, changed economic/research/
recovery decisions are understood and the removed barrier does not hide a new
mandatory workaround. If three is retained, show the runnable lost-decision cases
and ship a concrete interaction reduction. Record the exact decision and its
limits in parent #15 before closure; do not silently call an untested assertion
complete. No Core implementation or product selection is claimed by this design.
