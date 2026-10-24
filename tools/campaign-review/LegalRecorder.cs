// SPDX-License-Identifier: GPL-3.0-only
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Game.Core;

namespace Game.Shared.Reviews;

// CLI-only instrumentation compiled beside the frozen legacy policy. It reads
// detached snapshots and records ordinary accepted commands; no state injection.
public sealed class LegalRecorder : IDisposable
{
    private readonly string _directory;
    private readonly StreamWriter _history;
    private StreamWriter? _events, _positions;
    private long _lastEvent, _startTick, _resolvedTick = -1;
    private int _wave;
    public static string Hash(MatchSnapshot state) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(state, WireJson.Options))));
    private static void Write(string path, object value) => File.WriteAllText(path, JsonSerializer.Serialize(value,
        new JsonSerializerOptions(WireJson.Options) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never }));
    public LegalRecorder(string directory, MatchSnapshot joined)
    {
        _directory = directory; Directory.CreateDirectory(directory);
        _history = new(Path.Combine(directory, "history.ndjson"));
        Write(Path.Combine(directory, "joined.json"), joined);
        Checkpoint("joined", joined);
    }
    public void Command(long atTick, int authenticatedPlayer, Command command, CommandResult result, MatchSnapshot after)
    {
        _history.WriteLine(JsonSerializer.Serialize(new { kind = "command", atTick, authenticatedPlayer, command, result, snapshotSha256 = Hash(after) }, WireJson.Options));
    }
    public void Checkpoint(string name, MatchSnapshot state)
    {
        _history.WriteLine(JsonSerializer.Serialize(new { kind = "checkpoint", atTick = state.Tick, name, snapshotSha256 = Hash(state) }, WireJson.Options));
    }
    public void BeginEncounter(MatchSnapshot before)
    {
        _events?.Dispose(); _positions?.Dispose(); _wave = before.Wave;
        string folder = Path.Combine(_directory, $"wave-{_wave:00}"); Directory.CreateDirectory(folder);
        _events = new(Path.Combine(folder, "events.ndjson"));
        _positions = new(Path.Combine(folder, "positions.ndjson"));
        _startTick = before.Tick; _resolvedTick = -1;
        Write(Path.Combine(folder, "before.json"), before);
        Checkpoint("before-encounter-" + _wave, before); Observe(before);
    }
    public void Observe(MatchSnapshot state)
    {
        if (_events is null || _positions is null) throw new InvalidOperationException("No encounter open.");
        if (_resolvedTick < 0 && state.Phase != Phase.Combat) _resolvedTick = state.Tick;
        var fresh = state.CombatEvents.Where(e => e.Sequence > _lastEvent).OrderBy(e => e.Sequence).ToArray();
        foreach (var e in fresh)
        {
            if (e.Sequence != _lastEvent + 1) throw new InvalidOperationException("Archive missed a bounded combat event.");
            _events.WriteLine(JsonSerializer.Serialize(e, WireJson.Options)); _lastEvent = e.Sequence;
        }
        if (state.Tick % 20 != 0 && fresh.Length == 0 && state.Phase == Phase.Combat) return;
        var units = state.Players.SelectMany(p => p.Soldiers).Concat(state.Enemies).Concat(state.DyingBodies)
            .Where(u => u.Deployed || u.Hex?.Lifecycle == UnitLifecycle.Dying).DistinctBy(u => u.Id)
            .Select(u => new { u.Id, u.Owner, u.Health, u.Type, u.SquadId, u.Hex, u.TargetKind, u.TargetId, u.Decision, u.Statuses, u.Capabilities });
        _positions.WriteLine(JsonSerializer.Serialize(new { state.Tick, state.Phase, units, state.Structures }, WireJson.Options));
    }
    public void EndEncounter(MatchSnapshot after)
    {
        if (_resolvedTick < 0 || after.DyingBodies.Length > 0) throw new InvalidOperationException("Encounter did not settle.");
        string folder = Path.Combine(_directory, $"wave-{_wave:00}");
        Write(Path.Combine(folder, "after.json"), after);
        Write(Path.Combine(folder, "timing.json"), new { startTick = _startTick, resolvedTick = _resolvedTick, cleanupEndTick = after.Tick,
            combatTicks = _resolvedTick - _startTick, corpseCleanupTicks = after.Tick - _resolvedTick,
            displayedSpeed = CombatSpeedRates.Label(after.CombatSpeed), rateFactor = CombatSpeedRates.Factor(after.CombatSpeed),
            nominalUnpausedCombatSeconds = (_resolvedTick - _startTick) / (double)Match.StepsPerSecond / CombatSpeedRates.Factor(after.CombatSpeed),
            nominalUnpausedCorpseCleanupSeconds = (after.Tick - _resolvedTick) / (double)Match.StepsPerSecond,
            actualPresentedWallSeconds = (double?)null, qualification = "Headless legal replay. Nominal rate conversion is not measured presentation or human timing." });
        Checkpoint("after-encounter-" + _wave, after); _events?.Flush(); _positions?.Flush();
    }
    public void Complete(MatchSnapshot state)
    { Write(Path.Combine(_directory, "final.json"), state); Checkpoint("final", state); _history.Flush(); }
    public void Dispose() { _history.Dispose(); _events?.Dispose(); _positions?.Dispose(); }

    private static StreamReader Reader(string file) => File.Exists(file) ? new StreamReader(file)
        : new StreamReader(new GZipStream(File.OpenRead(file + ".gz"), CompressionMode.Decompress));
    private static string ReadText(string file) { using var reader = Reader(file); return reader.ReadToEnd(); }
    private static IEnumerable<string> ReadLines(string file)
    {
        using var reader = Reader(file);
        while (reader.ReadLine() is { } line) yield return line;
    }

    public static object Verify(string directory)
    {
        var joined = JsonSerializer.Deserialize<MatchSnapshot>(ReadText(Path.Combine(directory, "joined.json")), WireJson.Options)!;
        using var replay = new Match(rules: joined.Rules, matchId: joined.MatchId, combatSeed: joined.CombatSeed);
        foreach (var expected in joined.Players) if (replay.Join()?.Id != expected.Id) throw new InvalidOperationException("Roster replay mismatch.");
        var expectedEvents = Directory.EnumerateDirectories(directory, "wave-*").SelectMany(folder =>
            ReadLines(Path.Combine(folder, "events.ndjson"))).Select(line => JsonSerializer.Deserialize<CombatEvent>(line, WireJson.Options)!)
            .ToDictionary(e => e.Sequence);
        long lastEvent = 0;
        void VerifyEvents(MatchSnapshot state)
        {
            foreach (var actual in state.CombatEvents.Where(e => e.Sequence > lastEvent).OrderBy(e => e.Sequence))
            {
                if (actual.Sequence != lastEvent + 1 || !expectedEvents.TryGetValue(actual.Sequence, out var expected)
                    || JsonSerializer.Serialize(actual, WireJson.Options) != JsonSerializer.Serialize(expected, WireJson.Options))
                    throw new InvalidOperationException("Combat event trace did not reproduce at sequence " + actual.Sequence);
                lastEvent = actual.Sequence;
            }
        }
        int commands = 0, checkpoints = 0;
        foreach (string line in ReadLines(Path.Combine(directory, "history.ndjson")))
        {
            using var row = JsonDocument.Parse(line); var value = row.RootElement;
            long at = value.GetProperty("atTick").GetInt64();
            for (int steps = 0; replay.Tick < at; steps++)
            {
                if (steps >= 18000 || replay.Phase != Phase.Combat && replay.Snapshot().DyingBodies.Length == 0)
                    throw new InvalidOperationException("Replay cannot legally advance to recorded tick.");
                replay.Step();
                VerifyEvents(replay.Snapshot());
            }
            if (replay.Tick != at) throw new InvalidOperationException("Replay passed a command boundary.");
            if (value.GetProperty("kind").GetString() == "command")
            {
                var command = value.GetProperty("command").Deserialize<Command>(WireJson.Options)!;
                var expected = value.GetProperty("result").Deserialize<CommandResult>(WireJson.Options)!;
                var before = replay.Snapshot();
                var result = replay.Apply(value.GetProperty("authenticatedPlayer").GetInt32(), command);
                EncounterIntegrity.RequireCommand(before, replay.Snapshot(), command);
                if (result != expected || !result.Accepted) throw new InvalidOperationException("Recorded command did not reproduce.");
                commands++;
            }
            else
            {
                checkpoints++;
                string name = value.GetProperty("name").GetString()!;
                if (name.StartsWith("after-encounter-", StringComparison.Ordinal))
                {
                    int wave = int.Parse(name["after-encounter-".Length..]);
                    var before = JsonSerializer.Deserialize<MatchSnapshot>(ReadText(Path.Combine(directory,$"wave-{wave:00}","before.json")),WireJson.Options)!;
                    EncounterIntegrity.RequireOutcome(before,replay.Snapshot());
                }
            }
            var accepted = replay.Snapshot(); VerifyEvents(accepted);
            if (Hash(accepted) != value.GetProperty("snapshotSha256").GetString())
                throw new InvalidOperationException($"Snapshot replay mismatch after {commands} commands at tick {at}.");
        }
        if (lastEvent != expectedEvents.Count) throw new InvalidOperationException("Event archive has unused records.");
        var receipt = new { commands, checkpoints, events = lastEvent, finalTick = replay.Tick, finalPhase = replay.Phase, finalSnapshotSha256 = Hash(replay.Snapshot()) };
        Write(Path.Combine(directory, "replay.json"), receipt); return receipt;
    }
}
