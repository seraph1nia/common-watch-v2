# Controlled screen journey (#17)

`HudPresentation` projects current authority, authenticated local readiness and
choice, playback finishing and arena intent. `HudJourney` journals transitions
by session object, match, battle start and playback generation. `SettlementHud`
applies the local entry effects. Application coordination supplies explicit
Settings/Menu/exit coverage; no panel advances authority or playback.

| Accepted trigger / baseline | Local behavior |
| --- | --- |
| Initial join, session replacement, reconnect or playback-generation reset | Derive the current valid surface. Reset draft intent, cancel management confirmations, and establish a baseline without replaying reward receipts. A previously selected card has a static receipt with unavailable historical deltas stated explicitly. |
| Local Ready / Unready | Existing acknowledged state controls editing and readiness. Preserve viewed city and clearing; opening screens submits neither command. |
| Intermediate production | Existing authoritative production count updates stocks and feedback once. Remain in planning and retain clearing context. |
| Final readiness enters combat | Close Army/Worldtree/building details and live inspection; cancel retirement and selected construction intent. Clear preview and obsolete transaction labels. Frame the actual battle. Remember only viewed city/clearing, never an executable action. |
| Authority ends combat while playback finishes | Continue the actual battle view, labeled with its rewarded wave, until the existing playback boundary completes. Suppress reward interception and next-wave preview. |
| Nonfinal outcome, local choice absent | Show the frozen factual outcome and actual offers, with no extra Continue. Settings/Menu/exit coverage postpones a newly arriving chooser until the explicit overlay closes; a deliberately covered existing chooser stays in place. |
| Choice acknowledgment precedes inventory snapshot | Leave the chooser for compact accepted/synchronizing status; wait for the inventory snapshot before claiming actual effect deltas. Exact retry identity is unchanged. A rejection leaves offers and the existing rejection notice available. |
| Accepted local choice | Close the chooser, emit one compact receipt from the observed card and actual deltas, and show team choosing count. If solo advances in that update, show the receipt directly in planning. |
| Remaining choices complete | Enter planning only from authority. Restore harmless viewed-city/clearing context, without reopening a modal. |
| Local elimination / terminal authority | Hide personal purchasing controls and use the existing observer/terminal actions. Suspend stale management intents. |

Settings remains open and adjustable during live combat. Menu and explicit exit
consent also remain; each names the current phase. Settings and exit consent have
an explicit return-to-battle control. Returning from exit cancels consent first;
it never leaves or pauses. Continue/Esc closes Menu. Existing Esc ordering closes
Settings/details/rewards before opening Menu. Hidden chooser/Ready focus returns
to a visible legal target. An arriving acceptance only restores chooser focus if
the chooser owned it before closing; an active Settings control keeps its focus.
The native regression opens Menu during legal combat, reaches the actual reward,
opens Settings through F10, then accepts the in-flight choice. It reproduced the
former focus jump to Menu and verifies Settings retains its slider afterward.
No local route sends an implicit Ready, reward, pause
or leave command.

Ally inspection remains read-only and authenticated command identity stays local.
Combat entry frames the local standing city; planning restores the inspected city
and clearing when still valid. Reward waiting permits City details, army and
Worldtree inspection under existing read-only permissions, but never exposes an
arena preview or predicts funded next-wave deployment. Deep tab/selection state
remains available for deliberate reopening, without automatic modal restoration.

Receipts use the canonical card effect and observed resource/Keep/soldier deltas.
If an intervening production settled, the receipt says so instead of attributing
those resource changes to the card. The bounded journal retains only the latest
receipt and previous snapshot. Battle outcomes remain owned by
`BattleOutcomeJournal`, frozen before any accepted healing/recovery effect.
Current-session event subscriptions disconnect on reset and exit. Card callbacks
check current shape, visible control, connection, pending state, phase and choice;
paused/old-session controls cannot submit an offer into a replacement session.

The obsolete housekeeping steps removed here are manually closing preparation
details to see combat, dismissing a chosen three-card surface, and dismissing a
separate result before choosing or returning to planning. Read-only details and
the explicit preferences/exit exceptions remain available.

## Verification and qualification

`mise run test-journey` owns four continuous native solo sequences (default and
debug/plain at 720p/1080p), followed by the native 2/4/6-player matrix in
`tools/journey-matrix.sh`. Setup submits legal commands; the continuous opening
uses actual doctrine, purchase, recruit, readiness and reward Controls. Separate
overlay cases exercise Army, Worldtree, retirement, Menu, Settings and exit
consent, including controller cancel. The network harness covers Unready, reconnect from Ready waiting, a
genuinely delayed authority acknowledgment, an early chooser, accepted-choice
waiting, a chosen reconnect baseline and slow playback while the host has an
outcome. Existing reconnect checks cover Building/Preparation/Combat/RewardDraft
and later planning reconnect, including a missing authority and elimination.

Captures and public state receipts live under `artifacts/captures/journey/`.
Private settings/session paths are temporary and never included in reports.
Combat is deliberately accelerated/stepped for bounded automation: these runs
establish lifecycle behavior, not human comprehension or normal-time pacing.

The initial matrix reproduced a debug-720p inspector layout loop: an automatic
scrollbar alternated available widths, wrapping text and exhausting Godot's
redraw message queue. A focused resize trace established the oscillation; the
inspector and selected-building/construction scroll areas now reserve scrollbar
width. The first cooperative matrix reproduced the same redraw-queue exhaustion
when a different doctrine opened its Arcanum context. This is distinct from the
previously documented intermittent native .NET/JIT risk.

Executed evidence:

| Checkpoint | Result |
| --- | --- |
| Locked build / unchanged authoritative projects | Zero warnings/errors; Core 46, Shared 145 passed. |
| Journey/policy contracts after the final acknowledgment projection | All 102 passed, including compact waiting before inventory delivery and unchanged frozen results. |
| Native regression checkpoint | Three session process tests, four complete default/debug gameplay profiles plus seven cooperative waves, animation, private reconnect and 1–6-player sessions passed. |
| Continuous journey matrix after scrollbar fixes | Four solo profiles and all eight cooperative profiles passed; the complete task took 697.71 seconds with each worker bounded separately. |
| Final focus refinement | All four solo profiles passed again, including legal acceptance while Settings holds focus. |
| Final acknowledgment refinement | Debug/plain 720p solo and default 720p two-player continuous three-wave runs passed again, including Ready-wait reconnect, real delayed authority, slow finishing, choice waiting and reconnect. |

The final focused artifacts are under
`artifacts/refinement-ii/r2/{ack-focus-final,ack-focus-final-network}`; completed
and diagnostic logs plus source hashes are under `artifacts/refinement-ii/r2/`.
The final focused checks follow the broader matrix; the full group matrix was
not repeated after the last focus/acknowledgment refinements. CI owns the four
solo profiles in addition to the established native integration/phase gates;
the complete eight-profile new cooperative matrix was executed locally.

No human, physical audio/controller/GPU, native Windows/macOS or live
Steam qualification is claimed. Core, Shared, protocol, simulation timing and
rules remain unchanged by this issue.
