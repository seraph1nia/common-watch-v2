using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace Game.Core;

public enum DeploymentAffinity { Neutral, Adventurers, Skeletons }
public readonly record struct HexCoordinate(int Q, int R)
{
    public int Column => Q + (R - (R & 1)) / 2;
    public static HexCoordinate FromOffset(int column, int row) => new(column - (row - (row & 1)) / 2, row);
    public int Distance(HexCoordinate other)
    {
        long q = (long)Q - other.Q, r = (long)R - other.R;
        return checked((int)((Math.Abs(q) + Math.Abs(r) + Math.Abs(q + r)) / 2));
    }
}

// Authoring DTOs are copied/validated by HexBoard; arrays never become live rules.
public sealed record HexCellDefinition(int Id, HexCoordinate Coordinate, DeploymentAffinity Affinity, int[] Neighbors);
public sealed record HexAnchor(int Id, int AnchorX, int AnchorForward);
public sealed record HexEntryDefinition(Faction Faction, int[] RearCells, int[] FrontCells);
public sealed record HexBoardDefinition(string Id, int Version, int Capacity, HexCellDefinition[] Cells,
    HexAnchor[] Anchors, HexEntryDefinition[] Entries, int[] SiegeCells)
{
    public static HexBoardDefinition Default() => new SharedArena([1]).Board.Definition();
}

public sealed class HexCell
{
    public int Id { get; }
    public HexCoordinate Coordinate { get; }
    public DeploymentAffinity Affinity { get; }
    public ReadOnlyCollection<int> Neighbors { get; }
    internal HexCell(HexCellDefinition definition)
    { Id = definition.Id; Coordinate = definition.Coordinate; Affinity = definition.Affinity; Neighbors = Array.AsReadOnly(definition.Neighbors.Order().ToArray()); }
}
public readonly record struct HexPosition(int Cell, int Anchor);
public readonly record struct HexTransition(int Id, HexPosition Source, HexPosition Destination, int EdgeToken);

// Immutable graph/anchor catalog and precomputed queries. The city anchor is
// a distance endpoint, deliberately absent from adjacency and transit routes.
public sealed class HexBoard
{
    private readonly FrozenDictionary<int, HexCell> _cells;
    private readonly FrozenDictionary<int, HexAnchor> _anchors;
    private readonly FrozenDictionary<(int Source, int Destination), int> _distances;
    private readonly FrozenDictionary<(HexPosition Source, HexPosition Destination), HexTransition> _transitions;
    private readonly FrozenDictionary<Faction, (ReadOnlyCollection<int> Rear, ReadOnlyCollection<int> Front)> _entries;
    private readonly FrozenDictionary<int, int> _cityDistances;
    private readonly HexBoardDefinition _definition;
    public string Id { get; }
    public int Version { get; }
    public int Capacity { get; }
    public int Diameter { get; }
    public ReadOnlyCollection<HexCell> Cells { get; }
    public ReadOnlyCollection<HexAnchor> Anchors { get; }
    public ReadOnlyCollection<int> SiegeCells { get; }
    public ReadOnlyCollection<HexTransition> Transitions { get; }

