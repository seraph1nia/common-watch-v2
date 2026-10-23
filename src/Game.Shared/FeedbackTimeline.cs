// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

public enum FeedbackKind { Action, Construction, Recruitment, Upgrade, Research, Rejected, Production, BattleStart, Impact, Ranged, Spell, Structure, BossArrival, WaveClear, Defeat, Victory }
public sealed record FeedbackCue(FeedbackKind Kind, string Text, int City = 0, int Slot = -1, int Cell = 0, int Unit = 0, UnitType Soldier = UnitType.Swordsman, TechnologyId Technology = TechnologyId.None)
{
    public bool Combat => Kind is FeedbackKind.Impact or FeedbackKind.Ranged or FeedbackKind.Spell or FeedbackKind.Structure or FeedbackKind.BossArrival;
    public int Priority => Kind is FeedbackKind.Defeat or FeedbackKind.Victory or FeedbackKind.BossArrival ? 5
        : Kind == FeedbackKind.BattleStart ? 4
        : Kind is FeedbackKind.WaveClear or FeedbackKind.Production ? 3
        : Kind is FeedbackKind.Rejected or FeedbackKind.Structure ? 2 : 1;
    public bool Visual => Kind is not (FeedbackKind.Impact or FeedbackKind.Ranged or FeedbackKind.Spell);
}

/// <summary>Consumes accepted identities and presented events; never calculates or submits game rules.</summary>
public sealed class FeedbackTimeline
{
    private string _match = "";
    private int _player, _production, _bossWave;
    private long _command, _event;
    private Phase _phase;
    private FeedbackCue? _outcome;
    public void Baseline(MatchSnapshot state, int player, long sequence)
    {
        _match = state.MatchId; _player = player; _command = sequence;
        BaselinePresentation(state);
    }
    public void BaselinePresentation(MatchSnapshot state)
    {
        _phase = state.Phase; _production = state.ProductionCount; _event = state.EventSequence;
        _bossWave = state.Phase == Phase.Combat ? state.Wave : 0; _outcome = null;
    }
    public FeedbackCue? Result(Command command, CommandResult result)
    {
        if (command.MatchId != _match || result.Sequence != command.Sequence || result.Sequence <= _command) return null;
        _command = result.Sequence;
        if (!result.Accepted) return new(FeedbackKind.Rejected, "Action rejected · see recent feedback", _player, command.Slot);
        if (command.City != _player) return null;
        return command.Action switch
        {
            "build" => new(FeedbackKind.Construction, "Built " + command.Building, _player, command.Slot),
            "recruit" => new(FeedbackKind.Recruitment, "Recruited " + command.SoldierType, _player, command.Slot, Soldier: command.SoldierType),
            "upgrade" or "upgrade-tower" or "upgrade-capacity" or "upgrade-healing" => new(FeedbackKind.Upgrade, "Upgrade complete", _player, command.Slot),
            "research-tech" => new(FeedbackKind.Research, "Learned " + command.Technology, _player, Technology: command.Technology),
            "ready" => new(FeedbackKind.Action, "Ready accepted", _player),
            "unready" => new(FeedbackKind.Action, "Preparing again", _player),
            "trade" => new(FeedbackKind.Action, "Trade complete", _player, command.Slot),
            "sell" => new(FeedbackKind.Action, "Building sold", _player, command.Slot),
            "buy-home" => new(FeedbackKind.Action, "Army home purchased", _player),
            "buy-plot" => new(FeedbackKind.Action, "Clearing purchased", _player, command.Slot),
            "deploy-squad" or "regroup-member" or "store" or "send" => new(FeedbackKind.Action, "Army assignment updated", _player, command.Slot, Unit: command.UnitId),
            "retire" => new(FeedbackKind.Action, "Soldier retired", _player),
            "pause" or "resume" => new(FeedbackKind.Action, command.Action == "pause" ? "Match paused" : "Match resumed", _player),
            _ when command.Action.StartsWith("reward:", StringComparison.Ordinal) => new(FeedbackKind.Research, "Reward selected", _player),
            _ => null
        };
    }
    public FeedbackCue[] State(MatchSnapshot state, bool finishing)
    {
        if (state.MatchId != _match) return [];
        var cues = new List<FeedbackCue>();
        if (state.ProductionCount > _production && !state.Players.Single(p => p.Id == _player).Eliminated)
            cues.Add(new(FeedbackKind.Production, state.ProductionCount == _production + 1 ? "Production collected" : $"Production ×{state.ProductionCount - _production}", _player));
        _production = state.ProductionCount;
        if (state.Phase != _phase)
        {
            _outcome = state.Phase switch
            {
                Phase.RewardDraft => new(FeedbackKind.WaveClear, $"Wave {state.LastRewardedWave} held"),
                Phase.Victory => new(FeedbackKind.Victory, "Victory · the watch held"),
                Phase.Defeat => new(FeedbackKind.Defeat, state.DefeatReason == DefeatReason.BattleStalled ? "Defeat · battle stalled" : "Defeat · no Keep stands"),
                _ => _outcome
            };
            _phase = state.Phase;
        }
        if (!finishing && _outcome is { } outcome) { cues.Add(outcome); _outcome = null; }
        return cues.ToArray();
    }
    public FeedbackCue? Presented(CombatEvent entry, bool paused)
    {
        if (entry.Sequence <= _event) return null;
        _event = entry.Sequence;
        if (paused) return null; // Consume paused boundaries; never burst old cues on resume.
        int city = entry.Structure?.Owner ?? (entry.Unit is { } actor ? actor.Owner > 0 ? actor.Owner : actor.Destination : _player);
        int cell = entry.Structure?.Cell ?? entry.Unit?.Hex?.Position.Cell ?? 0;
        return entry.Type switch
        {
            CombatEventType.WaveStarted => new(FeedbackKind.BattleStart, "Battle begins"),
            CombatEventType.Impact when entry.Unit?.Type == UnitType.Mage => new(FeedbackKind.Spell, "", city, Cell: cell),
            CombatEventType.Impact when entry.Unit?.Type == UnitType.Crossbowman || entry.Tower is not null => new(FeedbackKind.Ranged, "", city, Cell: cell),
            CombatEventType.DefenderShot => new(FeedbackKind.Ranged, "", city, Cell: cell),
            CombatEventType.Impact when entry.Landed && entry.Damage > 0 => new(FeedbackKind.Impact, "", city, Cell: cell),
            CombatEventType.StructureDestroyed => new(FeedbackKind.Structure, $"P{city} {entry.Structure?.Kind} fell", city, Cell: cell),
            CombatEventType.StructureHit when entry.Damage > 0 && entry.Structure?.Kind == ArenaStructureKind.Keep => new(FeedbackKind.Structure, $"P{city} Keep hit", city, Cell: cell),
            _ => null
        };
    }
    public FeedbackCue? Boss(MatchSnapshot state, IEnumerable<UnitState> presented, bool paused)
    {
        if (paused || state.Phase != Phase.Combat || _bossWave == state.Wave || !presented.Any(u => u.IsBoss && u.Hex?.Lifecycle == UnitLifecycle.Alive)) return null;
        _bossWave = state.Wave;
        return new(FeedbackKind.BossArrival, $"BOSS · wave {state.Wave}");
    }
}
