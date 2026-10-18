namespace Game.Core;

internal sealed record SquadMoveActor(int Id, Faction Faction, int Cell, int Initiative, ulong Rank);
internal sealed record SquadMoveRequest(int Id, int Destination, int Duration);
internal sealed record SquadMoveCommit(int Id, int Source, int Destination, long StartTick, long EndTick, int Batch);

// Movement admission is a single transaction over actor occupancy, not locks on
// all sources and edges. Unique destination winners form dependency chains.
// Friendly chains/cycles share an arrival tick; opposing exchanges are denied.
internal static class SquadMovement
{
    internal static SquadMoveCommit[] Admit(SharedArena arena, IEnumerable<SquadMoveActor> actors,
        IEnumerable<SquadMoveRequest> requests, IEnumerable<int> pendingDestinations, long tick)
    {
        SquadMoveActor[] ordered = actors.OrderBy(a => a.Id).ToArray();
        if (ordered.Any(a => a.Id <= 0 || !arena.CanOccupy(a.Cell) || !Enum.IsDefined(a.Faction))
            || ordered.Select(a => a.Id).Distinct().Count() != ordered.Length || ordered.Select(a => a.Cell).Distinct().Count() != ordered.Length)
            throw new ArgumentException("Living squads must have unique valid cells and identities.", nameof(actors));
        var byId = ordered.ToDictionary(a => a.Id);
        var occupancy = ordered.ToDictionary(a => a.Cell);
        var reserved = pendingDestinations.ToHashSet();
        SquadMoveRequest[] proposed = requests.ToArray();
        if (proposed.Select(r => r.Id).Distinct().Count() != proposed.Length
            || proposed.Any(r => !byId.ContainsKey(r.Id) || r.Duration <= 0 || !arena.CanOccupy(r.Destination)
                || !arena.Board.Cell(byId[r.Id].Cell).Neighbors.Contains(r.Destination)))
            throw new ArgumentException("Moves require one adjacent legal destination per actor.", nameof(requests));
        var winners = proposed.Where(r => !reserved.Contains(r.Destination)).GroupBy(r => r.Destination)
            .Select(group => group.OrderBy(r => byId[r.Id].Initiative).ThenBy(r => byId[r.Id].Rank).ThenBy(r => r.Id).First())
            .ToDictionary(r => r.Id);
        var accepted = new Dictionary<int, bool>();
        bool Accept(int id, HashSet<int> visiting)
        {
            if (accepted.TryGetValue(id, out bool prior)) return prior;
            if (!visiting.Add(id)) return true; // A same-faction cycle, checked at every dependency edge.
            SquadMoveRequest move = winners[id];
            bool valid = !occupancy.TryGetValue(move.Destination, out SquadMoveActor? occupant)
                || occupant.Faction == byId[id].Faction && winners.ContainsKey(occupant.Id) && Accept(occupant.Id, visiting);
            visiting.Remove(id); accepted[id] = valid; return valid;
        }
        foreach (int id in winners.Keys.Order()) Accept(id, []);
        int[] allowed = winners.Keys.Where(id => accepted[id]).Order().ToArray();
        var components = new List<int[]>(); var visited = new HashSet<int>();
        foreach (int id in allowed)
        {
            if (!visited.Add(id)) continue;
            var members = new List<int>(); var queue = new Queue<int>(); queue.Enqueue(id);
            while (queue.TryDequeue(out int current))
            {
                members.Add(current);
                foreach (int linked in allowed.Where(other => winners[current].Destination == byId[other].Cell || winners[other].Destination == byId[current].Cell))
                    if (visited.Add(linked)) queue.Enqueue(linked);
            }
            components.Add(members.Order().ToArray());
        }
        return components.SelectMany((component, index) =>
        {
            long end = checked(tick + component.Max(id => winners[id].Duration));
            return component.Select(id => new SquadMoveCommit(id, byId[id].Cell, winners[id].Destination, tick, end, index + 1));
        }).OrderBy(m => m.Id).ToArray();
    }
}
