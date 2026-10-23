// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Game.GodotClient;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class ArmySelectionTests
{
    [Fact]
    public void EmptyDestinationAndSquadSurviveHarmlessRefreshAndReopening()
    {
        using var solo = ScenarioRunner.CreateAt("settlement");
        Assert.True(solo.Send("ready").Accepted); Assert.True(solo.Send("recruit", 1).Accepted);
        var selection = new ArmySelection(); selection.Observe(solo, solo.Snapshot, solo.PlayerId);
        Assert.Equal(9, selection.Home); Assert.Equal(solo.City.Soldiers[0].SquadId, selection.Squad);
        selection.SelectHome(10); selection.Observe(solo, solo.Snapshot, solo.PlayerId);
        Assert.Equal(10, selection.Home); Assert.Equal(solo.City.Soldiers[0].SquadId, selection.Squad);
        selection.SelectHome(2); Assert.Equal(10, selection.Home);
        selection.Observe(new object(), solo.Snapshot, solo.PlayerId);
        Assert.Equal(9, selection.Home); Assert.Equal(0, selection.Member);
    }

    [Fact]
    public void SelectedMemberFollowsAcceptedStorageAndReturnAndIgnoresRejection()
    {
        using var solo = GameplayScenarios.CreateAt("town-hall");
        var stored = solo.City.Soldiers.First(u => u.Assignment is { Stored: true });
        var selection = new ArmySelection(); selection.Observe(solo, solo.Snapshot, solo.PlayerId); selection.SelectMember(stored.Id);
        Assert.Equal(stored.Id, selection.Member); Assert.Equal(0, selection.Squad);
        Assert.True(solo.Send("send", stored.Assignment!.HallSlot, unitId: stored.Id).Accepted);
        selection.Observe(solo, solo.Snapshot, solo.PlayerId);
        var field = solo.City.Soldiers.Single(u => u.Id == stored.Id);
        Assert.Equal(field.SquadId, selection.Squad); Assert.Equal(field.Assignment!.Tile + 9, selection.Home);
        Assert.False(solo.Send("deploy", squadId: field.SquadId, cell: -100).Accepted);
        selection.Observe(solo, solo.Snapshot, solo.PlayerId);
        Assert.Equal(field.Assignment.Tile + 9, selection.Home); Assert.Equal(stored.Id, selection.Member);
        Assert.True(solo.Send("store", stored.Assignment!.HallSlot, unitId: stored.Id).Accepted);
        selection.Observe(solo, solo.Snapshot, solo.PlayerId);
        Assert.Equal(0, selection.Squad); Assert.Equal(stored.Id, selection.Member);
    }

    [Fact]
    public void SwitchingBetweenSixCitiesRetainsOnlyEachCitysHarmlessSelection()
    {
        using var match = new Match(); var identity = new object();
        int[] owners = Enumerable.Range(0, 6).Select(_ => match.Join()!.Id).ToArray();
        Assert.True(match.Apply(owners[0], Command.FromSnapshot(match.Snapshot(), 1, "start", owners[0])).Accepted);
        var selection = new ArmySelection();
        foreach (int owner in owners) { selection.Observe(identity, match.Snapshot(), owner); selection.SelectHome(8 + owner); }
        foreach (int owner in owners.Reverse())
        {
            selection.Observe(identity, match.Snapshot(), owner);
            Assert.Equal(8 + owner, selection.Home); Assert.Equal(0, selection.Member); Assert.Equal(0, selection.Squad);
        }
    }

    [Fact]
    public void NewMatchResetsEvenWhenSessionIdentityIsReused()
    {
        using var first = GameplayScenarios.CreateAt("army"); using var second = ScenarioRunner.CreateAt("initial");
        var selection = new ArmySelection(); var identity = new object();
        selection.Observe(identity, first.Snapshot, first.PlayerId); selection.SelectMember(first.City.Soldiers[0].Id);
        // Detached identity-only unit fixture, never a native campaign checkpoint.
        selection.Observe(identity, second.Snapshot with { MatchId = "replacement-match" }, second.PlayerId);
        Assert.Equal(0, selection.Member); Assert.Equal(0, selection.Squad); Assert.Equal(9, selection.Home);
    }
}
