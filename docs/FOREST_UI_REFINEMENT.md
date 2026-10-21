# Forest UI refinement — 21 October 2026

The existing enchanted-forest interface now exposes production and important session feedback, gives construction cards a readable hierarchy, and makes troop/home and run inspection discoverable. The settlement painting, building sprites, shrine, original theme files, six-player topology, fixed settlement camera and authoritative gameplay remain intact.

Local HEAD and remote `origin` HEAD were checked before editing; both were `ada103a48956698782da6a8225d1d5b33d0ec760`. Existing environment/settings changes were already present in the working tree and were preserved. This pass does not claim those changes as UI-refinement work.

## File-by-file changes

Paths in this table are relative to `src/Game.Godot/UI/` unless stated otherwise.

| File | Change and purpose |
| --- | --- |
| `ResourcePanel.cs` | Shows 18px stock and signed 14px income from the detached production projection, together with each existing resource icon. |
| `SettlementHud.cs` | Reflows the entire resource row when it cannot fit; sizes the context dock to content; retains only navigation when unselected; adds clear selection, direct Troops & homes and City details; presents persistent feedback status and temporary acceptance, with complete messages in an eight-notice history. Caches layout inputs and only rebuilds the notice history when feedback changes. |
| `ConstructionCard.cs` | Small native Button helper with separate name, purpose, cost and restriction labels. Content determines minimum height; decoration ignores input. Selection, pointer hover and keyboard focus remain independent. |
| `ConstructionPanel.cs` | Replaces rigid four-column behavior with measured 1–4 column reflow (three at 720p, four at 1080p). Keeps all five categories, exact quoted costs, disabled reasons and explicit Build selected confirmation. Gold recovery shares the confirmation row to fit at 720p. |
| `BuildingInspector.cs` | Places plot information before actions in a vertical native layout; removes ordinary Upgrade controls when no ordinary quote exists and redundant troop links on unrelated buildings. Recruitment, Town Hall and tower branch controls remain contextual. |
| `MatchControls.cs` | Gives Ready the expanding primary button, explains production 1/3 through 3/3 and actual unfunded soldiers, keeps Pause secondary, and enlarges terminal results. Hides Pause after the match ends. |
| `GameplayDialog.cs` | Reuses the existing modal for City details: doctrine, acquired rewards, wave/phase and recent feedback. Allocates a bounded unit-inspection column so Army management tabs have enough space. The same shade and focus restoration protect every page. |
| `ArmyRosterPanel.cs` | Shortens the summary and exposes Roster, Recruit, Town Halls and all six homes/towers as visible tabs. Retains the existing Controls and selections across refreshes. |
| `LobbyPanel.cs` | Emphasizes Single Player and explains doctrine selection. Reuses the same native menu content as a framed popup with Continue; no background is attached to popup instances. |
| `DoctrineCard.cs` | Uses readable 16px descriptions at 720p, reduces blank spacing, bounds card height and provides native scrolling for longer text while retaining the starting allocation and selection button. |
| `HudSkin.cs` | Improves disabled-button text contrast on the runtime theme copy. Supplied theme and PNG files are unchanged. |
| `ResearchTreePanel.cs` | Slightly widens/repositions the native text aperture inside the existing Worldtree frame. The shrine, frame proportions, sockets, prerequisites and purchase flow remain unchanged. |
| `SettingsPanel.cs` | Defers Esc to the covering menu popup so closing the menu preserves the underlying Settings screen. Existing environment/settings edits are retained. |
| `RewardCard.cs` | Gives titles and rarity distinct typography, aligns the lower actions and uses existing primary button styling. |
| `../Development/GameplayChecks.cs` | Verifies visible authoritative income, card content bounds, City details routing, quote confirmation, retained roster scroll/selection and clear selection in the real application. |
| `../Development/SettlementMapChecks.cs` | Uses the actual clear-selection Control to expose each of the nine pads before native picking at multiple viewport sizes. |
| `../Application/SessionScreens.cs` | Opens/closes a transparent modal menu without replacing the current session, restores focus, and retains the existing explicit solo/host/join/exit routes. Popup and startup LAN endpoints stay synchronized for reconnect. |
| `../Application/GameApplication.cs` | Routes the HUD menu to the popup; stops world picking/camera input underneath it; closes it on session replacement. Simulation timing and pause remain authoritative. |
| `../Application/InputActions.cs` | Adds F10 and controller Start to toggle the popup from any screen. Esc closes it before underlying modals. |
| `../Application/SteamScreens.cs` | Moves the existing Steam Controls into the active menu instance; no new Steam operation, callback or identity behavior. |
| `../Development/MenuChecks.cs` | Exercises the menu popup over doctrine selection without replacing offers or authority. |
| `../Development/ControlProbe.cs` | Exposes actual feedback-Control visibility/text to the existing development-only network worker. |
| `tools/gameplay-network.py` | Requires pending, rejected and disconnected feedback to be visible in the native Control before capturing it. No command or network policy changes. |
| `README.md`, `docs/FOREST_HUD.md`, `docs/gameplay/PLAYING.md` | Documents the actual navigation, income, confirmation and feedback behavior. |

