using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class SettlementHud : Control
{
    private GodotSession _session = null!;
    private Label _phase = null!, _keep = null!;
    private ResourcePanel _economy = null!;
    private CitySelector _cities = null!;
    private MatchControls _controls = null!;
    private PanelContainer _ribbon = null!, _dock = null!, _action = null!;
    private HBoxContainer _nav = null!;
    private VBoxContainer _ribbonBody = null!;
    private ScrollContainer _contextBody = null!;
    private HFlowContainer _contextNav = null!;
    private Control _ribbonSpacer = null!;
    private Button _feedback = null!, _clear = null!;
    private string _feedbackKey = "";
    private double _acceptedUntil;
    private readonly Queue<string> _recentFeedback = new();
    private (Vector2 View, Vector2 Ribbon, Vector2 Action, Vector2 Inspector, Vector2 Nav, bool Selected, bool Construction, bool Modal, bool Arena)? _layout;
    private bool _arenaContext;
    private string _uiKey = "";
    private int _viewed;
    private Button _rewards = null!, _arena = null!;
    private Button _overview = null!;
    public Button SettingsButton { get; private set; } = null!;
    public BuildingInspector Inspector { get; private set; } = null!;
    public DraftPanel Draft { get; private set; } = null!;
    public GameplayDialog Details { get; private set; } = null!;

    public string SessionFeedback { get; private set; } = "";
    public Control Resources => _economy;
    public Button ReadyButton => _controls.ReadyButton;
    internal CitySelector Cities => _cities;
    internal Rect2 ContextArea => _contextBody.GetGlobalRect();
    internal Button ClearSelectionButton => _clear;
    internal Button FeedbackButton => _feedback;
    public bool ShowArena { get; private set; }
    internal void OpenArena() { ShowArena = true; ArenaChanged?.Invoke(); }
    public bool BlocksWorld => Details.Visible || Draft.Visible;
    public Rect2 WorldArea => new(Vector2.Zero, Size);
    public event Action? CommandSubmitted, MenuRequested, ReconnectRequested, ArenaChanged, SettingsRequested;
    public event Action? ArenaOverviewRequested;
    public void Reset()
    {
        _uiKey = ""; _viewed = 0; ShowArena = false;
        _feedbackKey = ""; _recentFeedback.Clear(); _acceptedUntil = 0;
        _layout = null;
        Inspector?.Construction.ResetSelection(); Draft?.Reset(); if (Details is not null) { Details.Close(); Details.ResetSelection(); }
    }
    private void Submitted() => CommandSubmitted?.Invoke();
    private GameplayContext Context => new(_session, Submitted);
    private void OpenDetails(DetailsPage page)
    {
        Draft.Dismiss(false); Details.Open(page, Context, GetViewport().GuiGetFocusOwner()); LayoutHud();
    }
    public void Initialize(HudSkin skin)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Ignore; Theme = skin.Theme;
        _ribbon = new PanelContainer(); AddChild(_ribbon); skin.Panel(_ribbon, "ribbon");
        _ribbon.AddThemeStyleboxOverride("panel", (StyleBox)_ribbon.GetThemeStylebox("panel").Duplicate());
        _ribbonBody = new VBoxContainer(); _ribbonBody.AddThemeConstantOverride("separation", 4); _ribbon.AddChild(_ribbonBody);
        _nav = new HBoxContainer(); _ribbonBody.AddChild(_nav);
        _cities = new CitySelector { CustomMinimumSize = new(220, 44), FitToLongestItem = false, ClipText = true }; _nav.AddChild(_cities); _cities.Initialize();
        _cities.CityChanged += () => { Details.ResetSelection(); _uiKey = ""; Refresh(_session, default!, false, true); };
        _keep = new Label(); _nav.AddChild(_keep);
        _economy = new ResourcePanel(); _nav.AddChild(_economy); _economy.Initialize(skin);
        _ribbonSpacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore }; _nav.AddChild(_ribbonSpacer);
        _phase = WrapLabel(); _phase.CustomMinimumSize = new(260, 0); _nav.AddChild(_phase);
        _arena = skin.Button("Show arena", () => { ShowArena = !ShowArena; ArenaChanged?.Invoke(); }); _nav.AddChild(_arena);
        SettingsButton = skin.Button("Settings", () => SettingsRequested?.Invoke()); _nav.AddChild(SettingsButton);

        _dock = new PanelContainer(); AddChild(_dock); skin.Panel(_dock);
        var column = new VBoxContainer(); _dock.AddChild(column);
        var navigation = new HFlowContainer(); _contextNav = navigation; column.AddChild(navigation);
        var build = skin.Button("Build", () => { Details.Close(); Draft.Dismiss(); LayoutHud(); }); navigation.AddChild(build);
        navigation.AddChild(skin.Button("Worldtree", () => OpenDetails(DetailsPage.Worldtree)));
        navigation.AddChild(skin.Button("Troops & homes", () => OpenDetails(DetailsPage.Army)));
        navigation.AddChild(skin.Button("City details", () => OpenDetails(DetailsPage.City)));
        _clear = skin.Button("×", () => { _session.SelectSlot(-1); Submitted(); }); _clear.CustomMinimumSize = new(44, 44);
        _clear.TooltipText = "Clear plot selection"; navigation.AddChild(_clear);
        foreach (var button in navigation.GetChildren().OfType<Button>())
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            {
                var style = (StyleBox)skin.Theme.GetStylebox(state, "Button").Duplicate();
                style.ContentMarginLeft = style.ContentMarginRight = 12;
                button.AddThemeStyleboxOverride(state, style);
            }
        _contextBody = new ScrollContainer { FollowFocus = true, SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; column.AddChild(_contextBody);
        Inspector = new BuildingInspector { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _contextBody.AddChild(Inspector); Inspector.Initialize(skin, Submitted); Inspector.Compact = true;
        Inspector.ArmyRequested += () => { OpenDetails(DetailsPage.Army); if (_session.SelectedSlot is >= 0 and < 9 && (_session.ViewedCity.Slots[_session.SelectedSlot].Type == Building.TownHall || _session.Snapshot.BuildingCatalog.Any(b => b.Type == _session.ViewedCity.Slots[_session.SelectedSlot].Type && b.Recruits is not null))) Details.Army.ShowManagementFor(_session.SelectedSlot, Context); };
        skin.Ornament(_dock, "branch_sprig", new Vector2(-6, -20), new Vector2(72, 32));

        _action = new PanelContainer(); AddChild(_action); skin.Panel(_action);
        var actions = new VBoxContainer(); _action.AddChild(actions);
        _controls = new MatchControls(); actions.AddChild(_controls); _controls.Initialize(skin, Submitted);
        _overview = skin.Button("Field overview  [F]", () => ArenaOverviewRequested?.Invoke()); actions.AddChild(_overview);
        var links = new HBoxContainer();
        _rewards = skin.Button("Rewards", () => { Details.Close(false); Draft.Open(GetViewport().GuiGetFocusOwner()); Refresh(_session, default!, false, true); }); actions.AddChild(_rewards);
        _feedback = skin.Button("Recent feedback", () => OpenDetails(DetailsPage.City));
        _feedback.AddThemeFontSizeOverride("font_size", 14); _feedback.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        actions.AddChild(links);
        links.AddChild(skin.Button("Menu", () => MenuRequested?.Invoke()));
        var reconnect = skin.Button("↻", () => ReconnectRequested?.Invoke()); reconnect.TooltipText = "Reconnect";
        reconnect.CustomMinimumSize = new(44, 44);
        links.AddChild(reconnect); links.AddChild(_feedback);
        foreach (var button in links.GetChildren().OfType<Button>())
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            {
                var style = (StyleBox)skin.Theme.GetStylebox(state, "Button").Duplicate();
                style.ContentMarginLeft = style.ContentMarginRight = 12;
                button.AddThemeStyleboxOverride(state, style);
            }
        Details = new GameplayDialog(); Details.Initialize(skin, Submitted); AddChild(Details); Details.FallbackFocus = build; Details.Closed += LayoutHud;
        Details.PlotRequested += slot => { _session.SelectSlot(slot); ShowArena = false; ArenaChanged?.Invoke(); Submitted(); };
        Draft = new DraftPanel(); AddChild(Draft); Draft.Initialize(skin, Submitted); Draft.FallbackFocus = build;
        Resized += LayoutHud; LayoutHud();
    }
    private void LayoutHud()
    {
        if (_ribbon is null || Draft is null) return;
        // Move the whole resource row; preserve readable type and every native control.
        float required = _nav.GetChildren().OfType<Control>().Where(c => c != _economy && c != _ribbonSpacer).Sum(c => c.GetCombinedMinimumSize().X)
            + _economy.WideMinimumWidth + 7 * _nav.GetThemeConstant("separation") + 24;
        bool stacked = required > Size.X - 16;
        _economy.SetCompact(stacked);
        var ribbonStyle = _ribbon.GetThemeStylebox("panel");
        ribbonStyle.ContentMarginTop = ribbonStyle.ContentMarginBottom = stacked ? 14 : 16;
        if (stacked && _economy.GetParent() != _ribbonBody) _economy.Reparent(_ribbonBody);
        else if (!stacked && _economy.GetParent() != _nav) { _economy.Reparent(_nav); _nav.MoveChild(_economy, 2); }
        _ribbonSpacer.Visible = stacked;
        bool selected = _session is not null && _session.SelectedSlot >= 0;
        bool construction = selected && Inspector.Construction.Visible && !BlocksWorld && !_arenaContext;
        _contextBody.Visible = selected && !BlocksWorld; _clear.Visible = selected;
        var layout = (Size, _ribbon.GetCombinedMinimumSize(), _action.GetCombinedMinimumSize(), Inspector.GetCombinedMinimumSize(),
            _contextNav.GetCombinedMinimumSize(), selected, construction, BlocksWorld, _arenaContext);
        if (_layout == layout) return;
        _layout = layout;
        _ribbon.Position = new(8, 8); _ribbon.Size = new(Size.X - 16, _ribbon.GetCombinedMinimumSize().Y);
        float actionHeight = Mathf.Max(120, _action.GetCombinedMinimumSize().Y);
        _action.Position = new(Size.X - 368, Size.Y - actionHeight - 8); _action.Size = new(360, actionHeight);
        float width = construction ? Mathf.Min(1100, _action.Position.X - 24) : Mathf.Min(610, _action.Position.X - 24);
        float frame = _dock.GetThemeStylebox("panel").GetMinimumSize().Y + _contextNav.GetCombinedMinimumSize().Y + 10;
        float height = frame + (selected ? Inspector.GetCombinedMinimumSize().Y : 0);
        height = Mathf.Min(height, Size.Y - _ribbon.Size.Y - 32);
        _dock.Visible = !_arenaContext && !BlocksWorld;
        _dock.Position = new(8, Size.Y - height - 8); _dock.Size = new(width, height);
    }
    public void Inspect(UnitState unit, bool preview = false)
    {
        Details.Inspector.Select(unit, preview, _session.Snapshot.Phase); OpenDetails(DetailsPage.Inspection);
    }
    public void Inspect(StructureState structure) { Details.Inspector.Select(structure); OpenDetails(DetailsPage.Inspection); }
    public void Refresh(GodotSession session, RenderOptions options, bool finishingCombat, bool force = false)
    {
        _session = session; var state = session.Snapshot; var city = session.ViewedCity;
        if (_viewed != city.Id) { _viewed = city.Id; Details.ResetSelection(); }
        SessionFeedback = !session.Connected ? "Disconnected — " + session.Status : session.Pending ? "Pending — waiting for remote authority acknowledgment…" : session.LastResult is { } result ? (result.Accepted ? "Accepted: " : "Rejected: ") + result.Message : session.Status;
        string feedbackKey = $"{state.MatchId}:{session.LastResult?.Sequence}:{SessionFeedback}";
        if (_feedbackKey != feedbackKey)
        {
            _feedbackKey = feedbackKey;
            _recentFeedback.Enqueue(SessionFeedback); while (_recentFeedback.Count > 8) _recentFeedback.Dequeue();
            Details.RecentFeedback = _recentFeedback.Reverse().ToArray();
            _acceptedUntil = Time.GetTicksMsec() + 4500;
            _feedback.AddThemeColorOverride("font_color", !session.Connected || session.LastResult is { Accepted: false }
                ? new Color("f0bc9d") : session.Pending ? new Color("efd478") : new Color("f5ecd4"));
        }
        _feedback.Text = !session.Connected ? "Disconnected · details" : session.Pending ? "Pending · details"
            : session.LastResult is { Accepted: false } ? "Rejected · details"
            : session.LastResult is { Accepted: true } && Time.GetTicksMsec() < _acceptedUntil ? "Accepted ✓" : "Recent feedback";
        _feedback.TooltipText = SessionFeedback + "\nOpen city details for recent feedback, doctrine and acquired rewards.";
        _phase.Text = $"Wave {state.Wave}/{state.TotalWaves} · {(state.Phase is Phase.Building or Phase.Preparation ? "Preparation " + state.Turn + "/3" : finishingCombat ? "Combat" : state.Phase.ToString())}";
        _keep.Text = $"Keep {HealthPoints.Format(city.Health)}/{state.Rules.CityHealth}";
        _rewards.Visible = state.Phase == Phase.RewardDraft && !finishingCombat && !session.City.Eliminated;
        _cities.Refresh(session); _economy.Refresh(Context); _controls.Refresh(session, finishingCombat);
        _arena.Text = ShowArena ? "Show settlement" : "Show arena";
        string key = $"{state.MatchId}:{state.Revision}:{_feedbackKey}:{session.ViewedCity.Id}:{session.SelectedSlot}:{Details.Visible}:{Draft.Visible}:{ShowArena}:{finishingCombat}";
        _arenaContext = ShowArena || state.Phase == Phase.Combat || finishingCombat;
        _overview.Visible = _arenaContext;
        if (force || key != _uiKey)
        {
            _uiKey = key;
            Inspector.EmptyHint = _arenaContext ? "Select a combatant to inspect the field." : "Select a clearing to build or manage your settlement.";
            if (!Details.Visible) Inspector.Refresh(Context, _arenaContext ? -1 : session.SelectedSlot);
            Details.Refresh(Context); Draft.Refresh(session, finishingCombat);
            if (Draft.Visible) Details.Close(false);
        }
        LayoutHud();
    }
}
