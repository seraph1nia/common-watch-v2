# Verification

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

## Qualification limits

Native two-account Steam SDK initialization, invitations, overlay delivery, Steam gameplay, relay selection and authenticated reconnect remain **unexecuted**. Native library loading/ABI tests, mocked operation rules and LAN success do not qualify Steam end to end. [Sessions](SESSIONS.md#native-steam-verification-gate) documents explicitly gated account workers.

An intermittent native .NET tiered-JIT crash was observed in a previous Godot multiplayer run. Passing subsequent runs do not resolve that risk. Repeated battle/session checks must preserve diagnostics if it recurs.

Linux Xvfb/Mesa checks qualify that automated environment. Native Windows/macOS execution, physical GPU/display/input and real audio hardware qualification are separate, unexecuted platform checks. Cross-builds alone do not qualify native execution. Existing automated audio checks decode the actual WAV, verify loop markers and exercise volume/mute/persistence with a silent driver.

Only supplied static Swordsman and Raider character artwork exists; ranged/magic units and missing buildings use geometry. Painted ground islands, fixed limbs, crowd overlap and stretched parchment centers retain the documented artwork limitations. No captures fabricate authoritative state.
