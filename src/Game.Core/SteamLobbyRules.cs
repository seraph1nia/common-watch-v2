using System.Globalization;

namespace Game.Core;

// Public discovery data authorizes only a connection attempt, never a player/city.
public static class SteamLobbyRules
{
    public const string GameIdentifier = "odot-nine-tiles";
    public static readonly string[] MetadataKeys = ["game", "protocol", "application", "original_host", "match", "state"];

    public static Dictionary<string, string> Advertisement(uint application, ulong originalHost, string match, bool started) => new()
    {
        ["game"] = GameIdentifier,
        ["protocol"] = WireJson.ProtocolVersion.ToString(CultureInfo.InvariantCulture),
        ["application"] = application.ToString(CultureInfo.InvariantCulture),
        ["original_host"] = originalHost.ToString(CultureInfo.InvariantCulture),
        ["match"] = match,
        ["state"] = started ? "in-progress" : "lobby"
    };

    public static bool TryReadHost(IReadOnlyDictionary<string, string> metadata, uint application, ulong lobbyOwner,
        out ulong originalHost, out string match, out string feedback)
    {
        originalHost = 0; match = "";
        string Value(string key) => metadata.GetValueOrDefault(key, "");
        feedback = "This invitation is for a different or incompatible game.";
        if (Value("game") != GameIdentifier || Value("protocol") != WireJson.ProtocolVersion.ToString(CultureInfo.InvariantCulture)
            || Value("application") != application.ToString(CultureInfo.InvariantCulture)) return false;
        feedback = "The original host ended this game. Ask your friend for a new invitation.";
        if (!ulong.TryParse(Value("original_host"), NumberStyles.None, CultureInfo.InvariantCulture, out ulong host)
            || host == 0 || host != lobbyOwner || Value("state") is not ("lobby" or "in-progress")) return false;
        feedback = "The invitation has no valid running match.";
        if (!Guid.TryParseExact(Value("match"), "N", out _)) return false;
        originalHost = host; match = Value("match"); feedback = ""; return true;
    }

    public static ulong[] InvitationTargets(IEnumerable<string> arguments)
    {
        string[] args = arguments.ToArray();
        var targets = new HashSet<ulong>();
        for (int index = 0; index < args.Length - 1; index++)
            if (args[index] == "+connect_lobby" && ulong.TryParse(args[++index], NumberStyles.None, CultureInfo.InvariantCulture, out ulong target) && target != 0)
                targets.Add(target);
        return targets.ToArray();
    }
}
