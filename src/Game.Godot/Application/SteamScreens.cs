using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private SteamCoordinator _steam = null!;
    private SteamPanel _steamPanel = null!;
    private SteamInvitationDialog _inviteConfirmation = null!;
    private SteamFriendsDialog _steamFriends = null!;
    private void CreateSteamMenu(VBoxContainer column)
    {
        _steam = new SteamCoordinator { Name = "Steam" }; AddChild(_steam);
        _steamPanel = new SteamPanel(); column.AddChild(_steamPanel);
        _steamFriends = new SteamFriendsDialog(); AddChild(_steamFriends);
        _steamFriends.Initialize(_skin, _steam.InviteFriends, _steam.InviteFriend);
        _steamPanel.Initialize(_skin, _steam.Host, text => { if (ulong.TryParse(text, out ulong lobby)) _steam.OfferJoin(lobby); }, _steamFriends.Open);
        _inviteConfirmation = new SteamInvitationDialog(); AddChild(_inviteConfirmation);
        _steam.Changed += SteamChanged;
        _steam.MenuRequested += ReturnToMenu;
        _steam.ConfirmationRequested += _inviteConfirmation.Ask;
        _steam.ConfirmationClosed += _inviteConfirmation.Clear;
        _steam.Initialize(_sessions, _args, SessionDataPath);
    }
    private void SteamChanged() => RefreshUi(true);
    private void RefreshSteamMenu()
    {
        var target = _menuPopup.Visible ? _menuPopup.MultiplayerContent : _session is { HasState: true } ? _sessionMenu.LobbyActions : _sessionMenu.MultiplayerContent;
        if (_steamPanel.GetParent() != target) _steamPanel.Reparent(target);
        _steamPanel.Refresh(_session is null || _menuPopup.Visible, _steam.Lobby != 0 && _session is { IsHost: true });
        _sessionMenu.MultiplayerStatus.Text = _steam.Feedback;
        _menuPopup.MultiplayerStatus.Text = _steam.Feedback;
        if (_steam.Feedback.Length > 0) _sessionMenu.Roster.Text += "\n" + _steam.Feedback;
    }
}
