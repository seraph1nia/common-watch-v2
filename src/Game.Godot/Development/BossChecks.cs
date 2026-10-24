// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckBossPresentation()
    {
        ReplaceSession(GameplayScenarios.CreateAt("major-10")); _gameUi.Details.Close(); _frozen = true; RefreshUi(true); RefreshWorld();
        var elite = _session.Snapshot.NextWave;
        Require(elite.Length == 6 && elite.All(u => !u.IsBoss) && elite.Count(u => u.Type == UnitType.Berserker && u.Level == 3) == 2,
            "W10 actual paired elite forecast, without a fabricated boss");
        Require(_gameUi.PreparationSummary.Text.Contains("Elite", StringComparison.OrdinalIgnoreCase), "elite purpose replaces the stale boss promise");
        await GameplayCapture("major-10-forecast"); await JourneyClick(_gameUi.ReadyButton);
        Require(_session.Snapshot.Phase == Phase.Combat && _session.Snapshot.Enemies.Select(u => u.Profile).OrderBy(p => p.Health).SequenceEqual(elite.Select(u => u.Profile).OrderBy(p => p.Health)),
            "native Ready consumes the actual elite profiles");
        RefreshWorld(); RefreshUi(true); await GameplayCapture("major-10-live");
        Require(_battlefield.BossFigureCount == 0 && _battlefield.CrownedFigures == 0, "formation does not invent boss crowns or profiles");
        GD.Print("GODOT_ELITE_PASS wave10 legal-paired-profiles native-ready no-fake-boss");
        foreach (int wave in new[] { 20 })
        {
            string scenario = "boss-" + wave;
            ReplaceSession(GameplayScenarios.CreateAt(scenario)); _gameUi.Details.Close(); _frozen = true; RefreshUi(true); RefreshWorld();
            var forecast = _session.Snapshot.NextWave.Single();
            Require(forecast.IsBoss && _session.Snapshot.Wave == wave, "legal single-boss preparation checkpoint");
            Require(_gameUi.PreparationSummary.Text.Contains("boss", StringComparison.OrdinalIgnoreCase)
                && _gameUi.PreparationSummary.TooltipText.Contains(HealthPoints.Format(forecast.Profile.Health)), "boss purpose and authoritative HP available before Ready");
            await GameplayCapture(scenario + "-forecast");
            await JourneyClick(_gameUi.ReadyButton);
            Require(_session.Snapshot.Phase == Phase.Combat && _session.Snapshot.Enemies.Single().Profile == forecast.Profile, "native Ready consumes the accepted boss profile exactly");
            for (int n = 0; n < 180 && !_session.Playback.Units().Any(u => u.IsBoss && u.Hex?.Lifecycle == UnitLifecycle.Alive); n++) _session.Step();
            _frozen = true; RefreshWorld(); RefreshUi(true); await GameplayCapture(scenario + "-live");
            Require(_battlefield.BossHealthText.Contains("BOSS") && _battlefield.BossHealthFits, "ordinary gameplay HUD retains visible boss health emphasis");
            _battle = new(scenario) { Paused = true }; _arenaCameraKey = "";
            var alive = _battle.Frames.First(f => f.Enemies.Any(u => u.IsBoss && u.Hex?.Lifecycle == UnitLifecycle.Alive));
            var boss = alive.Enemies.Single(u => u.IsBoss);
            var windup = _battle.Frames.First(f => f.Enemies.Any(u => u.IsBoss && u.PendingImpact && u.ImpactTick > f.Tick));
            var attack = windup.Enemies.Single(u => u.IsBoss);
            var hit = _battle.Frames.SelectMany(f => f.CombatEvents).First(e => e.Type == CombatEventType.Hit && e.Unit is { IsBoss: true } && e.Damage > 0);
            var death = _battle.Frames.SelectMany(f => f.CombatEvents).Single(e => e.Type == CombatEventType.Death && e.Unit is { IsBoss: true });
            SeekBattle(alive.Tick); RefreshUi(true); await GameplayCapture(scenario + "-entrance");
            Require(_battlefield.BossFigureCount == 1 && _battlefield.CrownedFigures == 1 && _battlefield.FigureScale(boss.Id) > 1.3f,
                "boss has a larger crowned figure in both sprite/geometric presentation");
            Require(_battlefield.BossHealthText.Contains("BOSS") && _battlefield.BossHealthFits && _battlefield.SquadCuesFit, "boss health emphasis fits without inspector");
            var layout = new ArenaLayout(alive.Arena!, new HexProjection(30, _options.GroundCompression));
            var point = CombatVisuals.Position(boss, alive, _battlefield.AnimationTick);
            var ground = new Vector2(point.X - layout.Center.X, point.Y * _options.GroundCompression - layout.Center.Y);
            Require(_battlefield.PickUnit(ground)?.Id == boss.Id, "boss rendering preserves logical ground picking");
            SeekBattle(attack.ActionStartTick + (attack.ImpactTick - attack.ActionStartTick) * .7); await GameplayCapture(scenario + "-windup");
            double tick = _battlefield.AnimationTick; string healthText = _battlefield.BossHealthText;
            for (int frame = 0; frame < 5; frame++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(tick == _battlefield.AnimationTick && healthText == _battlefield.BossHealthText, "paused authoritative playback freezes boss anticipation and health");
            SeekBattle(hit.Tick + 1); await GameplayCapture(scenario + "-wounded");
            var presented = _battlefield.Playback.Units().Single(u => u.Id == boss.Id);
            Require(_battlefield.BossHealthText.Contains(HealthPoints.Format(presented.Health)), "health number samples the actual presented boss");
            SeekBattle(death.Tick + 6); await GameplayCapture(scenario + "-defeated");
            Require(_battlefield.BossDefeatBursts == 1 && _battlefield.BossHealthText.Contains("DEFEATED"), "one playback-timed defeat response for the real boss death");
            RefreshWorld(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(_battlefield.BossDefeatBursts == 1, "redrawing does not duplicate death events");
            _battle.Restart(); RefreshWorld(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            Require(_battlefield.BossDefeatBursts == 0 && !_battlefield.BossHealthText.Contains("DEFEATED"), "restart clears prior boss death presentation");
            _battle = null; _arenaCameraKey = "";
        }
        ReplaceSession(ScenarioRunner.CreateAt("initial")); _frozen = true; RefreshWorld(); RefreshUi(true);
        GD.Print("GODOT_BOSS_PASS wave20 legal-forecast larger-crown health windup pause death restart native-ready unchanged-ground");
    }
}
