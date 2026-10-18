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
    public IReadOnlyDictionary<string, Button> Buttons => _actions.Buttons;
    public void Initialize(HudSkin skin)
    {
        _skin = skin; AddChild(WrapLabel("Recruit one persistent soldier; reinforce or start a homogeneous squad."));
        _destination = new OptionButton(); AddChild(_destination);
        _destination.ItemSelected += _ => Refresh(_context, _slot);
        _actions = new ActionList(); AddChild(_actions);
    }
    public void Refresh(GameplayContext context, int slot)
    {
        _context = context; _slot = slot; _actions.Begin();
        var building = context.City.Slots[slot];
        var definition = context.State.BuildingCatalog.FirstOrDefault(b => b.Type == building.Type);
        NativeOptions.Update(_destination, new[] { (0, "Automatic compatible squad"), (int.MaxValue, "New squad at an empty home") }
            .Concat(context.City.Soldiers.Where(u => u.Assignment is { Stored: false }).GroupBy(u => u.SquadId)
                .Select(g => (g.Key, $"Squad #{g.Key} {g.First().Type} — {g.Count()}/{context.City.Army?.SquadCapacity}"))));
        foreach (var type in definition?.Recruits ?? [])
        {
            var quote = context.City.RecruitmentQuotes.Single(q => q.Type == type && q.Level == building.Level);
            string reason = context.Destination(type, NativeOptions.Squad(_destination)) is null ? "No compatible squad space or free purchased home." : "";
            _actions.Item(_skin, "Recruit" + type,
                $"Recruit {type} L{quote.Level} — {GameplayContext.Cost(quote.Cost)}\nHP {HealthPoints.Format(quote.Profile.Health)}; damage {HealthPoints.Format(quote.Profile.Damage)}/{HealthPoints.Format(quote.Profile.SecondaryDamage)}; range {quote.Profile.HexRange}; upkeep {quote.Upkeep} Food",
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
        var values = options.ToArray(); int selected = control.ItemCount > 0 ? control.GetSelectedId() : int.MinValue;
        if (values.Length == control.ItemCount && values.Select((v, i) => v.Id == control.GetItemId(i) && v.Text == control.GetItemText(i)).All(v => v)) return;
        control.Clear(); foreach (var value in values) control.AddItem(value.Text, value.Id);
        int index = control.GetItemIndex(selected); if (index >= 0) control.Select(index);
    }
}
