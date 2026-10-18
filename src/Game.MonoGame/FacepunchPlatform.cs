using Game.Core;
using Steamworks;
using Steamworks.Data;

namespace Game.MonoGameClient;

internal sealed class FacepunchPlatform : IMonoSteamPlatform
{
    private bool _initialized;
    public bool Ready => _initialized && SteamClient.IsValid && SteamClient.IsLoggedOn;
    public ulong Identity => Ready ? SteamClient.SteamId.Value : 0;
    public uint AppId { get; private set; }
    public string Name => Ready ? SteamClient.Name : "Steam offline";
    public event Action<ulong>? Invitation;
    public bool Initialize(out string feedback)
    {
        feedback = "Steam unavailable. Offline and LAN remain available.";
        if (Environment.GetEnvironmentVariable("COMMON_WATCH_STEAM_DISABLED") == "1") return false;
        if (_initialized) { feedback = Name; return Ready; }
        if (!uint.TryParse(Environment.GetEnvironmentVariable("COMMON_WATCH_STEAM_APP_ID"), out uint app) || app == 0)
        { feedback = "Set COMMON_WATCH_STEAM_APP_ID to your authorized AppID."; return false; }
        try
        {
            SteamClient.Init(app, asyncCallbacks: false); _initialized = true; AppId = app;
            SteamNetworkingUtils.InitRelayNetworkAccess(); SteamFriends.OnGameLobbyJoinRequested += OnInvitation;
            feedback = Name; return Ready;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or InvalidOperationException or TypeInitializationException)
        { if (SteamClient.IsValid) SteamClient.Shutdown(); return false; }
    }
    private void OnInvitation(Lobby lobby, SteamId friend) => Invitation?.Invoke(lobby.Id.Value);
    public void Poll() { if (_initialized) SteamClient.RunCallbacks(); }
    public async Task<IMonoSteamLobby?> Create()
    { var lobby = await SteamMatchmaking.CreateLobbyAsync(4).ConfigureAwait(false); return lobby is { } value ? new NativeLobby(value) : null; }
    public async Task<IMonoSteamLobby?> Join(ulong id)
    { var lobby = await SteamMatchmaking.JoinLobbyAsync(id).ConfigureAwait(false); return lobby is { } value ? new NativeLobby(value) : null; }
    internal bool InviteFriend(ulong lobby, ulong recipient) => Ready
        && SteamFriends.GetFriends().Any(friend => friend.Id.Value == recipient)
        && new Lobby(lobby).InviteFriend(recipient);
    public void Invite(ulong lobby) => SteamFriends.OpenGameInviteOverlay(lobby);
    public void Dispose()
    {
        if (!_initialized) return;
        SteamFriends.OnGameLobbyJoinRequested -= OnInvitation; SteamClient.Shutdown(); _initialized = false;
    }
    private sealed class NativeLobby(Lobby lobby) : IMonoSteamLobby
    {
        public ulong Id => lobby.Id.Value;
        public ulong Owner => lobby.Owner.Id.Value;
        public IReadOnlyDictionary<string, string> Metadata => SteamLobbyRules.MetadataKeys.Append("transport_version").ToDictionary(key => key, lobby.GetData);
        public void Publish(IReadOnlyDictionary<string, string> data)
        { foreach (var (key, value) in data) if (!lobby.SetData(key, value)) throw new IOException("Steam lobby metadata update failed."); }
        public bool Contains(ulong identity) => lobby.Members.Any(friend => friend.Id.Value == identity);
        public void MakePrivate() { if (!lobby.SetPrivate() || !lobby.SetJoinable(true)) throw new IOException("Steam lobby privacy setup failed."); }
        public void Leave() => lobby.Leave();
    }
}
