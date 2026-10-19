# HUD logic and implementation notes

## Goal: less simultaneous UI, not fewer game features

The current receiving HUD builds a 200px header, a 368px right column and a bottom
message panel. A selected plot can appear both in that side inspector and inside
the details dialog. Main navigation exposes Army, Homes & towers, Worldtree, Battle,
Run and Rewards simultaneously. The proposal changes information architecture,
not authoritative commands or capability coverage.

The odot-game interface is useful as a semantic reference: its resource ledger,
context-sensitive construction/inspection, green Ready, violet whole-match Pause,
Worldtree and native controls are already separated from authoritative logic.
Do not import its entire UI framework or copy its licensed art/font assets.

## Three persistent regions

1. **Resource ribbon.** Settings, inspected city, Keep, six resources in order
   Gold/Food/Wood/Stone/Metal/Cloth, exact stock and signed production income, wave
   and preparation stage, and the existing arena/settlement switch. Give the signed
   income a visible "+ per production" caption or other unambiguous accessible label.
   Do not call it income per second. Food opens the real funding breakdown. Show the
   viewed city and read-only state when inspecting another player's city.
2. **Context dock.** Build / Army / Worldtree / Run. With no selection the dock
   collapses to navigation and a short selection hint. For an empty owned plot it
   shows the actual categorized construction catalog. A category selector replaces
   a second permanently expanded navigation bar. For locked land it shows the real
   purchase quote. For an occupied plot it shows the relevant supported actions.
3. **Action area.** One obvious next action with an adjacent explanation of its
   consequence. Ready / Unready is separate from Pause / Resume whole match.
   Food/funding summary and battle preview are available here; deep breakdowns
   open only on demand. No always-on five-step stage ladder.

Persistent connection trouble and rejected/pending actions must not disappear to
make the layout cleaner. Use a small durable status indicator/notice and expandable
message history; important errors need more than a short fading toast.

## Context and feature routing

- Empty owned plot: actual Production / Military / Defense / Trade-utility options,
  with names, real costs, production/effects and denial reasons. Selection alone
  must spend nothing. Existing explicit action confirms the intended purchase.
- Locked plot: land quote and explicit purchase. Rings/pad picking remain the same.
- Producer: yield, level, upgrade and actual sale refund.
- Recruiter: recruit route with current trainer, equipment quote and squad choice.
- Market: actual bundle sales. Town Hall: actual storage/recovery tracks.
- Defense: supported branches/upgrades and relevant capacities.
- Army: roster + individual member inspector, recruitment, compatible/new squads,
  reinforcement, member transfer, storage/return, retirement, and six army homes /
  defense towers. These six homes remain distinct from the nine settlement plots.
- Worldtree: the current 18 canonical technologies, costs, prerequisites, exclusive
  branches, acquired state and accepted research points; one inspect/buy workflow.
- Run: doctrine benefits/drawbacks, acquired card stacks/effects and run statistics.
- Arena/battle: actual forecast before battle and actual unit/structure inspection
  during battle; stored/unfunded members remain inspectable without fake field actors.
- Rewards: three real offers from the current draft. Closing/reopening preserves
  those offers and never makes a choice. Use the actual existing reward confirmation.

Army and Worldtree are larger tasks and may use the existing retained modal shell.
At most one top-level details surface is visible. Do not destroy/recreate controls
on every snapshot; retain selection, keyboard focus and scroll position.

## Phase logic is authoritative

During the three preparations, show "Ready" (or "Unready" once accepted) and nearby
text explaining which production will settle. The third ready leads to battle;
there is no extra fourth spending window. Pending acknowledgment is not success.
Do not invent a local countdown or locally advance phase for an animation.

Food forecast is not current stock. The real final-production forecast may include
production not yet received; only the accepted snapshot updates resource stocks.
After payment, show the real receipt, not a forecast relabeled as payment. A food
shortage warns about unfunded soldiers sitting out; it must not independently block
Ready. Let the existing rules decide readiness and affordability.

During combat, hide readiness and show the supported host-controlled speeds
1x / 1.5x / 2x, battle inspection and pause. Do not invent 4x/skip/wave spawning.
Use the actual snapshot phase and playback-completion boundary so rewards cannot
obscure combat before the existing presentation says it is finished.

During reward phase, show the current draft or an explicit way to reopen it.
Victory/defeat use the existing outcome and menu workflow; no invented replay/next-
wave actions. Keep existing terminal-state semantics.

## Cooperative safety and user feedback

- City selection changes inspection, never authenticated command identity.
- Ready while inspecting an ally still acts on YOUR city and is labeled accordingly.
- Allied, eliminated or disconnected city editing remains restricted as before.
- Any currently authorized member may pause/resume according to the existing rules;
  speed controls remain host-restricted. Do not conflate these permission models.
- Opening a local details panel/settings NEVER pauses cooperative play.
- Retain pending, rejected, accepted and disconnected feedback and reconnect handling.
- Never optimistically show paid resources, recruited units or acquired technology
  before authoritative acknowledgment.
- No worker/population or extra currency is introduced from the reference HUD.

## Implementation touchpoints

Inspect the current versions of:

- src/Game.Godot/UI/SettlementHud.cs — composition and context routing.
- ResourcePanel.cs — display accepted stocks, income, forecast/receipt.
- MatchControls.cs — existing Ready/pause/speed identities and guards.
- BuildingInspector.cs / ConstructionPanel.cs — reuse quote/command logic.
- GameplayDialog.cs — single retained modal; remove duplicated visible inspectors.
- ArmyRosterPanel.cs / DeploymentPanel.cs / ResearchTreePanel.cs /
  BattleInspectionPanel.cs / RunProgressionPanel.cs / DraftPanel.cs — keep features.
- HudSkin.cs / WatchTheme.tres — illustrated theme adapter and plain fallback.

The pack's PNG resources use native res://UI/ForestHud paths. Preserve strict asset
validation and optional loading; don't blindly merge new IDs into VisualAssets'
existing known-ID list or asset catalog. The new theme must not be hidden by the
current parchment Label theme and manually drawn hover-only button backgrounds.

Use ordinary native labels, buttons, dropdowns, scroll containers and modal input.
Keep ornaments separate and pointer-inert. Only actual interactive UI rectangles
should intercept the map. Scrolling a HUD panel must not zoom the world; dragging a
slider must not pan it. A modal must block world selection; closing it restores a
valid native focus target, respecting nested settings/confirmations.

## Verification and definition of done

Inspect current mise tasks, then run the affected build, art/session and gameplay UI
checks. In the reviewed repository these include build, test-core, test-shared,
test-art, test-session, test-gameplay-ui, capture-gameplay-ui and capture-settlement.
Run relevant multiplayer/reconnect tests when their UI routes are affected.
Do not weaken existing tests simply because labels/layout change; update layout
assertions while retaining authority, quote, focus and input behavior assertions.

Capture actual empty/default, construction, locked-land, occupied-building, Army,
Worldtree, forecast, combat, reward, allied read-only, pending/rejected and
disconnected states. Cover 1280×720 and 1920×1080, long labels, large numbers, hover,
pressed, disabled and independent keyboard-focus states. Test all nine pad clicks
with pan/zoom and both HUD modes. Unrun checks must be called out explicitly.

The preview sample values, illustrated selection ring and map framing are not a
slot-layout source or fixture for tests. Use existing map data and legal scenarios.
Do not change Game.Core, gameplay balances, match protocols, fonts or dependency
locks. Do not replace the environment or make a new interface framework.
