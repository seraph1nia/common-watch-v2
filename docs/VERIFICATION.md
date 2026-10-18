# Verification

The migration started at `4ffabb2e29cfb00b4aa44865abc0acd9835bb4ed` on Linux x86_64, using pinned .NET SDK 10.0.401 and Godot .NET 4.7.2. Before removal, the original solution build passed with zero warnings/errors, Core passed 40 tests and Shared passed 44 tests. The original Godot integration suite also passed all four tests (1m56s): native UI/art/fallback loops, 2–4 player LAN and reconnect/fault handling.

Migration transcripts are kept in `/tmp/common-watch-migration/` during execution. Actual application diagnostics and capture receipts are under ignored `artifacts/`. No system packages or new game artwork are installed/generated. Core and the soundtrack remain byte-for-byte unchanged.

## Current migration gates

The removal baseline passed `mise run build` (zero warnings/errors), `test-core` (40), `test-shared` (44), and `test` (113 .NET tests: 40 Core, 44 Shared, 25 Godot session/animation/Steam logic, four actual graphical/network tests; 14 Pillow tests). No failed or skipped tests. The four process tests cover debug/sprite modes, installed/empty/corrupt artwork, native nine-slices, 1280/1920 resizing, music loop and preferences, solo lifecycle, LAN with 2–4 players, reward drafting and reconnect/fault cases.

`capture-art` passed all nine actual views; debug settlement and illustrated draft frames were inspected. `test-animation` passed eight pose/lifecycle contracts, 19 captured phases in each art mode and missing-art/angled camera shader fallback captures. `test-session` passed both graphical facts again with explicit `--steam` startup and `COMMON_WATCH_STEAM_DISABLED=1` (41s). The first capture attempt failed when its running shell script was edited; the complete restart passed. See [migration record](MIGRATION.md) for the intermediate baseline and refactor sequence.

## Qualification limits

Native two-account Steam SDK initialization, invitations, overlay delivery, Steam gameplay, relay selection and authenticated reconnect remain **unexecuted**. Native library loading/ABI tests, mocked operation rules and LAN success do not qualify Steam end to end. [Sessions](SESSIONS.md#native-steam-verification-gate) documents explicitly gated account workers.

An intermittent native .NET tiered-JIT crash was observed in a previous Godot multiplayer run. Passing subsequent runs do not resolve that risk. Repeated battle/session checks must preserve diagnostics if it recurs.

Linux Xvfb/Mesa checks qualify that automated environment. Native Windows/macOS execution, physical GPU/display/input and real audio hardware qualification are separate, unexecuted platform checks. Cross-builds alone do not qualify native execution. Existing automated audio checks decode the actual WAV, verify loop markers and exercise volume/mute/persistence with a silent driver.

Only supplied static Swordsman and Raider character artwork exists; ranged/magic units and missing buildings use geometry. Painted ground islands, fixed limbs, crowd overlap and stretched parchment centers retain the documented artwork limitations. No captures fabricate authoritative state.
