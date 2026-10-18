namespace Game.Core;

public enum UnitLifecycle { Queued, Alive, Dying, Reserve, Stored }
public enum UnitActionKind { Waiting, Moving, Windup, Recovery }
public readonly record struct HexPosePoint(HexPosition Position, HexPosition Destination = default, int Transition = 0, int ElapsedTicks = 0, int DurationTicks = 0);
public sealed record HexUnitState(int Id, int City, Faction Faction, UnitLifecycle Lifecycle, HexPosition Position,
    UnitActionKind Action = UnitActionKind.Waiting, HexPosition Destination = default, int Transition = 0,
    long ActionSequence = 0, long StartTick = 0, long EndTick = 0, long DeathStartTick = 0, long DeathEndTick = 0, int FrozenMoveTicks = 0,
    long AdmittedTick = 0, long? FrozenTick = null, HexPosePoint? FrozenAim = null, int Size = 1)
{
    public bool HoldsTransit => Action == UnitActionKind.Moving;
    public bool IsTargetable => Lifecycle == UnitLifecycle.Alive;
}
public readonly record struct PositionReservation(int City, int Cell, int Anchor, int UnitId, long ActionSequence, Faction Faction, int Size);
public readonly record struct TransitReservation(int City, int SourceCell, int DestinationCell, int EdgeToken, int UnitId, long ActionSequence);
public sealed record CombatReservations(PositionReservation[] Positions, TransitReservation[] Transit)
{
    // Logical claims belong to squads, not cosmetic members. Source occupancy
    // stays exclusive until synchronized arrival; dying actors release immediately.
    public static CombatReservations Reconstruct(ArenaState arena, IEnumerable<SquadState> squads)
    {
        ArgumentNullException.ThrowIfNull(arena);
        return Reconstruct(new HexBoard(arena.Board), arena.Footprints, squads);
    }
    // Successive snapshots of one verified topology reuse its immutable geometry.
    public static CombatReservations Reconstruct(SharedArena arena, IEnumerable<SquadState> squads)
    {
        ArgumentNullException.ThrowIfNull(arena);
        return Reconstruct(arena.Board, arena.Footprints, squads);
    }
    private static CombatReservations Reconstruct(HexBoard board, IEnumerable<ArenaFootprint> footprints, IEnumerable<SquadState> squads)
    {
        ArgumentNullException.ThrowIfNull(squads);
        SquadState[] values = squads.OrderBy(s => s.Id).ToArray();
        if (board.Capacity != 1 || values.Any(s => s.Id <= 0 || s.Hex.Id != s.Id || s.Hex.City != 1 || s.Hex.Size != 1 || !Enum.IsDefined(s.Hex.Lifecycle)
                || !Enum.IsDefined(s.Hex.Action) || s.Hex.Faction != s.Faction || !Enum.IsDefined(s.Faction) || s.Hex.ActionSequence < 0
                || s.Hex.Lifecycle is UnitLifecycle.Queued or UnitLifecycle.Reserve or UnitLifecycle.Stored && s.Hex.HoldsTransit)
            || values.Select(s => s.Id).Distinct().Count() != values.Length)
            throw new ArgumentException("Invalid shared squad identities or rules.", nameof(squads));
        SquadState[] living = values.Where(s => s.Hex.Lifecycle == UnitLifecycle.Alive).ToArray();
        var blocked = footprints.Select(f => f.Cell).ToHashSet();
        var cells = board.Cells.Select(c => c.Id).ToHashSet();
        if (living.Select(s => s.Hex.Position.Cell).Distinct().Count() != living.Length
            || living.Any(s => !cells.Contains(s.Hex.Position.Cell) || blocked.Contains(s.Hex.Position.Cell) || s.Hex.Position.Anchor != 1))
            throw new ArgumentException("Living squads require exclusive navigable source cells.", nameof(squads));
        SquadState[] moving = living.Where(s => s.Hex.HoldsTransit).ToArray();
        if (moving.Select(s => s.Hex.Destination.Cell).Distinct().Count() != moving.Length)
            throw new ArgumentException("Squads cannot share arrival destinations.", nameof(squads));
        var byCell = living.ToDictionary(s => s.Hex.Position.Cell);
        foreach (SquadState squad in moving)
        {
            HexUnitState hex = squad.Hex;
            if (hex.StartTick < 0 || hex.EndTick <= hex.StartTick || hex.ActionSequence <= 0 || hex.Destination.Anchor != 1
                || !cells.Contains(hex.Destination.Cell) || blocked.Contains(hex.Destination.Cell)
                || !board.Cell(hex.Position.Cell).Neighbors.Contains(hex.Destination.Cell)
                || board.Transition(hex.Position, hex.Destination).Id != hex.Transition)
                throw new ArgumentException("Invalid committed squad transit.", nameof(squads));
            if (byCell.TryGetValue(hex.Destination.Cell, out SquadState? occupant)
                && (occupant.Faction != squad.Faction || !occupant.Hex.HoldsTransit || occupant.Hex.EndTick != hex.EndTick))
                throw new ArgumentException("An occupied destination requires a synchronized friendly advance.", nameof(squads));
        }
        return new(living.Select(s => new PositionReservation(1, s.Hex.Position.Cell, 1, s.Id, s.Hex.ActionSequence, s.Faction, 1)).ToArray(),
            moving.Select(s => new TransitReservation(1, s.Hex.Position.Cell, s.Hex.Destination.Cell,
                board.Transition(s.Hex.Position, s.Hex.Destination).EdgeToken, s.Id, s.Hex.ActionSequence)).ToArray());
    }
}
