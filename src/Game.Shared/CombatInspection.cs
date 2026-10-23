using Game.Core;

namespace Game.Shared;

/// <summary>Plain-language labels over a presented combat frame; no combat or timing decisions.</summary>
public static class CombatInspection
{
    public static string Role(UnitState unit) => unit.Class switch
    { UnitClass.Melee => "Frontline", UnitClass.Ranged => "Ranged", _ => "Magic support" };
    public static string Owner(UnitState unit) => unit.Faction == Faction.Adventurers ? $"P{unit.Owner}" : $"Enemy → P{unit.Destination}";
    public static UnitState[] TargetMembers(UnitState unit, IReadOnlyList<UnitState> units)
    {
        if (unit.TargetKind == SquadTargetKind.Squad)
            return units.Where(u => u.Id == unit.TargetId && u.Faction != unit.Faction).ToArray();
        if (unit.TargetKind == SquadTargetKind.None && unit.Decision is { ObjectiveId: > 0, ObjectiveCity: false } objective)
            return units.Where(u => u.SquadId == objective.ObjectiveId && u.Faction != unit.Faction).ToArray();
        return [];
    }
    public static StructureState? TargetStructure(UnitState unit, IReadOnlyList<StructureState> structures)
    {
        if (unit.TargetKind is SquadTargetKind.Keep or SquadTargetKind.Watchpost)
            return structures.FirstOrDefault(s => s.Owner == unit.TargetId && s.Kind == (unit.TargetKind == SquadTargetKind.Watchpost ? ArenaStructureKind.Watchpost : ArenaStructureKind.Keep));
        if (unit.TargetKind == SquadTargetKind.None && unit.Decision is { ObjectiveId: > 0, ObjectiveCity: true } objective)
            return structures.FirstOrDefault(s => s.Owner == objective.ObjectiveId && s.Cell == objective.ObjectiveCell);
        return null;
    }
    public static string Target(UnitState unit, IReadOnlyList<UnitState> units, IReadOnlyList<StructureState>? structures = null)
    {
        string approach = unit.TargetKind == SquadTargetKind.None ? "Approaching " : "";
        if (structures is not null && TargetStructure(unit, structures) is { } structure)
            return $"{approach}P{structure.Owner} {structure.Kind}";
        if (TargetMembers(unit, units).FirstOrDefault() is { } target)
            return $"{approach}{Owner(target)} · {target.Type}";
        return "No current target";
    }
    public static string Effects(UnitState unit, double tick)
    {
        var parts = new List<string>();
        bool Active(StatusEffect? effect) => effect is not null && tick >= effect.AppliedTick && tick < effect.ExpiresTick;
        if (Active(unit.Statuses.Burn)) parts.Add("Burning");
        int poison = unit.Statuses.Poison.Count(e => Active(e));
        if (poison > 0) parts.Add($"Poison ×{poison}");
        if (Active(unit.Statuses.Chill)) parts.Add($"Chilled {unit.Statuses.Chill!.Strength}%");
        return parts.Count == 0 ? "None" : string.Join(" · ", parts);
    }
    public static string Basic(UnitState unit, IReadOnlyList<UnitState> units, double tick, bool preview = false, IReadOnlyList<StructureState>? structures = null)
        => (preview ? "NEXT WAVE · read-only\n" : "")
        + $"{Owner(unit)} · {unit.Type}{(unit.IsBoss ? " · BOSS" : "")}\n{Role(unit)} · Equipment L{unit.Level}"
        + $"\nHP {HealthPoints.Format(unit.Health)}/{HealthPoints.Format(unit.Profile.Health)}"
        + $" · Base strike {HealthPoints.Format(unit.Profile.Damage)} · Reach {unit.Profile.HexRange}"
        + $"\nNow: {(unit.Health <= 0 ? "Fallen" : unit.Hex?.Action.ToString() ?? "Preparing")}\nTarget: {Target(unit, units, structures)}"
        + $"\nEffects: {Effects(unit, tick)}";
}
