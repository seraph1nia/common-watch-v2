# Preparation rhythm — issue #22 experiment design

This design precedes any preparation-rule edit. #21 is locally verified on
`da3a51d`; final CI75 passed, including both exports. The baseline source remains unchanged while an
isolated experimental checkout may run headless candidate comparisons. No
shipping-default change occurs before that gate and the measured decision.
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
complete. That preimplementation declaration is preserved here; the implemented selection follows below.

## Initial executable results (selection pending)

The isolated experiment builds without warnings and passes Core68 / Shared147.
The three-stage 72-run report is byte-identical to the fresh #21 report. Canonical
two-stage policy runs reach1,021battles/48full Victories versus1,037/49. Total
accepted barriers are2,042 versus3,111; no-command stages350 versus1,068. These
totals have different survival exposure. A surviving full campaign has40actual
Ready boundaries rather than60, with19nonfinal reward drafts.

Canonical Grovekeeper first-wave participation drops4→2, both seeds, and both
Keeps fall. The second Metal Mine was bought after first Ready; its middle
payment formerly funded recruits before the final commitment. In the candidate,
that mine's first income arrives only when battle starts. This is a lost paid
reinvestment chain, not an empty third button. Exact accepted commands and stock
movements are retained in r5/grovekeeper-reinvestment.json. Huntsman ranged seed1
also loses atW8; compare common-wave participants/casualties instead of only wins.

Three explicit normal-payment opening orders were tested under both schedules,
six doctrines and two seeds (72opening runs). Canonical and army-first reach
three waves12/12 with three stages and10/12 with two. Economy-first reaches12/12
under both. Grovekeeper invests its initial Wood in the second Metal Mine before
first Ready. Other doctrines use their stated ordinary order; an unaffordable
optional Arcanum does not block necessary recruitment in the alternate policy.
No new resources, prices, enemy profiles or hidden payment were supplied. These
recoverable paths do not erase the original failed canonical cases.

## Selected behavior

Retain **three actual preparation decisions** in ordinary play. The tested two-stage
alternative removes twenty barriers from a surviving twenty-wave campaign, but
also removes a spendable reinvestment window and one Research/recovery event per
wave. Recovering its Grovekeeper opening requires moving the second mine before
the first payment. This is feasible, but more order-sensitive; it is not evidence
that the lost window was empty. Research and funded recovery need further changes
in #24/#25 before those opportunities could be exchanged confidently. No price,
starting-stock, enemy, combat profile or research threshold is changed here.

Ship **Y to Ready / Unready** through the same authenticated command path. It is
shown on the existing button, requires a fresh non-repeat keypress, and refuses
pending, disconnected, paused, eliminated, nonplanning and covered/modal contexts.
A player can finish a planning decision without moving the pointer back to Ready.
The three confirmations remain deliberate; saving, ally inspection and placement
never auto-commit. In the native three-wave task, three of nine Ready pointer
activations are replaced by keys; all nine could use the shortcut in play. This
is a concrete interaction reduction, not a measured reduction in human time.

The alternative remains runnable behind explicit development launch options,
without a new player-facing setting. Protocol19 / battleflow4 / combat8 / reward1
and a stage-count fingerprint prevent peers from assuming different timing. Old
protocol18 sessions are rejected; no resume migration. Session generations,
private credentials, command expectations and RPC ownership remain intact.

## Actual production and investment timing

Each row gives marginal ordinary output of one producer. All six producer build
and upgrade cases execute paid commands from normal Warden stock in Core tests.
Doctrine bonuses apply once per city with a qualifying producer; Worldtree output
is included before the delivery multiplier. No source of output is applied twice.

