# Roadmap #15 implementation audit

This audit uses the original parent and children #16–#25, the numbered contracts
in this directory, the original stacked PRs, consolidated PR #35 and the final selected-rule evidence.
It records implementation and automated review; it does not claim external
review, human playtesting, a merge or issue closure. The original baseline is
b4fa3f7807fcdf08040e9be2e186833b0504ab01. Final evidence is indexed by
`artifacts/refinement-ii/r3/shipping-manifest.json`; raw generated evidence remains
local and gitignored, with reproduction commands in the repository.

| Original scope | Implemented behavior and evidence | Delivery |
| --- | --- | --- |
| #16 phase HUD | `HudPresentation` assigns stocks, threat, funding, identity and errors one primary home. Combat removes the ledger/build palette/preparation ladder. Initial primary actions12→9, forecast7→3 logical lines. All management capabilities have replacement routes; hidden input is guarded. [Inventory](16_PHASE_HUD.md). | [PR26](https://github.com/seraph1nia/common-watch-v2/pull/26),23e94a3 |
| #17 controlled journey | Authority/playback-based transition policy cancels executable intent, restores harmless context, closes ordinary modals at battle entry and replaces accepted reward cards with an exact receipt/waiting surface. Overlay/focus, delayed ACK, rejection, same-frame advance, reconnect and repeated snapshots are checked. [Contract](17_SCREEN_JOURNEY.md). | [PR27](https://github.com/seraph1nia/common-watch-v2/pull/27),583ec3a |
| #18 combat hierarchy | Native safe-region framing, stable Manual/Overview, ownership base shapes, selective HP/labels/numbers and subject-aware inspection. Early/dense/major/final and debug/missing-art paths retain actual combat timing. Six-player follow-up in #20 increases720 median figure size22.9%,1080 4.2%, with zero initial HUD overlap. [Comparison](18_COMBAT_READABILITY.md). | [PR28](https://github.com/seraph1nia/common-watch-v2/pull/28),59fbebf |
| #19 spatial army | Six real homes, one selected-home surface, paid recruitment and legal relocation in one workspace. Towers, storage, return, history, retirement and all disabled/read-only reasons remain reachable. Scripted task12→8 actions,4→2 navigation changes,2→0 major-section changes at both sizes. [Routes](19_ARMY_WORKSPACE.md). | [PR29](https://github.com/seraph1nia/common-watch-v2/pull/29),cb8d787 |
| #20 cooperative context | Inspected city agrees with default camera; explicit Manual/Overview survives updates. Compact Team exceptions and accepted-reward waiting retain authenticated local identity. Fallen views allow useful read-only observation and continued real allied combat; no authority transfer. Six-player Team205→147px. [Evidence/limits](20_COOPERATIVE_CONTEXT.md). | [PR30](https://github.com/seraph1nia/common-watch-v2/pull/30),ce2d5c6 |
| #21 immediate rewards | Six immediate resource/recovery families replace passive battle cards. Three distinct positive eligible offers, frozen exact quotes, once-only acceptance, original battle-loss journal and terminal no-new-draft semantics. Worldtree remains long-term specialization; no research reward substitution. Full catalog/economy and compatibility review. [Catalog](21_IMMEDIATE_REWARDS.md),[economy](21_ECONOMY.md). | [PR31](https://github.com/seraph1nia/common-watch-v2/pull/31),da3a51d |
| #22 preparation rhythm | Executable2-stage alternative compared against3; selected default3. Alternative pays2×then1×resources with Research/healing only once per actual Ready. Negative openings, Food, research timing, waiting and readiness barriers are retained. Explicit guarded Ready shortcut, no automatic spending/timer/Ready. Selection posted to parent; no external product approval inferred. [Decision](22_PREPARATION_RHYTHM.md). | [PR32](https://github.com/seraph1nia/common-watch-v2/pull/32),dbcd3d9 |
| #23 normal opening | All6doctrines×2seeds reach three clears with normal Wood-first payment, no mandatory Gold recovery. Paid specialist and failed frontline-first alternatives, four48-run matrices and full campaigns retained. Short doctrine copy, actual producer output/cost and one optional prompt replace the checklist; no economy subsidy. [Evidence](23_NORMAL_OPENINGS.md). | [PR33](https://github.com/seraph1nia/common-watch-v2/pull/33),a37b48f |
| #24 recovery | Selected-squad review batches ordinary acknowledged member commands, explicitly partial and cancellable. Exact capacity/eligibility/next HP/Food/replacement quote; no free healing, equipment change, auto-retirement or reconnect resumption. Healthy squads have no maintenance prompt. Ten legal contexts and24campaigns/480battles compare individual/group equivalence. Scripted wound flow11→2 inputs720,6→2 at1080. [Decision](24_SQUAD_RECOVERY.md). | [PR34](https://github.com/seraph1nia/common-watch-v2/pull/34),41cc004 |
| #25 consequences | Original20campaign/400battle archive and no-investment defeat reproduce independently.44controlled legal cases include same-army placement,15Metal budget, tower/home and three-wave investment; alpha/heavy major alternatives remain runnable. Selected W10 ordinary elite pairs, W20 original single boss; all other prices/timing/rewards unchanged.240campaign paired matrix, marginal/uneven1–6-city groups, final current baselines and exact replay/integrity checks retain negative results. [Selection](25_CAMPAIGN_CONSEQUENCES.md),[reproduction](../../tools/campaign-review/README.md). | Final stacked draft PR; production58a4e89, harness379e0aa |

