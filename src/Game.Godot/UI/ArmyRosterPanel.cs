using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class ArmyRosterPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private Label _summary = null!;
    private ActionList _members = null!;
    private OptionButton _trainer = null!;
    private RecruitmentPanel _recruitment = null!;
    private TownHallPanel _hall = null!;
    private OptionButton _halls = null!;
    private OptionButton _squads = null!;
    private Label _squadDetails = null!;
    private TabContainer _sections = null!;
    private int _selectedMember, _memberSquad;
    public bool HomesVisible => _sections.CurrentTab == 3;
    public int SelectedSquad => _squads.ItemCount > 0 ? _squads.GetSelectedId() : 0;
    internal RecruitmentPanel Recruitment => _recruitment;
    internal OptionButton SquadSelector => _squads;
    internal ScrollContainer HomeViewport => Deployment.GetParent<ScrollContainer>();
    public event Action? SectionChanged;
    public void ResetSelection() { _selectedMember = _memberSquad = 0; if (_squads.ItemCount > 0) _squads.Select(0); _sections.CurrentTab = 0; }
    public void SelectSquad(int squad)
    {
        int index = _squads.GetItemIndex(squad); if (index < 0) return;
        _squads.Select(index); _recruitment.SelectSquad(squad); if (_context is not null) Refresh(_context);
    }
    public DeploymentPanel Deployment { get; private set; } = null!;
    public void ShowManagementFor(int slot, GameplayContext context)
    {
        Refresh(context); bool hall = context.City.Slots[slot].Type == Building.TownHall;
        var selector = hall ? _halls : _trainer;
        int index = selector.GetItemIndex(slot); if (index >= 0) selector.Select(index);
        _sections.CurrentTab = hall ? 2 : 1; Refresh(context);
    }
    public void ShowHomes()
    {
        if (SelectedSquad == 0 && _squads.ItemCount > 1) SelectSquad(_squads.GetItemId(1));
        _sections.CurrentTab = 3;
    }
    public void ShowManagement() => _sections.CurrentTab = 2;
    public void ShowRoster() => _sections.CurrentTab = 0;
    public event Action<UnitState>? UnitSelected;
    public IReadOnlyDictionary<string, Button> MemberButtons => _members.Buttons;
    internal ScrollContainer RosterScroll => _members.GetParent<ScrollContainer>();
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; AddThemeConstantOverride("separation", 4); _summary = WrapLabel(); AddChild(_summary);
        _squads = new OptionButton { FitToLongestItem = false, ClipText = true }; AddChild(_squads); _squads.ItemSelected += _ => SelectSquad(SelectedSquad);
        _squadDetails = WrapLabel(); AddChild(_squadDetails);
        _sections = new TabContainer { CustomMinimumSize = new(0, 360), SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill }; AddChild(_sections);
        _sections.TabChanged += _ =>
        {
            if (HomesVisible && SelectedSquad == 0 && _squads.ItemCount > 1) SelectSquad(_squads.GetItemId(1));
            SectionChanged?.Invoke();
        };
        var rosterScroll = new ScrollContainer { Name = "Roster", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _sections.AddChild(rosterScroll);
        _members = new ActionList { SizeFlagsHorizontal = SizeFlags.ExpandFill }; rosterScroll.AddChild(_members);
        var managementScroll = new ScrollContainer { Name = "Recruit", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _sections.AddChild(managementScroll);
        var management = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; managementScroll.AddChild(management);
        _trainer = new OptionButton(); management.AddChild(_trainer); _trainer.ItemSelected += _ => Refresh(_context);
        _recruitment = new RecruitmentPanel(); management.AddChild(_recruitment); _recruitment.Initialize(skin);
        _recruitment.DestinationChanged += squad => { if (squad > 0) SelectSquad(squad); };
        var hallScroll = new ScrollContainer { Name = "Town Halls", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _sections.AddChild(hallScroll);
        var halls = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; hallScroll.AddChild(halls);
        halls.AddChild(WrapLabel("Town Halls — select a hall; choose a veteran in Roster to store or return."));
        _halls = new OptionButton(); halls.AddChild(_halls); _halls.ItemSelected += _ => Refresh(_context);
        _hall = new TownHallPanel(); halls.AddChild(_hall); _hall.Initialize(skin);
        var homes = new ScrollContainer { Name = "6 homes & towers", FollowFocus = true, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _sections.AddChild(homes);
        Deployment = new DeploymentPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill }; homes.AddChild(Deployment); Deployment.Initialize(skin, submitted);
    }
    public void Refresh(GameplayContext context)
    {
        _context = context; var city = context.City;
        _summary.Text = $"P{city.Id} ARMY · {city.Soldiers.Length} members · {city.Army?.PurchasedHomes}/6 homes"
            + (context.EditReason.Length > 0 && !HomesVisible ? "\n" + context.EditReason : "");
        NativeOptions.Update(_squads, new[] { (0, "All roster members — select a squad for deployment") }.Concat(city.Soldiers
            .Where(u => u.Health > 0 && u.Assignment is { Stored: false }).GroupBy(u => u.SquadId)
            .OrderBy(g => g.First().Assignment!.Tile).Select(g => (g.Key, ArmyDeploymentView.SquadLabel(g, city.Army?.SquadCapacity ?? 3)))));
        if (city.Soldiers.FirstOrDefault(u => u.Id == _selectedMember) is { } followed && followed.SquadId != _memberSquad)
        {
            _memberSquad = followed.SquadId;
            int index = _squads.GetItemIndex(followed.SquadId);
            if (followed.Assignment is { Stored: false } && index >= 0) { _squads.Select(index); _recruitment.SelectSquad(followed.SquadId); }
        }
        Deployment.Refresh(context, SelectedSquad);
        NativeOptions.Update(_trainer, city.Slots.Select((slot, i) => (slot, i)).Where(x => context.State.BuildingCatalog.FirstOrDefault(b => b.Type == x.slot.Type)?.Recruits is not null)
            .Select(x => (x.i, $"Plot {x.i + 1} — {GameplayContext.BuildingName(x.slot.Type)} L{x.slot.Level}")));
        _recruitment.Visible = _trainer.ItemCount > 0;
        if (_recruitment.Visible) _recruitment.Refresh(context, _trainer.GetSelectedId());
        NativeOptions.Update(_halls, city.Army?.Halls.Select(h => (h.Slot, $"Town Hall plot {h.Slot + 1}")) ?? []);
        _hall.Visible = _halls.ItemCount > 0; if (_hall.Visible) _hall.Refresh(context, _halls.GetSelectedId());
        var inspected = context.State.Squads.FirstOrDefault(s => s.Id == _squads.GetSelectedId() && s.Owner == city.Id);
        _squadDetails.Text = inspected is null ? "Select a squad to inspect its members; choose a soldier for individual actions." :
            $"{inspected.Type}: {city.Soldiers.Count(u => u.SquadId == inspected.Id)} members · "
            + $"{city.Soldiers.Count(u => u.SquadId == inspected.Id && city.FoodForecast?.Participating.Contains(u.Id) == true)} projected to fight. Select a member to regroup/store/return.";
        _squadDetails.Visible = !HomesVisible;
        _members.Begin();
        foreach (var unit in city.Soldiers.Where(u => inspected is null || u.SquadId == inspected.Id).OrderBy(u => u.Assignment?.Stored).ThenBy(u => u.SquadId).ThenBy(u => u.Id))
        {
            int id = unit.Id;
            bool? funded = context.State.Phase is Phase.Building or Phase.Preparation ? city.FoodForecast?.Funded.Contains(id) : city.LastUpkeep?.Funded.Contains(id);
            _members.Item(_skin, "Member" + id, $"#{id} {unit.Type} L{unit.Level} · HP {HealthPoints.Format(unit.Health)}/{HealthPoints.Format(unit.Profile.Health)} · XP {unit.Experience}\nSquad #{unit.SquadId}; " +
                (unit.Assignment is { Stored: true } a ? $"Town Hall {a.HallSlot + 1}, place {a.Tile + 1}" : $"home {(unit.Assignment?.Tile ?? -1) + 1}") +
                $"; {(funded is null ? "funding unavailable" : funded.Value ? "funded" : "unfunded")}; recovery {(unit.RecoveryEligible ? "eligible" : "ineligible")}",
                () => { if (_context.City.Soldiers.FirstOrDefault(u => u.Id == id) is { } selected) { _selectedMember = id; _memberSquad = selected.SquadId; UnitSelected?.Invoke(selected); } });
        }
        _members.End();
    }
}
