using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class ResourcePanel : GridContainer
{
    private readonly Dictionary<Resource, Label> _rows = [];
    public Label Summary { get; private set; } = null!;
    public Label CompactSummary { get; private set; } = null!;
    public void Initialize(HudSkin skin, Action foodDetails)
    {
        Columns = 6; SizeFlagsHorizontal = SizeFlags.ExpandFill; MouseFilter = MouseFilterEnum.Ignore;
        foreach (var resource in new[] { Resource.Gold, Resource.Food, Resource.Wood, Resource.Stone, Resource.Metal, Resource.Cloth })
        {
            var chip = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore }; AddChild(chip);
            if (skin.Icon(resource.ToString().ToLowerInvariant()) is { } icon)
                chip.AddChild(new TextureRect { Texture = icon, CustomMinimumSize = new(26, 26), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore });
            var label = new Label { MouseFilter = MouseFilterEnum.Ignore }; chip.AddChild(label); _rows.Add(resource, label);
            if (resource == Resource.Food)
            {
                var food = skin.Button("?", foodDetails); food.TooltipText = "Food stock, forecast and actual upkeep receipts";
                food.CustomMinimumSize = new(44, 44);
                chip.AddChild(food);
            }
        }
        Summary = WrapLabel(); CompactSummary = WrapLabel();
    }
    public void Reflow(float width)
    {
        float minimum = _rows.Values.Sum(label => label.GetCombinedMinimumSize().X) + 6 * 44 + 54;
        Columns = minimum > width ? 3 : 6;
    }
    public void Refresh(GameplayContext context)
    {
        var city = context.City; var state = context.State;
        bool inactive = city.Eliminated || state.Phase is Phase.Victory or Phase.Defeat;
        foreach (var pair in _rows)
        {
            int? income = inactive ? 0 : city.ProductionIncome?.Amount(pair.Key);
            string signed = income is null ? "?" : income.Value.ToString("+0;-0;+0");
            pair.Value.Text = $"{pair.Key} {city.Resources.Amount(pair.Key)}\n{signed} / production";
        }
        if (state.Phase is Phase.Building or Phase.Preparation)
        {
            var food = city.FoodForecast;
            Summary.Text = food is null ? "Next battle upkeep unavailable." :
                $"Next battle forecast: demand {food.Demand} Food; current stock {city.Food}\n" +
                (state.Phase == Phase.Preparation ? $"Final production +{city.ProductionIncome?.Food.ToString() ?? "?"} included in forecast; " : "") +
                $"pay {food.Paid}; left {food.Available - food.Paid}\nFunded field {food.Participating.Length}; unfunded {food.Unfed.Length}; " +
                $"sit out {city.Soldiers.Count(u => u.Assignment is { Stored: false } && food.Unfed.Contains(u.Id))}; stored {city.Soldiers.Count(u => u.Assignment?.Stored == true)}";
            CompactSummary.Text = food is null ? "Upkeep forecast unavailable" : $"Forecast: {food.Paid}/{food.Demand} Food · {food.Unfed.Length} unfunded";
        }
        else
        {
            Summary.Text = city.LastUpkeep is { } paid ? $"{(state.Phase == Phase.Combat ? "Paid this battle" : "Last battle")} W{paid.Wave}: {paid.Paid} Food\nFunded {paid.Funded.Length}; field {paid.Participating.Length}; unfunded {paid.Unfed.Length}" : "No battle upkeep paid yet.";
            CompactSummary.Text = city.LastUpkeep is { } receipt ? $"Paid W{receipt.Wave}: {receipt.Paid} Food · {receipt.Unfed.Length} unfunded" : "No upkeep paid yet";
        }
        if (!context.Session.Connected) Summary.Text += "\nDisconnected — synchronized values are stale.";
    }
}
