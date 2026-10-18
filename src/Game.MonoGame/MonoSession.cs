using System.Diagnostics;
using System.Text.Json;
using Game.Core;
using Game.Shared;
using LiteNetLib;

namespace Game.MonoGameClient;

/// <summary>Application-thread session. LiteNetLib callbacks run only inside PollEvents.</summary>
public sealed class MonoSession : IDisposable
{
    public const string Application = "common-watch-v2-monogame";
    public const int MaximumMessageBytes = 512 * 1024;
    private readonly EventBasedNetListener _listener = new();
    private NetManager? _net;
    private MonoConnection? _server;
    private readonly Dictionary<int, MonoConnection> _lan = [];
    private MonoSteamTransport? _steam;
    private string? _expectedMatch;
    public bool IsSteam => _steam is not null;
    private readonly Dictionary<int, MonoConnection> _admitted = [];
    private readonly Dictionary<int, (long Revision, long Sent)> _inFlight = [];
    private readonly Dictionary<long, (SessionMessage Message, long Sent)> _pending = [];
    private AuthoritySession? _authority;
    private SoloSession? _solo;
    private SessionFile? _resume;
    private readonly string _attempt = Guid.NewGuid().ToString("N");
    private readonly long _started = Environment.TickCount64;
    private long _sequence;
    private int _requestedProtocol = WireJson.ProtocolVersion;
    private bool _disposed, _admissionClosed;
    private double _publishCredit;
    public MatchSnapshot Snapshot { get; private set; } = null!;
    public bool HasState => Snapshot is not null;
    public bool Connected { get; private set; }
    public bool IsHost => _authority is not null || _solo is not null;
    public bool IsNetwork => _net is not null || _steam is not null;
    public int PlayerId { get; private set; }
    public CityState City => Snapshot.Players.Single(p => p.Id == PlayerId);
    private readonly CombatPlayback _playback = new();
    public CombatPlayback Playback => _solo?.Playback ?? _playback;
    public CommandResult? LastResult { get; private set; }
    public int SelectedSlot { get; private set; } = -1;
    public string Status { get; private set; } = "Connecting…";
    internal bool ProbeDuplicates { get; set; }
    public bool Pending => _pending.Count != 0;

