using System.Text.Json;

namespace Game.Shared;

// Private client data. Never include credentials in diagnostics or public snapshots.
// Optional namespace fields preserve existing ENet files for the same running match.
public sealed record LocalSession(string Endpoint, string MatchId, string Token, long NextSequence,
    string Transport = "enet", string Application = "common-watch-v2", string OriginalHostIdentity = "", string? LastCommand = null)
{
    public override string ToString() => $"LocalSession {{ Transport = {Transport}, Application = {Application}, MatchId = {MatchId} }}";
}
public sealed class SessionFile(string path, string endpoint, string transport = "enet", string application = "common-watch-v2", string originalHostIdentity = "")
{
    public LocalSession? Value { get; private set; }
    public string Transport => transport;
    public void Load()
    {
        Value = null;
        if (!File.Exists(path)) return;
        if (new FileInfo(path).Length > 8192) throw new InvalidDataException("Invalid local session file.");
        LocalSession? stored = JsonSerializer.Deserialize<LocalSession>(File.ReadAllText(path));
        if (stored is null) throw new InvalidDataException("Invalid local session file.");
        if (stored.Endpoint == endpoint && stored.Transport == transport && stored.Application == application
            && stored.OriginalHostIdentity == originalHostIdentity)
        {
            if (stored.NextSequence <= 0 || string.IsNullOrEmpty(stored.Token) || stored.Token.Length > 128 || string.IsNullOrEmpty(stored.MatchId))
                throw new InvalidDataException("Invalid local resume credential or sequence. Explicitly forget this session to join as a new player.");
            Value = stored;
        }
    }
    public void Welcome(string matchId, string token)
    {
        Value = new(endpoint, matchId, token, Value?.MatchId == matchId ? Value.NextSequence : 1, transport, application, originalHostIdentity, Value?.MatchId == matchId ? Value.LastCommand : null); Save();
    }
    public long Reserve()
    {
        LocalSession stored = Value ?? throw new InvalidOperationException("Session not initialized.");
        long sequence = stored.NextSequence;
        Value = stored with { NextSequence = checked(sequence + 1) }; Save(); return sequence;
    }
    public void Remember(string command)
    {
        if (command.Length > 2048) throw new ArgumentException("Command too large.", nameof(command));
        Value = (Value ?? throw new InvalidOperationException("Session not initialized.")) with { LastCommand = command };
        Save();
    }
    public void Clear() { Value = null; if (File.Exists(path)) File.Delete(path); }
    private void Save()
    {
        string full = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        string temp = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
            if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            using (var stream = new FileStream(temp, options))
            { JsonSerializer.Serialize(stream, Value); stream.Flush(flushToDisk: true); }
            File.Move(temp, full, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
