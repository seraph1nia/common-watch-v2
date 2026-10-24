using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class OpeningGuidanceTests
{
    private static void Accept(CommandResult result) => Assert.True(result.Accepted, result.Message);
    [Theory]
    [InlineData("warden")]
    [InlineData("merchant")]
    [InlineData("warmaster")]
    [InlineData("arcanist")]
    [InlineData("grovekeeper")]
    [InlineData("huntsman")]
    public void EveryDoctrineHasATestedLegalThreeWavePathWithoutNewEconomyRules(string doctrine)
    {
        foreach (ulong seed in new[] { SoloSession.DefaultSeed, 1UL })
        {
            var run = OpeningCampaign.Review(doctrine, seed);
            Assert.Equal(3, run.Waves.Length);
            Assert.All(run.Waves, w => { Assert.Equal("RewardDraft", w.Phase); Assert.True(w.Participants.Values.Sum() > 0); Assert.Equal(0, w.Unfunded); });
            Assert.Equal(RunPresentation.Starting(new Rules(), RunCatalog.Doctrine(doctrine)), run.Starting);
            var introduction = DoctrineIntroduction.For(doctrine);
            Assert.NotEmpty(introduction.Playstyle); Assert.NotEmpty(introduction.Immediate);
            if (doctrine == "merchant") Assert.Equal(4, run.Starting.Wood);
            Assert.DoesNotContain(run.Actions, a => a.Contains("GoldRecovery"));
            if (doctrine == "arcanist") Assert.Equal(3, run.Waves[0].Participants["Swordsman"]);
            if (doctrine == "huntsman") Assert.True(run.Waves[2].Participants["Crossbowman"] > 0);
        }
    }
    [Fact]
    public void OptionalObjectivesExplainRecoveryReadyParticipationAndActualOutcomeWithoutMutations()
    {
        using var solo = ScenarioRunner.CreateAt("initial");
        Assert.Contains("one production", OpeningGuidance.NextReady(solo.Snapshot));
        Accept(solo.Send("build", 0, Building.Farm)); Accept(solo.Send("build", 1, Building.MetalMine)); Accept(solo.Send("build", 2, Building.Barracks));
        long revision = solo.Snapshot.Revision;
        var recovery = Assert.IsType<OpeningHint>(OpeningGuidance.Project(solo.Snapshot, solo.City));
        Assert.Contains("Gold recovery", recovery.Message); Assert.Contains("4 Gold", recovery.Message);
        Assert.Equal(revision, solo.Snapshot.Revision);
        Accept(solo.Send("build", 3, Building.Lumbermill, payment: ConstructionPayment.GoldRecovery));
        Assert.Equal(OpeningDestination.Recruit, OpeningGuidance.Project(solo.Snapshot, solo.City)!.Destination);
        Accept(solo.Send("ready")); Accept(solo.Send("recruit", 2)); Accept(solo.Send("recruit", 2));
        Accept(solo.Send("ready")); Accept(solo.Send("recruit", 2)); Accept(solo.Send("recruit", 2));
        Assert.Equal(OpeningDestination.Homes, OpeningGuidance.Project(solo.Snapshot, solo.City)!.Destination);
        Assert.Contains("Commit when ready", OpeningGuidance.Project(solo.Snapshot, solo.City)!.Objective);
        Assert.Contains("Food", OpeningGuidance.NextReady(solo.Snapshot));
        var expected = solo.City.FoodForecast!.Participating;
        var outcomes = new BattleOutcomeJournal(); outcomes.Observe(solo.Snapshot);
        Accept(solo.Send("ready")); outcomes.Observe(solo.Snapshot);
        Assert.Equal(expected, solo.City.LastUpkeep!.Participating);
        GameplayScenarios.FinishCombat(solo); outcomes.Observe(solo.Snapshot);
        Assert.Null(OpeningGuidance.Project(solo.Snapshot, solo.City));
        Assert.Equal(expected.Length, outcomes.For(solo.PlayerId)!.Participants); // Result remains in its existing owner.
    }
    [Fact]
    public void GuideNeverGatesAnExpertWhoSkipsItsSuggestedOpening()
    {
        using var solo = ScenarioRunner.CreateAt("initial");
        for (int stage = 0; stage < 3; stage++)
        {
            Assert.NotNull(OpeningGuidance.Project(solo.Snapshot, solo.City));
            Accept(solo.Send("ready"));
        }
        Assert.Equal(Phase.Combat, solo.Snapshot.Phase);
        Assert.Null(OpeningGuidance.Project(solo.Snapshot, solo.City));
    }
}
