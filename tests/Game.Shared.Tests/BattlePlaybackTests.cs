using Game.Core;
using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public class BattlePlaybackTests
{
    [Fact]
    public void FutureHealthDoesNotLeadImpactAndRepeatedSnapshotsDoNotRepeatHits()
    {
        var battle = new BattleShowcase("swordsman");
        MatchSnapshot hitFrame = battle.Frames.First(f => f.CombatEvents.Any(e => e.Type == CombatEventType.Hit && e.Damage > 0));
        MatchSnapshot prior = battle.Frames.Last(f => f.Tick < hitFrame.Tick);
        var playback = new CombatPlayback(); playback.Accept(prior, true);
        int victim = hitFrame.CombatEvents.First(e => e.Type == CombatEventType.Hit).Unit!.Id;
        int before = playback.Units().Single(u => u.Id == victim).Health;
        playback.Accept(hitFrame);
        Assert.Equal(before, playback.Units().Single(u => u.Id == victim).Health);
        Assert.Empty(playback.Drain());
        playback.Advance(1.0 / 30, true);
        CombatEvent[] impacts = playback.Drain();
        Assert.Equal(hitFrame.CombatEvents.Length, impacts.Length);
        Assert.True(playback.Units().Single(u => u.Id == victim).Health < before);
        Assert.False(playback.Accept(hitFrame)); Assert.Empty(playback.Drain());
        Assert.False(playback.Accept(prior)); Assert.Empty(playback.Drain());
    }
    [Fact]
    public void HistoryGapAndSessionReplacementBaselineWithoutReplayingOldDamage()
    {
        var battle = new BattleShowcase("swordsman"); var playback = new CombatPlayback();
        playback.Accept(battle.Frames[0]); int generation = playback.Generation;
        MatchSnapshot late = battle.Frames[180];
        Assert.True(playback.Accept(late with { OldestEventSequence = late.EventSequence }));
        Assert.Equal(generation + 1, playback.Generation); Assert.Empty(playback.Drain());
        Assert.Equal(late.Tick, playback.Tick);
        Assert.True(playback.Accept(late with { MatchId = "new-session" }));
        Assert.Equal(generation + 2, playback.Generation); Assert.Empty(playback.Drain());
    }
    [Fact]
    public void ReconnectDuringDeathSamplesRemainingLifecycleAndDoesNotRestoreOccupancy()
    {
        var battle = new BattleShowcase("swordsman");
        CombatEvent death = battle.Frames.SelectMany(f => f.CombatEvents).First(e => e.Type == CombatEventType.Death);
        MatchSnapshot frame = battle.Frames.First(f => f.Tick == death.Tick + 2);
        var playback = new CombatPlayback(); playback.Accept(frame, true);
        UnitState body = playback.Units().Single(u => u.Id == death.Unit!.Id);
        Assert.False(body.Hex!.IsTargetable);
        Assert.InRange(CombatPlayback.DeathPose(body, playback.Tick, 1), .001, .99);
        Assert.Empty(playback.Drain());
        var completed = battle.Frames.First(f => f.Tick >= body.Hex.DeathEndTick);
        playback.Accept(completed, true);
        Assert.DoesNotContain(playback.Units(), u => u.Id == body.Id);
    }
    [Fact]
    public void AllAdvancedCapturesComeFromRealCoreEventsAndStatusApplications()
    {
        foreach (string scenario in new[] { "ranged", "status", "burn", "chill", "structures" })
        {
            var battle = new BattleShowcase(scenario);
            Assert.NotEmpty(BattleCaptures.Plan(battle, false));
            if (scenario == "structures")
                Assert.Contains(battle.Frames.SelectMany(f => f.CombatEvents), e => e.Type == CombatEventType.StructureDestroyed);
            else if (scenario != "ranged")
                Assert.Contains(battle.Frames.SelectMany(f => f.Enemies), u => scenario == "status" ? u.Statuses.Poison.Length > 0 : scenario == "burn" ? u.Statuses.Burn is not null : u.Statuses.Chill is not null);
        }
    }
}
