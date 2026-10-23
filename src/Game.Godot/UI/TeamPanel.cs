// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Compact status, explicit camera context and optional read-only team inspection.</summary>
public partial class TeamPanel : PanelContainer
{
    private Button _heading = null!, _focus = null!, _watch = null!;
    private Label _warnings = null!, _observer = null!, _details = null!;
    private VBoxContainer _disclosure = null!;
    private readonly Dictionary<DetailsPage, Button> _inspectionButtons = [];
    private HBoxContainer _inspect = null!;
    private GridContainer _grid = null!;
    private GodotSession _session = null!;
    private readonly Dictionary<int, Button> _cities = [];
    private readonly Dictionary<int, (StyleBoxFlat Normal, StyleBoxFlat Hover)> _styles = [];
    private TeamOverview? _view;
    private ArenaCameraMode _cameraMode;
    private int _focusCity;
    private bool _arena, _expanded;
    public event Action? CityChanged, FocusRequested;
    public event Action<DetailsPage>? InspectionRequested;
    internal IReadOnlyDictionary<int, Button> CityButtons => _cities;
    internal string Heading => _heading.Text;
    internal string Warnings => _warnings.Text;
    internal TeamOverview? Overview => _view;
    internal Button WatchButton => _watch;
    internal Button FocusButton => _focus;
    internal Button DetailsButton => _heading;
    internal bool Expanded => _expanded;
    internal IReadOnlyDictionary<DetailsPage, Button> InspectionButtons => _inspectionButtons;
    internal bool TargetsFit => GetViewport().GetVisibleRect().Encloses(GetGlobalRect())
        && _cities.Values.All(b => GetGlobalRect().Encloses(b.GetGlobalRect()))
        && new[] { _heading, _focus, _watch }.Where(b => b.IsVisibleInTree()).All(b => GetGlobalRect().Encloses(b.GetGlobalRect()));
    public void Initialize(HudSkin skin)
    {
        Theme = skin.Theme;
        AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("102a29ed"), BorderColor = new Color("9d916a"), BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1, ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 5, ContentMarginBottom = 5,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
        var body = new VBoxContainer(); body.AddThemeConstantOverride("separation", 4); AddChild(body);
        var header = new HBoxContainer(); body.AddChild(header);
        _heading = skin.Button("Team ▾", () => { _expanded = !_expanded; RefreshDetails(); }, "ForestQuietButton");
        _heading.CustomMinimumSize = new(0, 32); _heading.SizeFlagsHorizontal = SizeFlags.ExpandFill; _heading.AddThemeFontSizeOverride("font_size", 12); header.AddChild(_heading);
        _focus = skin.Button("Focus city", () => { if (_focus.IsVisibleInTree()) FocusRequested?.Invoke(); }, "ForestQuietButton");
        _focus.CustomMinimumSize = new(0, 32); _focus.AddThemeFontSizeOverride("font_size", 12); header.AddChild(_focus);
        _grid = new GridContainer { Columns = 3 }; _grid.AddThemeConstantOverride("h_separation", 4); _grid.AddThemeConstantOverride("v_separation", 4); body.AddChild(_grid);
        _warnings = WrapLabel(); _warnings.AddThemeFontSizeOverride("font_size", 12); body.AddChild(_warnings);
        _observer = WrapLabel("Your Keep fell · current survivors finish; later waves exclude your army."); _observer.AddThemeFontSizeOverride("font_size", 12); body.AddChild(_observer);
        _watch = skin.Button("Watch living ally", () => { if (_watch.IsVisibleInTree() && _view is { Observer: true }) Navigate(_view.SuggestedCity); }, "ForestQuietButton");
        _watch.AddThemeFontSizeOverride("font_size", 12); _watch.CustomMinimumSize = new(0, 36); body.AddChild(_watch);
        _disclosure = new VBoxContainer(); body.AddChild(_disclosure);
        var copy = new ScrollContainer { CustomMinimumSize = new(0, 140), HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Reserve, FollowFocus = true };
        _disclosure.AddChild(copy);
        _details = WrapLabel(); _details.SizeFlagsHorizontal = SizeFlags.ExpandFill; _details.AddThemeFontSizeOverride("font_size", 12); copy.AddChild(_details);
        _inspect = new HBoxContainer(); _disclosure.AddChild(_inspect);
        foreach (var (text, page) in new[] { ("Army", DetailsPage.Army), ("Worldtree", DetailsPage.Worldtree) })
        {
            var button = skin.Button(text, () => { if (_disclosure.IsVisibleInTree()) InspectionRequested?.Invoke(page); }, "ForestQuietButton");
            button.SizeFlagsHorizontal = SizeFlags.ExpandFill; _inspect.AddChild(button); _inspectionButtons.Add(page, button);
        }
        Hide(); _disclosure.Hide(); _focus.Hide();
    }
    private void Navigate(int city)
    {
        if (_session is not { HasState: true } || _session.Snapshot.Players.All(p => p.Id != city)) return;
        _session.ViewCity(city); CityChanged?.Invoke();
    }
    public void Reset()
    {
        _view = null; _session = null!; _expanded = false; _arena = false; _cameraMode = ArenaCameraMode.Automatic;
        foreach (var button in _cities.Values) { _grid.RemoveChild(button); button.QueueFree(); }
        _cities.Clear(); _styles.Clear(); _disclosure.Hide(); Hide();
    }
    internal void CameraContext(ArenaCameraMode mode, bool arena, int focusCity)
    {
        if (_cameraMode == mode && _arena == arena && _focusCity == focusCity) return;
        _cameraMode = mode; _arena = arena; _focusCity = focusCity;
        if (_view is not null) RefreshHeading();
    }
    public void Refresh(GodotSession session, TeamOverview? overview = null)
    {
        if (!ReferenceEquals(_session, session)) Reset();
        _session = session; var state = session.Snapshot;
        Visible = state.Players.Length > 1 && state.Phase != Phase.Lobby;
        if (!Visible) return;
        _view = overview ?? TeamAwareness.Project(state, session.PlayerId, session.Connected, state.Phase == Phase.Combat ? session.Playback.Units() : null);
        _grid.Columns = state.Players.Length <= 4 ? 2 : 3;
        RefreshHeading();
        foreach (var member in _view.Members)
        {
            if (!_cities.TryGetValue(member.Id, out var button))
            {
                int id = member.Id; var owner = session; string match = state.MatchId;
                button = new Button { Name = "TeamCity" + id, CustomMinimumSize = new(0, 36), SizeFlagsHorizontal = SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart };
                button.AddThemeFontSizeOverride("font_size", 12);
                button.Pressed += () => { if (ReferenceEquals(_session, owner) && owner.HasState && owner.Snapshot.MatchId == match && button.IsVisibleInTree()) Navigate(id); };
                _grid.AddChild(button); _cities.Add(id, button);
            }
            bool empty = member.State is not ("Fallen" or "Away" or "Victory" or "Defeat") && member.Field == 0 && member.UnfundedField == 0;
            string identity = $"P{member.Id}{(member.Id == session.PlayerId ? " You" : "")}";
            button.Text = state.Players.Length <= 4 ? identity + " · " + member.State + (empty ? state.Phase == Phase.RewardDraft ? "\nNo field survivors" : "\nNo field army" : "")
                : identity + "\n" + member.State;
            button.TooltipText = member.Details + "\nSelect to inspect/focus this city. Select your own P" + session.PlayerId + " to return locally.";
            if (!_styles.TryGetValue(member.Id, out var styles))
            {
                var normal = new StyleBoxFlat { BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                    ContentMarginLeft = 3, ContentMarginRight = 3, ContentMarginTop = 2, ContentMarginBottom = 2 };
                var hover = (StyleBoxFlat)normal.Duplicate(); hover.BorderColor = new Color("f1dda2");
                button.AddThemeStyleboxOverride("normal", normal); button.AddThemeStyleboxOverride("hover", hover); button.AddThemeStyleboxOverride("pressed", hover);
                styles = (normal, hover); _styles.Add(member.Id, styles);
            }
            styles.Normal.BgColor = new Color(member.State == "Fallen" ? "393b3de8" : member.State is "Ready" or "Selected" or "Syncing" ? "244d43eb" : "182e32ed");
            styles.Hover.BgColor = styles.Normal.BgColor;
            styles.Normal.BorderColor = new Color(member.Id == session.ViewedCity.Id ? "efd478" : member.Endangered || member.UnfundedField > 0 ? "d9947c" : "718f89");
        }
        var warnings = new List<string>();
        void Group(string title, Func<TeamMember, bool> include, Func<TeamMember, string>? detail = null)
        {
            var members = _view.Members.Where(include).ToArray();
            if (members.Length > 0) warnings.Add(title + ": " + string.Join(", ", members.Select(m => "P" + m.Id + (detail?.Invoke(m) ?? ""))));
        }
        Group("LOW KEEP", m => m.Endangered, m => " (" + HealthPoints.Format(m.KeepHealth) + ")");
        Group(state.Phase == Phase.RewardDraft ? "Last upkeep · field unfunded" : "Field sit-outs", m => m.State != "Fallen" && m.UnfundedField > 0, m => " (" + m.UnfundedField + ")");
        Group(state.Phase == Phase.RewardDraft ? "Last upkeep · stored unfed" : "Stored unfed", m => m.State != "Fallen" && m.UnfundedStored > 0, m => " (" + m.UnfundedStored + ")");
        if (state.Players.Length > 4 && state.Phase is not (Phase.Victory or Phase.Defeat)) Group(state.Phase == Phase.RewardDraft ? "No field survivors" : "No field army", m => m.State is not ("Fallen" or "Away") && m.Field == 0 && m.UnfundedField == 0);
        _warnings.Text = string.Join("\n", warnings); _warnings.Visible = warnings.Count > 0;
        _observer.Visible = _watch.Visible = _view.Observer;
        _watch.Text = $"Watch P{_view.SuggestedCity} · living ally"; _watch.TooltipText = _view.Explanation;
        RefreshDetails();
    }
    private void RefreshHeading()
    {
        if (_view is null) return;
        var state = _session.Snapshot; int viewed = _session.ViewedCity.Id;
        string text = !_session.Connected ? "Reconnecting · last state"
            : state.Phase is Phase.Victory or Phase.Defeat ? state.Phase.ToString()
            : _arena ? _cameraMode switch { ArenaCameraMode.Manual => $"Manual · P{viewed}", ArenaCameraMode.Overview => "Field overview", _ => $"P{_focusCity} sector" }
            : _view.Boundary.RequiredCities.Length == 0 ? "No connected standing city"
            : state.Phase == Phase.RewardDraft ? $"Choosing {_view.Boundary.Waiting}/{_view.Boundary.RequiredCities.Length}"
            : $"Ready {_view.Boundary.Complete}/{_view.Boundary.RequiredCities.Length}";
        _heading.Text = text + (_view.Paused ? " · PAUSED" : "") + (_expanded ? " ▴" : " ▾");
        _heading.TooltipText = $"Your city P{_session.PlayerId} · inspecting P{viewed}. Expand team details.\n" + _view.Explanation;
        _focus.Visible = _arena && _cameraMode != ArenaCameraMode.Automatic;
        _focus.Text = "Focus P" + viewed; _focus.TooltipText = "Focus the inspected city. R restores that sector; your own city button returns locally.";
    }
    private void RefreshDetails()
    {
        if (_view is null) return;
        RefreshHeading(); _disclosure.Visible = _expanded;
        var selected = _view.Members.Single(m => m.Id == _session.ViewedCity.Id);
        _details.Text = selected.Details + "\n" + _view.Explanation;
        _inspect.Visible = _session.ViewedCity.Id != _session.PlayerId || _view.Observer;
    }
    public override void _ExitTree() { CityChanged = null; FocusRequested = null; InspectionRequested = null; }
}
