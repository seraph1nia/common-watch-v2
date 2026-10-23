using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class ConstructionPanel : VBoxContainer
{
    public bool Compact { get; init; }
    public bool SharedNotice { get; init; }
    private HudSkin _skin = null!;
    private GameplayContext _context = null!;
    private readonly Dictionary<Building, Button> _buttons = [];
    private GridContainer _cards = null!;
    private int _category;
    private readonly List<Button> _categories = [];
    private int _slot, _city;
    private object? _scope;
    private Building? _selected;
    private ConstructionPayment _payment;
    public Button? RecoveryButton { get; private set; }
    private Button _confirm = null!;
    private Label _reason = null!;
    private Label _recoveryHint = null!;
    public Rect2 CardArea => _cards.GetGlobalRect();
    public Button ConfirmButton => _confirm;
    public IReadOnlyList<Button> Categories => _categories;
    public IReadOnlyDictionary<Building, Button> BuildButtons => _buttons;
    public void Initialize(HudSkin skin)
    {
        _skin = skin;
        var tabs = new HFlowContainer(); tabs.AddThemeConstantOverride("separation", 4); AddChild(tabs);
        foreach (string name in new[] { "Production", "Military", "Defense", "Trade", "Utility" })
        {
            int index = _categories.Count;
            var tab = skin.Button(name, () => { _category = index; _selected = null; Refresh(_context, _slot); }, skin.Painted is null ? "" : "PaintedTab");
            tab.AddThemeFontSizeOverride("font_size", 14);
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled", "focus" })
            {
                var style = (StyleBox)skin.Theme.GetStylebox(state, skin.Painted is null ? "Button" : "PaintedTab").Duplicate();
                style.ContentMarginLeft = style.ContentMarginRight = 8;
                tab.AddThemeStyleboxOverride(state, style);
            }
            tab.ToggleMode = true; tab.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            skin.Painted?.IndicateSelection(tab);
            _categories.Add(tab); tabs.AddChild(tab);
        }
        _recoveryHint = WrapLabel(); _recoveryHint.AddThemeFontSizeOverride("font_size", 14); AddChild(_recoveryHint); _recoveryHint.Hide();
        _cards = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill }; AddChild(_cards);
        Resized += Reflow;
        RecoveryButton = skin.Button("", () => { _selected = Building.Lumbermill; _payment = ConstructionPayment.GoldRecovery; Refresh(_context, _slot); }, skin.Painted is null ? "" : "ForestCard");
        if (skin.Painted is not null)
        {
            RecoveryButton.AddThemeFontSizeOverride("font_size", 14); RecoveryButton.CustomMinimumSize = new(0, 92);
            foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
            {
                var style = (StyleBox)skin.Theme.GetStylebox(state, "ForestCard").Duplicate();
                style.ContentMarginLeft = style.ContentMarginRight = 12;
                RecoveryButton.AddThemeStyleboxOverride(state, style);
            }
        }
        RecoveryButton.ToggleMode = true; RecoveryButton.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        skin.Painted?.IndicateSelection(RecoveryButton);
        BoxContainer choice = Compact ? new VBoxContainer() : new HBoxContainer(); AddChild(choice);
        choice.AddChild(RecoveryButton); RecoveryButton.Hide(); RecoveryButton.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _confirm = skin.Button("Select a building", () =>
        {
            if (_confirm.IsVisibleInTree() && !_confirm.Disabled && _context.EditReason.Length == 0 && _selected is { } type) _context.Submit(new(GameplayAction.Build, _slot, type, Payment: _payment));
        }, "ForestPrimaryButton");
        _confirm.AutowrapMode = TextServer.AutowrapMode.WordSmart; _confirm.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        _confirm.CustomMinimumSize = new(300, 48); choice.AddChild(_confirm);
        _reason = WrapLabel(); _reason.CustomMinimumSize = new(200, 0); choice.AddChild(_reason);

    }
    private void Reflow()
    {
        float separation = _cards.GetThemeConstant("h_separation");
        int columns = Math.Clamp((int)((Size.X + separation) / (176 + separation)), 1, 4);
        int visible = _buttons.Values.Count(button => button.Visible);
        if (visible is > 3 and <= 6 && columns >= 3) columns = 3;
        if (_cards.Columns != columns) _cards.Columns = columns;
    }
    private Button CreateCard(Action choose)
    {
        var button = new ConstructionCard { Theme = _skin.Theme, ThemeTypeVariation = _skin.Illustrated ? "ForestCard" : "" };
        button.Pressed += () => { if (button.IsVisibleInTree() && !button.Disabled) choose(); };
        _cards.AddChild(button); button.Initialize(_skin.Painted is not null); _skin.Painted?.IndicateSelection(button); return button;
    }
    private static string Availability(GameplayContext context, ResourceCost cost, string plotReason = "")
    {
        if (context.EditReason.Length > 0) return context.EditReason;
        if (plotReason.Length > 0) return plotReason;
        return string.Join(" · ", Enum.GetValues<Resource>().Where(r => context.City.Resources.Amount(r) < cost.Amount(r))
            .Select(r => $"Short {cost.Amount(r) - context.City.Resources.Amount(r)} {r}"));
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
        var plot = ArmyConfiguration.IsArmyPlot(slot) ? context.City.Army!.Homes.Single(h => h.PlotId == slot).Plot : context.City.Slots[slot];
        object scope = (context.Session, context.State.MatchId, context.City.Id, slot, plot.Generation);
        if (!Equals(_scope, scope)) ResetSelection();
        _scope = scope;
        if (ArmyConfiguration.IsArmyPlot(slot)) _category = 2;
        for (int i = 0; i < _categories.Count; i++) { _categories[i].SetPressedNoSignal(i == _category); _categories[i].Visible = !ArmyConfiguration.IsArmyPlot(slot); }
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
            ((ConstructionCard)button).Present(GameplayContext.BuildingName(type),
                definition.Produces is { } resource ? $"Produces {resource}" : definition.Recruits is { } recruits ? $"Trains {string.Join(", ", recruits)}" : definition.Type switch
                { Building.TownHall => "Veterans & recovery", Building.ResearchTower => "Research progress", Building.Market => "Trade for Gold", _ => "Automatic defense" },
                GameplayContext.Cost(definition.Construction),
                Availability(context, definition.Construction, wrongPlot ? "Army homes accept defense towers only." : ""), Function(definition),
                _selected == type && _payment == ConstructionPayment.Standard, showReason: !SharedNotice || context.EditReason.Length == 0);
            ((ConstructionCard)button).SetThumbnail(_skin.BuildingSprite(type), _skin.Painted?.Texture("portrait"));
            if (context.EditReason.Length == 0 && context.CostReason(definition.Construction) is { Length: > 0 } shortage)
                button.TooltipText += "\n" + shortage;
            button.Visible = category == _category;
        }
        var lumber = context.State.BuildingCatalog.First(b => b.Type == Building.Lumbermill);
        _recoveryHint.Visible = !ArmyConfiguration.IsArmyPlot(slot) && context.City.Wood == 0 && lumber.RecoveryConstruction is not null;
        _recoveryHint.Text = lumber.RecoveryConstruction is { } recovery ? "Zero Wood: Lumbermill Gold recovery · " + GameplayContext.Cost(recovery) + " (Production)." : "";
        RecoveryButton!.Visible = !ArmyConfiguration.IsArmyPlot(slot) && context.City.Wood == 0
            && lumber.RecoveryConstruction is not null && _category == 0;
        if (!ArmyConfiguration.IsArmyPlot(slot) && context.City.Wood == 0 && lumber.RecoveryConstruction is { } quote)
        {
            RefreshCard(RecoveryButton, $"{(_selected == Building.Lumbermill && _payment == ConstructionPayment.GoldRecovery ? "✓ " : "")}Gold recovery · {GameplayContext.Cost(quote)}\nLumbermill · no Wood required", Availability(context, quote),
                _selected == Building.Lumbermill && _payment == ConstructionPayment.GoldRecovery);

        }
        var selected = context.State.BuildingCatalog.FirstOrDefault(definition => definition.Type == _selected);
        ResourceCost? selectedCost = _payment == ConstructionPayment.GoldRecovery ? selected?.RecoveryConstruction : selected?.Construction;
        string blocked = selected is null ? "Select a building; confirmation spends the quoted resources." : context.Block(selectedCost,
            ArmyConfiguration.IsArmyPlot(slot) && !ArmyConfiguration.IsTower(selected.Type) ? "Army homes accept defense towers only." : "");
        _confirm.Text = selected is null ? "Build selected" : $"Build {GameplayContext.BuildingName(selected.Type)} · {GameplayContext.Cost(selectedCost ?? default)}";
        _confirm.Visible = selected is not null;
        _confirm.TooltipText = selected is null ? "" : $"Build {GameplayContext.BuildingName(selected.Type)}{(_payment == ConstructionPayment.GoldRecovery ? " (Gold recovery)" : "")} — {GameplayContext.Cost(selectedCost ?? default)}";
        _confirm.Disabled = selected is null || blocked.Length > 0; _reason.Text = blocked; _reason.TooltipText = blocked; _reason.Visible = blocked.Length > 0 && !(SharedNotice && blocked == context.EditReason);
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
