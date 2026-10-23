# Forest presentation and approved asset paths — issue #11

The forest settlement, existing building paintings, menu backdrop and painted
bark/mushroom/woodland HUD remain intact. This pass completes two explicit
battlefield mappings, makes building levels visible, and gives secondary support
controls a quieter native material. It uses only existing approved paintings and permitted deterministic cropping;
it downloads or repaints no game artwork. All changes are local presentation derived from accepted snapshots.

## Approved asset inventory and remaining dependency

The original installed catalog had 46 entries. The detailed forest pack has ten
building and five character cutouts, with no dedicated enemy Berserker. Inspection
of the broader approved source sheets found a distinct raised-arm orc pose on
`04-expanded-art-and-hud-sheet.png`, beside the existing sword-and-shield pose.
The existing deterministic Pillow pipeline now crops that supplied pose as the
47th entry. No source painting is modified, recolored or extended. All previous
46 catalog records and PNG hashes reproduce unchanged.

| Authoritative object | Verified installed path | Presentation decision |
| --- | --- | --- |
| Enemy Berserker | `assets/sprites/units/enemy_berserker.png` | Use the existing raised-arm orc pose; the previous enemy Swordsman keeps its separate sword-and-shield crop. Enemy rings, E→P# badge and inspection retain faction/ownership/type. |
| Watchpost | `assets/sprites/arena/defense_tower.png` | Reuse the supplied defense-tower cutout at 82 world pixels; ArrowTower retains 96, and the Watchpost label/health/inspection retain its identity. |
| Keep / ArrowTower / CatapultTower | Existing `arena/keep`, `arena/defense_tower`, `building/catapulttower` | Explicit mapping for all four structure kinds; one shared path serves preview and live drawing. |

The new enemy crop uses source rectangle `[1040,330,1175,510]` from the unchanged
1536×1024 RGBA sheet, SHA-256
`66107cc4648506e0228e170cbf677303efb7076e691c4da6118410dcb5983cd5`.
The alpha floor/ceiling, trimming and 256×256 canvas use the existing pinned
processing rules. The measured content bounds are `[13,5,122,173]`; the foundation
point is `[74,163]` in crop coordinates, between the planted feet. The installed
PNG SHA-256 is
`3b081b67899d53fdb6e98c026310222cd7ac07d7a27ad91204363bcb70a9e7dd`.
Its source/crop/anchor and output hash are recorded in the extraction manifest
and generated catalog. The contact sheet adds the new entry.

The Watchpost's defense tower source is
`art/source/modular_battle_arena/assets/structures/defense_tower.png`, retained
byte-for-byte with SHA-256
`6c580bd448a9a3d7df8f746fef25d9852c4483b76bfb6df0760ba96d9577ac0c`.
Its catalog, anchor, size and source bytes remain unchanged.

**Dependency resolution:** both named gaps are supplied by existing approved
artwork. No external art dependency is required for these presentation paths.
The detailed pack still has no separate purple forest enemy Berserker variant;
the deliberate current selection is the supplied orc pose, not a newly invented
variant. No new tier paintings are claimed. Explicit replacement catalog paths
remain supported, and missing/corrupt files retain geometry. Debug never probes
or decodes artwork.

`VisualAssets.UnitId` retains logical IDs; the enemy now has its matching catalog
entry, used by HUD portraits, preview and live figures. `AssetLoader` owns cached
textures. `VisualAssets.StructureId` supplies preview/live `WorldArt`. Neither
path changes logical cells, occupancy, damage, targeting, figure timing, picking
or command identity.

## Materials, hierarchy and persistent progression