All construction, recruitment, upgrades, selling, trading, research, readiness, pause and army operations continue to submit the existing authoritative commands. Card selection and inspection never pay a quote. The Menu button, F10 or controller Start opens only the centered menu square over the current settlement, Army, Worldtree, combat, rewards, doctrine or Settings screen. There is no replacement background or dimming layer. Continue, Esc, F10 or Start returns to the underlying view; the live match continues unless separately paused. The startup menu retains its approved backdrop when there is no underlying screen. Combat speed permissions, field overview, projection flags, defaults, reconnection and simulation timing are unchanged.

## Real before and after captures

[Interactive screenshot review](../artifacts/forest-ui-refinement/index.html) provides resolution/presentation selectors, paired viewport captures and scrollable native-pixel inspection. Its colors follow the existing ForestTheme. These are actual Godot frames earned through legal scenarios, not mockups. Doctrine offers are intentionally personal and can differ between launches.

| View | Before 720p | After 720p | Before 1080p | After 1080p |
| --- | --- | --- | --- | --- |
| Construction / focus / hover | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/hud-build-focus-hover.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/hud-build-focus-hover.png) | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/1920/hud-build-focus-hover.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/1920/hud-build-focus-hover.png) |
| Idle settlement / ribbon | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/hud-idle.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/hud-idle.png) | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/1920/hud-idle.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/1920/hud-idle.png) |
| Gold recovery confirmation | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/build-gold-recovery-selected.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/build-gold-recovery-selected.png) | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/1920/build-gold-recovery-selected.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/1920/build-gold-recovery-selected.png) |
| Army / unit inspection | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/army-roster.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/army-roster.png) | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/1920/army-roster.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/1920/army-roster.png) |
| Rewards | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/reward-draft.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/reward-draft.png) | [PNG](../artifacts/forest-ui-refinement/before/gameplay/forest/1920/reward-draft.png) | [PNG](../artifacts/forest-ui-refinement/after/gameplay/forest/1920/reward-draft.png) |

The after folders also contain `menu-over-{settlement,army,worldtree,combat,rewards,doctrine}.png` and `menu-over-settings-{1280,1920}.png`, including 1080p equivalents. Pixel assertions compare four sample points outside the square before and after opening it; the previous screen, selection and session identity remain intact.

The same before/after folders retain main menu, doctrine selection, all five construction categories, locked land, occupied buildings, Market, Town Hall, six-home deployment, Worldtree, shortages, combat, victory and defeat. `network-forest/` and `network-plain/` retain real pending/rejected/disconnected states at both sizes. Pending is captured by briefly suspending the actual host, then resuming and verifying acknowledgment. The allied read-only capture uses the real host/guest session.

