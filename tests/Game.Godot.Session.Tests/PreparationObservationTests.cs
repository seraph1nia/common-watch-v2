using Game.Core;
using Game.GodotClient;
using Game.Shared;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class PreparationObservationTests
{
    [Fact]
    public void LegalOpeningCountsDecisionsAndFinalReadyOnce()
    {
        using var solo = new SoloSession();
        var observations = new PreparationObservations();
        void Sample(double time) => observations.Observe(solo, solo.Snapshot, solo.PlayerId, true, solo.LastCommand, solo.LastResult, time);
        Sample(0); solo.Start(); Sample(1);
        solo.Send("build", 0, Building.Farm); Sample(2); Sample(3);
        solo.RetryLast(); Sample(4);
        solo.Send("ready"); Sample(6);
        var first = Assert.Single(observations.Completed);
        Assert.Equal(1, first.Decisions); Assert.Equal(1, first.ReadyRequests);
        Assert.Equal(5, first.Seconds); Assert.True(first.Complete); Assert.False(first.Partial);
        Assert.False(first.Empty); Assert.Equal(0, first.UnobservedCommands);
        solo.Send("ready"); Sample(7); solo.Send("ready"); Sample(8);
        Assert.Equal(3, observations.Completed.Length);
        Assert.Equal(3, solo.Snapshot.ProductionCount); Assert.Null(observations.Current);
    }
    [Fact]
    public void MissedCommandsAreVisibleAndRepeatedSessionsStayBounded()
    {
        using var solo = new SoloSession();
        var observations = new PreparationObservations();
        observations.Observe(solo, solo.Snapshot, solo.PlayerId, true, null, null, 0);
        Assert.True(solo.Start().Accepted);
        observations.Observe(solo, solo.Snapshot, solo.PlayerId, true, solo.LastCommand, solo.LastResult, 1);
        Assert.True(solo.Send("build", 0, Building.Farm).Accepted);
        Assert.True(solo.Send("build", 1, Building.Barracks).Accepted);
        observations.Observe(solo, solo.Snapshot, solo.PlayerId, true, solo.LastCommand, solo.LastResult, 2);
        Assert.Equal(1, observations.Current!.UnobservedCommands);
        for (int i = 0; i < 120; i++)
            observations.Observe(new object(), solo.Snapshot, solo.PlayerId, true, null, null, 3 + i);
        Assert.Equal(96, observations.Completed.Length);
        Assert.All(observations.Completed, stage => { Assert.True(stage.Partial); Assert.False(stage.Complete); });
    }
    [Fact]
    public void WaitingExcludesPauseAndDisconnectionAndReplacementClosesPartialStage()
    {
        using var authority = new AuthoritySession(AuthorityPolicy.PlayingHost);
        var admission = authority.Admit(2, WireJson.ProtocolVersion, "", 0);
        Assert.True(admission.Accepted); int guest = admission.PlayerId;
        long sequence = 0;
        Command? command = null; CommandResult? result = null;
        var observations = new PreparationObservations();
        void Sample(double time, bool connected = true) => observations.Observe(authority, authority.Snapshot(), authority.LocalPlayerId, connected, command, result, time);
        void Send(string action)
        {
            command = Command.FromSnapshot(authority.Snapshot(), ++sequence, action, authority.LocalPlayerId);
            result = authority.ExecuteLocal(command); Assert.True(result.Accepted, result.Message);
        }
        Sample(0); Send("start"); Sample(1); Send("ready"); Sample(2);
        Send("pause"); Sample(5); Sample(9, false); Send("resume"); Sample(11, false);
        Sample(14); Sample(16);
        observations.Observe(new object(), null, guest, true, null, null, 16);
        var stage = Assert.Single(observations.Completed);
        Assert.False(stage.Complete); Assert.Equal(5, stage.ReadyWaitingSeconds);
        Assert.Equal(6, stage.PausedSeconds); Assert.Equal(5, stage.AwaySeconds);
        Assert.True(stage.Empty);
    }
}
