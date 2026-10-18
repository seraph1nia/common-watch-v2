using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class GameApplication
{
    private void SmokeSessionLifecycle()
    {
        ReturnToMenu(); Require(_session is null && _sessionMenu.Visible, "menu owns no authority");
        _sessionMenu.SoloButton.EmitSignal(BaseButton.SignalName.Pressed);
        var old = _session!; string match = old.Snapshot.MatchId;
        Require(old.Snapshot.Phase == Phase.Lobby, "pregame selection");
        ((Button)_sessionMenu.Doctrines.GetChild(1)).EmitSignal(BaseButton.SignalName.Pressed);
        Require(old.City.Run.Doctrine == RunCatalog.Doctrines[1].Id, "doctrine UI");
        _sessionMenu.StartButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(old.Snapshot.Phase == Phase.Building, "start UI");
        ReturnToMenu(); BeginSolo();
        Require(_session!.Snapshot.MatchId != match && _session.City.Run.Doctrine == "warden", "replacement isolation");
        try { old.Send("ready"); throw new InvalidOperationException("Disposed session accepted input"); }
        catch (ObjectDisposedException) { }
        ReplaceSession(ScenarioRunner.CreateAt("combat")); ReturnToMenu(); BeginSolo();
        Require(_session.Snapshot.Phase == Phase.Lobby && _battlefield.Motion.Events.Count == 0, "leave during combat");
        GD.Print("GODOT_SESSION_PASS menu doctrine start replacement cleanup combat-leave offline");
    }
}
