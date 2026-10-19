using Godot;
using Game.Core;

namespace Game.GodotClient;

/// <summary>One retained native modal; opening it never pauses a cooperative match.</summary>
public partial class GameplayDialog : Control
{
    private Control? _invoker;
    private TabContainer _tabs = null!;
    public ArmyRosterPanel Army { get; private set; } = null!;
    public DeploymentPanel Deployment { get; private set; } = null!;
    public ResearchTreePanel Research { get; private set; } = null!;
    public BattleInspectionPanel Battle { get; private set; } = null!;
    public RunProgressionPanel Run { get; private set; } = null!;
    private ScrollContainer _unitPane = null!;
    private int _plotOverride = -1;
    private Node _buildingHome = null!;
    private ScrollContainer _buildingTab = null!;
    public Control? FallbackFocus { get; set; }
    public event Action? Closed;
    private Button _close = null!;
    public BuildingInspector Building => _building;
    private BuildingInspector _building = null!;
    public UnitInspector Inspector { get; private set; } = null!;
    public void Initialize(HudSkin skin, Action submitted, BuildingInspector building, Label upkeep)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Stop;
        var shade = new ColorRect { Color = new Color(0, 0, 0, .65f), MouseFilter = MouseFilterEnum.Stop }; AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panel = new PanelContainer(); AddChild(panel); panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); panel.OffsetLeft = 30; panel.OffsetRight = -30; panel.OffsetTop = 18; panel.OffsetBottom = -82; skin.Panel(panel, "modal");
        var body = new VBoxContainer(); panel.AddChild(Margin(body, 16));
        _close = skin.Button("Close details [Esc] — match continues", Close); body.AddChild(_close);
        var row = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddChild(row);
        _tabs = new TabContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; row.AddChild(_tabs);
        Army = new ArmyRosterPanel(); Tab("Army", Army); Army.Initialize(skin, submitted);
        Deployment = Army.Deployment;
        Research = new ResearchTreePanel(); Tab("Worldtree", Research); Research.Initialize(skin);
        Battle = new BattleInspectionPanel(); Tab("Battle forecast", Battle); Battle.Initialize(skin);
        Run = new RunProgressionPanel(); Tab("Run", Run); Run.Initialize();
        _building = building; _buildingHome = building.GetParent();
        _buildingTab = new ScrollContainer { Name = "Selected plot", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _tabs.AddChild(_buildingTab);
        Tab("Upkeep", upkeep);
        var scroll = new ScrollContainer { CustomMinimumSize = new(350, 0), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; row.AddChild(scroll); _unitPane = scroll;
        Inspector = new UnitInspector { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(Inspector); Inspector.Initialize(skin);
        Army.UnitSelected += unit => { Inspector.Select(unit, false, _context.State.Phase); Inspector.Refresh(_context); };
        Battle.UnitSelected += (unit, preview) => { Inspector.Select(unit, preview, _context.State.Phase); Inspector.Refresh(_context); };
        Inspector.PlotRequested += slot => { _plotOverride = slot; _tabs.CurrentTab = 4; Refresh(_context); };
        Battle.StructureSelected += structure => { Inspector.Select(structure); Inspector.Refresh(_context); };
        _tabs.TabChanged += _ => { if (_context is not null) Refresh(_context); };
        Hide();
    }
    private void Tab(string title, Control content)
    {
        var scroll = new ScrollContainer { Name = title, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _tabs.AddChild(scroll);
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill; scroll.AddChild(content);
    }
    private GameplayContext _context = null!;
    public void Open(int tab, GameplayContext context, Control? invoker = null)
    {
        if (tab == 5) _plotOverride = -1;
        if (!Visible) _invoker = invoker ?? GetViewport().GuiGetFocusOwner();
        _context = context;
        int index = tab switch { 0 or 1 => 0, _ => tab - 1 };
        _tabs.CurrentTab = index; Show(); MoveToFront();
        if (tab == 1) Army.ShowHomes();
        Refresh(context);
        _tabs.GetTabBar().GrabFocus();
    }
    public void Close() => Close(true);
    public void Close(bool restoreFocus)
    {
        bool wasVisible = Visible; Hide(); _building.Compact = true;
        if (_building.GetParent() != _buildingHome) _building.Reparent(_buildingHome);
        if (wasVisible) Closed?.Invoke();
        if (!restoreFocus) return;
        if (wasVisible && _invoker is { } control && IsInstanceValid(control) && control.IsVisibleInTree()) control.GrabFocus();
        else if (wasVisible && IsInstanceValid(FallbackFocus) && FallbackFocus!.IsVisibleInTree()) FallbackFocus.GrabFocus();
    }
    public void ResetSelection() { _plotOverride = -1; Inspector.Clear(); Army.ResetSelection(); Deployment.ResetSelection(); Research.ResetSelection(); }
    public void Refresh(GameplayContext context)
    {
        _context = context; if (!Visible) return;
        _building.Compact = false;
        _unitPane.Visible = _tabs.CurrentTab is 0 or 2;
        var target = _tabs.CurrentTab == 4 ? (Node)_buildingTab : _buildingHome;
        if (_building.GetParent() != target) _building.Reparent(target);
        switch (_tabs.CurrentTab)
        {
            case 0: Army.Refresh(context); break;
            case 1: Research.Refresh(context); break; case 2: Battle.Refresh(context); break; case 3: Run.Refresh(context); break;
            case 4: _building.Refresh(context, _plotOverride >= 0 ? _plotOverride : context.Session.SelectedSlot); break;
        }
        Inspector.Refresh(context);
    }
}
