namespace Game.Core;

public sealed record SteamFriend(ulong Id, string Name, string Presence, bool Online);
public readonly record struct SteamInviteContext(ulong Lobby, long Generation, bool Hosting, bool LoggedIn);
public enum SteamInviteResult { Sent, Failed, Unavailable, Stale, NotFriend, Busy }

// UI selections belong to one opening of one original host's lobby.
public sealed class SteamInvitationPicker
{
    private SteamInviteContext _opened;
    private readonly HashSet<ulong> _friends = [];
    private long _ticket;
    private bool _open, _sending;

    public long Open(SteamInviteContext context)
    {
        Close(); _opened = context; _open = context is { Lobby: not 0, Hosting: true };
        return _ticket;
    }

    public bool IsCurrent(long ticket, SteamInviteContext context) => _open && ticket == _ticket
        && context.Hosting && context.Lobby == _opened.Lobby && context.Generation == _opened.Generation;

    public bool SetFriends(long ticket, IEnumerable<SteamFriend> friends)
    {
        if (!_open || ticket != _ticket || _sending) return false;
        _friends.Clear();
        foreach (SteamFriend friend in friends) if (friend.Id != 0) _friends.Add(friend.Id);
        return true;
    }

    public SteamInviteResult Send(long ticket, ulong recipient, SteamInviteContext context,
        Func<ulong, bool> isFriend, Func<ulong, ulong, bool> send)
    {
        ArgumentNullException.ThrowIfNull(isFriend); ArgumentNullException.ThrowIfNull(send);
        if (!IsCurrent(ticket, context)) return SteamInviteResult.Stale;
        if (!context.LoggedIn) return SteamInviteResult.Unavailable;
        if (_sending) return SteamInviteResult.Busy;
        _sending = true;
        try
        {
            if (!_friends.Contains(recipient) || !isFriend(recipient)) return SteamInviteResult.NotFriend;
            // A reentrant query may have closed/replaced this picker.
            if (!IsCurrent(ticket, context)) return SteamInviteResult.Stale;
            return send(context.Lobby, recipient) ? SteamInviteResult.Sent : SteamInviteResult.Failed;
        }
        finally { _sending = false; }
    }

    public void Close() { _open = false; _friends.Clear(); _ticket++; }
}
