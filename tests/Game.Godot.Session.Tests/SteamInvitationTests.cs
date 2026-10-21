using Game.GodotClient;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class SteamInvitationTests
{
    private sealed class Platform : ISteamInvitationPlatform
    {
        public bool LoggedIn { get; set; } = true;
        public bool Friend { get; set; } = true;
        public bool Accepted { get; set; } = true;
        public bool Fail { get; set; }
        public List<(ulong Lobby, ulong Friend)> Invitations { get; } = [];
        public IReadOnlyList<SteamFriend> Friends() => Fail ? throw new IOException() : [new(9, "Zed"), new(7, "Amy")];
        public bool IsFriend(ulong identity) => Friend;
        public bool Invite(ulong lobby, ulong identity)
        {
            Invitations.Add((lobby, identity));
            return Fail ? throw new IOException() : Accepted;
        }
    }
    [Fact]
    public void OpeningAndReadingFriendsNeverSendsInvitations()
    {
        var platform = new Platform(); var invitations = new SteamInvitations(platform);
        var data = invitations.Load(42);
        Assert.Equal(42UL, data.Lobby);
        Assert.Equal(new ulong[] { 7, 9 }, data.Friends.Select(friend => friend.Id));
        Assert.Empty(platform.Invitations);
    }
    [Theory]
    [InlineData(0UL, 42UL)]
    [InlineData(42UL, 0UL)]
    [InlineData(42UL, 99UL)]
    public void ClosedOrReplacedLobbiesCannotReceiveStaleSelections(ulong requested, ulong current)
    {
        var platform = new Platform();
        var result = new SteamInvitations(platform).Send(requested, current, new(7, "Amy"));
        Assert.False(result.Submitted); Assert.Contains("closed or changed", result.Status);
        Assert.Empty(platform.Invitations);
    }
    [Fact]
    public void MissingLobbyAndOfflineFriendsProduceActionableFeedback()
    {
        var platform = new Platform { LoggedIn = false }; var invitations = new SteamInvitations(platform);
        Assert.Contains("Host a private Steam lobby", invitations.Load(0).Status);
        Assert.Contains("offline", invitations.Load(42).Status);
        Assert.False(invitations.Send(42, 42, new(7, "Amy")).Submitted);
        Assert.Empty(platform.Invitations);
    }
    [Theory]
    [InlineData(0UL, true)]
    [InlineData(7UL, false)]
    public void InvalidOrRemovedFriendsAreNotInvited(ulong identity, bool friend)
    {
        var platform = new Platform { Friend = friend };
        Assert.False(new SteamInvitations(platform).Send(42, 42, new(identity, "Amy")).Submitted);
        Assert.Empty(platform.Invitations);
    }
    [Fact]
    public void OnlyExplicitSendInvitesExactSelectedFriendAndLobby()
    {
        var platform = new Platform(); var invitations = new SteamInvitations(platform);
        invitations.Load(42);
        var result = invitations.Send(42, 42, new(7, "Amy"));
        Assert.True(result.Submitted);
        Assert.Contains("Delivery and acceptance are not confirmed", result.Status);
        Assert.Equal(new[] { (42UL, 7UL) }, platform.Invitations);
    }
    [Fact]
    public void SdkRefusalAndExceptionsRemainVisibleAndDoNotClaimSuccess()
    {
        var platform = new Platform { Accepted = false }; var invitations = new SteamInvitations(platform);
        Assert.Contains("refused", invitations.Send(42, 42, new(7, "Amy")).Status);
        platform.Fail = true;
        Assert.Contains("could not be loaded", invitations.Load(42).Status);
        var result = invitations.Send(42, 42, new(7, "Amy"));
        Assert.False(result.Submitted); Assert.Contains("could not submit", result.Status);
    }
}
