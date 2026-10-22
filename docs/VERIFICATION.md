# Verification

## Forest consistency and approved mappings — roadmap #13 / issue #11 (2026-10-23)

The [forest presentation review](gameplay/FOREST_PRESENTATION_REVIEW.md) records
the inventory, source hashes, mapping choices, level cues and visual evidence.
Enemy Berserker now has its own catalog entry, cropped by the existing pinned
Pillow pipeline from the approved raised-arm orc pose on sheet 04. Watchposts
reuse the supplied defense tower at 82 pixels, with their own label/health;
ArrowTower remains 96. All four structure kinds have explicit paths. No source
painting is generated, repainted, recolored or downloaded. Every previous 46
catalog record and PNG hash remains identical; the new crop is entry 47.

Native typography and secondary forest surfaces are consistent across doctrine/
reward cards, roster rows, disclosures and inspector/dialog closes. Gold roster
selection remains independent of cyan focus. Quiet session links preserve
feedback/history; reconnect is shown when disconnected. Emerald Ready/choices,
violet Pause, the original painted frames, scene and building art remain intact.
Screen-sized `L#` foundation annotations expose accepted building levels in both
map and debug paths, after all tall sprites; hit geometry and anchors do not
change. Native pointer production/upgrade pays exactly once and changes L1→L2;
a legal late checkpoint shows L5.

Executed checks: locked zero-warning build; 46 Core / 137 Shared / 19 Pillow tests;
six focused forest cases (sprites/debug at 720p/1080p plus real missing/corrupt
native decode paths); three session graphical facts repeated on the installed
47-entry catalog; four ordinary/debug full UI passes plus seven-wave cooperative
UI worker; five-size nine-pad picking/hover/fixed-camera checks; reconnect/faults;
12 animation contracts and actual native phases. Artwork/showroom and ordered
battle sequences are refreshed after installation. Artifact logs/captures are
under `roadmap-11-*` and `artifacts/captures/forest-consistency/`. Shell/Python
syntax and `git diff --check` pass. Core, pins, source art, WAV/loop markers,
credentials/settings formats and RPC/session ownership remain intact.

These are local Linux/Xvfb/Mesa/Dummy-audio observations and actual screenshots,
accepted by the user as roadmap evidence. No human comprehension/enjoyment,
physical hardware, live Steam or Windows/native-platform qualification is
claimed. The intermittent native .NET/JIT risk remains applicable.

## Cooperative readiness and observers — roadmap #13 / issue #10 (2026-10-23)

The persistent native team panel exposes each city’s readiness, personal reward
choice, pause/away/fallen state, Keep and Food/army warnings. Allied buttons use
the existing viewed-city path; native pointer Ready still acts only on the local
authenticated city. Playback cell locations supply actual cross-sector cues.
Fallen players get an explanation/watch-ally shortcut and no Ready button;
fallen/terminal veteran inspection no longer promises future recovery/purchases.
See the [cooperation review](gameplay/COOPERATION_REVIEW.md) for the retained
failure curve, methods and limits.

Twenty legal uneven-group runs cover 2–6 players, two seeds and 220 battles.
Replay assertions preserve original enemy allocations, future fallen-army
exclusion and clear rewards. Ten full-policy groups with an idle city win,
including a four-city W18 fall followed by victory. Deliberately limited groups
lose in 1–3 waves; post-fall combat intervals range from 1139–5280 ticks. This is
not proof of inevitable loss or human balance. Keep the current rules; record
the 88-second simulated tail and recoverable last stand as explicit tradeoffs.

Executed local checks: locked zero-warning build; 46 Core and 135 Shared tests;
three session graphical facts; real 1–6-client admission/readiness/combat/draft;
reconnect through preparation/combat/reward and fault cases; four ordinary/debug
720p/1080p UI passes and the seven-wave cooperative UI worker (30 actors/11 cues);
14 art views; 12 animation contracts and native phases; ordered battle frames
in both renderers plus artwork/camera fallback. Shell/Python syntax and
`git diff --check` pass. The new dedicated `capture-cooperation` records 2–6-city
720p/1080p layouts, split readiness, allied pointer/local Ready, pause, Away/private
resume, real low Keep, early elimination, observer navigation and terminal state.
Its receipts distinguish screenshot waiting/capture pauses from accelerated
active spectator time. Exact locally generated receipts/logs live in
`artifacts/captures/cooperation/`, `artifacts/observations/cooperation/` and
`artifacts/test-logs/roadmap-10-*`.

The user accepts local tests, legal replays and actual screenshots as roadmap
evidence. Live Steam, Windows/native-platform, physical controller/audio and
human group observations remain unexecuted; no comprehension/enjoyment/balance
claim is made. The known intermittent native .NET/JIT risk remains applicable.

## Preparation clarity — roadmap #13, initial priority #3 pass (2026-10-22)

Completion follow-up: the requested acceptance method is local tests, legal
replays and actual screenshots. The [preparation review](gameplay/PREPARATION_REVIEW.md)
now records nine stages for solo and both cooperative participants, including
actions, empty stages, duration and Ready waiting. All have zero missed sequences.
The opt-in bounded `--preparation-observations` probe excludes paused/away time
from waiting and stores no credentials/commands/snapshots. Three deterministic
observation contracts, a three-wave native solo run, the seven-wave cooperative
worker and a fresh three-process session suite passed. Build/Core (46)/Shared
(88) were rerun successfully. Existing production/research/recovery rules remain
intact; the local evidence does not support replacing the three-stage rhythm.

Ready names the next preparation or battle, and cooperative readiness counts only
connected, standing cities. An allied forecast follows the inspected city while
Ready remains labeled for the authenticated local city. A retained native frame
groups the destination-filtered enemy manifest (including bosses), next production,
authoritative participation, field sit-outs, storage and projected Food payment.
Stages 1–2 explicitly use current stock; stage 3 includes final production.
The resource tooltip no longer implies that every forecast includes production.
All three settlements, research/recovery, rules, transport, assets and pins remain
unchanged. No human pacing or balance result is claimed; the
[playing guide](gameplay/PLAYING.md) records the outstanding playtest questions.

Native checks exercise pointer Ready through all three boundaries, one production
per boundary, final forecast/upkeep agreement, controller pause/resume, partial Food
funding, forecast/army inspection, native focus and the existing campaign UI.
Host/guest checks retain pending/rejected/disconnected controls and add readiness
wording, authenticated allied inspection and waiting/allied preparation captures
at 720p/1080p. Only legal commands earn scenario snapshots.

| Executed check | Result |
| --- | --- |
| `mise run build` | Passed; zero warnings/errors, locked dependencies |
| `mise run test-core` | 46 passed |
| `mise run test-shared` | 88 passed |
| `mise run test-session` | Final repeat: three actual graphical process tests passed |
| `mise run capture-art` | Final repeat: 14 actual viewport captures passed |
| `COMMON_WATCH_GAMEPLAY_ART=illustrated mise run capture-gameplay-ui` | Final 720p/1080p Controls/captures and seven-wave illustrated host/guest replay passed |
| `mise exec -- bash tools/graphical.sh test-gameplay-network` | Final plain host/guest replay, waiting/allied captures, pending/rejection/disconnection and authenticated Unready passed |
| `mise run capture-settlement -- --hud-art illustrated` | All nine clearings, native purchase/construction, fixed camera and five window sizes passed |
| Python compile, `git diff --check`, protected-path comparison | Passed; Core/Shared, source/installed artwork, music and tool/dependency pins have no changes |

The full default capture task also passed all four solo native runs (plain and
illustrated, both sizes) during iteration. Its added cooperative probe initially
failed presentation/layout sequencing; the final isolated plain worker and full
illustrated capture task above passed after the fixes. A remote Unready check
also exposed an acknowledgment/snapshot race in the probe; it now waits for the
accepted command's city snapshot before checking readiness and turn.

