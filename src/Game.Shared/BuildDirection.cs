// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.Shared;

/// <summary>Factual inventory and existing strategy relationships, never a strength score.</summary>
public static class BuildDirection
{
    public static string Summary(CityState city) => RunCatalog.Doctrine(city.Run.Doctrine).Name + " · "
        + (city.Soldiers.Length == 0 ? "no soldiers" : string.Join(" / ", city.Soldiers.GroupBy(u => u.Type).Select(g => $"{g.Count()} {g.Key}")))
        + $" · {city.Research.Technologies.Length} technologies · {city.Run.Rewards.Length} consumed rewards";
    public static string Inspection(CityState city) => Summary(city) + "\n"
        + "Research choices: " + (city.Research.Technologies.Length == 0 ? "none yet" : string.Join(", ", city.Research.Technologies.Select(TechnologyIds.Name)))
        + "\n\nExisting directions · combine or pivot freely\n"
        + "Fortified defense: Warden Watchpost health and Guardian research protect the army. Tower homes replace soldier space.\n\n"
        + "Protected ranged: Huntsman trades Crossbow health for reach. Screen ranged homes with melee; Worldtree fletching and poison specialize existing recruits.\n\n"
        + "Magic specialization: Arcanist Mage secondary and deliberate Fire/Frost research. Secondary damage stays within the engaged squad.\n\n"
        + "Economic investment: Grovekeeper producers trade against recruitment surcharges; Merchant rewards the first sale each wave. Worldtree production remains a deliberate research purchase.\n\n"
        + "Consumed battle rewards (history only): " + (city.Run.Rewards.Length == 0 ? "none yet" : string.Join("; ", city.Run.Rewards.Select(RunPresentation.Receipt)));
}
