using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class SettlementHud : Control
{
    private GodotSession _session = null!;
    private Label _phase = null!, _message = null!;
    private ResourcePanel _economy = null!;
    private CitySelector _cities = null!;
    private MatchControls _controls = null!;
    private string _uiKey = "";
    private int _viewed;
    private Button _rewards = null!;
    private Button _arena = null!;
    public const float SideWidth = 368, HeaderHeight = 200;
    public BuildingInspector Inspector { get; private set; } = null!;
    public DraftPanel Draft { get; private set; } = null!;
    public GameplayDialog Details { get; private set; } = null!;
    public Label Resources => _economy.Summary;
    public Label Message => _message;
    public Button ReadyButton => _controls.ReadyButton;
    internal CitySelector Cities => _cities;
    public bool ShowArena { get; private set; }
    internal void OpenArena() { ShowArena = true; ArenaChanged?.Invoke(); }
    public bool BlocksWorld => Details.Visible || Draft.Visible;
    public event Action? CommandSubmitted, CameraResetRequested, MenuRequested, ReconnectRequested, ArenaChanged;
    public void Reset() { _uiKey = ""; _viewed = 0; ShowArena = false; Draft?.Reset(); if (Details is not null) { Details.Close(); Details.ResetSelection(); } }
    private void Submitted() => CommandSubmitted?.Invoke();
    private GameplayContext Context => new(_session, Submitted);
    public void Initialize(HudSkin skin)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Ignore; Theme = skin.Theme;
        var header = new PanelContainer(); AddChild(header); header.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide); header.OffsetBottom = HeaderHeight; skin.Panel(header);
        var top = new VBoxContainer(); header.AddChild(Margin(top, 10));
        var nav = new HBoxContainer(); top.AddChild(nav); nav.CustomMinimumSize = new(0, 36);
        nav.AddChild(new Label { Text = "THE COMMON WATCH" });
        _cities = new CitySelector(); nav.AddChild(_cities); _cities.Initialize();
        _cities.CityChanged += () => { Details.ResetSelection(); _uiKey = ""; Refresh(_session, default!, false, true); };
        _arena = skin.Button("Arena / settlement", () => { ShowArena = !ShowArena; ArenaChanged?.Invoke(); }); nav.AddChild(_arena);
        foreach (var (name, tab) in new[] { ("Army", 0), ("Homes & towers", 1), ("Worldtree", 2), ("Battle", 3), ("Run", 4) })
        {
            Button button = skin.Button(name, () => { Details.Open(tab, Context); }); nav.AddChild(button);
        }
        _rewards = skin.Button("Rewards", () => { Draft.Open(); Refresh(_session, default!, false, true); }); nav.AddChild(_rewards);
        _phase = WrapLabel(); top.AddChild(_phase);
        _economy = new ResourcePanel(); top.AddChild(_economy); _economy.Initialize();
        var side = new PanelContainer(); AddChild(side); side.SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        side.OffsetLeft = -SideWidth; side.OffsetTop = HeaderHeight + 8; side.OffsetBottom = -60; side.OffsetRight = -8; skin.Panel(side);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; side.AddChild(Margin(scroll, 16));
        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; scroll.AddChild(column);
        _controls = new MatchControls(); column.AddChild(_controls); _controls.Initialize(skin, Submitted);
        column.AddChild(new HSeparator());
        column.AddChild(skin.Button("Expand selected plot details", () => Details.Open(5, Context)));
        Inspector = new BuildingInspector(); column.AddChild(Inspector); Inspector.Initialize(skin, Submitted);
        column.AddChild(skin.Button("Reset camera [R]", () => CameraResetRequested?.Invoke()));
        column.AddChild(skin.Button("Return to main menu", () => MenuRequested?.Invoke()));
        column.AddChild(skin.Button("Reconnect to my city", () => ReconnectRequested?.Invoke()));
        var footer = new PanelContainer(); AddChild(footer); footer.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide); footer.OffsetTop = -52; skin.Panel(footer, "hud/tooltip");
        _message = WrapLabel("Select a plot. Actions are validated by the authority."); footer.AddChild(Margin(_message, 10));
        Details = new GameplayDialog(); Details.Initialize(skin, Submitted); AddChild(Details);
        Draft = new DraftPanel(); AddChild(Draft); Draft.Initialize(skin, Submitted);
    }
    public void Inspect(UnitState unit, bool preview = false)
    {
        Details.Inspector.Select(unit, preview, _session.Snapshot.Phase); Details.Open(3, Context);
    }
    public void Inspect(StructureState structure) { Details.Inspector.Select(structure); Details.Open(3, Context); }
    public void Refresh(GodotSession session, RenderOptions options, bool finishingCombat, bool force = false)
    {
        _session = session; var state = session.Snapshot; var city = session.ViewedCity;
        if (_viewed != city.Id) { _viewed = city.Id; Details.ResetSelection(); }
        _message.Text = !session.Connected ? "Disconnected — " + session.Status : session.Pending ? "Pending — waiting for remote authority acknowledgment…" : session.LastResult is { } result ? (result.Accepted ? "Accepted: " : "Rejected: ") + result.Message : session.Status;
        _phase.Text = $"Wave {state.Wave}/{state.TotalWaves} · {(state.Phase is Phase.Building or Phase.Preparation ? "Preparation " + state.Turn + "/3 — Ready settles one production" : state.Phase.ToString())} · P{city.Id} Keep {HealthPoints.Format(city.Health)}/{state.Rules.CityHealth} · {session.Status}";
        _rewards.Visible = state.Phase == Phase.RewardDraft && !session.City.Eliminated;
        _cities.Refresh(session); _economy.Refresh(Context); _controls.Refresh(session);
        _arena.Text = ShowArena ? "Show settlement" : "Show arena";
        // Snapshot revisions drive ordinary panel updates; native controls retain identity/focus.
        string key = $"{state.MatchId}:{state.Revision}:{session.Connected}:{session.Pending}:{session.ViewedCity.Id}:{session.SelectedSlot}:{Details.Visible}:{finishingCombat}";
        if (!force && key == _uiKey) return; _uiKey = key;
        Inspector.Refresh(session); Details.Refresh(Context); Draft.Refresh(session, finishingCombat);
    }
}