    public MonoSession(SoloSession? solo = null)
    {
        if (solo is null) return;
        _solo = solo; PlayerId = solo.PlayerId; SelectedSlot = solo.SelectedSlot;
        Connected = true; Status = "Single player"; SetState(solo.Snapshot, true);
    }
    public void Host(int port)
    {
        _authority = new(AuthorityPolicy.PlayingHost, matchId: Guid.NewGuid().ToString("N"), combatSeed: SoloSession.DefaultSeed);
        Attach();
        if (!_net!.Start(port)) { Dispose(); throw new IOException("Cannot listen on LAN port."); }
        PlayerId = _authority.LocalPlayerId; Connected = true; Status = $"Hosting LAN :{port}"; SetState(_authority.Snapshot(), true);
    }
    public void Join(string host, int port, string dataPath, int protocol = WireJson.ProtocolVersion)
    {
        _requestedProtocol = protocol;
        _resume = new SessionFile(Path.Combine(dataPath, "resume.json"), $"{host}:{port}", "litenetlib", Application);
        _resume.Load(); Attach();
        if (!_net!.Start()) throw new IOException("Cannot create LAN socket.");
        _net.Connect(host, port, Application + "/1");
    }
    private void Attach()
    {
        _net = new NetManager(_listener) { AutoRecycle = true, ChannelsCount = 2, DisconnectTimeout = 7000, MaxFragmentsCount = 512 };
        _listener.ConnectionRequestEvent += request =>
        { if (!_disposed && _authority is not null && _net.ConnectedPeersCount < 8) request.AcceptIfKey(Application + "/1"); else request.Reject(); };
        _listener.PeerConnectedEvent += peer =>
        {
            var connection = new MonoConnection(peer.Id, null, (bytes, channel) => peer.Send(bytes, channel, DeliveryMethod.ReliableOrdered), () => _net.DisconnectPeer(peer));
            _lan[peer.Id] = connection; OnConnected(connection);
        };
        _listener.PeerDisconnectedEvent += (peer, _) => { if (_lan.Remove(peer.Id, out var connection)) OnDisconnected(connection); };
        _listener.NetworkReceiveEvent += (peer, reader, _, _) =>
        { if (_lan.TryGetValue(peer.Id, out var connection)) ReceiveBytes(connection, reader.GetRemainingBytes()); };
    }
    private void OnConnected(MonoConnection peer)
    {
        if (_disposed || IsHost || _resume is null) return;
        _server = peer;
        SendMessage(peer, new("admit", CoreVersion: _requestedProtocol, Attempt: _attempt, Credential: _resume.Value?.Token ?? "", Match: _expectedMatch ?? _resume.Value?.MatchId ?? ""));
    }
    private void OnDisconnected(MonoConnection peer)
    {
        if (_disposed) return;
        _admitted.Remove(peer.Id); _inFlight.Remove(peer.Id); _authority?.Disconnect(peer.Id + 2);
        if (!IsHost) { Connected = false; _pending.Clear(); Status = "Host unavailable. Reconnect or return to menu."; }
    }
    private void ReceiveBytes(MonoConnection peer, byte[] data)
    {
        if (_disposed) return;
        try
        {
            var message = SessionMessageCodec.Decode(data);
            Receive(peer, message);
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or ArgumentException or IOException or UnauthorizedAccessException)
        { peer.Close(); if (!IsHost) { Connected = false; Status = "Invalid or incompatible session data."; } }
    }
    internal void HostSteam(ulong identity, Func<ulong, bool> member)
    {
        _authority = new(AuthorityPolicy.PlayingHost, matchId: Guid.NewGuid().ToString("N"), originalHostIdentity: identity.ToString(), requireTrustedIdentity: true, combatSeed: SoloSession.DefaultSeed);
        _steam = new MonoSteamTransport(OnConnected, ReceiveBytes, OnDisconnected); _steam.Host(member);
        PlayerId = _authority.LocalPlayerId; Connected = true; Status = "Private Steam lobby"; SetState(_authority.Snapshot(), true);
    }
    internal void JoinSteam(ulong host, ulong lobby, uint appId, string match, string directory)
    {
        _expectedMatch = match;
        _resume = new SessionFile(Path.Combine(directory, "steam-resume.json"), $"{host}:{lobby}", "steam", Application + ":" + appId, host.ToString()); _resume.Load();
        _steam = new MonoSteamTransport(OnConnected, ReceiveBytes, OnDisconnected); _steam.Join(host);
    }
    private void Receive(MonoConnection peer, SessionMessage message)
    {
        if (message.Application != Application || message.Version != 1)
        { SendMessage(peer, new("refused", Payload: "Incompatible protocol version.")); return; }
        if (_authority is not null)
        {
            switch (message.Kind)
            {
                case "admit":
                    if (message.Attempt.Length != 32) return;
                    var admission = _authority.Admit(peer.Id + 2, message.CoreVersion, message.Credential,
                        (ulong)Environment.TickCount64, authenticatedIdentity: peer.Identity, expectedMatchId: message.Match.Length == 0 ? null : message.Match);
                    if (admission.Ignored) return;
                    SendMessage(peer, new("welcome", Attempt: message.Attempt, Accepted: admission.Accepted, Player: admission.PlayerId,
                        Credential: admission.Credential, Match: _authority.MatchId,
                        Payload: admission.State is null ? admission.Message : SnapshotPayload.Encode(admission.State)));
                    if (admission.Accepted) _admitted.Add(peer.Id, peer);
                    break;
                case "command":
                    if (_authority.Request(peer.Id + 2, message.Payload, (ulong)Environment.TickCount64) is { } result)
                        SendMessage(peer, new("ack", Match: _authority.MatchId, Payload: JsonSerializer.Serialize(result, WireJson.Options)));
                    break;
                case "received":
                    if (message.Match == _authority.MatchId && _inFlight.TryGetValue(peer.Id, out var flight) && flight.Revision == message.Revision)
                        _inFlight.Remove(peer.Id);
                    break;
            }
            return;
        }
        if (peer != _server) return;
        if (message.Kind == "refused") { Connected = false; Status = message.Payload; return; }
        if (message.Kind == "welcome" && !_admissionClosed && !HasState && message.Attempt == _attempt && _resume is not null)
        {
            if (!message.Accepted) { _admissionClosed = true; Connected = false; Status = message.Payload; return; }
            if (message.Credential.Length != 64) throw new InvalidDataException("Invalid private admission credential.");
            var state = SnapshotPayload.DecodeForReceiving(message.Payload);
            if (state.MatchId != message.Match || !state.Players.Any(p => p.Id == message.Player)
                || _resume.Value is { } saved && saved.MatchId != state.MatchId) throw new InvalidDataException("Stale admission.");
            _resume.Welcome(state.MatchId, message.Credential); PlayerId = message.Player;
            Connected = true; Status = "Connected"; SetState(state, true); return;
        }
        if (!HasState || message.Match != Snapshot.MatchId) return;
        switch (message.Kind)
        {
            case "ack":
                var result = JsonSerializer.Deserialize<CommandResult>(message.Payload, WireJson.Options);
                if (result is not null && _pending.Remove(result.Sequence)) LastResult = result;
                break;
            case "state" when Connected:
                var state = SnapshotPayload.DecodeForReceiving(message.Payload);
                if (state.MatchId != message.Match) throw new InvalidDataException("Stale snapshot.");
                SetState(state); SendMessage(peer, new("received", Match: state.MatchId, Revision: state.Revision)); break;
            case "ended": Connected = false; _pending.Clear(); Status = "The original host ended the game."; break;
        }
    }
    private static void SendMessage(MonoConnection peer, SessionMessage message)
    {
        byte[] data = SessionMessageCodec.Encode(message);
        peer.Send(data, message.Kind == "state" ? (byte)1 : (byte)0);
    }
    private void SetState(MatchSnapshot state, bool baseline = false)
    {
        if (HasState && (state.MatchId != Snapshot.MatchId || state.Revision < Snapshot.Revision)) return;
        bool gap = HasState && state.OldestEventSequence > Snapshot.EventSequence + 1;
        Playback.Accept(state, baseline); Snapshot = state;
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
            var message = new SessionMessage("command", Match: Snapshot.MatchId, Payload: json);
            _pending.Add(sequence, (message, Environment.TickCount64)); SendMessage(_server!, message);
            if (duplicate) SendMessage(_server!, message);
        }
    }
    public void RetryLast()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Connected || !HasState || IsHost || Pending || _resume?.Value?.LastCommand is not { } json) return;
        var command = JsonSerializer.Deserialize<Command>(json, WireJson.Options);
        if (command is null || command.MatchId != Snapshot.MatchId) return;
        LastResult = null;
        var message = new SessionMessage("command", Match: Snapshot.MatchId, Payload: json);
        _pending.Add(command.Sequence, (message, Environment.TickCount64)); SendMessage(_server!, message);
    }
    public void Start() => Send("start");
    public void ChooseReward(string id) => Send("reward:" + id);
    public void SelectSlot(int slot) { if (slot is < -1 or >= 9) throw new ArgumentOutOfRangeException(nameof(slot)); SelectedSlot = slot; _solo?.SelectSlot(slot); }
    public void Send(string action, int slot = -1, Building building = Building.Empty, UnitType soldierType = UnitType.Swordsman, ConstructionPayment payment = ConstructionPayment.Standard)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!Connected || !HasState) return;
        if (_solo is not null) { LastResult = _solo.Send(action, slot, building, soldierType, payment); SetState(_solo.Snapshot); return; }
        if (_pending.Count >= 16) { Status = "Waiting for authority acknowledgments."; return; }
        long sequence = _authority is null ? _resume!.Reserve() : ++_sequence;
        var command = Command.FromSnapshot(Snapshot, sequence, action, PlayerId, slot, building, soldierType, payment: payment);
        if (_authority is not null) { LastResult = _authority.ExecuteLocal(command); SetState(_authority.Snapshot()); }
        else
        {
            var message = new SessionMessage("command", Match: Snapshot.MatchId, Payload: JsonSerializer.Serialize(command, WireJson.Options));
            _resume!.Remember(message.Payload);
            LastResult = null; _pending.Add(sequence, (message, Environment.TickCount64)); SendMessage(_server!, message);
            if (ProbeDuplicates) SendMessage(_server!, message);
        }
    }
    public void Advance(double seconds)
    {
        if (_disposed) return;
        _net?.PollEvents(); _steam?.Poll();
        if (_solo is not null) { _solo.Advance(seconds); SetState(_solo.Snapshot); }
        if (_authority is not null) { _authority.Advance(seconds); SetState(_authority.Snapshot()); }
        if (_solo is null) Playback.Advance(seconds, Connected);
        if (IsNetwork && !_admissionClosed && !Connected && !HasState && Environment.TickCount64 - _started > 15000)
        { _admissionClosed = true; _net?.Stop(); _steam?.Dispose(); Status = "Connection/admission timed out. Return to menu."; }
        foreach (var pair in _pending.ToArray())
            if (Environment.TickCount64 - pair.Value.Sent > 1500 && Connected)
            { SendMessage(_server!, pair.Value.Message); _pending[pair.Key] = (pair.Value.Message, Environment.TickCount64); }
        _publishCredit += seconds;
        if (_authority is null || _publishCredit < .1) return;
        _publishCredit = 0;
        string? payload = null;
        foreach (var pair in _admitted.ToArray())
        {
            if (_inFlight.TryGetValue(pair.Key, out var flight))
            { if (Environment.TickCount64 - flight.Sent > 10000) pair.Value.Close(); continue; }
            payload ??= SnapshotPayload.Encode(Snapshot);
            _inFlight[pair.Key] = (Snapshot.Revision, Environment.TickCount64);
            SendMessage(pair.Value, new("state", Match: Snapshot.MatchId, Payload: payload));
        }
    }
    public void Step() { ObjectDisposedException.ThrowIf(_disposed, this); _solo?.Step(); _authority?.Step(); SetState(_solo?.Snapshot ?? _authority!.Snapshot()); }
    public void Dispose()
    {
        if (_disposed) return;
        if (_authority is not null) foreach (var peer in _admitted.Values) SendMessage(peer, new("ended", Match: Snapshot.MatchId));
        _disposed = true; Connected = false; _pending.Clear(); _net?.Stop(); _steam?.Dispose(); _authority?.Dispose(); _solo?.Dispose();
    }
}
