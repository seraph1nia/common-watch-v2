namespace Game.Core;

// A balance score, not a combat predictor. One level-one Swordsman at the
// pre-rebalance 40 HP / 10 damage / 60-tick cadence is one power point.
// Actual resolved stats include level rounding, rank, boss and enemy strength.
// Primary sustained damage needs no assumed splash victim count or range bonus.
public static class WavePower
{
    public static decimal Member(WeaponProfile profile)
    {
        if (profile.Health <= 0 || profile.Damage < 0 || profile.CadenceTicks <= 0)
            throw new ArgumentException("Power requires a valid combat profile.", nameof(profile));
        return (profile.Health / (40m * HealthPoints.Scale)
            + profile.Damage * 60m / (10m * HealthPoints.Scale * profile.CadenceTicks)) / 2m;
    }

    public static decimal Total(WaveDefinition wave, CombatConfiguration combat, int players = 1)
    {
        ArgumentNullException.ThrowIfNull(wave);
        ArgumentNullException.ThrowIfNull(combat);
        if (players is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(players));
        // Allocation scales with the ORIGINAL roster, including fallen owners.
        return players * wave.Entries.Sum(entry => entry.Count * Member(combat.Unit(
            entry.Type, entry.Rank, entry.IsBoss, entry.Level, faction: Faction.Skeletons).Runtime()));
    }
}
