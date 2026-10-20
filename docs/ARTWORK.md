# Pillow to Godot 2D

The optional [modular battle arena](MODULAR_BATTLE_ARENA.md) adds 16 byte-preserved
component PNGs to the existing 29 crop entries. Its supplied RGB ground images
and RGBA strips/cutouts use explicit `arena/*` IDs and source hashes. The
assembled alternatives remain source-only. The tiled combat descriptions below
also describe the retained missing-pack fallback.

The current sprite settlement uses the [single illustrated forest map](SETTLEMENT_MAP.md), with nine measured elliptical pads and no world text labels. The earlier tiled-settlement sections below document the retained cutout pipeline and historical comparisons; terrain tiles still belong to combat/showrooms. Debug settlement uses its native grid fallback.

The supplied archive was inspected for absolute paths, traversal and symlinks, then its four PNGs were extracted once into `art/source/`, retaining filenames and bytes. `art/manifests/provenance.json` records the archive hash, source hashes, RGBA modes and dimensions (all 1536 × 1024). The archive's README is retained unchanged. The archive is no longer an input: no archive discovery, extraction script, configuration or mise task exists.

The separately supplied `~/Downloads/common_watch_cutout_prototype.zip` was inspected and safely extracted to a temporary directory for the tilted-view work. It contains four prepared transparent cutouts, four rough versions and an opaque scale preview. Only the unchanged 512 × 512 shrine source is retained under `art/source/prototypes/`. [Prototype provenance](../art/manifests/prototype-provenance.json) records the archive hash, every inspected image's hash/dimensions/alpha and the reuse decisions. Builds and processing never read Downloads.

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

The sheets have useful cutout alpha already. Hidden RGB pixels retain their painted backgrounds; RGB-only viewers can make the files appear opaque. Processing zeros alpha values at most 2 and normalizes values at least 250. Every other alpha value and foreground RGB is retained. No global RGB threshold is used. Explicit crop-local polygon masks (at most two pixels feather) and boundary-connected color flood cleanup are supported. The shrine uses a measured polygon mask with 0.6 pixel feather to remove its broad baked hex platform while retaining roots, steps and low foliage. No color flood is enabled. A dark object enclosed within the foreground is never removed by the boundary flood.

Buildings use 512 × 512, units 256 × 256, resource icons 128 × 128 and the woodland hex 256 × 256. Visible contents are trimmed and uniformly fitted into transparent canvases. Buildings/units use paired `anchor` (canvas coordinates) and `content_anchor` (trimmed-content coordinates). The detailed integration measures supporting foundations or feet rather than using the lowest decoration; uncalibrated inputs default to bottom-center `(0.5, 0.92)`. Terrain/resources are centered. Canvas size, artwork silhouette and display size never affect occupancy, commands or selection.

## Installed selection

| Stable ID | Source sheet | Installed file |
| --- | --- | --- |
| `building/lumbermill` | 01 | `assets/sprites/buildings/lumbermill.png` |
| `building/farm` | 01 | `assets/sprites/buildings/farm.png` |
| `building/barracks` | 01 | `assets/sprites/buildings/barracks.png` |
| `building/arcanum` | Supplied shrine prototype | `assets/sprites/buildings/arcanum.png` |
| `unit/swordsman` | 03 | `assets/sprites/units/swordsman.png` |
| `unit/enemy_swordsman` | 04 | `assets/sprites/units/enemy_swordsman.png` |
| `terrain/woodland_hex` | 01 | `assets/sprites/terrain/woodland_hex.png` |
| `resource/gold`, `resource/wood`, `resource/stone` | 04 | `assets/sprites/resources/{gold,wood,stone}.png` |
| `hud/panel`, `hud/card_frame`, `hud/button`, `hud/tooltip` | 04 | `assets/hud/{panel,card_frame,button,tooltip}.png` |

