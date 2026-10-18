namespace Game.Core.Tests;

// Explicit compact data for isolated timing/transition regressions. Ordinary
// strategy and network victory acceptance always use the default twenty waves.
internal static class CampaignFixture
{
    public static CampaignDefinition Three(int first = 4, int second = 6, int third = 8)
    {
        UnitType[][] roles = [[UnitType.Swordsman, UnitType.Berserker], [UnitType.Swordsman, UnitType.Berserker, UnitType.Crossbowman],
            [UnitType.Swordsman, UnitType.Berserker, UnitType.Crossbowman, UnitType.Mage]];
        int[] counts = [first, second, third];
        return new(Enumerable.Range(0, 3).Select(index => new WaveDefinition(index + 1, false,
            Enumerable.Range(0, counts[index]).Select(n => new SpawnEntry(roles[index][n % roles[index].Length], 1)).ToArray(), default)).ToArray());
    }
}