The explicit debug/plain/topdown equivalents are in `before/gameplay/` and `after/gameplay/` (1080p under `1920/`). Illustrated equivalents are under each folder's `forest/` subdirectory. Artifacts are ignored local files; the source report and verification probes are repository files.

## Verification

All 15 recorded validation commands completed with exit 0. Command exits and durations are recorded in [verification.json](../artifacts/forest-ui-refinement/verification.json); complete transcripts are in [the artifact directory](../artifacts/forest-ui-refinement/).

| Command | Result | Evidence |
| --- | --- | --- |
| `mise run build` | Exit 0 · 2.23s. 0 warnings; 0 errors; pinned dependency verification and locked restore. | [Log](../artifacts/forest-ui-refinement/build.log) |
| `mise run test-session` | Exit 0 · 54.50s. 3 actual-process integration tests passed. | [Log](../artifacts/forest-ui-refinement/test-session.log) |
| `mise run test-gameplay-ui` | Exit 0 · 52.96s. Illustrated/angled and debug/plain/topdown native Controls passed; real seven-wave host/guest replay passed. | [Log](../artifacts/forest-ui-refinement/test-gameplay-ui.log) |
| `mise run capture-gameplay-ui` | Exit 0 · 133.14s. Real before/after-compatible gameplay panels and menu overlays captured at 720p/1080p in both presentation modes; same native assertions and network replay passed. | [Log](../artifacts/forest-ui-refinement/capture-gameplay-ui.log) |
| `mise run capture-settlement` | Exit 0 · 18.40s. All nine painted pads, native clear selection/purchase, fixed camera, both projections, modal/focus checks; 720p/1080p plus three additional sizes. | [Log](../artifacts/forest-ui-refinement/capture-settlement.log) |
| `mise exec -- bash tools/graphical.sh capture-settlement --hud-art illustrated` | Exit 0 · 16.26s. Same nine-pad/camera/input checks using the default illustrated HUD; all sizes passed. | [Log](../artifacts/forest-ui-refinement/capture-settlement-forest.log) |
| `mise run capture-worldtree` | Exit 0 · 16.23s. 18 sockets, circular picking, prerequisite connections, selection/focus/hover, research quotes/purchases and acquired states passed at both resolutions. | [Log](../artifacts/forest-ui-refinement/capture-worldtree.log) |
| `mise run capture-art` | Exit 0 · 70.44s. 14 actual Godot verification frames, including plain fallback, illustrated HUD and the existing showroom. | [Log](../artifacts/forest-ui-refinement/capture-art.log) |
| `mise run test-multiplayer` | Exit 0 · 67.83s. 1, 2, 3, 4, 5 and 6 actual Godot processes: admission, legal opening, readiness, combat, individual drafting and identical wave-two snapshots passed. | [Log](../artifacts/forest-ui-refinement/test-multiplayer.log) |
| `mise run test-reconnect` | Exit 0 · 56.35s. Existing reconnect/fault suite passed, including reconnect during Building, Preparation, Combat and RewardDraft. | [Log](../artifacts/forest-ui-refinement/test-reconnect.log) |
| `env COMMON_WATCH_GAMEPLAY_ART=illustrated mise exec -- bash tools/graphical.sh test-gameplay-network` | Exit 0 · 25.99s. Actual illustrated pending/rejected/disconnected feedback captured at both resolutions; visible native status and seven-wave host/guest replay passed. | [Log](../artifacts/forest-ui-refinement/capture-network-forest.log) |
| `mise run test-steam` | Exit 0 · 4.80s. 61 offline Steam/session/animation logic tests passed; 0 failed; 0 skipped. Live accounts were not tested. | [Log](../artifacts/forest-ui-refinement/test-steam.log) |
| `mise run test-core` | Exit 0 · 2.54s. 46 passed; 0 failed; 0 skipped. | [Log](../artifacts/forest-ui-refinement/test-core.log) |
| `mise run test-shared` | Exit 0 · 33.76s. 88 passed; 0 failed; 0 skipped. | [Log](../artifacts/forest-ui-refinement/test-shared.log) |
| `mise run test-art` | Exit 0 · 0.43s. 19 Pillow/art tests passed. | [Log](../artifacts/forest-ui-refinement/test-art.log) |