`assets/catalog.json` is the filesystem catalog. World entries are relative to its assets directory, with `sprites/` prefixes; HUD entries have `hud/` prefixes. `--assets-root` retains its existing meaning: the **sprites directory**, whose parent contains the optional catalog and HUD directory. Without a catalog, the original conventional world filenames still work. Catalog paths cannot escape the assets directory. Invalid catalogs, absent PNGs, dimension mismatches and native decode failures use plain/geometric fallbacks. Debug/plain defaults bypass image decoding and catalog reading. Replacement paths, display size, offset and anchor can change in the catalog without changing C# or simulation rules. Regenerate through manifests to retain reproducible source metadata.

## Views and modes

```sh
mise run play
mise run play-assets -- --view angled
```

The application accepts `--world-art debug|sprites` (alias for the retained `--render-mode`), `--view topdown|angled`, `--hud-art plain|illustrated`, and `--debug-overlay on|off`. Defaults are debug, topdown, plain and off. World and HUD artwork are independent preferences. Debug world rendering never loads world PNGs; an explicitly illustrated HUD may still load its four HUD PNGs.

The `play-assets` and `battle-assets` tasks prefer the angled view; `play-assets` also enables the illustrated nine-slice HUD. Append `-- --hud-art plain` for plain controls, or `-- --view topdown` for the topdown comparison. Bare application/debug defaults remain unchanged.

Angled projection compresses the logical ground plane by 0.72 and uses the same inverse transform for picking. Buildings and characters retain their image aspect ratios. Camera2D owns fixed settlement and responsive combat framing, AssetLoader caches native external PNG textures and BattlefieldView owns combat presentation; detached metadata and logical projection remain engine independent. Terrain finishes before depth-sorted tall sprites. Anchor/bounds overlays and logical hex borders remain visible. Godot's viewport uses native window dimensions so tall showroom windows and resized Controls have real layout space.

Terrain now draws as one textured polygon per logical cell using the **same six corners** as its outline. `ground_polygon` records six crop-local points on the source's top surface, in `HexProjection.Corners` order (upper-right, lower-right, bottom, lower-left, upper-left, top). Processing converts these through trimming, canvas padding and actual rounded resize dimensions into normalized `ground_uv` catalog coordinates. Godot maps that surface directly onto projected corners: it does not compress the complete upright terrain cutout or apply the source's perspective twice. Source perimeter/side faces remain outside the UV footprint. Geometry supplies the fallback when the texture or six qualified UVs are absent. Future tall terrain decoration belongs in an upright object sprite, outside this ground-only surface.

The retained woodland PNG's bytes are unchanged. Its mapped surface fills adjacent cells without the old unprojected rectangular overhang. Board outlines finish after terrain in settlement and combat. Settlement sprites and their captions draw from far to near by projected ground Y, then X, so a rear caption no longer paints over a nearer shrine. Logical selection still uses the inverse projection and native Camera2D inversion. All sprite anchors and scales remain catalog controlled; the shrine retains 148 world pixels with its measured root-foundation anchor (see below).

Combat retains the original authoritative movement interpolation. Godot samples idle, movement, five-stage melee, hit and death poses from playback timing. Native procedural shots, magic, statuses and structure effects accompany real events. No animation frames, rig or simulation writes were introduced. Debug figures receive equivalent transforms and effects. See [the combat animation experiment](COMBAT_ANIMATION.md) for actual captures, legal showcases and limitations.

## Nine-slice HUD

Godot uses native `NinePatchRect` behind normal Controls, with a shared `UI/WatchTheme.tres` theme and application-scoped PNG cache. A non-Control parent keeps Container layout from resetting its scale. Native tooltip ownership supplies a skinned Control; labels, values, costs, reward descriptions and button interaction remain native UI.

The source margins are retained in the catalog:

