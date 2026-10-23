using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Read-only preparation diagram from accepted arena, homes, funding and NextWave.</summary>
public partial class ArenaPreview : Node2D
{
    private AssetLoader _assets = null!;
    private EnvironmentView _environmentView = null!;
    private EnvironmentController _environment = null!;
    private readonly ArenaScenery _scenery = new();
    public void Initialize(AssetLoader assets, EnvironmentController environment) { _assets = assets; AddChild(_scenery); _environment = environment; _environmentView = environment.Attach(this, EnvironmentScene.Arena); _environmentView.ShowBehindParent = true; }
    private MatchSnapshot? _state;
    public Rect2 RenderBounds => ArenaPresentation.RenderBounds(_state!.Arena!, _projection, _assets.Options.Mode == RenderMode.Sprites);
    private HexProjection _projection = new(30);
    private readonly Dictionary<int, Vector2> _units = [];
    private readonly Dictionary<(int, ArenaStructureKind, int), Vector2> _structures = [];
    private Vector2 _center;
    private UnitState[] _previewUnits = [];
    private StructureState[] _previewStructures = [];
    public void Present(MatchSnapshot state, bool visible, float compression)
    {
        Visible = visible; _state = state; _projection = new(30, compression);
        _scenery.AnimatedEnvironment = _environment.Settings.Quality != EnvironmentQuality.Off;
        if (visible && state.Arena is { } layoutArena) _environmentView.SetArena(new ArenaLayout(layoutArena, _projection));
        _scenery.Present(_assets, visible ? state.Arena : null, _projection);
        if (!visible || state.Arena is not { } arena) return;
        var points = arena.Board.Cells.Select(c => _projection.Project(c.Coordinate)).ToArray();
        _center = new((points.Min(p => p.X) + points.Max(p => p.X)) / 2, (points.Min(p => p.Y) + points.Max(p => p.Y)) / 2);
        _units.Clear(); _structures.Clear();
        _previewUnits = BattleDeployment.Preview(state).Where(u => u.Deployed).ToArray();
        _previewStructures = BattleDeployment.Structures(state);
        foreach (var unit in _previewUnits)
        {
            var p = CombatVisuals.Position(unit, state, state.Tick);
            _units[unit.Id] = new Vector2(p.X, p.Y * compression) - _center;
        }
        foreach (var structure in _previewStructures) _structures[(structure.Owner, structure.Kind, structure.Slot)] = Cell(structure.Cell);
        QueueRedraw();
    }
    private Vector2 Cell(int id)
    {
        var coordinate = _state!.Arena!.Board.Cells.Single(c => c.Id == id).Coordinate;
        var p = _projection.Project(coordinate); return new Vector2(p.X, p.Y) - _center;
    }
    internal Vector2 UnitPosition(int id) => _units[id];
    internal IReadOnlyList<UnitState> ParticipatingUnits => _previewUnits;
    public UnitState? PickUnit(Vector2 point) => _units.OrderBy(pair => pair.Value.DistanceSquaredTo(point)).FirstOrDefault(pair => pair.Value.DistanceTo(point) < 18) is { Key: not 0 } hit
        ? _previewUnits.FirstOrDefault(u => u.Id == hit.Key) : null;
    public StructureState? PickStructure(Vector2 point) => _structures.FirstOrDefault(pair => pair.Value.DistanceTo(point) < 20) is { Key.Item1: > 0 } hit
        ? _previewStructures.FirstOrDefault(s => (s.Owner, s.Kind, s.Slot) == hit.Key) : null;
    public override void _Draw()
    {
        if (_state?.Arena is not { } arena) return;
        bool modular = _scenery.Available;
        if (!modular) ArenaPresentation.Draw(this, _assets, arena, _projection);
        foreach (var cell in arena.Board.Cells)
        {
            var corners = _projection.Corners(cell.Coordinate).Select(p => new Vector2(p.X, p.Y) - _center).ToArray();
            WorldArt.Hex(this, corners, modular ? Colors.Transparent : new Color("45553b"), modular ? new Color(.65f, .73f, .78f, .22f) : new Color("6d7955"), 1);
        }
        var art = new WorldArt(_assets);
        var actors = _structures.Select(p => (Ground: p.Value, Structure: (StructureState?)_previewStructures.Single(s => (s.Owner, s.Kind, s.Slot) == p.Key), Unit: (UnitState?)null))
            .Concat(_units.Select(p => (Ground: p.Value, Structure: (StructureState?)null, Unit: (UnitState?)_previewUnits.Single(u => u.Id == p.Key))))
            .OrderBy(a => a.Ground.Y);
        foreach (var actor in actors)
        {
            if (!WorldArt.VisibleArea(this).Grow(120).HasPoint(actor.Ground)) continue;
            if (actor.Structure is { Active: true, Health: > 0 } structure)
            {
                art.Structure(this, structure, actor.Ground, Colors.White);
                WorldArt.Text(this, $"P{structure.Owner} {structure.Kind}", actor.Ground + new Vector2(0, 14), 10);
            }
            if (actor.Unit is { } unit)
            {
                var tint = new Color(unit.Faction == Faction.Adventurers ? "a4dfca" : "efd478");
                DrawArc(actor.Ground, 12, 0, Mathf.Tau, 24, tint, 1.5f, true);
                if (!art.Sprite(this, VisualAssets.UnitId(unit), actor.Ground, new Vector2(48, 60)))
                {
                    DrawCircle(actor.Ground, unit.IsBoss ? 9 : 6, tint);
                    WorldArt.Text(this, $"{unit.Type.ToString()[0]}{unit.Level}", actor.Ground - new Vector2(0, 10), 9);
                }
            }
        }
        foreach (var sector in arena.Sectors)
        {
            var city = _state.Players.Single(p => p.Id == sector.Owner);
            foreach (var home in city.Army?.Homes ?? [])
            {
                int fighting = _previewUnits.Count(u => u.Owner == city.Id && u.Assignment is { Stored: false } a && a.Tile == home.Cell);
                string count = home.Used > 0 ? $" {fighting}/{home.Used}" : "";
                WorldArt.Text(this, $"H{home.Number}{count}", Cell(sector.DeploymentCells[home.Number - 1]) + new Vector2(0, 23), 10, new Color("b7c8cd"));
            }
        }
        if (_state.Phase == Phase.RewardDraft)
        {
            WorldArt.Text(this, "Choose a reward first", new Vector2(0, -50), 13);
            WorldArt.Text(this, "Funded deployment follows in preparation", new Vector2(0, -30), 11);
        }
    }
}