The [screenshot inventory](../artifacts/forest-ui-refinement/screenshot-inventory.json) verifies all 296 before/after review references: every file exists and has its claimed 1280×720 or 1920×1080 dimensions. SHA-256 hashes are included. The review JavaScript passes `node --check`.

The visual review examined actual default illustrated and explicit debug/plain frames at 720p and 1080p. Construction and confirmation, all resource incomes, context dismissal, Army tabs, reward alignment and important feedback and the popup over existing screens were inspected visually as well as through native assertions. The checks also cover modal click capture (including HUD controls at the viewport edge), native F10/controller Start and cancel, keyboard/controller focus, all nine plot targets, both projections, fixed settlement input and authoritative purchases.

## Preservation and limitations

The [preservation receipt](../artifacts/forest-ui-refinement/preservation-receipt.json) compares 290 recorded asset, ForestHud, Core and Shared files with their pre-edit SHA-256 hashes. None changed. There are no tracked differences in `assets`, `art`, `src/Game.Godot/UI/ForestHud`, `src/Game.Core` or `src/Game.Shared`. No artwork was generated, downloaded, replaced or installed, including the rejected continuity buildings and Moonleaf overhaul.

One iteration hit the known native .NET/JIT `UnmanagedCallersOnly` failure (exit 139, [log](../artifacts/forest-ui-refinement/iteration-3.log)); a subsequent restore had an MSBuild child-node premature exit ([log](../artifacts/forest-ui-refinement/iteration-3-retry.log)). Those failed attempts are retained separately from final successful validation. Earlier visual/input assertions also caught and led to correction of card-text clipping, doctrine-menu overflow, recovery confirmation bounds and HUD coverage of the upper and bottom-right pads; compilation alone was not treated as success.

