using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class ConstructionPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private readonly Dictionary<Building, Button> _buttons = [];
    private ActionList _actions = null!;
    private OptionButton _category = null!;
    private int _slot;
    public OptionButton Category => _category;
    public IReadOnlyDictionary<Building, Button> BuildButtons => _buttons;
    public void Initialize(HudSkin skin)
    {
        _skin = skin;
        _category = new OptionButton(); foreach (string name in new[] { "Production", "Military", "Defense", "Trade / utility" }) _category.AddItem(name);
        AddChild(_category); _category.ItemSelected += _ => Refresh(_context, _slot);
        _actions = new ActionList(); AddChild(_actions);
    }
    public void Refresh(GameplayContext context, int slot)
    {
        _context = context; _slot = slot; _actions.Begin(); _buttons.Clear();
        foreach (var definition in context.State.BuildingCatalog)
        {
            var type = definition.Type;
            int category = definition.Produces is not null ? 0 : definition.Recruits is not null ? 1 : ArmyConfiguration.IsTower(type) ? 2 : 3;
            bool wrongPlot = ArmyConfiguration.IsArmyPlot(slot) && !ArmyConfiguration.IsTower(type);
            var button = _actions.Item(_skin, "Build" + type,
                $"{GameplayContext.BuildingName(type)} — {GameplayContext.Cost(definition.Construction)}\n{Function(definition)}",
                () => _context.Submit(new(GameplayAction.Build, _slot, type)),
                context.Block(definition.Construction, wrongPlot ? "Army homes accept defense towers only." : ""));
            button.Visible = category == _category.Selected;
            _buttons[type] = button;
        }
        var lumber = context.State.BuildingCatalog.First(b => b.Type == Building.Lumbermill);
        if (!ArmyConfiguration.IsArmyPlot(slot) && context.City.Wood == 0 && lumber.RecoveryConstruction is { } quote)
            _actions.Item(_skin, "Recovery", $"Lumbermill gold recovery — {GameplayContext.Cost(quote)}",
                () => _context.Submit(new(GameplayAction.Build, _slot, Building.Lumbermill, Payment: ConstructionPayment.GoldRecovery)), context.Block(quote));
        _actions.End();
        foreach (var definition in context.State.BuildingCatalog)
        {
            int category = definition.Produces is not null ? 0 : definition.Recruits is not null ? 1 : ArmyConfiguration.IsTower(definition.Type) ? 2 : 3;
            _buttons[definition.Type].Visible = category == _category.Selected;
        }
    }
    public static string Function(BuildingDefinition definition) => definition.Produces is { } resource ? $"Produces {definition.LevelOneOutput}/{definition.LevelTwoOutput} {resource} per settlement (base). Max L{definition.MaximumLevel}."
        : definition.Recruits is { } recruits ? $"Trains {string.Join(", ", recruits)}. Max equipment L{definition.MaximumLevel}."
        : definition.Type switch
        {
            Building.ResearchTower => "Produces research progress each preparation; 3 progress becomes 1 point.",
            Building.TownHall => "Stores individual veterans; paid survivors recover at production. Independent capacity and healing tracks.",
            Building.Market => "Sell whole resource bundles for Gold.",
            _ => "Automatic defense weapon; inspect range, damage and timing after construction."
        };
}
