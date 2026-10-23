using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public enum DetailsPage { Army, Worldtree, Inspection, City }

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
    internal Rect2[] FeedbackAreas => new Control[] { _close, _worldtreeClose, _tabs.GetTabBar() }
        .Where(c => c.IsVisibleInTree()).Select(c => c.GetGlobalRect()).ToArray();
    private PanelContainer _panel = null!;
    private MarginContainer _bodyMargin = null!;
    private StyleBox _panelStyle = null!, _tabStyle = null!;
    private readonly StyleBoxEmpty _emptyStyle = new();
    private GameplayContext _context = null!;
    private DetailsPage _page;
    private Label _cityDetails = null!, _events = null!;
    public IReadOnlyList<string> RecentFeedback { get; set; } = [];
    public BattleOutcome? LastOutcome { get; set; }
    public void Initialize(HudSkin skin, Action submitted)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Stop;
        var shade = new ColorRect { Color = new Color(0, 0, 0, .65f), MouseFilter = MouseFilterEnum.Stop }; AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _panel = new PanelContainer(); AddChild(_panel); _panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _panel.OffsetLeft = 30; _panel.OffsetRight = -30; _panel.OffsetTop = 18; _panel.OffsetBottom = -18; skin.Panel(_panel, "modal");
        _panelStyle = _panel.GetThemeStylebox("panel");
        var body = new VBoxContainer(); _bodyMargin = Margin(body, 16); _panel.AddChild(_bodyMargin);
        _close = skin.Button("Close [Esc] — match continues", Close, "ForestQuietButton"); body.AddChild(_close);
        var row = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; body.AddChild(row);
        _tabs = new TabContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; row.AddChild(_tabs);
        _tabStyle = _tabs.GetThemeStylebox("panel");
        var armyScroll = new ScrollContainer { Name = "Troops & homes", FollowFocus = true, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _tabs.AddChild(armyScroll);
        Army = new ArmyRosterPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; armyScroll.AddChild(Army); Army.Initialize(skin, submitted);
        Research = new ResearchTreePanel { Name = "Worldtree" }; _tabs.AddChild(Research); Research.Initialize(skin);
        var cityScroll = new ScrollContainer { Name = "City details", FollowFocus = true, FocusMode = FocusModeEnum.All, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _tabs.AddChild(cityScroll);
        var cityBody = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; cityScroll.AddChild(cityBody);
        _cityDetails = WrapLabel(); cityBody.AddChild(_cityDetails);
        cityBody.AddChild(new HSeparator());
        var eventsHeading = WrapLabel("Recent session feedback"); eventsHeading.AddThemeFontSizeOverride("font_size", 22); cityBody.AddChild(eventsHeading);
        _events = WrapLabel(); cityBody.AddChild(_events);
        _unitPane = new ScrollContainer { FollowFocus = true, CustomMinimumSize = new(350, 0), SizeFlagsHorizontal = SizeFlags.Fill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; row.AddChild(_unitPane);
        Resized += () => _unitPane.CustomMinimumSize = new(Mathf.Max(350, Size.X * .32f), 0);
        Inspector = new UnitInspector { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _unitPane.AddChild(Inspector); Inspector.Initialize(skin);
        Army.UnitSelected += unit => { Inspector.Select(unit, false, _context.State.Phase); Inspector.Refresh(_context); };
        Army.SectionChanged += () => { if (_context is not null) Refresh(_context); };
        Inspector.PlotRequested += slot =>
        {
            if (ArmyConfiguration.IsArmyPlot(slot))
            {
                Open(DetailsPage.Army, _context); Army.ShowHomes(); Deployment.InspectHome(slot, _context);
            }
            else { Close(false); PlotRequested?.Invoke(slot); }
        };
        _tabs.TabChanged += tab => { _page = tab == 0 ? DetailsPage.Army : tab == 1 ? DetailsPage.Worldtree : DetailsPage.City; if (_context is not null) Refresh(_context); };
        _worldtreeClose = skin.Button("Close [Esc]", Close, "ForestQuietButton"); AddChild(_worldtreeClose);
        _worldtreeClose.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        _worldtreeClose.OffsetLeft = -190; _worldtreeClose.OffsetRight = -12; _worldtreeClose.OffsetTop = 4; _worldtreeClose.OffsetBottom = 48;
        _worldtreeClose.Hide(); Hide();
    }
    public void Open(DetailsPage page, GameplayContext context, Control? invoker = null)
    {
        if (!Visible) _invoker = invoker ?? GetViewport().GuiGetFocusOwner();
        _context = context;
        if (page != DetailsPage.Inspection) _tabs.CurrentTab = page == DetailsPage.Worldtree ? 1 : page == DetailsPage.City ? 2 : 0;
        _page = page; Show(); MoveToFront(); Refresh(context);
        if (_tabs.Visible) _tabs.GetTabBar().GrabFocus(); else _close.GrabFocus();
    }
    public void Close() => Close(true);
    public void Close(bool restoreFocus)
    {
        bool wasVisible = Visible; Inspector.CancelRetirement(); Hide();
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
        _unitPane.Visible = _page == DetailsPage.Inspection || _page == DetailsPage.Army && !Army.HomesVisible;
        if (_page == DetailsPage.City)
        {
            _cityDetails.Text = $"P{context.City.Id} · Wave {context.State.Wave}/{context.State.TotalWaves} · {context.State.Phase}\n"
                + (context.City.Id != context.Session.PlayerId ? "Allied city · read-only\n" : "")
                + "\nNext encounter\n" + EncounterBriefing.Project(context.State, context.City.Id).Details
                + "\n\n" + BuildDirection.Inspection(context.City) + "\n\n" + BuildImpact.Inspection(context.City);
            if (context.City.FoodForecast is { } food)
                _cityDetails.Text += $"\n\nExact Food manifest · payment {food.Paid}/{food.Demand} · {food.Available - food.Paid} left"
                    + "\nParticipating: " + string.Join(", ", food.Participating.Select(id => "#" + id))
                    + "\nFunded: " + string.Join(", ", food.Funded.Select(id => "#" + id))
                    + "\nUnfunded: " + string.Join(", ", food.Unfed.Select(id => "#" + id));
            if (context.City.LastUpkeep is { } paid)
                _cityDetails.Text += $"\n\nLast upkeep · wave {paid.Wave}: {paid.Paid} Food paid · {paid.Unfed.Length} unfunded";
            if (LastOutcome is { } outcome) _cityDetails.Text += "\n\nLast battle\n" + outcome.Summary + "\n" + outcome.Details;
            _cityDetails.Text += "\n\nCampaign · allocations per original city; fallen allocations are redistributed\n" + EncounterBriefing.Project(context.State, context.City.Id).Schedule;
            _events.Text = string.Join("\n\n", RecentFeedback);
        }
        if (_page == DetailsPage.Army) Army.Refresh(context);
        if (_page == DetailsPage.Worldtree) Research.Refresh(context);
        _unitPane.SizeFlagsHorizontal = _page == DetailsPage.Inspection ? SizeFlags.ExpandFill : SizeFlags.Fill;
        Inspector.FallbackFocus = _tabs.Visible ? _tabs.GetTabBar() : _close;
        Inspector.Refresh(context);
    }
}
