using System.Text.Json;
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>One session node owns authority and ENet/RPC delivery on the application thread.</summary>
public partial class GodotSession : Node, IDisposable
{
    public const string Application = "common-watch-v2-godot/1";
    private AuthoritySession? _authority;
    private SoloSession? _solo;
    private MultiplayerPeer? _peer;
    private MultiplayerApi? _api;
    private SessionFile? _resume;
    private readonly string _attempt = Guid.NewGuid().ToString("N");
    private readonly HashSet<int> _admitted = [];
    // Host-local lobby acknowledgment, separate from simulation state and private resume data.
    // Survives peer reconnects for the lifetime of this authority; never infers choice from default Warden.
    private readonly HashSet<int> _doctrineChoices = [];
    public bool AllDoctrinesChosen => HasState && IsHost && (_authority?.Snapshot() ?? Snapshot).Players
        .Where(p => p.Connected).All(p => _doctrineChoices.Contains(p.Id));
    private void ObserveDoctrineChoice(Command? command, CommandResult? result)
    {
        if (result is { Accepted: true } && command is { ExpectedPhase: Phase.Lobby, City: > 0 } && command.Action.StartsWith("doctrine:", StringComparison.Ordinal))
            _doctrineChoices.Add(command.City);
    }
    private readonly Dictionary<int, (long Revision, ulong Sent)> _inFlight = [];
    private readonly Dictionary<long, (string Json, ulong Sent)> _pending = [];
    private long _sequence;
    private Func<int, string?>? _identity;
    private string? _expectedMatch;
    private string? _expectedHostIdentity;
    public bool IsSteam => _identity is not null;
    private int _requestedProtocol = WireJson.ProtocolVersion;
    private bool _disposed, _admissionClosed;
    private ulong _started = Time.GetTicksMsec();
    private double _publishCredit;
    public MatchSnapshot Snapshot { get; private set; } = null!;
    public bool HasState => Snapshot is not null;
    public bool Connected { get; private set; }
    public bool IsHost => _authority is not null || _solo is not null;
    public bool IsNetwork => _peer is not null;
    public int PlayerId { get; private set; }
    public int ViewedPlayerId { get; private set; }
    private ArmyConfiguration? _armyConfiguration;
    internal ArmyConfiguration ArmyConfiguration => _armyConfiguration ??= new(Snapshot.Rules.Army,
        new HexBoard(Snapshot.Arena!.Board), Snapshot.Rules.Arena.SquadCapacity);
    public CityState ViewedCity => Snapshot.Players.FirstOrDefault(p => p.Id == ViewedPlayerId) ?? City;
    public void ViewCity(int player)
    {
        if (!Snapshot.Players.Any(p => p.Id == player)) return;
        ViewedPlayerId = player; SelectSlot(-1);
    }
    public CityState City => Snapshot.Players.Single(p => p.Id == PlayerId);
    public int SelectedSlot { get; private set; } = -1;
    private readonly CombatPlayback _playback = new();
    public CombatPlayback Playback => _solo?.Playback ?? _playback;
    public CommandResult? LastResult { get; private set; }
    public event Action<Command, CommandResult>? CommandAcknowledged;
    internal long FeedbackSequence => _solo?.LastCommand?.Sequence ?? (_resume?.Value is { } saved ? saved.NextSequence - 1 : _sequence);
    internal Command? ObservedCommand => _solo?.LastCommand ?? _lastCommand;
    public string Status { get; private set; } = "Connecting…";
    internal bool ProbeDuplicates { get; set; }
    public bool Pending => _pending.Count != 0;

