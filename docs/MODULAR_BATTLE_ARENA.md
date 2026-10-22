# Modular battle arena

The supplied Rain / Ember pack is installed as an optional sprite presentation.
`BattlefieldView` and the preparation preview use the real detached `ArenaState`;
Core, transport, deployment, targeting and route planning are unchanged.

## Topology and coordinates

This checkout uses **6 columns × 9 rows per sector**, board version 2. The pack's
October 20 report described an older 7-column checkout. `ArenaLayout` derives
columns and sector order from the actual snapshot's enemy entry cells, rather
than importing either number from the artwork. Every live cell is retained.
The projection tests cover both 6- and 7-column repeat distances.

The repeat distance is `columns * sqrt(3) * radius`, not the sector's staggered
bounding width. All cells retain their global odd-row parity. Ground, structures,
units, event effects and inverse picking share the same projection center.
Angled ground uses the existing 0.72 compression; upright sprites and decorative
bands retain their aspect ratios. Background and foreground strips sit outside
the active rows. Exactly two endcaps sit outside the complete board's outermost
corners, with continued ground underneath their transparent inner edges.

Ground variants follow `1, 2, 3, 2, 1, 3`; their matching backdrops repeat at the
same distance. Hex outlines are separate native geometry. For optional atmosphere,
including static-rain replacement and supplied smoke/embers/glow, see
[dynamic environment effects](DYNAMIC_ENVIRONMENT.md). Assembled plates are retained
only in the source pack, never installed or layered over these components.

A separate viewport CanvasLayer places a heavily pre-blurred, low-contrast,
slightly darkened copy of the supplied `backdrop_repeat_01` behind the sharp
arena. Pillow processes this scenery offline into `arena/atmosphere`; no units,
keep cutouts, hex outlines or UI are composited into it. The TextureRect preserves
aspect ratio with cover cropping, ignores mouse input and follows viewport size,
independently of camera pose and arena bounds. It uses no fullscreen blur pass.
The sharp scenery alone has a lightweight alpha shader: it stays fully opaque
over the entire playable bounding rectangle and feathers through the outer
decorative margins. There are no fades at sector joins, and actors, effects and
hex outlines do not inherit the material. Debug and missing-pack fallback remain
geometric and artwork independent.

## Structures and camera

The supplied keep and ArrowTower use the catalog ground anchor
`(0.5, 0.83984375)` at their actual `structure.Cell`. Their canvas sizes are 111
and 96 world pixels (3.7R and 3.2R). Catapults retain existing catapult artwork;
Watchposts now reuse the supplied defense-tower cutout at a smaller scale, with their own label/health; missing/corrupt artwork retains geometry. Buildings and units share one exact
ground-depth ordering; sampled bounce does not change that order. Health bars,
owner/kind labels, inspection, hit flashes and destruction fades follow playback
state and events. There are no independent damage or combat-animation clocks.

Entering the arena focuses the local player's sector with 25% more magnification
than the previous local framing, shifted slightly toward the keep. This default
does not zoom out as sectors are added. Middle/right drag pans; the wheel zooms;
R restores the closer local pose, including after **Field overview** or F fits
the whole field. Routine snapshot refreshes preserve the camera pose. Changing
projection, resolution or entering a new arena establishes fresh local framing.
Settlement retains its fixed framing and disabled pan/zoom.

`WorldArt` culls external textures before decoding; `AssetLoader` shares native
textures for the application lifetime. Actors outside the viewport are hidden,
while authoritative poses remain available for picking/aiming. No combined
six-sector texture is allocated. Missing/corrupt assets retain geometric/plain
fallback; debug mode does not decode world artwork. Arena cutouts require valid
catalog metadata so a missing ground anchor cannot silently misplace them.

## Sources and repeatable installation

The ZIP was checked for absolute/traversal paths, symlinks and excessive expanded
size, extracted without overwriting different local files, and verified against
every entry in its SHA256SUMS. The full pack, including README_NL, metadata,
previews, source images and optional offline tools, remains unchanged under
`art/source/modular_battle_arena/`. Archive provenance is recorded in
`art/manifests/modular-arena-provenance.json`.

Sixteen supplied component PNGs and one processed atmosphere have explicit
`arena/*` IDs in the existing extraction
manifest and `assets/catalog.json`. The pack's `metadata/assets.json` does not
replace that catalog. The pipeline's scoped `preserve_png` option copies these
supplied files byte-for-byte and verifies their source hashes. Existing crop
processing remains in use for the other 29 sprites.
The atmosphere entry records its scenery source hash and Pillow processing
parameters; the source pack and existing installed component PNGs stay intact.

```sh
mise run art-process
mise run art-validate
mise run art-install
mise run test-art
mise run capture-arena
```

Downloads and the original ZIP are not build/runtime dependencies. Exports copy
the same repository-local `assets/` directory through the existing build tasks.

## Actual graphical verification

`capture-arena` launches Godot on a supervised private Xvfb display with temporary
settings and silent automated audio. The scenario admits 1–6 players through
`AuthoritySession`, chooses doctrines, purchases an ArrowTower and readies all
players using accepted commands under default rules. It records real combat;
there are no injected units, fabricated structure states or edited rules.

For each roster it captures topdown/angled at 1280×720 and 1920×1080: initial
local framing, whole-field overview, real structure hit/destruction and (for
2–6 sectors) an actual interpolated move across a sector boundary. It checks all
cell centers through the native camera inverse, actor/structure picking, native
wheel zoom and drag persistence, the F overview action and native R reset back to
the local default. It asserts the 25% zoom increase and identical magnification
across roster sizes. For one/six sectors it additionally captures both extreme
pan/zoom poses and compares backdrop pixels, including filled viewport corners.
There are 132 actual
viewport PNGs and 24 result records in
`artifacts/captures/godot/modular-arena/verification.json`.

The existing animation capture suite separately covers friendly/enemy figures,
projectiles, statuses, shots, deaths and structure events. The pack's offline
previews are source artifacts and do not count as Godot verification. Executed
checks and platform gaps are recorded in [verification](VERIFICATION.md).
