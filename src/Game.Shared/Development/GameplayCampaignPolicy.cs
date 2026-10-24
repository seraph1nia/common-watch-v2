// SPDX-License-Identifier: GPL-3.0-only
// Reference verification policy adapted from odot-game/tools/DevRunner/CampaignStrategy.cs.
// Development replay only: submits paid commands, never writes authoritative state.
using Game.Core;

namespace Game.Shared;

internal sealed record EconomyAction(string Action, int Slot = -1, Building Building = Building.Empty, UnitType Unit = UnitType.Swordsman,
    TechnologyId Technology = TechnologyId.None, Resource Resource = Resource.Wood, int Bundles = 0, ConstructionPayment Payment = ConstructionPayment.Standard, int UnitId = 0)
{
    public Command Command(MatchSnapshot state, int player, long sequence = 1)
    {
        _ = state.Players.Single(p => p.Id == player);
        return Game.Core.Command.FromSnapshot(state, sequence, Action, player, Slot, Building, Unit,
            Technology, Resource, Bundles, Payment, UnitId);
    }
}

// Ordinary-command verification policy shared by the cheap balance cases and
// process drivers. All prices, profiles, outputs and maxima come from snapshots.
internal static class GameplayCampaignPolicy
{
    private static readonly int[] ProducerSlots = [0, 1, 3, 4];
    private static readonly Resource[] SaleOrder = [Resource.Cloth, Resource.Metal, Resource.Food, Resource.Stone, Resource.Wood];
    private static readonly (int Slot, Building Type)[] Core = [(0, Building.Farm), (1, Building.MetalMine), (2, Building.Barracks), (3, Building.Lumbermill), (4, Building.Stonecutter)];
    // A ranged opening must earn wood for equipment, not field an empty army
    // after spending every END's wood on the generic melee producer sequence.
    private static readonly (int Slot, Building Type)[] RangedCore = [(0, Building.Farm), (1, Building.MetalMine), (3, Building.Lumbermill), (4, Building.Lumbermill), (2, Building.ArcheryRange)];
    private static readonly (int Slot, Building Type)[] Ranged = [(5, Building.Stonecutter), (6, Building.Market), (7, Building.Mine)];
    private static readonly (int Slot, Building Type)[] Frontline = [(5, Building.Market), (6, Building.Mine)];
    private static readonly (int Slot, Building Type)[] Mixed = [(8, Building.Market), (5, Building.Weaver), (6, Building.Arcanum), (7, Building.ArcheryRange)];
    private static readonly (int Slot, Building Type)[] Towers = [(8, Building.Market), (5, Building.ArrowTower), (6, Building.CatapultTower), (7, Building.ArrowTower)];
    private static readonly (int Slot, Building Type)[] Research = [(5, Building.ResearchTower), (6, Building.Market), (7, Building.Mine)];
    private static readonly (int Slot, Building Type)[] Reserve = [(5, Building.TownHall), (6, Building.Market), (7, Building.Mine)];

