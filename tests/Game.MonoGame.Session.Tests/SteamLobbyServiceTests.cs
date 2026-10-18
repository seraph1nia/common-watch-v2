using Game.MonoGameClient;
using Xunit;

namespace Game.MonoGame.Session.Tests;

public sealed class SteamLobbyServiceTests
{
    private sealed class Platform : IMonoSteamPlatform
    {
        public bool Ready { get; set; } = true;
        public ulong Identity => 42;
        public uint AppId => 123;
        public string Name => "Test account";
        public event Action<ulong>? Invitation;
        public TaskCompletionSource<IMonoSteamLobby?> Completion { get; private set; } = new();
        public int Invites { get; private set; }
        public bool Initialize(out string feedback) { feedback = Ready ? Name : "Steam unavailable"; return Ready; }
        public void Poll() { }
        public Task<IMonoSteamLobby?> Create() { Completion = new(); return Completion.Task; }
        public Task<IMonoSteamLobby?> Join(ulong lobby) { Completion = new(); return Completion.Task; }
        public void Invite(ulong lobby) => Invites++;
        public void Offer(ulong lobby) => Invitation?.Invoke(lobby);
        public void Dispose() { }
    }
    private sealed class Lobby : IMonoSteamLobby
    {
        public ulong Id => 99;
        public ulong Owner { get; set; } = 43;
        public IReadOnlyDictionary<string, string> Metadata { get; set; } = SteamLobbyMetadata.Create(123, 43, Guid.NewGuid().ToString("N"), false);
        public int Leaves { get; private set; }
        public bool Private { get; private set; }
        public void Publish(IReadOnlyDictionary<string, string> metadata) => Metadata = metadata;
        public bool Contains(ulong identity) => identity is 42 or 43;
        public void MakePrivate() => Private = true;
        public void Leave() => Leaves++;
    }
    [Fact]
    public void MissingSteamDoesNotCreateSessionAndCanRetry()
    {
        var platform = new Platform { Ready = false }; using var service = new SteamLobbyService(platform);
        Assert.False(service.BeginHost(0)); Assert.False(service.BeginJoin(99, 0));
        platform.Ready = true; Assert.True(service.BeginHost(1));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledAndTimedOutTasksCannotAttachLateLobby(bool timeout)
    {
        var platform = new Platform(); using var service = new SteamLobbyService(platform);
        Assert.True(service.BeginHost(0));
        if (timeout) service.Poll(15001); else service.Leave();
        Assert.False(service.BeginHost(15002));
        var lobby = new Lobby(); platform.Completion.SetResult(lobby); service.Poll(15003);
        Assert.Null(service.Lobby); Assert.Equal(1, lobby.Leaves); Assert.True(service.BeginHost(15004));
    }
    [Fact]
    public void CreatePublishesPrivateMetadataAndAllowsInvitations()
    {
        var platform = new Platform(); using var service = new SteamLobbyService(platform);
        int admissions = 0; service.AdmittedLobby += (hosting, _, host, match) => { Assert.True(hosting); Assert.Equal(42UL, host); admissions++; };
        Assert.True(service.BeginHost(0)); var lobby = new Lobby { Owner = 42 }; platform.Completion.SetResult(lobby); service.Poll(1);
        Assert.True(lobby.Private); Assert.Equal(1, admissions);
        service.Publish(Guid.NewGuid().ToString("N"), true); Assert.Equal("in-progress", lobby.Metadata["state"]);
        service.Invite(); Assert.Equal(1, platform.Invites);
        service.Leave(); Assert.Null(service.Lobby); Assert.Equal(1, lobby.Leaves);
    }
    [Theory]
    [InlineData("game", "wrong-engine")]
    [InlineData("application", "999")]
    [InlineData("protocol", "0")]
    [InlineData("state", "ended")]
    public void InvalidDiscoveryNeverCreatesGameplaySession(string key, string value)
    {
        var platform = new Platform(); using var service = new SteamLobbyService(platform);
        bool admitted = false; service.AdmittedLobby += (_, _, _, _) => admitted = true;
        Assert.True(service.BeginJoin(99, 0)); var lobby = new Lobby(); var data = lobby.Metadata.ToDictionary(); data[key] = value; lobby.Metadata = data;
        platform.Completion.SetResult(lobby); service.Poll(1);
        Assert.False(admitted); Assert.Equal(1, lobby.Leaves);
    }
    [Fact]
    public void OriginalHostDepartureEndsSessionWithoutMigration()
    {
        var platform = new Platform(); using var service = new SteamLobbyService(platform);
        Assert.True(service.BeginJoin(99, 0)); var lobby = new Lobby(); platform.Completion.SetResult(lobby); service.Poll(1);
        Assert.Same(lobby, service.Lobby); bool ended = false; service.HostDeparted += () => ended = true;
        lobby.Owner = 50; service.Poll(2); Assert.True(ended); Assert.Null(service.Lobby); Assert.Equal(1, lobby.Leaves);
    }
    [Fact]
    public void DuplicateInvitationsAndStaleConfirmationCannotReplaceSessions()
    {
        var platform = new Platform(); using var service = new SteamLobbyService(platform);
        platform.Offer(99); platform.Offer(100); Assert.Equal(99UL, service.Invitation);
        service.Leave(); Assert.Equal(0UL, service.AcceptInvitation());
        platform.Offer(100); Assert.Equal(100UL, service.AcceptInvitation()); Assert.Equal(0UL, service.AcceptInvitation());
    }
    [Fact]
    public void FaultedTaskDrainsAndRepeatedHostingRecovers()
    {
        var platform = new Platform(); using var service = new SteamLobbyService(platform);
        Assert.True(service.BeginHost(0)); Assert.False(service.BeginJoin(99, 0));
        platform.Completion.SetException(new IOException("SDK failed")); service.Poll(1);
        Assert.Null(service.Lobby); Assert.True(service.BeginJoin(99, 2));
    }
    [Fact]
    public void LoginLossBeforeCallbackRejectsLobby()
    {
        var platform = new Platform(); using var service = new SteamLobbyService(platform);
        service.BeginHost(0); platform.Ready = false; var lobby = new Lobby(); platform.Completion.SetResult(lobby); service.Poll(1);
        Assert.Null(service.Lobby); Assert.Equal(1, lobby.Leaves);
    }
}
