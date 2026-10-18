# Optional world sprites

These PNGs are processed from the user-supplied sheets preserved in `art/source/`. The default debug renderer ignores them. `--world-art sprites` (or `--render-mode sprites`) enables native decoding and geometric fallbacks.

`assets/catalog.json` maps stable IDs to world PNGs under this folder and HUD skins under `assets/hud/`, with anchors, display sizes and nine-slice margins. Building/unit IDs retain lowercase enum names. `terrain/woodland_hex` is the installed tile; the older optional `terrain/hex` identifier remains valid. Resource IDs are `resource/gold`, `resource/wood` and `resource/stone`.

Missing or corrupt images never affect positions, hit areas or rules. See [the reusable Pillow workflow](../../docs/ARTWORK.md), [contact sheet](../../art/previews/contact-sheet.png) and [provenance](../../art/manifests/provenance.json).

`building/arcanum` is a masked **prototype** from a separate user-supplied ZIP; see [prototype provenance](../../art/manifests/prototype-provenance.json) and the production-artwork limitations above. Existing PNGs remain unchanged. Terrain uses six catalog `ground_uv` points to map only its top surface onto projected logical hexes; unqualified terrain uses geometry. Buildings and units remain upright single sprites.
