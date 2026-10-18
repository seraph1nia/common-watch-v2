# The Common Watch — two C# vertical slices

Godot 2D and MonoGame DesktopGL/Gum clients share the original deterministic `Game.Core`. Both implement settlement → automatic combat → three-card reward draft → next preparation. Default presentation is permanently artwork independent: shapes and readable labels.

## Setup

Required tools are pinned in `mise.toml`/`mise.lock` and `global.json`:

- mise; .NET SDK **10.0.401** (including the .NET 10 runtime).
- Godot **4.7.2 .NET/Mono**, with its adjacent GodotSharp directory intact. The standard Godot edition cannot run C#.
- NuGet access for the initial restore. Package versions and transitive resolutions are committed in `packages.lock.json`.
- A display and working OpenGL 3.3 driver for interactive play. MonoGame additionally uses SDL2/OpenAL native runtime libraries; package-native dependencies are resolved through NuGet where provided.
- Linux automated graphical checks/captures require `Xvfb`, `xvfb-run`, `xauth`, `timeout` and Mesa software OpenGL. Tests use their own virtual displays and silent audio backends. Tasks never install OS packages or tool upgrades.

From this directory:

```sh
mise trust
mise install --locked
mise run build
mise run test
```

Tool setup currently pins Linux x86_64, which is the verified capture/test platform. Windows and Linux x64 release builds are supported below; native Windows/macOS runtime qualification has not been performed. Install OS prerequisites yourself if missing; graphical commands report missing executables and fail instead of silently passing. `test-core` and `test-shared` need neither a display nor Godot.

## Play

```sh
mise run play-godot                 # default: shapes and text
mise run play-monogame
mise run play-godot-no-assets       # explicit: ignore sprite files
mise run play-monogame-no-assets
mise run play-godot-assets          # optional sprites, same geometric fallback
mise run play-monogame-assets
```

Left-click a settlement hex to inspect/build/recruit. Right-drag pans in both clients (Godot also supports middle-drag), wheel zooms, and **R** resets the camera. Side-panel actions display the authoritative result or rejection reason. Advance/Ready settles production and advances the original preparation stages. Combat runs automatically on the separate shared arena. Choose one of three rewards after winning. New game restarts the solo session.

A tested legal opening under unchanged rules:

1. Build **Farm** on plot 1, **Barracks** on plot 2 and **Metal Mine** on plot 3. These spend the six starting wood.
2. Build **Lumbermill** on plot 4 using the **4 Gold recovery** construction action.
3. Advance preparation; select Barracks and recruit **two Swordsmen**.
4. Advance preparation again and recruit **two more Swordsmen**.
5. Ready for combat. Win wave one and choose a reward to enter wave two.

The Swordsmen require production from the Metal Mine. Insufficient resources, occupied plots and unavailable actions are rejected by Core. There is no client-side replacement economy or combat logic.

Press **Escape** or close the window to exit MonoGame; close the Godot window to exit Godot.

Both clients play the reference game's **Echoes of Valhalla** background music, including its intro and authored loop. Open **Settings** and adjust **Music volume**; **0%** mutes it. Changes apply immediately and are saved per client between launches. Opening settings or restarting a game keeps the existing track playing. Music works in both rendering modes and uses your normal audio device during interactive play; automated graphical checks use silent audio backends. The unchanged WAV and its original provenance are in [assets/audio](assets/audio/README.md).

Both rendering modes use identical logical positions and gameplay. Add appropriately licensed PNGs under `assets/sprites` if desired; see [sprite mappings](assets/sprites/README.md). Missing/corrupt files fall back to the same shapes. Debug mode never decodes sprite files. No game artwork is included or generated.

## Windows and Linux builds

```sh
mise run build-godot-windows
mise run build-godot-linux
mise run build-monogame-windows
mise run build-monogame-linux
```

These create self-contained **x64 Release** builds under the gitignored `artifacts/builds/` directory:

| Task | Output executable |
| --- | --- |
| `build-godot-windows` | `artifacts/builds/godot/windows/CommonWatch.exe` |
| `build-godot-linux` | `artifacts/builds/godot/linux/CommonWatch.x86_64` |
| `build-monogame-windows` | `artifacts/builds/monogame/windows/Game.MonoGame.exe` |
| `build-monogame-linux` | `artifacts/builds/monogame/linux/Game.MonoGame` |

Copy the **whole output folder** to the target machine. Each includes the .NET runtime, game dependencies, music, current optional sprites and provenance; no .NET installation or source checkout is needed to play. A working desktop graphics/audio environment is still required. Builds default to geometric presentation; pass `--render-mode sprites` to MonoGame, or `-- --render-mode sprites` to Godot, to enable bundled optional sprites. Assets resolve relative to the executable, independently of the working directory.

Run build tasks from the project root with the pinned mise tools. Godot additionally requires its matching **4.7.2.stable.mono .NET export templates**, installed through Godot's **Manage Export Templates** dialog or unpacked into its standard `export_templates/4.7.2.stable.mono` directory. Tasks do not download templates or install system packages. NuGet access may be needed for the first self-contained publish. Runtime-specific dependency locks live in `tools/nuget-locks`; they preserve the original Core lockfile. Each task replaces only its own platform output folder.

## Test and capture

```sh
mise run build                     # locked restore and complete solution build
mise run test-core                 # upstream authoritative rules + slice regressions
mise run test-shared               # adapter, projection, assets and deterministic replay
mise run test-godot                # real graphical integration, both modes
mise run test-monogame             # real graphical integration, both modes
mise run test                     # all of the above test suites
mise run capture-godot
mise run capture-monogame
```

Captures are actual application frames in `artifacts/captures/{godot,monogame}/`: `settlement.png`, `building-selected.png`, `combat.png`, `draft.png`. They default to debug mode and replay legal fixed-seed checkpoints. Capture/test commands always own an Xvfb display; they do not use the desktop session. Raw client options can be forwarded, for example:

```sh
mise run capture-godot -- --width 1920 --height 1080
mise run play-monogame -- --checkpoint draft
mise run play-godot-assets -- --assets-root /absolute/path/to/sprites
mise run scenario post-draft artifacts/scenarios/post-draft.json
```

Checkpoints: `initial`, `settlement` (four buildings), `building-selected`, `combat` (first real attack), `draft`, `post-draft`. A checkpoint freezes automatic advancement for capture inspection; gameplay actions resume it. The scenario runner records a snapshot and selection from legal replay, without opening a graphics client. The seed is `20261018`.

## Layout and rules boundary

- `src/Game.Core`: unchanged upstream rules and Arch ECS; independently buildable with `mise exec -- dotnet build src/Game.Core/Game.Core.csproj`.
- `src/Game.Shared`: solo authority adapter, replay, hex projection/hit testing, visual IDs and render options. No graphical framework references.
- `src/Game.Godot`: code-first Node2D drawing and native Controls; minimal bootstrap scene.
- `src/Game.MonoGame`: SpriteBatch drawing and code-only Gum Forms. Framework font/shape resources are technical UI dependencies, not game artwork.
- `tests`: Core, shared and actual-process client integration suites.
- `tools`: scenario CLI and bounded graphical launch/capture scripts.

All commands run from the project root. See [source provenance](docs/PROVENANCE.md), [verification](docs/VERIFICATION.md), [agent instructions](AGENTS.md) and [GPL-3.0-only license](LICENSE). Do not modify the reference repository.
