// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class BuildImpactTests
{
    private static void Accept(CommandResult result) => Assert.True(result.Accepted, result.Message);
    [Fact]
    public void ThreePrimaryChoicesDescribeFrozenBenefitsWithoutTechCountersOrMutation()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        string before = JsonSerializer.Serialize(solo.Snapshot, WireJson.Options);
        var view = RunPresentation.Project(solo.Snapshot, solo.City, solo.PlayerId, true, false);
        Assert.Equal(3, view.Cards.Length);
        foreach (var quote in solo.City.Run.Quotes)
        {
            var card = view.Cards.Single(c => c.Id == quote.Id);
            Assert.Equal(RunPresentation.Benefit(quote.Resources, quote.KeepHealing, quote.SoldierHealing, quote.Soldiers.Length), card.Description);
            Assert.True(BuildImpact.Reward(solo.Snapshot, solo.City, quote.Id).HasCurrentTarget);
            foreach (string removed in new[] { "multiplier", "stack", "relic", "Current effects" })
                Assert.DoesNotContain(removed, card.Description + card.Subtitle + card.Footer, StringComparison.OrdinalIgnoreCase);
        }
        Assert.False(BuildImpact.Reward(solo.Snapshot, solo.City, "A01").HasCurrentTarget);
        Assert.Equal(before, JsonSerializer.Serialize(solo.Snapshot, WireJson.Options));
    }
    [Fact]
    public void ActualChosenBonusMatchesTheQuoteAndLeavesProfilesAndProductionUnmodified()
    {
        using var solo = ScenarioRunner.CreateAt("draft");
        var before = solo.City; var quote = before.Run.Quotes.First();
        Accept(solo.ChooseReward(quote.Id));
        var after = solo.City; var receipt = Assert.Single(after.Run.Rewards);
        Assert.Equal(quote.Resources, receipt.Resources); Assert.Equal(quote.KeepHealing, receipt.KeepHealing);
        Assert.Equal(quote.SoldierHealing, receipt.SoldierHealing); Assert.Equal(quote.Soldiers.Length, receipt.SoldierCount);
        Assert.True(before.Resources.TryAdd(quote.Resources, out var stock)); Assert.Equal(stock, after.Resources);
        Assert.Equal(before.ProductionIncome, after.ProductionIncome);
        Assert.Equal(before.Soldiers.Select(u => u.Profile), after.Soldiers.Select(u => u.Profile));
        Assert.Contains("history", BuildImpact.Inspection(after));
        Assert.Contains("Fortified defense", BuildDirection.Inspection(after));
    }
    [Fact]
    public void EveryTechnologyStillHasAReadOnlyExplanationOnALegalMixedRoster()
    {
        using var solo = GameplayScenarios.CreateAt("army");
        string before = JsonSerializer.Serialize(solo.Snapshot, WireJson.Options);
        foreach (var technology in solo.Snapshot.TechnologyCatalog)
        {
            var view = BuildImpact.Research(solo.Snapshot, solo.City, technology);
            Assert.NotEmpty(view.Targets); Assert.NotEmpty(view.Details);
        }
        Assert.Equal(before, JsonSerializer.Serialize(solo.Snapshot, WireJson.Options));
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
    [Theory]
    [InlineData("frontline")]
    [InlineData("ranged")]
    [InlineData("research")]
    public void PaidImmediateRewardReplaysRetainThreeDistinctOffersAndExactEconomy(string family)
    {
        var first = RewardEconomyReview.Run("warden", family, 1, 5);
        var second = RewardEconomyReview.Run("warden", family, 1, 5);
        Assert.Equal(5, first.Battles.Length);
        Assert.Equal(JsonSerializer.Serialize(first, WireJson.Options), JsonSerializer.Serialize(second, WireJson.Options));
        Assert.All(first.Battles, battle => { Assert.Equal("RewardDraft", battle.Result); Assert.Equal(3, battle.Offers.Distinct().Count()); });
    }
}
