# The Common Watch — Godot 2D

A code-first C# game with a pure C# authoritative simulation and one Godot 2D application. Solo and host-authoritative 1–6 player sessions cover lobby/doctrine selection, settlement, shared automatic combat, individual reward drafting and wave progression. Optional GodotSteam integration remains unqualified with live accounts. The game defaults to the existing sprites and illustrated HUD in angled view, with readable geometric/plain fallback when artwork is unavailable.

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
mise run play                      # normal game; same defaults as exported builds
mise run play-debug                # artwork-independent debug/plain/topdown verification
mise run play-assets               # compatibility alias for play
mise run dev-multiplayer           # supervised LAN host and guest
mise run dev-multiplayer -- --players 6
mise run play-steam                # optional; development Steam App ID is hardcoded to 480
```

`play` runs from source for quick iteration; it does not export a release on each launch. It uses the same runtime presentation defaults, main menu, native controls, local preferences and normal audio as the exported game. `play-steam` adds only optional Steam startup.

Doctrine cards lead with playstyle; **Full effects & stock** retains all numeric detail. An optional **Guide** supports the first three waves without gating Ready.

The main menu has **Single Player**, **Multiplayer**, **Settings** and **Exit Game**. Single Player opens three distinct personal doctrine cards; explicitly select one before **Start Match**. Multiplayer contains LAN hosting/joining/reconnect and Steam entry points. Only the host starts a network match, after every connected player explicitly selects a doctrine. Every submenu returns to the main menu.

Left-click selects a settlement clearing. The illustrated map covers the viewport under floating HUD controls; settlement pan, wheel zoom and camera reset are disabled. **Build** uses Production, Military, Defense, Trade and Utility tabs. Selecting a card spends nothing, including Lumbermill Gold recovery; **Build selected** confirms. Recruitment, upgrades, selling and trading stay in the selected building context. **Troops & homes** opens roster, squad, veteran and six-home management directly from the dock; selected military buildings and Town Halls also expose contextual management. **Worldtree** opens research. The city selector inspects allies read-only. Ready/Unready, whole-match pause and host combat speed remain authoritative. Combat sectors are six columns wide, with closer local camera framing and an optional sharp forest arena over a soft viewport backdrop. Successful nonfinal combat opens three reward cards. Resource income is visible beside stocks, and construction cards reflow with the floating dock. **City details** inspects doctrine, rewards, progression and recent feedback; the match controls retain important notices until resolved. **×** clears plot selection. **Esc**, **Menu**, F10 or controller Start opens only a menu square over the current view, with no backdrop or dimming; Continue/Esc closes it without leaving or pausing. There is no permanent Run dashboard or history footer. See [gameplay controls](docs/gameplay/PLAYING.md).

The menu offers **Return to main menu** and **Exit Game**, each with a confirmation explaining session loss. Solo progress is not saved; a host leaving ends the match for everyone; a guest disconnects and can reconnect only while the same host session remains running. Cancel/Esc keeps the session open. Opening the menu does not pause the match. Esc first closes an open Settings, details or rewards panel. A fresh match gets a new identity and personal doctrine offers. Offers derive from match/player identity independently of simulation RNG, remain stable through resize/settings/reconnect, and require a fresh explicit selection after restarting the client. The seven-column board is incompatible with this topology: board version **2**, combat rules **8**, wire protocol **17** reject older sessions/snapshots rather than migrating combat positions.

A legal opening: Farm on plot 1, Barracks on plot 2, Metal Mine on plot 3, then Lumbermill on plot 4 using the **4 Gold recovery** action. Advance preparation, recruit two Swordsmen, advance again and recruit two more, then ready for combat. Costs, economy, combat and drafting are always validated by Core.

Settings controls music volume, defaulting to 30%, with zero muting. Saved preferences retain their chosen volume. Preferences persist under Godot's user directory; music continues across session changes and retains the soundtrack's authored intro and loop. See [music provenance](assets/audio/README.md).

Presentation options: `--world-art debug|sprites` (`--render-mode` remains an alias), `--hud-art plain|illustrated`, `--view topdown|angled`, `--debug-overlay on|off`. Missing/corrupt PNGs use geometric/plain fallback. Ordinary play and exports use sprites/illustrated/angled with the debug overlay off. `play-debug` selects debug/plain/topdown. For example, `mise run play -- --view topdown` overrides the view. Twenty-nine supplied cutouts (including the detailed forest-crystal pack and retained prototype shrine) and their Pillow pipeline are described in [artwork](docs/ARTWORK.md). No editor import or new artwork is required.

## Battle and artwork development tools

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

Battle showcases and the asset showroom are development tools, separate from ordinary play. `profile-animation` repeats native battles at 30/60/120 FPS and three combat speeds; `capture-environment` records matched Off/Full environment timings, and `review-environment` presents those results. Measurements from software rendering describe that environment and do not establish physical GPU performance.

Combat poses, shots, statuses, hits and deaths follow authoritative playback. Showcase scenarios are earned through legal commands. [Animation documentation](docs/COMBAT_ANIMATION.md) covers controls, receipts, captures and known static-sprite limitations.

Sprite combat now uses the supplied [modular Rain / Ember arena](docs/MODULAR_BATTLE_ARENA.md).
All snapshot cells remain playable (this branch uses 6×9 per sector). The camera
starts near your sector; drag with middle/right mouse, zoom with the wheel and
choose **Field overview** or F for the complete field. `mise run capture-arena`
checks 1–6 sectors in both projections at 720p/1080p using legal combat replays.

## Verification

```sh
mise run build
mise run test-core
mise run test-shared
mise run test                       # Pillow + .NET/process tests + animation captures
mise run test-session
mise run test-gameplay-ui          # default + debug native UI; seven-wave host/guest replay
mise run capture-gameplay-ui       # actual panels at 720p/1080p and allied inspection
mise run test-multiplayer
mise run test-reconnect
mise run test-network-faults
mise run test-steam                 # offline rules/state tests
mise run test-steam-native -- host FRIEND_STEAM_ID  # explicitly gated
mise run capture
mise run capture-art
mise run capture-settlement        # nine-pad map picking, purchase and actual screenshots
mise run capture-opening           # six doctrines, three waves, actual production controls
mise run capture-veterans          # history, recovery/recruit comparison and reviewed retirement
mise run capture-bosses            # real W10/W20 forecasts, health/windup/death and restart
mise run capture-lobby
```

[Sessions](docs/SESSIONS.md) documents networking, reconnect credentials and the live Steam gate. [Verification](docs/VERIFICATION.md) records executed checks and platform gaps. Captures are actual viewport PNGs under `artifacts/captures/godot/`; logs are under `artifacts/test-logs/`. The session checks include a startup with no presentation flags, exercising the production menu, doctrine selection, purchases, combat, rewards and resizing. Gameplay checks run those same production Controls in default and explicit debug presentation. Capture checkpoints replay legal commands into real snapshots; they do not replace the gameplay UI.

Deterministic checkpoints (`initial`, `settlement`, `building-selected`, `battle-start`, `combat`, `draft`, `post-draft`) replay the authoritative rules, with seed `20261018`.

```sh
mise run capture -- --width 1920 --height 1080
mise run play-assets -- --checkpoint draft --view angled
mise run play-assets -- --checkpoint sprite-settlement --debug-overlay on
mise run scenario post-draft artifacts/scenarios/post-draft.json
```

## Distribution and CI

```sh
mise run build-linux
mise run build-windows
```

Self-contained x64 Release outputs are `artifacts/builds/godot/linux/CommonWatch.x86_64` and `artifacts/builds/godot/windows/CommonWatch.exe`. Copy the **whole output folder**, including managed runtime, optional assets, music and licenses. Asset lookup is executable relative. Exports start with the same artwork/angled defaults as `mise run play`. Exported game arguments follow Godot's separator, for example: `CommonWatch.x86_64 -- --world-art debug --hud-art plain --view topdown`.

Install matching **4.7.2.stable.mono export templates** in Godot's standard user `export_templates` directory before exporting. The [Godot GitHub Actions workflow](.github/workflows/godot.yml) fetches the pinned upstream templates and verifies their SHA-256. Builds use RID-specific locks under `tools/nuget-locks`, preserving Core's original lockfile. Tasks replace only their own output directory.

One workflow builds the entire solution, runs headless/art/session/Steam logic and real graphical/multiplayer/reconnect/animation checks, cross-builds Linux/Windows and uploads complete archives plus diagnostics. It checks existing graphical prerequisites and never installs system packages. Live Steam tests are excluded. Cross-building Windows does not qualify Windows runtime execution.

## Ownership

- `src/Game.Core`: pinned upstream gameplay and Arch ECS with the documented six-player extension, independently buildable/testable.
- `src/Game.Shared`: solo adapter, deterministic scenarios/playback helpers, bounded snapshot codec, private resume files, logical projection and asset metadata; no graphical dependencies or transport framework.
- `src/Game.Godot`: sole graphical application, native Controls/Node2D, ENet RPC sessions, GodotSteam, audio and presentation resources.
- `tests`: headless rules, shared/session logic, artwork and actual Godot process checks.
- `tools`: reproducible CLI launch, capture, scenario, profiling, artwork and release tasks.

`Scenes/Main.tscn` declares the application, session owner, Camera2D, settlement/battlefield views and audio owner. `Application/GameApplication.cs` coordinates them; `UI` owns native screens, `Combat` owns sampled poses and Sprite2D figures, `Art` caches external PNGs, and `Networking/Steam` owns native callback lifetimes. `UI/WatchTheme.tres`, `Combat/HitFlash.gdshader` and `Audio/default_bus_layout.tres` centralize presentation resources. Small hex boards/effects retain efficient custom drawing; logical angled projection and deterministic playback remain independent of camera transforms.

Ordinary development edits C#, text scenes/resources and scripts, then runs mise; opening the Godot editor is optional. See [agent instructions](AGENTS.md), [source provenance](docs/PROVENANCE.md) and [GPL-3.0-only license](LICENSE).

The sprite settlement now uses the supplied [illustrated forest map](docs/SETTLEMENT_MAP.md): nine elliptical clearings, open hover/selection rings, gold icons on locked land and foundation-anchored buildings without world labels. The nine-slot rules and native inspector actions are unchanged. Missing map assets fall back to the debug grid. `mise run capture-settlement` verifies fixed-camera picking, ignored drag/wheel input and resizing and captures empty, hovered, selected and built states at 720p/1080p.

The [painted forest HUD](docs/PAINTED_FOREST_HUD.md) adds authored RGBA paintings to
the preserved supplied theme. Settings, a vertical resource ledger, selected context,
construction/navigation and the wave/Ready/Pause panel frame the settlement. The
same native composition also works in plain mode.
Selected buildings expose recruitment, squads, Town Hall management and the six homes/towers.
Selecting a construction card spends nothing; **Build selected** confirms it.
`COMMON_WATCH_GAMEPLAY_ART=illustrated mise run capture-gameplay-ui` captures the
actual forest HUD at 720p/1080p, including network feedback states.

The illustrated [Worldtree](docs/WORLDTREE.md) uses the supplied static shrine scene,
18 circular research controls and framed canonical details. `mise run capture-worldtree`
verifies native input and captures empty, hovered, locked, available and acquired
states at 720p/1080p. Plain mode and missing artwork retain the text tree.

Optional [dynamic environment effects](docs/DYNAMIC_ENVIRONMENT.md) bring the existing illustrated scenery to life. Settings offers **Off / Subtle / Full** (Off by default) and a saved **Weather preset** selector with Scene default plus all seven presets, or launch with `--environment-effects full --environment-preset rain`. See the linked guide for all presets, scene effects and timing controls. `mise run test-environment` captures repeatable animated frames and Off/Full frame timings with the existing renderer and artwork.