| Resource / producer | L1 / L2 output | Built before first Ready, either schedule | Built after first: three stages | Built after first: two stages |
| --- | ---: | ---: | ---: | ---: |
| Food / Farm | 5 / 8 | 15 / 24 | 10 / 16, half spendable before battle | 5 / 8, all at battle commit |
| Gold / Mine | 1 / 2 | 3 / 6 | 2 / 4, half spendable | 1 / 2, all at commit |
| Wood / Lumbermill | 1 / 2 | 3 / 6 | 2 / 4, half spendable | 1 / 2, all at commit |
| Stone / Stonecutter | 1 / 2 | 3 / 6 | 2 / 4, half spendable | 1 / 2, all at commit |
| Metal / Metal Mine | 4 / 8 | 12 / 24 | 8 / 16, half spendable | 4 / 8, all at commit |
| Cloth / Weaver | 4 / 8 | 12 / 24 | 8 / 16, half spendable | 4 / 8, all at commit |

For an upgrade after first Ready, replace output with its L2−L1 increment in the
last two columns. Base Gold remains 6 per complete unchanged-city wave. Grovekeeper
adds 3 Wood / 3 Food per wave with any Lumbermill / Farm under either schedule,
but a qualifying producer first added after first Ready gets two versus one bonus
payments. A producer added in the final editable window receives one payment in
both schedules; its stock can fund Food but cannot purchase recruits before combat.

For every wave, gross fixed-city resource output is 3× current base output. New
immediate rewards are separate after-clear grants: W1–6 multiplier1, W7–13
multiplier2, W14+ multiplier3; they apply after choice before the next planning
window, never as a preparation payment. Exact per-wave stock/command receipts
are in the canonical and reordered reports, including earned-surplus differences.

The actual sequence is: edit → all eligible connected cities Ready → validate all
resource/Research/recovery/capacity plans → pay resources, one Research event,
one eligible stored heal → reopen editing, or on final Ready pay Food and enter
combat → resolve clear/base payout → choose and immediately apply personal loot.
No recruitment window exists between final production and combat. A disconnected
city cannot cast a readiness vote; paused production cannot settle.

## Focused and campaign comparisons

In addition to the canonical72 and opening-order72 runs:

- Forecast-response policies: 20 full campaigns per schedule, all20 Victories,
  400 battles and10 actual forecast-response actions each. Includes ranged,
  frontline, mixed, towers and research; same forecasts are checked against spawned
  manifests and accepted Food. This does not establish universal difficulty.
- Economy-first first-wave continuation: all six doctrines × four compatible
  families × two seeds =48 full campaigns per schedule,46 Victories each.
  Ranged owns a different trainer/slot layout and is compared separately above;
  an initial incompatible-policy harness attempt was rejected and retained in
  `/tmp/common-watch-r5-campaigns.log`, not counted as a game loss.
- Reserve/research/tower focused matrix: six full campaigns per schedule,
  360 versus240 actual preparation payments. Every recorded payment was independently
  checked against stocks, actual final Food, one production count, Research and
  each stored member's eligibility/capped heal. No reward heal is counted as ordinary.
- Reserve ordinary healing totals are2820.95 versus3241.43 HP. These have different
  wounds/storage exposure: the larger candidate total does **not** mean a higher
  heal per event. Exact member histories and eligibility are retained.
- Research purchases in both seeds: Foundation W4/P1 under both; Guardian W7/P1
  versus W7/P2; Guardian Mastery W10/P1 versus W11/P1. Reduced progress delays a
  concrete later purchase despite unchanged resource totals.
- Legal partial-Food checkpoint W8:16 funded /2 unfunded,16 Food under both.
  The actual final payment and participants exactly match the displayed forecast.
  Canonical matrices separately include56 Gold-recovery constructions each and
  475 versus661 recruitments in the final editable window; those are deliberate
  paid actions, not use of final-commit income.

Canonical differences remain visible: the two Grovekeeper first-wave defeats
and Huntsman ranged W8 defeat are not overwritten by improved opening orders.
Raw losses, surplus, shortages, funded participants and purchases are compared
at common waves as well as at terminal outcomes. No automatic counter or claim
of human confusion is inferred from a script.

## Native observations

Complete paired economy-first three-wave loops passed under both schedules in
solo720/1080 and actual2/4/6-client720 runs. Canonical two-stage2p also passed.
Same Controls/input paths exercise unready, pause, delayed ACK, reward receipt and
ready/draft reconnect. The final solo checks additionally use native Y, refuse it
while paused and ensure key repeat cannot pay another preparation.

