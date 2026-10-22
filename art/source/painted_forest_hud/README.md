# Painted Forest HUD

HUD-only paintings authored with OpenAI's built-in `image_gen` on 21 October 2026,
under the user's explicit authorization. The supplied production kit was a
specification/reference delivery, not finished runtime artwork. Its reference is
retained in `references/`; no runtime ornament was extracted from it.

`masters/` contains the actual original generated RGBA PNGs. The tool produced no
layered painting files: these are raster originals, not layered/editable paintings
or traced SVGs. `rejected/plaque-v1.png` records the first construction plaque,
revised because its foliage and narrow reading aperture conflicted with native
text at 720p. `rejected/plaque-v2-inset.png` records the second version: its baked
portrait socket stretched into text at wide sizes. Only the final continuous-face
`masters/plaque.png` is exported. Cards attach an independent, aspect-preserving
40×40 portrait frame and 32×32 existing building sprite.

The first quality proof used panel, squirrel, mushroom, clock, Ready and Pause
masters before authoring the rest of the set. Godot's development-only
`Development/PaintedHudProof.tscn` assembles those separate components and native
text without constructing a game session. `proof/` contains actual proof renders
and a clearly labeled reference comparison; these are not settlement screenshots.

Reproduce deterministic runtime exports and validation:

```sh
mise run art-painted-hud
```

The pinned Pillow exporter trims insignificant alpha below 12, normalizes alpha
above 245, preserves intermediate alpha, adds transparent padding, and exports
frame sizes independently of native content margins. Normal, hover, pressed and
disabled states share exactly the same alpha and geometry. Their RGB changes use
fixed brightness/saturation transforms; the warm disabled plaque retains contrast.
Focus uses an independent cyan SVG contour. Selection adds a separate native gold
contour, independently of pointer hover and keyboard focus.

`manifest.json` records stable roles, source/runtime paths and dimensions, padding,
slices, content margins, normalized attachment/perch anchors, display ranges,
registration, provenance and SHA-256 checksums for **44 PNGs and four technical
SVGs**. Panel/inset/portrait/tooltip/interior reuse the panel master. The separator
reuses the sprig; lantern glow is a separate soft technical export. The vectors
contain actual vector geometry, never embedded paintings. `contact-sheet.jpg`
shows the PNG exports against alternating light and dark backgrounds.

Production textures live only in `src/Game.Godot/UI/PaintedForestHud/textures/`.
`HudSkin` applies them to a runtime theme copy. The original ForestHud pack, world
catalog, buildings, map, units, Worldtree, arena, effects and soundtrack remain
unchanged. Animal/mushroom/lantern/crystal decorations use uniform Sprite2D scaling
and separate attachment anchors; they ignore pointer input and never enter layout
measurement or authoritative timing. Runtime margins used by each region are in
the manifest; creatures' perch anchors are `(0.5, 0.95)`.

Generation instructions are retained in `generation-record.json`,
`proof-prompts.json`, `plaque-revision.json` and `plaque-fixed-portrait-revision.json`.
Existing code and technical vector
additions remain GPL-3.0-only. Supplied reference rights/provenance are retained;
no third-party artwork/font license grant is inferred from the reference kit.
