# Gameplay parity verification

Environment: Linux x86_64, pinned .NET 10.0.401 / Godot .NET 4.7.2, owned Xvfb
screens with Mesa software rendering, disposable settings/private session paths,
Dummy audio in automation. No packages or game artwork were installed/generated.

The unchanged pinned Core supplies every gameplay mutation. All new checkpoints
use paid commands and bounded combat steps; no presentation-only authority fixture
is used as reachability evidence.

## Test coverage

- Shared adapter tests cover complete command payloads, context capture, exact local
  retry, invalid inputs, pause/speed/readiness, persistent army land and tower sale.
- Paid economy replay constructs every catalog building, uses upgrade quotes and
  actual half-investment refunds, and submits all affordable Market resources.
- Mixed-army replay recruits all four classes through actual trainers, buys homes,
  deploys/regroups members, tests tower exclusion, stores/returns/retires veterans.
- Reserve replay earns wounded stored veterans and completed paid recovery,
  independent capacity/healing levels, and authoritative production healing.
- Six paid research paths buy every one of the 18 technologies through the adapter;
  additional tests cover unavailable mastery, duplicate/exclusive purchases and
  synchronized production output.
- Final-preparation replay includes next enemies, 18 field members, only 16 funded,
  two sitting out and a purchased tower. Final production remains outside stock
  until Ready settles it; actual upkeep then matches the prior forecast.
- Default-rule campaign replay reaches Victory at wave 20 with 60 productions and
  the accepted wave-19 choice retained as historical run data. There is no new final
  draft. Uninvested default Keep defense reaches actual Defeat without offers.
- Native checks activate construction, sale, trades, technology purchase, deployment,
  roster inspection, store/return and reward Controls. They check retained focus,
  modal shade/scroll behavior, no pause from dialogs, forecast world picking,
  read-only forecast actions, and reopening unchanged draft offers after inspection.
- The new actual host/guest process test plays seven paid cooperative waves with
  distinct economies, independent drafts and ordinary snapshots. It checks
  ready/unready, global pause, guest speed rejection, host speed, remote bundle/
  resource/technology/member/squad/cell arguments, exact remote trade retry and
  native allied-tree inspection with purchase disabled. The existing 2–4-player
  and reconnect/fault suites remain intact and are included in the full test task.

## Graphical evidence

`mise run capture-gameplay-ui` produces actual application PNGs under
`artifacts/captures/gameplay/`, with large captures under `1920/`. It includes the
catalog, stocks/upkeep, Market, upgrades, roster/squads/deployment, Town Hall,
Worldtree, unit inspector, forecast/arena, partial funding, run, draft, Victory,
Defeat and a real remote client's allied-city tree. No screenshot mockups are used.

Capture review led to expanded plot details, distinct roster/recruitment/Town Hall
sections, compact arena labels and terminal outcome text above acquired-card lists.
Native scroll areas intentionally contain longer catalogs/rosters/research; clipped
rows at a scroll viewport boundary are scrollable, not lost controls.

## Executed results

All following checks completed successfully on 2026-10-18:

| Gate | Result |
| --- | --- |
| `mise run build` | Locked restore; zero warnings/errors |
| `mise run test-core` | 40 passed |
| `mise run test-shared` | 62 passed, including 18 new adapter/parity cases |
| `mise run test` | 132 .NET tests passed: 40 Core, 62 Shared, 25 Godot logic/Steam/animation, 5 real-process integration facts; 14 Pillow tests passed; 19 named combat phases in each art mode and fallback captures passed |
| `mise run test-gameplay-ui` | Native gameplay suite and seven-wave host/guest verification passed |
| `mise run capture-gameplay-ui` | Native captures at both sizes and remote allied inspection passed; final additional tower-inspection/branch captures passed through the same graphical harness |
| `mise run capture-art` | All nine original art/fallback/showroom views passed |
| `mise run build-linux` / `build-windows` | Both final self-contained x64 exports succeeded |
| Exported Linux gameplay suite | Passed from a temporary working directory outside the checkout; executable-relative assets/audio and all native gameplay checkpoints loaded |
| Source integrity | Core, assets/art/audio, tool pins and RID locks unchanged; `git diff --check`, shell syntax and Python compilation passed |

The 5 integration facts include the original native session/art/audio/focus loops,
2–4-player LAN and reconnect/fault cases, plus the new gameplay UI/host-guest fact.
The new native suite additionally covers tower selection → plot actions → accepted
Bombard branch, closing/reopening the draft without choosing, and partially funded
readiness. The final focused native suite passed after the last UI changes.

A full default-rule campaign reached wave-20 Victory with all 60 productions. The
ordinary defeat scenario reached wave-1 Defeat. The mixed scenario contains all
four classes, the reserve scenario contains paid wounded stored veterans, and the
forecast scenario has 16 funded participants and two sitting out. No grants, direct
simulation setters or fake authoritative snapshots were used.

Actual 720p and 1080p panels were inspected. Catalog/Market text, roster membership,
Town Hall quotes, research locks, forecast funding, reward offers, tower profiles,
allied read-only state and terminal summaries are readable with native scrolling.
`artifacts/captures/gameplay/contact-sheet.png` is an overview made only from these
actual captures. Full-size source PNGs remain alongside it.

Retained transcripts are under `artifacts/test-logs/gameplay-parity/`. An earlier
full run passed its .NET/process/animation checks but failed at shell completion
when the graphical script was edited while running; the unchanged-script full
restart passed. Two early legal test assumptions (Town Hall affordability and
historical final-wave offers) were corrected to match actual accepted Core state.
The requested live-platform gates below remain separate from these local results.

## Qualification limits

Live two-account Steam invitations, overlay/gameplay/relay/authenticated reconnect,
native Windows/macOS execution, physical GPU/input and real audio hardware remain
unexecuted. Cross-builds and Linux LAN/offline tests do not qualify them. The known
intermittent native .NET/JIT crash risk remains unresolved. Remote GitHub Actions
was not triggered; this task changes the local working tree only.

## Refinement roadmap

The [combat readability review](COMBAT_READABILITY.md) documents issue #4, its
factual accounting boundaries, native inspection checks and real 720p/1080p
solo/cooperative captures. The [roadmap checkpoint](REFINEMENT_PROGRESS.md) tracks
subsequent issue work separately from the original parity verification above.

The [visual army/deployment review](ARMY_DEPLOYMENT_REVIEW.md) covers issue #5:
six visible native home targets, legal placement/capacity projections, retained
member context, Food forecasts, remote pending/rejected commands and allied
read-only inspection at both capture sizes.
