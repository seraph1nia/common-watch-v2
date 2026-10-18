# Verification

Verified on Linux x86_64 with .NET SDK 10.0.401, Godot .NET 4.7.2, MonoGame DesktopGL 3.8.4.1 and Gum.MonoGame 2026.1.31.1. Real graphics ran on owned Xvfb displays using Mesa llvmpipe OpenGL 4.6. Graphical integration uses silent audio backends; separate output checks used isolated PulseAudio sinks. No OS packages were installed.

## Artwork integration (2026-10-18)

The supplied four source sheets were extracted once and preserved byte-for-byte. Thirteen assets were cropped, uniformly fitted into their documented canvases, validated and installed, with source/output metadata. All source, output and preview PNGs also passed an explicit PNG-chunk CRC check. No replacement artwork, archive tool or Core changes were introduced. All 47 tracked Core files were also compared byte-for-byte against their original Git blobs. A second real processing run reproduced the installed PNG hashes, catalog bytes and preview bytes exactly. See [workflow, catalog, source choices and limitations](ARTWORK.md).

- `mise run art-process`, `art-validate`, `art-install`, `art-preview`, `test-art`: successful. **14 Pillow tests** cover crops, RGBA/alpha, visible bounds, aspect ratio, canvas/anchor placement, invalid inputs, conservative edge cleanup, deterministic bytes and nine-slice corners at all four required sizes.
- `mise run build`: locked restore and complete solution build with zero warnings/errors. No dependency versions or NuGet locks changed.
- `mise run test-core`: **40 passed**. `mise run test-shared`: **41 passed**, including catalog replacement/missing metadata, path rejection, installed IDs, 0.72 projection/inverse selection, actual authoritative attack/hit/death event sampling and identical game results across art settings.
- `mise run test`: **132 .NET tests passed**, none failed/skipped, plus the 14 Pillow tests. Breakdown: Core 40, Shared 41, Godot session 17, MonoGame session 26, graphical/process integration 8. This includes the existing multiplayer and reconnect process checks. The aggregate transcript is `artifacts/full-test.log`.
- Both actual graphical clients ran their complete UI/gameplay smoke loops against installed art, empty asset directories and corrupt PNG fixtures. Installed-art checks use angled view, illustrated HUD and overlays in both world modes, exercise native texture decoding and native nine-slices, preserve dynamic text and test 1280/1920 resizing. Additional empty-directory illustrated-HUD checks verify fallback to ordinary controls. Debug reports zero world-image decoding. Per-client evidence is in `artifacts/test-logs/{godot,monogame}-{installed,empty,empty-illustrated,invalid}.log`.
- Final focused reruns after the last layout corrections: `mise run test-godot` and `test-monogame` each passed both graphical facts, including installed art and empty/corrupt/illustrated-fallback launches. Their transcripts are `artifacts/final-{godot,monogame}-test.log`.
- `mise run capture-godot-art` and `capture-monogame-art`: nine actual frames each, all inspected. Paths are `artifacts/captures/{godot,monogame}/art/`. The required names cover debug/topdown, angled/overlays, sprites, combat, plain/illustrated draft HUD, small/large nine-slice layouts and showrooms. Large captures are 1920 × 1080; showrooms are 1280 × 1080. Showroom logs confirm `authority=none`.
- Inspection corrected Godot container scaling/background draw order, native viewport sizing, labels hidden by tall sprites, button/card text padding, MonoGame frame insets and showroom UI remnants. Native corners and connected edges remain intact; the large painted parchment center retains the documented grain/stretch limitation.

These results qualify Linux/Mesa presentation. Windows/macOS rendering, physical input/audio and fresh platform release builds were not run for this artwork change. The release/session sections below retain their earlier qualification history. Previously empty optional directories now contain the supplied cutouts; positive native PNG decoding is now exercised.

## Session extension results

