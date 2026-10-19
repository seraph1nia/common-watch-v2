using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class ConstructionPanel : HBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private readonly Dictionary<Building, Button> _buttons = [];
    private ActionList _actions = null!;
    private OptionButton _category = null!;
    private int _slot, _city;
    private Building? _selected;
    private Button _confirm = null!;
    private Label _reason = null!;
    private ScrollContainer _cardScroll = null!;
    public Rect2 CardArea => _cardScroll.GetGlobalRect();
    public Button ConfirmButton => _confirm;
    public OptionButton Category => _category;
    public IReadOnlyDictionary<Building, Button> BuildButtons => _buttons;
    public void Initialize(HudSkin skin)
    {
        _skin = skin;
        _category = new OptionButton { CustomMinimumSize = new(170, 44), SizeFlagsVertical = SizeFlags.ShrinkBegin }; foreach (string name in new[] { "Production", "Military", "Defense", "Trade / utility" }) _category.AddItem(name);
        var choice = new VBoxContainer { CustomMinimumSize = new(220, 0) }; AddChild(choice);
        choice.AddChild(_category); _category.ItemSelected += _ => { _selected = null; Refresh(_context, _slot); };
        _confirm = skin.Button("Select a building", () =>
        {
            if (_selected is { } type) _context.Submit(new(GameplayAction.Build, _slot, type));
        }, "ForestPrimaryButton");
        _confirm.AutowrapMode = TextServer.AutowrapMode.WordSmart; choice.AddChild(_confirm);
        _reason = WrapLabel(); _reason.MaxLinesVisible = 2; choice.AddChild(_reason);
        var scroll = new ScrollContainer { FollowFocus = true, SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto, VerticalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _cardScroll = scroll; AddChild(scroll); _actions = new ActionList { Vertical = false }; scroll.AddChild(_actions);
    }
    public void ResetSelection() { _selected = null; _category.Select(0); }
    public void Refresh(GameplayContext context, int slot)
    {
        if (_slot != slot || _city != context.City.Id) _selected = null;
        _context = context; _slot = slot; _city = context.City.Id; _actions.Begin(); _buttons.Clear();
        foreach (var definition in context.State.BuildingCatalog)
        {
            var type = definition.Type;
            int category = definition.Produces is not null ? 0 : definition.Recruits is not null ? 1 : ArmyConfiguration.IsTower(type) ? 2 : 3;
            bool wrongPlot = ArmyConfiguration.IsArmyPlot(slot) && !ArmyConfiguration.IsTower(type);
            var button = _actions.Item(_skin, "Build" + type,
                $"{(_selected == type ? "✓ " : "")}{GameplayContext.BuildingName(type)} — {GameplayContext.Cost(definition.Construction)}\n{Function(definition)}",
                () => { _selected = type; Refresh(_context, _slot); },
                context.Block(definition.Construction, wrongPlot ? "Army homes accept defense towers only." : ""));
            button.ToggleMode = true; button.SetPressedNoSignal(_selected == type); button.CustomMinimumSize = new(240, 100); button.ThemeTypeVariation = _skin.Illustrated ? "ForestCard" : "";
            button.Visible = category == _category.Selected;
            _buttons[type] = button;
        }
        var lumber = context.State.BuildingCatalog.First(b => b.Type == Building.Lumbermill);
        if (!ArmyConfiguration.IsArmyPlot(slot) && context.City.Wood == 0 && lumber.RecoveryConstruction is { } quote)
            _actions.Item(_skin, "Recovery", $"Lumbermill gold recovery — {GameplayContext.Cost(quote)}",
                () => _context.Submit(new(GameplayAction.Build, _slot, Building.Lumbermill, Payment: ConstructionPayment.GoldRecovery)), context.Block(quote));
        var selected = context.State.BuildingCatalog.FirstOrDefault(definition => definition.Type == _selected);
        string blocked = selected is null ? "Select a card." : context.Block(selected.Construction,
            ArmyConfiguration.IsArmyPlot(slot) && !ArmyConfiguration.IsTower(selected.Type) ? "Army homes accept defense towers only." : "");
        _confirm.Text = selected is null ? "Build selected" : $"Build {GameplayContext.BuildingName(selected.Type)}\n{GameplayContext.Cost(selected.Construction)}";
        _confirm.Visible = selected is not null;
        _confirm.TooltipText = selected is null ? "" : $"Build {GameplayContext.BuildingName(selected.Type)} — {GameplayContext.Cost(selected.Construction)}";
        _confirm.Disabled = selected is null || blocked.Length > 0; _reason.Text = blocked; _reason.TooltipText = blocked; _reason.Visible = blocked.Length > 0;
        _actions.End();
        foreach (var button in _actions.Buttons.Values) button.CustomMinimumSize = new(240, 100);
        foreach (var definition in context.State.BuildingCatalog)
        {
            int category = definition.Produces is not null ? 0 : definition.Recruits is not null ? 1 : ArmyConfiguration.IsTower(definition.Type) ? 2 : 3;
            _buttons[definition.Type].Visible = category == _category.Selected;
        }
    }
    public static string Function(BuildingDefinition definition) => definition.Produces is { } resource ? $"Output {definition.LevelOneOutput}/{definition.LevelTwoOutput} {resource}/production (base); max L{definition.MaximumLevel}."
        : definition.Recruits is { } recruits ? $"Trains {string.Join(", ", recruits)}. Max equipment L{definition.MaximumLevel}."
        : definition.Type switch
        {
            Building.ResearchTower => "Produces research progress each preparation; 3 progress becomes 1 point.",
            Building.TownHall => "Stores individual veterans; paid survivors recover at production. Independent capacity and healing tracks.",
            Building.Market => "Sell whole resource bundles for Gold.",
            _ => "Automatic defense weapon; inspect range, damage and timing after construction."
        };
}