    private static EconomyAction? Build(CityState city, MatchSnapshot state, int slot, Building type)
    {
        BuildingDefinition definition = state.BuildingCatalog.Single(b => b.Type == type);
        if (city.Resources.TryPay(definition.Construction, out _)) return new("build", slot, type);
        if (city.Wood == 0 && definition.RecoveryConstruction is ResourceCost recovery && city.Resources.TryPay(recovery, out _))
            return new("build", slot, type, Payment: ConstructionPayment.GoldRecovery);
        return null;
    }
    internal static bool CanField(CityState city, UnitType type)
    {
        ArmyState army = city.Army ?? throw new InvalidOperationException("Authority must project army homes.");
        return army.Homes.Any(h => h.Purchased && h.Plot.Type == Building.Empty && (h.Used == 0
            || h.Used < army.SquadCapacity && city.Soldiers.Where(u => u.Health > 0 && u.Assignment is { Stored: false } a && a.Tile == h.Cell)
                .All(u => u.Type == type && !u.IsBoss)));
    }
    private static EconomyAction? Recruit(CityState city, int slot, RecruitmentQuote quote, int maximumHomes = 6)
    {
        if (!city.Resources.TryPay(quote.Cost, out _) || city.Food < (city.FoodForecast?.Demand ?? 0) + quote.Upkeep) return null;
        ArmyState army = city.Army ?? throw new InvalidOperationException("Authority must project army homes.");
        if (CanField(city, quote.Type)) return new("recruit", slot, Unit: quote.Type);
        int expansion = army.PurchasedHomes - 2;
        if (army.PurchasedHomes < maximumHomes && expansion < army.HomePrices.Length)
            return city.Gold >= army.HomePrices[expansion] ? new("buy-home") : null;
        // Bounded homes make replacement an ordinary army investment, not a hidden overflow.
        UnitState? veteran = city.Soldiers.Where(u => u.Assignment is { Stored: false } && u.Level < quote.Level)
            .OrderBy(u => u.Level).ThenBy(u => u.Health).ThenBy(u => u.Id).FirstOrDefault();
        return veteran is null ? null : new("retire", UnitId: veteran.Id);
    }
    private static EconomyAction? Rotate(CityState city, bool upgrades)
    {
        ArmyState army = city.Army!;
        foreach (TownHallState hall in army.Halls)
        {
            UnitState[] stored = city.Soldiers.Where(u => u.Assignment is { Stored: true } a && a.HallSlot == hall.Slot).ToArray();
            UnitState? recovered = stored.Where(u => (long)u.Health * 100 >= (long)u.Profile.Health * 90).OrderByDescending(u => u.Level).ThenBy(u => u.Id).FirstOrDefault();
            if (recovered is not null && CanField(city, recovered.Type)) return new("send", hall.Slot, UnitId: recovered.Id);
            if (upgrades && stored.Any(u => u.RecoveryEligible && u.Health < u.Profile.Health) && hall.HealingQuote is { } healing && city.Resources.TryPay(healing, out _))
                return new("upgrade-healing", hall.Slot);
            UnitState? wounded = city.Soldiers.Where(u => u.Assignment is { Stored: false } && u.RecoveryEligible
                && u.Level >= city.Slots[2].Level - 1 && (long)u.Health * 100 < (long)u.Profile.Health * 65)
                .OrderBy(u => u.Health / (decimal)u.Profile.Health).ThenBy(u => u.Id).FirstOrDefault();
            if (wounded is null) continue;
            if (stored.Length < hall.Capacity) return new("store", hall.Slot, UnitId: wounded.Id);
            if (upgrades && hall.CapacityQuote is { } capacity && city.Resources.TryPay(capacity, out _)) return new("upgrade-capacity", hall.Slot);
            UnitState? obsolete = stored.Where(u => u.Level < wounded.Level).OrderBy(u => u.Level).ThenBy(u => u.Id).FirstOrDefault();
            if (obsolete is not null) return new("retire", UnitId: obsolete.Id);
        }
        return null;
    }
    internal static string RewardChoice(RunState run) => run.Offers.Order(StringComparer.Ordinal).First();
    public static EconomyAction? Next(MatchSnapshot state, int player, string family = "frontline", int maximumHomes = 6, int maximumTier = 5, bool hallUpgrades = true, bool purchaseResearch = true)
    {
        if (maximumHomes is < 2 or > 6 || maximumTier is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(maximumHomes));
        CityState city = state.Players.Single(p => p.Id == player);
        if (state.Phase == Phase.RewardDraft && !city.Eliminated && city.Connected && !state.Paused && city.Run.Choice is null) return new("reward:" + RewardChoice(city.Run));
        if (city.Eliminated || !city.Connected || city.Ready || state.Phase is not (Phase.Building or Phase.Preparation) || state.Paused) return null;
        if (family == "support")
        {
            if (state.Wave == 1) return SupportOpening(state, city, maximumHomes);
            family = "frontline";
        }
        if (state.Wave == 1 && family != "ranged") return SupportOpening(state, city, maximumHomes);
        if (family == "ranged" && state.Wave <= 3) return RangedOpening(state, city, maximumHomes);
        bool Affordable(ResourceCost cost) => city.Resources.TryPay(cost, out _);
        EconomyAction? Construction(int slot, Building type)
        {
            if (city.Slots[slot].Type != Building.Empty) return null;
            if (!city.Slots[slot].Purchased)
            {
                if (city.Slots.Any(s => s.Purchased && s.Type == Building.Empty)) return null;
                int expansions = city.Slots.Count(s => s.Purchased) - 5;
                return expansions < state.PlotPrices.Length && Affordable(new(state.PlotPrices[expansions])) ? new("buy-plot", slot) : null;
            }
            return Build(city, state, slot, type);
        }
        EconomyAction? Upgrade(int slot, int target)
            => city.Slots[slot].Type != Building.Empty && city.Slots[slot].Level < target && city.Slots[slot].UpgradeQuote is ResourceCost quote && Affordable(quote)
                ? new("upgrade", slot) : null;
        if (state.Wave >= 13 && city.Stone >= 24 && city.Slots[2].Level == 5)
        {
            if (city.Slots[4].Type == Building.Stonecutter) return new("sell", 4);
            if (city.Slots[4].Type == Building.Empty && Affordable(state.BuildingCatalog.Single(b => b.Type == Building.Mine).Construction)) return new("build", 4, Building.Mine);
        }
        foreach ((int slot, Building type) in family == "ranged" ? RangedCore : Core)
            if (Construction(slot, type) is EconomyAction founding) return founding;
        int soldierTier = Math.Min(maximumTier, 1 + (state.Wave + 1) / 3);
        if (Upgrade(2, soldierTier) is EconomyAction soldierUpgrade) return soldierUpgrade;
        foreach (int slot in ProducerSlots)
            if (Upgrade(slot, 2) is EconomyAction producerUpgrade) return producerUpgrade;
        (int Slot, Building Type)[] branch = family switch { "ranged" => state.Wave == 1 ? [] : Ranged, "mixed" => Mixed, "towers" => Towers, "research" => Research, "reserve" => Reserve, _ => Frontline };
        foreach ((int slot, Building type) in branch)
        {
            // Reserve investment follows the first field expansion; it must not
            // spend the opening's gold on unused land instead of an army choice.
            if (family == "reserve" && city.Army!.PurchasedHomes < (type == Building.TownHall ? 3 : 4)) continue;
            if (Construction(slot, type) is EconomyAction expansion) return expansion;
        }
        foreach ((int slot, Building type) in branch)
        {
            BuildingDefinition definition = state.BuildingCatalog.Single(b => b.Type == type);
            int target = definition.Recruits is null ? definition.MaximumLevel : Math.Min(maximumTier, 1 + (state.Wave + 1) / 4);
            if (Upgrade(slot, target) is EconomyAction branchUpgrade) return branchUpgrade;
        }
        if (family == "reserve" && Rotate(city, hallUpgrades) is EconomyAction rotation) return rotation;
        if (family == "research" && purchaseResearch)
            foreach (TechnologyId id in new[] { TechnologyId.MeleeFoundation, TechnologyId.Guardian, TechnologyId.GuardianMastery })
                if (city.Technologies.Single(t => t.Id == id).Available) return new("research-tech", Technology: id);
        foreach ((UnitType type, int target) in Targets(family, state.Wave))
        {
            if (city.Soldiers.Count(u => u.Type == type && u.Assignment is not { Stored: true }) >= target) continue;
            int slot = Array.FindIndex(city.Slots, s => state.BuildingCatalog.SingleOrDefault(b => b.Type == s.Type)?.Recruits?.Contains(type) == true);
            if (slot < 0) continue;
            RecruitmentQuote quote = city.RecruitmentQuotes.Single(q => q.Type == type && q.Level == city.Slots[slot].Level);
            if (Recruit(city, slot, quote, maximumHomes) is EconomyAction recruitment) return recruitment;
        }
        int market = Array.FindIndex(city.Slots, s => s.Type == Building.Market);
        if (market >= 0)
        {
            foreach (Resource resource in SaleOrder)
            {
                // Gold is less restrictive now; retain equipment reserves and sell only surplus food when funded.
                if (city.Gold >= 20 && resource != Resource.Food) continue;
                int retain = resource switch { Resource.Food => checked((int)(city.FoodForecast?.Demand ?? 0) + 20), Resource.Metal or Resource.Cloth => 20, Resource.Stone => 8, _ => 10 };
                MarketRate rate = state.MarketRates.Single(r => r.Resource == resource);
                int bundles = (city.Resources.Amount(resource) - retain) / rate.Units;
                if (bundles > 0) return new("trade", market, Resource: resource, Bundles: bundles);
            }
        }
        return null;
    }
    // Two genuine settlements earn equipment before the last editable gate.
    // Sell the temporary producers for their actual half-refunds; no income
    // from final END is spendable. W2 prioritizes six L1 members over upgrades.
    private static EconomyAction? RangedOpening(MatchSnapshot state, CityState city, int maximumHomes)
    {
        if (state.Wave == 1)
        {
            if (state.Turn == 1)
            {
                foreach (var (slot, type) in new[] { (0, Building.Farm), (1, Building.MetalMine), (3, Building.Lumbermill), (4, Building.Lumbermill), (2, Building.Lumbermill) })
                    if (city.Slots[slot].Type == Building.Empty) return Build(city, state, slot, type);
            }
            if (state.Turn != 3) return null;
            foreach (int slot in new[] { 0, 1, 2 })
                if (city.Slots[slot].Type is Building.Farm or Building.MetalMine or Building.Lumbermill) return new("sell", slot);
            if (city.Slots[2].Type == Building.Empty) return Build(city, state, 2, Building.ArcheryRange);
        }
        else if (city.Slots[0].Type == Building.Empty) return Build(city, state, 0, Building.Farm);
        int target = state.Wave == 1 ? 3 : 6;
        RecruitmentQuote quote = city.RecruitmentQuotes.Single(q => q.Type == UnitType.Crossbowman && q.Level == 1);
        return city.Soldiers.Length < target ? Recruit(city, 2, quote, maximumHomes) : null;
    }
    private static EconomyAction? SupportOpening(MatchSnapshot state, CityState city, int maximumHomes)
    {
        if (state.Turn == 1)
            foreach (var (slot, type) in new[] { (0, Building.Farm), (1, Building.MetalMine), (4, Building.MetalMine), (3, Building.Lumbermill) })
                if (city.Slots[slot].Type == Building.Empty) return Build(city, state, slot, type);
        if (state.Turn != 3) return null;
        if (city.Slots[4].Type == Building.MetalMine) return new("sell", 4);
        if (city.Slots[2].Type == Building.Empty) return Build(city, state, 2, Building.Barracks);
        RecruitmentQuote quote = city.RecruitmentQuotes.Single(q => q.Type == UnitType.Swordsman && q.Level == 1);
        return city.Soldiers.Length < 6 ? Recruit(city, 2, quote, maximumHomes) : null;
    }
    private static (UnitType Type, int Target)[] Targets(string family, int wave)
    {
        if (family == "ranged") return [(UnitType.Crossbowman, wave == 1 ? 6 : wave < 5 ? 12 : 18)];
        if (wave == 1) return [(UnitType.Swordsman, 6)];
        return family switch
        {
            // Six physical homogeneous homes: three melee groups and one per
            // support/Berserker role. The former 10/2/2/2 target required seven
            // homes and could retire/refill forever while starving support.
            "mixed" => [(UnitType.Swordsman, 9), (UnitType.Mage, 3), (UnitType.Crossbowman, 3), (UnitType.Berserker, 3)],
            "towers" => [(UnitType.Swordsman, wave < 8 ? 10 : 14)],
            "research" => [(UnitType.Swordsman, 16)],
            _ => [(UnitType.Swordsman, wave < 5 ? 12 : 18)]
        };
    }
}
