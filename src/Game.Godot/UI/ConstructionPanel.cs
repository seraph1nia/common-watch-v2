using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class ConstructionPanel : VBoxContainer
{
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private readonly Dictionary<Building, Button> _buttons = [];
    private GridContainer _cards = null!;
    private int _category;
    private readonly List<Button> _categories = [];
    private int _slot, _city;
    private Building? _selected;
    private ConstructionPayment _payment;
    public Button? RecoveryButton { get; private set; }
    private Button _confirm = null!;
    private Label _reason = null!;
    public Rect2 CardArea => _cards.GetGlobalRect();
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
        _cards = new GridContainer { Columns = 4, SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(_cards);
        var choice = new HBoxContainer(); AddChild(choice);
        _confirm = skin.Button("Select a building", () =>
        {
            if (_selected is { } type) _context.Submit(new(GameplayAction.Build, _slot, type, Payment: _payment));
        }, "ForestPrimaryButton");
        _confirm.AutowrapMode = TextServer.AutowrapMode.WordSmart; _confirm.SizeFlagsHorizontal = SizeFlags.ExpandFill; choice.AddChild(_confirm);
        _reason = WrapLabel(); _reason.MaxLinesVisible = 2; choice.AddChild(_reason);
        RecoveryButton = CreateCard(() => { _selected = Building.Lumbermill; _payment = ConstructionPayment.GoldRecovery; Refresh(_context, _slot); });
        RecoveryButton.Hide();
    }
    private Button CreateCard(Action choose)
    {
        var button = _skin.Button("", choose);
        button.ToggleMode = true; button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.CustomMinimumSize = new(200, 100); button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        button.ThemeTypeVariation = _skin.Illustrated ? "ForestCard" : "";
        if (_skin.Illustrated)
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            {
                var style = (StyleBox)_skin.Theme.GetStylebox(state, "ForestCard").Duplicate();
                style.ContentMarginLeft = style.ContentMarginRight = 16;
                style.ContentMarginTop = style.ContentMarginBottom = 12;
                button.AddThemeStyleboxOverride(state, style);
            }
        _cards.AddChild(button); return button;
    }
    private static void RefreshCard(Button button, string text, string blocked, bool selected)
    {
        button.Text = text + (blocked.Length > 0 ? "\n" + blocked : "");
        button.TooltipText = blocked; button.Disabled = blocked.Length > 0;
        button.SetPressedNoSignal(selected);
    }
    public void ResetSelection() { _selected = null; _category = 0; }
    public void Refresh(GameplayContext context, int slot)
    {
        if (_slot != slot || _city != context.City.Id) ResetSelection();
        for (int i = 0; i < _categories.Count; i++) _categories[i].SetPressedNoSignal(i == _category);
        _context = context; _slot = slot; _city = context.City.Id;
        foreach (var definition in context.State.BuildingCatalog)
        {
            var type = definition.Type;
            int category = CategoryOf(definition);
            bool wrongPlot = ArmyConfiguration.IsArmyPlot(slot) && !ArmyConfiguration.IsTower(type);
            if (!_buttons.TryGetValue(type, out var button))
            {
                button = CreateCard(() => { _selected = type; _payment = ConstructionPayment.Standard; Refresh(_context, _slot); });
                button.Name = "Build" + type; _buttons.Add(type, button);
            }
            RefreshCard(button,
                $"{(_selected == type && _payment == ConstructionPayment.Standard ? "✓ " : "")}{GameplayContext.BuildingName(type)} — {GameplayContext.Cost(definition.Construction)}\n{Function(definition)}",
                context.Block(definition.Construction, wrongPlot ? "Army homes accept defense towers only." : ""),
                _selected == type && _payment == ConstructionPayment.Standard);
            button.Visible = category == _category;
        }
        var lumber = context.State.BuildingCatalog.First(b => b.Type == Building.Lumbermill);
        RecoveryButton!.Visible = !ArmyConfiguration.IsArmyPlot(slot) && context.City.Wood == 0
            && lumber.RecoveryConstruction is not null && _category == 0;
        if (!ArmyConfiguration.IsArmyPlot(slot) && context.City.Wood == 0 && lumber.RecoveryConstruction is { } quote)
        {
            RefreshCard(RecoveryButton, $"Lumbermill gold recovery — {GameplayContext.Cost(quote)}", context.Block(quote),
                _selected == Building.Lumbermill && _payment == ConstructionPayment.GoldRecovery);
            _cards.MoveChild(RecoveryButton, -1);
        }
        var selected = context.State.BuildingCatalog.FirstOrDefault(definition => definition.Type == _selected);
        ResourceCost? selectedCost = _payment == ConstructionPayment.GoldRecovery ? selected?.RecoveryConstruction : selected?.Construction;
        string blocked = selected is null ? "Select a card." : context.Block(selectedCost,
            ArmyConfiguration.IsArmyPlot(slot) && !ArmyConfiguration.IsTower(selected.Type) ? "Army homes accept defense towers only." : "");
        _confirm.Text = selected is null ? "Build selected" : $"Build {GameplayContext.BuildingName(selected.Type)}{(_payment == ConstructionPayment.GoldRecovery ? " (Gold recovery)" : "")}\n{GameplayContext.Cost(selectedCost ?? default)}";
        _confirm.Visible = selected is not null;
        _confirm.TooltipText = selected is null ? "" : $"Build {GameplayContext.BuildingName(selected.Type)}{(_payment == ConstructionPayment.GoldRecovery ? " (Gold recovery)" : "")} — {GameplayContext.Cost(selectedCost ?? default)}";
        _confirm.Disabled = selected is null || blocked.Length > 0; _reason.Text = blocked; _reason.TooltipText = blocked; _reason.Visible = blocked.Length > 0;
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
