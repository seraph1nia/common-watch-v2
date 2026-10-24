using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class RecruitmentPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private int _slot;
    private OptionButton _destination = null!, _type = null!;
    private ActionList _actions = null!;
    private int? _preferred;
    private UnitType? _preferredType;
    private bool _compact;
    private Label _availability = null!;
    private string _details = "";
    public event Action<int>? DestinationChanged;
    public event Action<string>? DetailsRequested;
    public void SelectSquad(int squad, UnitType? type = null) { _preferred = squad; _preferredType = type; }
    public IReadOnlyDictionary<string, Button> Buttons => _actions.Buttons;
    public void Initialize(HudSkin skin, bool compact = false)
    {
        _skin = skin; _compact = compact;
        var history = WrapLabel("New recruits start with zero service history. Trainer equipment applies only to new recruits; existing soldiers keep their tier.");
        AddChild(history); history.Visible = !compact;
        var timing = WrapLabel("Recruit before the last Ready. Final income arrives before Food payment, but cannot buy more troops before combat.");
        timing.AddThemeFontSizeOverride("font_size", 14); AddChild(timing); timing.Visible = !compact;
        if (compact)
        {
            var options = new HBoxContainer(); AddChild(options);
            _type = new OptionButton { FitToLongestItem = false, ClipText = true, SizeFlagsHorizontal = SizeFlags.ExpandFill }; options.AddChild(_type);
            _type.ItemSelected += _ => Refresh(_context, _slot);
            options.AddChild(skin.Button("Destination ▾", () => _destination.Visible = !_destination.Visible, "ForestQuietButton"));

        }
        _destination = new OptionButton { FitToLongestItem = false, ClipText = true }; AddChild(_destination);
        _destination.Visible = !compact;
        _destination.ItemSelected += _ => { DestinationChanged?.Invoke(NativeOptions.Squad(_destination)); Refresh(_context, _slot); };
        _actions = new ActionList { ExplainRestrictions = !compact }; AddChild(_actions);
        _availability = WrapLabel(); _availability.AddThemeFontSizeOverride("font_size", 14); AddChild(_availability);
    }
    public void Refresh(GameplayContext context, int slot)
    {
        _context = context; _slot = slot;
        var building = context.City.Slots[slot];
        _actions.Begin((context.Session, context.State.MatchId, context.City.Id, slot, building.Generation));
        var definition = context.State.BuildingCatalog.FirstOrDefault(b => b.Type == building.Type);
        NativeOptions.Update(_destination, new[] { (0, "Automatic compatible squad"), (int.MaxValue, "New squad at an empty home") }
            .Concat(context.City.Soldiers.Where(u => u.Assignment is { Stored: false }).GroupBy(u => u.SquadId)
                .Select(g => (g.Key, ArmyDeploymentView.SquadLabel(g, context.City.Army?.SquadCapacity ?? 3)))));
        if (_preferred is { } preferred) { int index = _destination.GetItemIndex(preferred); if (index >= 0) _destination.Select(index); _preferred = null; }
        _destination.TooltipText = _destination.GetItemText(_destination.Selected);
        if (_compact)
        {
            NativeOptions.Update(_type, (definition?.Recruits ?? []).Select(t => ((int)t, t.ToString())));
            if (_preferredType is { } wanted) { int index = _type.GetItemIndex((int)wanted); if (index >= 0) _type.Select(index); _preferredType = null; }
        }
        var explanations = new List<string>(); var profiles = new List<string>();
        foreach (var type in definition?.Recruits ?? [])
        {
            var quote = context.City.RecruitmentQuotes.Single(q => q.Type == type && q.Level == building.Level);
            string reason = context.Destination(type, NativeOptions.Squad(_destination)) is null ? "No compatible squad space or free purchased home." : "";
            string destination = ArmyDeploymentView.Destination(context.City, context.Army, type, NativeOptions.Squad(_destination));
            var assignment = context.Destination(type, NativeOptions.Squad(_destination));
            bool reinforces = assignment is not null && context.City.Soldiers.Any(u => u.Health > 0 && u.Assignment is { Stored: false } a && a.Tile == assignment.Tile);
            string title = $"{(_compact && reinforces ? "Reinforce" : "Recruit")} {type} L{quote.Level} — {GameplayContext.Cost(quote.Cost)}";
            string profile = $"HP {HealthPoints.Format(quote.Profile.Health)}; damage {HealthPoints.Format(quote.Profile.Damage)}/{HealthPoints.Format(quote.Profile.SecondaryDamage)}; range {quote.Profile.HexRange}; upkeep {quote.Upkeep} Food";
            string blocked = context.Block(quote.Cost, reason);
            var button = _actions.Item(_skin, "Recruit" + type, title + "\n" + (_compact ? assignment is null ? $"Upkeep {quote.Upkeep} Food" : destination.Replace(" after arrival", "") : destination + "\n" + profile),
                () => _context.Submit(new(GameplayAction.Recruit, _slot, SoldierType: type, SquadId: NativeOptions.Squad(_destination))), context.Block(quote.Cost, reason), variation: _compact ? "ForestQuietButton" : "");
            if (_compact) button.Visible = (int)type == _type.GetSelectedId();
            profiles.Add(title + "\n" + destination + "\n" + profile + (blocked.Length > 0 ? "\n" + blocked : ""));
            if (_compact && (int)type == _type.GetSelectedId() && context.EditReason.Length == 0 && blocked.Length > 0)
            {
                string shortfall = string.Join(", ", Enum.GetValues<Resource>().Where(r => context.City.Resources.Amount(r) < quote.Cost.Amount(r))
                    .Select(r => $"{quote.Cost.Amount(r) - context.City.Resources.Amount(r)} {r}"));
                explanations.Add(type + ": " + (reason.Length > 0 ? reason : "Need " + shortfall));
            }
        }
        _actions.End();
        if (_compact) foreach (var type in definition?.Recruits ?? []) _actions.Buttons["Recruit" + type].Visible = (int)type == _type.GetSelectedId();
        _details = string.Join("\n\n", profiles) + "\n\nNew recruits use this trainer's current equipment. Existing members keep their tier. Final income cannot buy troops after the final Ready.";
        if (_compact) DetailsRequested?.Invoke(_details);
        _availability.Text = string.Join("\n", explanations); _availability.Visible = explanations.Count > 0;
    }
}

public static class NativeOptions
{
    public static int Squad(OptionButton control) => control.GetSelectedId() == int.MaxValue ? -1 : control.GetSelectedId();
    public static void Update(OptionButton control, IEnumerable<(int Id, string Text)> options)
    {
        control.CustomMinimumSize = new(control.CustomMinimumSize.X, 44);
        var values = options.ToArray(); int selected = control.ItemCount > 0 ? control.GetSelectedId() : int.MinValue;
        if (values.Length == control.ItemCount && values.Select((v, i) => v.Id == control.GetItemId(i) && v.Text == control.GetItemText(i)).All(v => v)) return;
        control.Clear(); foreach (var value in values) control.AddItem(value.Text, value.Id);
        int index = control.GetItemIndex(selected); if (index >= 0) control.Select(index);
    }
}
