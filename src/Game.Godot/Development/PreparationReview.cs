// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
namespace Game.GodotClient;
public partial class GameApplication
{
    private string ReviewSoloMatchId => _args.Contains("--journey-test") && Argument("--review-match-id") is { } match ? match : Guid.NewGuid().ToString("N");
    private string ReviewOpeningOrder => Argument("--review-opening-order") ?? "canonical";
    private Rules ReviewPreparationRules => new() { PreparationStages = Argument("--review-preparations") is { } value ? int.Parse(value) : new Rules().PreparationStages };
}
