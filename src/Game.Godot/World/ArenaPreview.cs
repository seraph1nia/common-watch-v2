using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Read-only preparation diagram from accepted arena, homes, funding and NextWave.</summary>
public partial class ArenaPreview : Node2D
{
    private MatchSnapshot? _state;
    private HexProjection _projection = new(30);
    private readonly Dictionary<int, Vector2> _units = [];
    private readonly Dictionary<(int, ArenaStructureKind, int), Vector2> _structures = [];
    private Vector2 _center;
    public void Present(MatchSnapshot state, bool visible, float compression)
    {
        Visible = visible; _state = state; _projection = new(30, compression);
        if (!visible || state.Arena is not { } arena) return;
        var points = arena.Board.Cells.Select(c => _projection.Project(c.Coordinate)).ToArray();
        _center = new((points.Min(p => p.X) + points.Max(p => p.X)) / 2, (points.Min(p => p.Y) + points.Max(p => p.Y)) / 2);
        _units.Clear(); _structures.Clear();
        foreach (var city in state.Players)
        {
            var sector = arena.Sectors.Single(s => s.Owner == city.Id);
            foreach (var unit in city.Soldiers.Where(u => u.Assignment is { Stored: false } && city.FoodForecast?.Participating.Contains(u.Id) == true))
            {
                int cell = sector.DeploymentCells[unit.Assignment!.Tile];
                _units[unit.Id] = Cell(cell) + new Vector2((unit.FormationSlot - 1) * 13, -7);
            }
            var enemies = state.NextWave.Where(u => u.Destination == city.Id).ToArray();
            for (int i = 0; i < enemies.Length; i++)
                _units[enemies[i].Id] = Cell(sector.EnemyCells[i % sector.EnemyCells.Length]) + new Vector2((i / sector.EnemyCells.Length % 3 - 1) * 14, -12 - i / (sector.EnemyCells.Length * 3) * 18);
        }
        foreach (var structure in state.Structures) _structures[(structure.Owner, structure.Kind, structure.Slot)] = Cell(structure.Cell);
        QueueRedraw();
    }
    private Vector2 Cell(int id)
    {
        var coordinate = _state!.Arena!.Board.Cells.Single(c => c.Id == id).Coordinate;
        var p = _projection.Project(coordinate); return new Vector2(p.X, p.Y) - _center;
    }
    internal Vector2 UnitPosition(int id) => _units[id];
    public UnitState? PickUnit(Vector2 point) => _units.OrderBy(pair => pair.Value.DistanceSquaredTo(point)).FirstOrDefault(pair => pair.Value.DistanceTo(point) < 18) is { Key: not 0 } hit
        ? _state!.NextWave.Concat(_state.Players.SelectMany(p => p.Soldiers)).FirstOrDefault(u => u.Id == hit.Key) : null;
    public StructureState? PickStructure(Vector2 point) => _structures.FirstOrDefault(pair => pair.Value.DistanceTo(point) < 20) is { Key.Item1: > 0 } hit
        ? _state!.Structures.FirstOrDefault(s => (s.Owner, s.Kind, s.Slot) == hit.Key) : null;
    public override void _Draw()
    {
        if (_state?.Arena is not { } arena) return;
        foreach (var cell in arena.Board.Cells)
        {
            var corners = _projection.Corners(cell.Coordinate).Select(p => new Vector2(p.X, p.Y) - _center).ToArray();
            WorldArt.Hex(this, corners, new Color("23343d"), new Color("3b4d50"), 1);
        }
        foreach (var sector in arena.Sectors)
        {
            var city = _state.Players.Single(p => p.Id == sector.Owner);
            foreach (var home in city.Army?.Homes ?? [])
                WorldArt.Text(this, $"P{city.Id} H{home.Number}", Cell(sector.DeploymentCells[home.Number - 1]) + new Vector2(0, 23), 10, new Color("b7c8cd"));
        }
        foreach (var pair in _structures)
        {
            var structure = _state.Structures.Single(s => (s.Owner, s.Kind, s.Slot) == pair.Key);
            DrawRect(new Rect2(pair.Value - new Vector2(11, 15), new Vector2(22, 22)), new Color("a8a084"));
            WorldArt.Text(this, $"P{structure.Owner} {structure.Kind}", pair.Value + new Vector2(0, 14), 10);
        }
        foreach (var pair in _units)
        {
            var unit = _state.NextWave.Concat(_state.Players.SelectMany(p => p.Soldiers)).Single(u => u.Id == pair.Key);
            DrawCircle(pair.Value, unit.IsBoss ? 9 : 6, new Color(unit.Faction == Faction.Adventurers ? "80bbab" : "efd478"));
            WorldArt.Text(this, $"{unit.Type.ToString()[0]}{unit.Level}", pair.Value - new Vector2(0, 10), 9);
        }
        WorldArt.Text(this, "NEXT WAVE — read-only forecast · click a unit or structure", new Vector2(0, -220), 14);
    }
}
