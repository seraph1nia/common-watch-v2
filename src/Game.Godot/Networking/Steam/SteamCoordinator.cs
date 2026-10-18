using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class SteamCoordinator : Node
{
    private readonly GodotSteamPlatform _steam = new();
    private readonly SteamOperationState _steamOperations = new();
    private readonly List<(StringName Signal, Callable Callback)> _steamSignals = [];
    private long _sessionGeneration;
    private ulong _steamLobby, _steamHost, _nextSteamCheck;
    private bool _steamLaunchRead;
    private string _steamFeedback = "", _steamPhase = "";
    private SessionCoordinator _sessions = null!;
    private GodotSession _session => _sessions.Current!;
    private string[] _args = [];
    private string SessionDataPath = "";
    private bool _disposed;
    public ulong Lobby => _steamLobby;
    public string Feedback => _steamFeedback;
    public event Action? Changed, MenuRequested, ConfirmationClosed;
    public event Action<Action, Action>? ConfirmationRequested;
    public void Initialize(SessionCoordinator sessions, string[] args, string dataPath)
    {
        _sessions = sessions; _args = args; SessionDataPath = dataPath;
        if (_args.Contains("--steam") || _args.Contains("--steam-host") || Argument("--steam-join") is not null || SteamLobbyRules.InvitationTargets(_args.Concat(OS.GetCmdlineArgs())).Length != 0) EnsureSteam();
    }
    private string? Argument(string key)
    {
        int index = Array.IndexOf(_args, key);
        return index >= 0 && index + 1 < _args.Length ? _args[index + 1] : null;
    }
    public void InviteFriends()
    {
        if (_steamLobby != 0 && _steam.LoggedIn) _steam.Api.Call("activateGameOverlayInviteDialog", _steamLobby);
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
                SubscribeSteam("join_requested", Callable.From<ulong, ulong>((lobby, _) => OfferJoin(lobby)));
            }
            return ready;
        }
        catch (Exception) { _steamFeedback = "Steam initialization unavailable. Offline and LAN remain available."; return false; }
    }
    private void SubscribeSteam(StringName signal, Callable handler)
    { if (_steam.Api.Connect(signal, handler) != Error.Ok) throw new IOException("Steam callback connection failed."); _steamSignals.Add((signal, handler)); }
    public void Host()
    {
        if (_session is not null || !EnsureSteam()) { Changed?.Invoke(); return; }
        if (_steamOperations.BeginCreate(_sessionGeneration, Time.GetTicksMsec(), _steam.LoggedIn))
        {
            _steamFeedback = "Creating private lobby…";
            try { _steam.Api.Call("createLobby", 0, 4); }
            catch (Exception) { _steamOperations.CreateFailed(); _steamFeedback = "Steam lobby request failed. Try again."; }
        }
        Changed?.Invoke();
    }
    private void SteamCreated(long result, ulong lobby)
    {
        if (lobby != 0 && lobby == _steamLobby) return;
        if (!_steamOperations.CompleteCreate(_sessionGeneration, _session is null) || result != 1 || lobby == 0)
        { _steam.Leave(lobby); _steamFeedback = "Steam lobby creation did not complete."; return; }
        try
        {
            _steamLobby = lobby; _steamHost = _steam.Identity; _steamPhase = "";
            _sessions.Open(session => session.HostSteam(_steam.Peer(), _steamHost, identity => _steam.Member(lobby, identity))); GD.Print($"GODOT_STEAM_LOBBY {lobby}"); PublishSteam();
            if (Argument("--steam-invite") is { } recipient && ulong.TryParse(recipient, out ulong friend))
            {
                if (_steam.Api.Call("getFriendRelationship", friend).AsInt64() != 3 || !_steam.Api.Call("inviteUserToLobby", lobby, friend).AsBool())
                    throw new IOException("Steam friend invitation was refused.");
                GD.Print("GODOT_STEAM_INVITE_SUBMITTED");
            }
            Changed?.Invoke();
        }
        catch (Exception) { MenuRequested?.Invoke(); _steamFeedback = "Steam could not create a gameplay peer."; }
    }
    public void OfferJoin(ulong lobby)
    {
        if (!EnsureSteam() || !_steamOperations.Offer(lobby, _steamLobby, _sessionGeneration, _steam.LoggedIn)) return;
        long offered = _sessionGeneration;
        void Accept()
        {
            if (!_steamOperations.AcceptConfirmation(lobby, _sessionGeneration, offered)) return;
            MenuRequested?.Invoke();
            if (_steamOperations.BeginJoin(lobby, _sessionGeneration, Time.GetTicksMsec(), _steam.LoggedIn))
            {
                _steamFeedback = "Joining Steam lobby…";
                try { _steam.Api.Call("joinLobby", lobby); }
                catch (Exception) { _steamOperations.JoinFailed(lobby); _steamFeedback = "Steam join request failed. Try again."; }
            }
        }
        if (_session is null) { Accept(); return; }
        ConfirmationRequested?.Invoke(Accept, () => _steamOperations.DeclineConfirmation(lobby, offered));
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
            _sessions.Open(session => session.JoinSteam(_steam.Peer(host), host, lobby, _steam.AppId, match, SessionDataPath));
            Changed?.Invoke();
        }
        catch (Exception) { _steam.Leave(lobby); MenuRequested?.Invoke(); _steamFeedback = "Invalid Steam lobby or unavailable original host."; }
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
    public void Poll()
    {
        if (!_steam.Initialized) return;
        try
        {
            _steam.Poll();
            if (!_steamLaunchRead && _steam.LoggedIn)
            {
                _steamLaunchRead = true;
                if (_args.Contains("--steam-host")) Host();
                else if (Argument("--steam-join") is { } target && ulong.TryParse(target, out ulong lobby)) OfferJoin(lobby);
                else if (SteamLobbyRules.InvitationTargets(_args.Concat(OS.GetCmdlineArgs()).Concat(_steam.Api.Call("getLaunchCommandLine").AsString().Split(' ', StringSplitOptions.RemoveEmptyEntries))).FirstOrDefault() is > 0 and var invitation) OfferJoin(invitation);
            }
            if (_steamOperations.ExpireConfirmation(_sessionGeneration)) ConfirmationClosed?.Invoke();
            if (_steamOperations.CancelIfInvalid(_sessionGeneration, _session is null, Time.GetTicksMsec(), 15000, out ulong abandoned))
            { _steam.Leave(abandoned); _steamFeedback = "Steam operation timed out."; }
            PublishSteam();
            if (_steamLobby != 0 && _session is { IsHost: false } && Time.GetTicksMsec() >= _nextSteamCheck)
            {
                _nextSteamCheck = Time.GetTicksMsec() + 1000;
                if (_steam.Api.Call("getLobbyOwner", _steamLobby).AsUInt64() != _steamHost)
                { MenuRequested?.Invoke(); _steamFeedback = "The original host left. This session ended."; }
            }
        }
        catch (Exception) { MenuRequested?.Invoke(); _steamFeedback = "Steam unavailable. Offline and LAN remain available."; }
    }
    public void Leave()
    {
        _sessionGeneration++; _steamOperations.Cancel(); ConfirmationClosed?.Invoke();
        _steam.Leave(_steamLobby); _steamLobby = _steamHost = 0; _steamPhase = "";
    }
    public void Shutdown()
    {
        if (_disposed) return; _disposed = true;
        Leave();
        foreach (var (signal, callback) in _steamSignals) _steam.Api.Disconnect(signal, callback);
        _steamSignals.Clear(); _steam.Dispose();
        Changed = MenuRequested = ConfirmationClosed = null; ConfirmationRequested = null;
    }
    public override void _ExitTree() => Shutdown();
}