| Skin | Source dimensions | Left / top / right / bottom | Native border scale |
| --- | --- | --- | --- |
| Panel | 488 × 190 | 64 / 64 / 64 / 64 | 0.42 |
| Card frame | 201 × 251 | 48 / 48 / 48 / 48 | 0.75 |
| Button | 251 × 104 | 32 / 32 / 32 / 32 | 0.42 |
| Tooltip | 241 × 97 | 30 / 30 / 30 / 30 | 0.50 |

These are source-coordinate cuts, not destination padding. Scaling the native border leaves corners fixed at their intended display size. Text is inset separately. Tooltip text is sampled from the hovered logical settlement plot. Missing skins retain ordinary UI and contrasting text.

[The reusable nine-slice preview](../art/previews/nine-slice-preview.png) includes 160 × 48 button, 480 × 160 panel, 640 × 420 dialog and 260 × 390 card. [The contact sheet](../art/previews/contact-sheet.png) shows all twenty-nine cutouts.

```sh
mise run art-showroom
mise run capture-art
```

Showrooms construct no authority and advance no match. They show assets near their game display sizes, anchors/bounds and native nine-slices at the preview sizes with separate text. The showroom scrolls at smaller viewport sizes. World assets are paginated ten per page; append `-- --showroom-page 2` or `3` to inspect the full roster. Buildings use the same upright drawing and projected terrain as the settlement. Interactive tasks use normal audio; capture/check tasks own Xvfb and temporary audio settings.

Art captures are fourteen real application frames under `artifacts/captures/godot/art/`: `settlement-debug.png`, `settlement-angled.png`, `settlement-sprites.png`, `shrine-angled.png`, `shrine-anchors.png`, `shrine-topdown.png`, `combat-sprites.png`, `hud-plain.png`, `hud-illustrated.png`, `nine-slice-small.png`, `nine-slice-large.png`, `asset-showroom.png`, `asset-showroom-2.png`, `asset-showroom-3.png`. Small/large checks use 1280 × 720 and 1920 × 1080; showroom is 1280 × 1080. `sprite-settlement` builds Lumbermill/Barracks/Stonecutter, settles production, then pays for Arcanum on plot 5. Scenario captures use legal replay, never fabricated match state.

## Source limitations

- All thirteen required selections were usable with their existing alpha. None required unreliable automatic foreground separation. The preserved contact shadows/grass islands belong to the illustrations; they are not isolated roofs/walls or mathematically perfect silhouettes. Fine fringe/shadow pixels remain deliberately conservative.
- The parchment texture has directional painted grain. Large nine-sliced centers amplify it into vertical streaks, especially in a tall inspector. The corners and borders stay intact, but this source is not a seamless production skin.
- Sheet 03 buttons have embedded labels; they were rejected in favor of the unlabelled sheet 04 button. The sheet 03 pointed tooltip was also bypassed in favor of the unlabelled rectangular parchment source. No painted gameplay text was copied into installed images.
- Sheet 02's scene crops include scenery/labels, making it less suitable than the chosen cutouts. It is preserved unchanged for future manual selection.
- The available enemy sword bearer is a green-skinned raider. It is a cosmetic stand-in for the authoritative enemy swordsman; faction and rules remain unchanged. No replacement art was created.
- The original Lumbermill, Farm, Barracks and prototype Arcanum are retained, supplemented by the ten detailed buildings and five units below. Unsupplied IDs (including Enemy Berserker, which had no installed PNG at import) and independent combat structures retain geometric fallbacks. Angled building/unit artwork is shown upright as-is in topdown comparison mode.
- These are static illustrations. Animation is procedural motion of one PNG per unit. The repeated woodland tile and painted ground islands do not form a production seamless terrain set.
- Current validation is Linux/Xvfb/Mesa. This change does not claim native Windows rendering or qualification of physical display/audio hardware. Platform publishing tasks and executable-relative asset layout are retained; release builds were not rerun for this artwork work.

## Remaining production artwork

