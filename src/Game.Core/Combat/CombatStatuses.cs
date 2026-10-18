namespace Game.Core;

internal sealed partial class CombatSimulation
{
    private readonly Dictionary<int, PeriodicContribution[]> _periodic = [];
    private Dictionary<int, StatusState> GatherPeriodic(CombatUnit[] before, long tick, SortedDictionary<int, int> damage)
    {
        _periodic.Clear();
        var updates = new Dictionary<int, StatusState>();
        foreach (CombatUnit unit in before.Where(u => u.Location.Lifecycle != UnitLifecycle.Dying && u.Statuses.Active))
        {
            var result = StatusPolicy.Advance(unit.Statuses, tick, _configuration.Statuses);
            updates.Add(unit.Id, result.State);
            if (result.Damage.Length > 0) _periodic.Add(unit.Id, result.Damage.Select(c => c with { Damage = unit.Capabilities.Reduce(c.Damage) }).ToArray());
            foreach (PeriodicContribution contribution in result.Damage)
                Accumulate(damage, unit.Id, unit.Capabilities.Reduce(contribution.Damage));
        }
        return updates;
    }
    private static void GatherApplications(CombatUnit actor, int victim, long tick, List<StatusApplication> applications, int damage, long attackSequence, int actualDamage = -1)
    {
        if (damage == 0) return;
        UnitCapabilities c = actor.Capabilities;
        foreach (var (kind, percent) in new[] { (StatusKind.Burn, c.BurnPercent), (StatusKind.Poison, c.PoisonPercent), (StatusKind.Chill, c.ChillPercent) })
        {
            int captured = kind == StatusKind.Burn && actor.Type == UnitType.Crossbowman && actualDamage >= 0 ? actualDamage : damage;
            int strength = kind == StatusKind.Chill ? percent : StatusPolicy.Potency(captured, percent);
            if (strength > 0) applications.Add(new(victim, kind, actor.Id, attackSequence, tick, strength));
        }
    }
    private void CommitStatuses(Dictionary<int, StatusState> updates, List<StatusApplication> applications)
    {
        foreach ((int id, StatusState state) in updates)
            Unit(id) = Unit(id) with { Statuses = state };
        foreach (StatusApplication application in applications.OrderBy(a => a.VictimId).ThenBy(a => a.Kind).ThenBy(a => a.SourceId).ThenBy(a => a.AttackSequence))
        {
            CombatUnit victim = Unit(application.VictimId);
            if (victim.Health > 0) Unit(victim.Id) = victim with { Statuses = StatusPolicy.Apply(victim.Statuses, application, _configuration.Statuses) };
        }
    }
}
