// SPDX-License-Identifier: GPL-3.0-only
using System.Runtime.CompilerServices;
using Game.Core;

namespace Game.Shared;

public sealed record ChoiceImpact(string Targets, string Details, bool HasCurrentTarget);

/// <summary>Read-only choice explanations. Hypothetical modifiers go only to Core's pure calculators.</summary>
public static class BuildImpact
{
    private sealed class Calculators(Rules rules)
    {
        public CombatConfiguration Combat { get; } = new(rules);
        public EconomyConfiguration Economy { get; } = new(rules);
    }
    private static readonly ConditionalWeakTable<Rules, Calculators> Cache = new();
    private static Calculators For(MatchSnapshot state) => Cache.GetValue(state.Rules, r => new(r));
    public static string Inspection(CityState city) => RunPresentation.Inspection(city);
    public static ChoiceImpact Reward(MatchSnapshot state, CityState city, string id)
    {
        var quote = city.Run.Quotes.FirstOrDefault(q => q.Id == id);
        if (quote is null) return new("Not in the frozen offer set", "This reward is not offered at the current boundary.", false);
        return new(RunPresentation.Benefit(quote.Resources, quote.KeepHealing, quote.SoldierHealing, quote.Soldiers.Length),
            RunCatalog.Card(id).MissingHealthPercent > 0
                ? "Up to 10 Keep HP and ceiling 20% of each living member's missing HP, including storage/reserves. Exact amounts frozen at the clear; no resurrection."
                : quote.ResourceCapped ? "Exact quoted resources are limited by remaining capacity. Applied once; no producer or production tick."
                : "Exact quoted resources, frozen at the completed wave. Applied once; no producer or production tick.", quote.HasBenefit);
    }
    private static ChoiceImpact Target(int count, string kind, string details, string pivot) => count == 0
        ? new("Pivot · no current " + kind, pivot, false) : new($"{count} current {kind}", details, true);
    private static string MemberTargets(UnitState[] units) => string.Join(" · ", units.GroupBy(u => u.Type).Select(g => $"{g.Count()} {g.Key}"))
        + $" · {units.Where(u => u.Assignment is { Stored: false }).Select(u => u.SquadId).Distinct().Count()} field squads"
        + (units.Any(u => u.Assignment is { Stored: true }) ? " + storage" : "");
    public static ChoiceImpact Research(MatchSnapshot state, CityState city, TechnologyDefinition technology)
    {
        bool owned = city.Research.Has(technology.Id);
        bool available = city.Technologies.Any(t => t.Id == technology.Id && t.Available);
        ResearchState after = available ? city.Research.Purchase(technology.Id, For(state).Economy.Research) : city.Research;
        if (technology.Production is { } benefit)
        {
            var definition = state.BuildingCatalog.Single(d => d.Type == benefit.Building);
            var slots = city.Slots.Select((s, i) => (s, i)).Where(p => p.s.Type == benefit.Building).ToArray();
            return Target(slots.Length, benefit.Building.ToString(), string.Join("\n", slots.Select(p => $"Plot {p.i + 1}: "
                + $"{EconomyConfiguration.ProducerOutput(definition, p.s.Level, city.Research, state.TechnologyCatalog)}"
                + (available ? $" → {EconomyConfiguration.ProducerOutput(definition, p.s.Level, after, state.TechnologyCatalog)}" : owned ? " (acquired)" : $"; locked bonus +{benefit.ExtraPerProducer}")
                + $" {benefit.Resource}/production before doctrine bonuses")), "Future " + benefit.Building + " producers benefit; inspect the prerequisites before investing.");
        }
        var units = city.Soldiers.Where(u => u.Health > 0 && u.Class == technology.Class).ToArray();
        return new(units.Length == 0 ? "Pivot · no current compatible soldiers" : MemberTargets(units),
            available ? UnitChanges(state, city, units, after, city.Run) + "\nMaximum HP increases do not heal living members."
                : owned ? "Already acquired; current soldier profiles include this technology." : "Locked: no purchase preview. Future compatible recruits also benefit when acquired.", units.Length > 0);
    }
    private static string UnitChanges(MatchSnapshot state, CityState city, UnitState[] units, ResearchState research, RunState run)
    {
        var calculators = For(state);
        return string.Join("\n", units.GroupBy(u => (u.Type, u.Level, u.Rank, u.IsBoss)).Select(g =>
        {
            var u = g.First(); var a = u.Profile;
            var capabilities = RunEffects.Capabilities(calculators.Economy.Research.Capabilities(research, u.Type), run, u.Type);
            var b = calculators.Combat.Unit(u.Type, u.Rank, u.IsBoss, u.Level, capabilities);
            var changes = new List<string>();
            void Stat(string name, int x, int y, bool hp = false) { if (x != y) changes.Add($"{name} {(hp ? HealthPoints.Format(x) : x)} → {(hp ? HealthPoints.Format(y) : y)}"); }
            Stat("max HP", a.Health, b.Health, true); Stat("primary", a.Damage, b.Damage, true); Stat("secondary", a.SecondaryDamage, b.SecondaryDamage, true); Stat("range", a.HexRange, b.HexRange);
            Stat("reduction %", u.Capabilities.ReductionPercent, capabilities.ReductionPercent);
            Stat("Burn %", u.Capabilities.BurnPercent, capabilities.BurnPercent); Stat("Poison %", u.Capabilities.PoisonPercent, capabilities.PoisonPercent); Stat("Chill %", u.Capabilities.ChillPercent, capabilities.ChillPercent);
            string homes = string.Join(", ", g.Select(member => member.Assignment is { Stored: true } ? "storage"
                : member.Assignment is { } assignment ? $"H{assignment.Tile + 1}" : "unassigned").Distinct().Order(StringComparer.Ordinal));
            return $"{g.Count()} {u.Type} L{u.Level} ({homes}): " + (changes.Count > 0 ? string.Join("; ", changes) : "no numerical change at current rounding/caps");
        }));
    }
}
