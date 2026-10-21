using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class ResourcePanel : GridContainer
{
    private readonly Dictionary<Resource, Label> _rows = [];
    private readonly Dictionary<Resource, Label> _income = [];
    private readonly Dictionary<Resource, BoxContainer> _values = [];
    private readonly Dictionary<Resource, string> _signed = [];
    private bool _compact, _icons;
    internal IReadOnlyDictionary<Resource, Label> IncomeLabels => _income;
    internal float WideMinimumWidth => _rows.Sum(pair => Mathf.Max(pair.Value.GetCombinedMinimumSize().X,
        _income[pair.Key].GetThemeFont("font").GetStringSize((_signed.GetValueOrDefault(pair.Key) ?? "+0") + " / production", fontSize: 14).X)
        + (_icons ? 26 + GetThemeConstant("h_separation", "HBoxContainer") : 0)) + 5 * GetThemeConstant("h_separation");
    internal void SetCompact(bool compact)
    {
        if (_compact == compact) return;
        _compact = compact;
        foreach (var (resource, values) in _values)
        {
            values.Vertical = !compact; values.AddThemeConstantOverride("separation", compact ? 6 : 0);
            _income[resource].Text = _signed.GetValueOrDefault(resource, "+0") + (compact ? " / prod." : " / production");
        }
    }
    public void Initialize(HudSkin skin)
    {
        Columns = 6; SizeFlagsHorizontal = SizeFlags.ExpandFill; MouseFilter = MouseFilterEnum.Ignore;
        foreach (var resource in new[] { Resource.Gold, Resource.Food, Resource.Wood, Resource.Stone, Resource.Metal, Resource.Cloth })
        {
            var chip = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore }; AddChild(chip);
            if (skin.Icon(resource.ToString().ToLowerInvariant()) is { } icon)
            {
                _icons = true;
                chip.AddChild(new TextureRect { Texture = icon, CustomMinimumSize = new(26, 26), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore });
            }
            var values = new BoxContainer { Vertical = true, MouseFilter = MouseFilterEnum.Ignore }; chip.AddChild(values); _values.Add(resource, values);
            values.AddThemeConstantOverride("separation", 0);
            var stock = new Label { MouseFilter = MouseFilterEnum.Ignore };
            stock.AddThemeFontSizeOverride("font_size", 18);
            values.AddChild(stock); _rows.Add(resource, stock);
            var income = new Label { MouseFilter = MouseFilterEnum.Ignore };
            income.AddThemeFontSizeOverride("font_size", 14);
            income.AddThemeColorOverride("font_color", new Color("b7ceba"));
            values.AddChild(income); _income.Add(resource, income);
        }
    }
    public void Refresh(GameplayContext context)
    {
        var city = context.City; var state = context.State;
        bool inactive = city.Eliminated || state.Phase is Phase.Victory or Phase.Defeat;
        foreach (var pair in _rows)
        {
            int? income = inactive ? 0 : city.ProductionIncome?.Amount(pair.Key);
            string signed = income is null ? "?" : income.Value.ToString("+0;-0;+0");
            pair.Value.Text = $"{pair.Key} {city.Resources.Amount(pair.Key)}";
            _signed[pair.Key] = signed;
            _income[pair.Key].Text = signed + (_compact ? " / prod." : " / production");
            pair.Value.TooltipText = _income[pair.Key].TooltipText = $"{pair.Key}: stock available now; {signed} at each production stage.";
        }
    }
}
