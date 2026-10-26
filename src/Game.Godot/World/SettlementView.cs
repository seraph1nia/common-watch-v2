using Godot;
using Game.Core;
using Game.Shared;
using NumVector = System.Numerics.Vector2;

namespace Game.GodotClient;

/// <summary>Nine authoritative slots on an illustrated map, with an artwork-independent grid fallback.</summary>
public partial class SettlementView : Node2D
{
    private GodotSession _session = null!;
    private WorldArt _art = null!;
    private AssetLoader _assets = null!;
    private EnvironmentView _environmentView = null!;
    private EnvironmentController _environment = null!;
    private SettlementBackdrop _backdrop = null!;
    private string _emitterKey = "";
    private (Texture2D Texture, SettlementMapLayout Layout)? _map;
    internal EnvironmentView Atmosphere => _environmentView;
    public bool HasMap => _map is not null;
    public SettlementMapLayout? MapLayout => _map?.Layout;
    public int? HoveredSlot { get; private set; }
    private static readonly Color _gold = new("efd478");
    // Settlement spacing is independent of the combat board's radius.
    public const float DefaultHexRadius = 81.25f;
    public float HexRadius { get; set; } = DefaultHexRadius;
    public HexProjection Projection { get; private set; } = new(DefaultHexRadius);
    public float Framing(Vector2 available) => _map is { } map
        ? Math.Min(Math.Max(available.X / map.Layout.Width, available.Y / map.Layout.Height),
            Math.Min((available.X - 40) / PlotBounds(map.Layout).Size.X, (available.Y - 100) / PlotBounds(map.Layout).Size.Y))
        : Math.Min(available.X / 900, available.Y / 720);
    private static Rect2 PlotBounds(SettlementMapLayout layout)
    {
        var min = new Vector2(layout.Spots.Min(s => s.Center.X - s.Radii.X), layout.Spots.Min(s => s.Center.Y - s.Radii.Y));
        var max = new Vector2(layout.Spots.Max(s => s.Center.X + s.Radii.X), layout.Spots.Max(s => s.Center.Y + s.Radii.Y));
        return new Rect2(min - new Vector2(layout.Width, layout.Height) / 2, max - min);
    }
    public Vector2 ScreenOrigin(Vector2 available)
    {
        if (_map is not { } map) return available * new Vector2(.56f, .5f);
        var plots = PlotBounds(map.Layout); float zoom = Framing(available);
        return new Vector2(Mathf.Clamp(available.X / 2, 20 - plots.Position.X * zoom, available.X - 20 - plots.End.X * zoom),
            Mathf.Clamp(available.Y / 2, 80 - plots.Position.Y * zoom, available.Y - 20 - plots.End.Y * zoom));
    }
    private float _compression = 1;
    private Vector2 Center => new(HexRadius * Mathf.Sqrt(3) / 4, HexRadius * 1.5f * 3 * _compression);
    public void Initialize(AssetLoader assets, EnvironmentController environment)
    {
        _assets = assets; _art = new(assets); _environment = environment;
        _backdrop = new SettlementBackdrop(); AddChild(_backdrop); _backdrop.Initialize(environment);
        _environmentView = environment.Attach(this, EnvironmentScene.Settlement); Hide();
    }
    public void Present(GodotSession session, bool visible)
    {
        _session = session;
        _map = _assets.SettlementMap();
        _backdrop.Present(_map?.Texture, _map is { } map ? new Vector2(map.Layout.Width, map.Layout.Height) : Vector2.Zero);
        if (_compression != _assets.Options.GroundCompression || Projection.Radius != HexRadius)
        { _compression = _assets.Options.GroundCompression; Projection = new(HexRadius, _compression); }
        Visible = visible; UpdateEnvironmentAnchors(); QueueRedraw();
    }
    private void UpdateEnvironmentAnchors()
    {
        if (!Visible || _map is not { } map) { _environmentView.SetArea(default, default); _environmentView.Hide(); return; }
        var bounds = new Rect2(-new Vector2(map.Layout.Width, map.Layout.Height) / 2, new Vector2(map.Layout.Width, map.Layout.Height));
        _environmentView.SetArea(bounds, new Rect2(bounds.Position, new Vector2(bounds.Size.X, bounds.Size.Y * .24f)));
        if (_environment.Settings.Quality == EnvironmentQuality.Off) return;
        string key = $"{_session.ViewedCity.Id}:" + string.Join(',', _session.ViewedCity.Slots.Select(s => s.Type));
        if (key == _emitterKey) return;
        _emitterKey = key; _environmentView.ClearEmitters();
        foreach (var spot in map.Layout.Spots)
        {
            var type = _session.ViewedCity.Slots[spot.Slot].Type;
            // The cutouts' foundation anchors and display sizes already define their pixel scale.
            string id = VisualAssets.BuildingId(type);
            float size = spot.BuildingSize * _assets.Catalog.Metadata(id).DisplaySize / 142;
            Vector2 ground = PlotPosition(spot.Slot);
            if (type is Building.Arcanum or Building.ResearchTower)
                _environmentView.AddEmitter(ground + new Vector2(0, -size * .64f), EnvironmentEmitterKind.Crystal, size * .16f);
            if (type is Building.MetalMine or Building.Lumbermill)
                _environmentView.AddEmitter(ground + new Vector2(size * .1f, -size * .58f), EnvironmentEmitterKind.Smoke, size * .12f);
        }
    }
    private Vector2 LocalPosition(NumVector point) => new Vector2(point.X, point.Y) - Center;
    private Vector2 MapPosition(MapPoint point) => new(point.X - _map!.Value.Layout.Width / 2f, point.Y - _map.Value.Layout.Height / 2f);
    public Vector2 PlotPosition(int index) => _map is { } map ? MapPosition(map.Layout.Spots[index].BuildingAnchor)
        : LocalPosition(Projection.Project(SettlementLayout.Coordinates[index]));
    public void HoverAt(Vector2? world)
    {
        int? hovered = Visible && world is { } point ? Pick(point) : null;
        if (HoveredSlot == hovered) return;
        HoveredSlot = hovered; QueueRedraw();
    }
    public int? Pick(Vector2 world)
    {
        world = ToLocal(world);
        if (_map is { } map)
            return map.Layout.Pick(new NumVector(world.X + map.Layout.Width / 2f, world.Y + map.Layout.Height / 2f));
        var p = world + Center;
        var hex = Projection.HitTest(new NumVector(p.X, p.Y), SettlementLayout.Coordinates);
        return hex is null ? null : SettlementLayout.Coordinates.ToList().IndexOf(hex.Value);
    }
    public override void _Draw()
    {
        _levelCues.Clear();
        if (_map is { } map) { DrawMap(map.Layout); return; }
        for (int i = 0; i < SettlementLayout.Coordinates.Count; i++)
        {
            var coordinate = SettlementLayout.Coordinates[i]; var slot = _session.ViewedCity.Slots[i];
            WorldArt.Hex(this, Projection.Corners(coordinate).Select(LocalPosition).ToArray(), slot.Purchased ? new Color("304a40") : new Color("252e34"), i == _session.SelectedSlot ? _gold : new Color("62796f"), i == _session.SelectedSlot ? 4 : 2);
        }
        // Complete the fallback grid before drawing anchored objects.
        foreach (int i in Enumerable.Range(0, SettlementLayout.Coordinates.Count)
                     .OrderBy(i => Projection.Project(SettlementLayout.Coordinates[i]).Y)
                     .ThenBy(i => Projection.Project(SettlementLayout.Coordinates[i]).X))
        {
            var coordinate = SettlementLayout.Coordinates[i]; var slot = _session.ViewedCity.Slots[i];
            var center = LocalPosition(Projection.Project(coordinate));
            if (slot.Type != Building.Empty)
            {
                if (!_art.Sprite(this, VisualAssets.BuildingId(slot.Type), center, Vector2.One * _assets.Catalog.Metadata(VisualAssets.BuildingId(slot.Type)).DisplaySize))
                {
                    DrawRect(new Rect2(center - new Vector2(24, 26), new Vector2(48, 43)), new Color("92b5a5"));
                    DrawLine(center + new Vector2(-28, -26), center + new Vector2(0, -44), _gold, 3);
                    DrawLine(center + new Vector2(0, -44), center + new Vector2(28, -26), _gold, 3);
                }
            }
        }
        for (int i = 0; i < SettlementLayout.Coordinates.Count; i++)
            DrawBuildingProgress(i, LocalPosition(Projection.Project(SettlementLayout.Coordinates[i])));
    }

