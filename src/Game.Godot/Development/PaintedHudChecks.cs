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
        Require(_gameUi.LedgerArea.End.Y + 8 <= _gameUi.PhaseArea.Position.Y, $"resource ledger and phase frame have a clear gap: ledger={_gameUi.LedgerArea}, phase={_gameUi.PhaseArea}");
        Require(_gameUi.PhaseArea.Encloses(_gameUi.ReadyButton.GetGlobalRect()) && _gameUi.PhaseArea.Encloses(_gameUi.PauseButton.GetGlobalRect()), "complete Ready and Pause surfaces fit their frame");
        Require(_gameUi.ReadyButton.Size.Y >= 44 && _gameUi.PauseButton.Size.Y >= 44, "primary pointer targets remain at least 44 pixels high");
        for (int turn = 2; turn <= 3; turn++)
        {
            Click(_gameUi.ReadyButton.GetGlobalRect().GetCenter()); _frozen = true;
            Require(_session.Snapshot.Turn == turn && _session.Snapshot.Phase is Phase.Building or Phase.Preparation, "native pointer Ready advances exactly one actual preparation stage");
            await GameplayCapture("preparation-" + turn);
        }
        Click(_gameUi.PauseButton.GetGlobalRect().GetCenter()); _frozen = true;
        Require(_session.Snapshot.Paused, "native pointer Pause submits authoritative pause");
        _gameUi.PauseButton.GrabFocus();
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }, true);
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false }, true);
        _frozen = true;
        Require(!_session.Snapshot.Paused, "native controller accept resumes through focused Pause control");
        GD.Print("GODOT_PAINTED_HUD_PASS ledger-gap targets native-pointer-ready-three-stages pause controller-resume");
    }
}
