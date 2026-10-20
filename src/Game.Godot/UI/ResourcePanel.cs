using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class ResourcePanel : GridContainer
{
    private readonly Dictionary<Resource, Label> _rows = [];
    public void Initialize(HudSkin skin)
    {
        Columns = 6; SizeFlagsHorizontal = SizeFlags.ExpandFill; MouseFilter = MouseFilterEnum.Ignore;
        foreach (var resource in new[] { Resource.Gold, Resource.Food, Resource.Wood, Resource.Stone, Resource.Metal, Resource.Cloth })
        {
            var chip = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore }; AddChild(chip);
            if (skin.Icon(resource.ToString().ToLowerInvariant()) is { } icon)
                chip.AddChild(new TextureRect { Texture = icon, CustomMinimumSize = new(26, 26), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore });
            var values = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore }; chip.AddChild(values);
            values.AddThemeConstantOverride("separation", 0);
            var stock = new Label { MouseFilter = MouseFilterEnum.Ignore };
            stock.AddThemeFontSizeOverride("font_size", 14);
            values.AddChild(stock); _rows.Add(resource, stock);
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
            pair.Value.TooltipText = $"{signed} / production";
        }
    }
}
