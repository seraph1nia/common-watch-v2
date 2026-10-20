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
    private int _category;
    private readonly List<Button> _categories = [];
    private int _slot, _city;
    private Building? _selected;
    private ConstructionPayment _payment;
    public Button? RecoveryButton => _actions.Buttons.GetValueOrDefault("Recovery");
    private Button _confirm = null!;
    private Label _reason = null!;
    private ScrollContainer _cardScroll = null!;
    public Rect2 CardArea => _cardScroll.GetGlobalRect();
    public Button ConfirmButton => _confirm;
    public IReadOnlyList<Button> Categories => _categories;
    public IReadOnlyDictionary<Building, Button> BuildButtons => _buttons;
    public void Initialize(HudSkin skin)
    {
        _skin = skin;
        var tabs = new HBoxContainer(); tabs.AddThemeConstantOverride("separation", 4); AddChild(tabs);
        foreach (string name in new[] { "Production", "Military", "Defense", "Trade", "Utility" })
        {
            int index = _categories.Count;
            var tab = skin.Button(name, () => { _category = index; _selected = null; Refresh(_context, _slot); });
            tab.AddThemeFontSizeOverride("font_size", 14);
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            {
                var style = (StyleBox)skin.Theme.GetStylebox(state, "Button").Duplicate();
                style.ContentMarginLeft = style.ContentMarginRight = 12;
                tab.AddThemeStyleboxOverride(state, style);
            }
            tab.ToggleMode = true; tab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            _categories.Add(tab); tabs.AddChild(tab);
        }
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(row);
        var choice = new VBoxContainer { CustomMinimumSize = new(145, 0) }; row.AddChild(choice);
        _confirm = skin.Button("Select a building", () =>
        {
            if (_selected is { } type) _context.Submit(new(GameplayAction.Build, _slot, type, Payment: _payment));
        }, "ForestPrimaryButton");
        _confirm.AutowrapMode = TextServer.AutowrapMode.WordSmart; choice.AddChild(_confirm);
        _reason = WrapLabel(); _reason.MaxLinesVisible = 2; choice.AddChild(_reason);
        var scroll = new ScrollContainer { FollowFocus = true, SizeFlagsHorizontal = SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Auto, VerticalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _cardScroll = scroll; row.AddChild(scroll); _actions = new ActionList { Vertical = false }; scroll.AddChild(_actions);
    }
    public void ResetSelection() { _selected = null; _category = 0; }
    public void Refresh(GameplayContext context, int slot)
    {
        if (_slot != slot || _city != context.City.Id) ResetSelection();
        for (int i = 0; i < _categories.Count; i++) _categories[i].SetPressedNoSignal(i == _category);
        _context = context; _slot = slot; _city = context.City.Id; _actions.Begin(); _buttons.Clear();
        foreach (var definition in context.State.BuildingCatalog)
        {
            var type = definition.Type;
            int category = CategoryOf(definition);
            bool wrongPlot = ArmyConfiguration.IsArmyPlot(slot) && !ArmyConfiguration.IsTower(type);
            var button = _actions.Item(_skin, "Build" + type,
                $"{(_selected == type && _payment == ConstructionPayment.Standard ? "✓ " : "")}{GameplayContext.BuildingName(type)} — {GameplayContext.Cost(definition.Construction)}\n{Function(definition)}",
                () => { _selected = type; _payment = ConstructionPayment.Standard; Refresh(_context, _slot); },
                context.Block(definition.Construction, wrongPlot ? "Army homes accept defense towers only." : ""));
            button.ToggleMode = true; button.SetPressedNoSignal(_selected == type && _payment == ConstructionPayment.Standard); button.CustomMinimumSize = new(215, 100); button.ThemeTypeVariation = _skin.Illustrated ? "ForestCard" : "";
            button.Visible = category == _category;
            _buttons[type] = button;
        }
        var lumber = context.State.BuildingCatalog.First(b => b.Type == Building.Lumbermill);
        if (!ArmyConfiguration.IsArmyPlot(slot) && context.City.Wood == 0 && lumber.RecoveryConstruction is { } quote)
        {
            var recovery = _actions.Item(_skin, "Recovery", $"Lumbermill gold recovery — {GameplayContext.Cost(quote)}",
                () => { _selected = Building.Lumbermill; _payment = ConstructionPayment.GoldRecovery; Refresh(_context, _slot); }, context.Block(quote));
            recovery.ToggleMode = true; recovery.SetPressedNoSignal(_selected == Building.Lumbermill && _payment == ConstructionPayment.GoldRecovery);
        }
        var selected = context.State.BuildingCatalog.FirstOrDefault(definition => definition.Type == _selected);
        ResourceCost? selectedCost = _payment == ConstructionPayment.GoldRecovery ? selected?.RecoveryConstruction : selected?.Construction;
        string blocked = selected is null ? "Select a card." : context.Block(selectedCost,
            ArmyConfiguration.IsArmyPlot(slot) && !ArmyConfiguration.IsTower(selected.Type) ? "Army homes accept defense towers only." : "");
        _confirm.Text = selected is null ? "Build selected" : $"Build {GameplayContext.BuildingName(selected.Type)}{(_payment == ConstructionPayment.GoldRecovery ? " (Gold recovery)" : "")}\n{GameplayContext.Cost(selectedCost ?? default)}";
        _confirm.Visible = selected is not null;
        _confirm.TooltipText = selected is null ? "" : $"Build {GameplayContext.BuildingName(selected.Type)}{(_payment == ConstructionPayment.GoldRecovery ? " (Gold recovery)" : "")} — {GameplayContext.Cost(selectedCost ?? default)}";
        _confirm.Disabled = selected is null || blocked.Length > 0; _reason.Text = blocked; _reason.TooltipText = blocked; _reason.Visible = blocked.Length > 0;
        _actions.End();
        if (RecoveryButton is { Visible: true } recoveryCard) recoveryCard.Visible = _category == 0;
        foreach (var button in _actions.Buttons.Values) button.CustomMinimumSize = new(215, 100);
        foreach (var definition in context.State.BuildingCatalog)
        {
            int category = CategoryOf(definition);
            _buttons[definition.Type].Visible = category == _category;
        }
    }
    public static int CategoryOf(BuildingDefinition definition) => definition.Produces is not null ? 0 : definition.Recruits is not null ? 1 : ArmyConfiguration.IsTower(definition.Type) ? 2 : definition.Type == Building.Market ? 3 : 4;
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
