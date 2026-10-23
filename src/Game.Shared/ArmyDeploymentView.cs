using Game.Core;

namespace Game.Shared;

/// <summary>Read-only home/roster labels. Placement and capacity remain Core's authority.</summary>
public sealed record ArmyHomeView(ArmyHomeState Home, int BattlefieldCell, int Row, int Column,
    UnitState[] Members, int Capacity, int Participating, int Wounded, string Restriction, ResourceCost? Purchase)
{
    public bool CanDeploy => Restriction.Length == 0;
    public bool Full => Members.Length >= Capacity;
    public string Occupancy => !Home.Purchased ? "Unpurchased land" : Home.Plot.Type != Building.Empty
        ? $"{Home.Plot.Type} L{Home.Plot.Level}" : Members.Length == 0 ? "Empty" : $"{Members[0].Type} {Members.Length}/{Capacity}{(Full ? " · FULL" : "")}";
    public string Participation => Members.Length == 0 ? "" : $"{Participating} fight · {Members.Length - Participating} sit out · {Wounded} wounded";
}

public static class ArmyDeploymentView
{
    public static ArmyHomeView[] Homes(MatchSnapshot state, int owner, int selectedSquad)
    {
        var city = state.Players.Single(p => p.Id == owner);
        if (city.Army is not { } army || state.Arena is not { } arena) return [];
        var sector = arena.Sectors.Single(s => s.Owner == owner);
        var topology = new SharedArena(state.Players.Select(p => p.Id));
        var cells = arena.Board.Cells.ToDictionary(c => c.Id);
        var selected = city.Soldiers.Where(u => u.Health > 0 && u.SquadId == selectedSquad && u.Assignment is { Stored: false }).ToArray();
        var participants = state.Phase is Phase.Building or Phase.Preparation ? city.FoodForecast?.Participating : city.LastUpkeep?.Participating;
        var quote = new ArmyConfiguration(state.Rules.Army, topology.Board, army.SquadCapacity).HomeQuote(army.PurchasedHomes);
        return army.Homes.Select(home =>
        {
            int cell = sector.DeploymentCells[home.Number - 1];
            var members = city.Soldiers.Where(u => u.Health > 0 && u.Assignment is { Stored: false } a && a.Tile == home.Cell).ToArray();
            string restriction = !home.Purchased ? "Purchase this home first."
                : !topology.CanDeploy(owner, cell, army.Homes.Select(h => h.Plot).ToArray()) ? "Tower occupies this home."
                : selected.Length == 0 ? "Select a field squad."
                : members.Any(u => u.SquadId != selectedSquad) ? "Another squad occupies this home." : "";
            var coordinate = cells[cell].Coordinate;
            return new ArmyHomeView(home, cell, coordinate.R, coordinate.Column, members, army.SquadCapacity,
                members.Count(u => participants?.Contains(u.Id) == true), members.Count(u => u.Health < u.Profile.Health), restriction, home.Purchased ? null : quote);
        }).OrderBy(h => h.Row).ThenBy(h => h.Column).ToArray();
    }
    public static string SquadLabel(IEnumerable<UnitState> members, int capacity)
    {
        var group = members.ToArray();
        return group.Length == 0 ? "No field squad" : $"Home {group[0].Assignment!.Tile + 1} · {group[0].Type} {group.Length}/{capacity}{(group.Length >= capacity ? " · FULL" : "")}";
    }
    public static string Destination(CityState city, ArmyConfiguration army, UnitType type, int squad, int except = 0)
    {
        if (city.Army is not { } homes) return "No army homes available.";
        var assignment = army.Find(city.Soldiers.Where(u => u.Id != except), homes.PurchasedHomes,
            plots: homes.Homes.Select(h => h.Plot).ToArray(), type: type, squadId: squad);
        if (assignment is null) return "No compatible squad space or free purchased home.";
        int count = city.Soldiers.Count(u => u.Id != except && u.Health > 0 && u.Assignment is { Stored: false } a && a.Tile == assignment.Tile);
        return $"→ Home {assignment.Tile + 1} · {type} {count + 1}/{homes.SquadCapacity} after arrival";
    }
}
