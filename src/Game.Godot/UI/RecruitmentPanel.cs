using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class RecruitmentPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private int _slot;
    private OptionButton _destination = null!;
    private ActionList _actions = null!;
    private int? _preferred;
    public event Action<int>? DestinationChanged;
    public void SelectSquad(int squad) => _preferred = squad;
    public IReadOnlyDictionary<string, Button> Buttons => _actions.Buttons;
    public void Initialize(HudSkin skin)
    {
        _skin = skin; AddChild(WrapLabel("Recruit one persistent soldier; reinforce or start a homogeneous squad."));
        var timing = WrapLabel("Recruit before the last Ready. Final income arrives before Food payment, but cannot buy more troops before combat.");
        timing.AddThemeFontSizeOverride("font_size", 14); AddChild(timing);
        _destination = new OptionButton { FitToLongestItem = false, ClipText = true }; AddChild(_destination);
        _destination.ItemSelected += _ => { DestinationChanged?.Invoke(NativeOptions.Squad(_destination)); Refresh(_context, _slot); };
        _actions = new ActionList(); AddChild(_actions);
    }
    public void Refresh(GameplayContext context, int slot)
    {
        _context = context; _slot = slot; _actions.Begin();
        var building = context.City.Slots[slot];
        var definition = context.State.BuildingCatalog.FirstOrDefault(b => b.Type == building.Type);
        NativeOptions.Update(_destination, new[] { (0, "Automatic compatible squad"), (int.MaxValue, "New squad at an empty home") }
            .Concat(context.City.Soldiers.Where(u => u.Assignment is { Stored: false }).GroupBy(u => u.SquadId)
                .Select(g => (g.Key, ArmyDeploymentView.SquadLabel(g, context.City.Army?.SquadCapacity ?? 3)))));
        if (_preferred is { } preferred) { int index = _destination.GetItemIndex(preferred); if (index >= 0) _destination.Select(index); _preferred = null; }
        _destination.TooltipText = _destination.GetItemText(_destination.Selected);
        foreach (var type in definition?.Recruits ?? [])
        {
            var quote = context.City.RecruitmentQuotes.Single(q => q.Type == type && q.Level == building.Level);
            string reason = context.Destination(type, NativeOptions.Squad(_destination)) is null ? "No compatible squad space or free purchased home." : "";
            _actions.Item(_skin, "Recruit" + type,
                $"Recruit {type} L{quote.Level} — {GameplayContext.Cost(quote.Cost)}\n{ArmyDeploymentView.Destination(context.City, context.Army, type, NativeOptions.Squad(_destination))}\nHP {HealthPoints.Format(quote.Profile.Health)}; damage {HealthPoints.Format(quote.Profile.Damage)}/{HealthPoints.Format(quote.Profile.SecondaryDamage)}; range {quote.Profile.HexRange}; upkeep {quote.Upkeep} Food",
                () => _context.Submit(new(GameplayAction.Recruit, _slot, SoldierType: type, SquadId: NativeOptions.Squad(_destination))), context.Block(quote.Cost, reason));
        }
        _actions.End();
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