The new shrine is explicitly a **prototype**, not a finished production asset. Its broad hex platform is masked out, but the cutout still has angular lower edges, small grass/ground fragments and no professionally painted contact shadow. It establishes scale, anchoring and a bright cyan fantasy direction in the actual game. The guardian was not substituted for a swordsman: it has a gray cutout remnant and a different silhouette/identity. Tree/waterfall cutouts have substantial diorama bases and no requested matching IDs, so they were not installed. No generation or download was used, consistent with repository instructions.

| Requested ID | Current usable integration | Work still needed for a unified production set |
| --- | --- | --- |
| `building/arcanum` | New 512 × 512 masked prototype, 148 px | Artist cleanup or replacement, isolated roots/shadow, clean alpha edges |
| `terrain/woodland_hex` | Existing 256 × 256 PNG with qualified top-surface UVs | Cleaner repeatable grassy surface; fewer large rocks and obvious repeated paths |
| `building/lumbermill`, `building/barracks` | Existing 512 × 512 PNGs, 142/148 px, unchanged | Matching faceted fantasy style, upper-left light and ground-only contact shadows |
| `unit/swordsman`, `unit/enemy_swordsman` | Existing 256 × 256 PNGs, 46 px, unchanged | Matching fantasy silhouettes/palette and faction identity at gameplay scale |

Supply independent transparent PNGs, preserve one sprite per ID, and replace source/crop/anchor/display-size entries in `extraction.json`; regenerate, validate and install through the existing commands. For a replacement terrain surface, measure its six top-surface points instead of guessing from the full canvas. Original source sheets and existing installed production PNGs remain intact.

## Detailed forest-crystal v2 integration (2026-10-18)

The user-supplied `common_watch_detailed_svg_sprite_pack.zip` was inspected outside the repository: the expected `common_watch_detailed_sprites/` root contained the approved detailed painted cutouts and actual path SVGs. Archive entries were checked for absolute/traversal paths and links before extraction into a temporary directory; all 72 supplied checksum records passed. The standard-library installer was read, dry-run, inspected, then applied with its default **painted** variant and a version-1 catalog merge. No simplified vector pack or SVG-export PNGs were used.

The permanent source root is `art/source/detailed_forest_v2/`: `buildings/` and `units/` preserve all 15 painted cutouts byte-for-byte, `svg/` preserves their actual editable SVGs, and `sheets/` retains both approved original sheets. `metadata/`, `README.md` and `SHA256SUMS.txt` retain the supplied records unchanged. [Detailed provenance](../art/manifests/detailed-forest-v2-provenance.json) records the archive hash, validation and every retained file hash; prior provenance records are unchanged. SVGs are source-only, outside the Godot project and the exported `assets/` directory. Builds never read Downloads or the extraction directory. No artwork was generated, retraced, recolored or replaced.

The extraction manifest merges these IDs with the original fourteen entries (29 unique entries total):

- Buildings: `building/mine`, `building/metalmine`, `building/archeryrange`, `building/researchtower`, `building/arrowtower`, `building/catapulttower`, `building/stonecutter`, `building/weaver`, `building/market`, `building/townhall`.
- Units: `unit/berserker`, `unit/crossbowman`, `unit/mage`, `unit/enemy_crossbowman`, `unit/enemy_mage`.

Runtime PNGs are regenerated from painted sources by the existing deterministic pipeline. Canvases remain 512 × 512 and 256 × 256; new display sizes remain 142 and 46. Existing display sizes remain 142 for Lumbermill/Farm and 148 for Barracks/Arcanum. No display sizes changed. Every pre-existing installed image, including HUD and terrain, remains byte-for-byte identical; only their appropriate ground metadata changes. No pre-existing Enemy Berserker PNG was present, so its fallback remains available.

### Ground registration

