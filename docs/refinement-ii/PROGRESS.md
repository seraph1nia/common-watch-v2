# Refinement II checkpoint

Source of truth: [roadmap #15](https://github.com/seraph1nia/common-watch-v2/issues/15)
and its ten children. Preserve the complete scope, including executable rules
alternatives and final native evidence. Baseline HEAD was clean at `b4fa3f7`.
Current branch: `refinement/20-cooperative-context`, stacked on
`refinement/19-squad-workspace` (`cb8d787`). Core source/rules and Shared simulation rules remain unchanged; #20 changes the Shared team presentation projection.

| Order | Issue | State |
| --- | --- | --- |
| 1 | #16 phase-specific HUD | Implemented and locally verified; draft [PR #26](https://github.com/seraph1nia/common-watch-v2/pull/26). Follow-up probe fixes through `23e94a3`; remote CI passed (run 53). See [contract/inventory](16_PHASE_HUD.md). |
| 2 | #17 controlled screen journey | Implemented and locally verified in `583ec3a`; draft [PR #27](https://github.com/seraph1nia/common-watch-v2/pull/27), remote Godot run 55 passed. All eight cooperative matrix profiles and final focused checks pass. See [transition contract](17_SCREEN_JOURNEY.md). |
| 3 | #18 combat framing/cues | Implemented and locally verified in `59fbebf`; draft [PR #28](https://github.com/seraph1nia/common-watch-v2/pull/28), remote Godot run 57 passed. Final maximum-group 720p framing recheck belongs to #20 / Gate A. See [contract and evidence](18_COMBAT_READABILITY.md). |
| 4 | #19 spatial squad-first army | Implemented locally: six retained home selectors, adjacent recruitment/placement, bounded optional member/storage/tower/profile depth; verified in `cb8d787`; draft [PR #29](https://github.com/seraph1nia/common-watch-v2/pull/29), remote Godot run 59 passed including Linux/Windows exports. See [contract and routes](19_ARMY_WORKSPACE.md). |
| 5 | #20 cooperative context | Active: compact cooperative status, explicit inspected/command/camera identities, stable observer flow, final maximum-group framing. Gate A follows these five presentation issues. |
| 6 | #21 immediate battle rewards | Pending; actual one-shot resources/recovery, no research-point substitute. |
| 7 | #22 preparation rhythm | Pending; execute matched two/three-stage alternatives. |
| 8 | #23 opening economy | Pending; execute cross-doctrine normal-payment alternatives. |
| 9 | #24 veteran maintenance | Pending; execute a real squad-recovery alternative under selected rewards/rhythm. |
| 10 | #25 consequences/campaign | Original twenty campaign runs reproduced before rules changes; command/event archival and controlled comparison harness still required before #21; final calibration last. |

Full child specifications are cached at `/tmp/common-watch-issues/16.md` through
`25.md` for this workspace session. They are not replaced by this table. Keep all
ten children and Gate A/B/C active. Do not claim Gate A until #18–#20 finish.

Baseline evidence: `artifacts/refinement-ii/baseline/{gameplay,cooperation,phases}`,
`campaign.json`, `rules-source.tar` and `manifest.json`: original sources, hashes,
20 runs / 400 battles. All screenshots show actual application views and legal
command checkpoints. Before #21, finish exact command/event archival and the
no-investment baseline; the report alone is insufficient.

#16 commits: `6cf46f7` serializes unsafe cold Arch fixtures; `717c3cf` implements
HUD subtraction. `67c5439` holds dense cooperative captures at tick 60 (short
battles may finish before 180) and observes missing-host startup for 45s while
retaining its actual 15s admission deadline. `23e94a3` reads the foreign-command
acknowledgment from one status snapshot instead of racing two reads across a
pending result replacement. Both fixed probes passed locally. Remote Godot runs
49 (`40006317040`) and 51 (`40010715857`) failed on those diagnosed probe defects.
Run 53 (`40013031985`, commit `23e94a3`) completed successfully on GitHub, including the native gates and Linux/Windows cross-build workflow.

Verified #16 evidence includes zero-warning locked build; Core 46, Shared 145,
session/model 92 and Pillow 19 tests; native session/default/missing/corrupt UI,
four full default/debug 720p/1080p gameplay sequences, seven paid network waves,
final native phase/focus, five-size settlement input, deterministic animation,
1–6-player multiplayer, private reconnect, 2–6-player cooperation and artwork.
See [test execution](TEST_EXECUTION.md) for the unsafe Arch cold-registration
finding, deliberately terminated unresponsive diagnostic host and bounded retry.
Do not claim all intermittent native .NET/JIT faults are solved.

#17 adds a bounded `HudJourney` observer and central HUD entry effects, not new
Core phases. Current model total 102 includes idempotence, finishing, same-update
solo receipt, acknowledgment-before-snapshot, rejection, reconnect/session reset
and frozen pre-heal outcomes. The heal-ordering snapshot is explicitly a detached
unit fixture, never a native campaign checkpoint. Native journey setup and all
continuous opening actions use legal commands and actual native controls.

During #17 verification, the debug 720p retirement inspector reproducibly
oscillated across automatic-scrollbar widths and exhausted Godot's redraw queue.
A focused native resize trace identified it. Reserving scrollbar width fixed the
inspector; a later Arcanum context exposed the same issue in selected-building
scrolls, now also reserved. Both failures are retained as diagnostics, distinct
from the earlier unproven allocator/JIT fault. A broader native regression also
caught reward content disappearing behind Menu; the coordinator now preserves
an already-visible chooser and only postpones a newly arriving chooser.

All edits belong to this task; the initial worktree was clean. Native automation
uses private temporary settings/resume paths and owns its process cleanup.
Accelerated automation establishes lifecycle behavior, not human pacing,
comprehension or balance. No physical audio/controller/GPU, native Windows/macOS
or live Steam qualification is claimed. No new artwork was created or fetched.

Issue-boundary checkpoint: #17 is locally complete; delivery branch
`refinement/17-screen-journey` targets `refinement/16-phase-hud`. Continue #18 on
the current stacked branch. Draft PR #27 targets #26 and its CI must be polled. Never mark the overall goal complete after
only these two issues. Older tool handles/PIDs are historical; revalidate current
process state before acting on any handoff note.

The complete eight-profile cooperative matrix passed (2-player default/debug at
both sizes; 4/6-player default at both sizes), following four native solo
sequences. A focused follow-up reproduced Settings losing focus to an old
chooser invoker after acceptance; the chooser now restores only its own focus.
Accepted acknowledgment before inventory delivery now uses compact syncing
status instead of three disabled offers. Final verification passed: all 102 contracts, a native debug/plain 720p solo
journey and a native default 720p two-player journey. The final focus guard also
passed all four solo profiles. The full group matrix predates only those final
focus/acknowledgment refinements; the focused native cases cover their changed
paths. See the contract for exact checkpoints. All #17 verification handles are
terminal; no owned live native workers remain.

#18 local evidence: zero-warning locked build, Core 46 / Shared 145 / models 102,
three native session process tests, four full gameplay profiles and seven paid
network waves, private reconnect, 24 modular arena/projection/size cases, animation
fallback captures, and nine repeated 30/60/120-target-FPS battles at all speeds.
All eight deterministic normal-speed solo before/after fixtures preserve outcomes
and ticks. Initial actors grow 9–15percent at 720p and 22–28percent at 1080p; initial
label/actor bounds overlaps fall 3/8/4/3 to 0; 470 selected-body/target observations
remain unobscured. Default/debug normal-speed sequences and actual 6p normal
encounters at both sizes pass. Optional videos are HALF-SCALE desktop proxies;
PNGs and geometry measurements are native pixel size.

Remaining Gate A obligation: maximum-group 720p actor-size gain is modest because
of the current Team/action footprint. #20 must reduce routine combat team
monitoring, keep explicit inspection/focus routes, and refresh final group
framing/captures. Consider actual native menu-button bounds rather than unused
HBox width when defining occluders; keep native picking free in any unused space.
Do not lose this required integration check when moving to #19. Full issue scope
remains in /tmp/common-watch-issues/{18,19,20}.md.

#18 artifacts: artifacts/captures/combat-review/{before,after,after-debug},
comparison.json and optional desktop videos. Source/capture limitations in
18_COMBAT_READABILITY.md. Capture setup uses legal commands. Fixed test match
identity stabilizes personal offers, but ENet command interleaving changes actor
IDs/tie breaks: exact outcome evidence is SOLO, network captures are coverage.
Initial PNG-heavy slow instrumentation and variable-offer network attempts are
retained separately. The recorder double-signal trailer issue was fixed; final
logs are clean. No new artwork/dependency pins or Core/Shared source changes.
All final #18 verification handles are terminal and no owned native workers remain.


#19 checkpoint: feature `cb8d787ab72436a17c5fef79d95351e64561d9be`, baseline
probe `fdb4a4e`, draft PR #29 on #28. All final local handles are terminal;
no owned native workers remain. Source/log manifest is
`artifacts/refinement-ii/r6/manifest.json`; native before/after task and six-profile
input/art matrix are under `artifacts/captures/army-workspace/`. The task comparison
passes exact payments and accepted assignments: 12→8 actions, 4→2 navigation,
2→0 major Army section changes, both sizes. Before popup input is explicitly
signal-qualified; after task is all native pointer. Arrow and D-pad traverse all
six retained homes in all six final cases; mouse/keyboard/controller confirm
actual placements. Home tower branches pay/refund exact quotes after legal
producer/production/resale setup. No Core/Shared rules/protocol/assets changed.

#19 local receipts: zero-warning build; Core 46 / Shared 145 / models 106;
three native session facts; four full illustrated/debug gameplay profiles;
final default/debug/missing-art matrix at both sizes; both seven-wave remote
workers with actual delayed authority, retry, rejection and allied ownership;
1–6-player multiplayer (21 client receipts); private reconnect including death
and draft; continuous three-wave journey/overlay gates; 14 art/showroom captures.
Four full profiles precede the last secondary-tab style and shorter unavailable
text; final matrix/task/remote/lifecycle checks cover the final presentation.
Diagnostic failed probes/layout/setup attempts and one simultaneous lost Xvfb
display are retained separately; final native gates were serialized. No physical
hardware/input, human, native other-OS or live Steam claims. Native runtime risk
remains. CI #19 must be polled by its full SHA while #20 progresses; isolate any
required #19 repair in a worktree rather than mixing incomplete #20 changes.

Next: read the complete #20 spec and existing TeamAwareness/TeamPanel/camera
contract, capture the cooperative baseline before production edits, then keep
command ownership separate from inspection and explicit camera modes. #18's
maximum-group 720p gain was modest; reduce routine Team footprint, retain
inspection, refresh normal group captures and verify final framing for Gate A.
Gate A also requires full unchanged-rule solo/co-op first-three-wave journeys.
Before #21, finish #25's exact command/event archival and no-investment baseline
reproduction from the original sources; existing reports alone are insufficient.
#21–#25 retain their entire cached specs and executable alternatives, not
prose-only evaluations or research-point substitutes.


#20 active checkpoint (2026-10-24)

Branch `refinement/20-cooperative-context`, on #19 `cb8d787`; probe baseline
`2bba5fd` changes no presentation. Production implements compact native team
boundaries/exceptions with bounded details and read-only Army/Worldtree; explicit
command owner / inspected city / physical camera focus; playback-sector live
location labels; one harmless observer choice with own-survivor/manual/explicit
choice deferral; typed unsubscribed events and old-session guards. Core rules,
transport, settings, assets, dependency pins and credentials remain unchanged.
Shared changes only its team presentation projection. The implementation/evidence
contract is [20_COOPERATIVE_CONTEXT.md](20_COOPERATIVE_CONTEXT.md).

Accepted checks: build8 zero warnings; Core46 / Shared145 / models109. All eight
focused native cooperative three-wave profiles passed on build5: 2p default/debug
both sizes and4/6p default both sizes, with host/P2/final guest identities, actual
P1→P2 canvas shift, old selection clearing, wheel/pan across real paused speed
updates, F/R/Focus city, bounded disclosure, read-only allied overlays,
keyboard/controller return, delayed ACK, slow finishing and ready/draft reconnect.
The final full native exception/layout worker before the new cross-sector checks
also passed. Eighteen paired prep/ready/local-ready panel states retain players
and exact boundaries while shrinking all panels: 2p87→84,4p146→126,6p205→147px.

Final normal 6p captures at both sizes pass under
`artifacts/captures/combat-review/r10-final`. Routine combat action height is
207px rather than336 at720. Speeds/Pause share a row, Overview/details another;
combat controls use the left with Team, yielding one wide field. Requested default
framing waits one native layout pass, then remains stable. Height reduction alone
and layout waiting alone did not improve figures; opposing corners were limiting.
Final median actors59.8→73.5px (720,+22.9%) and106.5→111.0px (1080,+4.2%), zero
initial text/body intersections. Comparator enforces >15% at the outstanding720
case and positive zoom/size with no collision regression at1080. Its provisional
5%-at-both-sizes requirement had rejected1080; that was not a product criterion.
Both final encounters270ticks, wall8.83/12.88s, normal1x factor.5 after paid
accelerated setup. Network histories are unpaired; do not claim causal outcomes.

Bounded paid own-survivor searches are terminal negative discovery results:
ordinary families54cases and capped armies81cases, plus earlier single-Crossbow
cases. Retain source copies in artifacts/refinement-ii/r10/ObserverCases-*-negative.cs
and /tmp/common-watch-r10-owned-search*.log. Search code was removed from the
production tools project; it found no positive native fallen-owner-survivor case.
Do NOT fabricate one or substitute preference fixtures for that evidence. The
three pure ObserverSelection fixtures establish only presentation deferral,
one-choice/explicit-choice stability and session/match resets. Final native
exception worker now also tests a fallen guest's explicit own/manual choice
through real unrelated updates, and actual cross-sector owner/location inspection.
That inspection uses the development selection API, not OS pointer qualification.

LIVE final serial native queue: exec41489, script
/tmp/common-watch-r10-gates-rest.sh. Poll with write_stdin. Do NOT rebuild Godot,
edit the live queue/worker files or launch another Xvfb job while it runs. It runs
full test-cooperative-context (8focus profiles + legal exceptions), test-session,
full gameplay, multiplayer1–6, reconnect/fault,24arena cases, art, animation and
four solo journeys. Per-task logs /tmp/common-watch-r10-*-final.log; inspect actual
process state if a handle is uncertain. Earlier72845 is terminal1: normal captures
passed, then the provisional comparator rejected1080; corrected comparison passes.

Remaining: finish final queue and receipts/source manifest; update verification,
refresh #18 Gate A obligation with real geometry, deliver #20 draft PR on #29 and
poll exact-SHA remote CI. Gate A requires final solo/co-op first-three-wave native
gates and unchanged rules, not just compilation. Accurately retain the rare
fallen-owner-survivor native gap and physical/human/GPU/audio/other-OS/liveSteam
limits. Before ANY #21 Core rules, finish original #25 exact command/event and
no-investment archives at b4fa3f7. All five remaining specs and executable
alternatives remain mandatory. Overall goal active/unbounded; no pending approval.
