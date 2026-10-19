using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>Decorative painted branches carry no rules. These arrows use the snapshot catalog.</summary>
public partial class WorldtreeConnections : Control
{
    private WorldtreeArt _art = null!;
    private TechnologyDefinition[] _catalog = [];
    private ResearchState _research;
    private TechnologyId _selected;
    public IReadOnlyList<(TechnologyId From, TechnologyId To)> Edges => _catalog
        .Where(n => n.Prerequisite != TechnologyId.None).Select(n => (n.Prerequisite, n.Id)).ToArray();

    public void Initialize(WorldtreeArt art) { _art = art; MouseFilter = MouseFilterEnum.Ignore; }
    public void Present(TechnologyDefinition[] catalog, ResearchState research, TechnologyId selected)
    {
        _catalog = catalog; _research = research; _selected = selected; QueueRedraw();
    }
    public override void _Draw()
    {
        foreach (var node in _catalog.Where(n => n.Prerequisite != TechnologyId.None))
        {
            var parent = _art.Sockets.Single(s => s.Id == node.Prerequisite);
            var child = _art.Sockets.Single(s => s.Id == node.Id);
            Vector2[] path = [parent.Center, .. child.ConnectorVia, child.Center];
            path[0] += (path[1] - path[0]).Normalized() * parent.HitRadius;
            path[^1] -= (path[^1] - path[^2]).Normalized() * child.HitRadius;
            bool highlighted = node.Id == _selected || node.Prerequisite == _selected;
            Color color = node.Id == _selected ? new("ffe3a0") : highlighted ? new("8ff7ec")
                : _research.Has(node.Id) ? new("d1bd80b0") : new("a9d2d7a0");
            for (int i = 1; i < path.Length; i++)
            {
                DrawLine(path[i - 1], path[i], new Color("102126b0"), highlighted ? 7 : 4, true);
                DrawDashedLine(path[i - 1], path[i], color, highlighted ? 3 : 2, 8, true);
            }
            Vector2 end = path[^1], direction = (end - path[^2]).Normalized(), side = direction.Orthogonal();
            Vector2[] arrow = [end - direction * 12 + side * 6, end, end - direction * 12 - side * 6];
            DrawPolyline(arrow, new Color("102126"), 7, true);
            DrawPolyline(arrow, color, 3, true);
        }
    }
}
