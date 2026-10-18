namespace Game.Core;

public sealed record BattleFoodForecast(long Demand, int Available, int Paid, int[] Participating, int[] Unfed)
{
    public long FieldDemand { get; init; }
    public long StoredDemand { get; init; }
    public int[] Funded { get; init; } = [];
}
public sealed record BattleUpkeepReceipt(int Wave, int Paid, int[] Participating, int[] Unfed)
{
    public int[] Funded { get; init; } = [];
}

public static class BattleFood
{
    public static BattleFoodForecast? Project(CityState city, bool settleProduction, MarketRate? sale = null)
    {
        if (!city.Resources.TryPay(sale is null ? default : new ResourceCost().With(sale.Resource, sale.Units), out ResourceCost balance)
            || sale is not null && !balance.TryAdd(new(Gold: checked(sale.Gold + city.MarketSaleBonus)), out balance)) return null;
        if (settleProduction && (city.ProductionIncome is not ResourceCost income || !balance.TryAdd(income, out balance))) return null;
        return Forecast(city.Soldiers, balance.Food, city.RecruitmentQuotes);
    }

    public static BattleFoodForecast Forecast(IEnumerable<UnitState> soldiers, int available, EconomyConfiguration economy)
        => Forecast(soldiers.Select(u => (u.Id, u.Health, u.Faction, u.Level, u.Type, Stored: u.Assignment?.Stored == true)), available, economy.Upkeep);
    public static BattleFoodForecast Forecast(IEnumerable<UnitState> soldiers, int available, IReadOnlyList<RecruitmentQuote> quotes)
        => Forecast(soldiers.Select(u => (u.Id, u.Health, u.Faction, u.Level, u.Type, Stored: u.Assignment?.Stored == true)), available, type => quotes.First(q => q.Type == type).Upkeep);
    internal static BattleFoodForecast Forecast(IEnumerable<CombatUnit> soldiers, int available, EconomyConfiguration economy)
        => Forecast(soldiers.Select(u => (u.Id, u.Health, u.Faction, Level: u.Identity.Level, u.Type, Stored: u.Assignment?.Stored == true)), available, economy.Upkeep);
    private static BattleFoodForecast Forecast(IEnumerable<(int Id, int Health, Faction Faction, int Level, UnitType Type, bool Stored)> soldiers, int available, Func<UnitType, int> upkeep)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(available);
        var living = soldiers.Where(u => u.Health > 0 && u.Faction == Faction.Adventurers)
            .OrderBy(u => u.Stored).ThenByDescending(u => u.Level).ThenBy(u => u.Id).ToArray();
        var participants = new List<int>(); var funded = new List<int>(); var unfed = new List<int>();
        long demand = 0, fieldDemand = 0, storedDemand = 0; int remaining = available;
        foreach (var unit in living)
        {
            int cost = upkeep(unit.Type); demand = checked(demand + cost);
            if (unit.Stored) storedDemand = checked(storedDemand + cost); else fieldDemand = checked(fieldDemand + cost);
            if (remaining >= cost) { remaining -= cost; funded.Add(unit.Id); if (!unit.Stored) participants.Add(unit.Id); }
            else unfed.Add(unit.Id);
        }
        return new(demand, available, available - remaining, participants.ToArray(), unfed.ToArray())
        { FieldDemand = fieldDemand, StoredDemand = storedDemand, Funded = funded.ToArray() };
    }
}
