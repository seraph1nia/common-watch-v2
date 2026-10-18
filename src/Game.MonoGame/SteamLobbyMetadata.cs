using Game.Core;

namespace Game.MonoGameClient;

public static class SteamLobbyMetadata
{
    public const string Game = "common-watch-v2-monogame";
    public static Dictionary<string, string> Create(uint app, ulong host, string match, bool started)
    {
        var data = SteamLobbyRules.Advertisement(app, host, match, started);
        data["game"] = Game; data["transport_version"] = "1"; return data;
    }
    public static bool Read(IReadOnlyDictionary<string, string> data, uint app, ulong owner, out ulong host, out string match)
    {
        host = 0; match = "";
        if (data.GetValueOrDefault("game") != Game || data.GetValueOrDefault("transport_version") != "1") return false;
        var copy = data.ToDictionary(); copy["game"] = SteamLobbyRules.GameIdentifier;
        return SteamLobbyRules.TryReadHost(copy, app, owner, out host, out match, out _);
    }
}
