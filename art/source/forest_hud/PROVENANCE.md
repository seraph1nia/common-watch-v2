# Sources and provenance

## UI art in this package

All SVG frames, icons and ornaments are new vector artwork authored for this
request using paths, basic shapes and gradients. Matching PNGs were rendered with
CairoSVG. No PNG is hidden inside an SVG. No font file, external image dependency,
or UI asset from either repository is redistributed.

The supplied `hud(1).png` was viewed as an art-direction and information-layout
reference only. Its painted controls, symbols, animals, labels and background were
not cut out into the runtime pack.

The mockup previews reuse the user's previously generated forest background
`enchanted_forest_settlement_map.png` and sample Berserker PNG from the supplied
`common_watch_detailed_svg_sprite_pack.zip`. They appear only in the composited
previews. Their original provenance/rights remain as provided; this pack asserts
no new license for these supplied illustrations or other third-party material.

## Repository behavior reviewed

Common Watch:
- https://github.com/seraph1nia/common-watch-v2/blob/main/README.md
- https://github.com/seraph1nia/common-watch-v2/blob/main/docs/gameplay/PLAYING.md
- https://github.com/seraph1nia/common-watch-v2/blob/main/src/Game.Godot/UI/SettlementHud.cs
- https://github.com/seraph1nia/common-watch-v2/blob/main/src/Game.Godot/UI/ResourcePanel.cs
- https://github.com/seraph1nia/common-watch-v2/blob/main/src/Game.Godot/UI/MatchControls.cs
- https://github.com/seraph1nia/common-watch-v2/blob/main/src/Game.Godot/UI/GameplayDialog.cs
- https://github.com/seraph1nia/common-watch-v2/blob/main/src/Game.Godot/UI/HudSkin.cs

Original game/reference implementation:
- https://github.com/seraph1nia/odot-game/blob/main/README.md
- https://github.com/seraph1nia/odot-game/blob/main/docs/ui-migration.md

The live source was read through the connected GitHub tool. Exact returned blob
hashes for reviewed receiving files are recorded in metadata/repository_review.json.
The implementation prompt tells the coding agent to inspect its current checkout;
source code may change after this design review.

Godot native skin reference:
- https://docs.godotengine.org/en/stable/classes/class_styleboxtexture.html
- https://docs.godotengine.org/en/stable/classes/class_control.html

No code, executable, icon pack, font or artwork from odot-game was copied into this
package. Its UI migration documentation was used to understand gameplay binding,
focus/input and permission boundaries, not as a mandate to import another framework.
