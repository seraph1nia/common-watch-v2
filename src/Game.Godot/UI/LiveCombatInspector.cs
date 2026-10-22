using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>Read-only, nonmodal inspection sampled from the same playback as the figures.</summary>
public partial class LiveCombatInspector : PanelContainer
{
    private Control? _invoker;
    private ScrollContainer _scroll = null!;
    public Control? FallbackFocus { get; set; }
    public UnitInspector Inspector { get; private set; } = null!;
    internal Button CloseButton { get; private set; } = null!;
    public int SelectedUnit => Visible ? Inspector.SelectedUnit : 0;
    public (int Owner, ArenaStructureKind Kind, int Slot)? SelectedStructure => Visible ? Inspector.SelectedStructure : null;
    public void Initialize(HudSkin skin)
    {
        skin.Panel(this);
        var column = new VBoxContainer(); AddChild(column);
        CloseButton = skin.Button("Close inspection [Esc]", () => Close()); column.AddChild(CloseButton);
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
        bool wasVisible = Visible; Hide(); Inspector.Clear();
        if (wasVisible && restoreFocus && _invoker is { } control && IsInstanceValid(control) && control.IsVisibleInTree()) control.GrabFocus();
        else if (wasVisible && restoreFocus && FallbackFocus is { } fallback && IsInstanceValid(fallback) && fallback.IsVisibleInTree()) fallback.GrabFocus();
    }
    public void Layout(Vector2 viewport)
    {
        Position = new(viewport.X >= 1600 ? 24 : 12, 92);
        Size = new(viewport.X >= 1600 ? 380 : 310, Mathf.Min(460, viewport.Y - 120));
    }
}
