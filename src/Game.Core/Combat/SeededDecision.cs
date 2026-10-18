namespace Game.Core;

public enum CombatPurpose : ulong { Target = 1, MovementRank = 2, GoalAnchor = 3, Route = 4, Formation = 5, Splash = 6 }
public enum CombatActorKind : ulong { Unit = 1, Defender = 2, Tower = 3 }
public readonly record struct CombatDecisionKey(ulong Seed, CombatFingerprint Configuration, int Wave, int City,
    CombatActorKind ActorKind, int ActorId, long Sequence, int Generation, CombatPurpose Purpose);

// Version 1's exact unsigned arithmetic/tuple order is a replay contract.
// Streams are local to a decision purpose, never globally consumed by traversal.
public static class SeededDecision
{
    public const ulong AlgorithmVersion = 1;
    private const ulong Increment = 0x9E3779B97F4A7C15;
    public static ulong Mix(ulong value)
    {
        unchecked
        {
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EB;
            return value ^ (value >> 31);
        }
    }
    public static ulong State(CombatDecisionKey key)
    {
        if (key.Wave < 1 || key.City < 1 || key.ActorId < 1 || key.Sequence < 0 || key.Generation < 0
            || !Enum.IsDefined(key.ActorKind) || !Enum.IsDefined(key.Purpose)) throw new ArgumentException("Invalid combat decision identity.", nameof(key));
        ulong state = key.Seed;
        ulong[] fields = [AlgorithmVersion, key.Configuration.A, key.Configuration.B, key.Configuration.C, key.Configuration.D,
            (ulong)key.Wave, (ulong)key.City, (ulong)key.ActorKind, (ulong)key.ActorId, (ulong)key.Sequence, (ulong)key.Generation, (ulong)key.Purpose];
        foreach (ulong field in fields) state = Mix(unchecked(state + Increment + field));
        return state;
    }
    public static ulong Rank(CombatDecisionKey key) => Next(State(key));
    public static int Choose(CombatDecisionKey key, int candidateCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(candidateCount);
        ulong bound = (ulong)candidateCount, threshold = unchecked(0UL - bound) % bound, state = State(key);
        while (true)
        {
            state = unchecked(state + Increment); ulong sample = Mix(state);
            if (sample >= threshold) return (int)(sample % bound);
        }
    }
    private static ulong Next(ulong state) => Mix(unchecked(state + Increment));
}
