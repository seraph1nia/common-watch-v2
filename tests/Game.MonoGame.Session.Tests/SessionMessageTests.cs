using System.Text;
using Game.MonoGameClient;
using Xunit;

namespace Game.MonoGame.Session.Tests;

public sealed class SessionMessageTests
{
    [Theory]
    [InlineData("Credential")]
    [InlineData("Payload")]
    [InlineData("Attempt")]
    [InlineData("Match")]
    [InlineData("Kind")]
    [InlineData("Application")]
    public void NullFieldsAreRefusedBeforeTheyReachAuthority(string property)
    {
        byte[] json = Encoding.UTF8.GetBytes("{\"Kind\":\"admit\",\"" + property + "\":null}");
        Assert.Throws<InvalidDataException>(() => SessionMessageCodec.Decode(json));
    }
    [Fact]
    public void EncodingPreservesVersionAndEnforcesItsBounds()
    {
        var message = new SessionMessage("command", Payload: "{}");
        Assert.Equal(message, SessionMessageCodec.Decode(SessionMessageCodec.Encode(message)));
        Assert.Throws<InvalidDataException>(() => SessionMessageCodec.Encode(message with { Payload = new string('x', MonoSession.MaximumMessageBytes) }));
        Assert.Throws<InvalidDataException>(() => SessionMessageCodec.Decode(new byte[MonoSession.MaximumMessageBytes + 1]));
    }
}
