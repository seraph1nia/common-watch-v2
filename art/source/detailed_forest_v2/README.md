# Common Watch — detailed forest-crystal sprites, v2

This pack converts the TWO APPROVED, DETAILED GENERATED SHEETS into separate assets. It does not use the earlier simple geometric SVG illustrations.

## Contents

- `svg/buildings/`: 10 transparent, path-based SVG building illustrations.
- `svg/units/`: 5 transparent, path-based SVG character illustrations.
- `assets/sprites/`: 15 game-size transparent PNGs made from the approved painted cutouts. These retain the source appearance most closely and are the recommended runtime files.
- `svg_exports/`: 15 transparent PNGs rendered FROM the SVGs, for comparison or use after choosing the traced appearance.
- `source/cutouts/`: separated painted PNGs at their original cutout resolution.
- `source/sheets/`: both approved sheets, preserved byte-for-byte.
- `previews/`: actual vector-render contact sheet and source-versus-vector comparison. Preview backgrounds and labels are NOT in the sprites.
- `metadata/`: stable IDs, file paths, anchors, dimensions, suggested catalog/extraction entries, hashes, and validation results.
- `tools/install_pngs.py`: optional, standard-library-only installer. Dry-run by default; backs up overwritten files when explicitly applied.

## Included assets

| Game ID | File stem |
| --- | --- |
| `building/mine` | `mine` |
| `building/metalmine` | `metalmine` |
| `building/archeryrange` | `archeryrange` |
| `building/researchtower` | `researchtower` |
| `building/arrowtower` | `arrowtower` |
| `building/catapulttower` | `catapulttower` |
| `building/stonecutter` | `stonecutter` |
| `building/weaver` | `weaver` |
| `building/market` | `market` |
| `building/townhall` | `townhall` |
| `unit/berserker` | `berserker` |
| `unit/crossbowman` | `crossbowman` |
| `unit/mage` | `mage` |
| `unit/enemy_crossbowman` | `enemy_crossbowman` |
| `unit/enemy_mage` | `enemy_mage` |

This is the 15-asset selection from the approved sheets, not a new complete roster. It does not include a new Farm, Lumbermill, Barracks, Arcanum, Swordsman, Enemy Swordsman, or Enemy Berserker.

## SVG quality and editability

The SVGs contain actual filled/stroked Bezier paths, not PNG images hidden inside SVG wrappers. There are no embedded images, external resources, fonts, scripts, filters, or opaque background rectangles.

They are AUTOMATIC HIGH-DETAIL TRACES, not hand-authored, semantically layered vector art. Individual paths can be edited, but roofs, arms, weapons, and leaves have not been organised into independent animation parts. Detailed tracing produces large SVG files (approximately 4.5–10 MB each uncompressed). Smooth paint transitions, subpixel edges, and glow transparency are approximations. Enlarging these traces does not recreate detail absent from the original sheets.

Use the `assets/sprites` PNGs for the closest match to the approved painted artwork. Use `svg_exports` to see the exact traced appearance at the same game dimensions. `previews/source_vs_svg.png` shows both.

## Game dimensions and anchors

Buildings: 512 × 512 canvas, suggested display size 142, anchor `(0.5, 0.92)`.
Characters: 256 × 256 canvas, suggested display size 46, anchor `(0.5, 0.92)`.

Transparent padding places each cutout's bottom centre at the ground anchor. Contact shadows and small decorative ground patches from the approved images are retained. These anchors are starting points, not in-game-calibrated collision or animation data. Logical occupancy remains the game's responsibility.

The approved single-view camera perspective is preserved. This asset pack DOES NOT change camera tilt, ground compression, hex geometry, selection, sorting, or gameplay code. Buildings and characters should remain upright rather than being squashed with the ground plane.

## Install the PNGs in the current game

The Common Watch loader inspected for this pack checks the PNG signature and resolves conventional `.png` filenames. The SVGs are editing/source assets; simply renaming an SVG to `.png` will not work.

### Recommended: backed-up installation with catalog merge

Extract this ZIP somewhere outside the repository. From the extracted pack directory:

```sh
python3 tools/install_pngs.py /absolute/path/to/common-watch-v2
```

That command only displays the proposed changes. Apply them explicitly:

```sh
python3 tools/install_pngs.py /absolute/path/to/common-watch-v2 --apply
```

The installer writes the 15 painted PNGs and merges only their entries into `assets/catalog.json`. Other catalog entries are retained. It backs up any replaced PNGs and the previous catalog under `artifacts/art-backups/` before changing them. It makes no C# or simulation changes.

To install the SVG-rendered PNGs instead of the source-painted PNGs:

```sh
python3 tools/install_pngs.py /absolute/path/to/common-watch-v2 --variant vector --apply
```

Then restart the game from the repository:

```sh
mise run play-assets -- --view angled
```

Each illustration appears only when its corresponding building/unit is actually present. This pack does not spawn new gameplay entities.

### Manual installation

Merge this pack's `assets/sprites/` folder into the repository's `assets/sprites/`, after backing up any matching filenames. These files replace the matching sprites from the earlier pack. When an existing catalog uses custom paths or sizes, merge the relevant records in `metadata/catalog_entries.json`; do not replace a full existing catalog with that partial file.

## Preserve changes through the repository art pipeline

The installer is a local PNG/catalog installation, not full pipeline registration. Existing extraction manifests may regenerate older artwork later.

For long-term integration, copy `source/cutouts/{buildings,units}` to `art/source/detailed_forest_v2/{buildings,units}` and merge `metadata/extraction_entries.json` into `art/manifests/extraction.json`, replacing matching IDs rather than duplicating them. Keep other existing entries and source provenance intact. Then run the repository's established art processing/validation/install commands. The pipeline may make a small padding/rounding adjustment when regenerating these sources under its own settings.

## Validation performed

All 15 SVGs were parsed as XML, checked for actual vector paths and absence of embedded/external images, and rendered using CairoSVG. All painted and vector-export PNGs were checked for RGBA format, expected canvas size, transparent corners, and visible content inside the canvas. An actual SVG-rendered contact sheet and light/dark transparency comparison were visually reviewed.

No Godot execution, game capture, gameplay test, animation test, or native platform qualification was performed for this pack. The installation helper was tested on a disposable filesystem fixture, not on your checkout.

## Inspected repository references

- https://github.com/seraph1nia/common-watch-v2/blob/main/src/Game.Shared/VisualAssets.cs
- https://github.com/seraph1nia/common-watch-v2/blob/main/src/Game.Godot/Art/AssetLoader.cs

The pack does not modify your repository, local Downloads directory, or game until you copy files or explicitly run the installer with `--apply`.
