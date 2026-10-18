# The Common Watch V2 — Generated Art Source Sheets

Four original generated source sheets (1536 × 1024 PNG), provided as **source references for an asset-processing pipeline**.

- `01-world-sprites.png`: three buildings, two character illustrations, woodland hex.
- `02-world-and-hud-reference-sheet.png`: labeled reference catalog with buildings, units, resources, terrain and UI parts.
- `03-angled-sprites-and-hud-sheet.png`: additional buildings, terrain, unit poses, icons and HUD components.
- `04-expanded-art-and-hud-sheet.png`: expanded buildings, units, terrain, resources and UI components.

## Important notes

These are **composite concept-art sheets, not individually cut transparent sprite files**. The visual backgrounds, labels and shadows may require masks and cleanup after cropping. An RGBA channel does not by itself guarantee a clean transparent object background.

For nine-slice UI, crop the appropriate plain panel/card/button first, clean its background and any nearby labels, then set slice margins after visual inspection. Avoid letting corner ornaments extend into a stretched edge.

Keep original sheets unchanged in `art/incoming/`; place processed PNGs separately in the game's asset catalog.
