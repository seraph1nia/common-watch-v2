using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private SteamCoordinator _steam = null!;
    private SteamPanel _steamPanel = null!;
    private SteamInvitationDialog _inviteConfirmation = null!;
    private void CreateSteamMenu(VBoxContainer column)
    {
        _steam = new SteamCoordinator { Name = "Steam" }; AddChild(_steam);
        _steamPanel = new SteamPanel(); column.AddChild(_steamPanel);
        _steamPanel.Initialize(_skin, _steam.Host, text => { if (ulong.TryParse(text, out ulong lobby)) _steam.OfferJoin(lobby); }, _steam.InviteFriends);
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
        _steamPanel.Refresh(_session is null, _steam.Lobby != 0 && _session is { IsHost: true });
        if (_steam.Feedback.Length > 0) _sessionMenu.Roster.Text += "\n" + _steam.Feedback;
    }
}
