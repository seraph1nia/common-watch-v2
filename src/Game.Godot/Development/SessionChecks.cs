using Godot;
using Game.Core;
using Game.Shared;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task SmokeSessionLifecycle()
    {
        ReturnToMenu(); Require(_session is null && _sessionMenu.Visible, "menu owns no authority");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(_sessionMenu.MainButtons.GetChildren().OfType<Button>().Select(b => b.Text).SequenceEqual(new[] { "Single Player", "Multiplayer", "Settings", "Exit Game" }), "exactly four main menu routes");
        _sessionMenu.SettingsButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_settings.Visible, "main menu settings"); _settings.Close();
        _sessionMenu.MultiplayerButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_sessionMenu.MultiplayerContent.Visible && !_sessionMenu.MainButtons.Visible, "separate multiplayer screen with LAN and Steam");
        _camera.SetPose(1.4f, new Vector2(37, -23));
        string endpoint = _sessionMenu.Endpoint.Text;
        _sessionMenu.Endpoint.GrabFocus(); _sessionMenu.Endpoint.CaretColumn = endpoint.Length;
        GetViewport().PushInput(new InputEventKey { Keycode = Key.R, PhysicalKeycode = Key.R, Unicode = 'r', Pressed = true }, true);
        GetViewport().PushInput(new InputEventKey { Keycode = Key.R, PhysicalKeycode = Key.R, Pressed = false }, true);
        Require(_sessionMenu.Endpoint.Text == endpoint + "r" && _camera.Magnification == 1.4f, "endpoint typing uses native focus and does not reset the camera");
        _sessionMenu.Endpoint.Text = endpoint; _camera.Reset(); GetViewport().GuiReleaseFocus();
        ReturnToMenu();
        _sessionMenu.SoloButton.EmitSignal(BaseButton.SignalName.Pressed);
        var old = _session!; string match = old.Snapshot.MatchId;
        Require(old.GetPath() == new NodePath("/root/CommonWatch/Session"), "stable native RPC node path");
        Require(old.Snapshot.Phase == Phase.Lobby, "pregame selection");
        Require(_sessionMenu.StartButton.Disabled && _sessionMenu.Offers.Count == 3 && _sessionMenu.Offers.Distinct().Count() == 3, "three distinct offers require explicit selection");
        string[] offers = _sessionMenu.Offers.ToArray();
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080), new Vector2I(1280, 720) })
        {
            GetWindow().Size = size; await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(GetViewport().GetVisibleRect().Encloses(_sessionMenu.BackButton.GetGlobalRect()), "doctrine setup fits after growing and shrinking the window");
            RefreshUi(true); _settings.Open(); _settings.Close(); RefreshUi(true);
            Require(offers.SequenceEqual(_sessionMenu.Offers) && _sessionMenu.StartButton.Disabled, "resize/settings preserve unselected offers");
        }
        ((DoctrineCard)_sessionMenu.Doctrines.GetChild(1)).ChooseButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(old.City.Run.Doctrine == offers[1] && !_sessionMenu.StartButton.Disabled, "offered doctrine applied explicitly");
        ((DoctrineCard)_sessionMenu.Doctrines.GetChild(2)).ChooseButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(old.City.Run.Doctrine == offers[2] && ((DoctrineCard)_sessionMenu.Doctrines.GetChild(2)).ChooseButton.ButtonPressed && !((DoctrineCard)_sessionMenu.Doctrines.GetChild(1)).ChooseButton.ButtonPressed, "selection replaces previous highlight");
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
