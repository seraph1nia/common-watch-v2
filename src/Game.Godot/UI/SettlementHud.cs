using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class SettlementHud : Control
{
    private GodotSession _session = null!;
    private Label _phase = null!, _keep = null!;
    private ResourcePanel _economy = null!;
    private TeamPanel _team = null!;
    internal TeamPanel Team => _team;
    private PreparationPanel _preparation = null!;
    private OpeningGuidePanel _opening = null!;
    private bool _openingHomesReviewed;
    internal OpeningGuidePanel OpeningGuide => _opening;
    private readonly BattleOutcomeJournal _outcomes = new();
    public LiveCombatInspector LiveInspector { get; private set; } = null!;
    internal BattleResultStrip Result { get; private set; } = null!;
    private PanelContainer _resultFrame = null!;
    private PanelContainer _combatLegend = null!;
    private bool _combatPresentation;
    private CitySelector _cities = null!;
    private MatchControls _controls = null!;
    private PanelContainer _ribbon = null!, _dock = null!, _action = null!, _palette = null!;
    private ScrollContainer _constructionBody = null!;
    private HBoxContainer _menu = null!;
    private PhaseTrack _phaseTrack = null!;
    private HBoxContainer _nav = null!;
    private VBoxContainer _ribbonBody = null!;
    private ScrollContainer _contextBody = null!;
    private HFlowContainer _contextNav = null!;
    private Button _feedback = null!, _clear = null!, _reconnect = null!;
    private string _feedbackKey = "";
    private double _acceptedUntil;
    private readonly Queue<string> _recentFeedback = new();
    private bool _arenaContext;
    private bool? _wideLayout;
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
    internal Button PauseButton => _controls.PauseButton;
    internal string TransitionText => _controls.TransitionText;
    internal Label PreparationSummary => _preparation.Summary;
    internal Rect2 PreparationArea => _preparation.GetGlobalRect();
    internal Rect2 LedgerArea => _ribbon.GetGlobalRect();
    internal Rect2 PhaseArea => _action.GetGlobalRect();
    internal CitySelector Cities => _cities;
    internal Rect2 ContextArea => Inspector.Construction.IsVisibleInTree() ? _constructionBody.GetGlobalRect() : _contextBody.GetGlobalRect();
    internal Button ClearSelectionButton => _clear;
    internal Button ArenaButton => _arena;
    internal Button FeedbackButton => _feedback;
    internal Button ReconnectButton => _reconnect;
    public bool ShowArena { get; private set; }
    internal void OpenArena() { ShowArena = true; ArenaChanged?.Invoke(); }
    public bool BlocksWorld => Details.Visible || Draft.Visible;
    public Rect2 WorldArea => new(Vector2.Zero, Size);
    public Rect2 SettlementFramingArea => new(new Vector2(0, 24), new Vector2(Size.X - (Size.X >= 1600 ? 420 : 382), Size.Y - 108));
    public event Action? CommandSubmitted, MenuRequested, ReconnectRequested, ArenaChanged, SettingsRequested;
    public event Action? ArenaOverviewRequested;
    public void Reset()
    {
        _team?.Reset();
        _openingHomesReviewed = false; _opening?.Hide();
        _outcomes.Reset(); LiveInspector?.Close(false); Result?.Refresh(null);
        _uiKey = ""; _viewed = 0; ShowArena = false;
        _feedbackKey = ""; _recentFeedback.Clear(); _acceptedUntil = 0;
        Inspector?.Construction.ResetSelection(); Draft?.Reset(); if (Details is not null) { Details.Close(); Details.ResetSelection(); }
    }
    private void Submitted() => CommandSubmitted?.Invoke();
    private GameplayContext Context => new(_session, Submitted);
    private void OpenDetails(DetailsPage page)
    {
        LiveInspector.Close(false);
        Draft.Dismiss(false); Details.Open(page, Context, GetViewport().GuiGetFocusOwner()); LayoutHud();
    }
    public void Initialize(HudSkin skin)
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); MouseFilter = MouseFilterEnum.Ignore; Theme = skin.Theme;
        _ribbon = new PanelContainer(); AddChild(_ribbon); skin.Panel(_ribbon, "ribbon");
        _ribbon.AddThemeStyleboxOverride("panel", (StyleBox)_ribbon.GetThemeStylebox("panel").Duplicate());
        _ribbonBody = new VBoxContainer(); _ribbonBody.AddThemeConstantOverride("separation", 4); _ribbon.AddChild(_ribbonBody);
        _nav = new HBoxContainer(); _ribbonBody.AddChild(_nav);
        _cities = new CitySelector { CustomMinimumSize = new(200, 44), SizeFlagsHorizontal = SizeFlags.ExpandFill, FitToLongestItem = false, ClipText = true }; _nav.AddChild(_cities); _cities.Initialize();
        _cities.CityChanged += () => { Details.ResetSelection(); _uiKey = ""; Refresh(_session, default!, false, true); };
        _keep = new Label(); _keep.AddThemeFontSizeOverride("font_size", 14); _ribbonBody.AddChild(_keep);
        _economy = new ResourcePanel(); _ribbonBody.AddChild(_economy); _economy.Initialize(skin);
        _team = new TeamPanel(); AddChild(_team); _team.Initialize(skin);
        _team.CityChanged += () => { Details.ResetSelection(); LiveInspector.Close(false); _uiKey = ""; Refresh(_session, default!, _combatPresentation, true); };
        _preparation = new PreparationPanel(); AddChild(_preparation); _preparation.Initialize(skin);
        _opening = new OpeningGuidePanel(); AddChild(_opening); _opening.Initialize(skin); _opening.Navigate += NavigateOpening;
        _menu = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore }; AddChild(_menu);
        SettingsButton = skin.Button("Settings", () => SettingsRequested?.Invoke()); _menu.AddChild(SettingsButton);
        SettingsButton.CustomMinimumSize = new(126, 48);
        _menu.AddChild(new Control { CustomMinimumSize = new(44, 0), MouseFilter = MouseFilterEnum.Ignore });
        _menu.AddChild(skin.Button("Menu", () => MenuRequested?.Invoke()));

        _dock = new PanelContainer(); AddChild(_dock); skin.Panel(_dock);
        var column = new VBoxContainer(); _dock.AddChild(column);
        var dismiss = new HBoxContainer(); column.AddChild(dismiss);
        var selectedHeading = new Label { Text = "Selected", SizeFlagsHorizontal = SizeFlags.ExpandFill }; dismiss.AddChild(selectedHeading);
        _clear = skin.Button("×", () => { _session.SelectSlot(-1); Submitted(); }); _clear.CustomMinimumSize = new(44, 44);
        _clear.TooltipText = "Clear plot selection · expose every clearing"; dismiss.AddChild(_clear);
        _palette = new PanelContainer(); AddChild(_palette); skin.Panel(_palette);
        if (skin.Painted is not null)
        {
            var paletteFrame = _palette.GetThemeStylebox("panel");
            paletteFrame.ContentMarginLeft = paletteFrame.ContentMarginRight = 20;
            paletteFrame.ContentMarginTop = 20; paletteFrame.ContentMarginBottom = 22;
        }
        var paletteColumn = new VBoxContainer(); _palette.AddChild(paletteColumn);
        var navigation = new HFlowContainer(); _contextNav = navigation; paletteColumn.AddChild(navigation);
        var build = skin.Button("Build", () => { Details.Close(); Draft.Dismiss(); LayoutHud(); }); navigation.AddChild(build);
        navigation.AddChild(skin.Button("Worldtree", () => OpenDetails(DetailsPage.Worldtree)));
        navigation.AddChild(skin.Button("Troops & homes", () => OpenDetails(DetailsPage.Army)));
        navigation.AddChild(skin.Button("City details", () => OpenDetails(DetailsPage.City), skin.Painted is null ? "" : "PaintedDetails"));
        foreach (var button in navigation.GetChildren().OfType<Button>())
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            {
                var style = (StyleBox)skin.Theme.GetStylebox(state, button.ThemeTypeVariation.IsEmpty ? "Button" : button.ThemeTypeVariation).Duplicate();
                style.ContentMarginLeft = style.ContentMarginRight = 12;
                button.AddThemeStyleboxOverride(state, style);
            }
        _contextBody = new ScrollContainer { FollowFocus = true, SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; column.AddChild(_contextBody);
        Inspector = new BuildingInspector { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _contextBody.AddChild(Inspector); Inspector.Initialize(skin, Submitted); Inspector.Compact = true;
        _constructionBody = new ScrollContainer { FollowFocus = true, SizeFlagsVertical = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; paletteColumn.AddChild(_constructionBody);
        Inspector.Construction.Reparent(_constructionBody); Inspector.Construction.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        Inspector.ArmyRequested += () => { OpenDetails(DetailsPage.Army); if (_session.SelectedSlot is >= 0 and < 9 && (_session.ViewedCity.Slots[_session.SelectedSlot].Type == Building.TownHall || _session.Snapshot.BuildingCatalog.Any(b => b.Type == _session.ViewedCity.Slots[_session.SelectedSlot].Type && b.Recruits is not null))) Details.Army.ShowManagementFor(_session.SelectedSlot, Context); };
        skin.Ornament(_dock, "branch_sprig", new Vector2(-6, -20), new Vector2(72, 32));

        _action = new PanelContainer(); AddChild(_action); skin.Panel(_action);
        var actions = new VBoxContainer(); _action.AddChild(actions);
        _phase = WrapLabel(); _phase.AddThemeFontSizeOverride("font_size", 18); actions.AddChild(_phase);
        _phaseTrack = new PhaseTrack { Illustrated = skin.Painted is not null }; actions.AddChild(_phaseTrack);
        _overview = skin.Button("Field overview  [F]", () => ArenaOverviewRequested?.Invoke()); actions.AddChild(_overview);
        var links = new HBoxContainer();
        _rewards = skin.Button("Rewards", () => { Details.Close(false); Draft.Open(GetViewport().GuiGetFocusOwner()); Refresh(_session, default!, false, true); }); actions.AddChild(_rewards);
        _feedback = skin.Button("Recent feedback", () => OpenDetails(DetailsPage.City), "ForestQuietButton");
        _feedback.ClipText = true;
        _feedback.AddThemeFontSizeOverride("font_size", 14); _feedback.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        actions.AddChild(links);
        _arena = skin.Button("Show arena", () => { ShowArena = !ShowArena; ArenaChanged?.Invoke(); }, "ForestQuietButton"); links.AddChild(_arena);
        var reconnect = skin.Button("↻", () => ReconnectRequested?.Invoke(), "ForestQuietButton"); _reconnect = reconnect; reconnect.TooltipText = "Reconnect";
        reconnect.CustomMinimumSize = new(44, 44);
        links.AddChild(reconnect); links.AddChild(_feedback);
        foreach (var button in links.GetChildren().OfType<Button>())
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            {
                var style = (StyleBox)skin.Theme.GetStylebox(state, button.ThemeTypeVariation.IsEmpty ? "Button" : button.ThemeTypeVariation).Duplicate();
                style.ContentMarginLeft = style.ContentMarginRight = 12;
                button.AddThemeStyleboxOverride(state, style);
            }
        _controls = new MatchControls(); actions.AddChild(_controls); _controls.Initialize(skin, Submitted);
        if (skin.Painted is { } art)
        {
            art.Attach(SettingsButton, "owl", new(58, 54), Vector2.One, new(8, -5), new(.5f, .95f));
            art.Attach(_menu, "mushrooms-small", new(50, 66), Vector2.Zero, new(-4, 48));
            art.Attach(_ribbon, "rabbit", new(60, 54), new(1, 0), new(-48, 24), new(.5f, .95f));
            art.Attach(_ribbon, "crescent", new(50, 50), Vector2.Zero, new(20, 30), new(.5f, .5f));
            art.Attach(_ribbon, "ivy", new(70, 100), Vector2.Zero, new(-14, 84));
            art.Attach(_ribbon, "lantern-glow", new(86, 104), new(0, .4f), new(-10, 0), new(.5f, .5f));
            art.Attach(_ribbon, "lantern", new(36, 88), new(0, .4f), new(-4, 24));
            art.Attach(_ribbon, "crystals", new(44, 64), new(0, 1), new(-6, -20));
            art.Attach(_ribbon, "mushrooms-right", new(62, 74), Vector2.One, new(-8, -10));
            art.Attach(_dock, "mushrooms-left", new(68, 90), new(0, 1), new(4, -6));
            art.Attach(_palette, "mushrooms-left", new(60, 80), new(0, 1), new(0, -8));
            art.Attach(_palette, "mushrooms-right", new(60, 80), Vector2.One, new(0, -8));
            art.Attach(_palette, "separator", new(128, 24), new(.5f, 0), new(0, 24));
            art.Attach(_action, "squirrel", new(66, 66), new(1, 0), new(-40, 38), new(.5f, .95f));
            art.Attach(_action, "clock", new(44, 44), Vector2.Zero, new(22, 26), new(.5f, .5f));
            art.Attach(_action, "mushrooms-small", new(54, 60), Vector2.One, new(-6, -10));
        }
        Details = new GameplayDialog(); Details.Initialize(skin, Submitted); AddChild(Details); Details.FallbackFocus = build; Details.Closed += LayoutHud;
        Details.PlotRequested += slot => { _session.SelectSlot(slot); ShowArena = false; ArenaChanged?.Invoke(); Submitted(); };
        Draft = new DraftPanel(); AddChild(Draft); Draft.Initialize(skin, Submitted); Draft.FallbackFocus = build;
        LiveInspector = new LiveCombatInspector(); AddChild(LiveInspector); LiveInspector.Initialize(skin); LiveInspector.FallbackFocus = _overview;
        _resultFrame = new PanelContainer(); AddChild(_resultFrame); skin.Panel(_resultFrame);
        Result = new BattleResultStrip { Compact = true }; _resultFrame.AddChild(Result); Result.Initialize(skin); _resultFrame.Hide();
        _combatLegend = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore }; AddChild(_combatLegend);
        _combatLegend.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(.025f, .045f, .06f, .88f), ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 4, ContentMarginBottom = 4 });
        var legend = WrapLabel("F frontline · R ranged · M magic\nP# allies · E→# foes · ! low health\nGold: selected · orange: target"); legend.AddThemeFontSizeOverride("font_size", 14); legend.MouseFilter = MouseFilterEnum.Ignore;
        _combatLegend.AddChild(legend); _combatLegend.Hide();
        Resized += LayoutHud; LayoutHud();
    }
    private void LayoutHud()
    {
        if (_ribbon is null || Draft is null) return;
        bool selected = _session is not null && _session.SelectedSlot >= 0;
        bool construction = selected && Inspector.Construction.Visible && !BlocksWorld && !_arenaContext;
        _contextBody.Visible = selected && !BlocksWorld;
        _constructionBody.Visible = construction;
        _clear.Visible = selected;
        _dock.Visible = selected && !_arenaContext && !BlocksWorld;
        _palette.Visible = !_arenaContext && !BlocksWorld;
        bool wide = Size.X >= 1600;
        float edge = wide ? 24 : 12;
        float ledgerWidth = wide ? 380 : 350;
        _menu.Position = new(edge + 12, edge + 8); _menu.Size = new(280, 48);
        _ribbon.Position = new(Size.X - ledgerWidth - edge, wide ? 60 : 32);
        _ribbon.Size = new(ledgerWidth, _ribbon.GetCombinedMinimumSize().Y);
        if (_wideLayout != wide) { _wideLayout = wide; _phase.AddThemeFontSizeOverride("font_size", wide ? 18 : 16); }
        float actionWidth = wide ? 380 : 350;
        float actionHeight = _action.GetCombinedMinimumSize().Y;
        _action.Size = new(actionWidth, actionHeight);
        _action.Position = new(Size.X - actionWidth - edge, Size.Y - actionHeight - edge);
        // The upper map contains a purchasable clearing. Keep the summary in the
        // left scenery margin and yield it to selected-plot management below.
        float preparationWidth = wide ? 380 : 276;
        _team.Position = new(edge, 92); _team.Size = new(preparationWidth, _team.GetCombinedMinimumSize().Y);
        _preparation.Position = new(edge, _team.Visible ? _team.GetGlobalRect().End.Y + 8 : 92);
        _preparation.Size = new(preparationWidth, _preparation.GetCombinedMinimumSize().Y);
        float guideLeft = _menu.Position.X + _menu.Size.X + 8;
        _opening.Layout(new(guideLeft, 12), Mathf.Min(940, _ribbon.Position.X - guideLeft - 12), 84);
        LiveInspector.Layout(Size, _team.Visible ? _team.GetGlobalRect().End.Y + 8 : 92);
        _resultFrame.Position = new(edge, _team.Visible ? _team.GetGlobalRect().End.Y + 8 : 92);
        _resultFrame.Size = new(wide ? 380 : 310, _resultFrame.GetCombinedMinimumSize().Y);
        _combatLegend.Position = new(edge, Size.Y - 92); _combatLegend.Size = new(310, 80);
        float inspectorWidth = wide ? 330 : 264;
        float inspectorHeight = Mathf.Min(_dock.GetThemeStylebox("panel").GetMinimumSize().Y + 52 + Inspector.GetCombinedMinimumSize().Y, Size.Y * .50f);
        if (_team.Visible) inspectorHeight = Mathf.Min(inspectorHeight, Size.Y - _team.GetGlobalRect().End.Y - edge - 8);
        _dock.Size = new(inspectorWidth, inspectorHeight);
        _dock.Position = new(edge, Size.Y - inspectorHeight - edge);
        // Empty-plot construction owns its own retained native scroll area. The collapsed
        // navigation stays shallow; clear-selection removes both context surfaces.
        float left = construction ? edge + inspectorWidth + 12 : wide ? 396 : 280;
        float right = _action.Position.X - 12;
        float paletteWidth = Mathf.Max(440, right - left);
        float paletteHeight = _palette.GetThemeStylebox("panel").GetMinimumSize().Y + _contextNav.GetCombinedMinimumSize().Y
            + (construction ? Inspector.Construction.GetCombinedMinimumSize().Y + 8 : 0);
        if (construction) paletteHeight = Mathf.Min(paletteHeight + 8, Size.Y - 180);
        _palette.Size = new(paletteWidth, paletteHeight);
        _palette.Position = new(left, Size.Y - paletteHeight - edge);
    }

    public void Inspect(UnitState unit, bool preview = false)
    {
        if (!preview && (_session.Snapshot.Phase == Phase.Combat || _combatPresentation))
        { Details.Close(false); LiveInspector.Inspect(unit, _session); return; }
        Details.Inspector.Select(unit, preview, _session.Snapshot.Phase); OpenDetails(DetailsPage.Inspection);
    }
    public void Inspect(StructureState structure)
    {
        if (_session.Snapshot.Phase == Phase.Combat || _combatPresentation)
        { Details.Close(false); LiveInspector.Inspect(structure, _session); return; }
        Details.Inspector.Select(structure); OpenDetails(DetailsPage.Inspection);
    }
    public void Refresh(GodotSession session, RenderOptions options, bool finishingCombat, bool force = false)
    {
        _session = session; var state = session.Snapshot; var city = session.ViewedCity;
        _combatPresentation = state.Phase == Phase.Combat || finishingCombat;
        _outcomes.Observe(state);
        LiveInspector.Refresh(session, _combatPresentation && !BlocksWorld);
        Result.Refresh(_outcomes.For(city.Id));
        Details.LastOutcome = _outcomes.For(city.Id);
        _resultFrame.Visible = !_combatPresentation && state.Phase is (Phase.RewardDraft or Phase.Victory or Phase.Defeat) && !BlocksWorld && Result.Visible;
        _combatLegend.Visible = _combatPresentation && !BlocksWorld;
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
        _reconnect.Visible = !session.Connected;
        _feedback.TooltipText = SessionFeedback + "\nOpen city details for recent feedback, doctrine and acquired rewards.";
        _phase.Text = $"Wave {state.Wave}/{state.TotalWaves} · {(state.Phase is Phase.Building or Phase.Preparation ? "Preparation " + state.Turn + "/3" : finishingCombat ? "Combat" : state.Phase.ToString())}";
        _phaseTrack.Refresh(state, finishingCombat);
        _keep.Text = $"Keep {HealthPoints.Format(city.Health)}/{state.Rules.CityHealth}";
        _rewards.Visible = state.Phase == Phase.RewardDraft && !finishingCombat && !session.City.Eliminated;
        _team.Refresh(session);
        _cities.Refresh(session); _economy.Refresh(Context); _controls.Refresh(session, finishingCombat);
        _preparation.Refresh(Context, finishingCombat);
        _preparation.Visible &= !BlocksWorld && !ShowArena && session.SelectedSlot < 0;
        if (Details.Visible && Details.Army.IsVisibleInTree() && Details.Army.HomesVisible && city.Id == session.PlayerId) _openingHomesReviewed = true;
        _opening.Refresh(state, city, _openingHomesReviewed, _outcomes.For(city.Id));
        _opening.Visible &= !BlocksWorld && !ShowArena && session.SelectedSlot < 0 && city.Id == session.PlayerId;
        _arena.Text = ShowArena ? "Show settlement" : "Show arena";
        string key = $"{state.MatchId}:{state.Revision}:{_feedbackKey}:{session.ViewedCity.Id}:{session.SelectedSlot}:{Details.Visible}:{Draft.Visible}:{ShowArena}:{finishingCombat}";
        _arenaContext = ShowArena || state.Phase == Phase.Combat || finishingCombat;
        _overview.Visible = _arenaContext;
        if (force || key != _uiKey)
        {
            _uiKey = key;
            Inspector.EmptyHint = _arenaContext ? "Select a combatant to inspect the field." : "Select a clearing to build or manage your settlement.";
            if (!Details.Visible) Inspector.Refresh(Context, _arenaContext ? -1 : session.SelectedSlot);
            Details.Refresh(Context); Draft.Refresh(session, finishingCombat, _outcomes.For(session.PlayerId));
            if (Draft.Visible) Details.Close(false);
        }
        LayoutHud();
    }
    private void NavigateOpening(Game.Shared.OpeningDestination destination)
    {
        if (_session is not { HasState: true } || _session.ViewedCity.Id != _session.PlayerId) return;
        if (destination == Game.Shared.OpeningDestination.Build)
        {
            int slot = Array.FindIndex(_session.City.Slots, s => s.Purchased && s.Type == Building.Empty);
            if (slot < 0) slot = Array.FindIndex(_session.City.Slots, s => s.Type == Building.Farm);
            if (slot >= 0)
            {
                bool recruiter = _session.City.Slots.Any(s => _session.Snapshot.BuildingCatalog.FirstOrDefault(b => b.Type == s.Type)?.Recruits is not null);
                int category = !recruiter && _session.City.Slots.Any(s => s.Type == Building.Farm) && _session.City.Slots.Any(s => s.Type == Building.Lumbermill) ? 1 : 0;
                _session.SelectSlot(slot); Submitted();
                if (Inspector.Construction.IsVisibleInTree())
                {
                    Inspector.Construction.Categories[category].EmitSignal(BaseButton.SignalName.Pressed);
                    Inspector.Construction.Categories[category].CallDeferred(Control.MethodName.GrabFocus);
                }
                else Inspector.Actions.Values.FirstOrDefault(b => b.IsVisibleInTree() && !b.Disabled)?.CallDeferred(Control.MethodName.GrabFocus);
            }
        }
        else if (destination == Game.Shared.OpeningDestination.Outcome) OpenDetails(DetailsPage.City);
        else
        {
            OpenDetails(DetailsPage.Army);
            if (destination == Game.Shared.OpeningDestination.Homes) { _openingHomesReviewed = true; Details.Army.ShowHomes(); }
            else
            {
                int trainer = Array.FindIndex(_session.City.Slots, s => _session.Snapshot.BuildingCatalog.FirstOrDefault(b => b.Type == s.Type)?.Recruits is not null);
                if (trainer >= 0) Details.Army.ShowManagementFor(trainer, Context);
            }
        }
    }
}
