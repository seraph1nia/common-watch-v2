# Source provenance

`src/Game.Core` is imported byte-for-byte from https://github.com/seraph1nia/odot-game at revision `501a3de7c3f5cf8b5e1726e01dbaefad5ea82f35` (2026-10-18 import). Copyright (c) 2026 Odot contributors; GPL-3.0-only; see root LICENSE. No original repository files were modified. New slice code is distributed under GPL-3.0-only.

Four user-supplied generated PNG source sheets were extracted unchanged from `common-watch-v2-art-source-sheets.zip` on 2026-10-18. No replacement artwork was generated or downloaded. Original filenames, sheet dimensions and archive/source SHA-256s are recorded in [art provenance](../art/manifests/provenance.json); the accompanying [archive README](../art/source/ARCHIVE-README.md) is retained unchanged. Thirteen cropped/processed PNGs are installed under `assets/sprites` and `assets/hud`; their source crop and output hashes are in `assets/catalog.json`. See [processing and limitations](ARTWORK.md). The archive did not supply a separate artwork license; the code's GPL notice is not asserted as a grant for these supplied images.

The later user-supplied `common_watch_cutout_prototype.zip` contributes one additional prototype: `building/arcanum`. Its original shrine PNG is preserved unchanged under `art/source/prototypes/`; an explicit extraction-manifest mask removes the broad baked platform in the installed derivative. [Prototype provenance](../art/manifests/prototype-provenance.json) records the archive and all inspected files' hashes, dimensions and reuse decisions. The other prototype cutouts were not installed. The existing thirteen PNGs and four original source sheets are unchanged. No separate artwork license was supplied and no new artwork was generated or downloaded.

`assets/audio/LVS04_11_Echoes_of_Valhalla_bpm82_loop.wav` and its accompanying README are copied unchanged from `src/Game/Assets/Music/` at the same pinned upstream revision. The track is **Echoes of Valhalla** by **LUShvalleySound**. The WAV SHA-256 is `3783f4deed1ecd7933a17538a1d880ffb04319f6e11f34dc66f2df7a05ab70b4`. See [the retained upstream music provenance](../assets/audio/README.md) for the owner's supplied-file history and the limits of the recorded licensing information; the music is not asserted to have the source code's GPL license.

## Session work

`src/Game.Shared/SnapshotPayload.cs` and `SessionFile.cs` adapt the original project's proven codec and private credential file behavior. `tests/Game.Godot.Session.Tests/SteamOperationStateTests.cs` adapts its callback-state regression tests. The authority remains the unchanged pinned import. Original lifecycle/RPC scene-reset infrastructure was not copied. New session implementations and tests are GPL-3.0-only.

GodotSteam is copied unchanged from the original project's pinned `src/Game/addons/godotsteam` distribution: **v4.22.1-gde**, SDK **1.65**, upstream archive `godotsteam-4.22.1-gdextension-plugin-4.4.zip`. The retained manifest contains its URL, archive SHA-256 and individual library hashes; licenses/readme are retained under `src/Game.Godot/addons/godotsteam`.

No artwork was created, imported or extracted for the session work. Lobby captures are actual application frames. The original repository was read only.

## Gameplay interface parity

`src/Game.Shared/Development/GameplayCampaignPolicy.cs` adapts the GPL-3.0-only
ordinary-command verification policy in upstream `tools/DevRunner/CampaignStrategy.cs`
at the same `501a3de7c3f5cf8b5e1726e01dbaefad5ea82f35` revision. It is used only to
earn deterministic development/test checkpoints. It is not a second simulation or
player-facing rules engine. New native Godot gameplay components, adapters and tests
are GPL-3.0-only. The original repository, Core, artwork and soundtrack are unchanged.
