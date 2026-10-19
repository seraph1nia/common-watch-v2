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
