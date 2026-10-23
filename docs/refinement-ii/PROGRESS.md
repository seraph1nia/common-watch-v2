# Refinement II implementation checkpoint

Source of truth: [roadmap #15](https://github.com/seraph1nia/common-watch-v2/issues/15)
and its ten children. Preserve the entire scope, including executable rules
alternatives and final native evidence. Baseline HEAD was clean at `b4fa3f7`.
Branch for #16: `refinement/16-phase-hud`. No Core/rules change has been made.

| Order | Issue | State |
| --- | --- | --- |
| 1 | #16 phase-specific HUD | Implemented and locally verified; delivery in progress. See [contract/inventory](16_PHASE_HUD.md). |
| 2 | #17 controlled screen journey | Pending; owns modal suspension, deterministic entry effects and exact reward receipt transitions. |
| 3 | #18 combat framing/cues | Pending. |
| 4 | #19 spatial squad-first army | Pending. |
| 5 | #20 cooperative context | Pending; Gate A follows these five presentation issues. |
| 6 | #21 immediate battle rewards | Pending; selected product direction requires actual one-shot resources/recovery, no research-point substitute. |
| 7 | #22 preparation rhythm | Pending; execute matched two/three-stage alternatives. |
| 8 | #23 opening economy | Pending; execute cross-doctrine normal-payment alternatives. |
| 9 | #24 veteran maintenance | Pending; execute a real squad-recovery alternative under selected rewards/rhythm. |
| 10 | #25 consequences/campaign | Original twenty campaign runs reproduced before rules changes; command/event archival and controlled comparison harness still required before #21; final calibration last. |

Full issue specifications were retrieved through GitHub and cached locally at
`/tmp/common-watch-issues/16.md` through `25.md` for this workspace session; refresh
GitHub if needed. These are not replaced by the abbreviated table above.

Baseline evidence: `artifacts/refinement-ii/baseline/{gameplay,cooperation,phases}`
and `campaign.json`, with baseline logs. `rules-source.tar` and `manifest.json`
archive the original rules/replay sources and hashes (20 runs / 400 battles). All captures are actual application views;
all battle setup uses legal commands. Original native default gameplay at both
sizes, seven-wave network check and 2–6-player cooperation checks passed.

Verified #16 checks: locked zero-warning build; 46 Core, 145 Shared, 92
session/policy and 19 Pillow tests; three actual session process tests covering
ordinary/default/missing/corrupt presentation; four full default/debug native UI
passes at 720p/1080p and a seven-wave network replay; final native focus/phase
sequence at both sizes; five-size settlement input; deterministic animation;
1–6-client multiplayer, private reconnect, 2–6-client cooperation and artwork
captures. Final focus captures are in `artifacts/refinement-ii/r1/focus-phases`.

[Simulation test execution](TEST_EXECUTION.md) records the serial xUnit collection
policy required by Arch's non-thread-safe cold component registry. The failed
Shared host, deliberately terminated unresponsive host, bounded timeout, and
subsequent full passing run are retained honestly. A Godot allocator startup abort
was followed by a passing isolated full UI rerun; its native cause is not proven.
The prior intermittent native .NET/JIT risk remains. No human comprehension,
physical audio/controller/GPU, native Windows/macOS or live Steam qualification
is claimed.

All #16 implementation changes were audited against its visibility, routing,
input, focus and preserved-rules criteria. Commit/PR delivery follows this audit.
Do not claim Gate A until #17–#20 also finish. Next: #17, including the exact
transition table, modal suspension, accepted reward receipts, no premature
waiting preview, safe overlay exceptions and continuous three-wave native
solo/cooperative sequences. Keep Core unchanged until the later rules issues.

All changes in this checkpoint were authored during this task; the starting tree
was clean. No live verification handles remain required for #16. Older process
IDs and handoff notes are historical, not evidence of currently running work.
