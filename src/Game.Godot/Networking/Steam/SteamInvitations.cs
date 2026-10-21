namespace Game.GodotClient;

public sealed record SteamFriend(ulong Id, string Name);
public sealed record SteamFriendList(ulong Lobby, IReadOnlyList<SteamFriend> Friends, string Status);
public sealed record SteamInviteResult(bool Submitted, string Status);

internal interface ISteamInvitationPlatform
{
    bool LoggedIn { get; }
    IReadOnlyList<SteamFriend> Friends();
    bool IsFriend(ulong identity);
    bool Invite(ulong lobby, ulong identity);
}

// A picker is bound to the lobby it opened for. Never redirect a stale selection
// into a replacement session, and never claim SDK submission proves delivery.
internal sealed class SteamInvitations(ISteamInvitationPlatform platform)
{
    public SteamFriendList Load(ulong lobby)
    {
        if (lobby == 0) return new(0, [], "Host a private Steam lobby before inviting friends.");
        try
        {
            if (!platform.LoggedIn) return new(lobby, [], "Steam is offline. Log in, then refresh your friends.");
            var friends = platform.Friends().Where(friend => friend.Id != 0).DistinctBy(friend => friend.Id)
                .OrderBy(friend => friend.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
            return new(lobby, friends, friends.Length == 0
                ? "No Steam friends are available. Refresh, or share the lobby ID below."
                : "Choose a friend, then Invite selected. The Steam overlay is not required.");
        }
        catch (Exception) { return new(lobby, [], "Steam friends could not be loaded. Refresh to retry, or share the lobby ID."); }
    }

    public SteamInviteResult Send(ulong requestedLobby, ulong currentLobby, SteamFriend friend)
    {
        if (requestedLobby == 0 || requestedLobby != currentLobby)
            return new(false, "This Steam lobby has closed or changed. Reopen Invite Steam friends.");
        try
        {
            if (!platform.LoggedIn) return new(false, "Steam is offline. Log in, then try again.");
            if (friend.Id == 0 || !platform.IsFriend(friend.Id))
                return new(false, "This player is no longer a Steam friend. Refresh your friends.");
            return platform.Invite(currentLobby, friend.Id)
                ? new(true, $"Invitation submitted to Steam for {friend.Name}. Delivery and acceptance are not confirmed.")
                : new(false, "Steam refused the invitation. Try again, or share the lobby ID.");
        }
        catch (Exception) { return new(false, "Steam could not submit the invitation. Try again, or share the lobby ID."); }
    }
}
