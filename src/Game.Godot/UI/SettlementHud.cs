using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class SettlementHud : Control
{
    private const float ExpandedDockHeight = 248;
    private GodotSession _session = null!;
    private Label _phase = null!, _keep = null!, _message = null!, _history = null!;
    private ResourcePanel _economy = null!;
    private CitySelector _cities = null!;
    private MatchControls _controls = null!;
    private PanelContainer _ribbon = null!, _dock = null!, _action = null!, _notice = null!;
    private HBoxContainer _nav = null!;
    private VBoxContainer _ribbonBody = null!;
    private ScrollContainer _contextBody = null!;
    private bool _collapsed, _arenaContext;
    private string _uiKey = "", _lastNotice = "";
    private readonly Queue<string> _notices = new();
    private int _viewed;
    private Button _rewards = null!, _arena = null!, _collapse = null!;
    public Button SettingsButton { get; private set; } = null!;
    public BuildingInspector Inspector { get; private set; } = null!;
    public DraftPanel Draft { get; private set; } = null!;
    public GameplayDialog Details { get; private set; } = null!;
    public Label Resources => _economy.Summary;
    public Label Message => _message;
    public Button ReadyButton => _controls.ReadyButton;
    internal CitySelector Cities => _cities;
    internal Rect2 ContextArea => _contextBody.GetGlobalRect();
    public bool ShowArena { get; private set; }
    internal void OpenArena() { ShowArena = true; ArenaChanged?.Invoke(); }
    public bool BlocksWorld => Details.Visible || Draft.Visible;
    // Framing follows actual native rectangles, including compact-view reflow.
    public Rect2 WorldArea
    {
        get
        {
            float top = _ribbon.Position.Y + _ribbon.Size.Y + 8;
            float bottom = Mathf.Min(_dock.Position.Y, _action.Position.Y) - 8;
            return new Rect2(8, top, Mathf.Max(1, Size.X - 16), Mathf.Max(1, bottom - top));
        }
    }
    public event Action? CommandSubmitted, CameraResetRequested, MenuRequested, ReconnectRequested, ArenaChanged, SettingsRequested;
    public void Reset()
    {
        _uiKey = ""; _viewed = 0; ShowArena = false; _collapsed = false; _notices.Clear(); _lastNotice = "";
        Inspector?.Construction.ResetSelection(); Draft?.Reset(); if (Details is not null) { Details.Close(); Details.ResetSelection(); }
    }
    private void Submitted() => CommandSubmitted?.Invoke();
    private GameplayContext Context => new(_session, Submitted);
    private void OpenDetails(int tab)
    {
        Draft.Dismiss(false); Details.Open(tab, Context, GetViewport().GuiGetFocusOwner()); _notice.MoveToFront(); LayoutHud();
    }
    public void Initialize(HudSkin skin)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Ignore; Theme = skin.Theme;
        _ribbon = new PanelContainer(); AddChild(_ribbon); skin.Panel(_ribbon, "ribbon");
        _ribbonBody = new VBoxContainer(); _ribbon.AddChild(_ribbonBody);
        _nav = new HBoxContainer(); _ribbonBody.AddChild(_nav);
        _cities = new CitySelector { CustomMinimumSize = new(220, 44), FitToLongestItem = false, ClipText = true }; _nav.AddChild(_cities); _cities.Initialize();
        _cities.CityChanged += () => { Details.ResetSelection(); _uiKey = ""; Refresh(_session, default!, false, true); };
        _keep = new Label(); _nav.AddChild(_keep);
        _economy = new ResourcePanel(); _nav.AddChild(_economy); _economy.Initialize(skin, () => OpenDetails(6));
        _phase = WrapLabel(); _phase.CustomMinimumSize = new(145, 0); _nav.AddChild(_phase);
        _arena = skin.Button("Show arena", () => { ShowArena = !ShowArena; ArenaChanged?.Invoke(); }); _nav.AddChild(_arena);
        SettingsButton = skin.Button("Settings", () => SettingsRequested?.Invoke()); _nav.AddChild(SettingsButton);

        _dock = new PanelContainer(); AddChild(_dock); skin.Panel(_dock);
        var column = new VBoxContainer(); _dock.AddChild(column);
        var navigation = new HBoxContainer(); column.AddChild(navigation);
        var build = skin.Button("Build", () => { _collapsed = false; Details.Close(); Draft.Dismiss(); LayoutHud(); }); navigation.AddChild(build);
        foreach (var (name, tab) in new[] { ("Army", 0), ("Worldtree", 2), ("Run", 4) })
            navigation.AddChild(skin.Button(name, () => OpenDetails(tab)));
        _collapse = skin.Button("Collapse", () => { _collapsed = !_collapsed; LayoutHud(); }); navigation.AddChild(_collapse);
        _contextBody = new ScrollContainer { FollowFocus = true, SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; column.AddChild(_contextBody);
        Inspector = new BuildingInspector { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _contextBody.AddChild(Inspector); Inspector.Initialize(skin, Submitted); Inspector.Compact = true;
        Inspector.ExpandRequested += () => OpenDetails(5);
        Inspector.ArmyRequested += () => { OpenDetails(0); Details.Army.ShowManagementFor(_session.SelectedSlot, Context); };
        skin.Ornament(_dock, "branch_sprig", new Vector2(-6, -20), new Vector2(72, 32));

        _action = new PanelContainer(); AddChild(_action); skin.Panel(_action);
        var actions = new VBoxContainer(); _action.AddChild(actions);
        _controls = new MatchControls(); actions.AddChild(_controls); _controls.Initialize(skin, Submitted);
        actions.AddChild(_economy.CompactSummary);
        var links = new HBoxContainer(); actions.AddChild(links);
        links.AddChild(skin.Button("Upkeep details", () => OpenDetails(6)));
        links.AddChild(skin.Button("Battle", () => OpenDetails(3)));
        _rewards = skin.Button("Rewards", () => { Details.Close(false); Draft.Open(GetViewport().GuiGetFocusOwner()); Refresh(_session, default!, false, true); }); links.AddChild(_rewards);
        _notice = new PanelContainer(); AddChild(_notice); skin.Panel(_notice, "hud/tooltip");
        var notices = new HBoxContainer(); _notice.AddChild(notices);
        _message = WrapLabel(); _message.MaxLinesVisible = 2; notices.AddChild(_message);
        notices.AddChild(skin.Button("History", () => OpenDetails(4)));
        Details = new GameplayDialog(); Details.Initialize(skin, Submitted, Inspector, _economy.Summary); AddChild(Details); Details.FallbackFocus = build; Details.Closed += LayoutHud;
        Details.Run.AddChild(skin.Button("Reset camera [R]", () => CameraResetRequested?.Invoke()));
        Details.Run.AddChild(skin.Button("Return to main menu", () => MenuRequested?.Invoke()));
        Details.Run.AddChild(skin.Button("Reconnect to my city", () => ReconnectRequested?.Invoke()));
        _history = WrapLabel(); Details.Run.AddChild(_history);
        Draft = new DraftPanel(); AddChild(Draft); Draft.Initialize(skin, Submitted); Draft.FallbackFocus = build;
        _notice.MoveToFront();
        Resized += LayoutHud; LayoutHud();
    }
    private void LayoutHud()
    {
        if (_ribbon is null || Draft is null) return;
        bool narrow = Size.X < 1600;
        _economy.Reflow(narrow ? Size.X - 48 : Size.X - 700);
        if (narrow && _economy.GetParent() == _nav) _economy.Reparent(_ribbonBody);
        else if (!narrow && _economy.GetParent() != _nav) { _economy.Reparent(_nav); _nav.MoveChild(_economy, 2); }
        _ribbon.Position = new(8, 8); _ribbon.Size = new(Size.X - 16, _ribbon.GetCombinedMinimumSize().Y);
        float footerHeight = Mathf.Max(52, _notice.GetCombinedMinimumSize().Y);
        _notice.Position = new(8, Size.Y - footerHeight - 8); _notice.Size = new(Size.X - 16, footerHeight);
        float actionHeight = Mathf.Max(ExpandedDockHeight, _action.GetCombinedMinimumSize().Y);
        _action.Position = new(Size.X - 450, _notice.Position.Y - actionHeight - 8); _action.Size = new(442, actionHeight);
        bool context = !_collapsed && !BlocksWorld;
        _contextBody.Visible = context;
        _collapse.Text = _collapsed ? "Expand" : "Collapse";
        float height = context && _session is not null && _session.SelectedSlot >= 0 && !_arenaContext ? ExpandedDockHeight : 110;
        _dock.Position = new(8, _notice.Position.Y - height - 8); _dock.Size = new(Size.X - 466, height);
    }
    public void Inspect(UnitState unit, bool preview = false)
    {
        Details.Inspector.Select(unit, preview, _session.Snapshot.Phase); OpenDetails(3);
    }
    public void Inspect(StructureState structure) { Details.Inspector.Select(structure); OpenDetails(3); }
    public void Refresh(GodotSession session, RenderOptions options, bool finishingCombat, bool force = false)
    {
        _session = session; var state = session.Snapshot; var city = session.ViewedCity;
        if (_viewed != city.Id) { _viewed = city.Id; Details.ResetSelection(); }
        _message.Text = !session.Connected ? "Disconnected — " + session.Status : session.Pending ? "Pending — waiting for remote authority acknowledgment…" : session.LastResult is { } result ? (result.Accepted ? "Accepted: " : "Rejected: ") + result.Message : session.Status;
        if (_message.Text != _lastNotice)
        {
            _lastNotice = _message.Text; _notices.Enqueue(_lastNotice); while (_notices.Count > 24) _notices.Dequeue();
            _history.Text = "Recent session feedback\n" + string.Join("\n", _notices.Reverse());
        }
        _phase.Text = $"Wave {state.Wave}/{state.TotalWaves}\n{(state.Phase is Phase.Building or Phase.Preparation ? "Preparation " + state.Turn + "/3" : finishingCombat ? "Combat" : state.Phase.ToString())}";
        _keep.Text = $"Keep\n{HealthPoints.Format(city.Health)}/{state.Rules.CityHealth}";
        _rewards.Visible = state.Phase == Phase.RewardDraft && !finishingCombat && !session.City.Eliminated;
        _cities.Refresh(session); _economy.Refresh(Context); _controls.Refresh(session, finishingCombat);
        _arena.Text = ShowArena ? "Show settlement" : "Show arena";
        string key = $"{state.MatchId}:{state.Revision}:{session.Connected}:{session.Pending}:{session.ViewedCity.Id}:{session.SelectedSlot}:{Details.Visible}:{Draft.Visible}:{ShowArena}:{finishingCombat}";
        _arenaContext = ShowArena || state.Phase == Phase.Combat || finishingCombat;
        if (force || key != _uiKey)
        {
            _uiKey = key;
            Inspector.EmptyHint = _arenaContext ? "Select a combatant or open Battle to inspect the field." : "Select a clearing to build or manage your settlement.";
            if (!Details.Visible) Inspector.Refresh(Context, _arenaContext ? -1 : session.SelectedSlot);
            Details.Refresh(Context); Draft.Refresh(session, finishingCombat);
            if (Draft.Visible) Details.Close(false);
            _notice.MoveToFront();
        }
        LayoutHud();
    }
}
