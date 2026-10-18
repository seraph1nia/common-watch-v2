namespace Game.Core;

internal sealed record CombatLocation(UnitLifecycle Lifecycle, HexPosition Position, long AdmittedTick = 0,
    long DeathStartTick = 0, long DeathEndTick = 0, int FrozenMoveTicks = 0, long? FrozenTick = null, HexPosePoint? FrozenAim = null);

// Observed intent/realized primary of a squad-owned strike, never AI or damage
// input. Its detached pose keeps a lost primary from visually retargeting during
// recovery; existing TargetId/FrozenAim snapshot fields carry the observation.
internal sealed record StrikeAim(long AttackSequence, int SquadId, int MemberId, HexPosePoint? Pose);

// This value is stored once, as an Arch component. Numerical policies receive
// immutable values; the serialized UnitState never serves as simulation input.
internal sealed record CombatUnit(UnitIdentity Identity, int Health, WeaponProfile Profile, CombatLocation Location,
    CombatAction Action, CombatDecisionState Decision)
{
    public int SquadId { get; init; }
    public int FormationSlot { get; init; }
    public int Experience { get; init; }
    public StrikeAim? StrikeAim { get; init; }
    public UnitCapabilities Capabilities { get; init; }
    public ArmyAssignment? Assignment { get; init; }
    public bool RecoveryEligible { get; init; }
    public StatusState Statuses { get; init; } = new();
    public int Id => Identity.Id;
    public UnitType Type => Identity.Type;
    public int Owner => Identity.Owner;
    public int Origin => Identity.Origin;
    public int Destination => Identity.Destination;
    public Faction Faction => Identity.Faction;
    public UnitClass Class => Catalogs.Class(Type);
    public bool Deployed => Location.Lifecycle is UnitLifecycle.Alive or UnitLifecycle.Dying;
    public bool IsTargetable => Location.Lifecycle == UnitLifecycle.Alive;
    public bool CanAct => IsTargetable && Location.FrozenTick is null;
    public bool PendingImpact => CanAct && Action is CombatAction.Windup;
    public long ReadyTick => Action.ReadyTick;
    public CombatTarget? Target => Action is CombatAction.Windup or CombatAction.Recovery ? Action.Attack?.Target
        : Decision.ObjectiveId > 0 ? new(Decision.ObjectiveCity ? CombatTargetKind.City : CombatTargetKind.Unit, Decision.ObjectiveId)
        : Action.Attack?.Target;
    public HexPosePoint? Pose(long tick)
    {
        if (!Deployed) return null;
        if (Action is not CombatAction.Moving move) return new(Location.Position);
        int duration = checked((int)(move.EndTick - move.StartTick));
        int elapsed = Location.Lifecycle == UnitLifecycle.Dying ? Location.FrozenMoveTicks
            : checked((int)Math.Clamp((Location.FrozenTick ?? tick) - move.StartTick, 0, duration));
        return new(Location.Position, move.Destination, move.Transition, elapsed, duration);
    }
}

internal static class CombatProjection
{
    internal static UnitState Detach(UnitState unit, WorkCounters? work = null)
    {
        if (unit.Decision is not { } decision) return unit with { Statuses = unit.Statuses.Detach() };
        work?.Add(WorkMetric.RouteElementsCopied, decision.Route.Length); work?.Add(WorkMetric.VisitedElementsCopied, decision.Visited.Length);
        return unit with { Statuses = unit.Statuses.Detach(), Decision = decision with { Route = decision.Route.ToArray(), Visited = decision.Visited.ToArray() } };
    }
    public static UnitState Snapshot(CombatUnit unit, long tick, WorkCounters? work = null)
    {
        work?.Add(WorkMetric.UnitProjections); work?.Add(WorkMetric.RouteElementsCopied, unit.Decision.Route.Length); work?.Add(WorkMetric.VisitedElementsCopied, unit.Decision.Visited.Length);
        CombatLocation location = unit.Location; CombatAction action = unit.Action;
        CombatAction.Moving? move = action as CombatAction.Moving;
        AttackRecord? attack = action.Attack; CombatTarget? target = unit.Target;
        return new(unit.Id, unit.Health, checked((int)Math.Max(0, action.ReadyTick - tick)), unit.Origin, unit.Destination)
        {
            Hex = new(unit.Id, unit.Destination, unit.Faction, location.Lifecycle, location.Position, action.Kind,
                move?.Destination ?? default, move?.Transition ?? 0, action.Sequence,
                move?.StartTick ?? attack?.StartTick ?? 0, move?.EndTick ?? action.ReadyTick,
                location.DeathStartTick, location.DeathEndTick, location.FrozenMoveTicks, location.AdmittedTick, location.FrozenTick, location.FrozenAim, 1),
            Decision = unit.Decision with { Route = unit.Decision.Route.ToArray(), Visited = unit.Decision.Visited.ToArray() },
            Type = unit.Type,
            SquadId = unit.SquadId,
            FormationSlot = unit.FormationSlot,
            Experience = unit.Experience,
            IsBoss = unit.Identity.IsBoss,
            Level = unit.Identity.Level,
            Faction = unit.Faction,
            Rank = unit.Identity.Rank,
            Owner = unit.Owner,
            Deployed = unit.Deployed,
            TargetId = target?.Id ?? 0,
            TargetCity = target?.Kind == CombatTargetKind.City,
            AttackSequence = action.AttackSequence,
            ActionStartTick = attack?.StartTick ?? 0,
            ImpactTick = attack?.ImpactTick ?? 0,
            ReadyTick = action.ReadyTick,
            PendingImpact = unit.PendingImpact,
            AttackLanded = attack?.Landed,
            Profile = unit.Profile,
            Capabilities = unit.Capabilities,
            Assignment = unit.Assignment,
            RecoveryEligible = unit.RecoveryEligible,
            Statuses = unit.Statuses.Detach()
        };
    }

