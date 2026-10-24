# Source provenance

## Painted HUD addition — 2026-10-21

The user explicitly authorized a dedicated HUD-only authoring phase. OpenAI's
built-in `image_gen` produced the original RGBA paintings retained in
[`art/source/painted_forest_hud`](../art/source/painted_forest_hud/README.md), guided
by the user-supplied reference in `CommonWatch_ReferenceHUD_Production_Kit.zip`.
The kit was specification/reference material, not finished assets. No opaque
reference crop was used as a runtime ornament. The manifest records original and
runtime hashes, dimensions, slice/content margins, state registration and anchors;
the prompt records and rejected first plaque document the revision. There are no
layered painting sources or traced painting SVGs. Four authored technical SVGs
contain only focus/phase geometry. Source code and technical SVG additions are
GPL-3.0-only. No separate license grant is inferred for the supplied reference.

The existing source art, installed world/audio assets, original ForestHud pack,
Worldtree and earlier provenance records remain unchanged. The historical records
below describe their original imports, independently of this HUD addition.

The user-supplied nine-clearing `Enchanted Forest Settlement Map.png` is retained unchanged as `assets/sprites/terrain/settlement_map.png` (1672 × 941, SHA-256 `f72bb78e769ee7c3fb97d111f00ae27d595ee1652d64d47cd7e140e3ed1ccdc5`). Its adjacent JSON records measured presentation coordinates. No artwork was generated, downloaded or repainted; no separate artwork license was supplied. See [settlement map](SETTLEMENT_MAP.md).

`src/Game.Core` was imported byte-for-byte from https://github.com/seraph1nia/odot-game at revision `501a3de7c3f5cf8b5e1726e01dbaefad5ea82f35` (2026-10-18 import). Copyright (c) 2026 Odot contributors; GPL-3.0-only; see root LICENSE. The local six-player extension adds `Match.MaximumPlayers` and updates admission, wave power, shared arena topology, snapshot validation and combat overflow/approach bounds in `World.cs`, `WavePower.cs`, `Combat/SharedArena.cs`, `Combat/HexBoard.cs` (now bounded to 324 cells), `Combat/SnapshotCompatibility.cs` and `Combat/CombatConfiguration.cs`. The subsequent requested six-column topology changes `SharedArena` sector width and tower rails, with board version 2, combat rules 8 and protocol 17 (`Diagnostics.cs`). Six army homes and nine settlement plots remain. Dependencies and the original repository remain unchanged. New slice code is distributed under GPL-3.0-only.

Four user-supplied generated PNG source sheets were extracted unchanged from `common-watch-v2-art-source-sheets.zip` on 2026-10-18. No replacement artwork was generated or downloaded. Original filenames, sheet dimensions and archive/source SHA-256s are recorded in [art provenance](../art/manifests/provenance.json); the accompanying [archive README](../art/source/ARCHIVE-README.md) is retained unchanged. Thirteen cropped/processed PNGs are installed under `assets/sprites` and `assets/hud`; their source crop and output hashes are in `assets/catalog.json`. See [processing and limitations](ARTWORK.md). The archive did not supply a separate artwork license; the code's GPL notice is not asserted as a grant for these supplied images.

The later user-supplied `common_watch_cutout_prototype.zip` contributes one additional prototype: `building/arcanum`. Its original shrine PNG is preserved unchanged under `art/source/prototypes/`; an explicit extraction-manifest mask removes the broad baked platform in the installed derivative. [Prototype provenance](../art/manifests/prototype-provenance.json) records the archive and all inspected files' hashes, dimensions and reuse decisions. The other prototype cutouts were not installed. The existing thirteen PNGs and four original source sheets are unchanged. No separate artwork license was supplied and no new artwork was generated or downloaded.

`assets/audio/LVS04_11_Echoes_of_Valhalla_bpm82_loop.wav` is copied unchanged from `src/Game/Assets/Music/` at the same pinned upstream revision. Its accompanying README retains the upstream music text, followed by a local Event feedback section. The track is **Echoes of Valhalla** by **LUShvalleySound**. The WAV SHA-256 is `3783f4deed1ecd7933a17538a1d880ffb04319f6e11f34dc66f2df7a05ab70b4`. See [the retained upstream music provenance](../assets/audio/README.md) for the owner's supplied-file history and the limits of the recorded licensing information; the music is not asserted to have the source code's GPL license.

## Intentional reward rules redesign — 2026-10-24

