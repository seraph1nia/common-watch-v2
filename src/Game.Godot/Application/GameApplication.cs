using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

/// <summary>Application lifetime, active session and screen coordination; gameplay remains authoritative C#.</summary>
public partial class GameApplication : Node
{
    private SessionCoordinator _sessions = null!;
    private GodotSession _session => _sessions.Current!;
    private RenderOptions _options = null!;
    private AssetLoader _assets = null!;
    private HudSkin _skin = null!;
    private EnvironmentController _environment = null!;
    private FeedbackCoordinator _feedback = null!;
    private bool _frozen;
    private string[] _args = [];

    public override async void _Ready()
    {
        try
        {
            _sessions = GetNode<SessionCoordinator>("Sessions");
            _sessions.Changed += OnSessionChanged;
            _args = OS.GetCmdlineUserArgs();
            GetWindow().MinSize = new Vector2I(1280, 720);
            if (_args.Contains("--check-steam-extension"))
            {
                CheckSteamExtension();
                GetTree().Quit(); return;
            }
            if (Argument("--width") is { } width && Argument("--height") is { } height) GetWindow().Size = new Vector2I(int.Parse(width), int.Parse(height));
            _options = RenderOptions.Parse(_args);
            if (Argument("--assets-root") is null) _options = _options with { AssetsRoot = AssetDirectory("sprites") };
            _assets = new AssetLoader(_options);
            _skin = new HudSkin(_assets);
            CreateWorld();
            InitializeMusic();
            string? checkpoint = Argument("--checkpoint");
            if (Argument("--battle") is { } scenario) { _battle = new(scenario); checkpoint = "initial"; }
            if (checkpoint is not null) WrapSolo(GameplayScenarios.Checkpoints.Contains(checkpoint) ? GameplayScenarios.CreateAt(checkpoint) : ScenarioRunner.CreateAt(checkpoint));
            _frozen = checkpoint is not null;
            CreateUi();
            CreateSessionMenu();
            _feedback = GetNode<FeedbackCoordinator>("Feedback");
            _feedback.Initialize(_audio, _gameUi, _settlementView, _battlefield); _feedback.Bind(_session);
            if (Argument("--host") is { } hostPort) BeginHost(int.Parse(hostPort));
            else if (Argument("--join") is { } endpoint) BeginJoin(endpoint);
            else if (_session is null) ReturnToMenu(); else RefreshUi(true);
            if (_options.Showroom) { ReturnToMenu(); _sessionMenu.Visible = false; var showroom = new AssetShowroom(); AddChild(showroom); showroom.Initialize(_assets, _skin, int.Parse(Argument("--showroom-page") ?? "1") - 1); }
            if (_battle is not null) InitializeBattle();
            RefreshWorld();
            await RunDevelopmentActions();
        }
        catch (Exception exception)
        {
            GD.PrintErr(exception);
            GetTree().Quit(1);
        }
    }

    private string? Argument(string key)
    {
        int at = Array.IndexOf(_args, key);
        return at >= 0 && at + 1 < _args.Length ? _args[at + 1] : null;
    }

    private static string AssetDirectory(string folder) => OS.HasFeature("editor")
        ? ProjectSettings.GlobalizePath("res://../../assets/" + folder)
        : System.IO.Path.Combine(System.IO.Path.GetDirectoryName(OS.GetExecutablePath())!, "assets", folder);

