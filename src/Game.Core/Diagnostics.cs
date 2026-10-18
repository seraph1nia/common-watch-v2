using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Core;

public sealed record GameEvent(string Type, int PeerId = 0, MatchSnapshot? State = null, string? Message = null, int PlayerId = 0, CommandResult? Result = null);

public static class WireJson
{
    // Both independent predecessor branches used14 for incompatible schemas.
    // Integrated shared squads + three-preparation forecasts require a fresh peer.
    public const int ProtocolVersion = 16;
    public const string EventPrefix = "ODOT_EVENT ";
    public static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
}
