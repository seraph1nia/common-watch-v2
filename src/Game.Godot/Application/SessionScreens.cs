using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private SettlementHud _gameUi = null!;
    private LobbyPanel _sessionMenu = null!;
    private LobbyPanel _menuPopup = null!;
    private SessionExitDialog _exitConfirmation = null!;
    private Control? _menuInvoker;
    private void CreateSessionMenu()
    {
        var layer = new CanvasLayer { Layer = 2 }; AddChild(layer);
        _sessionMenu = new LobbyPanel(); layer.AddChild(_sessionMenu); _sessionMenu.Initialize(_skin);
        var popupLayer = new CanvasLayer { Layer = 6 }; AddChild(popupLayer);
        _menuPopup = new LobbyPanel(); popupLayer.AddChild(_menuPopup); _menuPopup.Initialize(_skin, popup: true);
        _menuPopup.Refresh(null); _menuPopup.Hide();
        _exitConfirmation = new SessionExitDialog { FallbackFocus = _gameUi.MenuButton }; AddChild(_exitConfirmation); _exitConfirmation.Initialize(_skin);
        foreach (var menu in new[] { _sessionMenu, _menuPopup })
        {
            menu.SoloRequested += BeginSolo;
            menu.HostRequested += () => BeginHost(int.Parse(Argument("--port") ?? "27025"));
            menu.JoinRequested += JoinFromMenu;
            menu.QuitRequested += RequestQuit;
            menu.SettingsRequested += OpenMenuSettings;
            menu.NavigationChanged += RefreshSteamMenu;
        }
        _menuPopup.ContinueRequested += CloseMenu;
        _settings.ReturnToBattleRequested += ReturnToBattle;
        _exitConfirmation.ReturnToBattleRequested += ReturnToBattle;
        _menuPopup.MenuRequested += RequestReturnToMenu;
        _sessionMenu.DoctrineSelected += id => { _session.Send("doctrine:" + id); RefreshUi(true); };
        _sessionMenu.StartRequested += () => { if (_session.AllDoctrinesChosen) Act(() => _session.Start()); };
        _sessionMenu.MenuRequested += RequestReturnToMenu;
        CreateSteamMenu(_sessionMenu.MultiplayerContent);
    }
    private void RefreshLivePhaseNotice()
    {
        bool active = _session is { HasState: true } && _session.Snapshot.Phase != Phase.Lobby && _gameUi.Visible;
        bool live = active && _gameUi.Presentation.LiveCombat;
        string notice = !active ? "" : live ? $"Wave {_gameUi.PresentedWave} · battle is live. The match continues behind this screen."
            : _gameUi.Presentation.Moment == HudMoment.ChoosingReward ? $"Wave {_session.City.Run.DraftWave} · rewards are ready behind this screen."
            : _gameUi.Presentation.Moment == HudMoment.RewardWaiting ? "Reward accepted · teammates are choosing."
            : _gameUi.Presentation.Moment == HudMoment.Terminal ? $"Run complete · {_session.Snapshot.Phase}."
            : _gameUi.Presentation.Moment == HudMoment.Observer ? "Observing · survivors advance the match."
            : $"Wave {_session.Snapshot.Wave} · preparation continues behind this screen.";
        _menuPopup.SetLivePhase(live, notice); _settings.SetLivePhase(live, notice); _exitConfirmation.SetLivePhase(live, notice);
    }
    private void ReturnToBattle()
    {
        CloseMenu(false);
        if (_settings.Visible) _settings.Close();
        _gameUi.ReturnToBattle(); RefreshUi(true); RefreshWorld();
    }
    private void OpenMenuSettings() { CloseMenu(false); _settings.Open(); }
    private void RequestReturnToMenu()
    {
        if (_session is null) ReturnToMenu();
        else _exitConfirmation.Ask(false, _session, ReturnToMenu);
    }
    private void JoinFromMenu(string endpoint, bool fresh)
    {
        _sessionMenu.Endpoint.Text = _menuPopup.Endpoint.Text = endpoint;
        string file = System.IO.Path.Combine(SessionDataPath, "resume.json");
        if (fresh && System.IO.File.Exists(file)) System.IO.File.Delete(file);
        BeginJoin(endpoint);
    }
    private void OpenMenu()
    {
        if (_menuPopup.Visible) return;
        _menuInvoker = GetViewport().GuiGetFocusOwner(); _camera.StopDrag();
        _settings.CoveredByMenu = true;
        _menuPopup.ShowMain(); _menuPopup.Refresh(null); RefreshSteamMenu();
        _menuPopup.ReturnToMenuButton!.Disabled = _session is null;
        _menuPopup.ContinueButton!.GrabFocus();
    }
    private void CloseMenu() => CloseMenu(true);
    private void CloseMenu(bool restoreFocus)
    {
        if (_menuPopup is null || !_menuPopup.Visible) return;
        _menuPopup.Hide();
        _settings.CoveredByMenu = false;
        if (_steamPanel is not null) RefreshSteamMenu();
        if (restoreFocus)
        {
            if (IsInstanceValid(_menuInvoker) && _menuInvoker!.IsVisibleInTree()) _menuInvoker.GrabFocus();
            else if (_settings.Visible) _settings.Slider.GrabFocus();
            else if (_gameUi.Visible) _gameUi.MenuButton.GrabFocus();
            else _sessionMenu.FocusDefault();
        }
        _menuInvoker = null;
    }
    private void BeginSolo()
    {
        ReplaceSession(new SoloSession(matchId: ReviewSoloMatchId, rules: ReviewPreparationRules));
        _frozen = false; RefreshUi(true);
    }
    private void ReturnToMenu()
    {
        _exitConfirmation?.Clear();
        if (_settings is { Visible: true }) _settings.Close();
        CloseMenu(false);
        _battle = null; _animationProfile = null; _battleLayer?.QueueFree(); _battleLayer = null;
        _battlefield.Reset();
        _sessions.Close(); _steam?.Leave();
        _gameUi.Reset(); _camera.StopDrag();
        _gameUi.Visible = false; _sessionMenu.Visible = true; _sessionMenu.ShowMain();
        RefreshSessionMenu(); RefreshWorld();
        _sessionMenu.FocusDefault();
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
        try { _sessions.Open(session => session.Host(port, Argument("--control-dir") is not null ? Argument("--test-match-id") : null, ReviewPreparationRules)); _frozen = false; RefreshUi(true); }
        catch (Exception e) { ReturnToMenu(); _sessionMenu.ShowError(e.Message); }
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
        catch (Exception e) { ReturnToMenu(); _sessionMenu.ShowError(e.Message); }
    }
    private void ReconnectCurrent()
    {
        if (_session is { IsHost: true }) return;
        ulong lobby = _steam.Lobby;
        if (lobby != 0) { ReturnToMenu(); _steam.OfferJoin(lobby); }
        else BeginJoin(Argument("--join") ?? _sessionMenu.Endpoint.Text);
    }
}
