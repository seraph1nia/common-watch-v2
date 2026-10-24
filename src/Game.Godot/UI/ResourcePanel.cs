using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>Read-only ledger of authoritative stock, production projections and upkeep receipts.</summary>
public partial class ResourcePanel : VBoxContainer
{
    private readonly Dictionary<Resource, Label> _rows = [];
    private readonly Dictionary<Resource, Label> _income = [];
    private bool? _wide;
    internal IReadOnlyDictionary<Resource, Label> IncomeLabels => _income;
    internal float WideMinimumWidth => GetCombinedMinimumSize().X;
    internal void SetCompact(bool compact) { }
    public void Initialize(HudSkin skin)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill; MouseFilter = MouseFilterEnum.Ignore;
        AddThemeConstantOverride("separation", 4);
        var grid = new GridContainer { Columns = 4, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        grid.AddThemeConstantOverride("h_separation", 6); grid.AddThemeConstantOverride("v_separation", 3); AddChild(grid);
        foreach (string heading in new[] { "", "Resources", "Stock", "Income" })
        {
            var label = new Label { Text = heading, MouseFilter = MouseFilterEnum.Ignore };
            label.AddThemeFontSizeOverride("font_size", 14); label.AddThemeColorOverride("font_color", new Color("e3bd75")); grid.AddChild(label);
        }
        foreach (var resource in new[] { Resource.Gold, Resource.Food, Resource.Wood, Resource.Stone, Resource.Metal, Resource.Cloth })
        {
            grid.AddChild(new TextureRect { Texture = skin.Icon(resource.ToString().ToLowerInvariant()), CustomMinimumSize = new(22, 22),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore });
            var name = new Label { Text = resource.ToString(), SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore }; grid.AddChild(name);
            var stock = new Label { HorizontalAlignment = HorizontalAlignment.Right, CustomMinimumSize = new(36, 0), MouseFilter = MouseFilterEnum.Ignore };
            stock.AddThemeFontSizeOverride("font_size", 18); grid.AddChild(stock); _rows.Add(resource, stock);
            var income = new Label { HorizontalAlignment = HorizontalAlignment.Right, MouseFilter = MouseFilterEnum.Ignore };
            income.AddThemeFontSizeOverride("font_size", 14); income.AddThemeColorOverride("font_color", new Color("b7ddc8")); grid.AddChild(income); _income.Add(resource, income);
        }
    }
    public void Refresh(GameplayContext context)
    {
        var city = context.City; var state = context.State;
        bool wide = GetViewportRect().Size.X >= 1600;
        if (_wide != wide)
        {
            _wide = wide;
            foreach (var label in _rows.Values) label.AddThemeFontSizeOverride("font_size", wide ? 18 : 16);
        }
        bool inactive = city.Eliminated || state.Phase is Phase.Victory or Phase.Defeat;
        foreach (var pair in _rows)
        {
            int? income = inactive ? 0 : city.ProductionIncome?.Amount(pair.Key);
            string signed = income is null ? "?" : income.Value.ToString("+0;-0;+0");
            pair.Value.Text = city.Resources.Amount(pair.Key).ToString();
            _income[pair.Key].Text = signed + " / prod.";
            pair.Value.TooltipText = _income[pair.Key].TooltipText = $"{pair.Key}: available stock now; {signed} each production stage.";
        }
    }
}
