using Godot;
using Game.Core;
using Game.Shared;
using NumVector = System.Numerics.Vector2;

namespace Game.GodotClient;

/// <summary>Nine logical plots batched in a retained native world view; ground depth orders artwork.</summary>
public partial class SettlementView : Node2D
{
    private GodotSession _session = null!;
    private WorldArt _art = null!;
    private AssetLoader _assets = null!;
    private static readonly Color _gold = new("efd478");
    public HexProjection Projection { get; private set; } = new(65);
    private float _compression = 1;
    private Vector2 Center => new(0, 65 * 1.5f * 3 * _compression);
    public void Initialize(AssetLoader assets) { _assets = assets; _art = new(assets); Hide(); }
    public void Present(GodotSession session, bool visible)
    {
        _session = session;
        if (_compression != _assets.Options.GroundCompression)
        { _compression = _assets.Options.GroundCompression; Projection = new(65, _compression); }
        Visible = visible; QueueRedraw();
    }
    private Vector2 LocalPosition(NumVector point) => new Vector2(point.X, point.Y) - Center;
    public Vector2 PlotPosition(int index) => LocalPosition(Projection.Project(SettlementLayout.Coordinates[index]));
    public int? Pick(Vector2 world)
    {
        var p = world + Center;
        var hex = Projection.HitTest(new NumVector(p.X, p.Y), SettlementLayout.Coordinates);
        return hex is null ? null : SettlementLayout.Coordinates.ToList().IndexOf(hex.Value);
    }
    public override void _Draw()
    {
        for (int i = 0; i < SettlementLayout.Coordinates.Count; i++)
        {
            var coordinate = SettlementLayout.Coordinates[i]; var slot = _session.ViewedCity.Slots[i];
            var center = LocalPosition(Projection.Project(coordinate));
            WorldArt.Hex(this, Projection.Corners(coordinate).Select(LocalPosition).ToArray(), slot.Purchased ? new Color("304a40") : new Color("252e34"), i == _session.SelectedSlot ? _gold : new Color("62796f"), i == _session.SelectedSlot ? 4 : 2);
            _art.Ground(this, "terrain/woodland_hex", Projection.Corners(coordinate).Select(LocalPosition).ToArray());
        }
        // Keep borders and selection readable above optional opaque terrain textures.
        for (int i = 0; i < SettlementLayout.Coordinates.Count; i++)
        {
            var corners = Projection.Corners(SettlementLayout.Coordinates[i]).Select(LocalPosition).ToArray();
            DrawPolyline(corners.Append(corners[0]).ToArray(), i == _session.SelectedSlot ? _gold : new Color("62796f"), i == _session.SelectedSlot ? 4 : 2, true);
        }
        // Complete terrain before drawing anchored objects, including future tall sprites.
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
                // Captions share their object's depth: a nearer building may cover a rear
                // caption, just as it covers the rear building. The inspector retains its name.
                DrawRect(new Rect2(center + new Vector2(-50, 17), new Vector2(100, 35)), new Color(0.04f, .07f, .08f, .8f));
                WorldArt.Text(this, slot.Type.ToString(), center + new Vector2(0, 32), 14);
                WorldArt.Text(this, $"Lv {slot.Level}", center + new Vector2(0, 47), 11);
            }
            else
            {
                WorldArt.Text(this, slot.Purchased ? "Empty" : "Buy plot", center + new Vector2(0, 6), 16);
                WorldArt.Text(this, $"{i + 1}", center + new Vector2(0, 26), 13);
            }
        }
    }
}
