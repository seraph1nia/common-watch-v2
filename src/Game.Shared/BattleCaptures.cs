using Game.Core;
using System.Text.Json;

namespace Game.Shared;

public sealed record BattleCapturePoint(string Name, string Scenario, double Tick);

public static class BattleCaptures
{
    public static IReadOnlyList<BattleCapturePoint> Plan(BattleShowcase battle, bool sequence = true)
    {
        var result = new List<BattleCapturePoint>();
        var events = battle.Frames.SelectMany(f => f.CombatEvents).ToArray();
        void Add(string name, double tick) => result.Add(new(name, battle.Scenario, tick));
        if (battle.Scenario == "swordsman")
        {
            CombatEvent attack = events.First(e => e.Type == CombatEventType.AttackStarted && e.Unit?.Faction == Faction.Adventurers);
            UnitState actor = attack.Unit!;
            HexUnitState move = battle.Frames.SelectMany(f => f.Players.SelectMany(c => c.Soldiers)).Select(u => u.Hex).OfType<HexUnitState>().First(h => h.HoldsTransit);
            CombatEvent hit = events.First(e => e.Type == CombatEventType.Hit && e.Damage > 0 && e.Unit?.Faction == Faction.Adventurers);
            CombatEvent death = events.First(e => e.Type == CombatEventType.Death);
            Add("swordsman-idle", battle.Frames[0].Tick);
            Add("swordsman-moving", (move.StartTick + move.EndTick) / 2.0);
            Add("swordsman-anticipation", actor.ActionStartTick + (actor.ImpactTick - actor.ActionStartTick) * .4);
            Add("swordsman-lunge", actor.ActionStartTick + (actor.ImpactTick - actor.ActionStartTick) * .92);
            Add("swordsman-impact", actor.ImpactTick + 1);
            Add("swordsman-recovery", actor.ImpactTick + (actor.ReadyTick - actor.ImpactTick) * .3);
            Add("swordsman-damaged", hit.Tick + 1);
            Add("swordsman-death", (death.Unit!.Hex!.DeathStartTick + death.Unit.Hex.DeathEndTick) / 2.0);
            Add("battle-complete", battle.Frames[^1].Tick);
            var towerShot = events.First(e => e.Type == CombatEventType.AttackStarted && e.Tower is not null);
            Add("structure-shot", towerShot.Tower!.ActionStartTick + (towerShot.Tower.ImpactTick - towerShot.Tower.ActionStartTick) * .85);
            if (sequence)
                for (double tick = battle.Frames[0].Tick; tick <= death.Unit.Hex.DeathEndTick + 6; tick += 6)
                    Add($"sequence/frame-{(int)((tick - battle.Frames[0].Tick) / 6):D4}", tick);
        }
        else if (battle.Scenario == "ranged")
        {
            CombatEvent shot = events.First(e => e.Type == CombatEventType.AttackStarted && e.Unit?.Type == UnitType.Crossbowman);
            Add("ranged-projectile", shot.Unit!.ActionStartTick + (shot.Unit.ImpactTick - shot.Unit.ActionStartTick) * .8);
            CombatEvent magic = events.First(e => e.Type == CombatEventType.Impact && e.Unit?.Type == UnitType.Mage);
            Add("magic-impact", magic.Tick + 2);
        }
        else if (battle.Scenario == "structures")
        {
            CombatEvent hit = events.First(e => e.Type == CombatEventType.StructureHit);
            CombatEvent death = events.First(e => e.Type == CombatEventType.StructureDestroyed);
            var tower = events.First(e => e.Type == CombatEventType.AttackStarted && e.Tower is { Slot: >= 0 }).Tower!;
            Add("tower-shot", tower.ActionStartTick + (tower.ImpactTick - tower.ActionStartTick) * .85);
            Add("structure-damaged", hit.Tick + 2);
            Add("structure-destroyed", death.Tick + 6);
            Add("structure-battle-complete", battle.Frames[^1].Tick);
        }
        else
        {
            MatchSnapshot status = battle.Frames.First(f => f.Players.SelectMany(p => p.Soldiers).Concat(f.Enemies).Any(u => u.Statuses.Active));
            Add(battle.Scenario == "status" ? "status-effect" : "status-" + battle.Scenario, status.Tick + 3);
            CombatEvent? shot = events.FirstOrDefault(e => e.Type == CombatEventType.DefenderShot);
            if (shot is not null) Add("structure-shot", shot.Tick + 1);
            CombatEvent? destroyed = events.FirstOrDefault(e => e.Type == CombatEventType.StructureDestroyed);
            if (destroyed is not null) Add("structure-destroyed", destroyed.Tick + 4);
        }
        return result;
    }
    public static void Metadata(string path, string engine, BattleShowcase battle, RenderOptions options, double frameMs, long allocated, int effects, bool shader, float zoom = 1, float panX = 0, float panY = 0)
    {
        var units = battle.Playback.Units();
        File.WriteAllText(path, JsonSerializer.Serialize(new
        {
            engine, scenario = battle.Scenario, source = "legal default-rules campaign command replay", wave = battle.Snapshot.Wave,
            authoritativeTick = battle.Snapshot.Tick, presentationTick = battle.Playback.Tick,
            presentationSeconds = battle.Playback.Tick / Match.StepsPerSecond, eventSequence = battle.Playback.EventCursor,
            mode = options.Mode.ToString(), view = options.View.ToString(), overlay = options.DebugOverlay, zoom, cameraPan = new { x = panX, y = panY },
            frameObservationMs = frameMs, allocatedBytesSinceLaunch = allocated, activeEventEffects = effects, shader,
            recentEvents = battle.Frames.SelectMany(f => f.CombatEvents).Where(e => e.Tick <= battle.Playback.Tick && e.Tick >= battle.Playback.Tick - 35)
                .Select(e => new { e.Sequence, e.Tick, type = e.Type.ToString(), unit = e.Unit?.Id, e.TargetId, e.Damage }),
            units = units.Select(u => new { u.Id, u.SquadId, type = u.Type.ToString(), u.Health, action = u.Hex?.Action.ToString(), lifecycle = u.Hex?.Lifecycle.ToString(), u.ActionStartTick, u.ImpactTick, u.ReadyTick, statuses = new { burn = u.Statuses.Burn?.ExpiresTick > battle.Playback.Tick, poison = u.Statuses.Poison.Any(s => s.AppliedTick <= battle.Playback.Tick && s.ExpiresTick > battle.Playback.Tick), chill = u.Statuses.Chill?.ExpiresTick > battle.Playback.Tick } })
        }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