The reported [GitHub Actions run 39560834801](https://github.com/seraph1nia/common-watch-v2/actions/runs/39560834801) was inspected separately. It failed with `Network opening rejected: Stale match, phase or turn` at `tools/session-probe/OpeningProbe.cs:35`, through `RunNetworkProbe`, and also logged missing `rg` on the runner. This UI change does not modify that policy or the runner setup. Local multiplayer passes do not prove the CI race fixed; no fresh GitHub Actions run was triggered.

Validation uses Linux/Xvfb/Mesa software rendering and automated Dummy audio. Live Steam accounts, Windows/macOS execution, physical GPU/controller/audio hardware and release exports were not qualified. Long contextual lists and unusually long doctrine descriptions retain native scrolling. Existing pre-edit environment/settings changes remain in the workspace.

## Steam invitation follow-up

The invite button previously made only a void `activateGameOverlayInviteDialog` request, silently skipping the call if the lobby/login was unavailable. It provided no visible failure result when the overlay could not appear. That is a confirmed failure path in the implementation; the user's specific Steam client environment has not been reproduced with live accounts.

**Invite Steam friends** now opens a native in-game picker. Selecting or refreshing does not invite anyone. **Invite selected** checks the current lobby and friendship again, submits `inviteUserToLobby`, and retains the SDK result or failure in the dialog. Submission is explicitly distinguished from delivery/acceptance. **Copy ID** provides direct joining as a fallback. See [session instructions](SESSIONS.md#optional-steam).

| File | Follow-up change |
| --- | --- |
| `Networking/Steam/SteamInvitations.cs` | Small invitation operation with a testable platform boundary, sorted existing friends, stale-lobby/friend/login guards and actionable SDK failures. |
| `Networking/Steam/GodotSteamPlatform.cs` | Uses the pinned SDK's existing-friend enumeration and direct lobby invitation calls; peer creation and transport identity are unchanged. |
| `Networking/Steam/SteamCoordinator.cs` | Binds picker operations to the active Steam host lobby. |
| `UI/SteamFriendsDialog.cs` | Native themed modal with explicit submission, refresh, scroll/selection retention, focus restoration, visible status and lobby-ID copying. Empty/error results do not leave a large empty friend list. |
| `Application/SteamScreens.cs`, `Application/GameApplication.cs` | Connects the existing invite button, blocks world/menu input under the picker and closes it on session replacement. |
| `Development/MenuChecks.cs`, `Development/StartupChecks.cs` | Checks native picker sizing, error visibility, focus restoration, explicit submission and retained selection/focus/scroll; checks required native extension methods without logging in. |
| `tests/Game.Godot.Session.Tests/SteamInvitationTests.cs`, its `.csproj` | Nine new offline cases covering no sends during enumeration, exact target submission, stale lobbies, offline state, removed friends, SDK refusal and exceptions. |
| `docs/SESSIONS.md` | Documents the picker, the old silent overlay path and live verification limits. |

Follow-up verification on the final code:

| Command | Result |
| --- | --- |
| `mise run build` | Exit 0; zero warnings/errors. [Log](../artifacts/steam-invite/logs/build.log) |
| `mise run test-core` | Exit 0; 46 passed. [Log](../artifacts/steam-invite/logs/core.log) |
| `mise run test-shared` | Exit 0; 88 passed. [Log](../artifacts/steam-invite/logs/shared.log) |
| `mise run test-steam` | Exit 0; 70 passed. [Log](../artifacts/steam-invite/logs/tests.log) |
| `mise run test-session` | Exit 0; three actual-process tests passed, 62.11s including build. [Log](../artifacts/steam-invite/logs/session.log) |
| `mise run capture-gameplay-ui` | Exit 0, 134.53s; illustrated/plain at both resolutions, native picker assertions and seven-wave host/guest UI checks passed. [Log](../artifacts/steam-invite/logs/capture.log) |
| `mise run test-multiplayer` | Exit 0, 85.22s; one through six players. [Log](../artifacts/steam-invite/logs/multiplayer.log) |
| `mise run test-reconnect` | Exit 0, 67.35s. [Log](../artifacts/steam-invite/logs/reconnect.log) |
| Offline `--check-steam-extension` probe | Exit 0; GodotSteam 4.22.1 required methods present on Godot 4.7.2. Early-exit probe also logged two CanvasItem RIDs/two ObjectDB instances; it does not qualify SDK connectivity. [Log](../artifacts/steam-invite/logs/abi.log) |

Actual unavailable-lobby Godot captures were visually inspected: [illustrated 720p](../artifacts/steam-invite/forest/1280x720.png), [illustrated 1080p](../artifacts/steam-invite/forest/1920x1080.png), [plain 720p](../artifacts/steam-invite/plain/1280x720.png), [plain 1080p](../artifacts/steam-invite/plain/1920x1080.png). [Dimensions and hashes](../artifacts/steam-invite/screenshots.json) cover all four. Populated-list selection/submission assertions use explicitly named UI test friends and a local callback; they do not contact Steam or fabricate simulation state. No live invitations were sent, and no captures claim to show real friend accounts or delivery.

Iteration checks caught an initially oversized wrapping-label dialog and an ambiguous C# `Range` reference; both were corrected. One combined capture run passed all four graphical passes but received SIGTERM on entering its network step ([receipt](../artifacts/steam-invite/logs/capture-terminated.log)); the full rerun above passed. The 290-file preservation comparison was repeated with zero changes. Live Steam delivery, acceptance, relay and authenticated reconnect remain unqualified under the existing native gate. The earlier CI failure remains tracked separately.
