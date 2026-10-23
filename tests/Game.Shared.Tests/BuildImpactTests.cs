// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class BuildImpactTests
{
    private static void Accept(CommandResult result) => Assert.True(result.Accepted, result.Message);
    [Fact]
    public void EmptyRosterLabelsFuturePurchasesAsPivotsWithoutInventingTargets()
    {
        using var solo = new SoloSession(); Accept(solo.Start());
        foreach (string id in new[] { "A01", "A04", "A06", "M03", "R01", "R03", "T02", "E01" })
        {
            var view = BuildImpact.Reward(solo.Snapshot, solo.City, id);
            Assert.False(view.HasCurrentTarget); Assert.Contains("Pivot", view.Targets);
        }
        Assert.True(BuildImpact.Reward(solo.Snapshot, solo.City, "T01").HasCurrentTarget);
        Assert.Contains("62.5 → 72.5", BuildImpact.Reward(solo.Snapshot, solo.City, "T01").Details);
        Assert.Contains("Keep +0 HP", BuildImpact.Reward(solo.Snapshot, solo.City, "F04").Targets);
    }
    [Fact]
    public void EveryExistingCardAndTechnologyCanBeInspectedOnALegalMixedRosterWithoutMutation()
    {
        using var solo = GameplayScenarios.CreateAt("army");
        string before = System.Text.Json.JsonSerializer.Serialize(solo.Snapshot, WireJson.Options);
        foreach (var card in RunCatalog.Cards)
        {
            var view = BuildImpact.Reward(solo.Snapshot, solo.City, card.Id);
            Assert.NotEmpty(view.Targets); Assert.NotEmpty(view.Details);
        }
        foreach (var technology in solo.Snapshot.TechnologyCatalog)
        {
            var view = BuildImpact.Research(solo.Snapshot, solo.City, technology);
            Assert.NotEmpty(view.Targets); Assert.NotEmpty(view.Details);
        }
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(solo.Snapshot, WireJson.Options));
        Assert.Contains("already", BuildImpact.Reward(solo.Snapshot, solo.City, "M06").Details);
        Assert.Contains("first", BuildImpact.Reward(solo.Snapshot, solo.City, "R01").Details, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void TowerTargetsIncludeSettlementAndArmyHomes()
    {
        using var solo = GameplayScenarios.CreateAt("late");
        Accept(solo.Send("buy-plot", 7)); Accept(solo.Send("build", 7, Building.ArrowTower));
        var home = solo.City.Army!.Homes[0];
        foreach (var unit in solo.City.Soldiers.Where(u => u.Assignment?.Tile == home.Cell).ToArray()) Accept(solo.Send("retire", unitId: unit.Id));
        Accept(solo.Send("build", home.PlotId, Building.ArrowTower));
        var view = BuildImpact.Reward(solo.Snapshot, solo.City, "T02");
        Assert.Equal("2 current arrow towers", view.Targets);
        Assert.Contains("Plot 8: damage 5.5/0 → 6/0", view.Details);
        Assert.Contains("Home 1: damage 5.5/0 → 6/0", view.Details);
    }
    [Fact]
    public void AvailableResearchPreviewMatchesTheAcceptedProfilesAndDoesNotHealVeterans()
    {
        using var solo = GameplayScenarios.CreateAt("research-open");
        var technology = solo.Snapshot.TechnologyCatalog.Single(t => t.Id == TechnologyId.MeleeFoundation);
        var before = solo.City.Soldiers.First(u => u.Type == UnitType.Swordsman);
        var view = BuildImpact.Research(solo.Snapshot, solo.City, technology);
        Accept(solo.Send("research-tech", technology: technology.Id));
        var after = solo.City.Soldiers.Single(u => u.Id == before.Id);
        Assert.Contains($"max HP {HealthPoints.Format(before.Profile.Health)} → {HealthPoints.Format(after.Profile.Health)}", view.Details);
        Assert.Contains($"primary {HealthPoints.Format(before.Profile.Damage)} → {HealthPoints.Format(after.Profile.Damage)}", view.Details);
        Assert.Equal(before.Health, after.Health);
        Assert.Contains("acquired", BuildImpact.Research(solo.Snapshot, solo.City, technology).Details);
    }
    [Fact]
    public void AvailableProductionResearchPreviewMatchesActualIncomeIncludingExistingBonuses()
    {
        using var solo = GameplayScenarios.CreateAt("research-full");
        Accept(solo.Send("sell", 4)); Accept(solo.Send("build", 4, Building.Stonecutter)); Accept(solo.Send("upgrade", 4));
        var technology = solo.Snapshot.TechnologyCatalog.Single(t => t.Id == TechnologyId.Stonecraft);
        var view = BuildImpact.Research(solo.Snapshot, solo.City, technology);
        Assert.Contains("Stonecutter", view.Targets); Assert.Contains("2 → 3 Stone/production", view.Details);
        var before = solo.City.ProductionIncome!.Value;
        Accept(solo.Send("research-tech", technology: technology.Id));
        Assert.Equal(before.Stone + solo.City.Slots.Count(s => s.Type == Building.Stonecutter), solo.City.ProductionIncome!.Value.Stone);
    }
    [Fact]
    public void RealOfferPreviewMatchesTheAcceptedRewardAndRetainsOffers()
    {
        using var solo = GameplayScenarios.CreateAt("reward");
        string[] offers = solo.City.Run.Offers.ToArray();
        string id = offers.First(); var view = BuildImpact.Reward(solo.Snapshot, solo.City, id);
        var before = solo.City.Soldiers.ToArray();
        Accept(solo.ChooseReward(id));
        Assert.Equal(offers, solo.City.Run.Offers); Assert.Equal(id, solo.City.Run.Choice);
        foreach (var soldier in solo.City.Soldiers)
        {
            var old = before.Single(u => u.Id == soldier.Id);
            if (old.Profile.Health != soldier.Profile.Health)
                Assert.Contains($"max HP {HealthPoints.Format(old.Profile.Health)} → {HealthPoints.Format(soldier.Profile.Health)}", view.Details);
        }
        Assert.Contains("Fortified defense", BuildDirection.Inspection(solo.City));
    }
    [Theory]
    [InlineData("E01")]
    [InlineData("A03")]
    public void OfferedProducerAndCrossbowBonusesMatchAcceptedIncomeAndRoundedDamage(string id)
    {
        using var solo = new SoloSession(); Accept(solo.Send("doctrine:huntsman")); Accept(solo.Start());
        for (int wave = 1; wave <= 4; wave++)
        {
            if (solo.Snapshot.Phase == Phase.RewardDraft) Accept(solo.ChooseReward(GameplayScenarios.RewardChoice(solo.City.Run)));
            for (int stage = 0; stage < 3; stage++)
            {
                for (int n = 0; n < 100 && OpeningCampaign.Next(solo.Snapshot, solo.PlayerId) is { } intent; n++) Accept(solo.Submit(intent));
                Accept(solo.Send("ready"));
            }
            GameplayScenarios.FinishCombat(solo);
        }
        Assert.Contains(id, solo.City.Run.Offers);
        var before = solo.City; var view = BuildImpact.Reward(solo.Snapshot, before, id);
        Assert.True(view.HasCurrentTarget); Accept(solo.ChooseReward(id));
        if (id == "E01")
        {
            Assert.Contains("Wood/production", view.Details);
            Assert.Equal(before.ProductionIncome!.Value.Wood + before.Slots.Count(s => s.Type == Building.Lumbermill), solo.City.ProductionIncome!.Value.Wood);
        }
        else
        {
            var bows = before.Soldiers.Where(u => u.Type == UnitType.Crossbowman).ToArray(); Assert.NotEmpty(bows);
            foreach (var old in bows)
            {
                var current = solo.City.Soldiers.Single(u => u.Id == old.Id);
                Assert.True(current.Profile.Damage > old.Profile.Damage);
                Assert.Contains($"primary {HealthPoints.Format(old.Profile.Damage)} → {HealthPoints.Format(current.Profile.Damage)}", view.Details);
                Assert.Equal(old.Health, current.Health);
            }
        }
    }
    [Theory]
    [InlineData("frontline")]
    [InlineData("ranged")]
    [InlineData("research")]
    public void LegalReplayNumberingAndRetainedDraftsStayDeterministic(string family)
    {
        var a = BuildIdentityReview.Review("warden", family, 1, 5);
        var b = BuildIdentityReview.Review("warden", family, 1, 5);
        Assert.Equal(5, a.Waves.Length);
        Assert.Equal(a.Actions, b.Actions);
        for (int i = 0; i < a.Waves.Length; i++)
        {
            Assert.Equal("RewardDraft", a.Waves[i].Result);
            Assert.Equal(i + 1, a.Waves[i].Draft!.CompletedWave);
            Assert.Equal(a.Waves[i].Draft!.Offers, b.Waves[i].Draft!.Offers);
            Assert.Equal(3, a.Waves[i].Draft!.Offers.Distinct().Count());
            Assert.Equal(3, a.Waves[i].Draft!.OneCurrentSlotExperiment.Distinct().Count());
        }
    }
}
