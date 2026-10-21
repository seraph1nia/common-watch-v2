using Godot;

namespace Game.GodotClient;

// Thin application-thread calls to the pinned GodotSteam GDExtension.
internal sealed class GodotSteamPlatform : IDisposable, ISteamInvitationPlatform
{
    public bool Initialized { get; private set; }
    public GodotObject Api { get; private set; } = null!;
    public const uint DevelopmentAppId = 480;
    public uint AppId => DevelopmentAppId;
    public bool LoggedIn => Initialized && Api.Call("isSteamRunning").AsBool() && Api.Call("loggedOn").AsBool();
    public ulong Identity => Api.Call("getSteamID").AsUInt64();
    public string Name => LoggedIn ? Api.Call("getPersonaName").AsString() : "Steam offline";
    public bool Initialize(out string status)
    {
        status = "Steam unavailable. Offline and LAN remain available.";
        if (System.Environment.GetEnvironmentVariable("COMMON_WATCH_STEAM_DISABLED") == "1") return false;
        if (Initialized) return LoggedIn;
        if (!Engine.HasSingleton("Steam") || !ClassDB.ClassExists("SteamMultiplayerPeer")) return false;
        Api = Engine.GetSingleton("Steam");
        if (!Api.Call("isSteamRunning").AsBool()) return false;
        var result = Api.Call("steamInitEx", DevelopmentAppId, false).AsGodotDictionary();
        if (result["status"].AsInt64() != 0) return false;
        Initialized = true; Api.Call("initRelayNetworkAccess");
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
    public IReadOnlyList<SteamFriend> Friends()
    {
        const int immediateFriends = 4;
        int count = (int)Api.Call("getFriendCount", immediateFriends).AsInt64();
        if (count < 0) throw new IOException("Steam friends unavailable.");
        var friends = new List<SteamFriend>(count);
        for (int index = 0; index < count; index++)
        {
            ulong identity = Api.Call("getFriendByIndex", index, immediateFriends).AsUInt64();
            string name = Api.Call("getFriendPersonaName", identity).AsString();
            friends.Add(new(identity, string.IsNullOrWhiteSpace(name) ? identity.ToString() : name));
        }
        return friends;
    }
    public bool IsFriend(ulong identity) => Api.Call("getFriendRelationship", identity).AsInt64() == 3;
    public bool Invite(ulong lobby, ulong identity) => Api.Call("inviteUserToLobby", lobby, identity).AsBool();
    public void Leave(ulong lobby) { if (Initialized && lobby != 0) Api.Call("leaveLobby", lobby); }
    public void Dispose() { if (Initialized) Api.Call("steamShutdown"); Initialized = false; }
}
