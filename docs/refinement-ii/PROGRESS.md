# Refinement II checkpoint

Source of truth: [roadmap #15](https://github.com/seraph1nia/common-watch-v2/issues/15)
and its ten children. Preserve the complete scope, including executable rules
alternatives and final native evidence. Baseline HEAD was clean at `b4fa3f7`.
Current branch: `refinement/21-immediate-rewards`, stacked on
`refinement/20-cooperative-context` (`ce2d5c6`). Gate A preserved original rules; #21 now intentionally changes the Core reward catalog/receipts under its committed design.

| Order | Issue | State |
| --- | --- | --- |
| 1 | #16 phase-specific HUD | Implemented and locally verified; draft [PR #26](https://github.com/seraph1nia/common-watch-v2/pull/26). Follow-up probe fixes through `23e94a3`; remote CI passed (run 53). See [contract/inventory](16_PHASE_HUD.md). |
| 2 | #17 controlled screen journey | Implemented and locally verified in `583ec3a`; draft [PR #27](https://github.com/seraph1nia/common-watch-v2/pull/27), remote Godot run 55 passed. All eight cooperative matrix profiles and final focused checks pass. See [transition contract](17_SCREEN_JOURNEY.md). |
| 3 | #18 combat framing/cues | Implemented and locally verified in `59fbebf`; draft [PR #28](https://github.com/seraph1nia/common-watch-v2/pull/28), remote Godot run 57 passed. Final maximum-group 720p framing recheck passed in #20 / Gate A. See [contract and evidence](18_COMBAT_READABILITY.md). |
| 4 | #19 spatial squad-first army | Implemented locally: six retained home selectors, adjacent recruitment/placement, bounded optional member/storage/tower/profile depth; verified in `cb8d787`; draft [PR #29](https://github.com/seraph1nia/common-watch-v2/pull/29), remote Godot run 59 passed including Linux/Windows exports. See [contract and routes](19_ARMY_WORKSPACE.md). |
| 5 | #20 cooperative context | Implemented and locally verified; draft [PR #30](https://github.com/seraph1nia/common-watch-v2/pull/30). Final revision ce2d5c6 passed Godot run71, including both exports. See [contract/evidence](20_COOPERATIVE_CONTEXT.md). |
| 6 | #21 immediate battle rewards | Immediate catalog/authority/UI implemented; Core65/Shared145/models109 and initial native gates pass; final graphical/transport/CI checks in progress. |
| 7 | #22 preparation rhythm | Pending; execute matched two/three-stage alternatives. |
| 8 | #23 opening economy | Pending; execute cross-doctrine normal-payment alternatives. |
| 9 | #24 veteran maintenance | Pending; execute a real squad-recovery alternative under selected rewards/rhythm. |
| 10 | #25 consequences/campaign | Original 20 campaigns plus no-investment defeat now have exact command/checkpoint/event replay; controlled trade-offs, elite formation and final calibration remain. |

Full child specifications are cached at `/tmp/common-watch-issues/16.md` through
`25.md` for this workspace session. They are not replaced by this table. Keep all
ten children and Gate A/B/C active. Do not claim Gate A until #18–#20 finish.

Baseline evidence: `artifacts/refinement-ii/baseline/{gameplay,cooperation,phases}`,
`campaign.json`, `rules-source.tar` and `manifest.json`: original sources, hashes,
20 runs / 400 battles. All screenshots show actual application views and legal
command checkpoints. Exact command/event archival and the no-investment baseline are complete;
reproduction tooling is staged separately before #21 rules changes.

## Presentation gate — #16 through #20

All five presentation changes are implemented on stacked draft PRs #26–#30.
Core rules remain original. Contracts and qualifications are in the five linked
issue documents. #20 final local gates passed: zero-warning locked build;
Core 46, Shared 145, session models 109; three native session facts; four full
native gameplay profiles and seven paid remote waves; multiplayer 1–6;
private reconnect/faults; 24 arena cases; 14 art views; animation/fallbacks;
four latest solo three-wave journeys; eight cooperative profiles plus final
finishing/phase-heading checks. Accepted source/log manifest:
`artifacts/refinement-ii/r10/manifest.json`.

Eight normal-speed solo pairs retain exact outcomes/ticks and pass 474
selected-body/target checks. Final six-player median figures grow 22.9% at 720p
and 4.2% at 1080p, with zero initial collisions; the #18 integration obligation
is resolved. Eighteen paired Team boundaries shrink at every roster/size.
Actual crossing, first-wave elimination/Defeat, manual fallen-view retention,
full paid uneven W20 Victory (both cities standing), native camera/team input,
delayed ACK, slow presentation and reconnect all passed. See #20 for exact
source scopes and timing qualifications. No owned native workers remain.

Rare gap: bounded legal searches found no positive living owned deployed
survivor at the owner's first elimination. Preference fixtures verify the
deferral policy; they do not qualify that native checkpoint. No fabricated state.
Physical hardware/input/audio, participant findings, native Windows/macOS and
live Steam remain unqualified. The known intermittent native runtime risk is
not claimed fixed. CI63/65 exhausted an outer journey watchdog; shutdown
StringName diagnostics followed termination. CI67/69 passed, including exports.
Final #20 commit ce2d5c6 passed Godot run71, including both exports, before Core reward changes. Retain per-observation
deadlines; complete six-client journeys now have a bounded 540-second allowance.

## Original-rule archive before #21

Exact original b4fa3f7 reproduction is complete locally: 20 campaigns / 400
battles plus no-investment Defeat at tick 1003; 3,470 commands, 844 checkpoints
and 41,512 events independently replay in fresh Matches. All 83 original
Core/Shared files match Git blobs. Aggregate report SHA remains
`6ef56ec5aa145a892fdd69e4c84c87ea2add5d57ed5606fe22d2b401f3505414`.
A copied corrupted event is rejected. Headless rendered wall time is null;
nominal combat and cleanup timing are separate. Eight representative native
original-rule timing reports are separately qualified, not rendered versions
of those 400 battles.

Early #25 tooling/docs are committed separately as `8da021d`, before any Core
change. Compressed traces were regenerated after timing metadata clarification;
SHA matches the baseline manifest (`ad17b06926b66becbae0dd744375477c6e172e148ba6c6434c33b714ac35a62f`).
No new project, dependencies, locks or shipping legacy reward path.

#21 original economy probe is `f4adfcf`, design/provenance `e36d20a`, accounting
follow-up `11ed2e3`. Locked build/Core46/Shared145 pass. The same 72-run policy
produces 1,050 battles and a byte-identical report against all 83 verified
original Core/Shared files. Report SHA
`197405bf601c7de6ee1bd4e008ef81c0a15453604e0b3e0a73bd9ba52cd0246d`.
Ten generic full-campaign policies terminate early (Merchant four nonranged
families W1, Grovekeeper ranged W3, both seeds); retain these losses for #23.
Original sources and exact policy copies are under `artifacts/refinement-ii/r7/`.
The [reward design](21_IMMEDIATE_REWARDS.md) audits all 36 old cards and declares
a frozen immediate-only catalog, actual receipts, capacity preflight and explicit
protocol18/reward-rules1 compatibility. The design preceded Core edits; actual immediate reward implementation is now in progress.

## Next issue boundary

Final #20 commit `ce2d5c6c7b282e28cfc1fdcf036617ffbb7bc6f7` is pushed; PR30
body is updated. Godot run71 (`40044935480`) completed successfully, including
both exports. Gate A is complete. The separate design/provenance and original
archive commits precede Core implementation; continue #21 now. Full specs remain /tmp/common-watch-issues/15.md and16..25.md. The selected direction
is actual one-shot supplies/recovery, without research-point substitutes.
#22 requires executable matched two/three-stage alternatives; #23 cross-doctrine
normal-payment opening alternatives; #24 real squad recovery under the selected
economy; #25 controlled trade-offs, elite formation, held-out seeds and final
integrated all-doctrine/uneven-group campaigns. All remain authorized and active.
Do not mark the overall goal complete after Gate A. No pending user permission.

## #21 local completion / CI boundary

Implementation `d77422b2ccb59e52582b283a7e5d19e68cce09ec`, stacked draft PR31
on #20. New loot-* identities, frozen exact quotes, positive capacity preflight,
consumed receipts, frozen battle losses; protocol18 / reward1 / fingerprint4.
Old active passive paths are gone. No default enemy/price/rhythm/research or asset
change. Design e36d20a and GateA-success4ad9b9e precede all Core edits.

All local gates passed: build6 zero warnings; Core4=65, Shared-final=147,
models4=109; native session3; final four illustrated/plain720/1080 profiles;
seven remote waves; native2p720/6p1080 three-wave focus/quote/receipt/delta,
unchanged Research/profiles, delayed ACK, slow playback, ready/draft reconnect;
multiplayer1–6; private reconnect/faults; animation/fallbacks;14art views.
Both final chooser sizes were visually inspected. All owned local native workers
are terminal, including30182 and21498. Do not restart passed tasks from stale
handles. Exact remote command retries before/after rejoin additionally pass in
147-test Shared suite. Manifest: artifacts/refinement-ii/r7/manifest.json.

Economy72matched runs: before1050battles/50full Victories, after1037/49. Two
Arcanist ranged runs now failW5; Grovekeeper ranged seed1 now winsW20. Ordinal
policy selects Equipment457, Provisions277, Recovery218, Stone25; no Treasury/
Timber selection by that policy. All six have actual authoritative grant tests.
No selected recovery is zero-benefit. Keep repair/caps additionally use explicit
pure/custom arithmetic fixtures; native campaign victorious policies seldom
damage Keep. Full tables21_ECONOMY.md; original/current-harness original report
SHA197405bf... reproduces byte-identically. Raw command/payout/shortage data
and source copies are retained. No fairness or human inference.

Diagnostics: stale development setups attempted unavailable50Food sale and full
six-place storage. The new legal setup sells only available bundles / Farm
income when needed, and buys second storage capacity before storing. Receiver
validation initially compared prior-wave losses with current upkeep; fixed and
covered. No simulation injection, enemy retune or timeout weakening.

Final feature CI73 (40049160250) runs on d77422b. Final two retry cases/docs still
need their commit/push and exact-SHA check. Source production is unchanged by that
last refinement. Record PR checks as authoritative; do not claim CI before green.
No owned workers remain and no user permission/input is pending.

Next boundary: finish #21 final CI, then #22. Before changing preparation rules,
archive this immediate-reward three-stage source and record fresh per-stage
commands, payments/research/recovery/no-command counts. Predefine comparison
metrics, implement an executable two-stage alternative and run matched legal
paths plus real solo and2/4/6-client loops under both configurations. All remaining
#22–#25 specs and executable alternatives remain active. The overall goal is not
complete; do not substitute a prose-only decision for the requested experiments.
