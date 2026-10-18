using Game.Core;
using Game.MonoGameClient;
using Xunit;

namespace Game.MonoGame.Session.Tests;

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
}
