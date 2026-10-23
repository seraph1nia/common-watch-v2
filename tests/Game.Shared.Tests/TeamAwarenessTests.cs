// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class TeamAwarenessTests
{
    [Fact]
    public void ReadyPauseAndConnectionProjectAcceptedStateWithoutChangingLocalAuthority()
    {
        using var match = new Match(); int p1 = match.Join()!.Id, p2 = match.Join()!.Id; long sequence = 0;
        void Send(string action) => Assert.True(match.Apply(p1, Command.FromSnapshot(match.Snapshot(), ++sequence, action, p1)).Accepted);
        Send("start"); Send("ready");
        var state = match.Snapshot(); long revision = state.Revision;
        var team = TeamAwareness.Project(state, p2);
        Assert.Equal("Ready", team.Members[0].State); Assert.Equal("Preparing", team.Members[1].State);
        Assert.False(team.Observer); Assert.Equal(revision, match.Snapshot().Revision);
        Send("pause"); Assert.True(TeamAwareness.Project(match.Snapshot(), p2).Paused);
        match.SetConnected(p2, false);
        var away = TeamAwareness.Project(match.Snapshot(), p1).Members[1];
        Assert.Equal("Away", away.State); Assert.Contains("do not block readiness", away.Details);
        match.SetConnected(p2, true); Assert.Equal("Preparing", TeamAwareness.Project(match.Snapshot(), p1).Members[1].State);
        Assert.Equal("Away", TeamAwareness.Project(match.Snapshot(), p2, connected: false).Members[1].State);
    }
    [Fact]
    public void LegalUnevenReplayExercisesEndangeredFallenCrossSectorAndExcludedLaterArmy()
    {
        bool low = false, observer = false, crossed = false, choice = false;
        var run = CooperationReview.Run(2, "limited", 1, state =>
        {
            var team = TeamAwareness.Project(state, 2);
            low |= team.Members.Any(m => m.Endangered);
            observer |= team.Observer;
            choice |= team.Members.Any(m => m.State == "Choosing");
            foreach (var city in state.Players)
            foreach (var unit in city.Soldiers.Where(u => u.Health > 0 && u.Deployed))
            {
                int sector = TeamAwareness.Sector(state, unit);
                crossed |= sector > 0 && sector != unit.Owner;
            }
            if (team.Observer)
            {
                Assert.Contains("redistributed", team.Explanation);
                Assert.False(state.Players.Single(p => p.Id == team.SuggestedCity).Eliminated);
            }
        });
        Assert.True(low); Assert.True(observer); Assert.True(crossed);
        CooperationReview.Run(2, "frontline", 1, state => choice |= TeamAwareness.Project(state, 1).Members.Any(m => m.State == "Choosing"), maximumWaves: 1);
        Assert.True(choice);
        Assert.True(run.FallWave > 0); Assert.True(run.SpectatorTicks > 0);
        var continued = CooperationReview.Run(3, "limited", 1, state =>
        {
            if (state.Phase != Phase.Combat || state.Wave < 2) return;
            foreach (var fallen in state.Players.Where(p => p.Eliminated))
            {
                Assert.Equal(0, TeamAwareness.Project(state, 1).Members.Single(m => m.Id == fallen.Id).Field);
                Assert.DoesNotContain(fallen.Soldiers, u => u.Health > 0 && u.Hex?.Lifecycle == UnitLifecycle.Alive);
                foreach (var veteran in fallen.Soldiers.Where(u => u.Health > 0))
                {
                    var comparison = VeteranComparison.Project(state, fallen, veteran.Id);
                    Assert.Contains("Fallen city", comparison.Unavailable); Assert.Empty(comparison.Replacements);
                    Assert.All(comparison.Recovery, r => Assert.False(r.CanStore));
                }
            }
        });
        Assert.Equal(1, continued.FallWave);
        Assert.Equal(2, continued.Battles.Length);
        Assert.Equal(0, continued.Battles[1].Participating[1]); Assert.Equal(0, continued.Battles[1].Participating[2]);
    }
    [Fact]
    public void IndividualRewardChoiceStaysDistinctUntilAllStandingCitiesChoose()
    {
        using var match = new Match(); int[] ids = [match.Join()!.Id, match.Join()!.Id];
        var sequences = ids.ToDictionary(id => id, _ => 0L);
        void Send(int id, string action) => Assert.True(match.Apply(id, Command.FromSnapshot(match.Snapshot(), ++sequences[id], action, id)).Accepted);
        Send(1, "start");
        for (int stage = 0; stage < 3; stage++)
        foreach (int id in ids)
        {
            for (int n = 0; n < 120; n++)
            {
                var state = match.Snapshot(); var intent = CooperationReview.Next(state, id, "frontline");
                if (intent is null) break;
                Assert.True(match.Apply(id, intent.Capture(state, ++sequences[id], id)).Accepted);
            }
            Send(id, "ready");
        }
        for (int n = 0; n < 18000 && match.Phase == Phase.Combat; n++) match.Step();
        Assert.Equal(Phase.RewardDraft, match.Phase);
        Assert.All(TeamAwareness.Project(match.Snapshot(), 1).Members, m => Assert.Equal("Choosing", m.State));
        Send(1, "reward:" + GameplayScenarios.RewardChoice(match.Snapshot().Players[0].Run));
        var team = TeamAwareness.Project(match.Snapshot(), 1);
        Assert.Equal("Selected", team.Members[0].State); Assert.Equal("Choosing", team.Members[1].State);
        Send(2, "reward:" + GameplayScenarios.RewardChoice(match.Snapshot().Players[1].Run));
        Assert.All(TeamAwareness.Project(match.Snapshot(), 1).Members, m => Assert.Equal("Preparing", m.State));
    }
    [Fact]
    public void TerminalVeteranProfilesDoNotPromiseFuturePurchasesOrRecovery()
    {
        using var solo = GameplayScenarios.CreateAt("victory");
        var member = solo.City.Soldiers.First(u => u.Health > 0);
        var view = VeteranComparison.Project(solo.Snapshot, solo.City, member.Id);
        Assert.Empty(view.Replacements); Assert.Contains("Match ended", view.Unavailable);
        Assert.Contains("no future recovery", view.Details); Assert.DoesNotContain("per funded battle", view.Summary);
        Assert.All(view.Recovery, r => { Assert.False(r.CanStore); Assert.Equal(member.Health, r.HealthAfterProduction); });
        var team = TeamAwareness.Project(solo.Snapshot, solo.PlayerId);
        Assert.False(team.Observer); Assert.Contains("No further production", team.Explanation);
    }
}
