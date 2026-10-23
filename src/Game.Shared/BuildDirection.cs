// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

/// <summary>Factual inventory and existing strategy relationships, never a strength score.</summary>
public static class BuildDirection
{
    public static string Summary(CityState city) => RunCatalog.Doctrine(city.Run.Doctrine).Name + " · "
        + (city.Soldiers.Length == 0 ? "no soldiers" : string.Join(" / ", city.Soldiers.GroupBy(u => u.Type).Select(g => $"{g.Count()} {g.Key}")))
        + $" · {city.Research.Technologies.Length} technologies · {city.Run.Cards.Sum(c => c.Count)} rewards";
    public static string Inspection(CityState city) => Summary(city) + "\n"
        + "Research choices: " + (city.Research.Technologies.Length == 0 ? "none yet" : string.Join(", ", city.Research.Technologies.Select(TechnologyIds.Name)))
        + "\n\nExisting directions · combine or pivot freely\n"
        + "Fortified defense: Warden Watchpost health, Guardian reduction, tower cards and Guardian Roots. Keep protection near the Watchpost; tower homes replace soldier space.\n\n"
        + "Protected ranged: Huntsman reach trades Crossbow health for range. Screen with melee homes; Precision, fletching and poison/Quiver cards change the ranged investment.\n\n"
        + "Status combinations: Arcanist Mage secondary, Fire/Frost or Venom, then Conduit against already afflicted squads. Secondary damage stays within the engaged squad; Warhorn changes the first committed attack.\n\n"
        + "Economic investment: Grovekeeper producers trade against recruitment surcharges; Merchant rewards the first sale each wave. Production cards stack with Worldtree production; Codex needs two later clears per research point.\n\n"
        + "Current rewards: " + (city.Run.Cards.Length == 0 ? "none yet" : string.Join(", ", city.Run.Cards.Select(c => RunCatalog.Card(c.Id).Name + " ×" + c.Count)));
}