    public void Solo(SoloSession solo)
    {
        _solo = solo; PlayerId = solo.PlayerId; SelectedSlot = solo.SelectedSlot;
        Connected = true; Status = "Single player"; SetState(solo.Snapshot, true);
    }
    public void Host(int port)
    {
        var peer = new ENetMultiplayerPeer();
        if (peer.CreateServer(port, 8, 2) != Error.Ok) { peer.Dispose(); throw new IOException("Cannot listen on LAN port."); }
        Attach(peer);
        _authority = new(AuthorityPolicy.PlayingHost, matchId: Guid.NewGuid().ToString("N"), combatSeed: SoloSession.DefaultSeed);
        PlayerId = _authority.LocalPlayerId; Connected = true; Status = $"Hosting LAN :{port}";
        SetState(_authority.Snapshot(), true);
    }
    public void Join(string host, int port, string dataPath, int protocol = WireJson.ProtocolVersion)
    {
        _requestedProtocol = protocol;
        _resume = new SessionFile(System.IO.Path.Combine(dataPath, "resume.json"), $"{host}:{port}", "enet", Application);
        _resume.Load();
        var peer = new ENetMultiplayerPeer();
        if (peer.CreateClient(host, port, 2) != Error.Ok) { peer.Dispose(); throw new IOException("Cannot connect to LAN host."); }
        Attach(peer);
    }
    internal void HostSteam(MultiplayerPeer peer, ulong host, Func<ulong, bool> member)
    {
        _identity = id =>
        {
            string? identity = GodotSteamPlatform.PeerIdentity(peer, id);
            return ulong.TryParse(identity, out ulong steamId) && member(steamId) ? identity : null;
        };
        Attach(peer);
        _authority = new(AuthorityPolicy.PlayingHost, matchId: Guid.NewGuid().ToString("N"), originalHostIdentity: host.ToString(), requireTrustedIdentity: true, combatSeed: SoloSession.DefaultSeed);
        PlayerId = _authority.LocalPlayerId; Connected = true; Status = "Private Steam lobby"; SetState(_authority.Snapshot(), true);
    }
    internal void JoinSteam(MultiplayerPeer peer, ulong host, ulong lobby, uint appId, string match, string directory)
    {
        _identity = id => GodotSteamPlatform.PeerIdentity(peer, id);
        _expectedMatch = match; _expectedHostIdentity = host.ToString();
        _resume = new SessionFile(System.IO.Path.Combine(directory, "steam-resume.json"), $"{host}:{lobby}", "steam", Application + ":" + appId, host.ToString());
        _resume.Load(); Attach(peer);
    }
    private void Attach(MultiplayerPeer peer)
    {
        _peer = peer; _api = Multiplayer;
        Multiplayer.PeerDisconnected += OnDisconnected;
        Multiplayer.ConnectedToServer += OnConnected;
        Multiplayer.ConnectionFailed += OnFailure;
        Multiplayer.ServerDisconnected += OnFailure;
        Multiplayer.MultiplayerPeer = peer;
    }
    private void OnConnected()
    {
        if (_disposed || _resume is null) return;
        if (_identity is not null && _identity(1) != _expectedHostIdentity) { OnFailure(); return; }
        RpcId(1, MethodName.Admit, Application, _requestedProtocol, _attempt,
            _resume.Value?.Token ?? "", _expectedMatch ?? _resume.Value?.MatchId ?? "");
    }
    private void OnFailure() { if (!_disposed) { Connected = false; Status = "Host unavailable. Reconnect or return to menu."; _pending.Clear(); } }
    private void OnDisconnected(long peer)
    {
        if (_disposed) return;
        _admitted.Remove((int)peer); _inFlight.Remove((int)peer); _authority?.Disconnect((int)peer);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Admit(string application, int protocol, string attempt, string credential, string match)
    {
        if (_disposed || _authority is null || attempt is null || credential is null || match is null || attempt.Length != 32 || application != Application) return;
        int peer = Multiplayer.GetRemoteSenderId();
        var admission = _authority.Admit(peer, protocol, credential, Time.GetTicksMsec(), authenticatedIdentity: _identity?.Invoke(peer), expectedMatchId: match.Length == 0 ? null : match);
        if (admission.Ignored) return;
        RpcId(peer, MethodName.Welcome, attempt, admission.Accepted, admission.PlayerId, admission.Credential,
            admission.Message, admission.State is null ? "" : SnapshotPayload.Encode(admission.State));
        if (admission.Accepted) _admitted.Add(peer);
    }
    [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Welcome(string attempt, bool accepted, int player, string credential, string message, string state)
    {
        if (_disposed || IsHost || _admissionClosed || HasState || attempt != _attempt || _resume is null) return;
        if (!accepted) { _admissionClosed = true; Status = message; Connected = false; return; }
        try
        {
            if (credential is null || credential.Length != 64) throw new InvalidDataException("Invalid private admission credential.");
            var snapshot = SnapshotPayload.DecodeForReceiving(state);
            if (!snapshot.Players.Any(p => p.Id == player) || _resume.Value is { } saved && saved.MatchId != snapshot.MatchId)
                throw new InvalidDataException("Admission does not match this session.");
            _resume.Welcome(snapshot.MatchId, credential); PlayerId = player;
            Connected = true; Status = "Connected"; SetState(snapshot, true);
        }
        catch (Exception e) when (e is InvalidDataException or IOException or UnauthorizedAccessException) { Status = e.Message; Connected = false; }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Request(string json)
    {
        if (_disposed || _authority is null || json is null) return;
        int peer = Multiplayer.GetRemoteSenderId();
        long before = _authority.Revision;
        if (_authority.Request(peer, json, Time.GetTicksMsec()) is { } result)
        {
            // Only a newly applied command is evidence; a cached ledger reply may have a different payload.
            if (result.Accepted && _authority.Revision > before) ObserveDoctrineChoice(JsonSerializer.Deserialize<Command>(json, WireJson.Options), result);
            RpcId(peer, MethodName.Acknowledgment, _authority.MatchId, JsonSerializer.Serialize(result, WireJson.Options));
        }
    }
    [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Acknowledgment(string match, string json)
    {
        if (_disposed || IsHost || !HasState || match != Snapshot.MatchId || json.Length > 4096) return;
        var result = JsonSerializer.Deserialize<CommandResult>(json, WireJson.Options);
        if (result is not null && _pending.Remove(result.Sequence, out var request))
        {
            LastResult = result;
            if (JsonSerializer.Deserialize<Command>(request.Json, WireJson.Options) is { } command) CommandAcknowledged?.Invoke(command, result);
        }
    }
    [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, TransferChannel = 1)]
    private void State(string match, string payload)
    {
        if (_disposed || IsHost) return;
        if (!Connected || !HasState) { RpcId(1, MethodName.Received, match, -1L); return; }
        if (match != Snapshot.MatchId) return;
        try
        {
            var state = SnapshotPayload.DecodeForReceiving(payload);
            if (state.MatchId != match) throw new InvalidDataException("Stale match.");
            SetState(state); RpcId(1, MethodName.Received, match, state.Revision);
        }
        catch (InvalidDataException) { OnFailure(); Status = "Incompatible authoritative state. Rejoin a matching host."; }
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Received(string match, long revision)
    {
        int peer = Multiplayer.GetRemoteSenderId();
        if (!_disposed && _authority?.MatchId == match && _inFlight.TryGetValue(peer, out var flight) && (flight.Revision == revision || revision == -1))
            _inFlight.Remove(peer);
    }
    [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void Ended(string match)
    { if (!_disposed && HasState && Snapshot.MatchId == match) { Connected = false; Status = "The original host ended the game."; _pending.Clear(); } }
    private void SetState(MatchSnapshot state, bool baseline = false)
    {
        if (HasState && (Snapshot.MatchId != state.MatchId || state.Revision < Snapshot.Revision)) return;
        bool gap = HasState && state.OldestEventSequence > Snapshot.EventSequence + 1;
        Playback.Accept(state, baseline); Snapshot = state;
        if (!state.Players.Any(p => p.Id == ViewedPlayerId)) ViewedPlayerId = PlayerId;
        if (gap) Status = "Recovered current combat baseline after history gap.";
    }
    internal void ProbeCommand(string action, int owner, bool duplicate = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Connected || !HasState) return;
        long sequence = _authority is null ? _resume!.Reserve() : ++_sequence;
        var command = Command.FromSnapshot(Snapshot, sequence, action, owner);
        string json = JsonSerializer.Serialize(command, WireJson.Options);
        if (_authority is not null) { LastResult = _authority.ExecuteLocal(command); if (duplicate) _authority.ExecuteLocal(command); SetState(_authority.Snapshot()); }
        else
        {
            LastResult = null;
            _pending.Add(sequence, (json, Time.GetTicksMsec())); RpcId(1, MethodName.Request, json);
            if (duplicate) RpcId(1, MethodName.Request, json);
        }
    }
    public void RetryLast()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Connected || !HasState || Pending) return;
        if (_solo is not null) { LastResult = _solo.RetryLast(); SetState(_solo.Snapshot); if (_solo.LastCommand is { } replayedCommand && LastResult is { } result) CommandAcknowledged?.Invoke(replayedCommand, result); return; }
        if (_authority is not null && _lastCommand is { } last) { LastResult = _authority.ExecuteLocal(last); SetState(_authority.Snapshot()); CommandAcknowledged?.Invoke(last, LastResult); return; }
        if (_resume?.Value?.LastCommand is not { } json) return;
        var command = JsonSerializer.Deserialize<Command>(json, WireJson.Options);
        if (command is null || command.MatchId != Snapshot.MatchId) return;
        LastResult = null;
        _pending.Add(command.Sequence, (json, Time.GetTicksMsec())); RpcId(1, MethodName.Request, json);
    }
    public void Start() => Send("start");
    public void ChooseReward(string id) => Send("reward:" + id);
    public void SelectSlot(int slot) { if (slot is < -1 or >= 9) throw new ArgumentOutOfRangeException(nameof(slot)); SelectedSlot = slot; _solo?.SelectSlot(slot); }
    private Command? _lastCommand;
    public void Submit(GameplayIntent intent) => Send(GameplayIntent.Name(intent.Action), intent.Slot, intent.Building,
        intent.SoldierType, intent.Payment, intent.Technology, intent.Resource, intent.Bundles, intent.UnitId, intent.SquadId, intent.Cell);
    public void Send(string action, int slot = -1, Building building = Building.Empty,
        UnitType soldierType = UnitType.Swordsman, ConstructionPayment payment = ConstructionPayment.Standard,
        TechnologyId technology = TechnologyId.None, Resource resource = Resource.Wood, int bundles = 0,
        int unitId = 0, int squadId = 0, int cell = 0)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Connected || !HasState) { Status = "Disconnected: action unavailable."; return; }
        if (_solo is not null)
        {
            LastResult = _solo.Send(action, slot, building, soldierType, payment, technology, resource, bundles, unitId, squadId, cell);
            ObserveDoctrineChoice(_solo.LastCommand, LastResult);
            SetState(_solo.Snapshot);
            if (_solo.LastCommand is { } observed) CommandAcknowledged?.Invoke(observed, LastResult);
            return;
        }
        if (_pending.Count >= 16) { Status = "Waiting for authority acknowledgments."; return; }
        long sequence = _authority is null ? _resume!.Reserve() : ++_sequence;
        var command = Command.FromSnapshot(Snapshot, sequence, action, PlayerId, slot, building, soldierType,
            technology, resource, bundles, payment, unitId, squadId, cell);
        _lastCommand = command;
        if (_authority is not null) { LastResult = _authority.ExecuteLocal(command); ObserveDoctrineChoice(command, LastResult); SetState(_authority.Snapshot()); CommandAcknowledged?.Invoke(command, LastResult); }
        else
        {
            string json = JsonSerializer.Serialize(command, WireJson.Options);
            _resume!.Remember(json);
            LastResult = null; _pending.Add(sequence, (json, Time.GetTicksMsec())); RpcId(1, MethodName.Request, json);
            if (ProbeDuplicates) RpcId(1, MethodName.Request, json);
        }
    }
    public void Advance(double seconds)
    {
        if (_disposed) return;
        if (_solo is not null) { _solo.Advance(seconds); SetState(_solo.Snapshot); }
        if (_authority is not null) { _authority.Advance(seconds); SetState(_authority.Snapshot()); }
        if (_solo is null) Playback.Advance(seconds, Connected);
        if (IsNetwork && !_admissionClosed && !Connected && !HasState && Time.GetTicksMsec() - _started > 15000)
        { _admissionClosed = true; _peer?.Close(); Status = "Connection/admission timed out. Return to menu."; }
        foreach (var pair in _pending.ToArray())
            if (Time.GetTicksMsec() - pair.Value.Sent > 1500 && Connected)
            { RpcId(1, MethodName.Request, pair.Value.Json); _pending[pair.Key] = (pair.Value.Json, Time.GetTicksMsec()); }
        _publishCredit += seconds;
        if (_authority is null || _publishCredit < .1) return;
        _publishCredit = 0;
        string? payload = null;
        foreach (int peer in _admitted.ToArray())
        {
            if (_inFlight.TryGetValue(peer, out var flight))
            { if (Time.GetTicksMsec() - flight.Sent > 10000) _peer!.DisconnectPeer(peer); continue; }
            payload ??= SnapshotPayload.Encode(Snapshot);
            _inFlight[peer] = (Snapshot.Revision, Time.GetTicksMsec());
            RpcId(peer, MethodName.State, Snapshot.MatchId, payload);
        }
    }
    public void Step() { ObjectDisposedException.ThrowIf(_disposed, this); _solo?.Step(); _authority?.Step(); SetState(_solo?.Snapshot ?? _authority!.Snapshot()); }
    public new void Dispose()
    {
        if (_disposed) return;
        if (_authority is not null && IsInsideTree()) foreach (int peer in _admitted) RpcId(peer, MethodName.Ended, Snapshot.MatchId);
        _disposed = true; CommandAcknowledged = null; Connected = false; _pending.Clear(); _authority?.Dispose(); _solo?.Dispose();
        if (_peer is not null)
        {
            _api!.PeerDisconnected -= OnDisconnected; _api.ConnectedToServer -= OnConnected;
            _api.ConnectionFailed -= OnFailure; _api.ServerDisconnected -= OnFailure;
            if (_authority is not null && _peer is ENetMultiplayerPeer enet && enet.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Disconnected) enet.Host.Flush();
            _api.MultiplayerPeer = new OfflineMultiplayerPeer(); _peer.Close(); _peer.Dispose();
        }
        if (IsInsideTree()) { GetParent()?.RemoveChild(this); QueueFree(); }
    }
}
