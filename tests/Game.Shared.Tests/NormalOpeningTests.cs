// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;
namespace Game.Shared.Tests;

public sealed class NormalOpeningTests
{
    [Theory]
    [InlineData("warden")]
    [InlineData("merchant")]
    [InlineData("warmaster")]
    [InlineData("arcanist")]
    [InlineData("grovekeeper")]
    [InlineData("huntsman")]
    public void NormalConstructionFundsEveryDoctrineAcrossThreeWaves(string doctrine)
    {
        foreach (ulong seed in new[] { 1UL, SoloSession.DefaultSeed })
        {
            var run = RewardEconomyReview.Run(doctrine, "opening", seed, openingOrder: "normal-wood");
            Assert.Equal(3, run.Battles.Length);
            Assert.All(run.Commands, c => Assert.True(c.Result.Accepted, c.Result.Message));
            Assert.All(run.Commands.Where(c => c.Command.Action == "build"), c => Assert.Equal(ConstructionPayment.Standard, c.Command.Payment));
            Assert.All(run.Battles, b => { Assert.Equal("RewardDraft", b.Result); Assert.Equal(0, b.Unfunded); });
            Assert.True(run.Battles[0].Participating >= 3);
        }
    }
    [Theory]
    [InlineData("arcanist", UnitType.Mage)]
    [InlineData("huntsman", UnitType.Crossbowman)]
    public void SpecialistAlternativeIsPaidAndItsCasualtiesAreNotHidden(string doctrine, UnitType specialist)
    {
        var run = RewardEconomyReview.Run(doctrine, "opening", 1, openingOrder: "normal-role");
        Assert.Equal(3, run.Battles.Length);
        Assert.Equal(3, run.Battles[0].Participating);
        Assert.True(run.Battles[0].Casualties > 0);
        Assert.Contains(run.Commands.TakeWhile(c => !c.Command.Action.StartsWith("reward:", StringComparison.Ordinal)),
            c => c.Command.Action == "recruit" && c.Command.SoldierType == specialist);
        Assert.All(run.Commands.Where(c => c.Command.Action == "build"), c => Assert.Equal(ConstructionPayment.Standard, c.Command.Payment));
    }
    [Fact]
    public void MerchantFrontlineFirstFailureRemainsARealConsequence()
    {
        var run = RewardEconomyReview.Run("merchant", "opening", 1, openingOrder: "normal-frontline");
        Assert.Single(run.Battles);
        Assert.Equal("Defeat", run.Battles[0].Result);
        Assert.Equal(2, run.Battles[0].Participating);
        Assert.Equal(2, run.Battles[0].Casualties);
        Assert.True(run.Battles[0].KeepDamage > 0);
    }
}
