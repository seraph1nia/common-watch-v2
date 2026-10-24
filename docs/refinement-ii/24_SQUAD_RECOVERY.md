# Squad recovery — issue #24 design

Baseline a37b48f (#23), immediate rewards1, three actual preparations, wire19 /
battleflow4. #23 local gates and exact-SHA CI81 passed, including both exports.
Preserve baseline source/captures and every compared failure. No Core change is
selected by this design.

Build a runnable, bounded wrapper over the existing Store/Return member commands.
Select a wounded squad/home, review exact IDs, current wounds, chosen hall and
space, next actual eligible healing, future stored Food and field absence; commit
once. Only members that fit are promised. Healthy members stay in the field.
Return recovered members is another explicit finite review/commit, using Core's
current compatible destination projection. Never retire/recruit automatically.

This sequence is **not atomic**. Submit at most one ordinary command at a time;
wait for its actual acknowledgment AND accepted assignment snapshot before the
next. A duplicate ACK must not advance twice. Cancel stops unsent operations;
an already accepted/in-flight move remains real and must be reported. Reconnect
or session replacement cancels the local sequence; a fresh preview excludes
already moved members. No persistent automation or batch state in preferences.

Freeze identity, phase/turn, roster health/profile/assignments, hall generation/
capacity/healing, plots, funds and next income. Recheck before each submission;
only the reviewed sequence's acknowledged assignment changes may differ. Pause,
Ready, selection/city/session/phase changes or unrelated owned changes stop/requote.
Allied/fallen/terminal views have no executable plan. Retirement keeps its existing
separate reviewed irreversible flow and safe cancel.

Keep existing Core Food priority, eligibility, individual capacity, healing tracks,
fixed equipment and service semantics. Store moves/heals no HP immediately.
Eligible storage heals once per actual Ready (three per ordinary wave), capped
by Core; stored members require funded Food and never fight. Ineligible members
may be stored but cannot promise healing until a completed funded battle. Immediate
recovery rewards and research/maximum-HP changes remain separate and invalidate
stale quotes. No free heal, resurrection, gear/XP benefit or refund.

Compare early healthy/same-tier and wounded cases, obsolete equipment, late full
field, Food shortage, full/partial/no hall, blocked returns and actual reward heal.
Record exact prices, next/cumulative eligible HP, time absent, Food, capacity,
participants and actions. De-emphasize recovery for dominated obsolete equipment;
Keep remains no action. Evaluate honest individual-versus-wrapper interaction
cost, including partial acceptance, cancellation and rejoin. Do not infer universal
optimality or human enjoyment from scripted cases.

Integrate one wounded-squad exception in the existing army context. Keep detailed
member exceptions/history optional; remove service counts from primary combat
comparison. No new maintenance dashboard or healthy-member checklist. Native
pointer/keyboard/controller events at720/1080 must verify real accepted counts,
healing, no refunds, stale-intent safety and the plain/art fallback. Required
build/Core/Shared, session/gameplay, campaign, multiplayer/reconnect and final CI
remain. A larger text comparison alone does not complete this issue.

## Selected implementation and comparison

Select the finite squad wrapper, preserving all Core recovery rules. No new
healing economy, automatic rotation, retirement, permanent manager, protocol or
serialized simulation field is introduced. A review moves only the exact
wounded members that fit; full/blocked members stay where they are. Recovered
return reviews the selected hall's full-HP members, not a fictional original
squad reunion. Destinations use current Core placement and are disclosed before
commit. Individual management remains available.

Ten reachable checkpoints compare keep, store and replacement under immediate
rewards and the selected three actual preparation stages. Every checkpoint is
earned with ordinary commands. Store costs no resources and heals no HP now;
retirement refunds nothing. Healing requires actual eligibility and payments,
Food, space and time out of combat. Current trainer prices are displayed rather
than treating low-level old gear as intrinsically valuable.

| Checkpoint | Executable consequence |
| --- | --- |
| Early healthy new squad | No recovery task or primary service warning |
| Early wounded | Three fit; the worst member2.5/40 needs19 eligible payments at5%; new L1 is2Metal |
| No hall / late full field without hall | No store; keep or individually replace, no invented destination |
| W8 older equipment | Three fit; next combined HP90.6→105.6; up to3 payments at15%; current L4 recruit5Metal,98HP versus old L1 max40 |
| Food shortage | Field funds first; stored ID11 loses eligibility after unfunded upkeep and stays35.8→35.8 on the next actual payment |
| Full hall | Zero moves; everyone remains in field |
| Partial hall | Exactly1/3 moves; two remain in field |
| Recovered hall | Two full-HP members return to compatible places without healing, price or equipment changes |
| Blocked field | No return; both remain stored |
| Late replacement | Paid L5 replacement7Metal/133HP; retirement has no refund or inherited service |

The comparison's independent individual and wrapper Matches have byte-identical
full snapshots before moves, after3 store commands and after the next Ready.
The wrapper reduces the reviewed native task from11 inputs at720 (including
scrolling) to2; the prior1080 route used6 versus2. These counts start after
selecting the same squad; the individual baseline already has its member roster
open and excludes the extra action needed to open it. Counts exclude later explicit return and are scripted actions,
not measured human task time. Both routes issue exactly3 authoritative commands.
Partial capacity is promised as1/3, not a failed all-or-nothing purchase.

The explicit reserve campaign comparison covers all6 doctrines×seeds1/20261018×
individual/wrapper:24 campaigns,480 battles,1546 member moves and722 group reviews.
Every command and battle-boundary full-snapshot digest matches within all12
pairs. All24 finish Victory under this particular paid reserve policy; this is
functional equivalence, not difficulty or universal recovery superiority.
Ordinary healing and immediate reward healing are recorded separately. A real
W8 recovery quote restores60.75 combined HP; retry changes nothing, and the next
production heal starts from the actual post-reward HP, capped once.

## Simplification and safety

Primary comparison no longer lists service counts/history. Those remain under
Advanced details. Healthy field members have no routine maintenance warning.
A selected wounded squad offers one review; newer available equipment is called
out, and the review gives payment time plus the actual replacement quote.
Keep remains no action. Retirement remains separate and reviewed. The group
flow replaces repetitive member storage clicks without adding a new dashboard.

One command is outstanding at a time. Its complete command identity, sequence,
ACK and accepted assignment must agree. Reordered/duplicate ACKs cannot move the
next member. Health, funding, profile, phase, turn, hall generation/capacity or
unrelated roster changes stop the remainder. Cancel retains accepted or in-flight
moves and reports the confirmed prefix; it never rolls back authority. Session
replacement/reconnect cancels local intent; a fresh review excludes stored IDs.
No recovery credentials, pending sequence or automation enter settings.

A real two-client test stops the host process before submission, cancels the
remote sequence with one move pending, resumes authority and observes exactly
one accepted move. Retry changes nothing. Private rejoin preserves exactly that
stored identity; a fresh review moves only the remainder and the next real Ready
matches Core's eligible capped healing. This qualifies remote state/ACK ordering,
not only synchronous local commands.

## Reproduction and evidence

See [reproduction instructions](../../tools/recovery-review/README.md).
Use `mise run build`, `mise run test-core`, `mise run test-shared`, then:

```sh
mise exec -- dotnet run --project tools/scenarios -- squad-recovery-review artifacts/refinement-ii/r8/comparison-selected.json
mise exec -- dotnet run --project tools/scenarios -- recovery-campaigns artifacts/refinement-ii/r8/campaigns.json
mise run capture-recovery
mise run test-recovery-network
```

Raw receipts include before/after snapshots, exact IDs, prices, Food, participant
manifests, reward quotes and eligibility. Fresh `comparison-selected.json` includes payment counts and current trainer
quotes; `campaigns-selected.json` is the rebuilt CLI result. Native images and action counts live in
`artifacts/captures/recovery`; the remote receipt includes cancel/retry/rejoin and
actual production. Logs use `/tmp/common-watch-r8-*` and the final manifest
archives them with source/capture hashes. Automated Linux software rendering,
synthetic pointer/key/controller input and inspected images are distinguished
from unmeasured human comprehension/enjoyment and physical hardware input.
Live Steam, native Windows/macOS execution and the known intermittent native
.NET/JIT crash qualification remain unchanged. Final gate results follow below.

## Existing management inventory

| Concern | Retained rule and interaction |
| --- | --- |
| Persistent identity/wounds | Exact IDs survive store, return and deployment; no recruit/identity substitution |
| Service | Historical battles/kills only, optional Advanced inspection |
| Equipment | Fixed recruitment tier; research may change profiles and invalidates pending reviews |
| Field/storage | Six purchased homes with compatible squad space; hall capacity/generation checked per move |
| Upkeep | Actual final Ready funds field first, then stored members; unfunded storage loses eligibility |
| Eligibility | Funded completed battle required; a newly stored recruit gets no immediate eligibility benefit |
| Healing | One capped ordinary heal per actual Ready; separately purchased capacity/healing tracks remain real costs |
| Return | Explicit selected-hall review and compatible destinations; no automatic rotation |
| Replacement | Separate per-member retirement confirmation then paid recruit; no refund, inherited history or auto-retire |

The eligibility and two hall tracks are retained rather than hidden or merged:
shortage receipts show a real funding consequence, and paid capacity/healing have
separate meanings. No untested rules simplification is claimed. This change
simplifies repeated handling through the existing commands; any later eligibility
or price change needs a separate explicit Core/version comparison.

## Final local gates

Zero-warning locked build, Core74/Shared166, native session3, both complete
illustrated/plain gameplay profiles plus seven remote waves, held-authority
recovery/rejoin,1–6-client matrix, full reconnect and animation/fallback checks
passed. All6doctrines×3waves passed again at both720/1080 with ordinary native
construction/recruitment/Ready. Illustrative recovery/return captures were
visually inspected at both sizes; plain and missing-world-art captures passed.
The first full gameplay run caught an old literal eligibility-text assertion;
it now checks the actual roster eligibility and both profiles pass. Failed logs
remain archived. No timeout or simulation rule was weakened.

Capture provenance correction: the final opening task reused its historical
working output directory and overwrote 84 #23 image paths. Their old hashes
remain in r9's manifest with an explicit availability report; these new images
must not be labelled old baseline evidence. The archived a37b48f source, raw
campaign/comparison receipts and final paired purchase captures remain intact.
This run's opening/recovery images are copied into r8 before later work, and
`COMMON_WATCH_OPENING_OUTPUT` now permits a distinct review destination.

Both self-contained Linux x64 and Windows x64 exports passed locally. Native
Windows execution is still unqualified. Exact-SHA CI is pending on the stacked
draft PR; its status is maintained in the r8 manifest and PR without rehashing
this source after #25 begins. No merge or issue closure is claimed.
