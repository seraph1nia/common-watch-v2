# Playing the complete Godot interface

Commands and detached authoritative snapshots still own prices, soldiers, technologies and phase. The explicitly changed combat topology uses six columns per player, up to six players; damage and unit counts are unchanged.

- **Single Player** opens three distinct random doctrine cards. Select one, then press **Start Match**. Offers stay stable through settings, resizing and reconnection; a new match identity creates new offers. **Multiplayer** holds LAN, reconnect and Steam operations, with personal doctrine selection and host-only start after every connected player chooses. **Settings** preserves audio preferences. **Exit Game** asks for confirmation before normal shutdown.
- The upper-right ledger shows the inspected city, Keep, six resource stocks and signed production income, followed by battle Food/funding or the completed upkeep receipt. The preparation frame below Settings groups that city's incoming enemies by type (including bosses), next income, projected fighting army, unfunded field soldiers who sit out, stored soldiers and Food payment. Stages 1–2 use current stock; stage 3 includes final income. Purchases and later production can change the forecast. Selecting a plot yields that frame to building management; **×** restores the overview. It also clears during arena viewing, combat and open details/rewards. Allied inspection is labeled read-only. Settings and Menu sit upper-left; wave/preparation and Ready/Pause sit lower-right. Fixed settlement framing reserves the HUD edge; wheel, drag and reset do not move it.
- The floating dock offers **Build**, **Worldtree**, **Troops & homes** and **City details**. **×** clears a selected plot to expose the map. Select any clearing. Empty owned land offers **Production**, **Military**, **Defense**, **Trade** and **Utility** tabs. Selecting a card spends nothing; confirmation pays the quoted construction cost. Lumbermill's **4 Gold recovery** card also requires confirmation. Locked land shows its purchase quote. Occupied plots provide upgrades, sale, recruitment, trading and relevant management controls.
- **Troops & homes** opens management from navigation at any time. The same shortcut on selected military buildings or Town Halls opens that building’s management section. Its roster and recruitment/hall sections retain squad selection, persistent HP, equipment, XP, storage, return and retirement. **Homes & towers** retains all six army homes, paid home purchases, deployment and tower management. These homes are separate from the nine settlement plots. Squads and towers cannot share a home.
- **Worldtree** retains all 18 technologies, prerequisites, exclusive branches, costs and acquired effects. Select a node, then purchase. Allied, fallen and disconnected cities remain read-only; inspecting another city never changes authenticated command identity.
- **Ready · prep 2 / prep 3 / battle** names the next transition. All connected, standing cities must be Ready before collecting one production payment and advancing research/eligible recovery. The third settlement also pays Food and starts battle. **Unready** reopens your city for editing while waiting. The readiness count excludes disconnected and fallen cities. Inspecting an ally keeps Ready attached to your own labeled city. Food shortages permit readiness; unfunded field members sit out, while stored members never fight. **Pause/Resume** affects the whole match; only the original host controls combat speed. Local dialogs never pause cooperative play.
- **Show arena** previews the forthcoming field army, enemies and structures. Click a combatant or structure to inspect it. During combat a compact live panel leaves the field visible: health, role, effects and target update with playback; **Advanced details** reveals IDs/timing. Gold rings mark the selected squad, orange marks its target; F/R/M badges mean frontline/ranged/magic, P# identifies allies, E→# the enemy destination, and ! flags low squad health. The complete battlefield frames automatically to 80% of the viewport bounds with uniform scaling, reserved figure/effect margins and an optional illustrated forest frame/background.
- Successful nonfinal combat offers three authoritative rewards alongside an expandable factual recap of survivors/losses, Keep damage, sit-outs and earned resources/research. No extra dismissal is needed to choose. City details retains the last recap; missing history after rejoining is labeled unavailable. Dismissing rewards chooses nothing; **Rewards** reopens the same offers. Wave 20 ends in Victory; defeat offers no draft. **Esc / Menu** opens a framed popup over the current view; **F10 / controller Start** toggles it from any screen. **Continue / Esc** closes it. Opening the menu does not leave or pause the match. Reconnect remains beside the match actions. **City details** inspects your doctrine, acquired rewards, wave and recent feedback. The match-action feedback control shows temporary acceptance and persistent pending, rejected or disconnected status; click it for complete recent messages. There is no permanent Run dashboard or history footer.

**Return to main menu** and **Exit Game** both ask for confirmation describing the consequences. Solo progress is not saved. A host leaving ends the match and disconnects everyone; a guest leaving lets the other players continue and can reconnect only while the same host session remains running. Settings are kept. Cancel or Esc dismisses the confirmation and restores focus without leaving. The match continues while a menu or confirmation is open.

Only one details or rewards surface opens at a time. Esc closes nested Settings, then details/rewards/live inspection, restoring native keyboard focus; otherwise it opens the game menu. Modal shades and native panels consume their input. Disabled actions explain their resource or authority restrictions. Command results are visible beside the match actions and in recent feedback. Reconnect retains the private credential/session workflow.

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

The [preparation review](PREPARATION_REVIEW.md) records the completed local
[#3](https://github.com/seraph1nia/common-watch-v2/issues/3) pass in
[#13](https://github.com/seraph1nia/common-watch-v2/issues/13), including observed
solo/cooperative actions, empty stages and waiting. It preserves all three
settlements. Optional human reviews can record actions taken in each preparation,
time spent preparing, time waiting after Ready, and whether a stage felt empty
over the first three waves. Ask players to name the next transition, expected
army/sit-outs and one sensible adjustment before advancing. No human pacing or
balance finding is established by the scripted campaign. A shorter rhythm would
need a separate decision covering purchases between incomes, research, recovery
and final Food payment; one triple-income payment is not an equivalent baseline.

There are no campaign disk saves, new mechanics, replacement world artwork, host migration or
matchmaking. Linux automation and cross-builds do not qualify live Steam accounts,
Windows/macOS runtime, physical GPU or real audio hardware. The previously observed
intermittent native .NET/JIT crash risk remains unresolved.
