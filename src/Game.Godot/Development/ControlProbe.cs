using System.Text.Json;
using Game.Core;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private long _controlWrite;
    private bool _pausedAtDeath;
    private void RunControlProbe()
    {
        if (Argument("--control-dir") is not { } directory) return;
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
                case "reconnect": BeginJoin(Argument("--join") ?? ""); break;
                case "quit": _ = QuitAfterAudioDrain(); break;
                case "retry": _session?.RetryLast(); break;
                case "send" when _session is { HasState: true }:
                    _session.Send(Text("action"), Number("slot"), Enum.Parse<Building>(Text("building", "Empty")),
                        payment: Enum.Parse<ConstructionPayment>(Text("payment", "Standard"))); break;
                case "probe" when _session is { HasState: true }:
                    _session.ProbeCommand(Text("action"), Number("owner", _session.PlayerId), Text("duplicate") == "yes"); break;
            }
        }
        if (!_pausedAtDeath && Argument("--pause-at-death") == "yes" && _session is { HasState: true, IsHost: true } && _session.Snapshot.DyingBodies.Length > 0)
        { _pausedAtDeath = true; _session.Send("pause"); }
        if (Argument("--test-fast-combat") == "yes" && _session is { HasState: true, IsHost: true } && _session.Snapshot.Phase == Phase.Combat)
            for (int i = 0; i < 10; i++) _session.Step();
        if (System.Environment.TickCount64 - _controlWrite < 80) return;
        _controlWrite = System.Environment.TickCount64;
        var status = new { connected = _session?.Connected ?? false, player = _session?.PlayerId ?? 0,
            pending = _session?.Pending ?? false, status = _session?.Status ?? "Main menu",
            animation = new { hits = _battlefield.Motion.ObservedHits, effects = _battlefield.Motion.Events.Count, generation = _session?.Playback.Generation, resets = _battlefield.Resets, dying = _session?.Playback.Units().Count(u => u.Hex?.Lifecycle == UnitLifecycle.Dying) },
            result = _session?.LastResult, state = _session is { HasState: true } ? _session.Snapshot : null };
        string output = System.IO.Path.Combine(directory, "status.json");
        System.IO.File.WriteAllText(output + ".tmp", JsonSerializer.Serialize(status, new JsonSerializerOptions(WireJson.Options) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never }));
        System.IO.File.Move(output + ".tmp", output, true);
    }
}
