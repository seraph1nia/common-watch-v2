using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task SmokeIntegration()
    {
        if (_args.Contains("--expect-default-presentation"))
        {
            Require(_options.Mode == RenderMode.Sprites && _options.Hud == HudArt.Illustrated
                && _options.View == ViewPreset.Angled && !_options.DebugOverlay && !_options.Showroom,
                "normal play and export presentation defaults");
            Require(_sessions.Current is null && _sessionMenu.Visible && _battle is null && !_frozen,
                "normal startup reaches the production main menu without a scenario");
            GD.Print("GODOT_DEFAULTS_PASS mode=Sprites view=Angled hud=Illustrated overlay=false main-menu=true");
        }
        await SmokeSessionLifecycle();
        ulong musicPlayer = _audio.Player.GetInstanceId();
        string checkpoint = Argument("--checkpoint") ?? "settlement";
        ReplaceSession(new SoloSession()); _session.Start(); RefreshUi(true);
        SelectAt(_camera.ToScreen(_settlementView.PlotPosition(0)));
        Require(_session.SelectedSlot == 0, "hex selection");
        Require(_gameUi.Inspector.BuildButtons.TryGetValue(Building.Lumbermill, out var build), "construction UI");
        build!.EmitSignal(BaseButton.SignalName.Pressed);
        _gameUi.Inspector.Construction.ConfirmButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.City.Slots[0].Type == Building.Lumbermill && _session.LastResult?.Accepted == true, "authoritative construction button");
        ReplaceSession(ScenarioRunner.CreateAt("building-selected")); RefreshUi(true);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        _gameUi.ReadyButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.LastResult?.Accepted == true, "ready button");
        RefreshUi(true);
        Require(_gameUi.Inspector.RecruitButton is not null, "recruitment UI");
        int roster = _session.City.Soldiers.Length;
        _gameUi.Inspector.RecruitButton!.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.LastResult?.Accepted == true && _session.City.Soldiers.Length == roster + 1, "recruitment button");
        _gameUi.Details.Close();
        ReplaceSession(ScenarioRunner.CreateAt("combat"));
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        long tick = _session.Snapshot.Tick;
        for (int i = 0; i < 60; i++) _session.Step();
        Require(_session.Snapshot.Tick > tick && _session.Playback.Units().Length > 0, "combat updates");
        ReplaceSession(ScenarioRunner.CreateAt("draft")); RefreshUi(true);
        Require(_gameUi.Draft.RewardButtons.Count == 3 && _gameUi.Draft.Visible, "three reward cards");
        _gameUi.Draft.RewardButtons[0].EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session.Snapshot.Phase is Phase.Building or Phase.Preparation && _session.LastResult?.Accepted == true, "reward selection button");
        Vector2I original = GetWindow().Size;
        GetWindow().Size = new Vector2I(1920, 1080);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(GetWindow().Size == new Vector2I(1920, 1080), "1920x1080 resize");
        Require(GetViewport().GetVisibleRect().Encloses(_gameUi.Resources.GetGlobalRect()), "resources remain in viewport after resize");
        Require(GetViewport().GetVisibleRect().Encloses(_gameUi.ReadyButton.GetGlobalRect()), "ready control remains in viewport after resize");
        Require(GetViewport().GetVisibleRect().Encloses(_gameUi.Draft.GetGlobalRect()), "reward overlay remains in viewport after resize");
        await SmokeAudioSettings();
        GetWindow().Size = original;
        ReplaceSession(ScenarioRunner.CreateAt(checkpoint)); _frozen = true; RefreshUi(true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await SmokeAudioSettings();
        await SmokeCameraAndInput();
        Require(_options.Mode != RenderMode.Debug || _assets.DecodeAttempts == 0, "no-assets ignores sprite files");
        Require(_audio.Player.GetInstanceId() == musicPlayer, "music player persists across game resets and phases");
        if (_args.Contains("--expect-installed-art"))
        {
            Require(_assets.Catalog.Entries.Count == 47 && _assets.Catalog.Resolve("unit/enemy_berserker", true) is not null && _skin.Illustrated && _skin.NinePatches > 0, "installed native forest StyleBoxTexture skins");
            Require(_options.Mode != RenderMode.Sprites || _assets.WorldTextureCount > 0, "native world PNG decoding");
            if (_options.Mode == RenderMode.Sprites)
                foreach (var entry in _assets.Catalog.Entries.Where(e => !e.Id.StartsWith("hud/", StringComparison.Ordinal)))
                    Require(_assets.Load(entry.Id) is not null, "native installed PNG decoding: " + entry.Id);
            GD.Print($"GODOT_ART_PASS view={_options.View} hud={_options.Hud} native-nine-slices={_skin.NinePatches} dynamic-text=true");
        }
        GD.Print($"SMOKE PASS Godot mode={_options.Mode} checkpoint={checkpoint}; startup, drawing, selection, construction, recruitment, ready, combat, draft, resize; sprite_decode_attempts={_assets.DecodeAttempts}");
    }
}
