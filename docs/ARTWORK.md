# Pillow to native 2D clients

The supplied archive was inspected for absolute paths, traversal and symlinks, then its four PNGs were extracted once into `art/source/`, retaining filenames and bytes. `art/manifests/provenance.json` records the archive hash, source hashes, RGBA modes and dimensions (all 1536 × 1024). The archive's README is retained unchanged. The archive is no longer an input: no archive discovery, extraction script, configuration or mise task exists.

## Reproduce the installed artwork

From the project root:

```sh
mise run art-setup      # once: local venv, pinned Pillow 11.3.0
mise run art-process    # source PNGs + extraction/processing manifests -> art/processed
mise run art-validate   # staged RGBA PNGs, hashes, dimensions, slice margins
mise run art-install    # validated PNGs + catalog -> assets/
mise run art-preview   # optional: repeat preview rendering
mise run test-art
```

Python 3 with venv/pip is needed for setup; tasks do not install system packages. `.venv-art/` and staged output are ignored. Original sheets, manifests, installed PNGs, catalog and previews are retained. `mise run test` includes the Pillow tests and therefore needs `art-setup` on a fresh checkout. Future artwork starts with individual source PNGs and manifest entries; it never depends on Downloads.

`extraction.json` contains explicit, non-grid crop rectangles, stable IDs, output paths, normalized anchors, display sizes and actual nine-slice margins. `processing.json` sets canvases, padding and conservative alpha cleanup. Crops use Pillow's half-open `[left, top, right, bottom]` convention. The pipeline validates paths, crops, alpha, dimensions, duplicate IDs, anchors and slice centers before installation. Output metadata includes source/crop dimensions, visible bounding boxes, content offsets, uniform resize scale, alpha statistics and input/output SHA-256s. There are no timestamps or random image operations.

The sheets have useful cutout alpha already. Hidden RGB pixels retain their painted backgrounds; RGB-only viewers can make the files appear opaque. Processing zeros alpha values at most 2 and normalizes values at least 250. Every other alpha value and foreground RGB is retained. No global RGB threshold is used. Optional explicit crop-local polygon masks (at most two pixels feather) and boundary-connected color flood cleanup are available for future inputs; neither is needed for these selected cutouts. A dark object enclosed within the foreground is never removed by the boundary flood.

Buildings use 512 × 512, units 256 × 256, resource icons 128 × 128 and the woodland hex 256 × 256. Visible contents are trimmed and uniformly fitted into transparent canvases. Buildings/units place the bottom of their visible content at the configurable ground anchor, normally `(0.5, 0.92)`. Terrain/resources are centered. Canvas size, artwork silhouette and display size never affect occupancy, commands or selection.

## Installed selection

| Stable ID | Source sheet | Installed file |
| --- | --- | --- |
| `building/lumbermill` | 01 | `assets/sprites/buildings/lumbermill.png` |
| `building/farm` | 01 | `assets/sprites/buildings/farm.png` |
| `building/barracks` | 01 | `assets/sprites/buildings/barracks.png` |
| `unit/swordsman` | 03 | `assets/sprites/units/swordsman.png` |
| `unit/enemy_swordsman` | 04 | `assets/sprites/units/enemy_swordsman.png` |
| `terrain/woodland_hex` | 01 | `assets/sprites/terrain/woodland_hex.png` |
| `resource/gold`, `resource/wood`, `resource/stone` | 04 | `assets/sprites/resources/{gold,wood,stone}.png` |
| `hud/panel`, `hud/card_frame`, `hud/button`, `hud/tooltip` | 04 | `assets/hud/{panel,card_frame,button,tooltip}.png` |

`assets/catalog.json` is the filesystem catalog. World entries are relative to its assets directory, with `sprites/` prefixes; HUD entries have `hud/` prefixes. `--assets-root` retains its existing meaning: the **sprites directory**, whose parent contains the optional catalog and HUD directory. Without a catalog, the original conventional world filenames still work. Catalog paths cannot escape the assets directory. Invalid catalogs, absent PNGs, dimension mismatches and native decode failures use plain/geometric fallbacks. Debug/plain defaults bypass image decoding and catalog reading. Replacement paths, display size, offset and anchor can change in the catalog without changing C# or simulation rules. Regenerate through manifests to retain reproducible source metadata.

## Views and modes

```sh
mise run play-godot
mise run play-monogame
mise run play-godot-no-assets
mise run play-monogame-no-assets
mise run play-godot-assets -- --view angled --hud-art illustrated
mise run play-monogame-assets -- --view angled --hud-art illustrated
```

Both clients accept `--world-art debug|sprites` (alias for the retained `--render-mode`), `--view topdown|angled`, `--hud-art plain|illustrated`, and `--debug-overlay on|off`. Defaults are debug, topdown, plain and off. World and HUD artwork are independent preferences. Debug world rendering never loads world PNGs; an explicitly illustrated HUD may still load its four HUD PNGs.

