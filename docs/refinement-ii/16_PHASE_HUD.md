# Phase-specific HUD (#16)

Parent: [Refinement II #15](https://github.com/seraph1nia/common-watch-v2/issues/15).
Baseline: `b4fa3f7807fcdf08040e9be2e186833b0504ab01`. This is the subtraction-only
pass: Core, rules, compatibility versions, dependency pins, artwork and audio are unchanged.

`UI/HudPresentation.cs` projects authority, authenticated local readiness/choice,
and the existing playback-finishing flag. It does not create a simulation phase.
`SettlementHud` applies that policy to retained Controls. No per-frame HUD recreation
or automatic command is introduced. #17 owns modal suspension, transition receipts
and restoring harmless context; #18 owns combat framing and selective labels;
#20 owns compact team exceptions. Those remain separate work.

## Information ownership

| Fact | Primary home | Intentional contextual inspection |
| --- | --- | --- |
| Stock and signed production income | Planning ledger, including committed Ready waiting | Actual building output/cost and Army/research quotes |
| Incoming threat and participating army | Short preparation summary | Hover brief; City details campaign schedule; arena preview |
| Funding exceptions | Preparation summary: field sitting out / stored unfunded | City details exact participating/funded/unfunded IDs and Food payment; Army member status |
| Last Food payment | City details completed-upkeep receipt | Frozen factual battle result |
| Next Ready consequence | MatchControls next-transition line and primary action | Ready tooltip describes research/recovery timing |
| Local identity | Own-city selector in planning; Ready explicitly names local player when inspecting allies | Combat ownership and Team rows; all submissions still authenticate locally |
| Team readiness / reward choice | Team rows | Compact count next to Ready or accepted reward; no second full team dashboard |
| Errors / remote progress | Persistent pending/rejected/disconnected action notice | City details recent feedback, including complete rejection reason |
| Routine acceptance | Actual changed world/resources and bounded factual feedback | City details recent feedback; accepted reward name in waiting |

The resource ledger no longer repeats Food/funding/old-upkeep paragraphs. The threat
summary no longer repeats six-resource income, transition explanation, ordinary stored
counts, or a fully funded Food quote. Full values and timing remain inspectable.
The guide starts collapsed and leaves with planning; it never gates Ready.

## Visibility contract

| Moment | Default regions | On demand / permitted routes | Removed default regions |
| --- | --- | --- | --- |
| Planning | Settlement, stocks/income, short threat summary, phase/Ready, collapsed Guide | Selected clearing; Build; Troops & homes; Worldtree; City details; arena preview | Routine Accepted/history link beside Ready; duplicate funding/income paragraphs; separate Settings plaque |
| Ready waiting | Committed ledger, threat, team/local Ready status, Unready | Read-only Army/Worldtree/City details and arena preview | Selected purchase inspector and Build entry; Guide |
| Combat / finishing | Actual arena, compact wave, damaged Keep health, team, pause/speed, optional actor inspection | Field overview; City details | Ledger and attached ornaments, selected plot, construction/navigation palette, preparation track, Guide, arena toggle |
| Choosing reward | Frozen result and actual three offers | Close chooser and inspect City details/result | Planning ledger/palette/Guide/track; underlying purchase context |
| Reward accepted / waiting | Accepted card name, choosing count, frozen result, Team | Read-only Army/Worldtree/City details; field preview | All three choice cards; Ready; ledger; selected purchase inspector |
| Fallen | Observation and team state; no local Ready | Permitted read-only inspection and living-ally navigation | Ledger, threat/Guide/track, Build and selected purchase context |
| Terminal | Authoritative final status and frozen result | City/history inspection; Menu and exit confirmations | Ledger, prep/Guide/track, Ready, Build and selected purchase context |

A forced live arena cannot advertise a no-op “Show arena” action. The manual preview
still offers the actual destination (“Show settlement”). Hidden Ready, speed, pause
and reward callbacks recheck their visible/valid context before submitting. Existing
`GameplayContext` guards prevent stale purchase handlers from editing an allied,
ready, paused, disconnected, fallen, terminal or non-preparation city. Focus leaves
a hidden region for a visible valid Ready, field overview or Menu; scroll/selection controls remain
retained. Decoration remains pointer-inert and leaves with its parent region.

## Capability/removal inventory

| Previously prominent capability | Current route / retained behavior |
| --- | --- |
| Separate Settings and Menu plaques | Menu → Settings; Esc, F10 and controller Start retain the same menu; exit confirmations unchanged |
| Routine Accepted / Recent feedback link beside Ready | City details → Recent session feedback; unresolved exceptions still show beside actions |
| Full Food quote in ledger and forecast | One exception summary; hover timing/quote and City details exact manifest/last upkeep |
| Expanded opening paragraphs | Collapsed optional Guide during planning, with its existing production/recruitment/homes links |
| Selected clearing management while committed | Unready restores the retained selection; read-only Army/Worldtree/City details stay available |
| Three disabled cards after selecting reward | Compact accepted-choice and team-choosing count; original result remains inspectable |
| Combat economy ledger and completed preparation ladder | Hidden until relevant planning returns; combat keeps speed/pause and damaged Keep context |
| Redundant Show arena during live combat | Hidden; actual combat is already displayed; preview navigation outside live combat remains |
| Construction / land / building operations | Same nine clearings, five construction categories, confirmation, upgrade/sell, recruitment, Market bundles and Town Hall routes |
| Homes, troops, recovery and research | Same Troops & homes workspace (six homes distinct from nine clearings), member/history/recovery comparison/retirement and all 18 technologies |

For the matched two-client initial state, primary actions drop from **12 to 9**.
The count includes Menu/Settings, Guide/navigation, arena/history and Ready/Pause;
it excludes inspected-city/team selectors and noninteractive labels. The forecast
shrinks from seven logical lines of facts to three, without changing font size.

Default idle planning removes one permanent top-level action (Settings) and the
routine feedback/history action beside Ready, collapses two guide actions into one,
and removes the repeated ledger funding block and forecast income/transition copy.
Live combat removes the entire ledger, completed preparation ladder, and the arena
switch. Reward acceptance removes all three disabled cards. These are actual
information/control removals, without deleting management capabilities.

## Evidence and verification

Baseline native captures are under `artifacts/refinement-ii/baseline/`: `gameplay/`
contains the default 720p/1080p selected/idle, combat, draft and terminal views;
`cooperation/` contains 2–6-player Ready, real elimination and observer/terminal
views; `phases/` captures the same two-client planning → Ready → combat → choice →
accepted-waiting → next-planning sequence used after the change. `campaign.json`
reproduces the twenty original five-policy/two-seed campaign comparisons before
any later rules change. Match IDs/seeds and per-wave costs/consequences are retained.
Enhanced command/event archival for #25 is still required before #21 changes rules.

Current phase captures and raw receipts: `artifacts/refinement-ii/r1/phases/`.
`tools/hud-phases.py` owns two real ENet clients, disposable settings/resume data,
bounded waits, whole-process cleanup and actual viewport captures at both sizes.
It builds/recruits through legal commands and uses native pointer Ready/reward
activation. It asserts absence of irrelevant combat regions and acknowledged
reward waiting without a chooser or premature Ready. Scenario setup commands and
accelerated combat are explicitly scripted functionality, not human pacing evidence.
The wider existing gameplay suite also covers keyboard/controller focus, full
catalogs, scrolling, allied read-only state and ordinary menu/exit paths.

`GameplayCapture` writes a JSON sidecar inventory of actual visible regions and
native action text/enabled state. `ControlProbe` exposes the same regions for
cooperative assertions. `mise run capture-hud` reproduces the phase sequence.

Validation passed: locked zero-warning build; 46 Core, 145 Shared, 92
session/policy and 19 Pillow tests; default/empty/corrupt-art native session tests;
four full default/debug gameplay captures at 720p/1080p and their seven-wave
network replay; five-size settlement input/captures; 1–6-client multiplayer;
private reconnect across phases; 2–6-client cooperation including real elimination;
artwork captures; deterministic animation contracts and both renderer captures.
The final focus correction was additionally verified by three actual session
process tests and the native phase sequence, with assertions for visible focus
when Ready or the chooser disappears. CI now runs the phase sequence too.

Current focus/phase captures are in `artifacts/refinement-ii/r1/focus-phases/`;
matched before/after images, receipts and logs are retained alongside the baseline.
The [test execution note](TEST_EXECUTION.md) records a necessary test-collection
serialization change, the aborted/unresponsive Shared attempts and passing full
rerun. A Godot debug-process startup attempt also aborted with
`malloc(): unaligned tcache chunk detected`; the isolated complete UI rerun passed.
Neither failure is silently reclassified as an assertion pass or a proven JIT cause.

## Experiential validation and qualification limits

Visual review inspects actual application PNGs. Scripted native interaction proves
functionality and layout, not comprehension, enjoyment or human decision pacing.
No human comprehension/playtest claim is made. There is no physical audio/controller
or GPU qualification, native Windows/macOS execution, or live Steam qualification.
The known intermittent native .NET/JIT crash risk remains. These limits do not turn
unexecuted observations into completed evidence.

Remote CI run 49 found two timing assumptions in inherited probes: the selected
wave can legally finish before tick 180 with different personal reward offers,
and a missing-host wait of 22 seconds included native startup as well as the
unchanged 15-second admission deadline. The dense native capture now requests
an exact tick 60 boundary while preserving its actor/read-only assertions; the
probe accepts a bounded explicit capture tick and keeps 180 as its default.
The absent-host observation budget is 45 seconds, without changing transport's
admission timeout. Both seven-wave native network checks and reconnect passed
with these probe repairs before pushing the CI follow-up.
