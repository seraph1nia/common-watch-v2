using System.Text.Json;
using Game.Core;


namespace Game.MonoGameClient;

public sealed partial class WatchGame
{
    private long _controlWrite;
    private bool _pausedAtDeath;
    private void RunControlProbe()
    {
        if (Arg("--control-dir") is not { } directory) return;
        System.IO.Directory.CreateDirectory(directory);
        foreach (string path in System.IO.Directory.GetFiles(directory, "request-*.json").Order(StringComparer.Ordinal))
        {
            using var request = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            var root = request.RootElement;
            string Text(string name, string fallback = "") => root.TryGetProperty(name, out var value) ? value.GetString() ?? fallback : fallback;
            int Number(string name, int fallback = -1) => root.TryGetProperty(name, out var value) ? value.GetInt32() : fallback;
            System.IO.File.Delete(path);
            switch (Text("kind"))
            {
                case "leave": ReturnToMenu(); break;
                case "reconnect": BeginJoin(Arg("--join") ?? ""); break;
                case "quit": Exit(); break;
                case "retry": session?.RetryLast(); break;
                case "send" when session is { HasState: true }:
                    session.Send(Text("action"), Number("slot"), Enum.Parse<Building>(Text("building", "Empty")),
                        payment: Enum.Parse<ConstructionPayment>(Text("payment", "Standard"))); break;
                case "probe" when session is { HasState: true }:
                    session.ProbeCommand(Text("action"), Number("owner", session.PlayerId), Text("duplicate") == "yes"); break;
            }
        }
        if (!_pausedAtDeath && Arg("--pause-at-death") == "yes" && session is { HasState: true, IsHost: true } && session.Snapshot.DyingBodies.Length > 0)
        { _pausedAtDeath = true; session.Send("pause"); }
        if (Arg("--test-fast-combat") == "yes" && session is { HasState: true, IsHost: true } && session.Snapshot.Phase == Phase.Combat)
            for (int i = 0; i < 10; i++) session.Step();
        if (Environment.TickCount64 - _controlWrite < 80) return;
        _controlWrite = Environment.TickCount64;
        var status = new { connected = session?.Connected ?? false, player = session?.PlayerId ?? 0,
            pending = session?.Pending ?? false, status = session?.Status ?? "Main menu",
            animation = new { hits = motion.ObservedHits, effects = motion.Events.Count, generation = session?.Playback.Generation, resets = visualResets, dying = session?.Playback.Units().Count(u => u.Hex?.Lifecycle == UnitLifecycle.Dying) },
            result = session?.LastResult, state = session is { HasState: true } ? session.Snapshot : null };
        string output = System.IO.Path.Combine(directory, "status.json");
        System.IO.File.WriteAllText(output + ".tmp", JsonSerializer.Serialize(status, new JsonSerializerOptions(WireJson.Options) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never }));
        System.IO.File.Move(output + ".tmp", output, true);
    }
}