Native card headings share 22-pixel typography, body text remains 16 and compact
technical details use 14. Doctrine/reward card copy shares 14-pixel inner spacing
inside the retained painted frame. Secondary disclosures, inspector/dialog close
controls, roster rows and ordinary session links use a dark forest surface with
a restrained border. Hover, gold selected/pressed state and the existing cyan
native focus ring stay distinct. Roster selection is retained explicitly when
scrolling or refreshing. Disabled actions retain readable muted text and reasons.
The green primary Ready/choose surfaces, violet pause and original painted frames
remain prominent. These are native Controls, without new raster/vector art.

A healthy connection hides the reconnect utility; it reappears when disconnected.
Recent feedback remains accessible and retains pending/rejected/disconnected text,
full tooltip/history and existing transient acceptance. Scene navigation and
feedback use quiet surfaces rather than competing painted bars beside Ready.
Settings/Menu, the ledger, critical Keep/Food/phase information and the painted
primary action remain in their established regions.

Each occupied settlement plot has a small `L#` annotation at its foundation:
neutral L1, gold for upgraded levels. Its 34×20 screen-pixel size stays stable as
fixed camera framing resizes. Labels finish after all tall sprites, so foreground
buildings cannot hide a rear building's level. Both map and debug-grid paths use
the accepted `SlotState.Level`; empty/unowned plots receive no badge. Building
paintings, anchors, sizes, canonical clearing ellipses and hit testing are
unchanged. These are technical font/shape annotations, without replacement tier
art or invented stats. Dense foreground buildings can still overlap as before;
the labels do not claim that each tier has a separate silhouette.

## Reproducible native review

```sh
mise run capture-forest
mise run capture-gameplay-ui
mise run capture-settlement
mise run test-session
mise run capture-art
mise run test-animation
mise run capture-battle
```

`capture-forest` runs the ordinary application in sprites/illustrated/angled and
explicit debug/plain/topdown at 1280×720 and 1920×1080. Legal production payments
fund a native pointer Farm upgrade; the same building changes from L1 to L2 with
exact quoted payment. A paid late-game checkpoint shows L5. A legal wave-two
preview and battle verify the Berserker texture on the retained figure and the
Watchpost path. Two extra runs use missing files and textual corrupt PNG fixtures
with real catalog metadata, verifying native rejection and usable geometry.
The check owns a virtual display, uses private temporary settings and bounds the
process lifetime. Captures are actual full-screen PNGs in
`artifacts/captures/forest-consistency/`, not component mockups.

The general UI pass also captures doctrine offers/selection, rewards/descriptions/
effects, army selection/scrolling, deployment, veteran comparisons/retirement,
inspectors, Worldtree, modal close/focus and ordinary HUD in both presentations at
both sizes. Five-size settlement checks retain all nine native clearing picks.
The dedicated and general screenshots are visually reviewed for scene hierarchy,
text fit, source art retention and focus/selection meaning. Local tests and actual
screenshots are the accepted evidence; they do not establish human comprehension,
physical input/audio behavior, live Steam or other-OS runtime qualification. The
known intermittent native .NET/JIT crash risk remains applicable.

## Verification receipt

The final registered-asset build has zero warnings/errors and passes 46 Core,
137 Shared and 19 Pillow tests. All 46 previous catalog records/PNG hashes were
compared with the starting commit and remain identical. The new source crop
processes/validates/installs reproducibly as entry 47. The final focused review
passes all six native cases: both target sizes in sprites/debug, plus missing
and corrupt textures. Three startup/session graphical facts pass again after
installation, including qualified installed art and empty/corrupt fallbacks.

The material/geometry pass also passed four ordinary/debug full UI runs and the
seven-wave cooperative worker, five-size nine-clearing input checks, reconnect,
12 animation contracts, native animation phases and ordered battle frames.
Final art/showroom and ordered battle captures are refreshed with the installed
47-entry catalog. Logs use `artifacts/test-logs/roadmap-11-*`; the registered-asset
repeats use `*-installed.log`. All screenshots are actual application output.
No live Steam, other-OS runtime, physical hardware or human visual-comprehension
qualification is claimed. The known native .NET/JIT risk remains applicable.