    private void CreateUi()
    {
        var layer = new CanvasLayer(); AddChild(layer);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = _skin.Theme };
        layer.AddChild(root); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _gameUi = new SettlementHud(); root.AddChild(_gameUi); _gameUi.Initialize(_skin);
        _gameUi.MenuRequested += OpenMenu;
        _gameUi.ReconnectRequested += ReconnectCurrent;
        _gameUi.CommandSubmitted += ResumeAfterCommand;
        _gameUi.ArenaChanged += RefreshWorld;
        _gameUi.ArenaOverviewRequested += OverviewArena;
        _gameUi.CombatFramingRequested += PrepareBattleCamera;
        CreateAudioUi();
    }
    private void ResumeAfterCommand() { _frozen = false; RefreshUi(true); RefreshWorld(); }
    private void Act(Action action)
    {
        action();
        _frozen = false; RefreshUi(true); RefreshWorld();
    }
    private GodotSession WrapSolo(SoloSession solo) => _sessions.Open(session => session.Solo(solo));
    private void OnSessionChanged() { _settings?.SetLivePhase(false, ""); _menuPopup?.SetLivePhase(false, ""); _exitConfirmation?.SetLivePhase(false, ""); _exitConfirmation?.Clear(); _steamFriends?.Close(); CloseMenu(false); _battlefield?.Reset(); _gameUi?.Reset(); _feedback?.Bind(_session); ResetCamera(); }
    private void ReplaceSession(SoloSession session) { _steam?.Leave(); WrapSolo(session); _gameUi.Visible = true; _sessionMenu.Visible = false; _battlefield?.Reset(); _gameUi?.Reset(); _feedback?.Bind(_session); ResetCamera(); }
    private void ResetCamera() => _camera?.Reset();

    public override void _Process(double delta)
    {
        if (_assets is null) return;
        _assets.Configure(_options);
        _steam.Poll();
        _environment.Suspended = _battle?.Paused ?? (_session is { HasState: true } && (_frozen || _session.Snapshot.Paused || !_session.Connected));
        RunPreparationObservations();
        RunControlProbe();
        if (_session is null || _gameUi is null) return;
        _animationProfile?.Begin(delta);
        if (_battle is not null) _battle.Advance(_animationProfile?.Step ?? delta);
        else if (!_frozen) _session.Advance(delta);
        if (!_session.HasState) { RefreshSessionMenu(); return; }
        RefreshWorld();
        RunNetworkProbe();
        RefreshUi();
        _feedback.Update(_session, _battle is null);
    }

    private void RefreshUi(bool force = false)
    {
        if (_session is null || !_session.HasState) { RefreshSessionMenu(); return; }
        if (_battle is not null) { _gameUi.Hide(); _sessionMenu.Hide(); UpdateBattleHud(); return; }
        RefreshSessionMenu();
        if (_gameUi.Visible)
        {
            _gameUi.OverlayCovered = _settings.Visible || _menuPopup.Visible || _exitConfirmation.Visible;
            _gameUi.Refresh(_session, _options, _battlefield.Finishing, force);
            RefreshLivePhaseNotice();
        }
    }

    private WorldCamera _camera = null!;
    private SettlementView _settlementView = null!;
    private BattlefieldView _battlefield = null!;
    private ArenaPreview _arenaPreview = null!;
    private ArenaBackdrop _arenaBackdrop = null!;
    private BattleShowcase? _battle;
    private bool CombatShader => _battlefield.ShaderEnabled;
    private Vector2 WorldOrigin => WorldArea.GetCenter();
    private void CreateWorld()
    {
        _environment = GetNode<EnvironmentController>("Environment");
        _environment.Initialize(_assets, _args, Argument("--settings-path") is { } settings ? settings + ".environment.json" : ProjectSettings.GlobalizePath("user://environment-settings.json"));
        _camera = GetNode<WorldCamera>("World/Camera");
        _arenaBackdrop = GetNode<ArenaBackdrop>("ArenaBackdrop");
        _settlementView = GetNode<SettlementView>("World/Settlement"); _settlementView.Initialize(_assets, _environment);
        _arenaPreview = GetNode<ArenaPreview>("World/ArenaPreview"); _arenaPreview.Initialize(_assets, _environment);
        _battlefield = GetNode<BattlefieldView>("World/Battlefield");
        _battlefield.ShaderEnabled = Argument("--combat-shader") != "off";
        _battlefield.Initialize(_assets, _environment); _battlefield.FramePresented += OnBattleFrame;
        _camera.Layout(WorldOrigin);
        InputActions.Register();
    }
    private void OnBattleFrame()
    {
        if (_animationProfile is not null && _animationProfile.End(_battle!, _battlefield.AnimationTick, _battlefield.Motion.Events.Count, _battlefield.FigureCount))
        {
            if (_animationProfile.Done) _ = QuitAfterAudioDrain(); else Engine.MaxFps = _animationProfile.Fps;
        }
    }
    private void RefreshWorld()
    {
        if (_camera is null) return;
        _assets.Configure(_options);
        if (_session is not { HasState: true }) { _settlementView.Hide(); _arenaPreview.Hide(); _battlefield.Reset(); _arenaBackdrop.Hide(); return; }
        _battlefield.SelectedUnit = _gameUi.LiveInspector.SelectedUnit;
        _battlefield.SelectedStructure = _gameUi.LiveInspector.SelectedStructure;
        _battlefield.Present(_session, _battle);
        bool preview = !_battlefield.Active && _gameUi.ShowArena && _session.Snapshot.Phase is Phase.Building or Phase.Preparation;
        _arenaPreview.Present(_session.Snapshot, preview, _options.GroundCompression);
        _arenaBackdrop.Present(_assets, _battlefield.Active || preview);
        _settlementView.Present(_session, !_battlefield.Active && !preview && _session.Snapshot.Phase != Phase.Lobby);
        if (_settlementView.Visible)
        {
            var frame = _gameUi.SettlementFramingArea;
            _camera.Frame(frame.Position + _settlementView.ScreenOrigin(frame.Size), _settlementView.Framing(frame.Size));
        }
        else if (_battlefield.Active || preview)
        {
            var state = _battle?.Snapshot ?? _session.Snapshot;
            string key = $"{state.MatchId}:{state.Arena!.Sectors.Length}:{_options.View}:{_options.Mode}:{WorldArea.Size}";
            if (_camera.Fixed || key != _arenaCameraKey)
            {
                _arenaCameraKey = key;
                var layout = new ArenaLayout(state.Arena, new HexProjection(30, _options.GroundCompression));
                var focus = layout.SectorCenter(_session.PlayerId);
                _camera.FrameArena(_battlefield.Active ? _battlefield.RenderBounds : _arenaPreview.RenderBounds, WorldArea.Size, new Vector2(focus.X, focus.Y), layout.Pitch);
            }
        }
        Vector2 mouse = GetViewport().GetMousePosition();
        _settlementView.HoverAt(!_gameUi.BlocksWorld && !_settings.Visible && !_inviteConfirmation.Visible && !_steamFriends.Visible && !_exitConfirmation.Visible && !_menuPopup.Visible && WorldArea.HasPoint(mouse)
            ? _camera.ToWorld(mouse) : null);
    }
    public override void _Input(InputEvent input)
    {
        if (_inviteConfirmation is { Visible: true } || _steamFriends is { Visible: true } || _exitConfirmation is { Visible: true }) { _camera.StopDrag(); return; }
        if (input.IsActionPressed("game_menu"))
        {
            if (_menuPopup.Visible) CloseMenu(); else OpenMenu();
            GetViewport().SetInputAsHandled(); return;
        }
        if (_menuPopup.Visible && input.IsActionPressed("ui_cancel"))
        { CloseMenu(); GetViewport().SetInputAsHandled(); return; }
        if (input is InputEventMouseButton { Pressed: false } mouse && mouse.ButtonIndex is MouseButton.Middle or MouseButton.Right) _camera.StopDrag();
    }
    public override void _UnhandledInput(InputEvent input)
    {
        if (_inviteConfirmation.Visible || _steamFriends.Visible || _exitConfirmation.Visible) return;
        if (input.IsActionPressed("ui_cancel"))
        {
            if (_settings.Visible) _settings.Close(); else if (_gameUi.Details.Visible) _gameUi.Details.Close(); else if (_gameUi.Draft.Visible) _gameUi.Draft.Dismiss(); else if (_gameUi.LiveInspector.Visible) _gameUi.LiveInspector.Close(); else OpenMenu();
            GetViewport().SetInputAsHandled(); return;
        }
        if (_menuPopup.Visible || _settings.Visible || _inviteConfirmation.Visible || GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit) return;
        if (_battle is not null)
        {
            if (input.IsActionPressed("game_pause")) { HandleBattleKey(Key.Space); GetViewport().SetInputAsHandled(); return; }
            if (input.IsActionPressed("toggle_art_mode")) { HandleBattleKey(Key.T); GetViewport().SetInputAsHandled(); return; }
            if (input.IsActionPressed("show_debug_overlay")) { HandleBattleKey(Key.O); GetViewport().SetInputAsHandled(); return; }
            if (input is InputEventKey { Pressed: true, Echo: false } key
                && key.Keycode is Key.Backspace or Key.Tab or Key.V or Key.Key1 or Key.Key2 or Key.Key3 or Key.Key4 or Key.Key5 or Key.Key6)
            { HandleBattleKey(key.Keycode); GetViewport().SetInputAsHandled(); return; }
        }
        if (_session is null || !_session.HasState || _session.Snapshot.Phase == Phase.Lobby || _gameUi.BlocksWorld) return;
        if (!_settlementView.Visible && input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F })
        { OverviewArena(); GetViewport().SetInputAsHandled(); return; }
        var area = WorldArea;
        if (!_settlementView.Visible) _camera.Handle(input, area);
        if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click && area.HasPoint(click.Position) )
            SelectAt(click.Position);
    }
    private Rect2 WorldArea => GetViewport().GetVisibleRect();
    private string _arenaCameraKey = "";
    private void PrepareBattleCamera() { _arenaCameraKey = ""; _camera.StopDrag(); }
    private void OverviewArena()
    {
        if (_battlefield.Active) FrameArena(_battlefield.RenderBounds);
        else if (_arenaPreview.Visible) FrameArena(_arenaPreview.RenderBounds);
    }
    private void FrameArena(Rect2 bounds)
    {
        _camera.FrameArena(bounds, WorldArea.Size);
    }
    private void SelectAt(Vector2 point)
    {
        Vector2 world = _camera.ToWorld(point);
        if (_arenaPreview.Visible)
        {
            if (_arenaPreview.PickUnit(world) is { } unit) _gameUi.Inspect(unit, unit.Id < 0);
            else if (_arenaPreview.PickStructure(world) is { } structure) _gameUi.Inspect(structure);
        }
        else if (_battlefield.Active)
        {
            if (_battlefield.PickUnit(world) is { } unit) _gameUi.Inspect(unit);
            else if (_battlefield.PickStructure(world) is { } structure) _gameUi.Inspect(structure);
        }
        else if (_settlementView.Pick(world) is { } index) _session.SelectSlot(index);
        RefreshUi(true);
    }

    private static void Require(bool success, string check) { if (!success) throw new InvalidOperationException("Godot integration failed: " + check); }
    public override void _ExitTree()
    {
        _sessions.Changed -= OnSessionChanged;
        if (_battlefield is not null) _battlefield.FramePresented -= OnBattleFrame;
        if (_gameUi is not null)
        {
            _gameUi.MenuRequested -= OpenMenu; _gameUi.ReconnectRequested -= ReconnectCurrent;
            _gameUi.CommandSubmitted -= ResumeAfterCommand; _gameUi.ArenaChanged -= RefreshWorld;
            _gameUi.ArenaOverviewRequested -= OverviewArena;
            _gameUi.CombatFramingRequested -= PrepareBattleCamera;
        }
        if (_closeSubscribed) GetWindow().CloseRequested -= RequestQuit;
        if (_sessionMenu is not null) _sessionMenu.SettingsRequested -= OpenMenuSettings;
        if (_menuPopup is not null) _menuPopup.SettingsRequested -= OpenMenuSettings;
        if (_menuPopup is not null) _menuPopup.MenuRequested -= RequestReturnToMenu;
        if (_sessionMenu is not null) _sessionMenu.MenuRequested -= RequestReturnToMenu;
        if (_menuPopup is not null) _menuPopup.QuitRequested -= RequestQuit;
        if (_sessionMenu is not null) _sessionMenu.QuitRequested -= RequestQuit;
        if (_exitConfirmation is not null) _exitConfirmation.Clear();
        if (_settings is not null) { _settings.Opened -= StopCameraDrag; _settings.ReturnToBattleRequested -= ReturnToBattle; }
        if (_exitConfirmation is not null) _exitConfirmation.ReturnToBattleRequested -= ReturnToBattle;
        _audio?.Stop();
        _sessions.Close();
        _steam?.Shutdown();
        _skin?.Dispose();
        _assets?.Dispose();
    }
}