The original default put the bottom of grass, rail extensions, stairs or a shadow at the plot center. Supporting foundations consequently sat behind that point. The existing `content_anchor` field can express the correction: pair a measured pivot inside the trimmed content with its existing location in the output canvas. This preserves the original silhouette, uniform fit and transparent padding while moving the supporting point to the projected plot center at draw time. No processing or sprite draw-code change, additional mask or artificial shadow was needed. `ground_point` documents each crop-local measurement; `ground_note` explains it. Both are reproducible manifest data; the pipeline uses `content_anchor` and `anchor`, and a Pillow regression checks their transform after cropping, trimming and resize rounding.

| Asset ID | Crop-local supporting point (pixels) | Canvas anchor | Reason |
| --- | --- | --- | --- |
| `building/lumbermill` | 255, 347 | (0.5000, 0.7470) | Center of the fenced sawmill/log-yard foundation, above foreground logs and grass. |
| `building/farm` | 244, 337 | (0.5071, 0.7502) | Center of the farmhouse/fenced garden ground, above foreground planting. |
| `building/barracks` | 252, 367 | (0.5078, 0.7257) | Courtyard foundation center; front gate and grass lie in front of the support. |
| `building/arcanum` | 256, 355 | (0.5144, 0.7884) | Root foundation behind the front steps; retained prototype mask removes the broad platform. |
| `building/mine` | 163, 320 | (0.5000, 0.7751) | Mine entrance foundation behind the projecting cart rails and front crystals. |
| `building/metalmine` | 153, 334 | (0.5012, 0.7941) | Forge foundation center, behind the foreground ore baskets. |
| `building/archeryrange` | 140, 360 | (0.5054, 0.7982) | Platform support center, behind the front targets and steps. |
| `building/researchtower` | 125, 471 | (0.5043, 0.8331) | Tower root foundation center behind the front stairs and flowers. |
| `building/arrowtower` | 133, 410 | (0.5010, 0.8305) | Trunk support center, above foreground rocks and foliage. |
| `building/catapulttower` | 137, 370 | (0.5011, 0.8161) | Timber support foundation, behind the foreground ammunition. |
| `building/stonecutter` | 148, 315 | (0.5012, 0.7752) | Saw/workshop foundation behind foreground stone blocks. |
| `building/weaver` | 146, 323 | (0.5012, 0.7966) | Workshop support center behind foreground bolts and paving. |
| `building/market` | 143, 290 | (0.5013, 0.7938) | Market stall footprint center behind front baskets and paving. |
| `building/townhall` | 152, 391 | (0.5010, 0.8066) | Hall foundation behind the projecting stairs and flowers. |
| `unit/berserker` | 265, 559 | (0.4967, 0.8652) | Midpoint of planted feet; excludes axe edge and trailing ground leaves. |
| `unit/crossbowman` | 234, 455 | (0.5000, 0.8702) | Midpoint between planted boots, above the foreground leaf fringe. |
| `unit/mage` | 249, 551 | (0.5130, 0.8759) | Feet/root contact point, excluding staff, robe flare and floating crystals. |
| `unit/enemy_crossbowman` | 275, 440 | (0.5052, 0.8649) | Midpoint of planted feet, above cape scraps and ground shadows. |
| `unit/enemy_mage` | 278, 455 | (0.5277, 0.8646) | Midpoint of feet, excluding staff silhouette and cape fringe. |

The Arcanum remains the pre-existing masked prototype, with its known angular ground fragments and illustrated-perspective limitations. Its source and existing conservative mask are unchanged. The new cutouts retain their painted grass, small paving and contact shadows. Placement does not turn those painted ground islands into seamless terrain; no asset is claimed to have received an artist cleanup pass.

### Settlement geometry and framing

Actual Godot comparisons used the unchanged legal `sprite-settlement` checkpoint at 1280 × 720: radius **65**, **81.25** (+25%) and **91** (+40%), with the same artwork, anchors, zoom 1 and illustrated HUD. **81.25** is the smallest tested increase that leaves convincing ground around the foundations; 91 spends more space on terrain without improving the ordinary footprint fit. Tall roofs and crystals naturally overlap the row behind. Foundation fit is judged independently of the whole canvas. Showroom pages cover the tall Research Tower and wider Market/Mine outside the legal settlement.

