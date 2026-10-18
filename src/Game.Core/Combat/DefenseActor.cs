namespace Game.Core;

internal sealed record DefenseProfile(int Damage, int WindupTicks, int RecoveryTicks);

internal readonly record struct DefenseIdentity
{
    public CombatActorKind Kind { get; }
    public int City { get; }
    public int Slot { get; }
    public int KeyId => Kind == CombatActorKind.Defender ? City : Slot + 1;
    private DefenseIdentity(CombatActorKind kind, int city, int slot)
    {
        if (city <= 0 || kind == CombatActorKind.Tower && slot is < 0 or >= ArmyConfiguration.TotalPlots) throw new ArgumentException("Invalid defense identity.");
        Kind = kind; City = city; Slot = slot;
    }
    public static DefenseIdentity Defender(int city) => new(CombatActorKind.Defender, city, 0);
    public static DefenseIdentity Tower(int city, int slot) => new(CombatActorKind.Tower, city, slot);
}

internal sealed record DefenseActor(DefenseIdentity Identity, TowerState State, DefenseProfile Profile)
{
    public void Commit(City city, TowerState state)
    {
        if (city.Id != Identity.City) throw new ArgumentException("Defense belongs to another city.", nameof(city));
        if (Identity.Kind == CombatActorKind.Defender) city.Defender = state;
        else city.Towers[Identity.Slot] = state;
    }
    public TowerState Start(int target, long tick)
    {
        CombatAction.Windup action = CombatActions.Attack(new CombatAction.Waiting(State.AttackSequence, State.AttackSequence, State.ReadyTick),
            new(CombatTargetKind.Unit, target), tick, Profile.WindupTicks, Profile.RecoveryTicks);
        return State with
        {
            AttackSequence = action.AttackSequence,
            TargetId = target,
            ActionStartTick = action.Attack!.StartTick,
            ImpactTick = action.Attack.ImpactTick,
            ReadyTick = action.ReadyTick,
            PendingImpact = true
        };
    }
}
