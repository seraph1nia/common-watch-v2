using System.Security.Cryptography;
using System.Text.Json;

namespace Game.Core;

public enum AuthorityPolicy { Solo, PlayingHost, Dedicated }

// A welcome is private delivery data. Its printable representation excludes the credential.
public sealed class AdmissionResult(bool accepted, bool ignored, int playerId, string credential, string message, MatchSnapshot? state)
{
    public bool Accepted { get; } = accepted;
    public bool Ignored { get; } = ignored;
    public int PlayerId { get; } = playerId;
    public string Credential { get; } = credential;
    public string Message { get; } = message;
    public MatchSnapshot? State { get; } = state;
    public override string ToString() => $"Admission: {Message}; player {PlayerId}.";
}

// Used only by an authority on its application thread; guests consume snapshots.
public sealed partial class AuthoritySession : IDisposable
{
    private readonly Match _match;
    private readonly Dictionary<int, int> _bindings = [];
    private readonly Dictionary<int, string> _credentials = [];
    private readonly Dictionary<int, string?> _identities = [];
    private readonly Dictionary<int, CommandLedger> _ledgers = [];
    private readonly Dictionary<int, (ulong Second, int Count)> _rates = [];
    private readonly bool _requireTrustedIdentity;

    public AuthorityPolicy Policy { get; }
    public int LocalPlayerId { get; }
    public string MatchId => _match.Id;
    public long Revision => _match.Revision;
    public Phase Phase => _match.Phase;
    public bool Paused => _endedState?.Paused ?? _match.Paused;
    public string? OriginalHostIdentity { get; }
    public bool IsEnded { get; private set; }
    internal bool CombatReleased => _match.Combat.IsDisposed && _match.Combat.RegistryReleased && _match.Combat.Events().Length == 0;

    public AuthoritySession(AuthorityPolicy policy, Rules? rules = null, string? matchId = null,
        string? originalHostIdentity = null, bool requireTrustedIdentity = false, ulong? combatSeed = null)
    {
        if (!Enum.IsDefined(policy)) throw new ArgumentOutOfRangeException(nameof(policy));
        if (requireTrustedIdentity && string.IsNullOrEmpty(originalHostIdentity))
            throw new ArgumentException("Authenticated sessions require their original host identity.", nameof(originalHostIdentity));
        Policy = policy; OriginalHostIdentity = originalHostIdentity; _requireTrustedIdentity = requireTrustedIdentity;
        _match = new(rules, matchId, combatSeed);
        if (policy != AuthorityPolicy.Dedicated)
        {
            LocalPlayerId = _match.Join()!.Id;
            _ledgers.Add(LocalPlayerId, new());
            _identities.Add(LocalPlayerId, originalHostIdentity);
        }
    }

    private MatchSnapshot? _endedState;
    public MatchSnapshot Snapshot() => _endedState ?? _match.Snapshot();
    public void Step() { if (!IsEnded) _match.Step(); }

    private double _stepCredit;
    // Admit a bounded wall-time slice. Keep simulation ticks, ordering, damage,
    // movement, projectiles and status durations untouched. No paused debt or
    // accumulated preparation time can burst into a later battle.
    public void Advance(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (IsEnded || _match.Paused || !_match.HasPendingSteps) { _stepCredit = 0; return; }
        Phase before = _match.Phase;
        double rate = before == Phase.Combat ? CombatSpeedRates.Factor(_match.CombatSpeed) : 1;
        _stepCredit += Math.Min(seconds, .1) * Match.StepsPerSecond * rate;
        int steps = Math.Min(6, (int)Math.Floor(_stepCredit + 1e-9));
        _stepCredit -= steps;
        if (_stepCredit < 0) _stepCredit = 0;
        for (int n = 0; n < steps; n++)
        {
            _match.Step();
            if (_match.Phase != before || !_match.HasPendingSteps) { _stepCredit = 0; break; }
        }
    }

    public bool CanStart(int playerId) => !IsEnded && _match.Phase == Phase.Lobby
        && _match.Players.TryGetValue(playerId, out City? city) && city.Connected
        && (Policy == AuthorityPolicy.Dedicated ? _bindings.ContainsValue(playerId) : playerId == LocalPlayerId);

