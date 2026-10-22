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
    public static ChoiceImpact Reward(MatchSnapshot state, CityState city, string id)
    {
        RewardCard card = RunCatalog.Card(id);
        if (city.Run.Stacks(id) >= card.MaxStacks) return new("Stack cap reached", "No additional stack is available.", false);
        RunState after = RunEffects.Acquire(city.Run, id);
        var calculators = For(state);
        if (RunEffects.ProducerCard(BuildingFor(id)) is not null)
        {
            Building building = BuildingFor(id);
            var definition = state.BuildingCatalog.Single(d => d.Type == building);
            var slots = city.Slots.Select((s, i) => (s, i)).Where(p => p.s.Type == building).ToArray();
            string details = string.Join("\n", slots.Select(p => $"Plot {p.i + 1}: {Output(p.s.Level, city.Run)} → {Output(p.s.Level, after)} {definition.Produces}/production"));
            int Output(int level, RunState run) => EconomyConfiguration.ProducerOutput(definition, level, city.Research, state.TechnologyCatalog) + RunCatalog.Amount(run, id);
            return Target(slots.Length, building.ToString(), details, "Build a " + building + " to use this production bonus.");
        }
        if (card.Multiplied)
        {
            if (!card.Resources.TryMultiply(RunEffects.Multiplier(city.Run.DraftWave), out var gain)) throw new OverflowException();
            return new("Immediate stock · no producer required", "+" + RunPresentation.Resources(gain) + ". No extra production.", true);
        }
        if (id == "F05") return new("Personal Worldtree points", $"Research {city.Research.Points} → {city.Research.Income(points: card.Amount).Points}; no progress or production tick.", true);
        if (id == "F04")
        {
            int wounded = city.Soldiers.Count(u => u.Health > 0 && u.Health < u.Profile.Health);
            int keep = Math.Min(HealthPoints.FromWhole(10), HealthPoints.FromWhole(state.Rules.CityHealth) - city.Health);
            return new($"Keep +{HealthPoints.Format(keep)} HP · {wounded} wounded soldiers", "Living members, including storage/reserves, recover 20% of missing HP. No resurrection.", keep > 0 || wounded > 0);
        }
        if (id == "T01" || id == "R06")
        {
            var watchpost = BattleDeployment.Structures(state).Single(s => s.Owner == city.Id && s.Kind == ArenaStructureKind.Watchpost);
            return new("Your Watchpost / Keep", id == "T01" ? $"Next-battle Watchpost maximum HP {HealthPoints.Format(watchpost.MaximumHealth)} → {HealthPoints.Format(RunEffects.Watchpost(HealthPoints.FromWhole(state.Rules.Arena.WatchpostHealth), after))}. Does not repair this battle."
                : "Keep structural damage reduced only while the actual Watchpost is alive; deploy protection around it.", true);
        }
        if (id.StartsWith('T'))
        {
            bool catapult = id is "T03" or "T06";
            var towers = city.Slots.Select((plot, i) => (Plot: plot, Label: $"Plot {i + 1}"))
                .Concat(city.Army!.Homes.Select((h, i) => (Plot: h.Plot, Label: $"Home {i + 1}")))
                .Where(p => catapult ? p.Plot.Type is Building.CatapultTower or Building.BombardTower : p.Plot.Type is Building.ArrowTower or Building.RapidArrowTower).ToArray();
            return Target(towers.Length, catapult ? "catapult towers" : "arrow towers", string.Join("\n", towers.Select(p =>
            {
                var basis = state.TowerCatalog.Single(t => t.Type == p.Plot.Type && t.Level == p.Plot.Level);
                var a = RunEffects.Tower(basis, city.Run); var b = RunEffects.Tower(basis, after);
                return $"{p.Label}: damage {HealthPoints.Format(a.Damage)}/{HealthPoints.Format(a.SecondaryDamage)} → {HealthPoints.Format(b.Damage)}/{HealthPoints.Format(b.SecondaryDamage)}; range {a.HexRange} → {b.HexRange}; cadence {a.CadenceTicks} → {b.CadenceTicks} ticks";
            })), "Purchase a compatible tower to use this bonus.");
        }
        if (id == "R05") return new("Delayed personal research", "Next +1 point after two successful clears; acquisition clear excluded. Does not require a Research Tower.", true);
        var members = city.Soldiers.Where(u => u.Health > 0 && Affects(id, u.Type)).ToArray();
        string targets = MemberTargets(members);
        if (members.Length == 0) return new("Pivot · no current compatible soldiers", "Future compatible recruits benefit. " + card.Effect, false);
        if (id == "M06")
        {
            bool source = state.Players.SelectMany(p => p.Soldiers).Any(u => u.Health > 0 && (u.Capabilities.BurnPercent > 0 || u.Capabilities.PoisonPercent > 0 || u.Capabilities.ChillPercent > 0));
            return new(targets + (source ? " · conditional" : " · needs status source"), card.Effect
                + " A status must already be present before impact; this does not buff every hit. "
                + (source ? "An allied current soldier can apply a status." : "No status source in current allied soldiers; acquire one to enable this combination."), source);
        }
        if (id is "R01" or "R03") return new(targets, card.Effect + " No guaranteed damage or yield forecast.", true);
        if (id == "A06") return new(targets, string.Join("\n", members.GroupBy(u => u.Type).Select(g => $"{g.Key}: {calculators.Economy.Upkeep(g.Key, city.Run)} → {calculators.Economy.Upkeep(g.Key, after)} Food per participating member")) + "\nStored/unfunded members do not pay this battle's upkeep.", true);
        return new(targets, UnitChanges(state, city, members, city.Research, after) + "\nMaximum HP increases do not heal living members.", true);
    }
    private static Building BuildingFor(string id) => id switch { "E01" => Building.Lumbermill, "E02" => Building.Farm, "E03" => Building.Mine, "E04" => Building.Stonecutter, "E05" => Building.MetalMine, "E06" => Building.Weaver, _ => Building.Empty };
    private static bool Affects(string id, UnitType type) => id switch
    {
        "A01" => type == UnitType.Swordsman, "A02" => type == UnitType.Berserker,
        "A03" or "M05" or "R02" => type == UnitType.Crossbowman,
        "A04" or "M01" or "M02" or "M03" or "M04" or "R04" => type == UnitType.Mage,
        "A06" => type is UnitType.Berserker or UnitType.Mage,
        _ => true
    };
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
                + $" {benefit.Resource}/production before reward/doctrine bonuses")), "Future " + benefit.Building + " producers benefit; inspect the prerequisites before investing.");
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
