# Illustrated Worldtree

`--hud-art illustrated` presents the existing research flow over the supplied static
1672 × 941 shrine scene. All **18 authoritative technologies** remain inspectable.
The pack's documentation incorrectly describes eighteen painted sockets: the image
has **14**, supplemented by four controls using the supplied node rings.

The seven original PNGs are installed in `assets/hud/worldtree/`, byte-identical to
`common_watch_worldtree_ui_pack.zip`. Original delivery notes live in
`art/source/worldtree/`; archive and asset hashes are recorded in
`art/manifests/worldtree-provenance.json`. Exports copy the repository's assets and
resolve them beside the executable. Downloads is never a runtime input.

## Image-space layout and connections

`assets/hud/worldtree/layout.json` stores technology IDs, measured socket centers,
diameters, explicit circular `hitRadius` values, existing HUD icon identifiers and
the details rectangle. Four sockets are marked `supplemental`. The `rings` entries
register each source image's visual aperture by center and radius; transparent PNG
padding does not determine its placement. Registration uses uniform scaling, so
all five states share one aperture center without distorting the artwork.

Background, connector overlays, rings, icons, text and native circular picking
share one scaled canvas transform. The source aspect ratio is retained with
letterboxing. The supplied details frame also retains its original proportions,
with dark backing behind its native text and Research button. The persistent
footer retains its reserved space. Long descriptions scroll within the frame;
Research remains outside the scroll area.

`WorldtreeConnections` draws separate dashed prerequisite arrows using only the
current snapshot's authoritative catalog. Selected nodes highlight incoming and
outgoing edges. Optional `connectorVia` waypoints in the layout route edges around
other sockets; they never define prerequisites. This includes the three production
offshoots. Painted branches remain decoration. The details panel names the actual
prerequisite and exclusive branch and shows authoritative eligibility/denial text.

## Interaction and authority

Clicking or keyboard/controller activation selects and inspects a technology;
inspection spends nothing, including locked and owned technologies. The separate
Research button submits the existing `GameplayAction.Research` intent through
`GameplayContext.Submit`. Names, costs, effects, point balance, eligibility and
exclusive branches come from detached authoritative snapshots. Core and research
rules are unchanged.

Available, locked, hovered, selected and owned rings use the supplied PNGs. Focus
has an independent outline; an owned check remains visible during hover/selection.
Native focus, pointer hover and selected technology can differ. Decorations ignore
mouse input. Foreign-city, pending, disconnected, paused and phase restrictions
continue to use the common context guard. Allied cities remain read-only.

The retained modal blocks world picking/camera input, preserves Esc and controller
cancel, nested Settings and focus restoration. Opening Worldtree does not pause
the match. Plain mode never reads these PNGs or the layout. Missing, corrupt or
invalid optional Worldtree data falls back as a whole to the native text tree,
using the same inspection and authoritative purchase flow.

## Verification and captures

```sh
mise run build
mise run test-core
mise run test-shared
mise run test-session
COMMON_WATCH_GAMEPLAY_ART=illustrated mise run test-gameplay-ui
mise run capture-art
mise run capture-worldtree
```

`capture-worldtree` owns a virtual display and disposable settings. It earns the
`research` checkpoint through legal commands and verifies native circle picking
for every catalog entry, inspection without spending, all canonical connector
edges, independent focus/hover/selection, exclusive-branch denial, keyboard and
controller input, a paid purchase at exactly the authoritative cost, owned
inspection, close/focus restoration and absence of an implicit match pause.

Actual Godot viewport PNGs are written at **1280 × 720** and **1920 × 1080** to
`artifacts/captures/godot/worldtree/`:

- `worldtree-{1280,1920}-empty.png`
- `worldtree-{1280,1920}-hover.png`
- `worldtree-{1280,1920}-locked.png`
- `worldtree-{1280,1920}-available.png`
- `worldtree-{1280,1920}-focus.png` (independent focus, hover and selection)
- `worldtree-{1280,1920}-owned-selected.png` (after the legal purchase)
- `worldtree-{1280,1920}-acquired.png`

The focused checks are extended in `Development/WorldtreeChecks.cs`. Current-run
logs use the `artifacts/worldtree-revision-` prefix. Linux software-rendered checks
do not qualify native Windows execution or live Steam accounts. The known
intermittent native .NET/JIT crash risk remains.

## Current change and results (2026-10-19)

Changed files:

- `assets/hud/worldtree/layout.json`: circular hit radii, ring registration,
  proportional details bounds and connector routes.
- `UI/WorldtreeArt.cs`: validated registration/layout loading and fallback.
- `UI/WorldtreeNode.cs`: registered rings, larger reused icons, circular picking
  and a focus outline independent of selection/research state.
- `UI/WorldtreeConnections.cs`: new catalog-driven connector overlay.
- `UI/ResearchTreePanel.cs`: shared canvas transform and proportional details.
- `Development/WorldtreeChecks.cs`: expanded native verification and captures.
- `docs/WORLDTREE.md`: behavior and verification documentation.

C# paths above are relative to `src/Game.Godot`. The pack PNGs were already
installed; all seven were checked against the supplied ZIP and provenance hashes
and retained unchanged. Core, prerequisites, catalogs, dependency pins and session
transport were not edited.

Passed: `mise run build` (zero warnings/errors), `test-core` (40), `test-shared`
(66), `test-session` (two actual graphical integration tests), illustrated
`test-gameplay-ui` (native panels/input and seven-wave host/guest replay including
allied read-only inspection), `capture-art`, and `capture-worldtree` (fourteen
actual viewport captures at the two resolutions). Additional real Godot runs with
disposable external asset roots verified plain research fallback for invalid ring
registration and a text-corrupt locked-state PNG, including a legal native
purchase. Asset hashes and `git diff --check` passed.

Not run: the full `mise run test` bundle, standalone multiplayer/reconnect/Steam
suites, distribution exports, native Windows execution and live Steam tests.
No claim is made that the existing intermittent native .NET/JIT crash risk is
resolved. No commit or push was made.
