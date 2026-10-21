using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckMenuPresentation()
    {
        ReturnToMenu(); RefreshUi(true);
        await GameplayCapture("main-menu");
        Require(_sessionMenu.MainButtons.GetChildren().OfType<Button>().Count(b => b.IsVisibleInTree()) == 4, "four visible primary buttons");
        Press(_sessionMenu.MultiplayerButton); await GameplayCapture("multiplayer-menu");
        Require(_steamPanel.IsVisibleInTree() && _sessionMenu.Endpoint.IsVisibleInTree(), "LAN and Steam entry points in multiplayer submenu");
        await CheckSteamFriendsPresentation();
        ReturnToMenu(); Press(_sessionMenu.SettingsButton); await GameplayCapture("menu-settings");
        Require(_settings.Visible, "menu settings route"); _settings.Close();
        BeginSolo();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await GameplayCapture("doctrine-offers");
        await CheckMenuPopup("doctrine");
        Require(_sessionMenu.StartButton.Disabled, "unselected match cannot start");
        foreach (DoctrineCard card in _sessionMenu.Doctrines.GetChildren())
            Require(GetViewport().GetVisibleRect().Encloses(card.GetGlobalRect()), "portrait doctrine card fits viewport");
        Require(GetViewport().GetVisibleRect().Encloses(_sessionMenu.StartButton.GetGlobalRect()), "Start Match fits viewport");
        Require(GetViewport().GetVisibleRect().Encloses(_sessionMenu.BackButton.GetGlobalRect()), "setup Back fits viewport");
        var selected = (DoctrineCard)_sessionMenu.Doctrines.GetChild(0);
        Click(selected.ChooseButton.GetGlobalRect().GetCenter());
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(selected.ChooseButton.ButtonPressed && !_sessionMenu.StartButton.Disabled, "native doctrine click highlights selection and enables start");
        await GameplayCapture("doctrine-selected");
        ReturnToMenu();
    }
    private async Task CheckSteamFriendsPresentation()
    {
        Control invoker = _sessionMenu.BackButton.IsVisibleInTree() ? _sessionMenu.BackButton : _sessionMenu.SettingsButton;
        invoker.GrabFocus();
        _steamFriends.Open(); await GameplayCapture("steam-invite-no-lobby");
        Require(_steamFriends.Size.X <= GetWindow().Size.X && _steamFriends.Size.Y <= GetWindow().Size.Y,
            $"complete Steam picker fits viewport: window={_steamFriends.Size}, list={_steamFriends.Friends.Size}, invite={_steamFriends.InviteButton.Size}");
        Require(_steamFriends.Visible && _steamFriends.Exclusive && _steamFriends.InviteButton.Disabled,
            "Steam picker opens visibly and captures input without a lobby");
        Require(_steamFriends.Status.Text.Contains("Host a private Steam lobby"), "Steam invite unavailable reason is visible");
        _steamFriends.Close();
        Require(invoker.HasFocus(), "Steam picker restores the invoking control focus");
        // UI-only test data exercises selection and submission; it is never a
        // simulation snapshot, native Steam operation, or gameplay capture.
        int submitted = 0;
        var fixture = new SteamFriendsDialog(); AddChild(fixture);
        fixture.Initialize(_skin, () => new(42, Enumerable.Range(7, 40).Select(id => new SteamFriend((ulong)id, "Test friend " + id)).ToArray(), "Choose a test friend."),
            (lobby, friend) => { Require(lobby == 42 && friend.Id == 7, "explicit exact invite target"); submitted++; return new(true, "Test submission."); });
        fixture.Open(); fixture.Friends.Select(0); fixture.Friends.EmitSignal(ItemList.SignalName.ItemSelected, 0);
        Require(submitted == 0 && !fixture.InviteButton.Disabled, "Steam friend selection does not send invitations");
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        fixture.Friends.GrabFocus(); fixture.Friends.GetVScrollBar().Value = 120;
        Press(fixture.RefreshButton);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        Require(fixture.Friends.GetSelectedItems().SequenceEqual(new[] { 0 }) && fixture.Friends.HasFocus()
            && fixture.Friends.GetVScrollBar().Value == 120 && submitted == 0, "Steam friend refresh retains selection, scrolling and keyboard focus without inviting");
        Require(fixture.Size.Y <= GetWindow().Size.Y, "populated Steam friend picker fits viewport");
        Press(fixture.InviteButton);
        Require(submitted == 1 && fixture.InviteButton.Disabled && fixture.Status.Text == "Test submission.", "explicit Steam invite submits once and reports status");
        fixture.Close(); RemoveChild(fixture); fixture.QueueFree();
    }
}