    // Deliberate fixture/restoration boundary; live rules never read this DTO.
    public static CombatUnit Restore(UnitState state, WeaponProfile profile, CombatDecisionState decision, long tick)
    {
        HexUnitState hex = state.Hex ?? (state.Deployed ? throw new ArgumentException("Deployed fixtures require explicit hex state.", nameof(state))
            : new(state.Id, state.Destination, state.Faction, UnitLifecycle.Queued, default, Size: 1));
        if (hex.Id != state.Id || hex.City != state.Destination || hex.Faction != state.Faction || hex.Size != 1 || !Enum.IsDefined(hex.Action))
            throw new ArgumentException("Fixture identity/action mismatch.", nameof(state));
        long ready = Math.Max(state.ReadyTick, checked(tick + state.Cooldown));
        AttackRecord? attack = null;
        if (state.AttackSequence > 0 && state.TargetId > 0)
        {
            long start = state.ActionStartTick != 0 ? state.ActionStartTick : hex.StartTick;
            long impact = state.ImpactTick != 0 ? state.ImpactTick : checked(start + profile.WindupTicks);
            attack = new(new(state.TargetCity ? CombatTargetKind.City : CombatTargetKind.Unit, state.TargetId), start, impact, state.AttackLanded);
        }
        bool pending = hex.Action == UnitActionKind.Windup && hex.Lifecycle == UnitLifecycle.Alive && hex.FrozenTick is null;
        if (state.PendingImpact != pending || hex.Action == UnitActionKind.Windup && attack is null
            || hex.Action == UnitActionKind.Windup && (hex.EndTick != 0 && hex.EndTick != ready
                || state.ActionStartTick != 0 && hex.StartTick != state.ActionStartTick))
            throw new ArgumentException("A pending impact requires a matching attack action.", nameof(state));
        CombatAction action = hex.Action switch
        {
            UnitActionKind.Moving => new CombatAction.Moving(hex.ActionSequence, hex.Destination, hex.Transition, hex.StartTick, hex.EndTick,
                state.AttackSequence, ready, attack),
            UnitActionKind.Windup when attack is not null => new CombatAction.Windup(hex.ActionSequence, state.AttackSequence, ready, attack),
            UnitActionKind.Recovery => new CombatAction.Recovery(hex.ActionSequence, state.AttackSequence, ready, attack),
            _ => ready > tick ? new CombatAction.Recovery(hex.ActionSequence, state.AttackSequence, ready, attack)
                : new CombatAction.Waiting(hex.ActionSequence, state.AttackSequence, ready, attack)
        };
        return new(new(state.Id, state.Type, state.Owner, state.Origin, state.Destination, state.Faction, state.Rank, state.IsBoss, state.Level), state.Health, profile,
            new(hex.Lifecycle, hex.Position, hex.AdmittedTick, hex.DeathStartTick, hex.DeathEndTick, hex.FrozenMoveTicks, hex.FrozenTick, hex.FrozenAim), action, decision)
        { SquadId = state.SquadId, FormationSlot = state.FormationSlot, Experience = state.Experience, Capabilities = state.Capabilities, Assignment = state.Assignment, RecoveryEligible = state.RecoveryEligible, Statuses = state.Statuses.Detach() };
    }
}