- `mise run build`: complete solution, pinned binary hash verification and locked NuGet restore; zero warnings/errors.
- `mise run test`: **120 passed**, none failed/skipped: Core **40**, Shared **31**, Godot session **17**, MonoGame session **26**, graphical/process integration **6**. Ordinary tests require neither Steam initialization nor artwork.
- Solo lifecycle checks in each actual client cover the main menu, doctrine selection, match start, legal construction/recruitment, combat, drafting, leaving during combat, resource cleanup and a new independent match. Existing settings, resize, music and optional-sprite fallback checks remain passing.
- Real application processes completed **two-, three- and four-player** games independently for Godot ENet and MonoGame LiteNetLib. Every player used paid construction/recruitment, Core readiness, shared automatic combat and an individual reward draft. Final authoritative snapshots converged. Lobby frames were captured at `artifacts/captures/{godot,monogame}/lobby-{2,3,4}p.png`; four-player frames were inspected in both clients.
- Each framework's real-network reconnect/fault exercise passed preparation, process-restart, combat and drafting reconnects; retained city identity; ongoing host combat; exact accepted-command retry without duplicate spending; unauthorized commands; duplicate commands; incompatible protocol; invalid/expired credentials; late admission rejection; four-city capacity; lobby departure and original-host shutdown. A replacement authority rejects the old match's credentials.
- Supervisors allocate isolated ports and private data, bound execution, terminate owned process groups and clean temporary files. Graphical workers own their virtual display. Interactive development tasks retain normal display/audio.
- Both clients passed graphical gameplay/lifecycle checks in debug and sprites modes with `COMMON_WATCH_STEAM_DISABLED=1`, including an explicit Steam startup request. Offline startup and LAN do not initialize the SDK.
- The final aggregate transcript is `artifacts/test-logs/final-session-suite.log`. See [session commands and prerequisites](SESSIONS.md) for focused tests and reproducible multi-process launches.

Godot keeps native RPC/ENet sessions separate from rendering; MonoGame keeps LiteNetLib and Steam Sockets delivery separate from Gum and SpriteBatch. Both use the unchanged Core authority. Solo avoids a network socket. The shared additions are bounded snapshot serialization and private resume-file storage, not a shared transport framework.

## Steam verification boundary

GodotSteam 4.22.1 GDExtension binaries load with pinned Godot 4.7.2 .NET; an actual headless native ABI check verified C# Steam peer conversion and every SDK method/signal used. MonoGame compiles against official Facepunch 2.5.2 POSIX/Win64 assemblies under .NET 10; its offline native-load check resolves the current Steam SDK initialization export without initializing Steam. Vendored binary manifests and licenses are preserved and checked by the build.

Offline tests cover operation ordering/cancellation, timeouts, duplicate invitations, late callbacks, session replacement, missing Steam, V2 metadata, immutable original hosts and authenticated-identity rules. Steam integration includes private lobbies, invitations, original-host validation, authenticated transports, reconnect and shutdown. MonoGame uses Steam Networking Sockets relay APIs; Godot uses SteamMultiplayerPeer.

