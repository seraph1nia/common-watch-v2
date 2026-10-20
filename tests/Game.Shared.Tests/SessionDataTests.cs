using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Game.Core;
using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public sealed class SessionDataTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void EverySupportedRosterStartsWithScaledForecastAndRoundTrips(int players)
    {
        using var host = new AuthoritySession(AuthorityPolicy.PlayingHost, combatSeed: SoloSession.DefaultSeed);
        for (int peer = 2; peer <= players; peer++)
            Assert.True(host.Admit(peer, WireJson.ProtocolVersion, "", 0).Accepted);
        var start = Command.FromSnapshot(host.Snapshot(), 1, "start", host.LocalPlayerId);
        Assert.True(host.ExecuteLocal(start).Accepted);
        MatchSnapshot state = host.Snapshot();
        Assert.Equal(players, state.Players.Length);
        Assert.Equal(players, state.Arena!.Sectors.Length);
        Assert.Equal(players * SharedArena.SectorWidth * SharedArena.Depth, state.Arena.Board.Cells.Length);
        Assert.Equal(players, state.NextWave.Select(u => u.Origin).Distinct().Count());
        var combat = new CombatConfiguration(state.Rules);
        Assert.Equal(players * WavePower.Total(state.WaveCatalog[0], combat),
            WavePower.Total(state.WaveCatalog[0], combat, players));
        MatchSnapshot decoded = SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(state));
        Assert.Equal(JsonSerializer.Serialize(state, WireJson.Options), JsonSerializer.Serialize(decoded, WireJson.Options));
        Assert.Throws<ArgumentOutOfRangeException>(() => WavePower.Total(state.WaveCatalog[0], combat, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WavePower.Total(state.WaveCatalog[0], combat, 7));
        Assert.Throws<ArgumentException>(() => new SharedArena(Enumerable.Range(1, 7)));
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(
            SnapshotPayload.Encode(state with { Players = Enumerable.Range(1, 7).Select(id => state.Players[0] with { Id = id }).ToArray() })));
    }

    [Fact]
    public void SnapshotCodecPreservesRealCombatAndRefusesHistoricalFlow()
    {
        using var session = ScenarioRunner.CreateAt("combat");
        string encoded = SnapshotPayload.Encode(session.Snapshot);
        var decoded = SnapshotPayload.DecodeForReceiving(encoded);
        Assert.Equal(JsonSerializer.Serialize(session.Snapshot, WireJson.Options), JsonSerializer.Serialize(decoded, WireJson.Options));
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(SnapshotPayload.Encode(session.Snapshot with { BattleFlowVersion = 0 })));
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving("bad-base64"));
    }
    [Fact]
    public void CompressedPayloadCannotDecompressBeyondItsBound()
    {
        using var compressed = new MemoryStream();
        using (var writer = new BrotliStream(compressed, CompressionLevel.Fastest, true)) writer.Write(new byte[16 * 1024 * 1024 + 1]);
        Assert.Throws<InvalidDataException>(() => SnapshotPayload.DecodeForReceiving(Convert.ToBase64String(compressed.ToArray())));
    }
    [Fact]
    public void ResumeIsPrivateAndSequenceReservationSurvivesReplacement()
    {
        string directory = Path.Combine(Path.GetTempPath(), "common-watch-private-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "resume.json");
        try
        {
            var first = new SessionFile(path, "localhost:1234", "enet", "v2-godot");
            first.Welcome("match", "private-credential");
            Assert.DoesNotContain("private-credential", first.Value!.ToString());
            Assert.Equal(1, first.Reserve());
            var second = new SessionFile(path, "localhost:1234", "enet", "v2-godot"); second.Load();
            Assert.Equal(2, second.Reserve());
            second.Welcome("match", "private-credential"); Assert.Equal(3, second.Reserve());
            if (!OperatingSystem.IsWindows()) Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
            foreach (var other in new[] { new SessionFile(path, "elsewhere", "enet", "v2-godot"), new SessionFile(path, "localhost:1234", "steam", "v2-godot"), new SessionFile(path, "localhost:1234", "enet", "unrelated-application") })
            { other.Load(); Assert.Null(other.Value); }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    [Fact]
    public void CorruptPrivateFileCannotSilentlyTurnResumeIntoFreshAdmission()
    {
        string directory = Path.Combine(Path.GetTempPath(), "common-watch-corrupt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory); string path = Path.Combine(directory, "resume.json");
        try
        {
            var file = new SessionFile(path, "endpoint");
            File.WriteAllText(path, JsonSerializer.Serialize(new LocalSession("endpoint", "match", "token", 0)));
            Assert.Throws<InvalidDataException>(() => file.Load());
            File.WriteAllText(path, "null"); Assert.Throws<InvalidDataException>(() => file.Load());
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void DisposedSoloCannotSendOrAdvanceAndDoesNotAffectReplacement()
    {
        using var old = ScenarioRunner.CreateAt("combat"); old.Dispose();
        using var next = new SoloSession(matchId: Guid.NewGuid().ToString("N"));
        Assert.Throws<ObjectDisposedException>(() => old.Send("ready"));
        Assert.Throws<ObjectDisposedException>(() => old.Advance(.1));
        Assert.Throws<ObjectDisposedException>(() => old.Step());
        Assert.True(next.Send("doctrine:merchant").Accepted);
        Assert.True(next.Start().Accepted);
        Assert.Equal("merchant", next.City.Run.Doctrine);
    }
}
