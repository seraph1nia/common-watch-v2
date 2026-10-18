using System.Collections.Frozen;

namespace Game.Core;

public enum TechnologyId
{
    None, MeleeFoundation, Guardian, GuardianMastery, Assault, AssaultMastery,
    RangedFoundation, Venom, VenomMastery, Precision, PrecisionMastery,
    MagicFoundation, Fire, FireMastery, Frost, FrostMastery,
    Stonecraft = 16, ManagedForestry = 17, Irrigation = 18
}
public static class TechnologyIds
{
    public static string Name(TechnologyId id) => string.Concat(id.ToString().Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    public static bool TryParse(string name, out TechnologyId id)
    {
        id = Enum.GetValues<TechnologyId>().FirstOrDefault(t => t != TechnologyId.None && (Name(t).Equals(name, StringComparison.OrdinalIgnoreCase) || t.ToString().Equals(name, StringComparison.OrdinalIgnoreCase)));
        return id != TechnologyId.None;
    }
}
public readonly record struct UnitCapabilities(int FoundationPercent = 0, int DirectDamagePercent = 0, int ReductionPercent = 0,
    int BurnPercent = 0, int PoisonPercent = 0, int ChillPercent = 0,
    int HealthPercent = 0, int PrimaryPercent = 0, int SecondaryPercent = 0, int RangeBonus = 0)
{
    public void Validate()
    {
        if (FoundationPercent is < 0 or > 10 || DirectDamagePercent is < 0 or > 20 || ReductionPercent is < 0 or > 20
            || BurnPercent is < 0 or > 40 || PoisonPercent is < 0 or > 20 || ChillPercent is < 0 or > 50
            || HealthPercent is < -10 or > 50 || PrimaryPercent is < 0 or > 50 || SecondaryPercent is < 0 or > 60 || RangeBonus is < 0 or > 1)
            throw new ArgumentException("Unsafe technology capabilities.");
    }
    public static int Scale(int value, int percent) => RunEffects.Scale(value, percent);
    public int Reduce(int damage) => RunEffects.Scale(damage, -ReductionPercent);
    public WeaponProfile Apply(WeaponProfile profile, int primaryBonus = 0, int secondaryBonus = 0)
    {
        Validate();
        return profile with
        {
            Health = Math.Max(1, RunEffects.Scale(Scale(profile.Health, FoundationPercent), HealthPercent)),
            Damage = RunEffects.Scale(Scale(profile.Damage, FoundationPercent), checked(DirectDamagePercent + PrimaryPercent + primaryBonus)),
            SecondaryDamage = RunEffects.Scale(Scale(profile.SecondaryDamage, FoundationPercent), checked(DirectDamagePercent + SecondaryPercent + secondaryBonus)),
            HexRange = profile.HexRange + RangeBonus
        };
    }
    internal UnitCapabilities Merge(UnitCapabilities other) => new(Math.Max(FoundationPercent, other.FoundationPercent), Math.Max(DirectDamagePercent, other.DirectDamagePercent),
        Math.Max(ReductionPercent, other.ReductionPercent), Math.Max(BurnPercent, other.BurnPercent), Math.Max(PoisonPercent, other.PoisonPercent), Math.Max(ChillPercent, other.ChillPercent));
}
public sealed record ProductionBenefit(Building Building, Resource Resource, int ExtraPerProducer);
public sealed record TechnologyDefinition(TechnologyId Id, UnitClass Class, int Tier, int Cost, TechnologyId Prerequisite, TechnologyId Branch, UnitCapabilities Benefit)
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public ProductionBenefit? Production { get; init; }
    public string Name => TechnologyIds.Name(Id);
    public UnitType[] EligibleTypes => Enum.GetValues<UnitType>().Where(t => Catalogs.Class(t) == Class).ToArray();
}
public sealed record TechnologyEligibility(TechnologyId Id, bool Available, string Reason);
public sealed record ResearchSettings
{
    public int FoundationCost { get; init; } = 3;
    public int SpecializationCost { get; init; } = 6;
    public int MasteryCost { get; init; } = 9;
    public int TowerLevelOneProgress { get; init; } = 1;
    public int TowerLevelTwoProgress { get; init; } = 2;
}
public readonly record struct ResearchState(int Points = 0, int Progress = 0, ulong Owned = 0)
{
    public TechnologyId[] Technologies => Enum.GetValues<TechnologyId>().Where(Has).ToArray();
    public bool Has(TechnologyId id) => id != TechnologyId.None && Enum.IsDefined(id) && (Owned & (1UL << (int)id)) != 0;
    public void Validate()
    {
        if (Points < 0 || Progress is < 0 or > 2 || (Owned & ~((1UL << 19) - 2)) != 0) throw new ArgumentException("Invalid research state.");
    }
    public ResearchState Income(int points = 0, int progress = 0)
    {
        Validate(); ArgumentOutOfRangeException.ThrowIfNegative(points); ArgumentOutOfRangeException.ThrowIfNegative(progress);
        long total = checked((long)Progress + progress);
        return this with { Points = checked((int)(Points + (long)points + total / 3)), Progress = (int)(total % 3) };
    }
    public ResearchState Purchase(TechnologyId id, ResearchCatalog catalog)
    {
        TechnologyEligibility eligibility = catalog.Eligibility(this, id);
        if (!eligibility.Available) throw new InvalidOperationException(eligibility.Reason);
        return this with { Points = Points - catalog.Get(id).Cost, Owned = Owned | (1UL << (int)id) };
    }
}
public sealed class ResearchCatalog
{
    private readonly FrozenDictionary<TechnologyId, TechnologyDefinition> _nodes;
    public ResearchCatalog(ResearchSettings? settings = null) : this(Default(settings ?? new())) { }
    public ResearchCatalog(IEnumerable<TechnologyDefinition> definitions)
    {
        TechnologyDefinition[] nodes = definitions.ToArray();
        if (nodes.Length != 18 || nodes.Select(n => n.Id).Distinct().Count() != nodes.Length
            || nodes.Any(n => n.Id == TechnologyId.None || !Enum.IsDefined(n.Id) || !Enum.IsDefined(n.Class) || n.Tier is < 1 or > 3 || n.Cost <= 0))
            throw new ArgumentException("A research catalog requires eighteen distinct valid nodes and positive prices.");
        _nodes = nodes.ToFrozenDictionary(n => n.Id);
        foreach (TechnologyDefinition node in nodes)
        {
            node.Benefit.Validate();
            if (node.Production is { } production && (node.Id < TechnologyId.Stonecraft || node.Tier != 3 || node.Benefit != default
                || !Enum.IsDefined(production.Building) || !Enum.IsDefined(production.Resource) || production.ExtraPerProducer is < 1 or > 2)
                || node.Id >= TechnologyId.Stonecraft && node.Production is null)
                throw new ArgumentException("Invalid village production benefit.");
            var visited = new HashSet<TechnologyId>(); TechnologyDefinition current = node;
            while (current.Prerequisite != TechnologyId.None)
            {
                if (!visited.Add(current.Id) || !_nodes.TryGetValue(current.Prerequisite, out TechnologyDefinition? parent)
                    || parent.Class != current.Class || parent.Tier != current.Tier - 1) throw new ArgumentException("Invalid research prerequisites or cycle.");
                current = parent;
            }
            if (current.Tier != 1 || current.Branch != TechnologyId.None
                || node.Tier == 1 && (node.Prerequisite != TechnologyId.None || node.Branch != TechnologyId.None)
                || node.Tier == 2 && node.Branch != node.Id
                || node.Tier == 3 && node.Branch != _nodes[node.Prerequisite].Branch) throw new ArgumentException("Inconsistent exclusive research descendants.");
        }
        foreach (UnitClass role in Enum.GetValues<UnitClass>())
            if (nodes.Count(n => n.Class == role && n.Tier == 1) != 1 || nodes.Count(n => n.Class == role && n.Tier == 2) != 2
                || nodes.Count(n => n.Class == role && n.Tier == 3 && n.Production is null) != 2) throw new ArgumentException("Each class needs one foundation and two exclusive paths.");
        if (nodes.Where(n => n.Tier == 2).Any(n => nodes.Count(m => m.Tier == 3 && m.Prerequisite == n.Id && m.Production is null) != 1))
            throw new ArgumentException("Each exclusive specialization needs exactly one combat mastery.");
    }
    public TechnologyDefinition Get(TechnologyId id) => _nodes[id];
    public TechnologyDefinition[] Definitions() => _nodes.Values.OrderBy(n => n.Id).ToArray();
    public void ValidateState(ResearchState state)
    {
        state.Validate();
        foreach (TechnologyDefinition node in _nodes.Values.Where(n => state.Has(n.Id)))
        {
            if (node.Prerequisite != TechnologyId.None && !state.Has(node.Prerequisite)
                || node.Branch != TechnologyId.None && _nodes.Values.Any(n => n.Tier == 2 && n.Class == node.Class && n.Id != node.Branch && state.Has(n.Id)))
                throw new ArgumentException("Invalid acquired research prerequisites or exclusive branches.");
        }
    }
    public TechnologyEligibility Eligibility(ResearchState state, TechnologyId id)
    {
        ValidateState(state);
        if (!_nodes.TryGetValue(id, out TechnologyDefinition? node)) return new(id, false, "Unknown technology.");
        if (state.Has(id)) return new(id, false, "Acquired.");
        TechnologyDefinition? sibling = _nodes.Values.FirstOrDefault(n => n.Tier == 2 && n.Class == node.Class && n.Id != node.Branch && state.Has(n.Id));
        if (node.Branch != TechnologyId.None && sibling is not null) return new(id, false, "Permanently locked by " + sibling.Name + ".");
        if (node.Prerequisite != TechnologyId.None && !state.Has(node.Prerequisite)) return new(id, false, "Requires " + TechnologyIds.Name(node.Prerequisite) + ".");
        return state.Points < node.Cost ? new(id, false, $"Needs {node.Cost} research points.") : new(id, true, "Available.");
    }
    public UnitCapabilities Capabilities(ResearchState state, UnitType type)
    {
        ValidateState(state);
        return Definitions().Where(n => n.Class == Catalogs.Class(type) && state.Has(n.Id)).Aggregate(default(UnitCapabilities), (c, n) => c.Merge(n.Benefit));
    }
    internal void Write(BinaryWriter writer, bool prices)
    {
        foreach (TechnologyDefinition node in Definitions().Where(n => prices || n.Production is null))
        {
            writer.Write((int)node.Id);
            if (prices)
            {
                writer.Write(node.Cost);
                if (node.Production is { } production)
                { writer.Write((int)node.Prerequisite); writer.Write((int)production.Building); writer.Write((int)production.Resource); writer.Write(production.ExtraPerProducer); }
            }
            else
            {
                writer.Write((int)node.Class); writer.Write(node.Tier); writer.Write((int)node.Prerequisite); writer.Write((int)node.Branch);
                writer.Write(node.Benefit.FoundationPercent); writer.Write(node.Benefit.DirectDamagePercent); writer.Write(node.Benefit.ReductionPercent);
                writer.Write(node.Benefit.BurnPercent); writer.Write(node.Benefit.PoisonPercent); writer.Write(node.Benefit.ChillPercent);
            }
        }
    }
    private static TechnologyDefinition[] Default(ResearchSettings s)
    {
        var nodes = new List<TechnologyDefinition>();
        void Class(UnitClass role, TechnologyId foundation, TechnologyId first, TechnologyId firstMastery, UnitCapabilities firstBenefit, UnitCapabilities firstAdvanced,
            TechnologyId second, TechnologyId secondMastery, UnitCapabilities secondBenefit, UnitCapabilities secondAdvanced)
        {
            nodes.Add(new(foundation, role, 1, s.FoundationCost, TechnologyId.None, TechnologyId.None, new(FoundationPercent: 5)));
            nodes.Add(new(first, role, 2, s.SpecializationCost, foundation, first, firstBenefit)); nodes.Add(new(firstMastery, role, 3, s.MasteryCost, first, first, firstAdvanced));
            nodes.Add(new(second, role, 2, s.SpecializationCost, foundation, second, secondBenefit)); nodes.Add(new(secondMastery, role, 3, s.MasteryCost, second, second, secondAdvanced));
        }
        Class(UnitClass.Melee, TechnologyId.MeleeFoundation, TechnologyId.Guardian, TechnologyId.GuardianMastery, new(ReductionPercent: 10), new(ReductionPercent: 20), TechnologyId.Assault, TechnologyId.AssaultMastery, new(DirectDamagePercent: 10), new(DirectDamagePercent: 20));
        Class(UnitClass.Ranged, TechnologyId.RangedFoundation, TechnologyId.Venom, TechnologyId.VenomMastery, new(PoisonPercent: 10), new(PoisonPercent: 15), TechnologyId.Precision, TechnologyId.PrecisionMastery, new(DirectDamagePercent: 10), new(DirectDamagePercent: 20));
        Class(UnitClass.Magic, TechnologyId.MagicFoundation, TechnologyId.Fire, TechnologyId.FireMastery, new(BurnPercent: 20), new(BurnPercent: 30), TechnologyId.Frost, TechnologyId.FrostMastery, new(ChillPercent: 20), new(ChillPercent: 40));
        nodes.Add(new(TechnologyId.Stonecraft, UnitClass.Melee, 3, s.MasteryCost, TechnologyId.Guardian, TechnologyId.Guardian, default) { Production = new(Building.Stonecutter, Resource.Stone, 1) });
        nodes.Add(new(TechnologyId.ManagedForestry, UnitClass.Ranged, 3, s.MasteryCost, TechnologyId.Precision, TechnologyId.Precision, default) { Production = new(Building.Lumbermill, Resource.Wood, 1) });
        nodes.Add(new(TechnologyId.Irrigation, UnitClass.Magic, 3, s.MasteryCost, TechnologyId.Frost, TechnologyId.Frost, default) { Production = new(Building.Farm, Resource.Food, 2) });
        return nodes.ToArray();
    }
}
