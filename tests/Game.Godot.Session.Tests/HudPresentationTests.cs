// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.GodotClient;
using Game.Shared;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class HudPresentationTests
{
    [Fact]
    public void ReadinessUsesLocalIdentityAndHidesPurchasesWithoutHidingThePlan()
    {
        using var match = new Match();
        int local = match.Join()!.Id, ally = match.Join()!.Id;
        Assert.True(match.Apply(local, Command.FromSnapshot(match.Snapshot(), 1, "start", local)).Accepted);
        Assert.True(match.Apply(local, Command.FromSnapshot(match.Snapshot(), 2, "ready", local)).Accepted);
        var waiting = HudPresentation.Project(match.Snapshot(), local, false, false);
        Assert.Equal(HudMoment.ReadyWaiting, waiting.Moment);
        Assert.True(waiting.Ledger); Assert.True(waiting.Navigation); Assert.False(waiting.Management);
        Assert.Equal(HudMoment.Planning, HudPresentation.Project(match.Snapshot(), ally, false, false).Moment);
    }

    [Theory]
    [InlineData("initial", HudMoment.Planning, true, false)]
    [InlineData("combat", HudMoment.Combat, false, true)]
    [InlineData("draft", HudMoment.ChoosingReward, false, false)]
    public void LegalCheckpointsHaveDifferentInformationBudgets(string checkpoint, HudMoment moment, bool ledger, bool arena)
    {
        using var solo = ScenarioRunner.CreateAt(checkpoint);
        var view = HudPresentation.Project(solo.Snapshot, solo.PlayerId, false, false);
        Assert.Equal(moment, view.Moment); Assert.Equal(ledger, view.Ledger); Assert.Equal(arena, view.Arena);
        if (moment != HudMoment.Planning) Assert.False(view.Management);
        if (moment == HudMoment.Combat) { Assert.False(view.PreparationTrack); Assert.False(view.Threat); }
    }

    [Fact]
    public void PlaybackFinishingKeepsCombatBudgetEvenAfterAuthorityOffersRewards()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        var view = HudPresentation.Project(solo.Snapshot, solo.PlayerId, true, false);
        Assert.Equal(HudMoment.Combat, view.Moment); Assert.True(view.LiveCombat);
        Assert.False(view.CanChooseReward); Assert.False(view.Ledger); Assert.False(view.PreparationTrack);
        Assert.Equal("Finishing combat", HudPresentation.PhaseLabel(solo.Snapshot, true));
        using var fighting = ScenarioRunner.CreateAt("combat");
        Assert.Equal("Combat", HudPresentation.PhaseLabel(fighting.Snapshot, true));
        Assert.True(solo.ChooseReward(GameplayScenarios.RewardChoice(solo.City.Run)).Accepted);
        Assert.Equal("Finishing combat", HudPresentation.PhaseLabel(solo.Snapshot, true));
        Assert.Equal(1, HudPresentation.DisplayedWave(solo.Snapshot, true));
    }

    [Fact]
    public void PreviewIsNavigationAndNeverChangesAuthorityPermissions()
    {
        using var solo = ScenarioRunner.CreateAt("initial");
        long revision = solo.Snapshot.Revision;
        var view = HudPresentation.Project(solo.Snapshot, solo.PlayerId, false, true);
        Assert.True(view.Arena); Assert.False(view.LiveCombat); Assert.False(view.Management);
        Assert.True(view.Ledger); Assert.Equal(revision, solo.Snapshot.Revision);
    }
    [Theory]
    [InlineData("victory")]
    [InlineData("defeat")]
    public void TerminalHistoryIsAnInspectionRatherThanAnInactiveManagementPalette(string checkpoint)
    {
        using var solo = GameplayScenarios.CreateAt(checkpoint);
        var view = HudPresentation.Project(solo.Snapshot, solo.PlayerId, false, false);
        Assert.Equal(HudMoment.Terminal, view.Moment);
        Assert.False(view.Navigation); Assert.False(view.Management); Assert.False(view.Ledger);
    }

}
