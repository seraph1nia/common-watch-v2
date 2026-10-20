# Playing the complete Godot interface

Commands and detached authoritative snapshots still own prices, soldiers, technologies and phase. The explicitly changed combat topology uses six columns per player, up to six players; damage and unit counts are unchanged.

- **Single Player** opens three distinct random doctrine cards. Select one, then press **Start Match**. Offers stay stable through settings, resizing and reconnection; a new match identity creates new offers. **Multiplayer** holds LAN, reconnect and Steam operations, with personal doctrine selection and host-only start after every connected player chooses. **Settings** preserves audio preferences. **Exit Game** follows normal shutdown.
- The ribbon shows the inspected city, Keep, six resource stocks, wave/preparation, Settings and the arena switch. Resource tooltips show production income. The illustrated settlement fills the viewport proportionally; wheel, drag and reset do not move it.
- **Build** and **Worldtree** are the floating construction panel's navigation. Select any clearing. Empty owned land offers **Production**, **Military**, **Defense**, **Trade** and **Utility** tabs. Selecting a card spends nothing; confirmation pays the quoted construction cost. Lumbermill's **4 Gold recovery** card also requires confirmation. Locked land shows its purchase quote. Occupied plots provide upgrades, sale, recruitment, trading and relevant management controls.
- **Manage troops & homes** on selected military buildings, Town Halls or plots with a surviving army opens contextual management. Its roster and recruitment/hall sections retain squad selection, persistent HP, equipment, XP, storage, return and retirement. **Homes & towers** retains all six army homes, paid home purchases, deployment and tower management. These homes are separate from the nine settlement plots. Squads and towers cannot share a home.
- **Worldtree** retains all 18 technologies, prerequisites, exclusive branches, costs and acquired effects. Select a node, then purchase. Allied, fallen and disconnected cities remain read-only; inspecting another city never changes authenticated command identity.
- **Ready/Unready** settles each of three preparations. Food shortages permit readiness; unfunded field members sit out. **Pause/Resume** affects the whole match; only the original host controls combat speed. Local dialogs never pause cooperative play.
- **Show arena** previews the forthcoming field army, enemies and structures. Click a combatant or structure to inspect it; active combat supports the same picking. The complete battlefield frames automatically to 80% of the viewport bounds with uniform scaling, reserved figure/effect margins and an optional illustrated forest frame/background.
- Successful nonfinal combat offers three authoritative rewards. Dismissing rewards chooses nothing; **Rewards** reopens the same offers. Wave 20 ends in Victory; defeat offers no draft. Menu/reconnect controls live beside the match actions. There is no Army navigation button, Run/forecast/selected-plot/upkeep tab, collapse control or history/feedback footer.

Only one details or rewards surface opens at a time. Esc closes nested Settings, then details/rewards, restoring native keyboard focus; otherwise it opens Settings. Modal shades and native panels consume their input. Disabled actions explain their resource or authority restrictions. Command results remain internal for debugging and automated checks. Reconnect retains the private credential/session workflow.

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