The first expanded-ledger capture failed its separation assertion at 720p. Later
cooperative checks caught a crowded paused/allied action frame; compact phase
wording, a single pause indicator and local-city labels keep the complete native
targets separate from the ledger. An initial top-center summary covered locked
clearing 7, so the summary now occupies the left scenery margin below Settings
and yields to selected-plot management. The actual nine-clearing picking/purchase
suite passes at all five window sizes. The cooperative capture probe also waits
for local combat presentation to complete before pausing, rather than freezing
the previous battle and expecting preparation Controls. Actual preparation
and shortage images are under `artifacts/captures/gameplay/forest/` and `1920/`;
plain equivalents are under `artifacts/captures/gameplay/`. Cooperative images
use `network-plain/` and `network-forest/` in that directory. Logs use the
`artifacts/test-logs/refinement-` prefix.

Linux automated checks use owned Xvfb displays, Mesa and disposable settings with
Dummy audio. Standalone multiplayer/reconnect suites, release exports, remote CI,
live two-account Steam, native Windows/macOS and physical GPU/controller/audio
checks were not run for the initial presentation pass. Reconnect was rerun for
the completion follow-up. The known intermittent native .NET/JIT crash risk
remains unresolved. Human timing/balance review is optional further evidence;
the remaining roadmap priorities are tracked in
[the compact checkpoint](gameplay/REFINEMENT_PROGRESS.md).

## Painted forest HUD (2026-10-21)

The [painted HUD report](PAINTED_FOREST_HUD.md) links the source delivery and actual
before/after screenshot review. Starting HEAD was
`45741e3a6d3a29af4b3b6daf64b4b5293ca7bcd3`; eight existing working-tree changes were
preserved. Fresh baseline build, 46 Core, 88 Shared, 19 art and three actual-process
session tests passed before implementation. Baseline and final matched captures
use the same legal scenarios at 720p/1080p, angled sprites, overlay off and
environment effects Off.

| Executed check | Result | Receipt under `artifacts/painted-forest-hud/` |
| --- | --- | --- |
| `mise run build` | Passed; zero warnings/errors | `build.log` |
| `mise run art-painted-hud` | 48 exports validated: RGBA, padding/edges, hashes, state registration, anchors, slices, technical SVG contents | `asset-validation.log` |
| `mise run test-core` | 46 passed | `test-core.log` |
| `mise run test-shared` | 88 passed; final normal task wrapped in a 90-second supervisor | `test-shared-final.log` |
| `mise run test-art` | 19 passed | `test-art.log` |
| `mise run test-session` | Three actual-process tests passed | `test-session.log` |
| `mise run test` | 46 Core, 88 Shared, 70 session/Steam/animation logic and six process tests passed, plus 19 art tests and debug/sprite battle-frame verification | `test.log` |
| `mise run test-gameplay-ui` | Illustrated and plain UI, native pointer/controller checks and seven-wave host/guest worker passed | `test-gameplay-ui.log` |
| Illustrated `mise run capture-gameplay-ui` | Native UI captures at 720p/1080p and network worker passed | `capture-gameplay-ui-final.log` |
| Explicit Off `tools/graphical.sh capture-gameplay` | Final matched production frames passed and copied to `after/gameplay/` | `capture-matched-last.log` |
| `mise run capture-settlement` and illustrated Off variant | All nine clearings, clear selection, purchase, native picking, fixed camera, both projections and five window sizes passed | `capture-settlement.log`, `capture-settlement-illustrated.log` |
| `mise run capture-worldtree` | 18 sockets, hover/focus, prerequisites, inspection, purchase and acquired states at both resolutions passed | `capture-worldtree.log` |
| `mise run capture-art` | 14 actual Godot frames passed | `capture-art.log` |
| `mise run test-multiplayer` | One through six actual Godot processes passed | `test-multiplayer.log` |
| `mise run test-reconnect` | Reconnect/fault suite passed across Building, Preparation, Combat and RewardDraft | `test-reconnect.log` |
| Illustrated `tools/graphical.sh test-gameplay-network` | Actual pending, rejected and disconnected feedback at both resolutions, allied inspection and seven-wave replay passed | `capture-network-illustrated.log` |

New checks use native pointer Ready through preparations 1, 2 and 3, pointer Pause
and controller accept to Resume. Existing card/confirmation, Gold recovery,
focus/scroll retention and menu assertions remain. Camera reset now asserts the
new safe framing scale **and** origin. All 470 protected source/runtime files
retain their original hashes (`preservation-receipt.json`). The screenshot inventory
includes only captures recorded by the fresh baseline/final logs; the review script
passes `node --check`. Proof and contact-sheet compositions are labeled separately
from actual gameplay PNGs.

Intermediate failures caught collapsed scrolling, clipped confirmation and
inspector overlap and led to layout fixes. One intermediate 1080p capture failed
`Esc closes details` during resize; subsequent complete captures passed without
changing menu/session policy. A late Shared repeat stalled with a busy test host;
that host was explicitly terminated. The earlier full suite, a bounded diagnostic
rerun (88 passed in 16.39s) and the final normal Shared task (88 passed) succeeded.
The aborted and diagnostic receipts remain separate; no Core/Shared changes were
made to make tests pass.

