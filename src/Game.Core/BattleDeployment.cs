namespace Game.Core;

// Read-only combat-entry projection, not a simulator. Uses the same grouping and
// first-free entry policy as CombatSimulation; never allocates authoritative IDs,
// pays upkeep, advances a tick, or consumes a combat decision/random number.
public static class BattleDeployment
{
    internal static bool CompatibleEnemy(UnitType type, int origin, int destination, bool boss,
        UnitType otherType, int otherOrigin, int otherDestination, bool otherBoss)
        => !boss && !otherBoss && type == otherType && origin == otherOrigin && destination == otherDestination;

    internal static int Entry(IEnumerable<int> entries, ISet<int> occupied)
        => entries.FirstOrDefault(c => !occupied.Contains(c));

    public static StructureState[] Structures(MatchSnapshot state)
        => state.Structures.Select(s => state.Phase is Phase.Building or Phase.Preparation
            && s.Kind == ArenaStructureKind.Watchpost && !state.Players.Single(c => c.Id == s.Owner).Eliminated
                ? s with { Health = s.MaximumHealth, DestroyedTick = 0 } : s).ToArray();

    public static UnitState[] Preview(MatchSnapshot state)
    {
        if (state.Phase is not (Phase.Building or Phase.Preparation) || state.Arena is not { } arena) return [];
        var result = new List<UnitState>();
        foreach (CityState city in state.Players)
        {
            var participating = (city.FoodForecast?.Participating ?? []).ToHashSet();
            ArenaSector sector = arena.Sectors.Single(s => s.Owner == city.Id);
            foreach (UnitState member in city.Soldiers)
            {
                bool stored = member.Assignment is { Stored: true };
                bool field = !city.Eliminated && participating.Contains(member.Id) && member.Assignment is { Stored: false };
                int cell = member.Assignment is { Stored: false } home ? sector.DeploymentCells[home.Tile] : 0;
                result.Add(member with
                {
                    Deployed = field,
                    Hex = new(member.Id, 1, member.Faction, stored ? UnitLifecycle.Stored : field ? UnitLifecycle.Alive : UnitLifecycle.Reserve,
                        stored ? default : new(cell, 1), StartTick: state.Tick),
                    Statuses = StatusState.Empty,
                    TargetId = 0,
                    TargetCity = false,
                    TargetKind = SquadTargetKind.None,
                    PendingImpact = false,
                    AttackSequence = 0,
                    ImpactTick = 0,
                    Cooldown = 0,
                    AttackLanded = null,
                    ActionStartTick = state.Tick,
                    ReadyTick = state.Tick
                });
            }
        }
        var groups = new List<List<UnitState>>();
        foreach (UnitState member in state.NextWave)
        {
            List<UnitState>? group = groups.FirstOrDefault(g => g.Count < state.Rules.Arena.SquadCapacity
                && CompatibleEnemy(member.Type, member.Origin, member.Destination, member.IsBoss,
                    g[0].Type, g[0].Origin, g[0].Destination, g[0].IsBoss));
            if (group is null) { group = []; groups.Add(group); }
            group.Add(member);
        }
        var occupied = result.Where(u => u.Deployed).Select(u => u.Hex!.Position.Cell).ToHashSet();
        foreach (List<UnitState> group in groups)
        {
            int cell = Entry(arena.Sectors.Single(s => s.Owner == group[0].Destination).EnemyCells, occupied);
            if (cell > 0) occupied.Add(cell);
            for (int slot = 0; slot < group.Count; slot++)
            {
                UnitState member = group[slot];
                result.Add(member with
                {
                    SquadId = group[0].Id,
                    FormationSlot = slot,
                    Deployed = cell > 0,
                    Hex = new(member.Id, 1, member.Faction, cell > 0 ? UnitLifecycle.Alive : UnitLifecycle.Queued,
                        new(cell, 1), StartTick: state.Tick)
                });
            }
        }
        var byId = result.ToDictionary(u => u.Id);
        return state.Players.SelectMany(c => c.Soldiers).Concat(state.NextWave).Select(u => byId[u.Id]).ToArray();
    }
}
