// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Xunit;

namespace Game.Shared.Tests;

public sealed class EncounterReviewTests
{
    [Fact]
    public void ActualCatalogPurposesDistinguishRolesEquipmentStepsAndBossRecovery()
    {
        using var solo = new SoloSession();
        var catalog = solo.Snapshot.WaveCatalog;
        var names = catalog.Select(w => EncounterBriefing.Purpose(w, catalog.FirstOrDefault(p => p.Number == w.Number - 1), w.Number == 20)).ToArray();
        Assert.True(names.Distinct().Count() >= 7);
        Assert.Equal("Ranged pressure", names[2]); Assert.Equal("Mixed-arms pressure", names[3]);
        Assert.Equal("Heavier frontline", names[4]); Assert.Equal("Elite formation", names[9]);
        Assert.Equal("Ranged follow-up", names[10]); Assert.Equal("Final boss", names[19]);
        Assert.All(catalog.Where(w => w.IsBoss), w => { var entry = Assert.Single(w.Entries); Assert.True(entry.IsBoss); Assert.Equal(1, entry.Count); });
        var combat = new CombatConfiguration(solo.Snapshot.Rules);
        var campaign = new CampaignConfiguration(solo.Snapshot.Rules.Campaign, combat);
        Assert.All(catalog, w => Assert.Equal(WavePower.Total(w, combat) * 6, WavePower.Total(w, combat, 6)));
    }
    [Theory]
    [InlineData("boss-20")]
    public void BossBriefUsesTheRealForecastAndRecordedBossDeathWithoutStateInjection(string checkpoint)
    {
        using var solo = GameplayScenarios.CreateAt(checkpoint);
        long revision = solo.Snapshot.Revision;
        var brief = EncounterBriefing.Project(solo.Snapshot, solo.PlayerId);
        var boss = Assert.Single(solo.Snapshot.NextWave);
        Assert.True(boss.IsBoss); Assert.Contains(HealthPoints.Format(boss.Profile.Health), brief.Boss);
        Assert.Contains("No escorts", brief.Boss); Assert.Contains("secondary", brief.Decisions);
        Assert.Equal(revision, solo.Snapshot.Revision);
        var recording = new BattleShowcase(checkpoint);
        var death = Assert.Single(recording.Frames.SelectMany(f => f.CombatEvents), e => e.Type == CombatEventType.Death && e.Unit is { IsBoss: true });
        Assert.Equal(boss.Profile, death.Unit!.Profile); Assert.Equal(0, death.Unit.Health);
        Assert.NotEqual(Phase.Combat, recording.Frames[^1].Phase);
        Assert.Equal(checkpoint == "boss-20" ? Phase.Victory : Phase.RewardDraft, recording.Frames[^1].Phase);
    }
    [Fact]
    public void EliteBriefAndRecordedDeathsUseTheCurrentCatalogWithoutInventingBosses()
    {
        using var solo = GameplayScenarios.CreateAt("major-10");
        var forecast = solo.Snapshot.NextWave;
        Assert.Equal(6, forecast.Length); Assert.DoesNotContain(forecast, u => u.IsBoss);
        Assert.Equal("Elite formation", EncounterBriefing.Project(solo.Snapshot, solo.PlayerId).Purpose);
        var recording = new BattleShowcase("major-10");
        Assert.Equal(6, recording.Frames.SelectMany(f => f.CombatEvents).Count(e => e.Type == CombatEventType.Death && e.Unit?.Faction == Faction.Skeletons));
        Assert.DoesNotContain(recording.Frames.SelectMany(f => f.Enemies), u => u.IsBoss);
        Assert.Equal(Phase.RewardDraft, recording.Frames[^1].Phase);
    }
    [Theory]
    [InlineData("mixed")]
    [InlineData("research")]
    public void ForecastResponsesAreLegalAndPreserveExactSpawningUpkeepRewardsAndTerminalProgression(string family)
    {
        var run = CampaignReview.Run(family, 1, true);
        Assert.Equal(20, run.Waves.Length); Assert.Equal("Victory", run.Waves[^1].Result);
        Assert.Contains(run.Waves, w => w.Adaptations.Any());
        Assert.Contains(run.Waves.SelectMany(w => w.Adaptations), a => a.Contains("Research"));
        foreach (var wave in run.Waves)
        {
            var reward = Assert.IsType<ResourceCost>(wave.Reward);
            Assert.Equal(wave.Wave is 10 or 20 ? 4 : 2, reward.Gold);
            Assert.Equal(wave.Wave is 10 or 20 ? 2 : 1, reward.Wood);
            Assert.Equal(wave.Wave is 10 or 20 ? 10 : 5, reward.Food);
            Assert.True(wave.Ticks > 0); Assert.True(wave.Participants > 0);
        }
    }
    [Fact]
    public void TerminalBriefDoesNotPromiseANewEncounterOrReward()
    {
        using var victory = GameplayScenarios.CreateAt("victory");
        var won = EncounterBriefing.Project(victory.Snapshot, victory.PlayerId);
        Assert.Equal("Campaign complete", won.Purpose); Assert.Empty(won.Boss);
        using var defeat = GameplayScenarios.CreateAt("defeat");
        Assert.Equal("Campaign ended", EncounterBriefing.Project(defeat.Snapshot, defeat.PlayerId).Purpose);
    }
}