`SettlementView.DefaultHexRadius` owns spacing. The same projection supplies centers, six corners, terrain polygons, borders and inverse picking; its horizontal origin is the midpoint of the nine-plot bounds. Ground compression stays **0.72** angled / **1** topdown. Upright images retain their proportions. The six terrain top-surface UVs, source bytes and ground-position depth ordering are unchanged. Battlefield radius and framing are independent and unchanged.

The current settlement uses proportional full-viewport cover with protected clearing bounds and fixed camera input. The HUD overlays the map. Resizing recalculates framing; combat separately fits its rendered bounds to 80% of the viewport. See [settlement framing](SETTLEMENT_MAP.md).

### Captures, verification and rollback

`bash tools/graphical.sh capture-forest` reproduces radius comparisons, both projections at 1280 × 720 / 1920 × 1080, normal/closer zoom, all three showroom pages and legal ranged/structure battles. Close zoom is 1.3 at 720p / 2.2 at 1080p. Outputs and transcripts are under `artifacts/detailed-forest-v2/` (ignored). The original before capture is preserved separately, using the old catalog/PNGs and old radius/zoom:

- `before/settlement-angled-1280.png` and `after/settlement-angled-1280.png`: same legal state, dimensions and illustrated HUD, original versus revised default.
- `comparison/radius-{65,81.25,91}.png`: constant-zoom size comparison.
- `after/settlement-{angled,topdown}-{1280,1920}.png`: final defaults, overlays off.
- `after/settlement-{angled,topdown}-{1280,1920}-close-anchors.png`: diagnostic bounds/anchors at closer zoom.
- `after/showroom-{1,2,3}.png`: every installed asset, all 15 additions, actual Godot drawing without authority.
- `after/combat-{ranged,structures}.png`: legal battle showcases; unprovided figures retain fallbacks and combat structures retain their existing geometric presentation.

The full original assets/catalog, manifests and previews are backed up in `artifacts/art-backups/detailed-forest-v2-original/`; the reviewed installer also backed up the prior catalog in `artifacts/art-backups/detailed-forest-v2-20261018T205302Z-6e937efe/`. To roll back the import, restore the original extraction manifest and catalog/previews, remove only the fifteen new runtime PNG paths listed in the new source metadata, and restore the original assets tree. Original presentation/text files are also retained at matching paths in that backup tree, with step-by-step `ROLLBACK.txt`; review and preserve unrelated later edits before restoring them. Source preservation may remain, or remove only `art/source/detailed_forest_v2/` and its new provenance file. Run the ordinary art pipeline after restoring the original manifest. Backups are local ignored artifacts, not a substitute for a release archive.

Executed checks: `art-process`, `art-validate`, `art-install`, `test-art` (16 passed), `build` (zero warnings/errors), `test-core` (40 passed), `test-shared` (64 passed), `test-session` (2 actual-process tests passed), `capture-art` (14 actual frames), and the additional `capture-forest` comparisons/showrooms/battles. `test-animation` also passed: 8 tests, 19 captured phases verified in each of debug and sprites, plus missing-sprite and angled shader-fallback captures (`test-animation.log`). One intermediate session run was invalidated by editing the capture shell helper while it was executing (shell EOF after both smoke passes); the helper passed `bash -n` and the final rerun passed without changes to product behavior. No native JIT crash occurred during the completed checks. Repeating process/validate/install produced byte-identical installed output with 29 unique entries and all registered paths resolving locally (`pipeline-receipt.json`). Native picking covers nine centers and six inset corners per cell, both projections, two viewport sizes and zooms 0.55/1/2.2, plus real mouse selection, modal input isolation and camera reset. Linux Xvfb/Mesa captures are actual game frames; native Windows, live Steam and physical audio/display hardware remain unqualified. The existing intermittent native .NET/JIT crash risk remains.