Angled projection compresses the logical ground plane by 0.72 and uses the same inverse transform for picking. Buildings and characters retain their image aspect ratios. Each engine owns camera pan/zoom and native texture drawing; only detached metadata, projection and cosmetic animation sampling are shared. Terrain finishes before depth-sorted tall sprites. Anchor/bounds overlays and logical hex borders remain visible. Godot's viewport uses native window dimensions so tall showroom windows and resized Controls have real layout space.

Combat retains the original authoritative movement interpolation. Drained attack/hit events drive short lunges and flashes, playback lifecycle/ticks drive death fade, and cosmetic time drives small idle bobs. No animation frames, rig or simulation writes were introduced. Debug circles receive equivalent motion/fade feedback.

## Nine-slice HUD

Godot uses native `NinePatchRect` behind normal Controls. A non-Control parent keeps Container layout from resetting its scale. Native tooltip ownership supplies a skinned Control; labels, values, costs, reward descriptions and button interaction remain native UI.

MonoGame uses Gum `NineSliceRuntime`, including native button backgrounds, with `CustomFrameTextureCoordinateWidth` and `BorderScale`. The pinned Gum version uses one source border width for both axes. Measured crops deliberately use equal margins around their actual ornaments, so no normalized duplicate texture or custom renderer is required:

| Skin | Source dimensions | Left / top / right / bottom | Native border scale |
| --- | --- | --- | --- |
| Panel | 488 × 190 | 64 / 64 / 64 / 64 | 0.42 |
| Card frame | 201 × 251 | 48 / 48 / 48 / 48 | 0.75 |
| Button | 251 × 104 | 32 / 32 / 32 / 32 | 0.42 |
| Tooltip | 241 × 97 | 30 / 30 / 30 / 30 | 0.50 |

These are source-coordinate cuts, not destination padding. Scaling the native border leaves corners fixed at their intended display size. Text is inset separately. MonoGame premultiplies straight-alpha PNG pixels once for SpriteBatch/Gum. Tooltip text is sampled from the hovered logical settlement plot. Missing skins retain ordinary UI and contrasting text.

[The reusable nine-slice preview](../art/previews/nine-slice-preview.png) includes 160 × 48 button, 480 × 160 panel, 640 × 420 dialog and 260 × 390 card. [The contact sheet](../art/previews/contact-sheet.png) shows all thirteen cutouts.

```sh
mise run art-showroom-godot
mise run art-showroom-monogame
mise run capture-godot-art
mise run capture-monogame-art
```

Showrooms construct no authority and advance no match. They show assets near their game display sizes, anchors/bounds, a debug fallback and native nine-slices at the preview sizes with separate text. Godot's showroom can scroll; MonoGame's 1280 × 1080 showroom fits the full layout. Interactive tasks use normal audio; capture/check tasks own Xvfb and temporary audio settings.

Art captures are real application frames under `artifacts/captures/{godot,monogame}/art/`: `settlement-debug.png`, `settlement-angled.png`, `settlement-sprites.png`, `combat-sprites.png`, `hud-plain.png`, `hud-illustrated.png`, `nine-slice-small.png`, `nine-slice-large.png`, `asset-showroom.png`. Small/large checks use 1280 × 720 and 1920 × 1080; showroom is 1280 × 1080. Scenario captures use legal replay, never fabricated match state.

## Source limitations

- All thirteen required selections were usable with their existing alpha. None required unreliable automatic foreground separation. The preserved contact shadows/grass islands belong to the illustrations; they are not isolated roofs/walls or mathematically perfect silhouettes. Fine fringe/shadow pixels remain deliberately conservative.
- The parchment texture has directional painted grain. Large nine-sliced centers amplify it into vertical streaks, especially in a tall inspector. The corners and borders stay intact, but this source is not a seamless production skin.
- Sheet 03 buttons have embedded labels; they were rejected in favor of the unlabelled sheet 04 button. The sheet 03 pointed tooltip was also bypassed in favor of the unlabelled rectangular parchment source. No painted gameplay text was copied into installed images.
- Sheet 02's scene crops include scenery/labels, making it less suitable than the chosen cutouts. It is preserved unchanged for future manual selection.
- The available enemy sword bearer is a green-skinned raider. It is a cosmetic stand-in for the authoritative enemy swordsman; faction and rules remain unchanged. No replacement art was created.
- Only the requested three building types and two unit types are illustrated. Other buildings (including the legal scenario's Metal Mine), structures and future unit IDs visibly use debug shapes. Angled artwork is also shown as-is in topdown comparison mode.
- These are static illustrations. Animation is procedural motion of one PNG per unit. The repeated woodland tile and painted ground islands do not form a production seamless terrain set.
- Current validation is Linux/Xvfb/Mesa. This change does not claim native Windows rendering or qualification of physical display/audio hardware. Platform publishing tasks and executable-relative asset layout are retained; release builds were not rerun for this artwork work.
