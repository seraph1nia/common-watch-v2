# Moonleaf — Common Watch forest HUD

A restrained woodland interface kit for the current Common Watch 2D presentation.
The art uses branch-and-gold borders, ink-green readable surfaces, cream lettering,
cyan focus, violet research/pause accents, leaves, lanterns, crystals and mushrooms.

## Included

- 78 original editable SVGs and matching transparent PNGs.
- 6 panel/frame styles; 16 button variants/states; 4 card states; 4 tab states;
  independent focus overlay; 4 progress textures; 1 separator; 3 textless badges.
- 31 resource/action icons and 8 fixed-scale woodland ornaments.
- `src/Game.Godot/UI/ForestHud/ForestTheme.tres`: texture-based Godot Theme starter.
- `ComponentPreview.tscn`: isolated native component scene, not a gameplay screen.
- State, nine-slice, content-padding and behavior metadata.
- 1920×1080 idle/build/Army layout studies, plus a 1280×720 idle study and asset sheet.
- `IMPLEMENTATION_PROMPT.txt` and `HUD_LOGIC.md`.

**These are original vector UI parts, not newly generated painterly illustrations
or cutouts from the HUD reference screenshot.** The simpler surfaces are intentional:
the map carries the detail while the HUD makes information legible. The SVGs contain
real paths and gradients, no embedded bitmap images and no font files.

## Scope and qualification

This ZIP is an asset/design delivery. It has not modified either GitHub repository.
The PNG/SVG outputs and resource references have been checked locally. Godot is not
installed in the asset-authoring environment, so the Theme, scene, interaction and
integration have NOT been qualified inside Godot. Treat the Theme as a starter to
review and validate with the game's pinned version.

All screenshots under `previews/` are composited design mockups with illustrative
values, not actual Godot captures and not authoritative match states. Never import
those values into production logic. Existing approved map/character art is used
only within the mockups; those assets are not replacement runtime files in this ZIP.
The Army study shows one homogeneous Berserker squad, not a mixed-class squad.

## Runtime installation

Review the archive and merge its `src/` folder into a local Common Watch checkout.
This adds `src/Game.Godot/UI/ForestHud/` without replacing current UI classes or the
existing theme. Do not blindly extract unrelated docs/source files over game files.
Back up any same-name resources from an earlier iteration first.

Load the theme ONLY through the illustrated HUD branch of `HudSkin`:

    res://UI/ForestHud/ForestTheme.tres

Keep plain HUD mode unchanged. The current `HudSkin` creates parchment-specific
Label colors and adds a separate NinePatch background to buttons. Those overrides
must not continue to mask this theme's normal/hover/pressed/disabled/focus styles or
put dark parchment text on these dark surfaces. Replace that illustrated branch
rather than stacking both skin systems.

Do not add this entire new icon/frame namespace to the old world asset catalog by
blind JSON merging. These starter resources use native `res://UI/ForestHud/` paths.
If you elect to route them through the existing PNG cache instead, intentionally
extend and validate its supported HUD identifiers and metadata first.

No image-generation service, new engine, runtime library, or editor import dialog
is required to use the supplied PNGs. Preserve the existing dependency locks.
Godot's normal import pipeline still needs to process new project resources during
its usual prepare/build workflow.

## Native theme use

Use native Buttons and containers; keep all text, prices, values, badges and actions
as native controls bound to current state. Native type variations included:

- `ForestPrimaryButton`: progression/Ready, green.
- `ForestArcaneButton`: Pause/Resume match, violet.
- `ForestDangerButton`: supported destructive actions, with existing confirmations.
- `ForestCard`: build/reward selection, not an automatic purchase.
- `ForestRibbon`, `ForestInset`, `ForestModal`: PanelContainer variants.

Base Button/PanelContainer, TabContainer/TabBar, TooltipPanel and ProgressBar styles
are included. Native widgets not covered should inherit the game's existing styles
or receive small coherent native overrides; do not remove functionality to fit the kit.

The theme supplies no font. Reuse existing properly licensed game fonts. Preview
fonts are not bundled and are not a guarantee of identical runtime line breaks.

## Nine-slicing and ornaments

`metadata/hud_manifest.json` records each texture size, source, role, slice margins,
content padding and suggested minimum dimensions. Source-pixel slice margins are
not equivalent to native content padding. Test resizing without stretching corners.
Do not add per-panel canvas scaling to compensate for a wrong texture contract.

The simple center of a frame is intentionally uniform to prevent directional grain
from turning into stretched streaks. Most decorations live in separate fixed-size
textures. Use them sparingly outside text/card regions; do not nine-slice or distort
leaves, mushrooms, lanterns or crystals. Alpha glows are already included.

The focus ring is independent of selection and hover. Do not replace it with a
color-only hover tint. Native keyboard focus must still work with pointer hover,
selected toggles, and modal restoration.

All ornaments should use `MouseFilter = Ignore`. The full-screen HUD layout root
also ignores mouse input. Visible real controls/panels consume input in their actual
rectangles; a modal scrim stops the whole world only while that modal is open.

## Recommended layout

Three persistent regions: one resource/phase ribbon; one collapsing context dock;
one next-action area. Four top-level destinations: Build, Army, Worldtree, Run.
Details open on demand, never all at once. Preserve the existing arena switch,
forecast, run rewards, reconnect feedback, allied read-only inspection, settings,
Worldtree, troop management and six army homes. See HUD_LOGIC.md for routing.

At 720p, reflow; do not proportionally shrink the 1080p interface and its fonts.
The narrow mockup demonstrates fitting the actual map within the remaining usable
region; blurred image margins are only a layout-study treatment, not a new required
game effect. The native implementation must use real HUD/world bounds and preserve
all nine interactive pads. Body text should stay at least 14px and controls at least
44px high; supplied button caps favor a 48px minimum.

## Reproducibility

PNG assets are ready to use. Native editable sources are under `source/svg/`.
`tools/generate_art.py` regenerates those SVGs/PNGs and the art manifest;
`tools/build_theme.py` regenerates the Theme, sample scene and behavior contract.
These optional authoring scripts use Pillow and CairoSVG; the exact versions used
are recorded in `metadata/validation.json`. They are not new game dependencies.
Do not silently install/upgrade tools to regenerate them during ordinary integration.

`tools/validate_pack.py` checks the delivered files, resource references, SVG safety,
alpha, dimensions, slice bounds and absence of embedded image/font data. It does not
start Godot or qualify UI behavior. Regeneration changes may require rebuilding the
package's `SHA256SUMS.txt`; never relabel stale hashes as current.

## Sources and provenance

`PROVENANCE.md` lists the source-code documents consulted and the supplied artwork
used only for layout mockups. No graphics, fonts, audio or UI kit from odot-game were
copied into this delivery. That repository was used for behavior/design reference.
No rights to third-party assets or to the supplied reference images are newly
asserted by this pack. Preserve existing source artwork provenance and licenses.
