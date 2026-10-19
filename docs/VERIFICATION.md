# Verification

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
