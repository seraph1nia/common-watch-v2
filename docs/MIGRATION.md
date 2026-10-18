# Godot-only migration record

Starting commit: `4ffabb2e29cfb00b4aa44865abc0acd9835bb4ed`.

## Removal baseline

The discarded MonoGame application, its session tests, Gum, LiteNetLib and Facepunch distribution, runtime locks and CI were removed first. Existing Godot behavior was retained without importing the other client's implementation. Scripts and tests now launch Godot directly; canonical mise tasks use generic names. These dependency names appear here solely to record the removal history.

The initial working tree contained a three-line Gum resize fix in the removed `WatchGame.cs`. Its exact binary diff was preserved before deletion at `/home/bart/.local/state/common-watch-v2-migration/preexisting.patch`; it was not silently discarded. No other initial changes existed.

Build/test results are recorded in VERIFICATION.md. This document is included in the dedicated removal commit, which leaves the original Godot implementation untouched. Native architecture work starts only after that verified commit.

## Native refactor

The removal gate was committed as `fba7945293cbdbc5e13512bc7ffb6e103220fa9d` before any Godot implementation changes.

`Application/SessionCoordinator` owns creation and closure of the active session node and emits a presentation-only C# change event. ENet RPCs, GodotSteam identity/protocol rules and Core command semantics remain in the existing session implementation. Solo replacement and old-session disposal continue to use the same path.

`Art/AssetLoader` owns cached native external PNG textures, optional catalog metadata and disposal. `UI/HudSkin` shares the native Theme and illustrated nine-slices. The asset extraction passed the full native UI/fallback suite and nine art captures.

`Audio/AudioController` owns the unchanged WAV and native Music bus. SFX/UI buses are defined in a text resource. The existing JSON settings path and format remain unchanged. `Scenes/UI/SettingsPanel.tscn` is agent-editable; its C# script owns native modal controls and focus restoration. Native bus volume, mute, loop markers and persistence pass actual-client checks.

`SettlementHud`, `BuildingInspector`, `DraftPanel`, `RewardCard` and `LobbyPanel` own native layouts and commands from their displayed state. Lobby content scrolls at smaller heights. The application now coordinates screens rather than constructing every action and card. Native UI and art captures passed after extraction.

`WorldCamera` adopts Camera2D for camera transforms while retaining the shared mathematical hex projection. Native transform/inverse picking passes all nine plots at 1280×720 and 1920×1080, topdown/angled, zoom 0.55/1/2.2 with pan. Actual viewport input checks cover world selection, UI scrolling, modal click capture and focus restoration. A synthetic wheel event initially omitted release; the harness now sends complete native input sequences.

`Combat/BattlefieldView` owns persistent actors, deterministic sampled effects and terminal cleanup. Actors use native Sprite2D children and the reusable HitFlash shader resource; geometric fallback remains batched native drawing. All timing stays on Core's playback clock. Nine profiled battles completed with zero final active effects. TileMapLayer, AnimationPlayer and autonomous particle timers were deliberately avoided: the small custom hex projection and sampled effects already provide bounded, precise behavior with less synchronization code.

`Networking/Steam/SteamCoordinator` owns callbacks and operation generations; separate native Controls own confirmations and lobby actions. Offline logic and Steam-disabled actual UI pass. Live two-account qualification remains unexecuted.

`Scenes/Main.tscn` now declares service/world ownership as text; `Application/GameApplication.cs` coordinates startup, screens and exit. Capture, ABI verification and showcase instrumentation live under Development. No editor session is required. Core, its lockfile, soundtrack, original art and surviving dependency versions remain unchanged.
