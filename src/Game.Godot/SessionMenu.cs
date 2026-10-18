using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class Main
{
    private Control _gameUi = null!;
    private PanelContainer _sessionMenu = null!;
    private Label _lobbyRoster = null!;
    private LineEdit _endpoint = null!;
    private Button _hostButton = null!, _joinButton = null!, _freshJoin = null!;
    private VBoxContainer _doctrines = null!;
    private Button _soloButton = null!, _startButton = null!;

    private void CreateSessionMenu()
    {
        var layer = new CanvasLayer { Layer = 2 }; AddChild(layer);
        _sessionMenu = new PanelContainer(); layer.AddChild(_sessionMenu);
        _sessionMenu.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _sessionMenu.OffsetLeft = -320; _sessionMenu.OffsetRight = 320;
        _sessionMenu.OffsetTop = -340; _sessionMenu.OffsetBottom = 340;
        var column = new VBoxContainer(); _sessionMenu.AddChild(Margin(column, 24));
        column.AddChild(new Label { Text = "THE COMMON WATCH", Modulate = _gold });
        _lobbyRoster = WrapLabel(); column.AddChild(_lobbyRoster);
        _soloButton = Button("Single player", BeginSolo); column.AddChild(_soloButton);
        _hostButton = Button("Host LAN", () => BeginHost(int.Parse(Argument("--port") ?? "27025"))); column.AddChild(_hostButton);
        _endpoint = new LineEdit { Text = "127.0.0.1:27025", PlaceholderText = "host:port" }; column.AddChild(_endpoint);
        _joinButton = Button("Join / reconnect LAN", () => BeginJoin(_endpoint.Text)); column.AddChild(_joinButton);
        _freshJoin = Button("Join LAN as new player (forget resume)", () =>
        {
            string file = System.IO.Path.Combine(SessionDataPath, "resume.json");
            if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
            BeginJoin(_endpoint.Text);
        }); column.AddChild(_freshJoin);
        _doctrines = new VBoxContainer(); column.AddChild(_doctrines);
        foreach (var doctrine in RunCatalog.Doctrines)
        {
            string id = doctrine.Id;
            _doctrines.AddChild(Button(doctrine.Name, () => { _session.Send("doctrine:" + id); RefreshUi(true); }));
        }
        _startButton = Button("Start match", () => Act(() => _session.Start())); column.AddChild(_startButton);
        column.AddChild(Button("Return to main menu", ReturnToMenu));
        column.AddChild(Button("Quit", async () => await QuitAfterAudioDrain()));
        CreateSteamMenu(column);
    }
    private void BeginSolo()
    {
        ReplaceSession(new SoloSession(matchId: Guid.NewGuid().ToString("N")));
        _frozen = false; RefreshUi(true);
    }
    private void ReturnToMenu()
    {
        _session?.Dispose(); _session = null!; LeaveSteam();
        _effects.Clear(); _uiKey = ""; _dragging = false;
        _gameUi.Visible = false; _sessionMenu.Visible = true;
        RefreshSessionMenu(); QueueRedraw();
    }
    private void RefreshSessionMenu()
    {
        _sessionMenu.Visible = _session is null || !_session.HasState || _session.Snapshot.Phase == Phase.Lobby;
        _gameUi.Visible = !_sessionMenu.Visible;
        _freshJoin.Visible = _soloButton.Visible = _hostButton.Visible = _joinButton.Visible = _endpoint.Visible = _session is null;
        _doctrines.Visible = _session is { HasState: true, Connected: true };
        _startButton.Visible = _session is { HasState: true, IsHost: true };
        _lobbyRoster.Text = _session is null ? "Single player or LAN • 1–4 players" : !_session.HasState ? _session.Status :
            $"{_session.Status} • {4 - _session.Snapshot.Players.Length} slots\n" + string.Join("\n", _session.Snapshot.Players.Select(p => $"Player {p.Id}{(p.Id == 1 ? " [host]" : "")}{(p.Id == _session.PlayerId ? " [you]" : "")}: {RunCatalog.Doctrine(p.Run.Doctrine).Name} {(p.Connected ? "" : "offline")}"));
        RefreshSteamMenu();
    }
    private string SessionDataPath => Argument("--data-path") ?? ProjectSettings.GlobalizePath("user://sessions");
    private void BeginHost(int port)
    {
        ReturnToMenu(); _session = new GodotSession { Name = "Session" }; AddChild(_session);
        try { _session.Host(port); _frozen = false; RefreshUi(true); }
        catch (Exception e) { ReturnToMenu(); _lobbyRoster.Text = e.Message; }
    }
    private void BeginJoin(string endpoint)
    {
        ReturnToMenu(); _session = new GodotSession { Name = "Session" }; AddChild(_session);
        try
        {
            var parts = endpoint.Split(':');
            _session.Join(parts[0], parts.Length == 2 ? int.Parse(parts[1]) : 27025, SessionDataPath, int.Parse(Argument("--core-version") ?? WireJson.ProtocolVersion.ToString()));
            _frozen = false; RefreshSessionMenu();
        }
        catch (Exception e) { ReturnToMenu(); _lobbyRoster.Text = e.Message; }
    }
    private void ReconnectCurrent()
    {
        if (_session is { IsHost: true }) return;
        ulong lobby = _steamLobby;
        if (lobby != 0) { ReturnToMenu(); OfferSteamJoin(lobby); }
        else BeginJoin(Argument("--join") ?? _endpoint.Text);
    }
    private void SmokeSessionLifecycle()
    {
        ReturnToMenu(); Require(_session is null && _sessionMenu.Visible, "menu owns no authority");
        _soloButton.EmitSignal(BaseButton.SignalName.Pressed);
        var old = _session!; string match = old.Snapshot.MatchId;
        Require(old.Snapshot.Phase == Phase.Lobby, "pregame selection");
        ((Button)_doctrines.GetChild(1)).EmitSignal(BaseButton.SignalName.Pressed);
        Require(old.City.Run.Doctrine == RunCatalog.Doctrines[1].Id, "doctrine UI");
        _startButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(old.Snapshot.Phase == Phase.Building, "start UI");
        ReturnToMenu(); BeginSolo();
        Require(_session!.Snapshot.MatchId != match && _session.City.Run.Doctrine == "warden", "replacement isolation");
        try { old.Send("ready"); throw new InvalidOperationException("Disposed session accepted input"); }
        catch (ObjectDisposedException) { }
        ReplaceSession(ScenarioRunner.CreateAt("combat")); ReturnToMenu(); BeginSolo();
        Require(_session.Snapshot.Phase == Phase.Lobby && _effects.Count == 0, "leave during combat");
        GD.Print("GODOT_SESSION_PASS menu doctrine start replacement cleanup combat-leave offline");
    }
}
