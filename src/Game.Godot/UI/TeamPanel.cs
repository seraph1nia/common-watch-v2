// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Persistent native team links. Navigation only changes the inspected city.</summary>
public partial class TeamPanel : PanelContainer
{
    private Label _heading = null!, _observer = null!;
    private GridContainer _grid = null!;
    private Button _watch = null!;
    private GodotSession _session = null!;
    private readonly Dictionary<int, Button> _cities = [];
    private TeamOverview? _view;
    private readonly Dictionary<int, (StyleBoxFlat Normal, StyleBoxFlat Hover)> _styles = [];
    public event Action? CityChanged;
    internal IReadOnlyDictionary<int, Button> CityButtons => _cities;
    internal string Heading => _heading.Text;
    internal TeamOverview? Overview => _view;
    internal Button WatchButton => _watch;
    internal bool TargetsFit => GetViewport().GetVisibleRect().Encloses(GetGlobalRect())
        && _cities.Values.All(b => GetGlobalRect().Encloses(b.GetGlobalRect()))
        && (!_watch.Visible || GetGlobalRect().Encloses(_watch.GetGlobalRect()));
    public void Initialize(HudSkin skin)
    {
        Theme = skin.Theme;
        AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("102a29ed"), BorderColor = new Color("9d916a"), BorderWidthLeft = 1, BorderWidthRight = 1,
            BorderWidthTop = 1, BorderWidthBottom = 1, ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 5, ContentMarginBottom = 5,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
        var body = new VBoxContainer(); body.AddThemeConstantOverride("separation", 4); AddChild(body);
        _heading = WrapLabel(); _heading.AddThemeFontSizeOverride("font_size", 13); body.AddChild(_heading);
        _grid = new GridContainer { Columns = 2 }; _grid.AddThemeConstantOverride("h_separation", 4); _grid.AddThemeConstantOverride("v_separation", 4); body.AddChild(_grid);
        _observer = WrapLabel("Keep fallen · current survivors finish. Later waves: observe allies; no new army."); _observer.AddThemeFontSizeOverride("font_size", 12); body.AddChild(_observer);
        _watch = skin.Button("Watch endangered ally", () => { if (_view is { Observer: true }) Navigate(_view.SuggestedCity); }); _watch.AddThemeFontSizeOverride("font_size", 12); body.AddChild(_watch); Hide();
    }
    private void Navigate(int city)
    {
        if (_session is not { HasState: true }) return;
        _session.ViewCity(city); CityChanged?.Invoke();
    }
    public void Reset()
    {
        _view = null; _session = null!;
        foreach (var button in _cities.Values) { _grid.RemoveChild(button); button.QueueFree(); }
        _cities.Clear(); _styles.Clear(); Hide();
    }
    public void Refresh(GodotSession session)
    {
        if (!ReferenceEquals(_session, session)) Reset();
        _session = session; var state = session.Snapshot;
        Visible = state.Players.Length > 1 && state.Phase != Phase.Lobby;
        if (!Visible) return;
        _view = TeamAwareness.Project(state, session.PlayerId, session.Connected, state.Phase == Phase.Combat ? session.Playback.Units() : null);
        _heading.Text = !session.Connected ? "TEAM · last state / reconnecting" : state.Phase is Phase.Victory or Phase.Defeat ? $"TEAM · {state.Phase}" : _view.Observer ? $"TEAM · P{session.PlayerId} observing" : "TEAM · inspect a city";
        if (_view.Paused) _heading.Text += " · PAUSED";
        _observer.Visible = _view.Observer;
        _heading.TooltipText = _view.Explanation;
        foreach (var member in _view.Members)
        {
            if (!_cities.TryGetValue(member.Id, out var button))
            {
                int id = member.Id;
                button = new Button { Name = "TeamCity" + id, CustomMinimumSize = new(0, 52), SizeFlagsHorizontal = SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart };
                button.AddThemeFontSizeOverride("font_size", 12); var owner = session; string match = session.Snapshot.MatchId;
                button.Pressed += () => { if (ReferenceEquals(_session, owner) && owner.Snapshot.MatchId == match && button.IsVisibleInTree()) Navigate(id); }; _grid.AddChild(button); _cities.Add(id, button);
            }
            string warning = member.State == "Fallen" ? "Observer" : member.Endangered ? "LOW KEEP" : member.UnfundedField > 0 ? $"{member.UnfundedField} sit out" : member.UnfundedStored > 0 ? $"{member.UnfundedStored} stored unfed" : member.Field == 0 ? "No field army" : member.Contribution.Length > 0 ? member.Contribution : "Food funded";
            button.Text = $"P{member.Id}{(member.Id == session.PlayerId ? " · you" : "")} {member.State}\nHP {HealthPoints.Format(member.KeepHealth)} · {member.Field} field\n{warning}";
            button.TooltipText = member.Details;
            if (!_styles.TryGetValue(member.Id, out var styles))
            {
                var normal = new StyleBoxFlat { BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
                    ContentMarginLeft = 3, ContentMarginRight = 3, ContentMarginTop = 2, ContentMarginBottom = 2 };
                var hover = (StyleBoxFlat)normal.Duplicate(); hover.BorderColor = new Color("f1dda2");
                button.AddThemeStyleboxOverride("normal", normal); button.AddThemeStyleboxOverride("hover", hover); button.AddThemeStyleboxOverride("pressed", hover);
                styles = (normal, hover); _styles.Add(member.Id, styles);
            }
            styles.Normal.BgColor = new Color(member.State == "Fallen" ? "393b3de8" : member.State == "Ready" || member.State == "Selected" ? "244d43eb" : "182e32ed");
            styles.Hover.BgColor = styles.Normal.BgColor;
            styles.Normal.BorderColor = new Color(member.Id == session.ViewedCity.Id ? "efd478" : member.Endangered || member.UnfundedField > 0 ? "d9947c" : "718f89");
        }
        _watch.Visible = _view.Observer;
        _watch.Text = $"Watch P{_view.SuggestedCity} · living ally"; _watch.TooltipText = _view.Explanation;
    }
}
