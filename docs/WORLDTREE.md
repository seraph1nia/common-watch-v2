# Illustrated Worldtree

`--hud-art illustrated` reskins the existing Worldtree tab with the supplied static
scene, eighteen circular native buttons and the decorative details frame. The
background keeps its source aspect ratio (1672 × 941, approximately 16:9), centered
with letterboxing. Node geometry and the frame share the same image-to-screen
transform. No artwork is generated or repainted at runtime.

The seven unchanged supplied PNGs live in `assets/hud/worldtree/`. The application
asset cache owns their textures. Export tasks already copy the complete `assets`
folder and look it up beside the executable, so Downloads is never a runtime
input. Original delivery notes are in `art/source/worldtree/`, with archive and
per-file hashes in `art/manifests/worldtree-provenance.json`.

## Layout

`assets/hud/worldtree/layout.json` contains measured source-pixel centers,
diameters, reused forest HUD icon identifiers and the details rectangle. It contains
no research prices, prerequisites or effects. The delivered background actually
contains **14 painted sockets**, although its README says eighteen. Those fourteen
centers are used directly; four supplemental sockets use the supplied ring art
near the lower tree and are marked `supplemental` in the layout. All eighteen
canonical technologies remain accessible. Painted branches are decorative; the
details panel identifies actual prerequisites and exclusive branches.

Each socket has a circular hit area, independent pointer hover, keyboard/controller
focus and selected state. Locked and acquired nodes remain inspectable. A tier mark
and acquired check distinguish reused icons. Focus has a separate crisp outline;
owned nodes retain an inner gold circle while hovered or selected. Decorations
ignore mouse input. The existing modal shade, tabs, Esc handling, nested Settings
and focus restoration remain in use. The illustrated tab reduces dialog chrome to
keep the scene large at 720p; other tabs keep their normal dialog frame.

The details panel uses the real catalog and detached city research eligibility.
The purchase action stays below its scrolling description and submits the existing
`GameplayAction.Research` intent through `GameplayContext.Submit`. Foreign-city,
pending, disconnected, paused and other editing restrictions still use the common
context guard. Core, research rules, canonical costs, production offshoots and
snapshot formats are unchanged.

Plain mode never decodes these PNGs or reads the layout. Missing/invalid layout or
any missing/undecodable Worldtree PNG falls back as a whole to the native scrolling
text tree, sharing the same selection/details/purchase flow.

## Verification and captures

```sh
mise run build
mise run test-core
mise run test-shared
mise run test-session
mise run test-gameplay-ui
mise run capture-art
mise run capture-worldtree
```

`capture-worldtree` owns its virtual display and disposable audio settings. It earns
the `research` checkpoint with legal commands, verifies all eighteen socket bounds
and details clearance, tests circular corners, actual mouse hover/click, controller
directional focus, keyboard activation and a paid authoritative purchase at both
1280 × 720 and 1920 × 1080. It writes actual viewport PNGs under
`artifacts/captures/godot/worldtree/`:

- `worldtree-{1280,1920}-empty.png`
- `worldtree-{1280,1920}-hover.png`
- `worldtree-{1280,1920}-locked.png`
- `worldtree-{1280,1920}-available.png`
- `worldtree-{1280,1920}-acquired.png`

Long requirements/effects can scroll inside the frame at 720p; purchase remains
outside that scroll area. Linux software-rendered checks do not qualify native
Windows execution or live Steam accounts. The repository's known intermittent
native .NET/JIT crash risk remains.

## Changed files

- `src/Game.Godot/UI/ResearchTreePanel.cs`: retained research presentation and fallback.
- `src/Game.Godot/UI/WorldtreeNode.cs`: native circular button and visual states.
- `src/Game.Godot/UI/WorldtreeArt.cs`: validated image-space layout and all-or-nothing pack load.
- `src/Game.Godot/UI/GameplayDialog.cs`: integrated tab sizing and compact illustrated chrome.
- `src/Game.Godot/UI/HudSkin.cs`, `src/Game.Godot/Art/AssetLoader.cs`: optional artwork/cache ownership.
- `src/Game.Godot/Application/InputActions.cs`: explicit native controller accept/cancel mappings, retaining keyboard defaults.
- `src/Game.Godot/Development/WorldtreeChecks.cs`, `GameplayChecks.cs`, `StartupChecks.cs`: graphical input/purchase probes.
- `tools/graphical.sh`, `mise.toml`: bounded capture task on a private virtual display.
- `assets/hud/worldtree/`: seven original PNGs and `layout.json`.
- `art/source/worldtree/`, `art/manifests/worldtree-provenance.json`: delivery notes and hashes.
- `README.md`, `docs/PROVENANCE.md`, `docs/WORLDTREE.md`: usage, attribution and verification.

## Executed checks (2026-10-19)

- `mise run build`: passed, no warnings/errors.
- `mise run test-core`: 40 passed; `mise run test-shared`: 66 passed.
- `mise run test-session`: actual native graphics/session/audio/camera checks passed.
- `mise run test-gameplay-ui`: native gameplay plus seven-wave hosted/remote replay passed.
- `COMMON_WATCH_GAMEPLAY_ART=illustrated mise exec -- bash tools/graphical.sh test-gameplay`: passed, including nested Settings/Esc and tab transitions.
- `mise run capture-art`: all existing actual art captures passed.
- `mise run capture-worldtree`: ten actual viewport captures; native mouse picking, controller navigation/accept/cancel, keyboard activation, authoritative research purchase and focus restoration at both resolutions.
- Two additional `test-gameplay` runs with disposable external asset roots: missing Worldtree pack and a text-corrupt locked-node PNG both passed using the plain research fallback. No simulation fixtures or user preferences were changed.
- Supplied PNGs verified byte-identical to the ZIP; `git diff --check` passed.

Logs: `artifacts/worldtree-build.log`, `worldtree-test-core.log`,
`worldtree-test-shared.log`, `worldtree-test-session.log`,
`worldtree-test-gameplay-ui.log`, `worldtree-test-illustrated.log`,
`worldtree-capture-art.log`, `worldtree-focused.log`, and
`worldtree-test-fallback.log`.

The complete `mise run test` bundle, distribution exports, native Windows execution
and live Steam verification were not run for this presentation change. The known
native .NET/JIT crash risk remains; no new runtime crash is claimed resolved.
