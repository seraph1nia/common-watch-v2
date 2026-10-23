// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>One owner for spatial selection, recruitment and optional member/storage depth.</summary>
public partial class ArmyRosterPanel : VBoxContainer
{
    private readonly ArmySelection _selection = new();
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private Label _summary = null!, _notice = null!, _trainerMissing = null!, _selectedHome = null!, _placementReason = null!, _info = null!;
    private OptionButton _trainer = null!, _halls = null!;
    private RecruitmentPanel _recruitment = null!;
    private TownHallPanel _hall = null!;
    private ActionList _members = null!;
    private ScrollContainer _memberScroll = null!;
    private Button _memberToggle = null!, _preview = null!, _deploy = null!;
    private TabContainer _details = null!;
    private int _recruitSquad = -1;
    private bool _refreshing;
    private DeployIntent? _armed;
    private sealed record DeployIntent(object Session, string Match, int City, int Home, int Squad, int Cell, int Turn);
    public DeploymentPanel Deployment { get; private set; } = null!;
    public BuildingInspector HomeInspector { get; private set; } = null!;
    public UnitInspector MemberInspector { get; private set; } = null!;
    public int SelectedSquad => _selection.Squad;
    public bool HomesVisible => IsVisibleInTree();
    public event Action? PreviewRequested;
    public event Action<int>? PlotRequested;
    public IReadOnlyDictionary<string, Button> MemberButtons => _members.Buttons;
    internal RecruitmentPanel Recruitment => _recruitment;
    internal ScrollContainer RosterScroll => _memberScroll;
    internal Control HomeViewport => Deployment;
    internal Button DeployButton => _deploy;
    internal Button PreviewButton => _preview;
    internal OptionButton TrainerSelector => _trainer;
    internal string Notice => _notice.Text;
    internal bool DetailTabsFit => Enumerable.Range(0, _details.GetTabCount()).All(i =>
    {
        var bar = _details.GetTabBar(); var rect = bar.GetTabRect(i);
        return bar.GetGlobalRect().Encloses(new Rect2(bar.GlobalPosition + rect.Position, rect.Size));
    });
    internal bool PlacementLocked => _context is null || _context.EditReason.Length > 0;

    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; AddThemeConstantOverride("separation", 6);
        _summary = WrapLabel(); _summary.AddThemeFontSizeOverride("font_size", 14); AddChild(_summary);
        var row = new HBoxContainer { SizeFlagsVertical = SizeFlags.ExpandFill }; row.AddThemeConstantOverride("separation", 16); AddChild(row);
        var board = new VBoxContainer { CustomMinimumSize = new(510, 0), SizeFlagsHorizontal = SizeFlags.ExpandFill }; row.AddChild(board);
        Deployment = new DeploymentPanel(); board.AddChild(Deployment); Deployment.Initialize(skin); Deployment.HomeSelected += SelectHome;
        _preview = skin.Button("Preview actual field", () => { if (_preview.IsVisibleInTree()) PreviewRequested?.Invoke(); }, "ForestQuietButton"); board.AddChild(_preview);
        _memberToggle = skin.Button("Members ▾", () => { _memberScroll.Visible = !_memberScroll.Visible; }, "ForestQuietButton"); board.AddChild(_memberToggle);
        _memberScroll = new ScrollContainer { CustomMinimumSize = new(0, 90), SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Reserve, FollowFocus = true }; board.AddChild(_memberScroll);
        _members = new ActionList { SizeFlagsHorizontal = SizeFlags.ExpandFill }; _memberScroll.AddChild(_members); _memberScroll.Hide();
        var context = new VBoxContainer { CustomMinimumSize = new(390, 0), SizeFlagsHorizontal = SizeFlags.Fill, SizeFlagsVertical = SizeFlags.ExpandFill }; row.AddChild(context);
        _notice = WrapLabel(); _notice.AddThemeFontSizeOverride("font_size", 14); context.AddChild(_notice);
        _selectedHome = WrapLabel(); _selectedHome.AddThemeFontSizeOverride("font_size", 18); context.AddChild(_selectedHome);
        _deploy = skin.Button("Move selected squad", Deploy, "ForestPrimaryButton"); context.AddChild(_deploy); _deploy.ButtonDown += ArmDeployment;
        _placementReason = WrapLabel(); _placementReason.AddThemeFontSizeOverride("font_size", 14); context.AddChild(_placementReason);
        _trainer = new OptionButton { FitToLongestItem = false, ClipText = true }; context.AddChild(_trainer); _trainer.ItemSelected += TrainerChanged;
        _recruitment = new RecruitmentPanel(); context.AddChild(_recruitment); _recruitment.Initialize(skin, compact: true);
        _recruitment.DestinationChanged += RecruitDestinationChanged; _recruitment.DetailsRequested += ShowDetails;
        _trainerMissing = WrapLabel("Build a trainer in the settlement to recruit here."); context.AddChild(_trainerMissing);
        _details = new TabContainer { CustomMinimumSize = new(0, 110), SizeFlagsVertical = SizeFlags.ExpandFill }; context.AddChild(_details);
        // TabContainer owns the theme forwarded to its internal TabBar.
        _details.AddThemeFontSizeOverride("font_size", 14);
        foreach (var (tab, button) in new[] { ("tab_selected", "pressed"), ("tab_unselected", "normal"), ("tab_hovered", "hover"), ("tab_disabled", "disabled"), ("focus", "focus") })
            _details.AddThemeStyleboxOverride(tab, skin.Theme.GetStylebox(button, skin.Illustrated ? "ForestQuietButton" : "Button"));
        var home = DetailPage("Home"); HomeInspector = new BuildingInspector { Compact = true, SharedNotice = true, SizeFlagsHorizontal = SizeFlags.ExpandFill }; home.AddChild(HomeInspector); HomeInspector.Initialize(skin, submitted);
        var member = DetailPage("Member"); MemberInspector = new UnitInspector { SizeFlagsHorizontal = SizeFlags.ExpandFill }; member.AddChild(MemberInspector); MemberInspector.Initialize(skin); MemberInspector.PlotRequested += ForwardPlot;
        var storage = DetailPage("Storage"); var storageBody = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; storage.AddChild(storageBody);
        _halls = new OptionButton { FitToLongestItem = false, ClipText = true }; storageBody.AddChild(_halls); _halls.ItemSelected += HallChanged;
        _hall = new TownHallPanel(); storageBody.AddChild(_hall); _hall.Initialize(skin);
        var information = DetailPage("Quotes"); _info = WrapLabel(); information.AddChild(_info);
    }
    private ScrollContainer DetailPage(string name)
    {
        var page = new ScrollContainer { Name = name, FollowFocus = true, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Reserve };
        _details.AddChild(page); return page;
    }
    public void SelectHome(int plot) { CancelIntents(); _selection.SelectHome(plot); _details.CurrentTab = 0; Refresh(_context); }
    public void SelectSquad(int squad) { CancelIntents(); _selection.SelectSquad(squad); Refresh(_context); }
    public void ShowHomes() { _details.CurrentTab = 0; FocusHome(); }
    public void ShowRoster() { _memberScroll.Show(); }
    public void ShowManagement() { _details.CurrentTab = 2; }
    public void ShowManagementFor(int slot, GameplayContext context)
    {
        Refresh(context);
        var selector = context.City.Slots[slot].Type == Building.TownHall ? _halls : _trainer;
        int index = selector.GetItemIndex(slot); if (index >= 0) selector.Select(index);
        if (selector == _halls) _details.CurrentTab = 2;
        Refresh(context);
    }
    private void SelectMember(int id)
    {
        CancelIntents(); _selection.SelectMember(id); _details.CurrentTab = 1; Refresh(_context);
    }
    private void TrainerChanged(long _) { if (!_refreshing) Refresh(_context); }
    private void HallChanged(long _) { if (!_refreshing) Refresh(_context); }
    private void RecruitDestinationChanged(int squad) { if (squad > 0) SelectSquad(squad); }
    private void ShowDetails(string text) { _info.Text = text; }
    private void ForwardPlot(int slot) => PlotRequested?.Invoke(slot);
    public void CancelIntents() { _armed = null; HomeInspector?.Construction.ResetSelection(); MemberInspector?.CancelRetirement(); }
    public void ResetSelection() { CancelIntents(); _selection.Reset(); _recruitSquad = -1; _memberScroll?.Hide(); }
    private void FocusHome()
    {
        if (Deployment.HomeButtons.TryGetValue("Home" + _selection.Home, out var button) && button.IsVisibleInTree()) button.GrabFocus();
    }
    public void Refresh(GameplayContext context)
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            var previousFocus = GetViewport().GuiGetFocusOwner(); bool ownedFocus = previousFocus is not null && IsAncestorOf(previousFocus);
            _context = context; var city = context.City; ShareRestriction(this, context.EditReason);
            _selection.Observe(context.Session, context.State, city.Id);
            Deployment.Refresh(context, _selection.Home, _selection.Squad);
            _summary.Text = $"P{city.Id} ARMY · {city.Soldiers.Count(u => u.Assignment is { Stored: false })} field / {city.Soldiers.Count(u => u.Assignment is { Stored: true })} stored · {city.Army?.PurchasedHomes}/6 homes";
            _notice.Text = context.EditReason.Length > 0 ? context.EditReason : context.Session.LastResult is { Accepted: false } rejected ? "Rejected: " + rejected.Message : "";
            _notice.Visible = _notice.Text.Length > 0;
            var home = Deployment.Homes.FirstOrDefault(h => h.Home.PlotId == _selection.Home);
            _selectedHome.Text = home is null ? "Select an army home" : $"Home {home.Home.Number} · {home.Occupancy}";
            bool current = home?.Members.Any(u => u.SquadId == _selection.Squad) == true;
            string blocked = context.Block(reason: home?.Restriction ?? "Select a home.");
            _deploy.Visible = _selection.Squad > 0 && !current;
            _deploy.Disabled = blocked.Length > 0;
            _deploy.Text = home is null ? "Move selected squad" : $"Move selected squad → Home {home.Home.Number}";
            _deploy.TooltipText = blocked;
            _placementReason.Text = _deploy.Visible && context.EditReason.Length == 0 ? blocked : "";
            _placementReason.Visible = _placementReason.Text.Length > 0;
            NativeOptions.Update(_trainer, city.Slots.Select((slot, i) => (slot, i)).Where(x => context.State.BuildingCatalog.FirstOrDefault(b => b.Type == x.slot.Type)?.Recruits is not null)
                .Select(x => (x.i, $"{GameplayContext.BuildingName(x.slot.Type)} L{x.slot.Level} · plot {x.i + 1}")));
            if (_recruitSquad != _selection.Squad)
            {
                _recruitSquad = _selection.Squad;
                var type = city.Soldiers.FirstOrDefault(u => u.SquadId == _selection.Squad && u.Assignment is { Stored: false })?.Type;
                _recruitment.SelectSquad(_selection.Squad, type);
                if (type is { } compatible)
                    for (int i = 0; i < _trainer.ItemCount; i++)
                        if (context.State.BuildingCatalog.First(b => b.Type == city.Slots[_trainer.GetItemId(i)].Type).Recruits!.Contains(compatible)) { _trainer.Select(i); break; }
            }
            bool planning = context.State.Phase is Phase.Building or Phase.Preparation;
            _preview.Visible = planning;
            _trainerMissing.Visible = planning && _trainer.ItemCount == 0 && context.EditReason.Length == 0;
            _trainer.Visible = _recruitment.Visible = planning && _trainer.ItemCount > 0 && home?.Home.Purchased == true && home.Home.Plot.Type == Building.Empty;
            if (_recruitment.Visible) _recruitment.Refresh(context, _trainer.GetSelectedId());
            _memberToggle.Text = $"Members ({city.Soldiers.Length}) ▾";
            _members.Begin((context.Session, context.State.MatchId, city.Id));
            foreach (var unit in city.Soldiers.OrderBy(u => u.Assignment?.Stored).ThenBy(u => u.SquadId).ThenBy(u => u.Id))
            {
                int id = unit.Id;
                var row = _members.Item(_skin, "Member" + id, $"{unit.Type} L{unit.Level} · HP {HealthPoints.Format(unit.Health)}/{HealthPoints.Format(unit.Profile.Health)} · "
                    + (unit.Assignment is { Stored: true } a ? $"stored in Town Hall {a.HallSlot + 1}" : $"Home {(unit.Assignment?.Tile ?? -1) + 1}"), () => SelectMember(id), variation: "ForestQuietButton");
                row.ToggleMode = true; row.SetPressedNoSignal(_selection.Member == id);
            }
            _members.End();
            HomeInspector.Refresh(context, _selection.Home);
            MemberInspector.FallbackFocus = Deployment.HomeButtons.GetValueOrDefault("Home" + _selection.Home);
            if (city.Soldiers.FirstOrDefault(u => u.Id == _selection.Member) is { } selected)
            { MemberInspector.Select(selected, false, context.State.Phase); MemberInspector.Refresh(context); }
            else MemberInspector.Clear();
            NativeOptions.Update(_halls, city.Army?.Halls.Select(h => (h.Slot, $"Town Hall plot {h.Slot + 1}")) ?? []);
            if (_details.CurrentTab == 1 && _selection.Member == 0 || _details.CurrentTab == 2 && _halls.ItemCount == 0) _details.CurrentTab = 0;
            _details.SetTabDisabled(1, _selection.Member == 0); _details.SetTabDisabled(2, _halls.ItemCount == 0);
            if (_halls.ItemCount > 0) _hall.Refresh(context, _halls.GetSelectedId());
            var currentFocus = GetViewport().GuiGetFocusOwner();
            if (ownedFocus && (currentFocus is null || !currentFocus.IsVisibleInTree())) FocusHome();
        }
        finally { _refreshing = false; }
    }
    private static void ShareRestriction(Node node, string reason)
    {
        if (node is ActionList list) list.SharedRestriction = reason;
        foreach (Node child in node.GetChildren()) ShareRestriction(child, reason);
    }
    private void ArmDeployment()
    {
        _armed = null;
        if (!_deploy.IsVisibleInTree() || _deploy.Disabled || _context.EditReason.Length > 0) return;
        var home = ArmyDeploymentView.Homes(_context.State, _context.City.Id, _selection.Squad).Single(h => h.Home.PlotId == _selection.Home);
        if (home.CanDeploy) _armed = new(_context.Session, _context.State.MatchId, _context.City.Id, _selection.Home, _selection.Squad, home.BattlefieldCell, _context.State.TurnSerial);
    }
    private void Deploy()
    {
        var intent = _armed; _armed = null;
        if (intent is null || !_deploy.IsVisibleInTree() || _deploy.Disabled || _context.EditReason.Length > 0
            || !ReferenceEquals(intent.Session, _context.Session) || intent.Match != _context.State.MatchId || intent.City != _context.City.Id
            || intent.Home != _selection.Home || intent.Squad != _selection.Squad || intent.Turn != _context.State.TurnSerial) return;
        var home = ArmyDeploymentView.Homes(_context.State, _context.City.Id, intent.Squad).Single(h => h.Home.PlotId == intent.Home);
        if (home.CanDeploy && home.BattlefieldCell == intent.Cell) _context.Submit(new(GameplayAction.Deploy, SquadId: intent.Squad, Cell: intent.Cell));
    }
    public override void _ExitTree()
    {
        Deployment.HomeSelected -= SelectHome; _trainer.ItemSelected -= TrainerChanged; _halls.ItemSelected -= HallChanged;
        _recruitment.DestinationChanged -= RecruitDestinationChanged; _recruitment.DetailsRequested -= ShowDetails;
        MemberInspector.PlotRequested -= ForwardPlot; _deploy.ButtonDown -= ArmDeployment;
        _selection.Reset(); _armed = null; PreviewRequested = null; PlotRequested = null;
    }
}
