namespace Game.Core;

public sealed record ArmySettings
{
    public int[] HomePrices { get; init; } = [5, 8, 12, 18];
}

// Persistent roster location, never a transient combat reservation or a second health owner.
public sealed record ArmyAssignment(int HallSlot, long HallGeneration, int Tile, int Anchor)
{
    public bool Stored => HallSlot >= 0;
    public HexPosition Position => new(Tile, Anchor);
}
public enum ArmyPlotStatus { Locked, OwnedEmpty, UnitOccupied, TowerOccupied }
public sealed record ArmyHomeState(int Cell, bool Purchased, int Used)
{
    public int PlotId { get; init; }
    public int Number => PlotId - ArmyConfiguration.PlotOffset + 1;
    public SlotState Plot { get; init; } = new(Building.Empty, 0) { Purchased = false };
    public ArmyPlotStatus State => !Purchased ? ArmyPlotStatus.Locked : Plot.Type != Building.Empty ? ArmyPlotStatus.TowerOccupied : Used > 0 ? ArmyPlotStatus.UnitOccupied : ArmyPlotStatus.OwnedEmpty;
}
public sealed record TownHallState(int Slot, long Generation, int CapacityLevel, int HealingLevel,
    int Capacity, int HealingPercent, ResourceCost? CapacityQuote, ResourceCost? HealingQuote);
public sealed record ArmyState(int PurchasedHomes, ArmyHomeState[] Homes, int[] HomePrices, TownHallState[] Halls)
{
    public int SquadCapacity { get; init; } = 3;
}

// One frozen quote/placement module serves authority, projections and disabled-action previews.
// Callers supply the owned roster; only living assigned soldiers claim placement.
// Health and lifetime remain in combat.
public sealed class ArmyConfiguration
{
    // City construction plots retain their existing 0..8 identities. Army plots
    // use the same command/instance lifecycle without changing city production.
    public const int PlotOffset = 9, PlotCount = 6, TotalPlots = PlotOffset + PlotCount;
    public static bool IsArmyPlot(int id) => id is >= PlotOffset and < TotalPlots;
    public static bool IsTower(Building type) => type is Building.ArrowTower or Building.CatapultTower or Building.RapidArrowTower or Building.BombardTower;
    private readonly int[] _prices;
    private readonly int[] _anchors;
    private readonly int[] _cells;
    private readonly int _squadCapacity;
    public IReadOnlyList<int> HomeCells => Array.AsReadOnly(_cells);
    public ArmyConfiguration(ArmySettings settings, HexBoard board, int squadCapacity = 3)
    {
        if (squadCapacity is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(squadCapacity));
        _squadCapacity = squadCapacity;
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.HomePrices is null || settings.HomePrices.Length != 4
            || settings.HomePrices.Any(p => p <= 0) || settings.HomePrices.Zip(settings.HomePrices.Skip(1)).Any(p => p.First >= p.Second))
            throw new ArgumentException("Four increasing positive home prices required.", nameof(settings));
        _prices = settings.HomePrices.ToArray();
        ArgumentNullException.ThrowIfNull(board);
        // Persistent home indexes are not battlefield hex identities. The
        // shared arena maps the owner's six homes to physical deployment cells.
        _cells = Enumerable.Range(0, 6).ToArray();
        _anchors = [1];
    }
    public ArmySettings Definition() => new() { HomePrices = _prices.ToArray() };
    public int[] Prices() => _prices.ToArray();
    public ResourceCost? HomeQuote(int purchased) => purchased is >= 2 and < 6 ? new ResourceCost(_prices[purchased - 2]) : null;
    public static int Capacity(int level) => level is >= 1 and <= 3 ? level * 6 : throw new ArgumentOutOfRangeException(nameof(level));
    public static int HealingPercent(int level) => level is >= 1 and <= 3 ? level * 5 : throw new ArgumentOutOfRangeException(nameof(level));
    public static ResourceCost? Upgrade(int level, bool healing) => level switch
    { 1 => new(healing ? 6 : 8, 2, Stone: 2), 2 => new(12, 2, Stone: 3), 3 => null, _ => throw new ArgumentOutOfRangeException(nameof(level)) };
    public static int HealedHealth(int health, int maximum, int percent)
    {
        if (health < 1 || maximum < health || percent is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(health));
        return checked((int)Math.Min(maximum, (long)health + ((long)maximum * percent + 99) / 100));
    }
    public ArmyAssignment? Find(IEnumerable<UnitState> soldiers, int purchased, int hallSlot = -1, long generation = 0, int capacity = 0,
        IReadOnlyList<SlotState>? plots = null, UnitType? type = null, int squadId = 0)
    {
        if (purchased is < 2 or > 6 || squadId < -1 || capacity < 0) throw new ArgumentOutOfRangeException(nameof(purchased));
        UnitState[] claims = soldiers.Where(u => u.Health > 0 && u.Assignment is not null)
            .Where(u => hallSlot < 0 ? !u.Assignment!.Stored : u.Assignment!.HallSlot == hallSlot && u.Assignment.HallGeneration == generation).ToArray();
        if (hallSlot >= 0)
        {
            // Town Hall capacity is individual soldiers, independent of squad size.
            for (int tile = 0; tile < capacity; tile++)
                if (!claims.Any(u => u.Assignment!.Tile == tile)) return new(hallSlot, generation, tile, _anchors[0]);
            return null;
        }
        int[] available = _cells.Where((_, index) => plots is null ? index < purchased : plots[index].Purchased && plots[index].Type == Building.Empty).ToArray();
        if (squadId >= 0)
            foreach (var group in claims.Where(u => available.Contains(u.Assignment!.Tile)).GroupBy(u => u.Assignment!.Tile).OrderBy(g => g.Key))
                if (group.Count() < _squadCapacity && group.All(u => (!type.HasValue || u.Type == type) && !u.IsBoss)
                    && (squadId == 0 || group.All(u => u.SquadId == squadId))) return group.First().Assignment;
        if (squadId > 0) return null;
        foreach (int tile in available)
            if (!claims.Any(u => u.Assignment!.Tile == tile)) return new(-1, 0, tile, _anchors[0]);
        return null;
    }
    public ArmyState Snapshot(IEnumerable<UnitState> soldiers, int purchased, IReadOnlyList<SlotState> slots, IReadOnlyList<SlotState>? plots = null)
    {
        UnitState[] living = soldiers.Where(u => u.Health > 0).ToArray();
        return new(purchased, _cells.Select((cell, index) =>
        {
            SlotState plot = plots?[index] ?? new SlotState(Building.Empty, 0) { Purchased = index < purchased };
            return new ArmyHomeState(cell, plot.Purchased, living.Count(u => u.Assignment is { Stored: false } a && a.Tile == cell))
            { PlotId = PlotOffset + index, Plot = plot };
        }).ToArray(), Prices(),
            slots.Select((slot, index) => (slot, index)).Where(s => s.slot.Type == Building.TownHall)
                .Select(s => new TownHallState(s.index, s.slot.Generation, s.slot.CapacityLevel, s.slot.HealingLevel,
                    Capacity(s.slot.CapacityLevel), HealingPercent(s.slot.HealingLevel), Upgrade(s.slot.CapacityLevel, false), Upgrade(s.slot.HealingLevel, true))).ToArray())
        { SquadCapacity = _squadCapacity };
    }
}
