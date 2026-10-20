using Godot;
using Game.Core;

namespace Game.GodotClient;

public enum DetailsPage { Army, Worldtree, Inspection }

/// <summary>Contextual management, research and combatant inspection; never pauses authority.</summary>
public partial class GameplayDialog : Control
{
    private Control? _invoker;
    private TabContainer _tabs = null!;
    public ArmyRosterPanel Army { get; private set; } = null!;
    public DeploymentPanel Deployment => Army.Deployment;
    public ResearchTreePanel Research { get; private set; } = null!;
    public UnitInspector Inspector { get; private set; } = null!;
    private ScrollContainer _unitPane = null!;
    public Control? FallbackFocus { get; set; }
    public event Action? Closed;
    public event Action<int>? PlotRequested;
    private Button _close = null!, _worldtreeClose = null!;
    private PanelContainer _panel = null!;
    private MarginContainer _bodyMargin = null!;
    private StyleBox _panelStyle = null!, _tabStyle = null!;
    private readonly StyleBoxEmpty _emptyStyle = new();
    private GameplayContext _context = null!;
    private DetailsPage _page;
    public void Initialize(HudSkin skin, Action submitted)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Stop;
        var shade = new ColorRect { Color = new Color(0, 0, 0, .65f), MouseFilter = MouseFilterEnum.Stop }; AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _panel = new PanelContainer(); AddChild(_panel); _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _panel.OffsetLeft = 30; _panel.OffsetRight = -30; _panel.OffsetTop = 18; _panel.OffsetBottom = -18; skin.Panel(_panel, "modal");
        _panelStyle = _panel.GetThemeStylebox("panel");
        var body = new VBoxContainer(); _bodyMargin = Margin(body, 16); _panel.AddChild(_bodyMargin);
        _close = skin.Button("Close [Esc] — match continues", Close); body.AddChild(_close);
        var row = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddChild(row);
        _tabs = new TabContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; row.AddChild(_tabs);
        _tabStyle = _tabs.GetThemeStylebox("panel");
        var armyScroll = new ScrollContainer { Name = "Troops & homes", FollowFocus = true, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _tabs.AddChild(armyScroll);
        Army = new ArmyRosterPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill }; armyScroll.AddChild(Army); Army.Initialize(skin, submitted);
        Research = new ResearchTreePanel { Name = "Worldtree" }; _tabs.AddChild(Research); Research.Initialize(skin);
        _unitPane = new ScrollContainer { CustomMinimumSize = new(350, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; row.AddChild(_unitPane);
        Inspector = new UnitInspector { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _unitPane.AddChild(Inspector); Inspector.Initialize(skin);
        Army.UnitSelected += unit => { Inspector.Select(unit, false, _context.State.Phase); Inspector.Refresh(_context); };
        Inspector.PlotRequested += slot =>
        {
            if (ArmyConfiguration.IsArmyPlot(slot))
            {
                Open(DetailsPage.Army, _context); Army.ShowHomes(); Deployment.InspectHome(slot, _context);
            }
            else { Close(false); PlotRequested?.Invoke(slot); }
        };
        _tabs.TabChanged += tab => { _page = tab == 0 ? DetailsPage.Army : DetailsPage.Worldtree; if (_context is not null) Refresh(_context); };
        _worldtreeClose = skin.Button("Close [Esc]", Close); AddChild(_worldtreeClose);
        _worldtreeClose.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        _worldtreeClose.OffsetLeft = -190; _worldtreeClose.OffsetRight = -12; _worldtreeClose.OffsetTop = 4; _worldtreeClose.OffsetBottom = 48;
        _worldtreeClose.Hide(); Hide();
    }
    public void Open(DetailsPage page, GameplayContext context, Control? invoker = null)
    {
        if (!Visible) _invoker = invoker ?? GetViewport().GuiGetFocusOwner();
        _context = context;
        if (page != DetailsPage.Inspection) _tabs.CurrentTab = page == DetailsPage.Worldtree ? 1 : 0;
        _page = page; Show(); MoveToFront(); Refresh(context);
        if (_tabs.Visible) _tabs.GetTabBar().GrabFocus(); else _close.GrabFocus();
    }
    public void Close() => Close(true);
    public void Close(bool restoreFocus)
    {
        bool wasVisible = Visible; Hide();
        if (wasVisible) Closed?.Invoke();
        if (!restoreFocus) return;
        if (wasVisible && _invoker is { } control && IsInstanceValid(control) && control.IsVisibleInTree()) control.GrabFocus();
        else if (wasVisible && IsInstanceValid(FallbackFocus) && FallbackFocus!.IsVisibleInTree()) FallbackFocus.GrabFocus();
    }
    public void ResetSelection() { Inspector.Clear(); Army.ResetSelection(); Deployment.ResetSelection(); Research.ResetSelection(); }
    public void Refresh(GameplayContext context)
    {
        _context = context; if (!Visible) return;
        bool worldtree = _page == DetailsPage.Worldtree && Research.Illustrated;
        _close.Visible = !worldtree; _worldtreeClose.Visible = worldtree;
        _panel.OffsetLeft = worldtree ? 8 : 30; _panel.OffsetRight = worldtree ? -8 : -30; _panel.OffsetTop = worldtree ? 4 : 18;
        _panel.AddThemeStyleboxOverride("panel", worldtree ? _emptyStyle : _panelStyle);
        _tabs.AddThemeStyleboxOverride("panel", worldtree ? _emptyStyle : _tabStyle);
        foreach (string side in new[] { "left", "right", "top", "bottom" }) _bodyMargin.AddThemeConstantOverride("margin_" + side, worldtree ? 0 : 16);
        _tabs.Visible = _page != DetailsPage.Inspection;
        _unitPane.Visible = _page != DetailsPage.Worldtree;
        if (_page == DetailsPage.Army) Army.Refresh(context);
        if (_page == DetailsPage.Worldtree) Research.Refresh(context);
        Inspector.Refresh(context);
    }
}