| Clients | Already-ready client seconds, three → two | Available decision-window client seconds, three → two |
| --- | ---: | ---: |
| 2 | 1.97 → 1.64 | 13.49 → 9.95 |
| 4 | 15.94 → 12.35 | 72.85 → 44.22 |
| 6 | 42.23 → 36.59 | 196.10 → 115.38 |

These are summed **scripted inter-observation client intervals**, not session
wall time, human waiting or a measured comprehension improvement. Native action,
awaiting-authority, pause, disconnect, capture and deliberate test-delay categories
are separately retained in each timing.json. Available decision-window includes
poll/script time and cannot be called active human thought. Solo synchronous
Ready has no separate already-ready wait; final solo-stage-timing.json separates
scripted decisions, capture and pause/key-repeat probes. At720, the paired solo script recorded3.413 decision seconds in each
schedule,2.241→1.504 capture seconds and0.363→0.266 boundary-probe seconds.
Already-ready/disconnect time is0 for this synchronous solo task. These values
remain automation observations.

Captured native Controls show two or three actual stage columns as configured;
final Ready says income + Food → battle, and candidate first delivery says2×.
Figure assets, UI art and authoritative playback are unchanged.

## Count audit and reproduction

The authoritative schedule owns validation, stage count, next-delivery projection
and delivery scaling. Core phase transitions, received snapshot income/phase
validation, rules fingerprints, Shared team projection, phase text/track, Ready
consequence, opening guidance, final Food forecasts, solo/network drivers and
canonical campaign checkpoints read that source. General CampaignReview,
CooperationReview, OpeningCampaign review, GameplayScenarios and BattleShowcase
loops now derive their bounds. Building inspection distinguishes base producer
output from a double next delivery.

The audited remaining literals are intentional: original archive always3;
normal default-only graphical fixtures explicitly exercise selected3; phase track
allocates at most3 preparation columns plus a fixed combat column and hides the
unused one for the candidate. Research denominator3, Town Hall track levels3,
squad capacity3, first-three-wave task horizon and artwork geometry are unrelated
to readiness. `r5/stage-audit-before.txt` preserves the search inventory. General
policies accept the stage source; development fixture-only tests do not advertise
an arbitrary rules configuration. No production UI guesses a stage from a label.

Reproduction commands and evidence limits are in
[tools/preparation-review](../../tools/preparation-review/README.md). Original
b4fa3f7, fresh #21, canonical candidate and reordered candidate reports stay
separate. Acceptance log/source hashes are recorded in the r5 manifest.

## Verification / review boundary

Selected production revision90cde21 passes zero-warning build, Core74, Shared147,
three native integration facts,1–6-client multiplayer, all reconnect/fault phases,
two complete illustrated/plain gameplay profiles with native resize coverage,
seven remote waves,12 animation model checks and native animation/fallbacks.
All four final solo2/3×720/1080 journeys pass with Y/paused/key-repeat checks.
Both final target sizes were visually inspected. No owned native workers remain.
Draft [PR32](https://github.com/seraph1nia/common-watch-v2/pull/32) is stacked on31.
CI77 is running; its terminal status and the final documentation revision must be
recorded accurately before claiming the remote gate. The decision was posted for
[parent review](https://github.com/seraph1nia/common-watch-v2/issues/15#issuecomment-6378598861);
posting evidence does not claim an external review. No human comprehension, physical
controller/audio, live Steam or native Windows/macOS qualification is claimed.
The known intermittent native .NET/JIT crash risk remains unproven/fixed.

This document is the explicit product decision for parent #15 review. Parent
review and final integrated #25 sign-off remain visible obligations; no issue or
PR is closed/merged by this work. #23 receives the retained three-stage rhythm
and normal-payment ordering failures; #24 receives exact immediate versus ordinary
recovery timing; #25 receives both runnable schedules and preserved negative cases.