## Integration gates

Gate A completed #16–#20 before reward/rules edits. Original Core remained intact
through the subtraction pass. Continuous solo/cooperative first-three-wave native
journeys cover ordinary pointer, keyboard and simulated controller input, delayed
commands, waiting, battle finishing, reward selection and next preparation.
Historical measurements compare the recorded revisions; they are not relabelled
as current screenshots.

Gate B implements immediate loot, compares2/3preparations, selects3 and then
separately evaluates normal openings and group recovery. Rule design/provenance
commits precede Core changes. Original reward/rhythm baseline archives remain
immutable; exact quotes/payments, frozen losses, current eligibility and private
reconnect survive the selected configuration. Retained constrained-Stone and
pure-ranged defeats expose compounding effects instead of hiding them.

Gate C implements W10's bounded elite alternative after controlled comparisons.
Primary/secondary/status research evidence does not support universal counter
claims.120selected full-matrix rows replay from fresh shipping-default authority
with exact initial/per-command/final hashes. Core deployment preview equals real
profiles, entry cells, Food participation and payments; frozen outcomes and actual
clear/immediate grants agree.36uneven1–6-city histories include real elimination
and continued group victories;12separate marginal doctrine histories all fail
legally atW8–16. Current normal-policy archive is8Victories/2ranged defeats;
legacy optimized policy archive separately wins20/20. These are correlated
policies, not independent players or a proof of fairness.

Current native regression, journeys, majors, six-client W10, recovery and endings
are generated from the shipping production source, at720/1080 and through plain/
debug/missing-art paths. Capture output overrides isolate this revision's files.
The final manifest records exact source, logs, screenshots, exports and CI status.
The full canonical suite checks Core75, Shared169, session/animation models109,
Pillow19 and6process integration tests including1–6clients/private reconnect,
illustrated/plain full gameplay and7remote waves. See manifest for final gates.

## Compatibility and ownership audit

The only #25 functional Core edit is W10's catalog row. W10 is a valid nonboss
formation, not a boss-plus-escort validator exception. W20 remains a valid single
L5boss with original multipliers. Combat8/reward1/battleflow4/protocol19 remain:
no command/schema/hit-timing change. Full campaign content is fingerprinted and
validated on receipt; fingerprinting is not local-default equality admission.
Valid old/new19catalogs can be authority-supplied, old18is rejected. Existing
matches/private resumes retain their declared catalog; fresh defaults use the
selected row. No active migration or reconnect credentials in settings/logs.

