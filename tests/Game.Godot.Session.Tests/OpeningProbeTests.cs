using System.Text.Json;
using Game.Core;
using Game.Development;
using Game.Shared;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class OpeningProbeTests
{
    [Fact]
    public async Task HostWaitsForDelayedGuestDoctrineAcknowledgment()
    {
        using var exercise = new Exercise(5);
        for (int player = 0; player < 4; player++) exercise.Tick(player);
        exercise.Tick(0);
        // Reproduce a loaded CI guest taking longer than the former 2.5-second start timer.
        await Task.Delay(2700);
        exercise.Tick(0);
        Assert.Equal(Phase.Lobby, exercise.State.Phase);

        exercise.Tick(4);
        exercise.Tick(0);
        Assert.Equal(Phase.Building, exercise.State.Phase);
        exercise.Tick(4); // Consume the guest's accepted lobby result after the host starts.
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void AcknowledgedRosterStartsImmediatelyAndCompletesLegalOpening(int players)
    {
        using var exercise = new Exercise(players);
        for (int player = 0; player < players; player++) exercise.Tick(player);
        exercise.Tick(0);
        Assert.Equal(Phase.Building, exercise.State.Phase);

        for (int frame = 0; frame < 5000 && !exercise.Probes.All(p => p.Complete); frame++)
        {
            for (int player = 0; player < players; player++) exercise.Tick(player);
            exercise.Step();
        }
        Assert.All(exercise.Probes, probe =>
        {
            Assert.True(probe.Complete);
            Assert.True(probe.SawCombat);
            Assert.True(probe.SawDraft);
        });
        Assert.Equal(2, exercise.State.Wave);
        Assert.Equal(Phase.Building, exercise.State.Phase);
    }

    private sealed class Exercise : IDisposable
    {
        private readonly AuthoritySession _authority = new(AuthorityPolicy.PlayingHost, combatSeed: SoloSession.DefaultSeed);
        private readonly int[] _players;
        private readonly CommandResult?[] _results;
        private readonly long[] _sequences;
        private readonly HashSet<int> _chosen = [];
        private ulong _time;
        public OpeningProbe[] Probes { get; }
        public MatchSnapshot State => _authority.Snapshot();

        public Exercise(int players)
        {
            _players = new int[players];
            _players[0] = _authority.LocalPlayerId;
            for (int index = 1; index < players; index++)
            {
                var admission = _authority.Admit(index + 1, WireJson.ProtocolVersion, "", 0);
                Assert.True(admission.Accepted);
                _players[index] = admission.PlayerId;
            }
            Probes = Enumerable.Range(0, players).Select(_ => new OpeningProbe()).ToArray();
            _results = new CommandResult?[players];
            _sequences = new long[players];
        }

        public void Tick(int index)
        {
            Probes[index].Tick(State, _players[index], index == 0, _players.Length,
                _chosen.Count == _players.Length, false, _results[index], (action, slot, building, payment) =>
                {
                    var command = Command.FromSnapshot(State, ++_sequences[index], action, _players[index], slot, building, payment: payment);
                    var result = index == 0 ? _authority.ExecuteLocal(command)
                        : _authority.Request(index + 1, JsonSerializer.Serialize(command, WireJson.Options), _time += 20);
                    Assert.NotNull(result);
                    Assert.True(result.Accepted, result.Message);
                    _results[index] = result;
                    if (action.StartsWith("doctrine:", StringComparison.Ordinal)) _chosen.Add(_players[index]);
                });
        }

        public void Step() => _authority.Step();
        public void Dispose() => _authority.Dispose();
    }
}
