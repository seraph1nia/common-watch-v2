using Game.Core;
using Game.Shared;
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
        var brief = EncounterBriefing.Project(state, city.Id);
        string composition = brief.Composition;
        Summary.TooltipText = brief.Details + "\n\nFull campaign purposes and clear rewards: City details.\nStored soldiers never fight.\nUnfunded field soldiers sit out; Food shortage permits Ready.\nEach production advances research and eligible stored-soldier recovery.";
        Summary.Text = $"Wave {state.Wave} → P{city.Id}: {threat.Length} enemies"
            + (composition.Length > 0 ? "\n" + composition : "");
        if (city.Eliminated) { Summary.Text += "\nFallen city · no participating army."; return; }
        if (city.FoodForecast is not { } food) { Summary.Text += "\nArmy / Food forecast unavailable."; return; }
        int stored = city.Soldiers.Count(u => u.Assignment is { Stored: true });
        int unfedStored = city.Soldiers.Count(u => u.Assignment is { Stored: true } && food.Unfed.Contains(u.Id));
        int sittingOut = food.Unfed.Length - unfedStored;
        Summary.Text += $"\nArmy: {food.Participating.Length} fight";
        if (sittingOut > 0) Summary.Text += $" · {sittingOut} sit out";
        if (unfedStored > 0) Summary.Text += $" · {unfedStored} stored unfunded";
        Summary.TooltipText += $"\n\nFood payment: {food.Paid}/{food.Demand} · {food.Available - food.Paid} left"
            + $"\n{stored} stored (never fight)"
            + (state.Phase == Phase.Preparation ? "\nIncludes final income" : "\nStock now; later income and purchases can change this");
    }
}
