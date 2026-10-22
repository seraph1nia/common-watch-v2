using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckPaintedHudControls()
    {
        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true;
        _session.SelectSlot(-1); await GameplayCapture("preparation-1");
        CheckPreparationSummary();
        Require(_gameUi.ReadyButton.Text.Contains("prep 2"), "Ready names the next preparation without a tooltip");
        Require(_gameUi.LedgerArea.End.Y + 8 <= _gameUi.PhaseArea.Position.Y, $"resource ledger and phase frame have a clear gap: ledger={_gameUi.LedgerArea}, phase={_gameUi.PhaseArea}");
        Require(_gameUi.PhaseArea.Encloses(_gameUi.ReadyButton.GetGlobalRect()) && _gameUi.PhaseArea.Encloses(_gameUi.PauseButton.GetGlobalRect()), "complete Ready and Pause surfaces fit their frame");
        Require(_gameUi.ReadyButton.Size.Y >= 44 && _gameUi.PauseButton.Size.Y >= 44, "primary pointer targets remain at least 44 pixels high");
        for (int turn = 2; turn <= 3; turn++)
        {
            var before = _session.City.Resources;
            var income = _session.City.ProductionIncome!.Value;
            int productions = _session.Snapshot.ProductionCount;
            Click(_gameUi.ReadyButton.GetGlobalRect().GetCenter()); _frozen = true;
            Require(_session.Snapshot.Turn == turn && _session.Snapshot.Phase is Phase.Building or Phase.Preparation, "native pointer Ready advances exactly one actual preparation stage");
            Require(_session.Snapshot.ProductionCount == productions + 1 && before.TryAdd(income, out var expected) && _session.City.Resources == expected,
                "each Ready retains exactly one authoritative income opportunity");
            Require(_gameUi.ReadyButton.Text.Contains(turn == 3 ? "battle" : "prep 3"), "Ready distinguishes preparation from battle");
            MoveMouse(new(10, 180));
            await GameplayCapture("preparation-" + turn);
            CheckPreparationSummary();
        }
        Click(_gameUi.PauseButton.GetGlobalRect().GetCenter()); _frozen = true;
        Require(_session.Snapshot.Paused, "native pointer Pause submits authoritative pause");
        _gameUi.PauseButton.GrabFocus();
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }, true);
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false }, true);
        _frozen = true;
        Require(!_session.Snapshot.Paused, "native controller accept resumes through focused Pause control");
        Require(!_gameUi.ReadyButton.Disabled, "resuming restores readiness");
        var forecast = _session.City.FoodForecast!;
        int productionCount = _session.Snapshot.ProductionCount;
        Click(_gameUi.ReadyButton.GetGlobalRect().GetCenter()); _frozen = true;
        Require(_session.Snapshot.Phase == Phase.Combat && _session.Snapshot.ProductionCount == productionCount + 1,
            "third Ready earns the final production then begins combat");
        Require(_session.City.LastUpkeep is { } receipt && receipt.Paid == forecast.Paid && receipt.Participating.SequenceEqual(forecast.Participating),
            "final forecast agrees with authoritative battle receipt");
        GD.Print("GODOT_PAINTED_HUD_PASS ledger-gap targets native-pointer-ready-three-stages pause controller-resume");
    }
    private void CheckPreparationSummary()
    {
        var city = _session.ViewedCity; var state = _session.Snapshot;
        var label = _gameUi.PreparationSummary;
        int enemies = state.NextWave.Count(u => u.Destination == city.Id);
        Require(label.Text.Contains($"Wave {state.Wave} → P{city.Id}: {enemies} enemies"), "visible threat counts only the inspected city's destination manifest");
        Require(label.Text.Contains($"Army: {city.FoodForecast!.Participating.Length} fight"), "visible army count uses authoritative participation");
        Require(label.Text.Contains($"Food payment: {city.FoodForecast.Paid}/{city.FoodForecast.Demand}"), "visible Food payment uses authoritative forecast");
        Require(label.Text.Contains(state.Phase == Phase.Preparation ? "Includes final income" : "Stock now"), "forecast distinguishes final income from current stock");
        Require(label.IsVisibleInTree() && _gameUi.PreparationArea.Encloses(label.GetGlobalRect()) && GetViewport().GetVisibleRect().Encloses(_gameUi.PreparationArea), "entire preparation summary fits its frame and viewport");
        Require(_gameUi.LedgerArea.End.Y + 8 <= _gameUi.PhaseArea.Position.Y,
            $"preparation ledger and phase frame remain separate: ledger={_gameUi.LedgerArea}, phase={_gameUi.PhaseArea}");
    }
}