Roadmap #15 / issue #21 explicitly authorizes replacing the imported passive
battle catalog with immediate supplies/recovery after the presentation gate.
The [committed reward design](refinement-ii/21_IMMEDIATE_REWARDS.md) specifies
new offer identities, frozen quantities, actual receipts, overflow preflight,
removal of active reward modifiers, protocol 18 and required reward-rules version
1 before implementation. Combat rules 8 and battle-flow 3 are retained here.
This is an intentional local Core extension, not a byte-identical upstream claim.
The existing Worldtree, doctrine definitions, enemy profiles, pins, locks, licenses,
source artwork and authored audio remain unchanged by this reward scope.
The [original-rule replay route](refinement-ii/25_ORIGINAL_BASELINE.md) retains
exact original sources, commands and events; old offers are rejected rather than
silently migrated into new meanings. Implementation and economic evidence are
recorded separately from this design declaration.

## Preparation schedule comparison — 2026-10-24

Issue #22's [precommitted experiment](refinement-ii/22_PREPARATION_RHYTHM.md)
compares the immediate-reward three-stage baseline with two actual Ready
boundaries. The candidate delivers resource multipliers 2 then 1, while Research
and eligible stored healing occur once at each real boundary. The measured
selection retains three in ordinary play, with a guarded native Ready shortcut.
Two remains an explicit development comparison. Declaration7cc21db preceded Core
implementation87daf98: battle-flow4/protocol19 and a fingerprint of the authoritative
stage count. Combat8/reward1, default prices, enemies,
Worldtree and assets stay fixed. This intentional local Core experiment is
separate from the unchanged original import/archive and the #21 source baseline.

## Session work

`src/Game.Shared/SnapshotPayload.cs` and `SessionFile.cs` adapt the original project's proven codec and private credential file behavior. `tests/Game.Godot.Session.Tests/SteamOperationStateTests.cs` adapts its callback-state regression tests. The authority uses the pinned import plus the six-player extension documented above. Original lifecycle/RPC scene-reset infrastructure was not copied. New session implementations and tests are GPL-3.0-only.

GodotSteam is copied unchanged from the original project's pinned `src/Game/addons/godotsteam` distribution: **v4.22.1-gde**, SDK **1.65**, upstream archive `godotsteam-4.22.1-gdextension-plugin-4.4.zip`. The retained manifest contains its URL, archive SHA-256 and individual library hashes; licenses/readme are retained under `src/Game.Godot/addons/godotsteam`.

No artwork was created, imported or extracted for the session work. Lobby captures are actual application frames. The original repository was read only.

## Gameplay interface parity

`src/Game.Shared/Development/GameplayCampaignPolicy.cs` adapts the GPL-3.0-only
ordinary-command verification policy in upstream `tools/DevRunner/CampaignStrategy.cs`
at the same `501a3de7c3f5cf8b5e1726e01dbaefad5ea82f35` revision. It is used only to
earn deterministic development/test checkpoints. It is not a second simulation or
player-facing rules engine. New native Godot gameplay components, adapters and tests
are GPL-3.0-only. The original repository, artwork and soundtrack are unchanged; local Core changes are documented above.

## Forest HUD delivery

The user supplied `common_watch_forest_hud.zip` on 2026-10-19 (SHA-256
`6ae519625ec971801b6354943c18ec5bd920abf561df3bb3261a441149aadc4c`). Its 78
transparent PNG parts, `ForestTheme.tres` and component preview are preserved under
`src/Game.Godot/UI/ForestHud/`. The 78 editable SVG originals, README, HUD logic,
provenance, metadata and original delivery checksum list are retained unchanged in
[art/source/forest_hud](../art/source/forest_hud/). Runtime resources and SVGs are
checked against that delivery checksum list by `test-art`.

The checksum list also names archive previews and optional authoring scripts;
those files are not runtime inputs and were not installed. No authoring/generation
script was run and no game artwork was created or downloaded. The pack's mockups
were layout references only. Its supplied provenance makes no new third-party
asset or font license grant; existing GPL-3.0-only code notices, map, sprites and
soundtrack provenance remain intact. [Integration and verification](FOREST_HUD.md).

## Worldtree UI delivery

The user supplied `common_watch_worldtree_ui_pack.zip` on 2026-10-19. All seven PNGs
are installed unchanged in `assets/hud/worldtree/`; the archive and installed-file
SHA-256s are recorded in [Worldtree provenance](../art/manifests/worldtree-provenance.json).
The delivery README and manifest are retained in `art/source/worldtree/`. No image
generation, repainting or downloading was performed. The pack supplies no separate
artwork license; existing code GPL-3.0-only notices and upstream provenance remain
intact. The image has fourteen painted sockets despite the delivery's claim of
eighteen; the presentation adds four controls using its supplied ring textures.
See [layout, fallback and verification](WORLDTREE.md).
