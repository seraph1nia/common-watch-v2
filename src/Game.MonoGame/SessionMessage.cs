using System.Text.Json;
using Game.Core;

namespace Game.MonoGameClient;

// Versioned MonoGame messages. Connection IDs are resolved locally, never supplied as ownership claims.
public sealed record SessionMessage(string Kind, int Version = 1, string Application = MonoSession.Application,
    string Match = "", string Attempt = "", string Credential = "", int CoreVersion = WireJson.ProtocolVersion,
    int Player = 0, string Payload = "", bool Accepted = false, long Revision = 0);

public static class SessionMessageCodec
{
    private static void Validate(SessionMessage message)
    {
        if (message.Kind is null || message.Kind.Length > 32 || message.Application is null || message.Application.Length > 64
            || message.Match is null || message.Match.Length > 64 || message.Attempt is null || message.Attempt.Length > 32
            || message.Credential is null || message.Credential.Length > 128 || message.Payload is null)
            throw new InvalidDataException("Malformed session envelope.");
    }
    public static SessionMessage Decode(ReadOnlySpan<byte> data)
    {
        if (data.Length > MonoSession.MaximumMessageBytes) throw new InvalidDataException("Message exceeds transport bound.");
        var message = JsonSerializer.Deserialize<SessionMessage>(data, WireJson.Options) ?? throw new InvalidDataException("Missing message.");
        Validate(message); return message;
    }
    public static byte[] Encode(SessionMessage message)
    {
        Validate(message);
        byte[] data = JsonSerializer.SerializeToUtf8Bytes(message, WireJson.Options);
        if (data.Length > MonoSession.MaximumMessageBytes) throw new InvalidDataException("Message exceeds transport bound.");
        return data;
    }
}
