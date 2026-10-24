// SPDX-License-Identifier: GPL-3.0-only
namespace Game.Core;

public sealed record DoctrineDefinition(string Id, string Name, string Crest, string Category, string Theme,
    ResourceCost StartingBonus, ResourceCost StartingPenalty,
    int WatchpostPercent = 0, int MeleePercent = 0, int MageSecondaryPercent = 0, int CrossbowRange = 0,
    int CrossbowHealthPercent = 0, int Research = 0, int MerchantSaleGold = 0, int LumbermillWood = 0,
    int FarmFood = 0, int MeleeMetalSurcharge = 0, int CrossbowMetalSurcharge = 0, int MageClothSurcharge = 0)
{
    public string Benefits => string.Join("; ", new[]
    {
        StartingBonus == default ? null : "+" + RunPresentation.Resources(StartingBonus),
        Research == 0 ? null : $"+{Research} Research",
        WatchpostPercent == 0 ? null : $"Watchpost +{WatchpostPercent}% maximum HP",
        MerchantSaleGold == 0 ? null : $"first Market sale each wave +{MerchantSaleGold} Gold (once per transaction)",
        MeleePercent == 0 ? null : $"Swordsman/Berserker +{MeleePercent}% primary damage",
        MageSecondaryPercent == 0 ? null : $"Mage +{MageSecondaryPercent}% secondary damage",
        LumbermillWood == 0 ? null : $"+{LumbermillWood} Wood with any Lumbermill per production (once per city)",
        FarmFood == 0 ? null : $"+{FarmFood} Food with any Farm per production (once per city)",
        CrossbowRange == 0 ? null : $"Crossbowmen +{CrossbowRange} range"
    }.OfType<string>());
    public string Drawback => string.Join("; ", new[]
    {
        StartingPenalty == default ? null : "Fewer starting resources: " + RunPresentation.Resources(StartingPenalty),
        MeleeMetalSurcharge == 0 ? null : $"+{MeleeMetalSurcharge} Metal per Swordsman/Berserker recruit",
        CrossbowMetalSurcharge == 0 ? null : $"+{CrossbowMetalSurcharge} Metal per Crossbowman recruit",
        MageClothSurcharge == 0 ? null : $"+{MageClothSurcharge} Cloth per Mage recruit",
        CrossbowHealthPercent == 0 ? null : $"Crossbowmen {CrossbowHealthPercent}% maximum HP"
    }.OfType<string>());
    public ResourceCost Recruitment(ResourceCost cost, UnitType type)
    {
        ResourceCost extra = type switch
        {
            UnitType.Swordsman or UnitType.Berserker => new(Metal: MeleeMetalSurcharge),
            UnitType.Crossbowman => new(Metal: CrossbowMetalSurcharge),
            UnitType.Mage => new(Cloth: MageClothSurcharge),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        if (!cost.TryAdd(extra, out ResourceCost total)) throw new OverflowException();
        return total;
    }
    public ResourceCost ProductionBonus(IEnumerable<SlotState> slots)
    {
        SlotState[] owned = slots.ToArray();
        return new(Wood: owned.Any(s => s.Type == Building.Lumbermill) ? LumbermillWood : 0,
            Food: owned.Any(s => s.Type == Building.Farm) ? FarmFood : 0);
    }
    public int MarketBonus(int lastSaleWave, int wave) => lastSaleWave != wave ? MerchantSaleGold : 0;
}
public sealed record RunState
{
    public string Doctrine { get; init; } = "warden";
    public bool Started { get; init; }
    public int DraftWave { get; init; }
    public ulong OfferSeed { get; init; }
    public string[] Offers { get; init; } = [];
    public BattleRewardQuote[] Quotes { get; init; } = [];
    public BattleRewardReceipt[] Rewards { get; init; } = [];
    public string? Choice { get; init; }
    public int MerchantWave { get; init; }
    public RunState Detach() => this with { Offers = Offers.ToArray(), Quotes = Quotes.Select(q => q.Detach()).ToArray(), Rewards = Rewards.ToArray() };
}
public static class RunCatalog
{
    public static IReadOnlyList<DoctrineDefinition> Doctrines { get; } = Array.AsReadOnly<DoctrineDefinition>(
    [
        new("warden", "The Warden", "♜", "Fortress / survival", "Stand as the old forest stands.", new(Stone: 2), new(Gold: 2), WatchpostPercent: 25),
        new("merchant", "The Merchant", "⚖", "Trade / flexibility", "Every road brings opportunity.", new(Gold: 4), new(Wood: 2), MerchantSaleGold: 1),
        new("warmaster", "The Warmaster", "⚔", "Aggressive melee", "Steel answers the darkness.", new(Metal: 4, Food: 6), default, MeleePercent: 10, MeleeMetalSurcharge: 1),
        new("arcanist", "The Arcanist", "✦", "Magic / research", "Read the secrets beneath the roots.", new(Cloth: 3, Stone: 3), new(Gold: 3), MageSecondaryPercent: 10, Research: 1),
        new("grovekeeper", "The Grovekeeper", "♧", "Sustainable economy", "Tend today what shelters tomorrow.", new(Wood: 2), default, LumbermillWood: 1, FarmFood: 1, MeleeMetalSurcharge: 1, CrossbowMetalSurcharge: 1, MageClothSurcharge: 1),
        new("huntsman", "The Huntsman", "➶", "Ranged / positioning", "Keep the enemy beyond the treeline.", new(Metal: 2), default, CrossbowRange: 1, CrossbowHealthPercent: -10)
    ]);
    public static IReadOnlyList<RewardCard> Cards => BattleRewards.Catalog;
    public static DoctrineDefinition Doctrine(string id) => Doctrines.Single(d => d.Id == id);
    public static RewardCard Card(string id) => BattleRewards.Card(id);
}

// Doctrine and Worldtree are the persistent identity routes. Consumed battle
// rewards never modify production, upkeep, recruitment, profiles or combat.
public static class RunEffects
{
    public static int Scale(int value, int percent) => checked((int)((checked((long)value * (100 + percent)) + 99) / 100));
    public static int Portion(int value, int percent) => checked((int)((checked((long)value * percent) + 99) / 100));
    public static UnitCapabilities Capabilities(UnitCapabilities c, RunState? run, UnitType type)
    {
        if (run is null || !run.Started) return c;
        DoctrineDefinition d = RunCatalog.Doctrine(run.Doctrine);
        return type switch
        {
            UnitType.Swordsman or UnitType.Berserker => c with { PrimaryPercent = d.MeleePercent },
            UnitType.Crossbowman => c with { HealthPercent = d.CrossbowHealthPercent, RangeBonus = d.CrossbowRange },
            UnitType.Mage => c with { SecondaryPercent = d.MageSecondaryPercent },
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }
    public static ResourceCost Recruitment(ResourceCost cost, RunState? run, UnitType type)
        => run is { Started: true } ? RunCatalog.Doctrine(run.Doctrine).Recruitment(cost, type) : cost;
    public static int MerchantBonus(RunState run, int wave) => run.Started ? RunCatalog.Doctrine(run.Doctrine).MarketBonus(run.MerchantWave, wave) : 0;
    public static int Watchpost(int health, RunState run) => Scale(health, run.Started ? RunCatalog.Doctrine(run.Doctrine).WatchpostPercent : 0);
    // Independent pure namespaces: no mutable combat RNG or UI enumeration.
    public static ulong Seed(ulong runSeed, ulong stream, int wave, int city = 0) => SeededDecision.Mix(runSeed ^ stream ^ ((ulong)(uint)wave << 32) ^ (uint)city);
    public const ulong RewardStream = 0x524557415244UL, EncounterStream = 0x454E434F554E5445UL;
}
