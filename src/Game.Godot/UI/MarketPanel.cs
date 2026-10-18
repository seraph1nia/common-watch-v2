using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class MarketPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private int _slot;
    private SpinBox _bundles = null!;
    private ActionList _actions = null!;
    public IReadOnlyDictionary<string, Button> Buttons => _actions.Buttons;
    public SpinBox Bundles => _bundles;
    public void Initialize(HudSkin skin)
    {
        _skin = skin; AddChild(WrapLabel("Market — bundles per sale"));
        _bundles = new SpinBox { MinValue = 1, MaxValue = 1000000, Step = 1, Value = 1 }; AddChild(_bundles);
        _actions = new ActionList(); AddChild(_actions);
        _bundles.ValueChanged += _ => { if (_context is not null) Refresh(_context, _slot); };
    }
    public void Refresh(GameplayContext context, int slot)
    {
        _context = context; _slot = slot; _actions.Begin();
        int bundles = (int)_bundles.Value;
        foreach (var rate in context.State.MarketRates)
        {
            var resource = rate.Resource;
            long units = (long)rate.Units * bundles, gold = (long)rate.Gold * bundles + context.City.MarketSaleBonus;
            long balance = context.City.Resources.Amount(resource) - units;
            string reason = balance < 0 ? $"Need {-balance} more {resource} ({GameplayContext.Producer(resource)})." : "";
            if (units > int.MaxValue || gold + context.City.Gold > int.MaxValue) reason = "Trade exceeds resource bounds.";
            string text = $"Sell {units} {resource} → {gold} Gold\n{rate.Units} {resource}/{rate.Gold} Gold per bundle; " +
                (balance >= 0 ? $"after sale {balance} {resource}, {context.City.Gold + gold} Gold" : "insufficient stock for this sale");
            if (resource == Resource.Food && units <= int.MaxValue)
            {
                var preview = BattleFood.Project(context.City, context.State.Phase == Phase.Preparation, new(resource, (int)units, gold <= int.MaxValue ? (int)(gold - context.City.MarketSaleBonus) : int.MaxValue));
                text += preview is null ? "\nFood forecast unavailable" : $"\nNext upkeep {preview.Paid}; food left {preview.Available - preview.Paid}; unfunded {preview.Unfed.Length}";
            }
            _actions.Item(_skin, "Trade" + resource, text,
                () => _context.Submit(new(GameplayAction.Trade, _slot, Resource: resource, Bundles: (int)_bundles.Value)), context.Block(reason: reason));
        }
        _actions.End();
    }
}
