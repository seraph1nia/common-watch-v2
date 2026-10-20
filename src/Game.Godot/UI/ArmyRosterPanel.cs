using Game.Core;
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
    public void ResetSelection() { if (_squads.ItemCount > 0) _squads.Select(0); _sections.CurrentTab = 0; }
    public DeploymentPanel Deployment { get; private set; } = null!;
    public void ShowManagementFor(int slot, GameplayContext context)
    {
        Refresh(context); bool hall = context.City.Slots[slot].Type == Building.TownHall;
        var selector = hall ? _halls : _trainer;
        int index = selector.GetItemIndex(slot); if (index >= 0) selector.Select(index);
        _sections.CurrentTab = hall ? 2 : 1; Refresh(context);
    }
    public void ShowHomes() => _sections.CurrentTab = 3;
    public void ShowManagement() => _sections.CurrentTab = 2;
    public event Action<UnitState>? UnitSelected;
    public IReadOnlyDictionary<string, Button> MemberButtons => _members.Buttons;
    public void Initialize(HudSkin skin, Action submitted)
    {
        _skin = skin; _summary = WrapLabel(); AddChild(_summary);
        _squads = new OptionButton(); AddChild(_squads); _squads.ItemSelected += _ => Refresh(_context);
        _squadDetails = WrapLabel(); AddChild(_squadDetails);
        _sections = new TabContainer { CustomMinimumSize = new(0, 360), SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(_sections);
        var rosterScroll = new ScrollContainer { Name = "Roster", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _sections.AddChild(rosterScroll);
        _members = new ActionList { SizeFlagsHorizontal = SizeFlags.ExpandFill }; rosterScroll.AddChild(_members);
        var managementScroll = new ScrollContainer { Name = "Recruitment", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _sections.AddChild(managementScroll);
        var management = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; managementScroll.AddChild(management);
        _trainer = new OptionButton(); management.AddChild(_trainer); _trainer.ItemSelected += _ => Refresh(_context);
        _recruitment = new RecruitmentPanel(); management.AddChild(_recruitment); _recruitment.Initialize(skin);
        var hallScroll = new ScrollContainer { Name = "Town Halls", HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _sections.AddChild(hallScroll);
        var halls = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; hallScroll.AddChild(halls);
        halls.AddChild(WrapLabel("Town Halls — select a hall; choose a veteran in Roster to store or return."));
        _halls = new OptionButton(); halls.AddChild(_halls); _halls.ItemSelected += _ => Refresh(_context);
        _hall = new TownHallPanel(); halls.AddChild(_hall); _hall.Initialize(skin);
        var homes = new ScrollContainer { Name = "Homes & towers", FollowFocus = true, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled }; _sections.AddChild(homes);
        Deployment = new DeploymentPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill }; homes.AddChild(Deployment); Deployment.Initialize(skin, submitted);
    }
    public void Refresh(GameplayContext context)
    {
        _context = context; var city = context.City; Deployment.Refresh(context);
        _summary.Text = $"P{city.Id} ARMY — {city.Soldiers.Length} persistent members; {city.Army?.PurchasedHomes}/6 homes\n{context.EditReason}\nRecruit one soldier into a new or compatible squad. Select any roster member to inspect, regroup, store, return or retire.";
        NativeOptions.Update(_trainer, city.Slots.Select((slot, i) => (slot, i)).Where(x => context.State.BuildingCatalog.FirstOrDefault(b => b.Type == x.slot.Type)?.Recruits is not null)
            .Select(x => (x.i, $"Plot {x.i + 1} — {GameplayContext.BuildingName(x.slot.Type)} L{x.slot.Level}")));
        _recruitment.Visible = _trainer.ItemCount > 0;
        if (_recruitment.Visible) _recruitment.Refresh(context, _trainer.GetSelectedId());
        NativeOptions.Update(_halls, city.Army?.Halls.Select(h => (h.Slot, $"Town Hall plot {h.Slot + 1}")) ?? []);
        _hall.Visible = _halls.ItemCount > 0; if (_hall.Visible) _hall.Refresh(context, _halls.GetSelectedId());
        NativeOptions.Update(_squads, new[] { (0, "All roster members") }.Concat(context.State.Squads.Where(s => s.Owner == city.Id && s.Faction == Faction.Adventurers)
            .Select(s => (s.Id, $"Squad #{s.Id} {s.Type} — {s.Members.Length}/{city.Army?.SquadCapacity}"))));
        var inspected = context.State.Squads.FirstOrDefault(s => s.Id == _squads.GetSelectedId() && s.Owner == city.Id);
        _squadDetails.Text = inspected is null ? "Select a squad to inspect its members; choose a soldier for individual actions." :
            $"Squad #{inspected.Id} {inspected.Type}: {inspected.Members.Length} members, {inspected.ParticipatingMembers.Length} participating; {inspected.Hex.Lifecycle}\n" +
            $"Deployment cell {inspected.DeploymentCell}; target {inspected.TargetKind} #{inspected.TargetId}; impact {inspected.ImpactTick}, ready {inspected.ReadyTick}";
        _members.Begin();
        foreach (var unit in city.Soldiers.Where(u => inspected is null || u.SquadId == inspected.Id).OrderBy(u => u.Assignment?.Stored).ThenBy(u => u.SquadId).ThenBy(u => u.Id))
        {
            int id = unit.Id;
            bool? funded = context.State.Phase is Phase.Building or Phase.Preparation ? city.FoodForecast?.Funded.Contains(id) : city.LastUpkeep?.Funded.Contains(id);
            _members.Item(_skin, "Member" + id, $"#{id} {unit.Type} L{unit.Level} · HP {HealthPoints.Format(unit.Health)}/{HealthPoints.Format(unit.Profile.Health)} · XP {unit.Experience}\nSquad #{unit.SquadId}; " +
                (unit.Assignment is { Stored: true } a ? $"Town Hall {a.HallSlot + 1}, place {a.Tile + 1}" : $"home {(unit.Assignment?.Tile ?? -1) + 1}") +
                $"; {(funded is null ? "funding unavailable" : funded.Value ? "funded" : "unfunded")}; recovery {(unit.RecoveryEligible ? "eligible" : "ineligible")}",
                () => { if (_context.City.Soldiers.FirstOrDefault(u => u.Id == id) is { } selected) UnitSelected?.Invoke(selected); });
        }
        _members.End();
    }
}
