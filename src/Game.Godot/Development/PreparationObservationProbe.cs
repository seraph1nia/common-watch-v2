using System.Text.Json;

namespace Game.GodotClient;

public partial class GameApplication
{
    private readonly PreparationObservations _preparationObservations = new();
    private long _observationWrite;
    private void RunPreparationObservations()
    {
        if (Argument("--preparation-observations") is not { } output) return;
        var session = _session;
        _preparationObservations.Observe(session, session is { HasState: true } ? session.Snapshot : null,
            session?.PlayerId ?? 0, session?.Connected ?? false, session?.ObservedCommand, session?.LastResult,
            Godot.Time.GetTicksMsec() / 1000.0);
        long now = System.Environment.TickCount64;
        if (now - _observationWrite < 1000) return;
        _observationWrite = now;
        string path = System.IO.Path.GetFullPath(output);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(new {
            source = "local observation; automated runs are not human pacing evidence",
            completed = _preparationObservations.Completed, current = _preparationObservations.Current }));
        System.IO.File.Move(path + ".tmp", path, true);
    }
}
