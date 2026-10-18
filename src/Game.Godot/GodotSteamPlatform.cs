using Godot;

namespace Game.GodotClient;

// Thin application-thread calls to the pinned GodotSteam GDExtension.
internal sealed class GodotSteamPlatform : IDisposable
{
    public bool Initialized { get; private set; }
    public GodotObject Api { get; private set; } = null!;
    public uint AppId { get; private set; }
    public bool LoggedIn => Initialized && Api.Call("isSteamRunning").AsBool() && Api.Call("loggedOn").AsBool();
    public ulong Identity => Api.Call("getSteamID").AsUInt64();
    public string Name => LoggedIn ? Api.Call("getPersonaName").AsString() : "Steam offline";
    public bool Initialize(out string status)
    {
        status = "Steam unavailable. Offline and LAN remain available.";
        if (System.Environment.GetEnvironmentVariable("COMMON_WATCH_STEAM_DISABLED") == "1") return false;
        if (Initialized) return LoggedIn;
        if (!uint.TryParse(System.Environment.GetEnvironmentVariable("COMMON_WATCH_STEAM_APP_ID"), out uint appId) || appId == 0)
        { status = "Set COMMON_WATCH_STEAM_APP_ID to your authorized AppID."; return false; }
        if (!Engine.HasSingleton("Steam") || !ClassDB.ClassExists("SteamMultiplayerPeer")) return false;
        Api = Engine.GetSingleton("Steam");
        if (!Api.Call("isSteamRunning").AsBool()) return false;
        var result = Api.Call("steamInitEx", appId, false).AsGodotDictionary();
        if (result["status"].AsInt64() != 0) return false;
        Initialized = true; AppId = appId; Api.Call("initRelayNetworkAccess");
        status = LoggedIn ? Name : "Log in to Steam first."; return LoggedIn;
    }
    public void Poll() { if (Initialized) Api.Call("run_callbacks"); }
    public MultiplayerPeer Peer(ulong host = 0)
    {
        var peer = ClassDB.Instantiate("SteamMultiplayerPeer").AsGodotObject() as MultiplayerPeer
            ?? throw new IOException("SteamMultiplayerPeer is incompatible with Godot C#.");
        Error error = (Error)(host == 0 ? peer.Call("create_host", 0) : peer.Call("create_client", host, 0)).AsInt64();
        if (error == Error.Ok) return peer;
        peer.Close(); peer.Dispose(); throw new IOException("Steam networking connection failed.");
    }
    public static string? PeerIdentity(MultiplayerPeer peer, int id)
    {
        ulong identity = peer.Call("get_steam_id_for_peer_id", id).AsUInt64();
        return identity == 0 ? null : identity.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    public bool Member(ulong lobby, ulong identity)
    {
        int count = (int)Api.Call("getNumLobbyMembers", lobby).AsInt64();
        for (int index = 0; index < count; index++)
            if (Api.Call("getLobbyMemberByIndex", lobby, index).AsUInt64() == identity) return true;
        return false;
    }
    public void Leave(ulong lobby) { if (Initialized && lobby != 0) Api.Call("leaveLobby", lobby); }
    public void Dispose() { if (Initialized) Api.Call("steamShutdown"); Initialized = false; }
}
