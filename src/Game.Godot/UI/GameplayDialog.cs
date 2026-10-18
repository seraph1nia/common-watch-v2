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
    public BuildingInspector Building => _building;
    private BuildingInspector _building = null!;
    public UnitInspector Inspector { get; private set; } = null!;
    public void Initialize(HudSkin skin, Action submitted)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Stop;
        var shade = new ColorRect { Color = new Color(0, 0, 0, .65f), MouseFilter = MouseFilterEnum.Stop }; AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panel = new PanelContainer(); AddChild(panel); panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); panel.OffsetLeft = 30; panel.OffsetRight = -30; panel.OffsetTop = 18; panel.OffsetBottom = -60; skin.Panel(panel);
        var body = new VBoxContainer(); panel.AddChild(Margin(body, 16));
        body.AddChild(skin.Button("Close details [Esc] — match continues", Close));
        var row = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddChild(row);
        _tabs = new TabContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; row.AddChild(_tabs);
        Army = new ArmyRosterPanel(); Tab("Army", Army); Army.Initialize(skin);
        Deployment = new DeploymentPanel(); Tab("Homes & towers", Deployment); Deployment.Initialize(skin, submitted);
        Research = new ResearchTreePanel(); Tab("Worldtree", Research); Research.Initialize(skin);
        Battle = new BattleInspectionPanel(); Tab("Battle forecast", Battle); Battle.Initialize(skin);
        Run = new RunProgressionPanel(); Tab("Run", Run); Run.Initialize();
        _building = new BuildingInspector(); Tab("Selected plot", _building); _building.Initialize(skin, submitted);
        var scroll = new ScrollContainer { CustomMinimumSize = new(350, 0), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; row.AddChild(scroll); _unitPane = scroll;
        Inspector = new UnitInspector { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(Inspector); Inspector.Initialize(skin);
        Army.UnitSelected += unit => { Inspector.Select(unit, false, _context.State.Phase); Inspector.Refresh(_context); };
        Battle.UnitSelected += (unit, preview) => { Inspector.Select(unit, preview, _context.State.Phase); Inspector.Refresh(_context); };
        Inspector.PlotRequested += slot => { _plotOverride = slot; _tabs.CurrentTab = 5; Refresh(_context); };
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
        _invoker = invoker; _tabs.CurrentTab = tab; Show(); MoveToFront(); Refresh(context);
        _tabs.GetTabBar().GrabFocus();
    }
    public void Close() { Hide(); if (_invoker is { } control && IsInstanceValid(control) && control.IsVisibleInTree()) control.GrabFocus(); }
    public void ResetSelection() { _plotOverride = -1; Inspector.Clear(); Army.ResetSelection(); Deployment.ResetSelection(); Research.ResetSelection(); }
    public void Refresh(GameplayContext context)
    {
        _context = context; if (!Visible) return;
        _unitPane.Visible = _tabs.CurrentTab != 5;
        switch (_tabs.CurrentTab)
        {
            case 0: Army.Refresh(context); break; case 1: Deployment.Refresh(context); break;
            case 2: Research.Refresh(context); break; case 3: Battle.Refresh(context); break; case 4: Run.Refresh(context); break;
            case 5: _building.Refresh(context, _plotOverride >= 0 ? _plotOverride : context.Session.SelectedSlot); break;
        }
        Inspector.Refresh(context);
    }
}
