using System.Collections.ObjectModel;

namespace Game.Core;

// Immutable breadth-first geometry, shared by board validation/distance queries.
// Actor occupancy and coordinated moves are owned by SharedArena/SquadMovement.
internal sealed class HexReachability
{
    public IReadOnlyDictionary<int, int> Distances { get; }
    public IReadOnlyDictionary<int, ReadOnlyCollection<int>> Parents { get; }
    public HexReachability(HexBoard board, int source, Func<int, bool> allows, WorkCounters? work = null)
    {
        work?.Add(WorkMetric.BfsSearches);
        var distances = new Dictionary<int, int> { [source] = 0 };
        var parents = new Dictionary<int, List<int>>(); var queue = new Queue<int>(); queue.Enqueue(source);
        while (queue.TryDequeue(out int cell))
        {
            work?.Add(WorkMetric.BfsDequeues);
            int steps = distances[cell] + 1;
            foreach (int next in board.Cell(cell).Neighbors)
            {
                work?.Add(WorkMetric.BfsEdges);
                if (!allows(next)) continue;
                if (!distances.TryGetValue(next, out int existing))
                { distances.Add(next, steps); parents.Add(next, [cell]); queue.Enqueue(next); }
                else if (existing == steps) parents[next].Add(cell);
            }
        }
        Distances = new ReadOnlyDictionary<int, int>(distances);
        Parents = new ReadOnlyDictionary<int, ReadOnlyCollection<int>>(parents.ToDictionary(p => p.Key, p => Array.AsReadOnly(p.Value.ToArray())));
    }
}
