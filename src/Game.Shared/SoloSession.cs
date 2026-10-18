using Game.Core;

namespace Game.Shared;

/// <summary>A single local authority. Clients submit commands and read detached snapshots.</summary>
public sealed class SoloSession : IDisposable
{
    public const ulong DefaultSeed = 20261018;
    private readonly AuthoritySession _authority;
    private long _sequence;
    private bool _disposed;
    public int PlayerId => _authority.LocalPlayerId;
    public MatchSnapshot Snapshot { get; private set; }
    public CityState City => Snapshot.Players.Single(p => p.Id == PlayerId);
    public CombatPlayback Playback { get; } = new();
    public CommandResult? LastResult { get; private set; }
    public int SelectedSlot { get; private set; } = -1;

    public SoloSession(ulong seed = DefaultSeed, string? matchId = null)
    {
        _authority = new(AuthorityPolicy.Solo, matchId: matchId ?? $"common-watch-{seed}", combatSeed: seed);
        Snapshot = _authority.Snapshot();
        Playback.Accept(Snapshot, baseline: true);
    }

    public CommandResult Start() => Send("start");
    public CommandResult ChooseReward(string id) => Send("reward:" + id);
    public void SelectSlot(int slot)
    {
        if (slot is < -1 or >= 9) throw new ArgumentOutOfRangeException(nameof(slot));
        SelectedSlot = slot;
    }
    public CommandResult Send(string action, int slot = -1, Building building = Building.Empty,
        UnitType soldierType = UnitType.Swordsman, ConstructionPayment payment = ConstructionPayment.Standard, TechnologyId technology = TechnologyId.None)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Invalid selections reach authority validation without indexing the snapshot array.
        Command command = slot is >= -1 and < ArmyConfiguration.TotalPlots
            ? Command.FromSnapshot(Snapshot, ++_sequence, action, PlayerId, slot, building, soldierType, payment: payment, technology: technology)
            : new(++_sequence, Snapshot.MatchId, Snapshot.Phase, Snapshot.TurnSerial, action, PlayerId, slot, building, soldierType, Payment: payment);
        LastResult = _authority.ExecuteLocal(command);
        Refresh();
        return LastResult;
    }
    public void Advance(double seconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _authority.Advance(seconds);
        Refresh();
        Playback.Advance(seconds, connected: true);
    }
    public void Step()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _authority.Step();
        Refresh();
        Playback.Advance(1.0 / Match.StepsPerSecond, connected: true);
    }
    private void Refresh()
    {
        Snapshot = _authority.Snapshot();
        Playback.Accept(Snapshot);
    }
    public void ResetPlayback() => Playback.Accept(Snapshot, baseline: true);
    public void Dispose() { if (_disposed) return; _disposed = true; _authority.Dispose(); }
}
