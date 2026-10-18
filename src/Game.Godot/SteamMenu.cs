using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class Main
{
    private readonly GodotSteamPlatform _steam = new();
    private readonly SteamOperationState _steamOperations = new();
    private readonly List<(StringName Signal, Callable Callback)> _steamSignals = [];
    private long _sessionGeneration;
    private ulong _steamLobby, _steamHost, _nextSteamCheck;
    private bool _steamLaunchRead;
    private Action? _acceptSteamInvitation, _declineSteamInvitation;
    private string _steamFeedback = "", _steamPhase = "";
    private Button _steamCreate = null!, _steamJoin = null!, _steamInvite = null!;
    private LineEdit _steamTarget = null!;
    private ConfirmationDialog _inviteConfirmation = null!;

    private void CreateSteamMenu(VBoxContainer column)
    {
        _steamCreate = Button("Host private Steam lobby", HostSteam); column.AddChild(_steamCreate);
        _steamTarget = new LineEdit { PlaceholderText = "Steam lobby ID" }; column.AddChild(_steamTarget);
        _steamJoin = Button("Join Steam lobby", () => { if (ulong.TryParse(_steamTarget.Text, out ulong id)) OfferSteamJoin(id); }); column.AddChild(_steamJoin);
        _steamInvite = Button("Invite Steam friends", () => { if (_steamLobby != 0 && _steam.LoggedIn) _steam.Api.Call("activateGameOverlayInviteDialog", _steamLobby); }); column.AddChild(_steamInvite);
        _inviteConfirmation = new ConfirmationDialog { DialogText = "Leave this session and join your friend's game?", Title = "Steam invitation" }; AddChild(_inviteConfirmation);
        _inviteConfirmation.Confirmed += () => { var callback = _acceptSteamInvitation; _acceptSteamInvitation = _declineSteamInvitation = null; callback?.Invoke(); };
        _inviteConfirmation.Canceled += () => { var callback = _declineSteamInvitation; _acceptSteamInvitation = _declineSteamInvitation = null; callback?.Invoke(); };
        if (_args.Contains("--steam") || _args.Contains("--steam-host") || Argument("--steam-join") is not null || SteamLobbyRules.InvitationTargets(_args.Concat(OS.GetCmdlineArgs())).Length != 0) EnsureSteam();
    }
    private bool EnsureSteam()
    {
        try
        {
            bool ready = _steam.Initialize(out _steamFeedback);
            if (_steam.Initialized && _steamSignals.Count == 0)
            {
                SubscribeSteam("lobby_created", Callable.From<long, ulong>(SteamCreated));
                SubscribeSteam("lobby_joined", Callable.From<ulong, long, bool, long>(SteamJoined));
                SubscribeSteam("join_requested", Callable.From<ulong, ulong>((lobby, _) => OfferSteamJoin(lobby)));
            }
            return ready;
        }
        catch (Exception) { _steamFeedback = "Steam initialization unavailable. Offline and LAN remain available."; return false; }
    }
    private void SubscribeSteam(StringName signal, Callable handler)
    { if (_steam.Api.Connect(signal, handler) != Error.Ok) throw new IOException("Steam callback connection failed."); _steamSignals.Add((signal, handler)); }
    private void HostSteam()
    {
        if (_session is not null || !EnsureSteam()) { RefreshSessionMenu(); return; }
        if (_steamOperations.BeginCreate(_sessionGeneration, Time.GetTicksMsec(), _steam.LoggedIn))
        {
            _steamFeedback = "Creating private lobby…";
            try { _steam.Api.Call("createLobby", 0, 4); }
            catch (Exception) { _steamOperations.CreateFailed(); _steamFeedback = "Steam lobby request failed. Try again."; }
        }
        RefreshSessionMenu();
    }
    private void SteamCreated(long result, ulong lobby)
    {
        if (lobby != 0 && lobby == _steamLobby) return;
        if (!_steamOperations.CompleteCreate(_sessionGeneration, _session is null) || result != 1 || lobby == 0)
        { _steam.Leave(lobby); _steamFeedback = "Steam lobby creation did not complete."; return; }
        try
        {
            _session = new GodotSession { Name = "Session" }; AddChild(_session);
            _steamLobby = lobby; _steamHost = _steam.Identity; _steamPhase = "";
            _session.HostSteam(_steam.Peer(), _steamHost, identity => _steam.Member(lobby, identity)); GD.Print($"GODOT_STEAM_LOBBY {lobby}"); PublishSteam();
            if (Argument("--steam-invite") is { } recipient && ulong.TryParse(recipient, out ulong friend))
            {
                if (_steam.Api.Call("getFriendRelationship", friend).AsInt64() != 3 || !_steam.Api.Call("inviteUserToLobby", lobby, friend).AsBool())
                    throw new IOException("Steam friend invitation was refused.");
                GD.Print("GODOT_STEAM_INVITE_SUBMITTED");
            }
            RefreshUi(true);
        }
        catch (Exception) { ReturnToMenu(); _steamFeedback = "Steam could not create a gameplay peer."; }
    }
    private void OfferSteamJoin(ulong lobby)
    {
        if (!EnsureSteam() || !_steamOperations.Offer(lobby, _steamLobby, _sessionGeneration, _steam.LoggedIn)) return;
        long offered = _sessionGeneration;
        void Accept()
        {
            if (!_steamOperations.AcceptConfirmation(lobby, _sessionGeneration, offered)) return;
            ReturnToMenu();
            if (_steamOperations.BeginJoin(lobby, _sessionGeneration, Time.GetTicksMsec(), _steam.LoggedIn))
            {
                _steamFeedback = "Joining Steam lobby…";
                try { _steam.Api.Call("joinLobby", lobby); }
                catch (Exception) { _steamOperations.JoinFailed(lobby); _steamFeedback = "Steam join request failed. Try again."; }
            }
        }
        if (_session is null) { Accept(); return; }
        _acceptSteamInvitation = Accept;
        _declineSteamInvitation = () => _steamOperations.DeclineConfirmation(lobby, offered);
        _inviteConfirmation.PopupCentered();
    }
    private void SteamJoined(ulong lobby, long permissions, bool locked, long response)
    {
        var completion = _steamOperations.CompleteJoin(lobby, _steamLobby, _sessionGeneration, _session is null);
        if (completion == SteamJoinCompletion.Ignore) return;
        if (completion != SteamJoinCompletion.Accept || response != 1) { _steam.Leave(lobby); return; }
        try
        {
            var keys = SteamLobbyRules.MetadataKeys.Append("transport_version");
            var data = keys.ToDictionary(key => key, key => _steam.Api.Call("getLobbyData", lobby, key).AsString());
            ulong owner = _steam.Api.Call("getLobbyOwner", lobby).AsUInt64();
            if (!SteamLobbyMetadata.Read(data, _steam.AppId, owner, out ulong host, out string match) || host == _steam.Identity)
                throw new IOException("Incompatible Steam lobby or departed original host.");
            _steamLobby = lobby; _steamHost = host;
            _session = new GodotSession { Name = "Session" }; AddChild(_session);
            _session.JoinSteam(_steam.Peer(host), host, lobby, _steam.AppId, match, SessionDataPath);
            RefreshSessionMenu();
        }
        catch (Exception) { _steam.Leave(lobby); ReturnToMenu(); _steamFeedback = "Invalid Steam lobby or unavailable original host."; }
    }
    private void PublishSteam()
    {
        if (_steamLobby == 0 || _session is not { IsHost: true, HasState: true }) return;
        string phase = _session.Snapshot.Phase == Phase.Lobby ? "lobby" : "in-progress";
        if (phase == _steamPhase) return;
        foreach (var (key, value) in SteamLobbyMetadata.Create(_steam.AppId, _steamHost, _session.Snapshot.MatchId, phase != "lobby"))
            if (!_steam.Api.Call("setLobbyData", _steamLobby, key, value).AsBool()) throw new IOException("Cannot publish lobby metadata.");
        _steam.Api.Call("setLobbyJoinable", _steamLobby, true); _steamPhase = phase;
    }
    private void ProcessSteam()
    {
        if (!_steam.Initialized) return;
        try
        {
            _steam.Poll();
            if (!_steamLaunchRead && _steam.LoggedIn)
            {
                _steamLaunchRead = true;
                if (_args.Contains("--steam-host")) HostSteam();
                else if (Argument("--steam-join") is { } target && ulong.TryParse(target, out ulong lobby)) OfferSteamJoin(lobby);
                else if (SteamLobbyRules.InvitationTargets(_args.Concat(OS.GetCmdlineArgs()).Concat(_steam.Api.Call("getLaunchCommandLine").AsString().Split(' ', StringSplitOptions.RemoveEmptyEntries))).FirstOrDefault() is > 0 and var invitation) OfferSteamJoin(invitation);
            }
            if (_steamOperations.ExpireConfirmation(_sessionGeneration)) _inviteConfirmation.Hide();
            if (_steamOperations.CancelIfInvalid(_sessionGeneration, _session is null, Time.GetTicksMsec(), 15000, out ulong abandoned))
            { _steam.Leave(abandoned); _steamFeedback = "Steam operation timed out."; }
            PublishSteam();
            if (_steamLobby != 0 && _session is { IsHost: false } && Time.GetTicksMsec() >= _nextSteamCheck)
            {
                _nextSteamCheck = Time.GetTicksMsec() + 1000;
                if (_steam.Api.Call("getLobbyOwner", _steamLobby).AsUInt64() != _steamHost)
                { ReturnToMenu(); _steamFeedback = "The original host left. This session ended."; }
            }
        }
        catch (Exception) { ReturnToMenu(); _steamFeedback = "Steam unavailable. Offline and LAN remain available."; }
    }
    private void LeaveSteam()
    {
        _sessionGeneration++; _steamOperations.Cancel(); _inviteConfirmation?.Hide(); _acceptSteamInvitation = _declineSteamInvitation = null;
        _steam.Leave(_steamLobby); _steamLobby = _steamHost = 0; _steamPhase = "";
    }
    private void RefreshSteamMenu()
    {
        _steamCreate.Visible = _steamJoin.Visible = _steamTarget.Visible = _session is null;
        _steamInvite.Visible = _steamLobby != 0 && _session is { IsHost: true };
        if (_steamFeedback.Length > 0) _lobbyRoster.Text += "\n" + _steamFeedback;
    }
    private void DisposeSteam()
    {
        LeaveSteam();
        foreach (var (signal, callback) in _steamSignals) _steam.Api.Disconnect(signal, callback);
        _steamSignals.Clear(); _steam.Dispose();
    }
}
