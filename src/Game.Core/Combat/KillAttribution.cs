namespace Game.Core;

public static class KillAttribution
{
    // Simultaneous contributions share actual applied health proportionally.
    // Largest remainders receive the remaining integer hundredths, stable ID
    // breaking ties. This avoids crediting overkill as applied damage.
    public static int Winner(int applied, IEnumerable<(int Attacker, int Damage)> contributions)
    {
        var hits = contributions.Where(c => c.Damage > 0).GroupBy(c => c.Attacker).OrderBy(g => g.Key)
            .Select(g => (Id: g.Key, Damage: g.Sum(c => (long)c.Damage))).ToArray();
        long total = hits.Sum(c => c.Damage);
        if (total == 0 || applied <= 0) return 0;
        var shares = hits.Select(h => (h.Id, Applied: checked((int)(h.Damage * applied / total)), Remainder: h.Damage * applied % total)).ToArray();
        int left = applied - shares.Sum(s => s.Applied);
        var extras = shares.OrderByDescending(s => s.Remainder).ThenBy(s => s.Id).Take(left).Select(s => s.Id).ToHashSet();
        return shares.OrderByDescending(s => s.Applied + (extras.Contains(s.Id) ? 1 : 0)).ThenBy(s => s.Id).First().Id;
    }
}