    private void DrawMap(SettlementMapLayout layout)
    {
        // The retained backdrop shares these uncompressed image pixels and Camera2D.
        // Its material never touches rings, upright buildings, labels or picking.
        foreach (var spot in layout.Spots)
        {
            bool selected = spot.Slot == _session.SelectedSlot, hovered = spot.Slot == HoveredSlot;
            if (selected || hovered)
                DrawRing(spot, selected ? _gold : new Color(.86f, 1, .92f, .65f), selected ? 4 : 2);
        }
        foreach (var spot in layout.Spots.OrderBy(s => s.BuildingAnchor.Y).ThenBy(s => s.BuildingAnchor.X))
        {
            var slot = _session.ViewedCity.Slots[spot.Slot];
            Vector2 ground = MapPosition(spot.BuildingAnchor);
            if (slot.Type != Building.Empty)
            {
                // Catalog anchors measure supporting foundations, excluding shadows/grass.
                string id = VisualAssets.BuildingId(slot.Type);
                float size = spot.BuildingSize * _assets.Catalog.Metadata(id).DisplaySize / 142;
                if (!_art.Sprite(this, id, ground, Vector2.One * size))
                {
                    DrawRect(new Rect2(ground - new Vector2(28, 44), new Vector2(56, 44)), new Color("92b5a5"));
                    DrawPolyline([ground + new Vector2(-34, -44), ground + new Vector2(0, -66), ground + new Vector2(34, -44)], _gold, 4, true);
                }
            }
            else if (!slot.Purchased)
                _art.Sprite(this, "resource/gold", ground, Vector2.One * 46);
        }
        // Annotations finish after every tall sprite, so rear-building levels stay visible.
        foreach (var spot in layout.Spots) DrawBuildingProgress(spot.Slot, MapPosition(spot.BuildingAnchor));
    }

    private void DrawRing(SettlementMapSpot spot, Color color, float width)
    {
        Vector2 center = MapPosition(spot.Center), radii = new(spot.Radii.X, spot.Radii.Y);
        // Two open arcs leave breathing room at the sides of the painted stone rim.
        for (int half = 0; half < 2; half++)
        {
            Vector2[] points = Enumerable.Range(0, 49).Select(i =>
            {
                float angle = half * Mathf.Pi + .16f + (Mathf.Pi - .32f) * i / 48;
                return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radii;
            }).ToArray();
            DrawPolyline(points, color, width, true);
        }
    }
}
