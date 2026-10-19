# Forest HUD integration

The user-supplied forest HUD replaces the illustrated parchment skin through
`HudSkin`. Debug/plain remains the default. The supplied 78 transparent PNGs,
`ForestTheme.tres`, SVG originals and delivery metadata are preserved; preview
mockups are not runtime assets. See [provenance](PROVENANCE.md#forest-hud-delivery).
No Core, shared simulation/adapter, world asset catalog, map coordinates, building
anchors, soundtrack, dependency pins or locks were changed.

## Interface

- The ribbon contains the inspected city, Keep, six accepted resource stocks and
  signed income per production, wave/preparation, Settings and arena switch. At
  720p resources move below navigation; larger values can reflow into three columns.
- The collapsible context dock contains Build, Army, Worldtree and Run. Empty land
  offers categorized cards with real quotes/effects/denials. Card selection spends
  nothing; Build selected submits the existing authoritative construction intent.
  The existing explicit Gold-recovery action remains available. Locked land uses
  the actual land quote; occupied land offers the supported management actions.
- One retained building inspector moves between the dock and details dialog.
  Recruitment, squads, Town Hall management and the six homes/towers are grouped
  under Army. Worldtree retains all canonical technologies; Run retains doctrine,
  cards and statistics and also offers camera reset, menu, reconnect and history.
- Bottom-right controls separate Ready/Unready and its production consequence from
  whole-match Pause/Resume. Upkeep details distinguish forecast from payment;
  shortage does not block Ready. Combat/playback completion uses battle controls
  and host speed permissions. Rewards remain the three real authoritative offers.
- Only one details/rewards surface opens at a time. Closing/reopening rewards makes
  no choice. The feedback notice stays above those surfaces, with 24 recent notices
  available in History. City inspection does not change authenticated identity;
  allied editing remains blocked. Local dialogs submit no pause commands.

Native style boxes provide nine-slice frames and all button states, with an
independent focus ring. The old parchment Label colors, separate button backgrounds
and hover tint handlers are removed. Fixed-size ornaments and the full-screen HUD
root ignore pointer input; actual panels/controls and modal shades consume it.
Native controls retain identity, selection and scroll state; card scrolling follows
keyboard focus. Main controls have a 44px minimum height and body text stays 16px. Context refresh avoids
hiding controls that remain active, preserving keyboard focus. Nested Settings/Esc
and dialog focus restoration are exercised by native input checks.

Camera framing uses actual ribbon/dock/action rectangles. The minimum camera zoom
is 0.25 so the unchanged nine-pad map fits the smaller safe region at 720p. Manual
pan, wheel zoom, reset and canvas inversion remain native Camera2D behavior. CLI
presentation options and geometric/plain fallback remain available. Native PNG
imports run automatically through the bounded prepare step when needed; a missing
forest theme dependency falls back to the plain theme.

## Actual captures

All frames come from the application and legal scenario checkpoints. The sample
values in the pack are not gameplay fixtures. Captures are ignored local artifacts.

| State | 1280 × 720 | 1920 × 1080 |
| --- | --- | --- |
| Idle | [PNG](../artifacts/captures/gameplay/forest/hud-idle.png) | [PNG](../artifacts/captures/gameplay/forest/1920/hud-idle.png) |
| Build | [PNG](../artifacts/captures/gameplay/forest/hud-build.png) | [PNG](../artifacts/captures/gameplay/forest/1920/hud-build.png) |
| Selected card, keyboard focus and another hover | [PNG](../artifacts/captures/gameplay/forest/hud-build-focus-hover.png) | [PNG](../artifacts/captures/gameplay/forest/1920/hud-build-focus-hover.png) |
| Locked land | [PNG](../artifacts/captures/gameplay/forest/hud-locked.png) | [PNG](../artifacts/captures/gameplay/forest/1920/hud-locked.png) |
| Occupied | [PNG](../artifacts/captures/gameplay/forest/hud-occupied.png) | [PNG](../artifacts/captures/gameplay/forest/1920/hud-occupied.png) |
| Army | [PNG](../artifacts/captures/gameplay/forest/army-roster.png) | [PNG](../artifacts/captures/gameplay/forest/1920/army-roster.png) |
| Worldtree | [PNG](../artifacts/captures/gameplay/forest/worldtree.png) | [PNG](../artifacts/captures/gameplay/forest/1920/worldtree.png) |
| Combat | [PNG](../artifacts/captures/gameplay/forest/hud-combat.png) | [PNG](../artifacts/captures/gameplay/forest/1920/hud-combat.png) |
| Rewards | [PNG](../artifacts/captures/gameplay/forest/reward-draft.png) | [PNG](../artifacts/captures/gameplay/forest/1920/reward-draft.png) |
| Pending acknowledgment | [PNG](../artifacts/captures/gameplay/network-forest/pending-1280.png) | [PNG](../artifacts/captures/gameplay/network-forest/pending-1920.png) |
| Rejected command | [PNG](../artifacts/captures/gameplay/network-forest/rejected-1280.png) | [PNG](../artifacts/captures/gameplay/network-forest/rejected-1920.png) |
| Disconnected | [PNG](../artifacts/captures/gameplay/network-forest/disconnected-1280.png) | [PNG](../artifacts/captures/gameplay/network-forest/disconnected-1920.png) |

The same folders also contain actual Market, Town Hall, six-home deployment,
shortage/forecast, unit, tower, Run, victory and defeat views. Allied read-only
Worldtree inspection is captured by the real host/guest worker in
[its multiplayer frame](../artifacts/captures/gameplay/multiplayer-city-inspection.png).
Network pending captures briefly suspend the actual host process, then resume it
and verify acknowledgment; they do not fabricate session state.

Reproduce forest captures with:

```sh
COMMON_WATCH_GAMEPLAY_ART=illustrated mise run capture-gameplay-ui
mise exec -- bash tools/graphical.sh capture-settlement --hud-art illustrated
```

Validation results and platform limits are recorded in
[verification](VERIFICATION.md#forest-hud-integration-2026-10-19).

## Continuity audit and minimal refinements (2026-10-19)

The supplied `CommonWatch_V2_Forest_Continuity_GapPack.zip` was checked for
absolute/traversal paths and symlinks before temporary extraction; every supplied
checksum passed. Archive SHA-256:
`ffb245be312dafe1061bc1899120163f02d13db3c516940e8e6e4a5082cc5f7d`.
The [reference contact sheet](../artifacts/forest-continuity/reference-contact-sheet.png)
uses the actual ten approved source buildings, five approved source units and
installed settlement painting. The [candidate contact sheet](../artifacts/forest-continuity/candidate-contact-sheet.png)
compares the four incumbent buildings with the supplied alternatives.

The following decisions were reported before code changes:

| Unchanged reference | Gap / candidate | Decision | Risk / evidence |
| --- | --- | --- | --- |
| Settlement PNG/JSON, ten detailed buildings, five detailed units, Worldtree, ForestHud | Approved visual family | Preserve | Verify original bytes; retain geometry, anchors and theme. |
| Farm, Lumbermill, Barracks | Four-pack alternatives | Reject replacements | Older medieval incumbents remain mismatched, but the candidates' darker, finer rendering and dense ground islands do not clearly fit the bright faceted reference better. |
| Prototype Arcanum | Four-pack Arcanum | Reject replacement | The alternative loses the incumbent's readable cyan crystal silhouette at small game scale. The incumbent's angular ground remnants remain a known limitation. |
| Swordsman, Enemy Swordsman, missing Enemy Berserker | No matching sprites supplied | Defer | Separate approved unit paintings required; keep existing sprites and geometric fallback. |
| Existing Army presentation and world effects | Four class symbols, shadow and glints | Defer | No demonstrated benefit sufficient to add assets or effects. |
| Existing ribbon, context dock and action area | Local sizing, selection, cost and emphasis fixes | Accept | Retain native input, focus, scrolling, information and authoritative commands. |

Review-only candidates were processed into an ignored, separate asset root with
foundation pivots behind the foreground steps/garden. Real Godot captures compare
`settlement` (Farm, Barracks, Lumbermill) and `sprite-settlement` (Lumbermill,
Barracks, Stonecutter, Arcanum) at **1280×720 and 1920×1080**, using identical
legal states, original map layout, display sizes and production canvas anchors.
See `artifacts/forest-continuity/{incumbent,candidate}-{checkpoint}-{width}.png`
and `candidate-review-pivots.json`. No candidate was installed or registered in
the production manifest; `art-process`/`art-validate`/`art-install` were therefore
not needed. The existing pipeline and all production artwork remain unchanged.

Targeted native-control changes:

- Resource stocks use 18px type; signed per-production values stay at 16px.
  Reflow measures whole chips, including icons, Food's 44px details button and
  theme spacing, rather than estimating widths from labels alone.
- The selected plot heading uses 18px type. A selected construction card also
  carries a check mark; its existing primary confirmation names the building and
  real quoted cost. Its existing choice column grows from 180px to 220px to keep
  the Lumbermill confirmation inside the 720p dock. Selecting still spends nothing.
- Compact inspector text spacing is 4px, with up to two summary lines and full details
  in the existing tooltip/details inspector. Action rows keep their natural height,
  leaving More details and Manage in Army accessible in the same dock.
- Ready retains the existing primary forest variation; Unready and Pause/Resume
  use the existing neutral variation. Consequences, funding, permissions, rewards
  and persistent session feedback are unchanged. The city selector explicitly
  labels an inspected ally `Ally · read-only`.

Before captures are retained under `artifacts/forest-continuity/before/`;
the canonical capture folders above contain the final frames. The local
`.lavish/forest-continuity/index.html` review compares real before/after frames,
candidate trials and the approved reference sheet, with native-pixel viewing.
No performance changes were made; no profiling claim is attached to this work.

Final executed checks: `build` (zero warnings/errors), `test-core` (40 passed),
`test-shared` (66 passed), `test-art` (19 passed), `test-session` (two actual-process
tests passed), illustrated `test-gameplay-ui` and `capture-gameplay-ui` (including
the seven-wave host/guest worker), illustrated `capture-settlement`,
`capture-worldtree` and `capture-art` (14 frames). Transcripts are under
`artifacts/forest-continuity/<task>.log`. Native assertions additionally verify the
selected construction quote and confirmation bounds, retained card scroll after
refresh, and the complete compact Arcanum Army button at both capture sizes.
Settlement verification retains all nine pads, native pan/zoom, both projections,
zoom 0.55/1/2.2, modal mouse capture and focus restoration. Actual 720p/1080p frames
were inspected for empty/selected pads, all four incumbent/candidate buildings,
Army, Worldtree, combat, rewards and pending/rejected/disconnected feedback.
All 410 recorded production-art, source-art, manifest, ForestHud, Core and Shared
files retain their original hashes (`preservation-receipt.json`). No native crash
occurred in the completed runs; the known intermittent native .NET/JIT crash risk
remains. These are Linux/Xvfb/Mesa checks; live Steam, native Windows/macOS and
physical GPU/audio hardware were not tested.
