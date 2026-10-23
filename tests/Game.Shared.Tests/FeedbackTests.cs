// SPDX-License-Identifier: GPL-3.0-only
using System.Buffers.Binary;
using System.Security.Cryptography;
using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class FeedbackTests
{
    [Fact]
    public void TransactionsUseAcceptedIdentityAndNeverReplayRetriesOrBaselines()
    {
        using var solo = new SoloSession(); Assert.True(solo.Start().Accepted);
        var timeline = new FeedbackTimeline(); timeline.Baseline(solo.Snapshot, solo.PlayerId, solo.LastCommand!.Sequence);
        var accepted = solo.Send("build", 0, Building.Farm);
        string before = System.Text.Json.JsonSerializer.Serialize(solo.Snapshot, WireJson.Options);
        var cue = timeline.Result(solo.LastCommand!, accepted);
        Assert.Equal(FeedbackKind.Construction, cue!.Kind); Assert.Equal(0, cue.Slot); Assert.Equal(solo.PlayerId, cue.City);
        Assert.Null(timeline.Result(solo.LastCommand!, solo.RetryLast()!));
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(solo.Snapshot, WireJson.Options));
        var rejected = solo.Send("build", 0, Building.Farm);
        Assert.False(rejected.Accepted); Assert.Equal(FeedbackKind.Rejected, timeline.Result(solo.LastCommand!, rejected)!.Kind);
        Assert.Null(timeline.Result(solo.LastCommand!, rejected));
        timeline.Baseline(solo.Snapshot, solo.PlayerId, solo.LastCommand!.Sequence);
        Assert.Null(timeline.Result(solo.LastCommand!, solo.RetryLast()!));
        Assert.Empty(timeline.State(solo.Snapshot, false));
        var sold = solo.Send("sell", 0);
        Assert.True(sold.Accepted); Assert.Equal("Building sold", timeline.Result(solo.LastCommand!, sold)!.Text);

        using var army = ScenarioRunner.CreateAt("settlement");
        Assert.True(army.Send("ready").Accepted);
        timeline.Baseline(army.Snapshot, army.PlayerId, army.LastCommand!.Sequence);
        var recruited = army.Send("recruit", 1);
        Assert.True(recruited.Accepted); Assert.Equal(FeedbackKind.Recruitment, timeline.Result(army.LastCommand!, recruited)!.Kind);
        var unit = Assert.Single(army.City.Soldiers);
        var home = ArmyDeploymentView.Homes(army.Snapshot, army.PlayerId, unit.SquadId)[1];
        var moved = army.Send("deploy-squad", squadId: unit.SquadId, cell: home.BattlefieldCell);
        Assert.True(moved.Accepted); Assert.Equal("Army assignment updated", timeline.Result(army.LastCommand!, moved)!.Text);
        Assert.Equal(home.Home.Cell, army.City.Soldiers.Single().Assignment!.Tile);
        Assert.Null(timeline.Result(army.LastCommand!, army.RetryLast()!));
    }
    [Fact]
    public void ProductionAndOutcomesAreObservedOnceAndWaitForPresentedCombat()
    {
        using var solo = ScenarioRunner.CreateAt("settlement");
        var timeline = new FeedbackTimeline(); timeline.Baseline(solo.Snapshot, solo.PlayerId, solo.LastCommand!.Sequence);
        Assert.True(solo.Send("ready").Accepted);
        Assert.Equal(FeedbackKind.Production, Assert.Single(timeline.State(solo.Snapshot, false)).Kind);
        Assert.Empty(timeline.State(solo.Snapshot, false));
        using var battle = ScenarioRunner.CreateAt("battle-start");
        timeline.Baseline(battle.Snapshot, battle.PlayerId, battle.LastCommand!.Sequence);
        GameplayScenarios.FinishCombat(battle);
        Assert.Equal(Phase.RewardDraft, battle.Snapshot.Phase);
        Assert.Empty(timeline.State(battle.Snapshot, true));
        var cleared = Assert.Single(timeline.State(battle.Snapshot, false));
        Assert.Equal(FeedbackKind.WaveClear, cleared.Kind); Assert.StartsWith("Wave 1 held", cleared.Text);
        Assert.Empty(timeline.State(battle.Snapshot, false));
        timeline.BaselinePresentation(battle.Snapshot);
        Assert.Empty(timeline.State(battle.Snapshot, false));
    }
    [Theory]
    [InlineData("swordsman", FeedbackKind.Impact)]
    [InlineData("ranged", FeedbackKind.Ranged)]
    [InlineData("chill", FeedbackKind.Spell)]
    public void ActualPlaybackEventsHaveBoundedIdentityAndPausedEventsDoNotBurstOnResume(string scenario, FeedbackKind kind)
    {
        var battle = new BattleShowcase(scenario); var first = battle.Frames[0];
        var timeline = new FeedbackTimeline(); timeline.Baseline(first, first.Players[0].Id, 0);
        var events = battle.Frames.SelectMany(f => f.CombatEvents).DistinctBy(e => e.Sequence).Where(e => e.Sequence > first.EventSequence).OrderBy(e => e.Sequence).ToArray();
        Assert.NotEmpty(events);
        var cues = new List<FeedbackCue>();
        foreach (var entry in events)
        {
            if (timeline.Presented(entry, false) is { } cue) cues.Add(cue);
            Assert.Null(timeline.Presented(entry, false));
        }
        Assert.Contains(cues, c => c.Kind == kind);
        timeline.Baseline(first, first.Players[0].Id, 0);
        foreach (var entry in events) { Assert.Null(timeline.Presented(entry, true)); Assert.Null(timeline.Presented(entry, false)); }
        timeline.BaselinePresentation(battle.Frames[^1]);
        Assert.All(events, entry => Assert.Null(timeline.Presented(entry, false)));
    }
    [Fact]
    public void BossArrivalIsNewPresentedPresenceAndTerminalSnapshotsDoNotReplay()
    {
        using var solo = GameplayScenarios.CreateAt("boss-20");
        var timeline = new FeedbackTimeline(); timeline.Baseline(solo.Snapshot, solo.PlayerId, solo.LastCommand!.Sequence);
        Assert.True(solo.Send("ready").Accepted);
        _ = timeline.State(solo.Snapshot, true);
        for (int n = 0; n < 150 && !solo.Playback.Units().Any(u => u.IsBoss && u.Hex?.Lifecycle == UnitLifecycle.Alive); n++) solo.Step();
        Assert.Null(timeline.Boss(solo.Snapshot, solo.Playback.Units(), true));
        Assert.Equal(FeedbackKind.BossArrival, timeline.Boss(solo.Snapshot, solo.Playback.Units(), false)!.Kind);
        Assert.Null(timeline.Boss(solo.Snapshot, solo.Playback.Units(), false));
        GameplayScenarios.FinishCombat(solo);
        Assert.Equal(FeedbackKind.Victory, Assert.Single(timeline.State(solo.Snapshot, false)).Kind);
        timeline.Baseline(solo.Snapshot, solo.PlayerId, solo.LastCommand!.Sequence);
        Assert.Empty(timeline.State(solo.Snapshot, false));
    }
    [Fact]
    public void DenseSixSectorAudioIsLimitedWhileCriticalOutcomesCanInterrupt()
    {
        var budget = new FeedbackBudget(); int sfx = 0, ui = 0;
        var kinds = new[] { FeedbackKind.Impact, FeedbackKind.Ranged, FeedbackKind.Spell, FeedbackKind.Structure };
        for (int i = 0; i < 12000; i++)
        {
            double now = i / 6000d;
            if (budget.Allow(new(kinds[i % 4], ""), now)) sfx++;
            if (budget.Allow(new(FeedbackKind.Action, ""), now)) ui++;
        }
        Assert.InRange(sfx, 1, 24); Assert.InRange(ui, 1, 20);
        Assert.True(budget.Allow(new(FeedbackKind.Victory, "Victory"), 2));
        Assert.False(budget.Allow(new(FeedbackKind.Victory, "Victory"), 2));
        Assert.False(budget.Allow(new(FeedbackKind.Action, ""), double.NaN));
    }
    [Fact]
    public void TechnicalPaletteIsDeterministicDistinctFiniteAndHasQuietEdges()
    {
        var hashes = new HashSet<string>();
        foreach (var kind in Enum.GetValues<FeedbackKind>())
        {
            byte[] pcm = FeedbackTones.Pcm(kind);
            Assert.Equal(pcm, FeedbackTones.Pcm(kind)); Assert.Equal((int)(FeedbackTones.Duration(kind) * FeedbackTones.Rate) * 2, pcm.Length);
            var samples = Enumerable.Range(0, pcm.Length / 2).Select(i => (int)BinaryPrimitives.ReadInt16LittleEndian(pcm.AsSpan(i * 2, 2))).ToArray();
            Assert.Equal(0, samples[0]); Assert.InRange(Math.Abs(samples[^1]), 0, 4);
            Assert.InRange(samples.Max(v => Math.Abs(v)), 100, 9830);
            hashes.Add(Convert.ToHexString(SHA256.HashData(pcm)));
        }
        Assert.Equal(Enum.GetValues<FeedbackKind>().Length, hashes.Count);
    }
}
