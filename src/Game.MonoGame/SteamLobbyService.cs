namespace Game.MonoGameClient;

public interface IMonoSteamLobby
{
    ulong Id { get; }
    ulong Owner { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
    void Publish(IReadOnlyDictionary<string, string> metadata);
    bool Contains(ulong identity);
    void MakePrivate();
    void Leave();
}
public interface IMonoSteamPlatform : IDisposable
{
    bool Ready { get; }
    ulong Identity { get; }
    uint AppId { get; }
    string Name { get; }
    bool Initialize(out string feedback);
    void Poll();
    Task<IMonoSteamLobby?> Create();
    Task<IMonoSteamLobby?> Join(ulong lobby);
    event Action<ulong> Invitation;
    void Invite(ulong lobby);
}

// Poll consumes task results on the application thread. No rendering or authority is owned here.
public sealed class SteamLobbyService : IDisposable
{
    private readonly IMonoSteamPlatform _platform;
    private Task<IMonoSteamLobby?>? _pending;
    private long _generation, _operationGeneration, _started, _inviteGeneration;
    private bool _creating, _cancelled, _disposed;
    private ulong _target, _originalHost;
    public IMonoSteamLobby? Lobby { get; private set; }
    public bool Hosting { get; private set; }
    public ulong Invitation { get; private set; }
    public string Feedback { get; private set; } = "Steam optional";
    public event Action<bool, IMonoSteamLobby, ulong, string>? AdmittedLobby;
    public event Action? HostDeparted;
    public SteamLobbyService(IMonoSteamPlatform platform)
    { _platform = platform; platform.Invitation += Offer; }
    public bool Ensure()
    {
        try { bool ready = _platform.Initialize(out string feedback); Feedback = feedback; return ready; }
        catch (Exception) { Feedback = "Steam unavailable. Offline and LAN remain available."; return false; }
    }
    public bool BeginHost(long now) => Begin(0, now);
    public bool BeginJoin(ulong lobby, long now) => lobby != 0 && Begin(lobby, now);
    private bool Begin(ulong lobby, long now)
    {
        if (_disposed || _pending is not null || Lobby is not null || !Ensure()) return false;
        _creating = lobby == 0; _target = lobby; _started = now; _operationGeneration = _generation; _cancelled = false;
        try { _pending = _creating ? _platform.Create() : _platform.Join(lobby); Feedback = _creating ? "Creating private lobby…" : "Joining lobby…"; return true; }
        catch (Exception) { Feedback = "Steam request failed."; return false; }
    }
    private void Offer(ulong lobby)
    {
        if (_disposed || !_platform.Ready || lobby == 0 || lobby == Lobby?.Id || Invitation != 0 || _pending is not null) return;
        Invitation = lobby; _inviteGeneration = _generation;
    }
    public ulong AcceptInvitation()
    {
        ulong lobby = _inviteGeneration == _generation ? Invitation : 0;
        Invitation = 0; return lobby;
    }
    public void DeclineInvitation() => Invitation = 0;
    public void Poll(long now)
    {
        if (_disposed) return;
        _platform.Poll();
        if (_pending is not null && now - _started > 15000)
        { _cancelled = true; Feedback = "Steam request timed out. Its late result will be discarded."; }
        if (_pending is { IsCompleted: true } task)
        {
            _pending = null;
            IMonoSteamLobby? lobby = null;
            try
            {
                lobby = task.GetAwaiter().GetResult();
                if (lobby is null) { Feedback = "Steam lobby request failed."; return; }
                if (_cancelled || _generation != _operationGeneration || !_platform.Ready) { lobby.Leave(); return; }
                ulong host; string match;
                if (_creating)
                { lobby.MakePrivate(); host = _platform.Identity; match = Guid.NewGuid().ToString("N"); }
                else if (lobby.Id != _target || !SteamLobbyMetadata.Read(lobby.Metadata, _platform.AppId, lobby.Owner, out host, out match) || host == _platform.Identity)
                { lobby.Leave(); Feedback = "Incompatible Steam lobby or departed original host."; return; }
                Lobby = lobby; _originalHost = host; Hosting = _creating;
                Feedback = _platform.Name; AdmittedLobby?.Invoke(Hosting, lobby, host, match);
            }
            catch (Exception) { lobby?.Leave(); Lobby = null; Feedback = "Steam could not establish this session."; }
        }
        if (Lobby is not null && !Hosting && Lobby.Owner != _originalHost)
        { Leave(); Feedback = "The original host left. No host migration."; HostDeparted?.Invoke(); }
    }
    public void Publish(string match, bool started)
    { if (Hosting && Lobby is not null) Lobby.Publish(SteamLobbyMetadata.Create(_platform.AppId, _originalHost, match, started)); }
    public void Invite() { if (Hosting && Lobby is not null && _platform.Ready) _platform.Invite(Lobby.Id); }
    public void Leave()
    {
        _generation++; _cancelled = true; Invitation = 0;
        Lobby?.Leave(); Lobby = null; Hosting = false; _originalHost = 0;
    }
    public void Dispose()
    {
        if (_disposed) return;
        Leave(); _disposed = true; _platform.Invitation -= Offer; _platform.Dispose();
    }
}
