# Illustrated settlement map

`mise run play-assets` uses the supplied nine-clearing forest map. Debug/plain remains the default. Missing, corrupt or mismatched map PNG/metadata falls back to the native nine-slot grid. Both `--view` presets leave the illustrated background's painted perspective intact; combat and the fallback grid retain their own projections.

The original 1672 × 941 PNG is retained byte-for-byte at `assets/sprites/terrain/settlement_map.png`. Its SHA-256 is `f72bb78e769ee7c3fb97d111f00ae27d595ee1652d64d47cd7e140e3ed1ccdc5`. The user supplied it as `Enchanted Forest Settlement Map.png`; the application never reads Downloads. No artwork was generated, downloaded or repainted. No separate artwork license was supplied; the code license is not asserted as an artwork grant.

`assets/sprites/terrain/settlement_map.json` records the measured centers and inner elliptical radii of the painted stone rims, building foundation anchors, reserved label positions and upright building sizes in original image pixels. Slot indices 0–6 occupy the seven central clearings; 7 is upper-right and 8 is lower-right. Labels are intentionally not drawn. Painted buildings are decorative scenery. The original nine authoritative slots, five initially purchased plots and four expansion prices are unchanged. Unpurchased empty plots show the existing gold icon, and selecting any plot opens the existing construction/purchase/upgrade inspector.

`SettlementMapLayout` validates this bounded presentation file independently of Godot. `AssetLoader` loads the map through its existing application-scoped optional PNG cache and verifies dimensions against the layout. The PNG uses the stable `terrain/settlement_map` identifier; its adjacent layout is separate from the unchanged cutout catalog and extraction pipeline. Ordinary art-process/install operations preserve these two files. Export tasks already copy the complete repository-local `assets/` directory.

`SettlementView` draws the background once, then open hover/selection rings, then upright building PNGs sorted by foundation Y/X. Existing catalog foundation anchors exclude stray shadows/grass; sprite scaling is uniform. The source's camera angle is never applied a second time. Native Camera2D inversion followed by the settlement node's local transform converts mouse coordinates to image pixels for ellipse picking. Camera framing uses the JSON frame, leaving all nine pads accessible at 1280 × 720 and 1920 × 1080. Pan, cursor-centered zoom, resize and reset retain their existing behavior. Hover clears over HUD panels and modal dialogs. The background and buildings have no separate camera transforms.

## Verification and captures

```sh
mise run build
mise run test-core
mise run test-shared
mise run test-session
mise run capture-art
mise run capture-settlement
```

The last command owns Xvfb, bounded process supervision and temporary audio preferences. It earns all state through ordinary commands. It captures empty pads, a hovered locked pad, selection, pan/zoom, purchased land with a Farm, and the paid four-building shrine scenario in `artifacts/captures/godot/settlement-map/`. Each filename includes the viewport width. The same process verifies native clicks on all nine pads at both sizes, real middle-drag/wheel/click input, purchase/construction controls, and transform-based picking at twelve inset ellipse points per pad in both view presets at zooms 0.55/1/2.2 with pan applied. Ellipse bounding-box corners and painted buildings/bridges are excluded. Shared tests cover invalid/duplicate coordinates. Ordinary session checks also exercise empty/corrupt/missing assets and modal input isolation.

Verification receipts and native-platform limitations are recorded in [VERIFICATION.md](VERIFICATION.md). These are Linux/Xvfb/Mesa frames; they do not qualify native Windows or live Steam. The existing intermittent native .NET/JIT crash risk remains.

## Changed files

- Assets: `assets/sprites/terrain/settlement_map.png`, `assets/sprites/terrain/settlement_map.json`.
- Runtime: `src/Game.Shared/SettlementMapLayout.cs`, `src/Game.Shared/VisualAssets.cs`, `src/Game.Godot/Art/AssetLoader.cs`, `src/Game.Godot/World/SettlementView.cs`, `src/Game.Godot/Application/GameApplication.cs`.
- Verification: `tests/Game.Shared.Tests/SettlementMapTests.cs`, `src/Game.Godot/Development/CameraChecks.cs`, `src/Game.Godot/Development/SettlementMapChecks.cs`, `src/Game.Godot/Development/StartupChecks.cs`, `tools/graphical.sh`, `mise.toml`.
- Documentation: this page, `README.md`, `docs/ARTWORK.md`, `docs/PROVENANCE.md`, `docs/VERIFICATION.md`.
