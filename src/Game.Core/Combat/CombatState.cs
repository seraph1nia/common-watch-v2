namespace Game.Core;

public enum DefeatReason { None, AllCitiesFallen, BattleStalled }
public enum BattleLimit { NoHealthProgress, WaveDuration }
public sealed record EngagementProgress(int City, long StartedTick, long LastHealthProgressTick, long Deadline);
public sealed record StallDiagnostic(BattleLimit Limit, long Tick, int? City = null, long? LastHealthProgressTick = null);

public enum UnitType { Swordsman, Crossbowman, Berserker, Mage }
public enum CombatEventType { AttackStarted, Impact, Hit, Death, DefenderShot, SquadDeath, StructureHit, StructureDestroyed, RallyDisrupted, RallyRestored, WaveStarted }
[System.Text.Json.Serialization.JsonUnmappedMemberHandling(System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow)]
public readonly record struct WeaponProfile(int Health, int Damage, int HexRange, int MoveTicks, int WindupTicks, int CadenceTicks)
{
    public int SecondaryDamage { get; init; }
    public int StructureDamage { get; init; }
    public int Initiative { get; init; }
    public int DeathTicks { get; init; }
}
public sealed record CombatEvent(long Sequence, long Tick, CombatEventType Type, UnitState? Unit, int TargetId = 0, bool TargetCity = false, int Damage = 0, bool Landed = false)
{
    public int SquadId { get; init; }
    public StructureState? Structure { get; init; }
    public TowerState? Tower { get; init; }
    public HexPosePoint? ImpactPose { get; init; }
    public int[] Victims { get; init; } = [];
    public PeriodicContribution[] Periodic { get; init; } = [];
}

internal readonly record struct UnitIdentity(int Id, UnitType Type, int Owner, int Origin, int Destination, Faction Faction, int Rank, bool IsBoss = false, int Level = 1);
public sealed record CombatDecisionState(long Sequence, int Generation, ulong SchedulingRank, int ObjectiveId = 0, bool ObjectiveCity = false,
    int ObjectiveCell = 0, long ObservedRevision = -1, long RetryTick = 0)
{
    public HexPosition[] Route { get; init; } = [];
    public int[] Visited { get; init; } = [];
}
