namespace Game.Core;

public enum SteamOperationKind { None, Creating, Joining }
public enum SteamJoinCompletion { Ignore, Leave, Accept }

// Steam's create callback has no request identifier; join callbacks identify only the lobby.
// Keep abandoned requests until their callback drains, so a retry cannot adopt an old result.
public sealed class SteamOperationState
{
    private readonly HashSet<ulong> _outstandingJoins = [];
    private long _generation, _confirmationGeneration;
    private ulong _started;
    private bool _createOutstanding;

    public SteamOperationKind Kind { get; private set; }
    public ulong Target { get; private set; }
    public ulong Confirmation { get; private set; }
    public bool CreateOutstanding => _createOutstanding;
    public bool JoinOutstanding(ulong lobby) => _outstandingJoins.Contains(lobby);

    public bool CanOffer(ulong lobby, ulong activeLobby) => lobby != 0 && lobby != activeLobby
        && Kind == SteamOperationKind.None && Confirmation == 0 && !_createOutstanding && !_outstandingJoins.Contains(lobby);

    public bool Offer(ulong lobby, ulong activeLobby, long generation, bool platformReady)
    {
        if (!platformReady || !CanOffer(lobby, activeLobby)) return false;
        Confirmation = lobby; _confirmationGeneration = generation;
        return true;
    }

    public bool AcceptConfirmation(ulong lobby, long generation, long offeredGeneration)
    {
        if (Confirmation != lobby || lobby == 0 || ExpireConfirmation(generation)
            || offeredGeneration != _confirmationGeneration) return false;
        Confirmation = 0;
        return true;
    }

    public bool ExpireConfirmation(long generation)
    {
        if (Confirmation == 0 || generation == _confirmationGeneration) return false;
        Confirmation = 0;
        return true;
    }

    public void DeclineConfirmation(ulong lobby, long offeredGeneration)
    {
        if (Confirmation == lobby && offeredGeneration == _confirmationGeneration) Confirmation = 0;
    }

    public bool BeginCreate(long generation, ulong now, bool platformReady)
    {
        if (!platformReady || Kind != SteamOperationKind.None || Confirmation != 0 || _createOutstanding) return false;
        _createOutstanding = true;
        Begin(SteamOperationKind.Creating, generation, now);
        return true;
    }

    public bool BeginJoin(ulong lobby, long generation, ulong now, bool platformReady)
    {
        if (!platformReady || !CanOffer(lobby, 0)) return false;
        _outstandingJoins.Add(lobby); Target = lobby;
        Begin(SteamOperationKind.Joining, generation, now);
        return true;
    }

    private void Begin(SteamOperationKind kind, long generation, ulong now)
    {
        Kind = kind; _generation = generation; _started = now;
    }

    public bool CompleteCreate(long generation, bool acceptingScreen)
    {
        bool requested = _createOutstanding;
        _createOutstanding = false;
        if (!requested || Kind != SteamOperationKind.Creating || generation != _generation || !acceptingScreen) return false;
        Cancel();
        return true;
    }

    public SteamJoinCompletion CompleteJoin(ulong lobby, ulong activeLobby, long generation, bool acceptingScreen)
    {
        bool requested = _outstandingJoins.Remove(lobby);
        if (lobby == activeLobby && lobby != 0) return SteamJoinCompletion.Ignore;
        // Locally creating a lobby also emits LobbyEnter, before or after LobbyCreated.
        if (Kind == SteamOperationKind.Creating && !requested) return SteamJoinCompletion.Ignore;
        if (!requested || Kind != SteamOperationKind.Joining || lobby != Target || generation != _generation || !acceptingScreen)
            return SteamJoinCompletion.Leave;
        Cancel();
        return SteamJoinCompletion.Accept;
    }

    public bool CancelIfInvalid(long generation, bool acceptingScreen, ulong now, ulong timeout, out ulong abandonedLobby)
    {
        abandonedLobby = 0;
        if (Kind == SteamOperationKind.None || (generation == _generation && acceptingScreen && now - _started <= timeout)) return false;
        abandonedLobby = Cancel();
        return true;
    }

    public ulong Cancel()
    {
        ulong target = Target;
        Kind = SteamOperationKind.None; Target = Confirmation = 0;
        return target;
    }

    public void CreateFailed()
    {
        _createOutstanding = false;
        Cancel();
    }

    public void JoinFailed(ulong lobby)
    {
        _outstandingJoins.Remove(lobby);
        if (Kind == SteamOperationKind.Joining && Target == lobby) Cancel();
    }
}
