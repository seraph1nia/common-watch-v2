// SPDX-License-Identifier: GPL-3.0-only
namespace Game.Core;

/// <summary>Actual Ready boundaries and their declared resource delivery. Research/healing occur once per boundary.</summary>
public static class PreparationSchedule
{
    public static void Validate(Rules rules)
    {
        if (rules.PreparationStages is not (2 or 3)) throw new ArgumentException("Preparation schedule must contain two or three decisions.", nameof(rules));
    }
    public static int ResourceMultiplier(Rules rules, int turn)
    {
        Validate(rules);
        if (turn < 1 || turn > rules.PreparationStages) throw new ArgumentOutOfRangeException(nameof(turn));
        return rules.PreparationStages == 2 && turn == 1 ? 2 : 1;
    }
    public static ResourceCost? Delivery(Rules rules, int turn, ResourceCost? production)
        => production is { } income && income.TryMultiply(ResourceMultiplier(rules, turn), out ResourceCost delivered) ? delivered : null;
    public static bool ValidPhase(Rules? rules, Phase phase, int turn)
        => rules is not null && rules.PreparationStages is 2 or 3 && turn >= 1 && turn <= rules.PreparationStages
            && (phase != Phase.Building || turn < rules.PreparationStages)
            && (phase != Phase.Preparation || turn == rules.PreparationStages);
}
