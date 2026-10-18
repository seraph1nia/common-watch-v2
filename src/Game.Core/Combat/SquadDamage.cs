namespace Game.Core;

// One strike has exactly two independent channels. Primary selection is not a
// victim cap; all other pre-impact living members receive Secondary, even when
// Primary is zero. Structure damage deliberately uses neither member channel.
public readonly record struct GroupDamageProfile(int Primary, int Secondary)
{
    public void Validate()
    {
        if (Primary < 0 || Secondary < 0) throw new ArgumentOutOfRangeException(nameof(Primary), "Damage channels cannot be negative.");
    }
}
public sealed record MemberStrike(int MemberId, GroupDamageProfile Damage);
public sealed record MemberDamage(int MemberId, int Damage);
public sealed record StrikeContribution(int AttackerMemberId, int PrimaryMemberId, MemberDamage[] Damage);

public static class SquadDamage
{
    public static StrikeContribution[] Contributions(IEnumerable<MemberStrike> attackers, IEnumerable<int> livingDefenders,
        CombatDecisionKey key)
    {
        int[] defenders = livingDefenders.Order().ToArray();
        MemberStrike[] strikes = attackers.OrderBy(a => a.MemberId).ToArray();
        if (defenders.Any(id => id <= 0) || defenders.Distinct().Count() != defenders.Length
            || strikes.Any(s => s.MemberId <= 0) || strikes.Select(s => s.MemberId).Distinct().Count() != strikes.Length)
            throw new ArgumentException("Unique positive member identities required.");
        foreach (MemberStrike strike in strikes) strike.Damage.Validate();
        if (defenders.Length == 0) return [];
        return strikes.Select(strike =>
        {
            int primary = Primary(defenders, key with { ActorId = strike.MemberId });
            return new StrikeContribution(strike.MemberId, primary, defenders.Select(id => new MemberDamage(id,
                id == primary ? strike.Damage.Primary : strike.Damage.Secondary)).ToArray());
        }).ToArray();
    }
    // The same existing seeded selection projects a member's visual intent;
    // presentation never performs its own random choice or target-group AI.
    internal static int Primary(IEnumerable<int> defenders, CombatDecisionKey key)
        => defenders.OrderBy(id => SeededDecision.Mix(SeededDecision.State(key) ^ (ulong)id)).ThenBy(id => id).First();

    public static MemberDamage[] Accumulate(IEnumerable<StrikeContribution> contributions)
        => contributions.SelectMany(c => c.Damage).GroupBy(d => d.MemberId).OrderBy(g => g.Key)
            .Select(g => new MemberDamage(g.Key, checked(g.Aggregate(0, (sum, damage) => checked(sum + damage.Damage))))).ToArray();

    // Arithmetic mean of living participating members, rounded up. One chilled
    // member cannot impose its full slow on every healthy squadmate.
    public static int AggregateChill(IEnumerable<int> memberPercentages)
    {
        int[] values = memberPercentages.ToArray();
        if (values.Any(p => p is < 0 or > 50)) throw new ArgumentOutOfRangeException(nameof(memberPercentages));
        return values.Length == 0 ? 0 : checked((int)((values.Sum(p => (long)p) + values.Length - 1) / values.Length));
    }
}
