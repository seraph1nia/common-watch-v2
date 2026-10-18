namespace Game.Core;

internal enum CombatTargetKind { Unit, City, Watchpost }

internal sealed record CombatTarget
{
    public CombatTargetKind Kind { get; }
    public int Id { get; }
    public CombatTarget(CombatTargetKind kind, int id)
    {
        if (!Enum.IsDefined(kind) || id <= 0) throw new ArgumentException("Invalid combat target.");
        Kind = kind; Id = id;
    }
}

internal sealed record AttackRecord(CombatTarget Target, long StartTick, long ImpactTick, bool? Landed = null);

// Only these four variants can be constructed. Timing and target fields occur
// once; snapshot fields and reservation identities are projections of them.
internal abstract record CombatAction
{
    public long Sequence { get; }
    public long AttackSequence { get; }
    public long ReadyTick { get; }
    public AttackRecord? Attack { get; }
    public abstract UnitActionKind Kind { get; }

    private protected CombatAction(long sequence, long attackSequence, long readyTick, AttackRecord? attack)
    {
        if (sequence < 0 || attackSequence < 0 || readyTick < 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        if (attack is not null && (attack.Target is null || attackSequence <= 0 || attack.StartTick < 0 || attack.ImpactTick <= attack.StartTick || readyTick <= attack.ImpactTick))
            throw new ArgumentException("An attack requires a positive identity, windup and recovery.", nameof(attack));
        Sequence = sequence; AttackSequence = attackSequence; ReadyTick = readyTick; Attack = attack;
    }

    internal sealed record Waiting : CombatAction
    {
        public override UnitActionKind Kind => UnitActionKind.Waiting;
        public Waiting(long sequence = 0, long attackSequence = 0, long readyTick = 0, AttackRecord? previousAttack = null)
            : base(sequence, attackSequence, readyTick, previousAttack) { }
    }

    internal sealed record Moving : CombatAction
    {
        public HexPosition Destination { get; }
        public int Transition { get; }
        public long StartTick { get; }
        public long EndTick { get; }
        public override UnitActionKind Kind => UnitActionKind.Moving;
        public Moving(long sequence, HexPosition destination, int transition, long startTick, long endTick,
            long attackSequence = 0, long readyTick = 0, AttackRecord? previousAttack = null)
            : base(sequence, attackSequence, readyTick, previousAttack)
        {
            if (sequence <= 0 || destination.Cell <= 0 || destination.Anchor <= 0 || transition <= 0 || startTick < 0 || endTick <= startTick || readyTick > startTick)
                throw new ArgumentException("Invalid committed movement interval or destination.");
            Destination = destination; Transition = transition; StartTick = startTick; EndTick = endTick;
        }
    }

    internal sealed record Windup : CombatAction
    {
        public override UnitActionKind Kind => UnitActionKind.Windup;
        public Windup(long sequence, long attackSequence, long readyTick, AttackRecord attack)
            : base(sequence, attackSequence, readyTick, attack)
        {
            if (sequence <= 0 || attack is null || attack.Landed is not null) throw new ArgumentException("A windup must be unresolved.", nameof(attack));
        }
    }

    internal sealed record Recovery : CombatAction
    {
        public override UnitActionKind Kind => UnitActionKind.Recovery;
        public Recovery(long sequence, long attackSequence, long readyTick, AttackRecord? previousAttack = null)
            : base(sequence, attackSequence, readyTick, previousAttack) { }
    }
}

// Pure deadline transitions. The caller owns eligibility, reservation commits,
// health application and events, so the same rules are testable without ECS.
internal static class CombatActions
{
    public static CombatAction.Moving Move(CombatAction action, HexTransition transition, long tick, int duration)
    {
        RequireWaiting(action, tick); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration);
        return new(checked(action.Sequence + 1), transition.Destination, transition.Id, tick, checked(tick + duration),
            action.AttackSequence, action.ReadyTick, action.Attack);
    }
    public static CombatAction.Windup Attack(CombatAction action, CombatTarget target, long tick, int windup, int recovery)
    {
        RequireWaiting(action, tick);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(windup); ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recovery);
        long impact = checked(tick + windup);
        return new(checked(action.Sequence + 1), checked(action.AttackSequence + 1), checked(impact + recovery), new(target, tick, impact));
    }
    public static CombatAction.Recovery Impact(CombatAction.Windup action, long tick, bool landed)
    {
        if (tick < action.Attack!.ImpactTick) throw new InvalidOperationException("Impact is not due.");
        return new(action.Sequence, action.AttackSequence, action.ReadyTick, action.Attack with { Landed = landed });
    }
    public static CombatAction.Waiting Complete(CombatAction action, long tick)
    {
        long deadline = action is CombatAction.Moving move ? move.EndTick : action.ReadyTick;
        if (action is not (CombatAction.Moving or CombatAction.Recovery) || tick < deadline)
            throw new InvalidOperationException("Action completion is not due.");
        return new(action.Sequence, action.AttackSequence, action.ReadyTick, action.Attack);
    }
    public static CombatAction Cancel(CombatAction action, long tick)
        => action.ReadyTick > tick ? new CombatAction.Recovery(action.Sequence, action.AttackSequence, action.ReadyTick)
            : new CombatAction.Waiting(action.Sequence, action.AttackSequence, action.ReadyTick);
    private static void RequireWaiting(CombatAction action, long tick)
    {
        if (action is not CombatAction.Waiting || tick < action.ReadyTick) throw new InvalidOperationException("Actor is not ready.");
    }
}