SessionCoordinator still exclusively owns application-root sessions and stable
`/root/CommonWatch/Session` RPC identity. UI reads detached accepted snapshots and
submits existing authoritative commands. Combat presentation samples Core playback.
No artwork, package, dependency/tool pin, NuGet lock, upstream WAV/loop, license,
new roster/currency/crafting or unrelated application service was introduced.
Existing nine settlement clearings, six army homes and forest assets remain.

## Evidence boundaries retained

Automated input verifies functionality and measured scripted task counts. Visual
inspection verifies the specified captures. Human comprehension, enjoyment,
perceived fairness and listening were not observed. Physical controller/GPU/audio,
live Steam and native Windows/macOS execution remain unqualified; exported
Linux/Windows packages are separate checks. The known intermittent native.NET/JIT
risk is not claimed fixed. CI63/65 watchdog failures are retained; later bounded
passes do not identify their runtime cause. The final network matrix also
retains one invalid probe-JSON read failure: seven profiles passed, the reader
was bounded to5reads with10ms between failures, and the final6p1080profile
is rerun separately. Persistent invalid JSON still fails; no low-level cause
is established by the cleaned-up temporary file.

The rare positive living-owned deployed survivor at first city elimination was
not found in bounded legal searches. Fixtures verify the observer deferral guard;
they are not a natural native observation. Real allied survivors, elimination,
Defeat and continued Victory are covered. #24's opening recheck overwrote84old
working r9image paths; historical hashes and explicit loss manifest are retained,
with source/raw receipts/final purchase pairs preserved. Current captures are
separate and are never presented as those lost historical bytes.

All ten implementation scopes and design decisions are delivered for review.
External acceptance, merging and closing the original issues remain with the
maintainer; these actions are not part of the implementation request.


Final native retry qualification: the first6p1080rerun exited during Ready.
The kernel journal at2026-10-24 07:34:56 reports `.NET BGC` segmentation fault
in`libclrjit.so`, and systemd-coredump identifies Godot PID1581378/SEGV. The
per-client logs, failed capture directory and filtered kernel receipt are
preserved. This is an observed instance of the documented native.NET/JIT risk,
not a gameplay assertion failure or a claimed runtime fix. A fresh bounded
rerun is recorded separately; earlier passing profiles are not discarded.


Final export qualification: both x64 folders build with the explicit local
editor-only Dummy text-driver override after default editor FreeType errors
reproduce independently in an empty project/fresh settings. Default task behavior
remains, with bounded120/360second import/export. The Linux exported game then
passes ordinary-text-renderer sprite/illustrated smoke outside the checkout.
See #25's final verification for the exact distinction between tooling override,
runtime smoke and default-driver remote CI. No font/system-package change occurs.


Final evidence-storage qualification: hashing encountered an actual EIO on
`r3/marginal-cooperative/coop-6-1-invested/summary.json`. The kernel independently
reports Btrfs checksum failures on the local NVMe filesystem. Its19,307,362-byte
serialized object was recovered directly from the readable aggregate report;
final snapshot SHA80191e92a391f474a542227287d15c3c876e67a57ebf4e77f1b9a8aa36140356
matches the prior replay. The preserved original later became readable and has
the same full SHA8ad61bd2786d69963bc4249152476aba60547f0127cb791acc9ecd026a5d65de.
The complete manifest pass read all1,050tracked sources,14,810evidence files and
550export files successfully on that attempt. These successful reads do not
qualify the storage device or establish the cause of the JSON/font/JIT failures.
The exact recovery/kernel receipts and fresh affected-campaign replay are retained
in the final manifest. No filesystem repair or package installation was attempted.
Remote clean-checkout CI remains separate from this host's evidence and failures.


## Consolidation and final review corrections

At the user's request, all ten scopes are delivered together in PR #35 against
`main`; PRs #26–#34 are superseded historical implementation boundaries. The
final review found and corrected the combat Keep-health visibility comparison
and the elapsed-tick table described in [PROGRESS.md](PROGRESS.md). Native
regression uses only ordinary Ready commands and real combat damage; it fails
with the old comparison and asserts the rendered label and layout after the fix.
The failed local no-mistakes infrastructure run is reported separately from
manual checks and remote CI. The original filesystem became read-only; final
work is isolated on a writable RAM filesystem without a storage repair claim.