**Native SDK initialization with eligible accounts, invitation delivery/acceptance, Steam gameplay, relay routing and authenticated reconnect have not been executed.** Loading libraries and passing mocked state machines do not qualify these features end to end. Explicitly gated native workers are provided for a pair of eligible accounts; [the native verification instructions](SESSIONS.md#native-steam-verification-gate) describe authorization, prerequisites and evidence. Steam remains unqualified until those checks run. No account credentials or fixed development AppID were added.

## Background music and settings

Both clients decode the unchanged upstream **Echoes of Valhalla** WAV and keep one player across game resets and phases, independently of sprite rendering. Godot retains the WAV's embedded loop metadata. MonoGame queues PCM buffers because its pinned DesktopGL SoundEffect implementation ignores custom loop points; buffer-boundary checks verify the jump from sample 6,195,530 (inclusive) to 1,548,939.

Actual graphical integration in both rendering modes exercises the native settings button, volume slider at 0/50/100%, underlying native volume, settings save/reload, closing the menu, and unchanged gameplay state. Test preferences are temporary; interactive launches use normal audio devices and persistent per-client preferences.

Separate normal-driver launches routed each application into its own temporary PulseAudio sink. Captured output contained nonzero music samples at saved volume 50% (Godot peak 8,947; MonoGame peak 8,939, signed 16-bit PCM) and exactly zero samples at saved volume 0%. These checks captured only the isolated application sinks and left desktop device/default settings unchanged. Results: `artifacts/test-logs/audio-output.json`. Physical speaker/headphone listening and a full-duration audible loop transition have not been assessed.

Actual settings-menu frames were captured and inspected at both 1280×720 and 1920×1080: `artifacts/captures/{godot,monogame}/settings-1280.png` and `settings-1920.png`. No controls are clipped.

## Platform release builds

All four commands completed successfully on the Linux development host: `mise run build-godot-windows`, `build-godot-linux`, `build-monogame-windows`, and `build-monogame-linux`. Outputs under `artifacts/builds/<client>/<platform>/` are gitignored. Executables are confirmed x64 Windows PE or Linux ELF as appropriate; each folder contains a self-contained .NET runtime, game dependencies, the unchanged music WAV, optional sprite directories, license and provenance.

Both Linux release folders were copied into temporary directories outside the checkout and launched from a different working directory with `DOTNET_ROOT` pointing to a nonexistent installation. Actual graphical smoke checks passed in **both rendering modes**, including the complete gameplay loop and music/settings checks. No source asset arguments were supplied: bundled music and sprites resolve beside the executable. The session-enabled release transcripts are `artifacts/test-logs/{godot,monogame}-release-session-{debug,sprites}.log`. The exported Godot bundle also passed its native extension ABI check (`godot-release-steam-abi.log`) without SDK initialization. Actual release settings captures were inspected at `artifacts/captures/{godot,monogame}/linux-release-settings.png`.

All four release builds were repeated for the session extension. Matching Linux/Windows native Steam libraries and their licenses are bundled. Windows PE and Linux ELF architectures were checked. Existing package versions remain pinned; MonoGame runtime locks additionally pin LiteNetLib 2.1.4. The Core default and runtime-specific lockfiles were not regenerated. The unchanged 47-file Core import was checked byte-for-byte again. Windows outputs were built but have **not** been launched on Windows.

Invalid-image fixtures are plain text in temporary files named `.png`, not manufactured artwork. In both clients, sprites mode exercised Farm and Swordsman fallback and debug ignored the same files. No game sprite or artwork was created, downloaded or copied.

## Actual captures

`mise run capture-godot` and `mise run capture-monogame` produce and were checked for:

| Checkpoint | Godot | MonoGame |
| --- | --- | --- |
| Four-building settlement | `artifacts/captures/godot/settlement.png` | `artifacts/captures/monogame/settlement.png` |
| Selected Barracks | `artifacts/captures/godot/building-selected.png` | `artifacts/captures/monogame/building-selected.png` |
| Wave-one combat at tick 180 | `artifacts/captures/godot/combat.png` | `artifacts/captures/monogame/combat.png` |
| Real three-card reward draft | `artifacts/captures/godot/draft.png` | `artifacts/captures/monogame/draft.png` |

These are real viewport/backbuffer frames at 1280×720, using no artwork. Both clients also resize to 1920×1080 during graphical checks; actual `draft-1920.png` captures were produced and inspected for both clients. The combat checkpoint has actual movement, attacks and damaged units; exact unit health is readable in the side panel. Terrain completes before anchored buildings/units; optional image dimensions never determine gameplay coordinates.

## Unexecuted and environment-dependent checks

- Positive native PNG loading is now covered by the artwork integration checks above. The original empty/corrupt-fixture checks remain in place.
- Native Windows/macOS launches and captures have not been run. This repository's pinned automated graphical workflow targets Linux x86_64.
- Virtual-display tests verify application rendering and callbacks. They do not qualify a physical GPU/display or desktop input device. Audio output has additionally been measured through isolated software sinks as described above; physical listening remains unexecuted.

The VSync warning from Godot under Xvfb is expected; graphical checks still render using actual software OpenGL. Nothing above relies on headless Godot to validate graphics.

## Source integrity

All **47** tracked `src/Game.Core` files match the pinned upstream revision byte-for-byte, including its project and NuGet lockfile. The four imported test/fixture sources also match their upstream blobs; see `tests/Game.Core.Tests/Upstream/PROVENANCE.md`. The original reference checkout was not modified.
