// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
namespace Game.Shared;

public sealed record CampaignCalibrationPlan(bool ConvertGrove = false, int MaximumHomes = 6, int MaximumTier = 5);

/// <summary>Explicit development policy choices, never player automation or a second economy.</summary>
public static class CampaignCalibration
{
    public static GameplayIntent? Next(MatchSnapshot state, int player, string family, CampaignCalibrationPlan plan)
    {
        var city = state.Players.Single(p => p.Id == player);
        if (state.Phase is not (Phase.Building or Phase.Preparation) || state.Paused || city.Ready || city.Eliminated || !city.Connected) return null;
        if (state.Wave == 1) return OpeningCampaign.Next(state, player);
        if (OpeningCampaign.Transition(state, player, "normal-wood") is { } transition) return transition;
        // A normal melee opening needs an explicit paid trainer conversion before
        // a ranged continuation. Never label unchanged Barracks recruits as bows.
        if (family == "ranged" && city.Slots[2].Type == Building.Barracks
            && city.Resources.TryPay(state.BuildingCatalog.Single(b => b.Type == Building.ArcheryRange).Construction, out _))
            return new(GameplayAction.Sell, 2);
        if (family == "ranged" && city.Slots[2].Type == Building.Empty)
            return city.Resources.TryPay(state.BuildingCatalog.Single(b => b.Type == Building.ArcheryRange).Construction, out _)
                ? new(GameplayAction.Build, 2, Building.ArcheryRange) : null;
        if (plan.ConvertGrove && city.Run.Doctrine == "grovekeeper" && city.Slots[4].Type == Building.MetalMine
            && city.Resources.TryPay(state.BuildingCatalog.Single(b => b.Type == Building.Stonecutter).Construction, out _))
            return new(GameplayAction.Sell, 4);
        var action = GameplayCampaignPolicy.Next(state, player, family, maximumHomes: plan.MaximumHomes, maximumTier: plan.MaximumTier);
        if (action is null) return null;
        var kind = Enum.GetValues<GameplayAction>().First(a => GameplayIntent.Name(a) == action.Action);
        return new(kind, action.Slot, action.Building, action.Unit, action.Technology, action.Resource, action.Bundles, action.Payment, action.UnitId);
    }
}
