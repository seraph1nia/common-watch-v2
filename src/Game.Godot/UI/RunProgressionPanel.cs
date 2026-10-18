using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class RunProgressionPanel : VBoxContainer
{
    private Label _summary = null!;
    public void Initialize() { _summary = WrapLabel(); AddChild(_summary); }
    public void Refresh(GameplayContext context)
    {
        var city = context.City; var state = context.State;
        string outcome = state.Phase is Phase.Victory or Phase.Defeat ?
            $"{state.Phase.ToString().ToUpperInvariant()} — wave {state.Wave}/{state.TotalWaves}\n" +
            (state.Phase == Phase.Defeat ? PresentationLimits.DefeatText(state.DefeatReason) : "Twenty waves held. The run is complete.") + "\n" +
            string.Join("\n", state.Players.Select(p => $"P{p.Id}: {(p.Eliminated ? "fallen" : "surviving")}, Keep HP {HealthPoints.Format(p.Health)}")) + "\n\n" : "";
        _summary.Text = outcome + $"P{city.Id} RUN — wave {state.Wave}/{state.TotalWaves}; {state.ProductionCount} production settlements\n" + RunPresentation.Inspection(city) +
            $"\n\nKeep: {HealthPoints.Format(city.Health)}/{state.Rules.CityHealth}; {(city.Eliminated ? "fallen" : "surviving")}\nRoster {city.Soldiers.Length}, stored {city.Soldiers.Count(u => u.Assignment?.Stored == true)}\nResearch {city.Research.Points}, acquired {city.Research.Technologies.Length}\nLast rewarded wave {state.LastRewardedWave}";
        if (city.LastReward is { } receipt) _summary.Text += $"\nLast clear W{receipt.Wave}: {GameplayContext.Cost(receipt.Amount)}; research +{receipt.Research}";

    }
}
