# Spatial army workspace — issue #19

Core/Shared rules, prices, capacities, topology, protocol and transport are unchanged.
`ArmySelection` observes detached accepted assignments behind a pure interface;
`ArmyRosterPanel` owns native selection and command intent. Deployment rendering
contains six inspection buttons and submits no commands. One placement button
revalidates session, match, city, home, squad, turn and authoritative destination
between button-down and confirmation. A bare stale pressed signal cannot deploy.

The workspace defaults to the six homes in Core's entry order: front 1–3 facing
the enemy, rear 4–6 toward the Keep. Selecting an occupied home chooses its squad;
an empty home keeps the selected squad as its placement target. Compatible trainer,
role and reinforcement destination follow that squad. Only one trainer role is
shown as a primary recruit action at a time. Other roles and automatic/new/alternate
destinations remain native choices. Prices, equipment, doctrine adjustments,
capacity, wounds and Food projections come from existing quotes and projections.
Recruitment hides during combat/reward/terminal phases; roster inspection remains
possible without predicting a next wave. Allied and pending homes remain selectable;
command permission is displayed once and actions remain blocked.

The board and primary actions do not scroll. Members is an optional retained,
bounded roster. Home/Member/Storage/Quotes disclose secondary detail in their own
scrolling area. The old four Army sections, top-level squad selector and six
repeated Manage actions are removed. Per-city harmless selection survives closing,
refresh and deliberate city inspection; session/match replacement resets it.
Selected members follow accepted regroup/store/return assignments. Closing or
changing context cancels retirement/construction/placement intents. Action controls
are scoped to their session, match, city and actual inspected identity; hidden,
disabled or obsolete controls cannot submit commands. Disappearing owned actions
restore focus to the selected home without taking another overlay's focus.

| Capability | Previous route | Workspace route / authoritative owner |
| --- | --- | --- |
| Inspect field squad and funding | Army → squad selector → Homes | Occupied home; current-stock/final-income/last-battle label; `ArmyDeploymentView` |
| Reinforce/recruit | Army → squad selector → Recruit → trainer | Home selects compatible trainer/role; adjacent quote and actual arrival; existing recruit command |
| New/alternate squad | Recruit destination selector | Destination disclosure, automatic/new/compatible options; Core `Army.Find` |
| Relocate | Squad selector → Homes → destination | Home selection → single placement confirmation; existing deploy command/cell |
| Buy land | Homes → repeated Manage → Buy | Locked home → one quoted Buy action; existing home prices; six homes, nine clearings |
| Tower construction | Homes → Manage → Defense → card/confirm | Empty home → Home details, Defense only → card/confirm; existing build cost/exclusion |
| Tower upgrade/branches/sale | Homes → Manage → tower actions | Tower home → same quoted upgrade/branch/refund actions |
| Member/history/regroup | Roster → separate inspector | Members → selected member; bounded details while board/recruitment remain visible |
| Store/return | Roster inspector / Town Halls section | Member details; actual Hall and squad choices, unchanged recovery/payment semantics |
| Hall capacity/healing | Town Halls section | Storage details with real Hall selector and quotes |
| Retirement | Roster inspector → review → confirm/cancel | Member details → same guarded review, cancellation and no-refund confirmation |
| Ally inspection | City selector → Army sections | City selector → same six-home workspace; selection permitted, commands blocked |
| Arena/battle | Close Army → Show arena → final Ready | Preview actual field → final Ready; no implicit authority advancement |

The before routes above are an inventory from retained baseline source and native
checks, not claimed human timings or a measured action count for every operation.

The paired ordinary task is reinforced Swordsman → move Home 1 to 2 → purchase
Home 3 → preview → final Ready. Both versions use the same legally paid settlement
checkpoint, production and recruitment. Trace checks exact resource deltas and the
actual accepted battle-entry cells against the preview.

| Measured task, each at 1280×720 and 1920×1080 | Before | After |
| --- | ---: | ---: |
| Control actions | 12 | 8 |
| Navigation changes including entering Army and preview | 4 | 2 |
| Changes between major Army sections | 2 | 0 |

Before probe/source is committed as `fdb4a4e`; captures and JSON are under
`artifacts/captures/army-workspace/before/{1280,1920}`. Its legacy squad popup
choice uses the native OptionButton selection signal; OS popup pointer/keyboard
routing is **unqualified**. After uses pointer input throughout, under
`artifacts/captures/army-workspace/after/{1280,1920}`. These are scripted control
counts with paid legal setup, not human speed, learning or comprehension evidence.

Executed locally: zero-warning locked build; Core 46, Shared 145 and session
models 106; three native session process facts; all four complete
illustrated/debug gameplay profiles; both seven-wave remote workers; 1–6-player
transport (21 client receipts); private reconnect through preparation, combat,
death and draft; the continuous native three-wave solo/overlay journey; and all 14 artwork/showroom captures.
Final focused artwork/input checks cover default, debug/plain and missing art at
both sizes, including arrow-key and D-pad traversal through all six homes,
controller/keyboard confirmation and all four secondary tabs fitting together.
The four full profiles preceded the final secondary-tab style and shorter
no-destination text; final focused captures, paired task, remote, session and
lifecycle checks exercise that final presentation. CI also runs the focused
matrix and retains its actual screenshots. No unexecuted source state is claimed
as a completed graphical check.
Reproduce focused input/artwork checks with `mise run test-army-workspace`, task
traces with `mise run capture-army-workspace`, then
`python3 tools/compare-army-workspaces.py` against the retained baseline. The
focused native check covers six retained targets, mouse/keyboard/controller event
paths, relocation, locked-home inspection/stale confirmation, compatible
reinforcement, accepted-member regroup, purchase/focus, both paid tower branches
and refunds, partial Food funding and exact preview bodies/cells. Full native
regression retains all four roles, wounded/stored veterans, storage/return,
retirement cancellation, all screen phases and fallback presentations. Remote
checks use real ENet delay, exact retry, rejection and foreign ownership probes.
No physical controller, native Windows/macOS, live Steam, human or hardware frame
rate qualification is implied. Intermittent native .NET/JIT risk remains as recorded.

Diagnostic attempts are retained separately from accepted evidence. Early layout
captures exposed a narrow child in a ScrollContainer, a page-wide Army scrollbar
and a clipped secondary tab; final focus/geometry assertions and screenshots
verify those corrections. Tower test setup initially lacked Metal/Wood and was
correctly blocked: its final legal replay buys producers, earns production and
sells them at actual refunds before testing the quoted branches. No prices or
permissions were relaxed. One simultaneous Xvfb capture lost its display; final
native gates were serialized. The task comparator initially assumed null JSON
members were emitted; it now reads the existing omission format and passes both
sizes. These are diagnostic failures, not successful gate receipts or evidence
that the separately recorded intermittent native runtime risk is solved.
