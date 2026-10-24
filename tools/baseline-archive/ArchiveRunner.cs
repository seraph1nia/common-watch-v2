// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using System.Text.Json;

internal static class ArchiveRunner
{
    internal static int VerifyOnly(string output)
    {
        foreach (var folder in Directory.EnumerateDirectories(output).Where(folder => File.Exists(Path.Combine(folder, "history.ndjson"))).Order())
            Console.WriteLine("ARCHIVE_EVENT_REPLAY_PASS " + Path.GetFileName(folder) + " " + JsonSerializer.Serialize(ArchiveRecorder.Verify(folder), WireJson.Options));
        return 0;
    }
    internal static int Run(string output)
    {
        Directory.CreateDirectory(output); var runs = new List<CampaignRun>();
        foreach (ulong seed in new[] { 1UL, SoloSession.DefaultSeed })
        foreach (string family in new[] { "frontline", "ranged", "mixed", "towers", "research" })
        foreach (bool adaptive in new[] { false, true })
        {
            string folder = Path.Combine(output, $"{family}-{seed}-{adaptive}");
            runs.Add(ArchivedCampaignReview.Run(family, seed, adaptive, folder));
            Console.WriteLine($"ARCHIVE_REPLAY_PASS {family}/{seed}/{adaptive}: {JsonSerializer.Serialize(ArchiveRecorder.Verify(folder), WireJson.Options)}");
        }
        File.WriteAllText(Path.Combine(output, "campaign.json"), JsonSerializer.Serialize(runs, WireJson.Options));
        string idle = Path.Combine(output, "no-investment");
        using (var match = new Match(matchId: "original-no-investment", combatSeed: 1))
        {
            int owner = match.Join()!.Id; using var archive = new ArchiveRecorder(idle, match.Snapshot()); long sequence = 0;
            void Send(string action)
            {
                var command = Command.FromSnapshot(match.Snapshot(), ++sequence, action, owner); long tick = match.Tick;
                var result = match.Apply(owner, command); if (!result.Accepted) throw new InvalidOperationException(result.Message);
                archive.Command(tick, owner, command, result, match.Snapshot());
            }
            Send("start"); for (int stage = 0; stage < 3; stage++) Send("ready");
            archive.BeginEncounter(match.Snapshot());
            for (int tick = 0; tick < 18000 && (match.Phase == Phase.Combat || match.Snapshot().DyingBodies.Length > 0); tick++)
            { match.Step(); archive.Observe(match.Snapshot()); }
            if (match.Phase != Phase.Defeat) throw new InvalidOperationException("Original no-investment defense did not lose.");
            archive.EndEncounter(match.Snapshot()); archive.Complete(match.Snapshot());
        }
        Console.WriteLine("ARCHIVE_IDLE_REPLAY_PASS " + JsonSerializer.Serialize(ArchiveRecorder.Verify(idle), WireJson.Options));
        return 0;
    }
}
