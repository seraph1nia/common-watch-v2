using Game.Core;

namespace Game.GodotClient;

/// <summary>Bounded, opt-in local pacing observations. Never contains commands, credentials or snapshots.</summary>
public sealed class PreparationObservations
{
    public sealed record Stage(int Run, int Player, int Players, int Wave, int Preparation, int Serial,
        int Decisions, int ReadyRequests, int UnreadyRequests, int Rejections, long UnobservedCommands,
        double Seconds, double ReadyWaitingSeconds, double PausedSeconds, double AwaySeconds,
        bool Partial, bool Complete)
    {
        public bool Empty => Decisions == 0;
    }
    private readonly Queue<Stage> _completed = new();
    private object? _session;
    private Stage? _current;
    private long _sequence;
    private double _last;
    private bool _ready, _paused, _away, _observedLobby;
    private int _run;
    public Stage[] Completed => _completed.ToArray();
    public Stage? Current => _current;
    public void Observe(object? session, MatchSnapshot? state, int player, bool connected,
        Command? command, CommandResult? result, double seconds)
    {
        if (seconds < _last) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (!ReferenceEquals(session, _session))
        {
            Finish(false); _session = session; _sequence = result?.Sequence ?? 0;
            _observedLobby = false; _run++; _last = seconds;
        }
        double elapsed = seconds - _last; _last = seconds;
        if (_current is { } current)
            _current = current with { Seconds = current.Seconds + elapsed,
                ReadyWaitingSeconds = current.ReadyWaitingSeconds + (_ready && !_paused && !_away ? elapsed : 0),
                PausedSeconds = current.PausedSeconds + (_paused ? elapsed : 0),
                AwaySeconds = current.AwaySeconds + (_away ? elapsed : 0) };
        if (state is null) return;
        if (state.Phase == Phase.Lobby) _observedLobby = true;
        // Count a command against its captured preparation, before closing the stage
        // whose final Ready produced the new snapshot. Retries keep their sequence.
        if (command is not null && result is not null && command.Sequence == result.Sequence && result.Sequence > _sequence)
        {
            if (_current is { } stage && command.City == player && command.TurnSerial == stage.Serial
                && command.ExpectedPhase is Phase.Building or Phase.Preparation)
            {
                bool decision = command.Action is not ("ready" or "unready" or "pause" or "resume")
                    && !command.Action.StartsWith("speed-", StringComparison.Ordinal);
                _current = stage with {
                    Decisions = stage.Decisions + (result.Accepted && decision ? 1 : 0),
                    ReadyRequests = stage.ReadyRequests + (result.Accepted && command.Action == "ready" ? 1 : 0),
                    UnreadyRequests = stage.UnreadyRequests + (result.Accepted && command.Action == "unready" ? 1 : 0),
                    Rejections = stage.Rejections + (!result.Accepted ? 1 : 0),
                    UnobservedCommands = stage.UnobservedCommands + Math.Max(0, result.Sequence - _sequence - 1) };
            }
            _sequence = result.Sequence;
        }
        bool planning = state.Phase is Phase.Building or Phase.Preparation;
        if (_current is { } active && (!planning || state.TurnSerial != active.Serial)) Finish(true);
        var city = state.Players.FirstOrDefault(p => p.Id == player);
        if (planning && city is { Eliminated: false } && _current is null)
        {
            _current = new(_run, player, state.Players.Length, state.Wave, state.Turn, state.TurnSerial,
                0, 0, 0, 0, 0, 0, 0, 0, 0, !_observedLobby, false);
            _observedLobby = true;
        }
        _ready = city?.Ready == true; _paused = state.Paused; _away = !connected || city?.Connected != true;
    }
    private void Finish(bool complete)
    {
        if (_current is null) return;
        if (_completed.Count == 96) _completed.Dequeue();
        _completed.Enqueue(_current with { Complete = complete }); _current = null;
    }
}
