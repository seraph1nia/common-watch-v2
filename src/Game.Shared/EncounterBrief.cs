// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

public sealed record EncounterBrief(string Purpose, string Problem, string Decisions, string Composition, string Boss, string Schedule)
{
    public string Details => Purpose + "\n" + Problem + "\n" + Composition + (Boss.Length == 0 ? "" : "\n" + Boss) + "\nConsider: " + Decisions;
}

/// <summary>Names problems in the actual catalog/forecast. Advice is optional, never a command or a damage prediction.</summary>
public static class EncounterBriefing
{
    public static string Purpose(WaveDefinition wave, WaveDefinition? previous, bool final = false)
    {
        if (wave.IsBoss) return final ? "Final boss duel" : "Boss duel";
        if (previous?.IsBoss == true) return "Return to formations";
        int front = wave.Entries.Where(e => e.Type == UnitType.Swordsman).Select(e => e.Level).DefaultIfEmpty(0).Max();
        int oldFront = previous?.Entries.Where(e => e.Type == UnitType.Swordsman && !e.IsBoss).Select(e => e.Level).DefaultIfEmpty(0).Max() ?? 0;
        if (oldFront > 0 && front > oldFront) return "Heavier frontline";
        if (wave.Entries.Select(e => e.Type).Distinct().Count() == 4) return "Mixed-arms pressure";
        if (wave.Entries.Any(e => e.Type == UnitType.Mage)) return "Mage support";
        if (wave.Entries.Any(e => e.Type == UnitType.Berserker)) return wave.Entries.Any(e => e.Type == UnitType.Crossbowman) ? "Heavy hitters with ranged cover" : "Heavy melee hitter";
        return wave.Entries.Any(e => e.Type == UnitType.Crossbowman) ? "Ranged pressure" : "Hold the frontline";
    }
    public static EncounterBrief Project(MatchSnapshot state, int cityId)
    {
        var current = state.WaveCatalog.Single(w => w.Number == state.Wave);
        var previous = state.WaveCatalog.FirstOrDefault(w => w.Number == state.Wave - 1);
        var source = state.Phase == Phase.Combat ? state.Enemies : state.NextWave;
        var forecast = source.Where(u => u.Destination == cityId).ToArray();
        bool boss = forecast.Any(u => u.IsBoss);
        bool ranged = forecast.Any(u => u.Type is UnitType.Crossbowman or UnitType.Mage);
        bool mage = forecast.Any(u => u.Type == UnitType.Mage);
        bool heavy = forecast.Any(u => u.Type == UnitType.Berserker);
        string composition = string.Join(" · ", forecast.GroupBy(u => (u.Type, u.Level, u.IsBoss)).Select(g => $"{g.Count()} {g.Key.Type} L{g.Key.Level}{(g.Key.IsBoss ? " BOSS" : "")}"));
        string problem = forecast.Length == 0 ? "No incoming allocation for this city in the accepted forecast."
            : boss ? "A durable single-member foe tests sustained primary damage and frontline survival."
            : mage ? "Magic support adds secondary damage within an engaged squad."
            : heavy ? "Berserkers add primary/secondary pressure to the frontline."
            : ranged ? "Crossbowmen can attack from range; exposed support needs protection."
            : "Close-range soldiers test whether your funded frontline can hold.";
        string decisions = boss ? "Compare single-target primary damage and protection; secondary attacks have no other member to hit inside a boss squad."
            : ranged ? "Compare melee screens and rear homes for your support; review funding before adding recruits."
            : heavy ? "Compare protection, recruitment equipment and slowing effects you can actually acquire."
            : "Compare funded members, available home capacity and the next trainer quote.";
        string bosses = string.Join("\n", forecast.Where(u => u.IsBoss).Select(u => $"P{u.Origin} boss → P{u.Destination}: {HealthPoints.Format(u.Profile.Health)} HP · primary {HealthPoints.Format(u.Profile.Damage)} · range {u.Profile.HexRange}. No escorts in its allocation."));
        string schedule = string.Join("\n", state.WaveCatalog.Select(w => $"{(w.Number == state.Wave ? "→" : "")}W{w.Number}: {Purpose(w, state.WaveCatalog.FirstOrDefault(p => p.Number == w.Number - 1), w.Number == state.TotalWaves)} · "
            + string.Join(", ", w.Entries.Select(e => $"{e.Count} {e.Type} L{e.Level}")) + " · clear " + RunPresentation.Resources(w.Reward)));
        if (state.Phase is Phase.Victory or Phase.Defeat)
            return new(state.Phase == Phase.Victory ? "Campaign complete" : "Campaign ended", "No further battle follows this result.", "Review the last battle and your choices.", "", "", schedule);
        return new(Purpose(current, previous, state.Wave == state.TotalWaves), problem, decisions, composition, bosses, schedule);
    }
}
