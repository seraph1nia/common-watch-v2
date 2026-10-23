using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>Read-only, nonmodal inspection sampled from the same playback as the figures.</summary>
public partial class LiveCombatInspector : PanelContainer
{
    private Control? _invoker;
    private ScrollContainer _scroll = null!;
    private string _placementKey = "";
    private Vector2 _placementViewport;
    private Rect2? _placement;
    private double _placedTick;
    public Control? FallbackFocus { get; set; }
    public UnitInspector Inspector { get; private set; } = null!;
    internal Button CloseButton { get; private set; } = null!;
    public int SelectedUnit => Visible ? Inspector.SelectedUnit : 0;
    public (int Owner, ArenaStructureKind Kind, int Slot)? SelectedStructure => Visible ? Inspector.SelectedStructure : null;
    public void Initialize(HudSkin skin)
    {
        skin.Panel(this);
        var column = new VBoxContainer(); AddChild(column);
        CloseButton = skin.Button("Close inspection [Esc]", () => Close(), "ForestQuietButton"); column.AddChild(CloseButton);
        CloseButton.TooltipText = "Your troops: circle bases. Allies: split circles. Enemies: triangular bases. Names, roles and targets appear here on selection; advanced details are optional.";
        _scroll = new ScrollContainer { FollowFocus = true, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill }; column.AddChild(_scroll);
        Inspector = new UnitInspector { ReadOnlyLive = true, SizeFlagsHorizontal = SizeFlags.ExpandFill }; _scroll.AddChild(Inspector); Inspector.Initialize(skin);
        Hide();
    }
    public void Inspect(UnitState unit, GodotSession session)
    { Open(); Inspector.Select(unit, false, session.Snapshot.Phase); Refresh(session, true); }
    public void Inspect(StructureState structure, GodotSession session)
    { Open(); Inspector.Select(structure); Refresh(session, true); }
    private void Open()
    { if (!Visible) _invoker = GetViewport().GuiGetFocusOwner(); Show(); CloseButton.GrabFocus(); }
    public void Refresh(GodotSession session, bool combat)
    {
        if (!combat) { Close(false); return; }
        if (!Visible) return;
        Inspector.Refresh(new(session, () => { }), session.Playback);
        if (Inspector.SelectedUnit == 0 && Inspector.SelectedStructure is null) Close();
    }
    public void Close(bool restoreFocus = true)
    {
        bool wasVisible = Visible; Hide(); Inspector.Clear(); _placement = null; _placementKey = "";
        if (wasVisible && restoreFocus && _invoker is { } control && IsInstanceValid(control) && control.IsVisibleInTree()) control.GrabFocus();
        else if (wasVisible && restoreFocus && FallbackFocus is { } fallback && IsInstanceValid(fallback) && fallback.IsVisibleInTree()) fallback.GrabFocus();
    }
    public void Layout(Vector2 viewport, float top = 92)
    {
        if (viewport != _placementViewport) { _placement = null; _placementViewport = viewport; }
        if (_placement is { } placed) { Position = placed.Position; Size = placed.Size; return; }
        Position = new(viewport.X >= 1600 ? 24 : 12, top);
        Size = new(viewport.X >= 1600 ? 340 : 280, Mathf.Min(300, viewport.Y - top - 104));
    }
    internal void KeepSubjectVisible(Vector2 viewport, Vector2[] points, IEnumerable<Rect2> obstacles, double tick)
    {
        if (!Visible || points.Length == 0) return;
        string key = $"{Inspector.SelectedUnit}:{Inspector.SelectedStructure}";
        var subject = new Rect2(points[0], Vector2.Zero);
        foreach (var point in points.Skip(1)) subject = subject.Expand(point);
        subject = subject.Grow(28);
        bool changed = _placementKey != key || _placementViewport != viewport || _placement is null;
        if (!changed && (!GetGlobalRect().Intersects(subject) || tick - _placedTick < 120)) return;
        float edge = viewport.X >= 1600 ? 24 : 12;
        var size = new Vector2(viewport.X >= 1600 ? 340 : 280, 300);
        var blocks = obstacles.ToArray();
        var candidates = new[] { new Vector2(edge, 80), new Vector2(viewport.X - size.X - edge, 80),
            new Vector2(edge, viewport.Y - size.Y - 104), new Vector2(viewport.X - size.X - edge, viewport.Y - size.Y - 104) }
            .Select(at => new Rect2(at, size));
        float Penalty(Rect2 area) => blocks.Sum(b => b.Intersects(area) ? b.Intersection(area).Area : 0)
            + (area.Intersects(subject) ? 100000 + area.Intersection(subject).Area : 0);
        var chosen = candidates.OrderBy(Penalty).ThenByDescending(r => r.GetCenter().DistanceSquaredTo(subject.GetCenter())).First();
        _placement = chosen; _placementKey = key; _placementViewport = viewport; _placedTick = tick;
        Position = chosen.Position; Size = chosen.Size;
    }
}
