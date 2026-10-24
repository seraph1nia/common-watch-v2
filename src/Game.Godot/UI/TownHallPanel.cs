using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class TownHallPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private int _slot;
    private Label _summary = null!;
    private ActionList _actions = null!;
    public event Action<int>? ReturnRequested;
    internal IReadOnlyDictionary<string, Button> Buttons => _actions.Buttons;
    public void Initialize(HudSkin skin)
    {
        _skin = skin; _summary = WrapLabel(); AddChild(_summary); _actions = new ActionList(); AddChild(_actions);
    }
    public void Refresh(GameplayContext context, int slot)
    {
        _context = context; _slot = slot;
        var hall = context.City.Army?.Halls.FirstOrDefault(h => h.Slot == slot); if (hall is null) return;
        var stored = context.City.Soldiers.Where(u => u.Assignment is { Stored: true } a && a.HallSlot == slot && a.HallGeneration == hall.Generation).ToArray();
        _summary.Text = $"Town Hall: {stored.Length}/{hall.Capacity} places · {hall.HealingPercent}% max HP per eligible Ready\nStored members do not fight and still need funded Food. Healing requires a completed funded battle. Capacity and healing upgrades are optional.";
        _actions.Begin((context.Session, context.State.MatchId, context.City.Id, slot, hall.Generation));
        Track("Capacity", GameplayAction.Capacity, hall.CapacityQuote, $"Upgrade capacity → {ArmyConfiguration.Capacity(Math.Min(3, hall.CapacityLevel + 1))} places");
        Track("Healing", GameplayAction.Healing, hall.HealingQuote, $"Upgrade healing → {ArmyConfiguration.HealingPercent(Math.Min(3, hall.HealingLevel + 1))}% per production");
        if (stored.Any(u => u.Health == u.Profile.Health) && context.City.Id == context.Session.PlayerId
            && !context.City.Eliminated && context.State.Phase is Phase.Building or Phase.Preparation)
            _actions.Item(_skin, "ReturnRecovered", "Review recovered members together", () => ReturnRequested?.Invoke(_slot), context.Block());
        _actions.End();
    }
    public override void _ExitTree() => ReturnRequested = null;
    private void Track(string key, GameplayAction action, ResourceCost? quote, string text)
        => _actions.Item(_skin, key, text + (quote is { } cost ? " — " + GameplayContext.Cost(cost) : " — complete"),
            () => _context.Submit(new(action, _slot)), _context.Block(quote, quote is null ? "Maximum level." : ""));
}