    public HexBoard(HexBoardDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Cells is null || definition.Anchors is null || definition.Entries is null || definition.SiegeCells is null)
            throw new ArgumentException("Missing board data.", nameof(definition));
        if (string.IsNullOrWhiteSpace(definition.Id) || definition.Version < 1 || definition.Capacity is not (1 or 6)
            || definition.Cells.Length is < 2 or > 256 || definition.Anchors.Length != definition.Capacity)
            throw new ArgumentException("Invalid board identity or bounded capacity/topology.", nameof(definition));
        Id = definition.Id; Version = definition.Version; Capacity = definition.Capacity;
        if (definition.Cells.Any(c => c is null || c.Neighbors is null || c.Id <= 0 || !Enum.IsDefined(c.Affinity)
            || Math.Abs((long)c.Coordinate.Q) > 10000 || Math.Abs((long)c.Coordinate.R) > 10000
            || c.Neighbors.Length > 6 || c.Neighbors.Distinct().Count() != c.Neighbors.Length)
            || definition.Cells.Select(c => c.Id).Distinct().Count() != definition.Cells.Length
            || definition.Cells.Select(c => c.Coordinate).Distinct().Count() != definition.Cells.Length)
            throw new ArgumentException("Invalid or duplicate cells/neighbors.", nameof(definition));
        Cells = Array.AsReadOnly(definition.Cells.OrderBy(c => c.Id).Select(c => new HexCell(c)).ToArray());
        _cells = Cells.ToFrozenDictionary(c => c.Id);
        foreach (HexCell cell in Cells)
            foreach (int neighbor in cell.Neighbors)
                if (!_cells.TryGetValue(neighbor, out HexCell? other) || cell.Coordinate.Distance(other.Coordinate) != 1 || !other.Neighbors.Contains(cell.Id))
                    throw new ArgumentException("Neighbors must be known adjacent axial cells with reciprocal links.", nameof(definition));
        var distances = new Dictionary<(int, int), int>();
        foreach (HexCell source in Cells)
        {
            var queue = new Queue<int>(); queue.Enqueue(source.Id); distances[(source.Id, source.Id)] = 0;
            while (queue.TryDequeue(out int current))
                foreach (int next in Cell(current).Neighbors)
                    if (distances.TryAdd((source.Id, next), distances[(source.Id, current)] + 1)) queue.Enqueue(next);
            if (Cells.Any(c => !distances.ContainsKey((source.Id, c.Id))))
                throw new ArgumentException("Combat board must be connected.", nameof(definition));
        }
        _distances = distances.ToFrozenDictionary(); Diameter = distances.Values.Max();
        if (definition.Anchors.Any(f => f is null || f.Id <= 0 || Math.Abs((long)f.AnchorX) > 10000 || Math.Abs((long)f.AnchorForward) > 10000)
            || definition.Anchors.Select(f => f.Id).Distinct().Count() != definition.Capacity
            || definition.Anchors.Select(f => (f.AnchorX, f.AnchorForward)).Distinct().Count() != definition.Capacity)
            throw new ArgumentException("Distinct bounded render anchors matching board capacity are required.", nameof(definition));
        Anchors = Array.AsReadOnly(definition.Anchors.OrderBy(f => f.Id).ToArray());
        _anchors = Anchors.ToFrozenDictionary(f => f.Id);
        if (definition.Entries.Length != 2 || definition.Entries.Any(e => e is null || e.RearCells is null || e.FrontCells is null)
            || definition.Entries.Select(e => e.Faction).Distinct().Count() != 2)
            throw new ArgumentException("Both factions need one entry definition.", nameof(definition));
        var entries = new Dictionary<Faction, (ReadOnlyCollection<int>, ReadOnlyCollection<int>)>();
        foreach (HexEntryDefinition entry in definition.Entries)
        {
            if (!Enum.IsDefined(entry.Faction) || entry.RearCells.Length == 0 || entry.FrontCells.Length == 0
                || entry.RearCells.Concat(entry.FrontCells).Distinct().Count() != entry.RearCells.Length + entry.FrontCells.Length
                || entry.RearCells.Any(id => !_cells.ContainsKey(id) || Cell(id).Affinity != Affinity(entry.Faction))
                || entry.FrontCells.Any(id => !_cells.ContainsKey(id) || Cell(id).Affinity != DeploymentAffinity.Neutral)
                || entry.RearCells.Any(id => !Cell(id).Neighbors.Any(entry.FrontCells.Contains)))
                throw new ArgumentException("Protected rear entries must connect to their neutral front band.", nameof(definition));
            entries.Add(entry.Faction, (Array.AsReadOnly(entry.RearCells.ToArray()), Array.AsReadOnly(entry.FrontCells.ToArray())));
        }
        _entries = entries.ToFrozenDictionary();
        if (definition.SiegeCells.Length == 0 || definition.SiegeCells.Distinct().Count() != definition.SiegeCells.Length
            || definition.SiegeCells.Any(id => !_cells.ContainsKey(id) || Cell(id).Affinity != DeploymentAffinity.Neutral))
            throw new ArgumentException("City anchor requires neutral siege links.", nameof(definition));
        SiegeCells = Array.AsReadOnly(definition.SiegeCells.Order().ToArray());
        _cityDistances = Cells.ToFrozenDictionary(c => c.Id, c => SiegeCells.Min(cell => Distance(c.Id, cell)) + 1);
        foreach (Faction faction in Enum.GetValues<Faction>())
            foreach (int rear in Rear(faction))
            {
                var reachable = new HashSet<int> { rear }; var queue = new Queue<int>(); queue.Enqueue(rear);
                while (queue.TryDequeue(out int current))
                    foreach (int next in Cell(current).Neighbors)
                        if (Allows(faction, next) && reachable.Add(next)) queue.Enqueue(next);
                if (!Front(faction).Any(reachable.Contains) || !SiegeCells.Any(reachable.Contains))
                    throw new ArgumentException("Each protected entry needs a legal frontier and city approach.", nameof(definition));
            }
        long transitionCount = Cells.Sum(c => c.Neighbors.Count) * (long)Anchors.Count * Anchors.Count;
        if (transitionCount > 500000) throw new ArgumentException("Board has too many anchor transitions.", nameof(definition));
        var transitions = new List<HexTransition>(); var edges = new Dictionary<(int, int), int>();
        foreach (HexCell source in Cells)
            foreach (int next in source.Neighbors)
            {
                var edge = (Math.Min(source.Id, next), Math.Max(source.Id, next));
                if (!edges.TryGetValue(edge, out int token)) edges.Add(edge, token = edges.Count + 1);
                foreach (HexAnchor from in Anchors)
                    foreach (HexAnchor to in Anchors)
                        transitions.Add(new(transitions.Count + 1, new(source.Id, from.Id), new(next, to.Id), token));
            }
        Transitions = Array.AsReadOnly(transitions.ToArray());
        _transitions = transitions.ToFrozenDictionary(t => (t.Source, t.Destination));
        _definition = BuildDefinition();
    }

    public HexCell Cell(int id) => _cells[id];
    public HexAnchor Anchor(int id) => _anchors[id];
    public int Distance(int source, int destination) => _distances[(source, destination)];
    public int CityDistance(int source) => _cityDistances[source];
    public bool Allows(Faction faction, int cell) => Capacity == 1 || Cell(cell).Affinity is DeploymentAffinity.Neutral || Cell(cell).Affinity == Affinity(faction);
    public ReadOnlyCollection<int> Rear(Faction faction) => _entries[faction].Rear;
    public ReadOnlyCollection<int> Front(Faction faction) => _entries[faction].Front;
    public HexTransition Transition(HexPosition source, HexPosition destination) => _transitions[(source, destination)];
    public HexBoardDefinition Definition() => _definition with
    {
        Cells = _definition.Cells.Select(c => c with { Neighbors = c.Neighbors.ToArray() }).ToArray(),
        Anchors = _definition.Anchors.ToArray(),
        Entries = _definition.Entries.Select(e => e with { RearCells = e.RearCells.ToArray(), FrontCells = e.FrontCells.ToArray() }).ToArray(),
        SiegeCells = _definition.SiegeCells.ToArray()
    };
    private HexBoardDefinition BuildDefinition() => new(Id, Version, Capacity,
        Cells.Select(c => new HexCellDefinition(c.Id, c.Coordinate, c.Affinity, c.Neighbors.ToArray())).ToArray(), Anchors.ToArray(),
        Enum.GetValues<Faction>().Select(f => new HexEntryDefinition(f, Rear(f).ToArray(), Front(f).ToArray())).ToArray(), SiegeCells.ToArray());
    private static DeploymentAffinity Affinity(Faction faction) => faction switch
    { Faction.Adventurers => DeploymentAffinity.Adventurers, Faction.Skeletons => DeploymentAffinity.Skeletons, _ => throw new ArgumentOutOfRangeException(nameof(faction)) };
}
