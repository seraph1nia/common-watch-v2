namespace Game.Core;

public enum SquadTargetKind { None, Squad, Keep, Watchpost }
public sealed record SquadState(int Id, int Owner, int Destination, Faction Faction, UnitType Type, bool IsBoss,
    int[] Members, int[] ParticipatingMembers, HexUnitState Hex, CombatDecisionState Decision)
{
    public int TargetId { get; init; }
    public SquadTargetKind TargetKind { get; init; }
    public long AttackSequence { get; init; }
    public long ImpactTick { get; init; }
    public long ReadyTick { get; init; }
    public long RallyDisruptedUntil { get; init; }
    public int DeploymentCell { get; init; }
}
public sealed record StructureState(int Owner, ArenaStructureKind Kind, int Slot, int Cell, int Health, int MaximumHealth,
    int Level = 1, long DestroyedTick = 0)
{
    public Building TowerType { get; init; } = Building.Empty;
    public bool Active { get; init; } = true;
    public bool Destroyed => Health <= 0;
}

// Actor state occurs exactly once here. Members retain only individual health,
// capabilities, assignment, participation and cosmetic death state in ECS.
internal sealed record CombatSquad(int Id, int Owner, int Destination, Faction Faction, UnitType Type, bool IsBoss,
    CombatLocation Location, CombatAction Action, CombatDecisionState Decision)
{
    public int DeploymentCell { get; init; }
    public long RallyDisruptedUntil { get; init; }
    public bool Alive => Location.Lifecycle == UnitLifecycle.Alive;
}
