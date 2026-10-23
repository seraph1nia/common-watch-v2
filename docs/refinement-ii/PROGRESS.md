# Refinement II checkpoint

Source of truth: [roadmap #15](https://github.com/seraph1nia/common-watch-v2/issues/15)
and its ten children. Preserve the complete scope, including executable rules
alternatives and final native evidence. Baseline HEAD was clean at `b4fa3f7`.
Current branch: `refinement/17-screen-journey`, stacked on
`refinement/16-phase-hud`. Core/Shared source and rules remain unchanged.

| Order | Issue | State |
| --- | --- | --- |
| 1 | #16 phase-specific HUD | Implemented and locally verified; draft [PR #26](https://github.com/seraph1nia/common-watch-v2/pull/26). Follow-up probe fixes through `23e94a3`; remote CI passed (run 53). See [contract/inventory](16_PHASE_HUD.md). |
| 2 | #17 controlled screen journey | Implemented and locally verified; all eight cooperative matrix profiles and final focused checks pass. See [transition contract](17_SCREEN_JOURNEY.md). |
| 3 | #18 combat framing/cues | Next. |
| 4 | #19 spatial squad-first army | Pending. |
| 5 | #20 cooperative context | Pending; Gate A follows these five presentation issues. |
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
a new stacked branch after recording the draft PR. Never mark the overall goal complete after
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
