using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class ResourcePanel : HBoxContainer
{
    private readonly Dictionary<Resource, (Label Stock, Label Income)> _rows = [];
    private Label _heading = null!;
    public Label Summary { get; private set; } = null!;
    public void Initialize()
    {
        var grid = new GridContainer { Columns = 7, SizeFlagsHorizontal = SizeFlags.ExpandFill }; grid.AddThemeConstantOverride("h_separation", 12); AddChild(grid);
        grid.AddChild(new Label { Text = "Resource" });
        Resource[] order = [Resource.Gold, Resource.Food, Resource.Wood, Resource.Stone, Resource.Metal, Resource.Cloth];
        foreach (var resource in order) grid.AddChild(new Label { Text = resource.ToString(), SizeFlagsHorizontal = SizeFlags.ExpandFill });
        grid.AddChild(new Label { Text = "Stock" });
        foreach (var resource in order) { var label = new Label(); grid.AddChild(label); _rows[resource] = (label, new Label()); }
        _heading = new Label { Text = "Income / production" }; grid.AddChild(_heading);
        foreach (var resource in order) grid.AddChild(_rows[resource].Income);
        Summary = WrapLabel(); Summary.CustomMinimumSize = new(350, 0); AddChild(Summary);
    }
    public void Refresh(GameplayContext context)
    {
        var city = context.City; var state = context.State;
        bool inactive = city.Eliminated || state.Phase is Phase.Victory or Phase.Defeat;
        foreach (var pair in _rows)
        {
            pair.Value.Stock.Text = city.Resources.Amount(pair.Key).ToString();
            int? income = inactive ? 0 : city.ProductionIncome?.Amount(pair.Key);
            pair.Value.Income.Text = income is null ? "Unavailable" : income > 0 ? $"+{income}" : "0";
        }
        _heading.Text = state.Phase is Phase.Building or Phase.Preparation ? "Income / production" : "Next production";
        if (state.Phase is Phase.Building or Phase.Preparation)
        {
            var food = city.FoodForecast;
            Summary.Text = food is null ? "Next battle upkeep unavailable." :
                $"Next battle: demand {food.Demand} Food; stock {city.Food}\n" +
                (state.Phase == Phase.Preparation ? $"Final production +{city.ProductionIncome?.Food.ToString() ?? "?"} included; " : "") +
                $"pay {food.Paid}; left {food.Available - food.Paid}\nFunded field {food.Participating.Length}; unfunded {food.Unfed.Length}; " +
                $"sit out {city.Soldiers.Count(u => u.Assignment is { Stored: false } && food.Unfed.Contains(u.Id))}; stored {city.Soldiers.Count(u => u.Assignment?.Stored == true)}";
        }
        else Summary.Text = city.LastUpkeep is { } paid ? $"{(state.Phase == Phase.Combat ? "Paid this battle" : "Last battle")} W{paid.Wave}: {paid.Paid} Food\nFunded {paid.Funded.Length}; field {paid.Participating.Length}; unfunded {paid.Unfed.Length}" : "No battle upkeep paid yet.";
        if (!context.Session.Connected) Summary.Text += "\nDisconnected — synchronized values are stale.";
    }
}
