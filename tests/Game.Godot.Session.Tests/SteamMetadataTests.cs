using Game.Core;
using Game.GodotClient;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class SteamMetadataTests
{
    [Fact]
    public void V2MetadataIsDistinctAndContainsOnlyPublicRoutingData()
    {
        var data = SteamLobbyMetadata.Create(123, 42, Guid.NewGuid().ToString("N"), false);
        Assert.NotEqual(SteamLobbyRules.GameIdentifier, data["game"]);
        Assert.True(SteamLobbyMetadata.Read(data, 123, 42, out var host, out var match));
        Assert.Equal(42UL, host); Assert.Equal(data["match"], match);
        Assert.Equal(SteamLobbyRules.MetadataKeys.Append("transport_version").Order(), data.Keys.Order());
        Assert.False(SteamLobbyMetadata.Read(data, 123, 99, out _, out _));
        Assert.False(SteamLobbyMetadata.Read(data, 456, 42, out _, out _));
        data["game"] = "other-engine";
        Assert.False(SteamLobbyMetadata.Read(data, 123, 42, out _, out _));
    }
    [Theory]
    [InlineData("protocol", "0")]
    [InlineData("transport_version", "0")]
    [InlineData("state", "ended")]
    [InlineData("match", "old-match")]
    public void StaleOrIncompatibleLobbiesCannotAuthorizeConnections(string key, string value)
    {
        var data = SteamLobbyMetadata.Create(123, 42, Guid.NewGuid().ToString("N"), false); data[key] = value;
        Assert.False(SteamLobbyMetadata.Read(data, 123, 42, out _, out _));
    }
    [Fact]
    public void SteamAdmissionUsesTransportIdentityInAdditionToPrivateCredential()
    {
        using var host = new AuthoritySession(AuthorityPolicy.PlayingHost, originalHostIdentity: "host", requireTrustedIdentity: true);
        Assert.False(host.Admit(2, WireJson.ProtocolVersion, "", 0).Accepted);
        var joined = host.Admit(2, WireJson.ProtocolVersion, "", 0, "guest"); Assert.True(joined.Accepted);
        host.Disconnect(2);
        Assert.False(host.Admit(3, WireJson.ProtocolVersion, joined.Credential, 0, "impostor").Accepted);
        Assert.True(host.Admit(3, WireJson.ProtocolVersion, joined.Credential, 1000, "guest").Accepted);
    }

    [Fact]
    public void FullSteamRosterRejectsSeventhIdentityButAllowsSixthToResume()
    {
        using var host = new AuthoritySession(AuthorityPolicy.PlayingHost, originalHostIdentity: "host", requireTrustedIdentity: true);
        AdmissionResult sixth = null!;
        for (int peer = 2; peer <= 6; peer++)
        {
            sixth = host.Admit(peer, WireJson.ProtocolVersion, "", 0, $"guest-{peer}");
            Assert.True(sixth.Accepted);
        }
        Assert.False(host.Admit(7, WireJson.ProtocolVersion, "", 0, "guest-7").Accepted);
        host.Disconnect(6);
        Assert.False(host.Admit(8, WireJson.ProtocolVersion, "", 0, "guest-8").Accepted);
        Assert.False(host.Admit(9, WireJson.ProtocolVersion, sixth.Credential, 0, "impostor").Accepted);
        var resumed = host.Admit(10, WireJson.ProtocolVersion, sixth.Credential, 0, "guest-6");
        Assert.True(resumed.Accepted);
        Assert.Equal(sixth.PlayerId, resumed.PlayerId);
        Assert.Equal(6, resumed.State!.Players.Length);
    }
}