The pre-existing [CI run at the starting commit](https://github.com/seraph1nia/common-watch-v2/actions/runs/39656535912)
failed confirmation focus restoration and logged missing `rg` on the runner. No
fresh CI run or fix is claimed. Linux/Xvfb/Mesa and Dummy audio do not qualify live
Steam, Windows/macOS execution or physical GPU/controller/audio hardware. Release
exports were not rerun. The known intermittent native .NET/JIT crash risk remains.

## Escape menu and session exit confirmations (2026-10-21)

Escape now opens the session menu after any open Settings/details/rewards panel
is dismissed. Return to main menu and Exit Game require native confirmation,
with separate solo, host, guest and no-session consequences. Cancel is focused
initially; Cancel/Escape restores the invoking button without leaving or pausing.
Confirmed return releases the session and world views; confirmed exit retains
the existing session cleanup and audio drain.

Executed locally: `mise run build` (zero warnings/errors), `test-core` (46),
`test-shared` (88), `test-session` (3 actual graphical process tests), and
`mise exec -- bash tools/graphical.sh test-gameplay` (default and debug), and
`mise run capture-art` (14 viewport captures).
The graphical checks exercise Escape entry, native button clicks, confirmation
cancellation/focus, unchanged combat state, cleanup, role-specific warnings and
confirmed process exit. An initial overlapping build/test invocation hit an
MSBuild file lock; the sequential Core rerun passed.

A default smoke run with `--gameplay-captures` recorded both confirmations at
720p/1080p under `artifacts/captures/godot/exit-menu/`; return at 720p and exit at
1080p were visually inspected. Logs: `artifacts/test-logs/escape-menu-*.log` and
the session suite's `godot-*.log` files. Linux checks used owned Xvfb displays,
Dummy audio and disposable settings/private guest credentials. Multiplayer and
reconnect suites, exports, live Steam, native Windows/macOS and physical GPU/audio
were not rerun. The known intermittent native .NET/JIT crash risk remains unresolved.

## Forest UI refinement and menu popup (2026-10-21)

The [file-level report](FOREST_UI_REFINEMENT.md) contains real before/after Godot captures at 720p/1080p, exact command receipts, the artwork preservation check and platform limitations. Build, Core (46), Shared (88), Pillow/art (19), session process checks (3), gameplay UI/captures in both presentations, settlement/Worldtree/art captures, 1–6 player multiplayer, reconnect and offline Steam/session logic (61) passed locally. The menu now opens as a framed square over the retained screen with no backdrop or dimming; native checks cover edge clicks, focus, controller cancel and unchanged outside pixels.

One native .NET/JIT crash and one MSBuild child-node failure occurred during iteration; failed logs are retained. The final command receipts are successful. CI run `39560834801` remains a separate stale phase/turn opening rejection; no CI fix or fresh CI pass is claimed. Live Steam, native Windows/macOS and physical hardware remain unverified.

## In-game weather selection (2026-10-21)

Settings now offers Scene default and all seven weather presets. Selection applies
immediately, including while paused, and persists with environment quality. Old
quality-only files still load. CLI overrides remain temporary unless explicitly
chosen in Settings; seed and capture time are not persisted.

Executed locally: `mise run build` (zero warnings/errors), `test-core` (46 passed),
`test-shared` (88 passed), focused `EnvironmentSettings` tests (35 passed),
`test-session` (3 actual graphical process tests passed) and `capture-art`
(14 viewport captures). The native settings checks exercise every preset,
quality changes, persistence, scene defaults and external preset synchronization
without changing authoritative state. A further `test-defaults` smoke run captured
the settings panel at 720p/1080p; both images were visually inspected.
Logs: `artifacts/test-logs/weather-settings-*.log`; new panel images:
`artifacts/captures/godot/weather-settings/settings-weather-{1280,1920}.png`.

Checks used Linux/Xvfb/Mesa with disposable settings and Dummy audio. Full
`test-environment`, multiplayer/reconnect, exports, live Steam, native Windows/macOS
and physical GPU/audio checks were not rerun for this settings change. No native
crash occurred in the completed checks; the known intermittent native .NET/JIT
risk remains unresolved.

## Ordinary play and export defaults (2026-10-21)

`mise run play` and a no-argument export now use the same sprites/illustrated/
angled defaults, with diagnostics off. `play-debug` explicitly selects
artwork-independent debug/plain/topdown. `play-assets` remains a compatibility
alias, and interactive LAN development also uses the normal game defaults.

Executed locally on Linux:

- `mise run build`: passed with zero warnings/errors.
- `mise run test-core`: **46 passed**; `mise run test-shared`: **88 passed**,
  including defaults, explicit overrides/alias ordering and art-independent rules.
- `mise run test-session`: **3 actual-process tests passed**. A new launch has
  no presentation or checkpoint flags, verifies the production main menu, then
  exercises doctrine/start, purchases, recruitment, combat, rewards, settings,
  720p/1080p resize and native picking. Explicit debug/plain, installed artwork,
  empty artwork, corrupt artwork and illustrated-HUD fallback remain covered.
- `mise run test-gameplay-ui`: passed in both no-flag default presentation and
  explicit debug/plain/topdown, plus the seven-wave host/guest replay. The
  existing production Controls read detached snapshots and submit authoritative
  commands; no scenario-specific gameplay screen was found or added. Showcase
  and showroom controls remain explicit development tools.
- `mise run capture-art`: **14 actual viewport captures** passed, covering
  both projections, plain/illustrated HUD, 720p/1080p and the asset showroom.
- `mise run test-multiplayer`: passed for **1–6 players**.
  `mise exec -- bash tools/reconnect.sh`: passed reconnect/fault probes with
  the already-built client and explicit debug/plain presentation.
- `mise run build-linux`: passed. The self-contained executable then passed
  `--smoke-test --expect-installed-art --expect-default-presentation` from a
  temporary directory outside the repository, with no asset/audio root flags.
  This verifies executable-relative artwork and soundtrack loading as well as
  matching presentation, production controls and native resize/input behavior.

Graphical checks used owned Xvfb displays, Mesa software rendering, Dummy audio
and disposable preferences/private session data. Logs are
`artifacts/test-logs/play-defaults-*.log`, `godot-defaults.log` and
`export-defaults.log`; images are under `artifacts/captures/godot/art/`.
An initial combined mise invocation treated the second task name as an argument;
separate multiplayer and reconnect invocations passed. No native .NET/JIT crash
occurred in these checks; the known intermittent risk remains unresolved.
The full `mise run test` suite, profilers, live Steam, native Windows and physical
GPU/audio verification were not run for this change.

## Retained environment rendering and exact preset parsing

Final focused review checks passed with scoped `DOTNET_TieredCompilation=0`
and `MSBUILDDISABLENODEREUSE=1` (no repository defaults or pins changed):

```sh
mise exec -- bash -c 'dotnet restore src/Game.Godot/Game.Godot.csproj --locked-mode && dotnet build src/Game.Godot/Game.Godot.csproj --no-restore --nologo && dotnet restore tests/Game.Godot.Session.Tests/Game.Godot.Session.Tests.csproj --locked-mode && dotnet test tests/Game.Godot.Session.Tests/Game.Godot.Session.Tests.csproj --no-restore --filter FullyQualifiedName~EnvironmentSettings --nologo && bash tools/graphical.sh capture-environment'
```

- Project build: zero warnings/errors. Environment settings: **34 passed**,
  including all seven exact lowercase presets, rejected composite/numeric/case/
  alias/unknown values, and retained missing-value diagnostics.
- Actual Godot 4.7.2 OpenGL Compatibility/Xvfb/Mesa checks passed with Dummy audio
  and disposable preferences. `GODOT_ENVIRONMENT_INVALIDATION_PASS` observes no
  unchanged native draw callbacks or shader submissions during pause, native
  focus loss, fixed samples, stationary camera poses and hidden debug scenes.
  Pan/zoom/resize, quality, immediate presets and independent controls invalidate
  correctly; repeated frames restore exact pixels. Explicit resampling changes
  effects without rebuilding layer geometry. Fire/crystal glow and smoke paths
  are exercised. The X11-only test focus driver selects its own native window
  explicitly because bare Xvfb has no window manager; it restores focus on exit.
- All prior live-clock, shader-identity and integrated-motion regressions passed,
  plus the 16 arena combinations, sampled animation, quality/Off restoration,
  unchanged combat tick, settlement presets/picking, menu/lifecycle, texture cache,
  and missing/debug fallback captures. The final graphical log has no shader
  errors. No native .NET/JIT failure occurred in the final checks.
- The first attempt stopped at a span/await overload compile error in the new
  assertions. After correcting that test code, build/parsing passed but the
  window-manager-dependent focus setup failed. The explicit native focus driver
  corrected the probe; the final scoped build/capture retry passed. The complete
  attempt history is retained in `artifacts/test-logs/environment-review-r4-r5.log`.

Refreshed `performance.json` has 32 whole-client Off/Full records (30 sampled
frames each), with zero steady texture decodes:

| Resolution | Off median range (ms) | Full median range (ms) | Maximum Full p95 (ms) |
| --- | --- | --- | --- |
| 1280×720 | 6.48–12.97 | 7.87–15.24 | 15.98 |
| 1920×1080 | 12.12–19.15 | 14.57–26.38 | 27.51 |

Mean process-wide managed allocation was about 458 KB/frame Off and 459 KB/frame
Full, including existing snapshot/presentation work and capture scheduling.
These are Linux software-renderer observations, not isolated effect/GPU timings
or physical hardware certification. The static regressions establish retained
command reuse, not zero compositor cost for paused visible effects.

Artifacts for copying after synchronization are in this validation worktree:

- `/home/bart/.no-mistakes/worktrees/3d5a80f8df35/01M5DR26EJTFEN4FG3SAP70PP4/artifacts/captures/godot/environment/`
  contains refreshed PNGs (including `invalidation-*.png` and shader identity
  pairs) and `performance.json`.
- `/home/bart/.no-mistakes/worktrees/3d5a80f8df35/01M5DR26EJTFEN4FG3SAP70PP4/artifacts/test-logs/`
  contains `environment.log` (final capture run) and
  `environment-review-r4-r5.log` (all focused attempts).

The complete test/lint suites and other pipeline phases were not run in this
review fix round. Native Windows, live Steam and physical GPU verification remain
unexecuted; the known intermittent native .NET/JIT risk remains unresolved.
Publication and remote CI remain outside this local validation task.

## Dynamic environment review regressions

Focused review verification passed with scoped `DOTNET_TieredCompilation=0`
and `MSBUILDDISABLENODEREUSE=1`:

```sh
mise exec -- bash -c 'dotnet restore src/Game.Godot/Game.Godot.csproj --locked-mode && dotnet build src/Game.Godot/Game.Godot.csproj --no-restore --nologo && bash tools/graphical.sh capture-environment'
```

The project build had zero warnings/errors. Godot 4.7.2 ran the actual OpenGL
Compatibility capture workflow on an owned Xvfb/Mesa llvmpipe display with Dummy
audio and disposable preferences. `GODOT_ENVIRONMENT_REGRESSION_PASS` confirms
live `Sample(null)` showcase play/pause/unpause, integrated two-axis layer and
particle motion at 600 seconds through rain/snow/clear transitions and signed
speed changes, deterministic explicit samples/live handover, and six graphical
neutral-shader identity comparisons using supplied translucent fire/crystal
textures with vertex colors, modulation and self-modulation. Each identity
comparison allows at most one 8-bit rounding step per channel.

All 16 arena combinations (1/6 players, local/overview, topdown/angled,
720p/1080p), quality/Off restoration, repeated sampled frames, unchanged combat
tick, settlement presets/picking, menu/lifecycle, steady texture cache and
missing/debug fallback captures also passed with no shader errors.
Evidence is in `artifacts/test-logs/environment.log` and
`artifacts/captures/godot/environment/`, including `identity-*-reference.png`,
`identity-*-shader.png` and `performance.json`.

This review round did not run the complete suite or qualify native Windows,
live Steam or physical GPUs. The known intermittent native .NET/JIT risk remains;
the scoped environment variables do not change repository defaults or fix it.

## Modular battle arena — 2026-10-20

Implemented the supplied Rain / Ember pack; see the
[rendering, import and capture contract](MODULAR_BATTLE_ARENA.md). This checkout
uses 6×9 per sector, rather than the pack report's 7×9. Rendering derives the
repeat width from snapshot cells. Core and dependency/tool locks are unchanged;
the initial worktree was clean and no commits or pushes were made.

| Executed check | Result / evidence |
| --- | --- |
| `mise run build` | Passed, zero warnings/errors; `artifacts/modular-final-build.log` |
| `mise run test-core` | 46 passed; `artifacts/modular-core.log` |
| `mise run test-shared` | 88 passed, including six legal roster recordings and 6/7-column projection distances; `artifacts/modular-shared.log` |
| `mise run test-session` | Both actual-process facts passed: installed, absent and corrupt artwork, lifecycle, native controls/input, settings and audio; `artifacts/modular-session.log` |
| `mise run test-animation` | 8 tests passed; 19 event/pose capture moments verified in each of debug and sprite modes, plus missing-art/shader-off captures; `artifacts/modular-animation.log` |
| `mise run capture-arena` | All 24 combinations passed (1–6 sectors, topdown/angled, 720p/1080p), with 116 actual viewport PNGs; `artifacts/modular-arena-run.log` |
| `mise run test-art`, `mise run art-validate` | 19 Pillow tests passed; 45 installed/staged asset records validated; `artifacts/modular-art.log` |
| `mise run art-process`, `mise run art-install` | Completed; original 29 catalog records unchanged and 16 byte-preserved component entries appended. Rechecked every original pack checksum and runtime component byte equality. |
| `mise run test-gameplay-ui` | Native UI and seven-wave real host/guest checks passed, including squad/deployment, allied inspection, pause, speed and retry; `artifacts/modular-gameplay.log` |
| `mise run test-multiplayer` | All rosters 1–6 passed in real Godot processes with identical wave-two snapshots; `artifacts/modular-multiplayer.log` |
| `mise run test-reconnect` | Passed preparation/combat/death/draft resume, ownership, credentials, sequence continuity, protocol refusal and shutdown; `artifacts/modular-reconnect.log` |
| `mise run capture-art` | Passed; existing settlement, HUD, combat, draft and showroom captures; `artifacts/modular-capture-art.log` |
| `git diff --check` | Passed |

The arena probe submits accepted commands under default rules and replays actual
combat. Its [24 receipts](../artifacts/captures/godot/modular-arena/verification.json)
record 54–324 real cells, actual structure-hit/destruction ticks and cross-sector
movement at tick 1201 for rosters 2–6. Native wheel/drag poses persist through
refresh, F fits the whole field, every cell center survives camera inversion,
and moving actors/structures pick at their projected positions in both views
and resolutions. Actual screenshots were inspected for joined ground/bands,
outside-only caps, proportional upright sprites and local/overview framing.

Representative original captures:

- [Six-sector overview at 1080p](../artifacts/captures/godot/modular-arena/6-sectors-angled-1920-overview.png)
- [Own-sector framing at 720p](../artifacts/captures/godot/modular-arena/6-sectors-angled-1280-own-sector.png)
- [Cross-sector movement](../artifacts/captures/godot/modular-arena/2-sectors-angled-1920-crossing.png)
- [Structure destruction](../artifacts/captures/godot/modular-arena/2-sectors-angled-1920-structure-destroyed.png)
- [Combat with the illustrated HUD](../artifacts/captures/godot/art/combat-sprites.png)

These checks qualify Linux Xvfb/Mesa with automated Dummy audio. The aggregate
`mise run test`, full ordered animation sequence (`test-animation` uses
`--no-sequence`), repeated performance profiling, release exports/runtime,
remote CI, native Windows/macOS, physical GPU/input/audio and live Steam
accounts/relay/invites were not executed for this integration. The existing
intermittent native .NET/JIT crash risk remains unresolved; these successful
runs do not establish its absence. The supplied offline PNG previews were not
counted as game verification. The optional HTML gallery uses the project theme;
its files were HTTP-checked, but Chrome review was unavailable because the
attached CDP target disappeared.

## Menu, HUD, doctrine and arena cleanup (2026-10-20)

Implemented the four-button main menu and separate LAN/Steam submenu; three personal doctrine cards with explicit selection; a fixed map beneath floating HUD controls; five construction tabs; contextual troop/home management; and the requested removal of Run, forecast, selected-plot, upkeep, collapse and history/footer presentation. Recruitment, trading, upgrades, sales, rewards, pause and host combat speed retain authoritative commands. Army-home tower inspection routes into Homes management rather than adding a settlement plot.

Architecture and compatibility:

- `Game.Shared/DoctrineOffers.cs` ranks eligible canonical `RunPresentation` cards using a versioned SHA-256 key from match identity, player identity and doctrine identity. It consumes no gameplay RNG. A new interactive match has a new GUID; refresh, settings, resize and reconnect preserve offers. The UI tracks explicit selection separately from Core's default Warden. A client process restart asks for explicit selection again while retaining the same offers.
- `GodotSession` observes newly accepted doctrine commands into a bounded host-local choice set. The host UI waits for every connected player's explicit choice; reconnect preserves that evidence. Cached command replies cannot establish new choice evidence. This presentation gate does not change Core's ordinary development command API or the existing disconnected-player fallback.
- Settlement framing uses proportional cover over the full viewport, protected clearing bounds and native canvas inversion. Extreme aspect ratios cap the foreground scale and fill decorative outer edges with a subdued cover of the same approved map. Combat and preview share rendered bounds, figure/effect margins, a uniform 80% fit and approved artwork/primitives beneath the playable layer.
- Core changes are confined to six-column topology and compatibility versions: board **2**, combat rules **8**, wire protocol **17**. Sectors retain nine rows, six army homes, nine settlement tower footprints, Keep and Watchpost. One through six players now produce 54–324 cells. Damage, squad capacity, unit counts and prices were not changed. Changed routing can change battle outcomes; old seven-column snapshots/sessions are rejected rather than migrated.
- SessionCoordinator ownership, session generations, `/root/CommonWatch/Session`, private credentials, settings/audio persistence, supported art/view flags and fallback behavior remain. Native doctrine resize subscriptions disconnect on exit. No artwork, soundtrack, dependency pin or lock was replaced or generated.

Completed verification (Linux/Xvfb/Mesa, disposable settings and private session directories):

| Check | Result |
| --- | --- |
| `mise run build` | Passed, zero warnings/errors, locked dependencies |
| `mise run test-core` | 46 passed, including all six roster widths, footprints, deployment and floor connectivity |
| `mise run test-shared` | 74 passed, including stable personal offers, old-board rejection and deterministic replay |
| `mise run test-session` | Both graphical process facts passed; includes menu routes, explicit choice/highlight/start, resize 720p → 1080p → 720p, audio/preferences, fallback, picking and modal/focus behavior |
| `mise run test-gameplay-ui` | Passed native legal gameplay and seven-wave host/guest replay, personal offers, host choice gate and lobby reconnect |
| `mise run test-animation` | Eight animation logic tests and 19 captured phases per world rendering mode passed |
| `COMMON_WATCH_GAMEPLAY_ART=illustrated mise run capture-gameplay-ui` | Passed actual menus, doctrines, HUD, construction, army/home tower sale, research, combat, rewards and terminal states at 720p/1080p, plus host/guest replay |
| `mise run capture-settlement -- --hud-art illustrated` | Passed all nine native plot clicks/hover, ignored drag/wheel, purchase/construction and map bounds at 1280×720, 1920×1080, 1280×800, 1600×1000 and 2560×1080 |
| `mise run capture-battle -- --width 1920 --height 1080 --hud-art illustrated` | Passed both rendering modes: 19 phases and 22 ordered frames each, plus missing-art and angled/shader fallback captures |
| `mise run test-multiplayer` / `mise run test-reconnect` | Passed all six roster sizes and reconnect through building, preparation, combat, rewards and subsequent building |
| `mise run test` | Isolated retry passed: 151 .NET/process tests, 19 Pillow tests and both animation phase/fallback capture checks |
| Source review | `git diff --check` passed; actual menu/doctrine/settlement/construction/combat PNGs inspected at both target sizes |

The complete suite initially encountered an intermittent native SIGSEGV in the missing-sprite capture. `coredumpctl` identifies `libclrjit.so` on the `.NET BGC` thread, consistent with the documented native .NET/JIT risk. The isolated retry exited 0. The risk remains unresolved; diagnostics are retained in `artifacts/test-logs/ui-cleanup-native-crash.log` and `ui-cleanup-native-coredump.txt`. Completed task transcripts are `artifacts/test-logs/ui-cleanup-*.log`.

Actual illustrated captures:

| View | 1280×720 | 1920×1080 |
| --- | --- | --- |
| Main menu | [PNG](../artifacts/captures/gameplay/forest/main-menu.png) | [PNG](../artifacts/captures/gameplay/forest/1920/main-menu.png) |
| Multiplayer | [PNG](../artifacts/captures/gameplay/forest/multiplayer-menu.png) | [PNG](../artifacts/captures/gameplay/forest/1920/multiplayer-menu.png) |
| Doctrine offers | [PNG](../artifacts/captures/gameplay/forest/doctrine-offers.png) | [PNG](../artifacts/captures/gameplay/forest/1920/doctrine-offers.png) |
| Selected doctrine | [PNG](../artifacts/captures/gameplay/forest/doctrine-selected.png) | [PNG](../artifacts/captures/gameplay/forest/1920/doctrine-selected.png) |
| Settlement / construction tabs | [PNG](../artifacts/captures/gameplay/forest/hud-build.png) | [PNG](../artifacts/captures/gameplay/forest/1920/hud-build.png) |
| Selected construction / confirmation | [PNG](../artifacts/captures/gameplay/forest/hud-build-focus-hover.png) | [PNG](../artifacts/captures/gameplay/forest/1920/hud-build-focus-hover.png) |
| Combat | [PNG](../artifacts/captures/gameplay/forest/hud-combat.png) | [PNG](../artifacts/captures/gameplay/forest/1920/hud-combat.png) |

The full 1080p combat phase/sequence captures are preserved separately under `artifacts/captures/godot/battle-1080/`; ordinary animation task output remains under `battle/`. Map interaction captures are under `artifacts/captures/godot/settlement-map/`. These are actual application frames from legal commands.

Important source owners: `UI/LobbyPanel.cs`, `UI/DoctrineCard.cs`, `Game.Shared/DoctrineOffers.cs`, `Application/SessionScreens.cs`, `Application/SteamScreens.cs`, `Networking/GodotSession.cs`, `UI/SettlementHud.cs`, `UI/ConstructionPanel.cs`, `UI/GameplayDialog.cs`, `UI/BuildingInspector.cs`, `UI/DeploymentPanel.cs`, `World/SettlementView.cs`, `World/WorldCamera.cs`, `Application/GameApplication.cs`, `Combat/BattlefieldView.cs`, `Combat/ArenaPresentation.cs`, `World/ArenaPreview.cs` and `Game.Core/Combat/SharedArena.cs`. Paths without a project prefix are within `src/Game.Godot`. Obsolete `BattleInspectionPanel` and `RunProgressionPanel` Controls were removed.

Live Steam with real accounts, native Windows/macOS execution, physical GPU/audio hardware and performance profiling were not run. No new platform or performance qualification is claimed. The existing native Steam verification gate remains in [sessions](SESSIONS.md#native-steam-verification-gate).

## Six-player sessions and development defaults (2026-10-20)

Gameplay supports one through six total players. Core admission, arena topology
(63–378 cells), wave power, snapshot validation and combat safety bounds share
`Match.MaximumPlayers`. Steam lobbies use that limit and the hardcoded development
App ID **480**. Fresh or invalid audio preferences default to **30%** music volume;
valid saved volume and mute settings retain their values. The local Core changes
are listed in [provenance](PROVENANCE.md).

Completed checks:

- `mise run build`: passed with zero warnings/errors and locked dependencies.
- `mise run test-core`: 40 passed; `mise run test-shared`: 72 passed, including
  all six roster sizes, scaled forecasts, snapshot round-trips and audio defaults.
- `mise run test-steam`: 26 passed, including authenticated six-player capacity,
  seventh-player refusal and credential/identity checks for the sixth player's resume.
- `mise run test-session`: both actual graphical process tests passed, including
  native audio buses/preferences, lifecycle, controls and artwork fallback.
- `mise run test-multiplayer`: all six player counts passed paid construction,
  all-player readiness, combat and individual drafting into identical wave-two
  snapshots. The final invocation exited cleanly. Actual `lobby-{1,2,3,4,5,6}p.png`
  captures are under `artifacts/captures/godot/`; the one- and six-player captures
  were inspected, including the full roster and remaining slot count.
- `mise run test-reconnect`: passed protocol/ownership/duplicate/credential checks,
  preparation/combat/draft/process-restart resume, six-player admission,
  seventh-player refusal, retained lobby cities and host shutdown.
- `mise run capture-art`: all 14 actual application frames passed.
- Shell/Python supervisor syntax and `git diff --check`: passed.

Logs use the `artifacts/test-logs/six-player-` prefix. The final multiplayer and
reconnect logs end in `-retry.log`. Initial failures exposed the old 256-cell
board bound, which was corrected before the passing checks. One shared-test
invocation exited before reporting tests; an unchanged rerun passed. An initial
multiplayer run completed every gameplay exercise but its outer task received
SIGTERM during cleanup; the serial rerun exited normally. An initial reconnect
run detected a host process abort (exit signal 6) during shutdown without a native
stack trace; its process logs are preserved in `six-player-reconnect-initial/`.
The unchanged reconnect rerun passed. The cause of that abort is unconfirmed and
the known intermittent native .NET/JIT crash risk remains unresolved.

Not run for this change: aggregate `mise run test`, live Steam/account/overlay
verification, native Windows/macOS runtime, physical display/audio, exports or
remote CI. Offline Steam logic and LAN checks do not qualify live Steam transport.

## CI native extension startup (2026-10-20)

GitHub run `39290970449` failed the native Steam ABI check because its fresh
Godot cache had no extension registration. CI now calls the existing
`tools/prepare-godot.sh` before the check, bounds the probe to 90 seconds with a
five-second kill grace period, and prints its diagnostic log on failure.

Local reproduction with `extension_list.cfg` removed failed at the same native
extension assertion. The updated workflow command block passed with registration
removed and PNG import forced. Locked solution build passed without warnings;
Core (40), Shared (66) and offline session/Steam (25) tests passed. This offline
ABI check does not qualify live Steam, native Windows/macOS or physical hardware;
the intermittent native .NET/JIT risk remains unresolved.

## Illustrated forest settlement (2026-10-18)

[Implementation, file inventory and reproduction commands](SETTLEMENT_MAP.md). The unchanged supplied nine-pad PNG is repository-local. Core, gameplay rules, transport, combat code, existing cutouts/catalog, soundtrack and dependency locks are unchanged.

- `mise run build`: passed, zero warnings/errors.
- `mise run test-core`: 40 passed; `mise run test-shared`: 66 passed, including measured ellipse/scenery exclusion and invalid-layout rejection.
- `mise run test-session`: both real-process tests passed, covering native lifecycle/actions, empty/corrupt art and missing illustrated HUD fallback, both projections, two sizes, pan, three zooms and modal/input focus isolation. Log: `artifacts/settlement-session.log`.
- `mise run capture-art`: all 14 actual application frames passed. Log: `artifacts/settlement-art.log`.
- `mise run capture-settlement`: passed; 11 actual frames in `artifacts/captures/godot/settlement-map/`. Native clicks and hover exercise all nine pads at 720p/1080p, real middle-drag/wheel/click input, locked-plot purchase and Farm construction via existing controls. Picking checks cover twelve inset ellipse boundary points per pad, rejected bounding-box corners, both projections, two sizes and three zooms with pan. Log: `artifacts/settlement-map-check.log`. An initial probe checked hover one frame before the OS mouse warp settled; allowing a second process frame corrected the probe, and subsequent runs passed.
- `mise run test-multiplayer`: real 2/3/4-player LAN sessions passed through paid opening, combat and individual drafting into wave two. Log: `artifacts/settlement-multiplayer.log`.
- `mise run test-animation`: eight tests passed; 19 actual phases verified per art mode, plus missing-sprite and angled camera/shader fallback frames. Log: `artifacts/settlement-animation.log`.
- Source/runtime map SHA-256s match; shell syntax and `git diff --check` pass. Screenshots of empty, hovered, selected, placed and purchased-building states were inspected directly.

Not run for this change: full aggregate `mise run test`, Pillow pipeline tasks, campaign UI suite, reconnect/fault suites, animation profiling, Steam gates, exports and remote CI. Native Windows/macOS, physical display/audio and live Steam remain unqualified. No native .NET/JIT crash occurred during these completed runs; the documented intermittent risk remains.

The completed gameplay/HUD parity work and its current test/export results are recorded in [gameplay verification](gameplay/VERIFICATION.md), with a [feature matrix](gameplay/PARITY.md) and [playing guide](gameplay/PLAYING.md). The migration history below is retained.

## Tilted ground and supplied shrine prototype (2026-10-18)

- `mise run build`: passed, zero warnings/errors. `test-core`: 40 passed. `test-shared`: 64 passed, including deterministic legal shrine construction, near-corner picking in both projections, catalog UV validation and unchanged authority outcomes across presentation modes.
- `art-process`, `art-validate`, `art-install`: passed for fourteen entries. `test-art`: 15 passed, including UV conversion through trimming/padding/resizing and deterministic PNG encoding. Only the prototype Arcanum PNG is new; the original thirteen installed PNGs, four source sheets, Core, audio and dependency/tool locks are unchanged.
- `mise run test-session`: two actual graphical process tests passed (42 seconds), covering debug/sprites, installed/empty/corrupt assets, native HUD fallback, camera/input at 1280 × 720 and 1920 × 1080, both projections, three zoom levels, pan, lifecycle and audio preferences. Transcript: `artifacts/test-logs/sprite-session.log`.
- `mise run capture-art`: passed all twelve views after caption depth ordering was finalized. Actual frames under `artifacts/captures/godot/art/` include `shrine-angled.png`, `shrine-anchors.png`, `shrine-topdown.png`, `settlement-sprites.png`, `settlement-debug.png` and `combat-sprites.png`. These six were visually inspected: the terrain footprint follows projected outlines without rectangular overhang/gaps; shrine proportions stay upright, its base sits on plot 5 and its foreground covers rear captions. Transcript: `artifacts/test-logs/sprite-capture-art.log`.
- `mise run play-assets -- --view angled --debug-overlay on --checkpoint sprite-settlement --capture …`: passed on an owned Xvfb display with disposable settings and automated Dummy audio. The actual illustrated-HUD viewport is `artifacts/captures/godot/art/play-assets-angled.png`; transcript: `artifacts/test-logs/sprite-play-assets.log`. Interactive play still uses normal audio.
- `mise run test-animation`: eight animation contracts passed; all 19 named phases per mode and missing-art/angled camera/shader fallback captures passed. The task uses `--no-sequence`, so no ordered sequence frames were generated in this run. Transcript: `artifacts/test-logs/sprite-animation.log`.
- An initial session-check invocation stopped during its prerequisite build with MSB4166 (MSBuild worker exited); graphical tests had not started. A subsequent full invocation with `DOTNET_PROCESSOR_COUNT=2` passed, as did final capture/animation/play builds using that environment. No source workaround or tool-version change was introduced. The named diagnostic directory was absent when inspected; the cause is unconfirmed. The separately documented intermittent native .NET/JIT risk remains unresolved.
- Full multiplayer/reconnect suites, live Steam, release exports, remote CI and native Windows/macOS execution were not rerun for this presentation-only change. Linux automated checks do not qualify physical GPU/audio hardware.

The supplied shrine remains a prototype, with angular lower cutout edges and residual small ground fragments. Existing terrain/buildings/units are reused and do not yet constitute a consistent production fantasy sprite set. [Artwork status and replacement requirements](ARTWORK.md#remaining-production-artwork) distinguish installed functionality from the remaining artist work.

The migration started at `4ffabb2e29cfb00b4aa44865abc0acd9835bb4ed` on Linux x86_64, using pinned .NET SDK 10.0.401 and Godot .NET 4.7.2. Before removal, the original solution build passed with zero warnings/errors, Core passed 40 tests and Shared passed 44 tests. The original Godot integration suite also passed all four tests (1m56s): native UI/art/fallback loops, 2–4 player LAN and reconnect/fault handling.

Migration transcripts are kept in `/tmp/common-watch-migration/` during execution. Actual application diagnostics and capture receipts are under ignored `artifacts/`. No system packages or new game artwork are installed/generated. Core and the soundtrack remain byte-for-byte unchanged.

## Current migration gates

The removal baseline passed `mise run build` (zero warnings/errors), `test-core` (40), `test-shared` (44), and `test` (113 .NET tests: 40 Core, 44 Shared, 25 Godot session/animation/Steam logic, four actual graphical/network tests; 14 Pillow tests). No failed or skipped tests. The four process tests cover debug/sprite modes, installed/empty/corrupt artwork, native nine-slices, 1280/1920 resizing, music loop and preferences, solo lifecycle, LAN with 2–4 players, reward drafting and reconnect/fault cases.

`capture-art` passed all nine actual views; debug settlement and illustrated draft frames were inspected. `test-animation` passed eight pose/lifecycle contracts, 19 captured phases in each art mode and missing-art/angled camera shader fallback captures. `test-session` passed both graphical facts again with explicit `--steam` startup and `COMMON_WATCH_STEAM_DISABLED=1` (41s). The first capture attempt failed when its running shell script was edited; the complete restart passed. See [migration record](MIGRATION.md) for the intermediate baseline and refactor sequence.

## Native refactor results

- `mise run build`: locked restore and compilation, zero warnings/errors. Core, its original lock, surviving dependency locks and all installed art/music remain unchanged.
- `mise run test-core`: 40 passed; `test-shared`: 44 passed.
- `mise run test`: 113 .NET tests passed (40 Core, 44 Shared, 25 Godot logic/animation/Steam, four real-process integration tests), zero failures/skips; 14 Pillow tests passed. The same task now also checks 19 actual animation phases per art mode plus missing-art and angled camera/shader fallback frames. Final transcript: `artifacts/migration/final-test.log`.
- Expanded native UI checks exercise Camera2D forward/inverse picking for nine plots at 1280×720 and 1920×1080, both projections and zoom 0.55/1/2.2 with pan. Viewport input activates world selection and the settings button, restores keyboard focus, captures modal clicks/reset shortcuts and scrolls the inspector without zooming. Missing/corrupt artwork and illustrated HUD fallback remain covered.
- Actual 2–4 player LAN games and preparation/process-restart/combat/death/draft reconnect/fault checks pass after the final scene reorganization. Earlier focused coordinator checks also passed twice. No recurrence of the known native JIT crash was observed in these runs; the risk remains unresolved.
- The native Steam ABI/API check passes with Steam disabled and no account initialization. An early-exit signal disconnect error found during this migration was corrected and the native check rerun cleanly. CI now checks this path and fails on engine errors as well as missing success receipts.
- Native Sprite2D/shader animation gates passed after extraction. The nine-run profile passed at 30/60/120 target FPS and all supported speeds, with zero final effects and three surviving actors in every run. Fresh `capture-art` passed all nine views; `capture-battle` passed 19 named phases and 32 ordered frames in each art mode plus fallback/camera frames; `capture` passed all four debug checkpoints. Sprite battle and illustrated reward frames were inspected directly. Release results follow after execution.

## Distribution and final cleanup

- `mise run build-linux` and `mise run build-windows` both passed after final native input/RPC-path cleanup. Self-contained x64 folders are `artifacts/builds/godot/linux/` and `artifacts/builds/godot/windows/`. Matching installed export templates were used; no versions or surviving locks changed.
- The Linux exported executable ran from a temporary directory outside the checkout in debug/plain and sprite/illustrated modes. Both actual smoke loops passed native lifecycle/commands, picking/input/focus, resizing, WAV loop, bus volume/mute, persistence and executable-relative asset loading. Illustrated mode decoded the installed catalog/skins; debug decoded zero world PNGs. Exported native Steam ABI checks also passed without account initialization. Logs: `artifacts/migration/linux-release-*.log`.
- Final focused `test-multiplayer`, `test-reconnect` and `test-session` passed after restoring the original `/root/CommonWatch/Session` RPC path under exclusive coordinator ownership. UI checks include native endpoint typing, modal shade capture at non-default zoom, native cancel and focus restoration. Ordinary Controls receive keyboard input before application shortcuts; the settings modal owns its cancel action.
- `art-process`, `art-validate`, `art-install`, `art-preview` passed and reproduced installed assets/previews without a Git difference. The unchanged soundtrack hash remains `3783f4deed1ecd7933a17538a1d880ffb04319f6e11f34dc66f2df7a05ab70b4`.
- Final profile: nine runs at target 30/60/120 FPS and all supported speeds; mean submission 1.07–1.20 ms, p95 1.29–1.89 ms, 60,900–68,389 managed bytes/frame, peak 37 active event effects, zero final effects and three surviving actors each run. Observed managed heap 18.98–32.29 MiB. These software-rendered whole-client measurements do not prove absence of leaks or qualify physical GPU performance. Receipt: `artifacts/profiles/godot-animation.json`.
- Dependency integrity, shell/Python syntax and `git diff --check` pass. Tracked source/scripts/workflows/active docs contain no discarded-engine dependency references; the short historical removal record is intentionally retained in MIGRATION.md. Core, source art, installed assets/previews, music and surviving dependency/tool locks compare unchanged against the starting commit.
- The single Godot workflow now covers native ABI, offline/headless/art tests, real UI/network/reconnect and animation checks, both exports and diagnostics. **Remote GitHub Actions was not triggered from this local migration.** Its required build/test/export steps were executed locally; a remote Ubuntu runner remains a separate check.

The removal baseline is `fba7945293cbdbc5e13512bc7ffb6e103220fa9d`; native architecture extraction is `ba5568f`. Final cleanup adds stable Godot resource UIDs and the input/RPC regression checks. Native Steam workers remain explicitly gated; invoking the worker without authorization was checked to exit 2 before account use.

## Qualification limits

Native two-account Steam SDK initialization, invitations, overlay delivery, Steam gameplay, relay selection and authenticated reconnect remain **unexecuted**. Native library loading/ABI tests, mocked operation rules and LAN success do not qualify Steam end to end. [Sessions](SESSIONS.md#native-steam-verification-gate) documents explicitly gated account workers.

An intermittent native .NET tiered-JIT crash was observed in a previous Godot multiplayer run. Passing subsequent runs do not resolve that risk. Repeated battle/session checks must preserve diagnostics if it recurs.

Linux Xvfb/Mesa checks qualify that automated environment. Native Windows/macOS execution, physical GPU/display/input and real audio hardware qualification are separate, unexecuted platform checks. Cross-builds alone do not qualify native execution. Existing automated audio checks decode the actual WAV, verify loop markers and exercise volume/mute/persistence with a silent driver.

Only supplied static Swordsman and Raider character artwork exists; ranged/magic units and missing buildings use geometry. Painted ground islands, fixed limbs, crowd overlap and stretched parchment centers retain the documented artwork limitations. No captures fabricate authoritative state.

## Forest HUD integration — 2026-10-19

The forest HUD uses the supplied native theme and transparent PNG parts. Its
[implementation and actual capture index](FOREST_HUD.md) describe the new ribbon,
context dock, action area and retained panels. Core, Shared, installed world/map
art, soundtrack and pinned tool/dependency locks compare unchanged to the starting
checkout. No generation scripts, system package installs, commits or pushes were
performed.

Validation transcripts are retained under `artifacts/test-logs/forest-hud/`.
Final executed gates and qualification limits are listed below.

| Gate | Result |
| --- | --- |
| `mise run build` | Passed; zero warnings/errors, locked restore |
| `mise run test-core` | 40 passed |
| `mise run test-shared` | 66 passed |
| `mise run test-art` | 19 passed; includes supplied PNG/SVG hashes, alpha, dimensions, slices and native theme resource references |
| `mise run test-session` | Both graphical integration facts passed; native lifecycle, commands, installed/empty/corrupt world art, both HUD modes, settings/audio, input and focus |
| `mise run test-gameplay-ui` | Native paid scenarios and seven-wave real host/guest checks passed, including actual pending/rejected/disconnected feedback |
| `COMMON_WATCH_GAMEPLAY_ART=illustrated mise run capture-gameplay-ui` | Passed; actual forest states at 720p/1080p, independent selected/hover/focus states, nested Esc, allied inspection and network feedback |
| `mise run capture-art` | Passed; actual plain/illustrated, settlement/combat/reward, nine-slice and showroom views |
| `mise run capture-settlement` and illustrated variant | Passed; all nine pad clicks/hover, native pan/zoom, purchases/construction, both projections, three zooms and both viewport sizes |
| `mise run test-multiplayer` | Passed; real 2–4 player LAN loops through combat/drafting |
| `mise run test-reconnect` | Passed; existing real-process reconnect/fault checks |
| Missing forest theme (temporary negative check, original restored) | Both debug/sprite native smoke loops passed using plain fallback |
| Shell/Python syntax, source/asset/pin comparison, `git diff --check` | Passed |

Native regression checks caught and corrected focus loss from briefly hiding the
construction panel on refresh. They also verify a focused card scrolls completely
into view, independently of a different hovered card. The action area reserves the
expanded dock's height, so selecting a clearing does not move its map pad out from
under the cursor. Real hover/click assertions remain intact.

Not run for this integration: the aggregate `mise run test`, standalone animation
contracts/battle sequences/performance profiling, release exports and exported
runtime checks, remote CI, native Windows/macOS, physical GPU/input/audio hardware,
and live two-account Steam SDK/invite/relay/reconnect qualification. Pathological
long-label/large-number manual stress was not performed; the layout supports
resource reflow and scrolling and was checked with legal scenario values. The
known intermittent native .NET/JIT crash risk remains unresolved; these successful
runs do not qualify it away.

## Arena backdrop and closer camera — 2026-10-21

The optional sprite arena now has a fixed viewport backdrop processed by Pillow
from the supplied forest scenery. It covers at the original aspect ratio, ignores
mouse input and uses no fullscreen blur. A scenery-only alpha material feathers
the complete board's outer decorative margins; playable cells and sector joins
remain opaque. Local framing is 25% closer and biased toward the keep, with the
same magnification for every roster size. F/Field overview remains available;
R restores the closer local framing after overview or manual pan/zoom.

Executed on Linux Xvfb/Mesa with private settings and automated Dummy audio:

- `mise run build`: passed, zero warnings/errors, locked restore.
- `mise run test-core`: 46 passed; `mise run test-shared`: 88 passed.
- `mise run art-process`, `art-validate`, `art-install`, `test-art`: passed;
  19 Pillow tests. Existing supplied component/source PNGs are unchanged.
- `mise run capture-arena`: all 24 cases passed (1–6 sectors, both projections,
  1280×720 and 1920×1080). Native wheel/drag persistence, all-cell inverse picking,
  moving actor/structure picking, overview fit and native R reset passed.
  Magnification is exactly 1.25 times the previous local default and identical
  across roster sizes. One/six-sector off-arena captures at zoom .25 and 2.2 have
  identical backdrop pixels and filled viewport corners. All 132 PNGs are actual
  legal scenario captures under `artifacts/captures/godot/modular-arena/`;
  `verification.json` records the poses and assertions. The one/six-sector local
  views at both sizes/projections and the six-sector overview were inspected.
- `mise run test-session`: both real-process graphical tests passed, including
  debug/plain, installed/empty/corrupt sprite packs, illustrated HUD, lifecycle,
  picking/input/focus and audio preferences. The first invocation caught the old
  45-asset smoke assertion; it was updated for the added derivative and the full
  task rerun successfully.
- `mise run test-animation`: eight contracts and actual phase/fallback captures
  passed. `mise run capture-battle`: named phases, ordered frame checks and
  missing-art/camera fallback captures passed. `mise run capture-art`: passed.
- `git diff --check`: passed. Gameplay/Core, transport, source artwork, music and
  dependency/tool locks are unchanged. No commit or push was performed.

The screenshot review is `artifacts/arena-background-review/index.html`, using
WatchTheme colors and original PNG links. Task transcripts are retained under
`artifacts/test-logs/arena-background/`.

Not rerun: aggregate `mise run test`, multiplayer/reconnect/network-fault suites,
performance profiling, release exports, remote CI, live two-account Steam and
native Windows/macOS or physical GPU/input/audio checks. The known intermittent
native .NET/JIT crash risk remains unresolved; these passing runs do not remove it.

## Dynamic environment — 2026-10-21

Implemented optional retained environmental layers on the existing Compatibility
renderer and artwork. `docs/DYNAMIC_ENVIRONMENT.md` documents composition,
configuration, masks and limits. No Core, Shared, networking, dependency pin,
source artwork, catalog, provenance or audio data changed.

Executed locally:

| Command | Result |
| --- | --- |
| `mise run build` | Pass, zero warnings/errors |
| `mise run test-core` | 46 passed |
| `mise run test-shared` | 88 passed |
| `mise run test-session` | 2 actual graphical integration tests passed |
| `mise run test-animation` | 8 contracts; 19 native phase frames per art mode and fallback/camera checks passed on retry |
| `mise run capture-battle` | Pass: 19 phases and 22 ordered frames per art mode, missing-art and angled/panned fallback |
| `mise run capture-arena` | Pass: all 24 player/projection/resolution records, 132 actual PNGs, native camera/picking/cross-sector/structure checks |
| `mise run capture-settlement` | Pass after repairing probe input sequencing: nine native picks, hover, buy/build, ignored pan/wheel, five window sizes, camera/modal checks |
| `mise run test-environment` | 13 settings contracts and actual renderer suite passed; 145 PNGs, independent layer motion, deterministic resampling, exact Off restoration, unchanged playback tick, presets, nine ellipse picks, lifecycle, shader swatches, missing/debug fallback |

An additional `mise exec -- bash tools/graphical.sh capture-arena
--environment-effects full --environment-time 8` passed all 24 records with
atmosphere enabled. The focused environment suite separately covers enabled
1/6-player local/overview, both projections and resolutions. The settlement
probe now clears only local selection between covered targets, releases its
synthetic wheel button and waits for native layout/input before the final click;
it does not click through panels or alter gameplay/picking logic.

Qualification: normal runs encountered the documented native .NET/JIT problem
(`Invalid Program: attempted to call a UnmanagedCallersOnly method from managed
code`), a native abort and a supervised camera-process timeout. MSBuild also
intermittently lost child nodes or threw `EntryPointNotFoundException` in its
own project evaluator. Build server shutdown/retries recovered builds; final
animation, arena, settlement and environment retries ran with scoped
`DOTNET_TieredCompilation=0 MSBUILDDISABLENODEREUSE=1`. Those are invocation-only
workarounds; no runtime version, repository launch default or check was weakened.
Passing retries do not resolve the native instability. Logs are preserved under
`artifacts/test-logs/environment*.log`; shader errors are explicitly fatal to the
new capture wrapper. An intermediate wrapper failure came from editing the shell
file during an active run; the unchanged rerun passed.

Initial pre-review dedicated Off/Full sampling (30 frames per case after warmup,
no concurrent graphical workload, Xvfb/Mesa llvmpipe; scoped runtime flags above).
The refreshed review measurements at the top of this document supersede these
initial observations and the current `performance.json` contains the new run:

| Resolution | Off median range | Full median range | Maximum Full p95 |
| --- | --- | --- | --- |
| 1280×720 | 6.15–12.65 ms | 7.68–15.16 ms | 16.50 ms |
| 1920×1080 | 11.43–18.43 ms | 13.80–25.56 ms | 25.93 ms |

Across all cases the process-wide allocation mean was approximately 458 KB/frame
Off and 462 KB/frame Full; these include existing whole-client redraw/snapshot
work and capture scheduling, not just environment code. Steady texture decodes
were zero. Full's worst software-rendered 1080p case is approximately 39 FPS,
so this is not a claim of universal 60 FPS. Subtle removes near-rain overdraw and
reduces density; physical GPU timing remains unqualified. Ground mist is confined
to a low band, arena embers to the burning backdrop, and unused layers are omitted.

Artifacts: `artifacts/captures/godot/environment/performance.json` and actual PNGs;
`mise run review-environment` builds a local comparison in
`.lavish/dynamic-environment/index.html` using WatchTheme colors. No game imagery
is synthesized by the review builder. Native Windows, physical GPU/audio and
live Steam are unexecuted gaps. Water/reflection, foliage/window masks and any
additional background separation remain asset-dependent; reusable shaders were
rendered only as technical swatches where no qualified surface exists.
