using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckSessionExitMenu()
    {
        var originalSize = GetWindow().Size;
        ReplaceSession(ScenarioRunner.CreateAt("combat")); _frozen = true; RefreshUi(true);
        var session = _session;
        long revision = session.Snapshot.Revision;
        bool paused = session.Snapshot.Paused;
        GetViewport().GuiReleaseFocus();
        CancelUi();
        Require(_menuPopup.Visible && !_settings.Visible, "Escape opens the session menu");
        Require(_menuPopup.ReturnToMenuButton is { Disabled: false } && !_menuPopup.SoloButton.Visible && !_menuPopup.MultiplayerButton.Visible,
            "session menu exposes return and exit without replacement routes");
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
        {
            GetWindow().Size = size;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            Click(_menuPopup.ReturnToMenuButton!.GetGlobalRect().GetCenter());
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(_exitConfirmation.Visible && _session == session && _exitConfirmation.GetCancelButton().HasFocus(), "return asks before disposing and focuses Cancel");
            Require(_exitConfirmation.DialogText.Contains("not saved") && _exitConfirmation.DialogText.Contains("cannot be resumed"), "solo warning explains progress loss");
            Require(_exitConfirmation.Size.X <= size.X && _exitConfirmation.Size.Y <= size.Y, "confirmation fits viewport");
            await GameplayCapture("confirm-return-" + size.X);
            // Escape belongs to the confirmation window and must not dismiss its parent menu.
            _exitConfirmation.PushInput(new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape, Pressed = true }, true);
            _exitConfirmation.PushInput(new InputEventKey { Keycode = Key.Escape, PhysicalKeycode = Key.Escape, Pressed = false }, true);
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(!_exitConfirmation.Visible && _menuPopup.Visible && _menuPopup.ReturnToMenuButton.HasFocus(),
                $"Escape cancels confirmation and restores invoking focus: dialog={_exitConfirmation.Visible} menu={_menuPopup.Visible} focus={GetViewport().GuiGetFocusOwner()?.GetPath()}");
            Click(_menuPopup.QuitButton.GetGlobalRect().GetCenter());
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(_exitConfirmation.Visible && _exitConfirmation.DialogText.Contains("game will close") && !_quitting, "exit asks before quitting");
            await GameplayCapture("confirm-exit-" + size.X);
            _exitConfirmation.GetCancelButton().EmitSignal(BaseButton.SignalName.Pressed);
            Require(!_exitConfirmation.Visible && !_quitting && _session == session && _menuPopup.QuitButton.HasFocus(), "Cancel keeps game running and restores exit focus");
        }
        Require(_session == session && session.Snapshot.Revision == revision && session.Snapshot.Paused == paused, "menu and canceled confirmations never issue commands or pause combat");
        _menuPopup.ReturnToMenuButton!.EmitSignal(BaseButton.SignalName.Pressed);
        _exitConfirmation.GetOkButton().EmitSignal(BaseButton.SignalName.Pressed);
        Require(_session is null && _sessionMenu.Visible && !_menuPopup.Visible && !_exitConfirmation.Visible && !_battlefield.Visible
            && !_gameUi.Details.Visible && !_gameUi.Draft.Visible, "confirmed return cleans up combat and presents main menu");
        try { session.Send("ready"); throw new InvalidOperationException("Disposed session accepted input"); }
        catch (ObjectDisposedException) { }

        BeginHost(0); OpenMenu();
        _menuPopup.ReturnToMenuButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_exitConfirmation.DialogText.Contains("everyone") && _exitConfirmation.DialogText.Contains("disconnect all players"), "host warning explains impact on other players");
        ReturnToMenu();
        string directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "common-watch-exit-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        try
        {
            _sessions.Open(guest => guest.Join("127.0.0.1", 1, directory)); RefreshUi(true); OpenMenu();
            _menuPopup.QuitButton.EmitSignal(BaseButton.SignalName.Pressed);
            Require(_exitConfirmation.DialogText.Contains("other players can continue") && _exitConfirmation.DialogText.Contains("same session"), "guest warning explains disconnect and conditional reconnect");
        }
        finally { ReturnToMenu(); System.IO.Directory.Delete(directory, recursive: true); }
        Require(!_exitConfirmation.Visible, "session replacement clears pending confirmations");
        _sessionMenu.QuitButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(_exitConfirmation.Visible && _exitConfirmation.DialogText.Contains("No match is active"), "main menu exit describes settings retention without claiming progress loss");
        _exitConfirmation.GetCancelButton().EmitSignal(BaseButton.SignalName.Pressed);
        GetWindow().Size = originalSize;
        GD.Print("GODOT_EXIT_MENU_PASS escape return exit cancel focus solo host guest cleanup sizes=1280/1920");
    }
}