    // authenticatedIdentity comes from the native delivery peer, never from a client message.
    public AdmissionResult Admit(int peer, int version, string credential, ulong timeMsec,
        string? authenticatedIdentity = null, string? expectedMatchId = null)
    {
        AdmissionResult Refuse(string message) => new(false, false, 0, "", message, null);
        if (IsEnded) return Refuse("Session ended.");
        if (peer <= 1 || !Allowed(peer, timeMsec) || _bindings.ContainsKey(peer))
            return new(false, true, 0, "", "Connection already bound or admission rate exceeded.", null);
        if (version != WireJson.ProtocolVersion) return Refuse("Protocol version mismatch.");
        if (expectedMatchId is not null && expectedMatchId != MatchId)
            return Refuse("Invalid or expired session. Fresh join is available only in a lobby.");
        if (Policy == AuthorityPolicy.Solo) return Refuse("Single player does not admit guests.");
        if (_requireTrustedIdentity && string.IsNullOrEmpty(authenticatedIdentity))
            return Refuse("Authenticated platform identity unavailable.");
        if (credential.Length > 128) return Refuse("Invalid or expired session. Fresh join is available only in a lobby.");
        int player;
        if (credential.Length != 0)
        {
            player = _credentials.FirstOrDefault(kv => kv.Value == credential).Key;
            if (player == 0 || !_match.Players.ContainsKey(player))
                return Refuse("Invalid or expired session. Fresh join is available only in a lobby.");
            if (_identities[player] != authenticatedIdentity)
                return Refuse("Session belongs to a different authenticated identity.");
            if (_bindings.ContainsValue(player)) return Refuse("Session already connected.");
        }
        else
        {
            if (authenticatedIdentity is not null && _identities.ContainsValue(authenticatedIdentity))
                return Refuse("Existing player must resume its session.");
            City? city = _match.Join();
            if (city is null) return Refuse("Roster locked or lobby full. Resume an existing session.");
            player = city.Id;
            _credentials.Add(player, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)));
            _identities.Add(player, authenticatedIdentity);
            _ledgers.Add(player, new());
        }
        _bindings.Add(peer, player);
        _match.SetConnected(player, true);
        return new(true, false, player, _credentials[player], "Joined.", Snapshot());
    }

    public int? Disconnect(int peer)
    {
        _rates.Remove(peer);
        if (!_bindings.Remove(peer, out int player)) return null;
        _match.SetConnected(player, false);
        return player;
    }

    public CommandResult? Request(int peer, string json, ulong timeMsec)
    {
        if (IsEnded || !_bindings.TryGetValue(peer, out int player)) return null;
        if (!Allowed(peer, timeMsec)) return new(0, false, "Command rate exceeded.");
        return DecodeAndExecute(player, json);
    }

    public CommandResult ExecuteLocal(Command command) => LocalPlayerId == 0
        ? new(command.Sequence, false, "Unauthenticated player.") : Execute(LocalPlayerId, command);

    public CommandResult RequestLocal(string json) => LocalPlayerId == 0
        ? new(0, false, "Unauthenticated player.") : DecodeAndExecute(LocalPlayerId, json);

    private CommandResult DecodeAndExecute(int player, string json)
    {
        if (json.Length > 2048) return new(0, false, "Command too large.");
        try
        {
            Command? command = JsonSerializer.Deserialize<Command>(json, WireJson.Options);
            return command is null ? new(0, false, "Malformed command.") : Execute(player, command);
        }
        catch (JsonException) { return new(0, false, "Malformed command."); }
    }

    private CommandResult Execute(int player, Command command)
    {
        if (IsEnded) return new(command.Sequence, false, "Session ended.");
        // A packet from another session must not consume this player's new ledger sequence.
        if (command.MatchId != MatchId) return new(command.Sequence, false, "Stale match, phase or turn.");
        return _ledgers[player].Execute(command, () =>
        {
            if (Policy == AuthorityPolicy.PlayingHost && player != LocalPlayerId
                && (command.Action == "start" || CombatSpeedRates.TryAction(command.Action, out _)))
                return new(command.Sequence, false, "Only the original host can start the match or change combat speed.");
            // Pausing/resuming discards fractional wall-time debt, even if both
            // commands arrive between two engine physics callbacks.
            CommandResult result = _match.Apply(player, command);
            if (result.Accepted && command.Action is "pause" or "resume") _stepCredit = 0;
            return result;
        });
    }

    private bool Allowed(int peer, ulong timeMsec)
    {
        ulong second = timeMsec / 1000;
        var rate = _rates.GetValueOrDefault(peer);
        rate = rate.Second == second ? (second, rate.Count + 1) : (second, 1);
        _rates[peer] = rate;
        return rate.Count <= 64;
    }

    public void Dispose() { End(); GC.SuppressFinalize(this); }

    public void End()
    {
        if (IsEnded) return;
        _endedState = _match.Snapshot(); _match.Dispose(); IsEnded = true;
        _bindings.Clear(); _credentials.Clear(); _identities.Clear(); _ledgers.Clear(); _rates.Clear();
    }
}
