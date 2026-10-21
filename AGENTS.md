# The Common Watch — Godot 2D

Work in this repository. Ordinary development edits C#, text Godot scenes/resources and scripts, then runs mise. Opening the Godot editor is optional. [README.md](README.md) documents pinned setup and canonical commands.

## Ownership and navigation

| Change | Owner | Focused verification |
| --- | --- | --- |
| Gameplay rules and Arch ECS | `src/Game.Core` is the pinned upstream import with the documented six-player extension; do not edit it for presentation. See [provenance](docs/PROVENANCE.md). | `mise run test-core` |
| Solo adapter, deterministic replay, bounded snapshots, private resume files, hex projection, asset metadata | `src/Game.Shared`; no Godot dependencies or transport framework | `mise run test-shared` |
| Startup, active session lifetime, screen transitions and exit | `src/Game.Godot/Application`; `Scenes/Main.tscn` declares services and retained world views | `mise run test-session` |
| ENet/RPC transport and reconnect | `src/Game.Godot/Networking/GodotSession.cs`; only `Application/SessionCoordinator.cs` creates/disposes sessions | `mise run test-multiplayer`, `test-reconnect` |
| Steam callbacks, identity and operations | `src/Game.Godot/Networking/Steam`; confirmation Controls live in `UI` | `mise run test-steam`; live gate in [sessions](docs/SESSIONS.md#native-steam-verification-gate) |
| Native HUD, inspectors, rewards, lobby and focus | `src/Game.Godot/UI`, `UI/WatchTheme.tres`, `Scenes/UI/SettingsPanel.tscn`; native layout containers and UI events | `mise run test-session`, `capture-art` |
| Settlement and logical selection | `World/SettlementView.cs`; fixed settlement/responsive arena Camera2D framing and native canvas inversion in `World/WorldCamera.cs` | `mise run test-session` includes native picking/input at multiple sizes, both projections and fixed camera input |
| Combat figures, deterministic effects and hit shader | `Combat/BattlefieldView.cs`, `CombatMotion.cs`, `CombatFigure.cs`, `HitFlash.gdshader` | `mise run test-animation`, `capture-battle`; `profile-animation` for repeated cleanup/performance |
| Optional PNG cache and ground anchors | `Art/AssetLoader.cs`, `Art/WorldArt.cs`, `UI/HudSkin.cs`; textures are application owned | `mise run test-session`, `capture-art`; [artwork](docs/ARTWORK.md) |
| Native audio buses and preferences | `Audio/AudioController.cs`, `Audio/default_bus_layout.tres`; retain the existing JSON settings format | `mise run test-session` checks WAV, loops, bus volume/mute and persistence |
| CLI-only development probes, capture and showroom | `src/Game.Godot/Development`, `tools`; probes submit legal commands or observe detached state | Relevant mise capture/integration task; [verification](docs/VERIFICATION.md) |

Paths without the `src/Game.Godot` prefix in the table are relative to that project. Keep application lifecycle partials scoped to coordination; put UI, drawing, transport and resources in their actual owners. Disconnect cross-owner events on exit. Session generations and old-session guards must survive refactors. Keep the published `/root/CommonWatch/Session` RPC path stable: the session remains an application-root child while SessionCoordinator exclusively owns its lifetime.

The application submits authoritative commands and reads detached snapshots. Never write simulation state or duplicate rules. Combat presentation samples Core playback; independent wall-clock Tweens/particles must not drive combat timing, damage or occupancy.

## Workflow and constraints

After coherent code changes run `mise run build`, `mise run test-core`, `mise run test-shared` and the affected actual graphical checks above. `mise run test` includes Pillow, all .NET/process tests and animated frame checks. Compilation alone does not verify presentation. Use the legal scenario checkpoints for captures; never fabricate authoritative state. Native text scenes/resources are source files and require no editor import dialogs.

Automated Linux checks own virtual displays and use temporary `--settings-path` files/private session directories. Preserve bounded process supervision and cleanup. Tasks must not install system packages. Interactive launches use normal audio; silent audio belongs only in automated checks.

Keep dependencies, tool versions and NuGet locks pinned. `build-linux` and `build-windows` produce self-contained x64 folders in gitignored `artifacts/builds/godot/<platform>/`, including music/optional sprites and executable-relative asset lookup. RID-specific locks are in `tools/nuget-locks`; never regenerate Core's pinned upstream lock for publishing.

Ordinary play and exported builds default to sprites, illustrated HUD and angled view with the debug overlay off. Explicit debug/plain verification ignores artwork. Preserve `--world-art debug|sprites`, `--hud-art plain|illustrated`, `--view topdown|angled`, `--debug-overlay on|off` and geometric/plain fallback. Never create, generate or download game artwork. Existing Pillow processing, technical font/shape resources and actual application screenshots are allowed.

The pinned upstream WAV, authored loop markers, source artwork and provenance must remain intact. Preferences are local presentation data; private reconnect credentials must not enter ordinary settings, logs or snapshots. Preserve GPL-3.0-only notices and licenses. Record unexecuted checks accurately, including live Steam/native-platform gaps and the known intermittent native .NET/JIT crash risk.
