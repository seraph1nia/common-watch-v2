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
    private (Texture2D Texture, SettlementMapLayout Layout)? _map;
    public bool HasMap => _map is not null;
    public SettlementMapLayout? MapLayout => _map?.Layout;
    public int? HoveredSlot { get; private set; }
    private static readonly Color _gold = new("efd478");
    // Settlement spacing is independent of the combat board's radius.
    public const float DefaultHexRadius = 81.25f;
    public float HexRadius { get; set; } = DefaultHexRadius;
    public HexProjection Projection { get; private set; } = new(DefaultHexRadius);
    // Keep the tested 720p composition as the window grows; camera controls remain relative
    // to world pixels, and combat retains its own framing.
    public static float DefaultMagnification(Vector2 available) => Math.Clamp(
        1.15f * Math.Min(available.X / 912, available.Y / 472), .55f, 2.2f);
    public float Framing(Vector2 available) => _map is { } map
        ? Math.Clamp(Math.Min(available.X / map.Layout.Frame.Size.X, available.Y / map.Layout.Frame.Size.Y), .55f, 2.2f)
        : _assets.Options.Mode == RenderMode.Sprites && _assets.Options.View == ViewPreset.Angled ? DefaultMagnification(available) : 1;
    private float _compression = 1;
    private Vector2 Center => new(HexRadius * Mathf.Sqrt(3) / 4, HexRadius * 1.5f * 3 * _compression);
    public void Initialize(AssetLoader assets) { _assets = assets; _art = new(assets); Hide(); }
    public void Present(GodotSession session, bool visible)
    {
        _session = session;
        _map = _assets.SettlementMap();
        if (_compression != _assets.Options.GroundCompression || Projection.Radius != HexRadius)
        { _compression = _assets.Options.GroundCompression; Projection = new(HexRadius, _compression); }
        Visible = visible; QueueRedraw();
    }
    private Vector2 LocalPosition(NumVector point) => new Vector2(point.X, point.Y) - Center;
    private Vector2 MapPosition(MapPoint point) => new(point.X - _map!.Value.Layout.Frame.Center.X, point.Y - _map.Value.Layout.Frame.Center.Y);
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
            return map.Layout.Pick(new NumVector(world.X + map.Layout.Frame.Center.X, world.Y + map.Layout.Frame.Center.Y));
        var p = world + Center;
        var hex = Projection.HitTest(new NumVector(p.X, p.Y), SettlementLayout.Coordinates);
        return hex is null ? null : SettlementLayout.Coordinates.ToList().IndexOf(hex.Value);
    }
    public override void _Draw()
    {
        if (_map is { } map) { DrawMap(map.Texture, map.Layout); return; }
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
    }

    private void DrawMap(Texture2D texture, SettlementMapLayout layout)
    {
        // The source already contains perspective. Image, rings and upright sprites share
        // uncompressed image pixels and the native Camera2D transform.
        DrawTextureRect(texture, new Rect2(MapPosition(new MapPoint(0, 0)), new Vector2(layout.Width, layout.Height)), false);
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
