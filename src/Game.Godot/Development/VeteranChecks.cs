// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckVeteranChoices()
    {
        ReplaceSession(GameplayScenarios.CreateAt("town-hall")); _frozen = true; RefreshUi(true);
        var unit = _session.City.Soldiers.First(u => u.Assignment is { Stored: true } && u.Health < u.Profile.Health);
        _gameUi.Inspect(unit); var inspector = _gameUi.Details.Inspector;
        Require(!inspector.Details.Contains("Service history") && inspector.AdvancedDetails.Contains("Service history") && !inspector.AdvancedDetails.Contains("XP"), "history stays optional and never implies stat progression");
        long revision = _session.Snapshot.Revision;
        Press(inspector.VeteranButton);
        inspector.VeteranButton.GrabFocus();
        await GameplayCapture("veteran-recovery-comparison");
        Require(_session.Snapshot.Revision == revision && inspector.VeteranDetails.Contains("next production HP")
            && inspector.VeteranDetails.Contains("new L4") && inspector.VeteranDetails.Contains("no refund"), "real recovery and replacement quotes inspect without a command");
        Require(inspector.VeteranCopyArea.Size.Y >= 190, "veteran comparison retains a scrollable copy area");
        inspector.VeteranViewport.GrabFocus();
        inspector.VeteranViewport.ScrollVertical = (int)inspector.VeteranViewport.GetVScrollBar().MaxValue;
        await GameplayCapture("veteran-replacement-quote");
        var options = VeteranComparison.Project(_session.Snapshot, _session.City, unit.Id);
        int health = options.Recovery.Single(r => r.StoredHere).HealthAfterProduction;
        _gameUi.Details.Close(); _session.Send("ready"); _frozen = true; RefreshUi(true);
        var healed = _session.City.Soldiers.Single(u => u.Id == unit.Id);
        Require(healed.Health == health && healed.Level == unit.Level && healed.Experience == unit.Experience, "native recovery review matches actual production without service/equipment gain");

        ReplaceSession(GameplayScenarios.CreateAt("late")); _frozen = true; RefreshUi(true);
        unit = _session.City.Soldiers.First(u => u.Level < _session.City.Slots[2].Level);
        _gameUi.Inspect(unit); inspector = _gameUi.Details.Inspector;
        revision = _session.Snapshot.Revision;
        inspector.Buttons["Retire"].GrabFocus();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        WorldtreeClick(inspector.Buttons["Retire"].GetGlobalRect().GetCenter());
        await GameplayCapture("veteran-retirement-review");
        Require(inspector.Buttons["CancelRetire"].HasFocus() && _session.Snapshot.Revision == revision
            && _session.City.Soldiers.Any(u => u.Id == unit.Id), "review defaults to keeping the soldier and spends nothing");
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = true }, true);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.Enter, Pressed = false }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_session.Snapshot.Revision == revision && inspector.Buttons["Retire"].HasFocus(), "cancel retains member and native focus");
        Press(inspector.Buttons["Retire"]);
        var staleConfirm = inspector.Buttons["ConfirmRetire"];
        _gameUi.Details.Close(); staleConfirm.EmitSignal(Button.SignalName.Pressed);
        Require(_session.Snapshot.Revision == revision && inspector.SelectedUnit == unit.Id, "closing inspection cancels retirement while preserving the member selection");
        _gameUi.Inspect(unit); Press(inspector.Buttons["Retire"]);
        var other = _session.City.Soldiers.First(u => u.Id != unit.Id);
        _gameUi.Inspect(other); RefreshUi(true);
        staleConfirm.EmitSignal(Button.SignalName.Pressed);
        Require(_session.Snapshot.Revision == revision && _session.City.Soldiers.Any(u => u.Id == unit.Id), "changing selection cancels old retirement intent");
        _gameUi.Inspect(unit); Press(inspector.Buttons["Retire"]);
        _session.Send("ready"); _frozen = true; RefreshUi(true);
        long commandSequence = _session.ObservedCommand?.Sequence ?? 0;
        staleConfirm.EmitSignal(Button.SignalName.Pressed);
        Require((_session.ObservedCommand?.Sequence ?? 0) == commandSequence && _session.City.Soldiers.Any(u => u.Id == unit.Id) && _session.LastResult?.Accepted == true, "ready/phase change makes an old confirmation read-only");
        // A separate replacement session can reuse member IDs; old UI intent must not act on it.
        ReplaceSession(GameplayScenarios.CreateAt("late")); _frozen = true; RefreshUi(true);
        revision = _session.Snapshot.Revision; staleConfirm.EmitSignal(Button.SignalName.Pressed);
        Require(_session.Snapshot.Revision == revision, "session replacement cancels old retirement intent");
        unit = _session.City.Soldiers.First(u => u.Level < _session.City.Slots[2].Level);
        _gameUi.Inspect(unit); var stock = _session.City.Resources; int count = _session.City.Soldiers.Length;
        Press(inspector.Buttons["Retire"]);
        inspector.Buttons["ConfirmRetire"].GrabFocus();
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = true }, true);
        GetViewport().PushInput(new InputEventJoypadButton { ButtonIndex = JoyButton.A, Pressed = false }, true);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        _frozen = true; RefreshUi(true);
        Require(_session.LastResult?.Accepted == true && _session.City.Soldiers.Length == count - 1
            && !_session.City.Soldiers.Any(u => u.Id == unit.Id) && _session.City.Resources == stock, "confirmed retirement removes exactly one member without a refund or replacement");
        Require(GetViewport().GuiGetFocusOwner() is { } focus && focus.IsVisibleInTree(), "retirement restores visible native focus");
        _gameUi.Details.Close();
        GD.Print("GODOT_VETERAN_PASS history equipment current-quotes exact-recovery review cancel confirm stale-session no-refund focus");
    }
}
