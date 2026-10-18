using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private SettlementHud _gameUi = null!;
    private LobbyPanel _sessionMenu = null!;
    private void CreateSessionMenu()
    {
        var layer = new CanvasLayer { Layer = 2 }; AddChild(layer);
        _sessionMenu = new LobbyPanel(); layer.AddChild(_sessionMenu); _sessionMenu.Initialize(_skin);
        _sessionMenu.SoloRequested += BeginSolo;
        _sessionMenu.HostRequested += () => BeginHost(int.Parse(Argument("--port") ?? "27025"));
        _sessionMenu.JoinRequested += (endpoint, fresh) =>
        {
            string file = System.IO.Path.Combine(SessionDataPath, "resume.json");
            if (fresh && System.IO.File.Exists(file)) System.IO.File.Delete(file);
            BeginJoin(endpoint);
        };
        _sessionMenu.DoctrineSelected += id => { _session.Send("doctrine:" + id); RefreshUi(true); };
        _sessionMenu.StartRequested += () => Act(() => _session.Start());
        _sessionMenu.MenuRequested += ReturnToMenu;
        _sessionMenu.QuitRequested += RequestQuit;
        CreateSteamMenu(_sessionMenu.Content);
    }
    private void BeginSolo()
    {
        ReplaceSession(new SoloSession(matchId: Guid.NewGuid().ToString("N")));
        _frozen = false; RefreshUi(true);
    }
    private void ReturnToMenu()
    {
        _battle = null; _animationProfile = null; _battleLayer?.QueueFree(); _battleLayer = null;
        _battlefield.Reset();
        _sessions.Close(); _steam?.Leave();
        _gameUi.Reset(); _camera.StopDrag();
        _gameUi.Visible = false; _sessionMenu.Visible = true;
        RefreshSessionMenu(); RefreshWorld();
    }
    private void RefreshSessionMenu()
    {
        _sessionMenu.Refresh(_session);
        _gameUi.Visible = !_sessionMenu.Visible;
        RefreshSteamMenu();
    }
    private string SessionDataPath => Argument("--data-path") ?? ProjectSettings.GlobalizePath("user://sessions");
    private void BeginHost(int port)
    {
        ReturnToMenu();
        try { _sessions.Open(session => session.Host(port)); _frozen = false; RefreshUi(true); }
        catch (Exception e) { ReturnToMenu(); _sessionMenu.Roster.Text = e.Message; }
    }
    private void BeginJoin(string endpoint)
    {
        ReturnToMenu();
        try
        {
            var parts = endpoint.Split(':');
            _sessions.Open(session => session.Join(parts[0], parts.Length == 2 ? int.Parse(parts[1]) : 27025, SessionDataPath, int.Parse(Argument("--core-version") ?? WireJson.ProtocolVersion.ToString())));
            _frozen = false; RefreshSessionMenu();
        }
        catch (Exception e) { ReturnToMenu(); _sessionMenu.Roster.Text = e.Message; }
    }
    private void ReconnectCurrent()
    {
        if (_session is { IsHost: true }) return;
        ulong lobby = _steam.Lobby;
        if (lobby != 0) { ReturnToMenu(); _steam.OfferJoin(lobby); }
        else BeginJoin(Argument("--join") ?? _sessionMenu.Endpoint.Text);
    }
}
