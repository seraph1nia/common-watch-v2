// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
namespace Game.GodotClient;
public partial class GameApplication
{
    private string ReviewOpeningOrder => Argument("--review-opening-order") ?? "canonical";
    private Rules ReviewPreparationRules => new() { PreparationStages = Argument("--review-preparations") is { } value ? int.Parse(value) : 3 };
}
