# Playing the complete Godot interface

The simulation and prices are the unchanged pinned Core. All controls submit
commands; the accepted snapshot supplies stocks, soldiers, technologies and phase.

- The compact ribbon selects the inspected city and shows Keep, six accepted
  resource stocks with signed income per production, wave/preparation, Settings
  and the arena/settlement switch. At 720p its resource row moves below navigation.
- The bottom context dock has **Build**, **Army**, **Worldtree** and **Run**, plus
  Collapse/Expand. Select one of the nine clearings to see its context. Empty
  owned land offers categorized build cards: selecting a card spends nothing;
  **Build selected** submits the actual construction. Locked land shows its current
  purchase quote. Occupied plots offer upgrades, refunds and relevant management;
  **More details** expands the same retained inspector. Recruitment and Town Hall
  management also route directly into Army. Scrolling keeps longer controls usable.
- The bottom-right action area separates Ready/Unready, its production consequence,
  upkeep forecast/details, battle inspection and whole-match Pause/Resume. Ready
  settles one production at each of three preparations. The final forecast includes
  production 3 without adding it to stock early. Food shortages permit readiness:
  unfunded field members sit out. During and after combat, the table shows the
  actual upkeep receipt. Food's **?** button also opens the breakdown.
- **Army** has a persistent roster, squad selector, and separate Recruitment and Town Halls
  sections. Choose a member to inspect HP, equipment, service XP, home/storage,
  funding and recovery eligibility. The same inspector handles transfer, storage,
  return and retirement. Recruitment chooses a trainer and an automatic, new or
  specific compatible squad; each click pays for one soldier.
- Army's **Homes & towers** section shows the six stable numbered army homes. These are separate
  from the nine settlement plots. Select a squad and deploy it to a purchased,
  eligible home. Inspect a home to buy land or construct/upgrade/sell a tower.
  Squads and towers cannot share a home.
- **Worldtree** shows all 18 canonical technologies, their prerequisites, exclusive
  branches, costs, acquired state and effects. Select a node to inspect/purchase.
  The panel stays open and synchronized after a purchase.
- **Show arena** presents the forthcoming field army, funded participants, next-wave
  enemies and structures as a read-only diagram. Click a figure/structure or use
  **Battle** lists to open the same inspector. Combat figures also support picking.
  Forecast markers never become combat actors. Stored/unfunded soldiers remain
  inspectable in lists without acquiring field bodies.
- **Run** shows doctrine benefits/drawback, acquired card stacks/effects, research,
  Keep and reward statistics, plus camera reset, return to menu, reconnect and the
  recent session feedback history. The pregame doctrine controls explain all six choices.
  A successful nonfinal battle offers three actual cards with rarity, effect and
  stack limit. Close rewards to inspect your city/run before deciding; **Rewards** reopens the same offers without choosing. Independent player choices remain authoritative. Wave 20 goes to
  Victory; defeat offers no draft.
- Ready/Unready affects your city, including while inspecting an ally. Pause/Resume
  affects the whole match. Only the original playing host controls 1x/1.5x/2x speed.
  Opening a local details panel never pauses cooperative play.
- The city selector inspects allies' economy, buildings, roster and research. Allied,
  fallen and disconnected cities remain read-only. The inspected player never
  replaces authenticated command identity.

Only one details or rewards surface opens at a time. Esc closes nested Settings,
then details/rewards, restoring native keyboard focus; otherwise it opens Settings. Modal shades block world input;
scrolling native panels does not zoom the board. Selection alone spends nothing.
Disabled actions explain missing resources, producers or authoritative restrictions.
The persistent footer distinguishes acceptance, rejection, pending acknowledgment
and lost connection, including while details/rewards are open. **History** opens
recent feedback in Run. Reconnect preserves the existing private credential/session workflow.

## Development and verification

```sh
mise run build
mise run test-core
mise run test-shared
mise run test-gameplay-ui
mise run capture-gameplay-ui
mise run test
mise run play
mise run test-multiplayer
mise run test-reconnect
mise run build-linux
mise run build-windows
```

`test-gameplay-ui` runs actual native Controls, world picking, modal/focus/scroll
checks, and a separate host/guest game through seven paid cooperative waves. The
new integration fact also belongs to `mise run test` and the existing CI process
suite. `capture-gameplay-ui` captures native panels at 1280×720 and 1920×1080, plus
an actual remote client's read-only allied tree. PNGs are under
`artifacts/captures/gameplay/` (large views under `1920/`).

Legal default-rule checkpoints are `economy`, `army`, `town-hall`, `research`,
`forecast`, `reward`, `late`, `research-full`, `research-open`, `victory`, `defeat`.
They replay paid commands and bounded combat steps; no state is injected. The
forecast checkpoint includes partial food funding and a purchased tower. For example:

```sh
mise run play -- --checkpoint army
mise run scenario victory artifacts/scenarios/victory.json
```

There are no campaign disk saves, new mechanics, new artwork, host migration or
matchmaking. Linux automation and cross-builds do not qualify live Steam accounts,
Windows/macOS runtime, physical GPU or real audio hardware. The previously observed
intermittent native .NET/JIT crash risk remains unresolved.
