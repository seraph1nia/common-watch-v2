# The Common Watch — Godot 2D

A code-first C# game with a pure C# authoritative simulation and one Godot 2D application. Solo and host-authoritative 1–4 player sessions cover lobby/doctrine selection, settlement, shared automatic combat, individual reward drafting and wave progression. Optional GodotSteam integration remains unqualified with live accounts. The complete game defaults to shapes and readable text; artwork is optional.

## Setup

Tools are pinned in `mise.toml`, `mise.lock` and `global.json`: .NET SDK **10.0.401**, Godot **4.7.2 .NET** (keep its adjacent GodotSharp directory), Python 3 and Pillow **11.3.0**. Initial restore requires NuGet access. Keep committed dependency locks and versions unchanged during ordinary development.

```sh
mise trust
mise install --locked
mise run art-setup
mise run build
mise run test
```

Interactive play requires a display and OpenGL 3.3 driver. Linux automated checks require existing `Xvfb`, `xvfb-run`, `xauth`, `timeout` and Mesa OpenGL; tasks never install system packages. Checks own their virtual displays, temporary settings and private session directories. Automated audio uses Godot's Dummy driver; interactive play uses normal audio. `test-core` and `test-shared` need no display, Godot startup, assets, Steam or audio hardware.

## Play

```sh
mise run play                      # artwork-independent debug/plain defaults
mise run play-assets               # optional sprites and illustrated HUD
mise run dev-multiplayer           # supervised LAN host and guest
mise run dev-multiplayer -- --players 4
mise run play-steam                # optional; requires your authorized AppID
```

Choose **Single player**, a doctrine and **Start match**, or **Host LAN** / **Join LAN**. Left-click selects logical settlement plots; middle/right drag pans, wheel zooms and **R** resets the camera. Controls display authoritative results and rejection messages. The complete catalog, Army/squads, six army homes, Town Hall, Worldtree, battle forecast, unit/structure inspectors and run progression are available through native panels. The city selector inspects allies read-only; Ready/Unready, whole-match pause and host combat speed remain separate from local dialogs. See [gameplay controls](docs/gameplay/PLAYING.md) and the [parity audit](docs/gameplay/PARITY.md). Ready advances preparation and settles real production. Combat plays automatically; choose a reward after winning. Main menu leaves the authority, and a fresh match gets a new identity.

A legal opening: Farm on plot 1, Barracks on plot 2, Metal Mine on plot 3, then Lumbermill on plot 4 using the **4 Gold recovery** action. Advance preparation, recruit two Swordsmen, advance again and recruit two more, then ready for combat. Costs, economy, combat and drafting are always validated by Core.

Settings controls music volume, with zero muting. Preferences persist under Godot's user directory; music continues across session changes and retains the soundtrack's authored intro and loop. See [music provenance](assets/audio/README.md).

Presentation options: `--world-art debug|sprites` (`--render-mode` remains an alias), `--hud-art plain|illustrated`, `--view topdown|angled`, `--debug-overlay on|off`. Missing/corrupt PNGs use geometric/plain fallback. Thirteen supplied cutouts and their Pillow pipeline are described in [artwork](docs/ARTWORK.md). No editor import or new artwork is required.

## Battle and artwork tools

```sh
mise run battle
mise run battle-assets
mise run test-animation
mise run capture-battle
mise run profile-animation
mise run review-battle
mise run art-process
mise run art-validate
mise run art-install
mise run art-preview
mise run art-showroom
mise run test-art
```

Combat poses, shots, statuses, hits and deaths follow authoritative playback. Showcase scenarios are earned through legal commands. [Animation documentation](docs/COMBAT_ANIMATION.md) covers controls, receipts, captures and known static-sprite limitations.

## Verification

```sh
mise run build
mise run test-core
mise run test-shared
mise run test                       # Pillow + .NET/process tests + animation captures
mise run test-session
mise run test-gameplay-ui          # native gameplay + seven-wave host/guest replay
mise run capture-gameplay-ui       # actual panels at 720p/1080p and allied inspection
mise run test-multiplayer
mise run test-reconnect
mise run test-network-faults
mise run test-steam                 # offline rules/state tests
mise run test-steam-native -- host FRIEND_STEAM_ID  # explicitly gated
mise run capture
mise run capture-art
mise run capture-lobby
```

[Sessions](docs/SESSIONS.md) documents networking, reconnect credentials and the live Steam gate. [Verification](docs/VERIFICATION.md) records executed checks and platform gaps. Captures are actual viewport PNGs under `artifacts/captures/godot/`; logs are under `artifacts/test-logs/`. Deterministic checkpoints (`initial`, `settlement`, `building-selected`, `battle-start`, `combat`, `draft`, `post-draft`) replay the unchanged rules, with seed `20261018`.

```sh
mise run capture -- --width 1920 --height 1080
mise run play-assets -- --checkpoint draft --view angled
mise run scenario post-draft artifacts/scenarios/post-draft.json
```

## Distribution and CI

```sh
mise run build-linux
mise run build-windows
```

Self-contained x64 Release outputs are `artifacts/builds/godot/linux/CommonWatch.x86_64` and `artifacts/builds/godot/windows/CommonWatch.exe`. Copy the **whole output folder**, including managed runtime, optional assets, music and licenses. Asset lookup is executable relative. Exported game arguments follow Godot's separator: `CommonWatch.x86_64 -- --world-art sprites`.

Install matching **4.7.2.stable.mono export templates** in Godot's standard user `export_templates` directory before exporting. The [Godot GitHub Actions workflow](.github/workflows/godot.yml) fetches the pinned upstream templates and verifies their SHA-256. Builds use RID-specific locks under `tools/nuget-locks`, preserving Core's original lockfile. Tasks replace only their own output directory.

One workflow builds the entire solution, runs headless/art/session/Steam logic and real graphical/multiplayer/reconnect/animation checks, cross-builds Linux/Windows and uploads complete archives plus diagnostics. It checks existing graphical prerequisites and never installs system packages. Live Steam tests are excluded. Cross-building Windows does not qualify Windows runtime execution.

## Ownership

- `src/Game.Core`: unchanged pinned upstream gameplay and Arch ECS, independently buildable/testable.
- `src/Game.Shared`: solo adapter, deterministic scenarios/playback helpers, bounded snapshot codec, private resume files, logical projection and asset metadata; no graphical dependencies or transport framework.
- `src/Game.Godot`: sole graphical application, native Controls/Node2D, ENet RPC sessions, GodotSteam, audio and presentation resources.
- `tests`: headless rules, shared/session logic, artwork and actual Godot process checks.
- `tools`: reproducible CLI launch, capture, scenario, profiling, artwork and release tasks.

`Scenes/Main.tscn` declares the application, session owner, Camera2D, settlement/battlefield views and audio owner. `Application/GameApplication.cs` coordinates them; `UI` owns native screens, `Combat` owns sampled poses and Sprite2D figures, `Art` caches external PNGs, and `Networking/Steam` owns native callback lifetimes. `UI/WatchTheme.tres`, `Combat/HitFlash.gdshader` and `Audio/default_bus_layout.tres` centralize presentation resources. Small hex boards/effects retain efficient custom drawing; logical angled projection and deterministic playback remain independent of camera transforms.

Ordinary development edits C#, text scenes/resources and scripts, then runs mise; opening the Godot editor is optional. See [agent instructions](AGENTS.md), [source provenance](docs/PROVENANCE.md) and [GPL-3.0-only license](LICENSE).
