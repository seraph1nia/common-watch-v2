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
    public void Initialize(HudSkin skin)
    {
        _skin = skin; _summary = WrapLabel(); AddChild(_summary); _actions = new ActionList(); AddChild(_actions);
    }
    public void Refresh(GameplayContext context, int slot)
    {
        _context = context; _slot = slot;
        var hall = context.City.Army?.Halls.FirstOrDefault(h => h.Slot == slot); if (hall is null) return;
        var stored = context.City.Soldiers.Where(u => u.Assignment is { Stored: true } a && a.HallSlot == slot && a.HallGeneration == hall.Generation).ToArray();
        _summary.Text = $"Town Hall: {stored.Length}/{hall.Capacity} individual storage slots\nCapacity L{hall.CapacityLevel}/3; healing L{hall.HealingLevel}/3 ({hall.HealingPercent}% max HP per production)\nStored soldiers never fight. Recovery requires food paid in a completed battle; storage alone grants no healing. Select a veteran in Army to store, return or retire.";
        _actions.Begin();
        Track("Capacity", GameplayAction.Capacity, hall.CapacityQuote, $"Upgrade capacity → {ArmyConfiguration.Capacity(Math.Min(3, hall.CapacityLevel + 1))} places");
        Track("Healing", GameplayAction.Healing, hall.HealingQuote, $"Upgrade healing → {ArmyConfiguration.HealingPercent(Math.Min(3, hall.HealingLevel + 1))}% per production");
        _actions.End();
    }
    private void Track(string key, GameplayAction action, ResourceCost? quote, string text)
        => _actions.Item(_skin, key, text + (quote is { } cost ? " — " + GameplayContext.Cost(cost) : " — complete"),
            () => _context.Submit(new(action, _slot)), _context.Block(quote, quote is null ? "Maximum level." : ""));
}
