using Game.Core;
using Godot;

namespace Game.GodotClient;

/// <summary>Read-only preparation decision summary from the inspected city's authoritative projections.</summary>
public partial class PreparationPanel : PanelContainer
{
    internal Label Summary { get; private set; } = null!;
    public void Initialize(HudSkin skin)
    {
        skin.Panel(this);
        Summary = WrapLabel(); Summary.AddThemeFontSizeOverride("font_size", 14); AddChild(Summary);
    }
    public void Refresh(GameplayContext context, bool finishingCombat)
    {
        var city = context.City; var state = context.State;
        Visible = !finishingCombat && state.Phase is Phase.Building or Phase.Preparation;
        if (!Visible) return;
        var threat = state.NextWave.Where(u => u.Destination == city.Id).ToArray();
        string composition = string.Join(" · ", threat.GroupBy(u => (u.Type, u.IsBoss))
            .Select(g => $"{g.Count()} {g.Key.Type}{(g.Key.IsBoss ? " BOSS" : "")}"));
        Summary.Text = $"Wave {state.Wave} → P{city.Id}: {threat.Length} enemies"
            + (city.Id != context.Session.PlayerId ? " · Allied forecast" : " · Your forecast")
            + (composition.Length > 0 ? "\n" + composition : "");
        if (city.Eliminated) { Summary.Text += "\nFallen city · no participating army."; return; }
        Summary.Text += "\nNext income: " + (city.ProductionIncome is { } income ? GameplayContext.Cost(income) : "Unavailable");
        if (city.FoodForecast is not { } food) { Summary.Text += "\nArmy / Food forecast unavailable."; return; }
        int stored = city.Soldiers.Count(u => u.Assignment is { Stored: true });
        int unfedStored = city.Soldiers.Count(u => u.Assignment is { Stored: true } && food.Unfed.Contains(u.Id));
        int sittingOut = food.Unfed.Length - unfedStored;
        Summary.Text += $"\nArmy: {food.Participating.Length} fight · {sittingOut} sit out · {stored} stored";
        Summary.Text += $"\nFood payment: {food.Paid}/{food.Demand} · {food.Available - food.Paid} left";
        Summary.Text += state.Phase == Phase.Preparation
            ? "\nIncludes final income · Food paid when battle starts"
            : "\nStock now · later income and purchases can change this";
        if (unfedStored > 0) Summary.Text += $"\n{unfedStored} stored unfunded for next battle's recovery";
        Summary.TooltipText = "Stored soldiers never fight.\nUnfunded field soldiers sit out; Food shortage permits Ready.\nEach production advances research and eligible stored-soldier recovery.";
    }
}
