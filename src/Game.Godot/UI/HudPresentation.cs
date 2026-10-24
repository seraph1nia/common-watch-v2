// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;

namespace Game.GodotClient;

public enum HudMoment { Planning, ReadyWaiting, Combat, ChoosingReward, RewardWaiting, Observer, Terminal }

/// <summary>Local presentation only. Authority and playback retain their existing phase boundaries.</summary>
public sealed record HudPresentation(HudMoment Moment, bool Arena, bool LiveCombat)
{
    public bool Ledger => Moment is HudMoment.Planning or HudMoment.ReadyWaiting;
    public bool Management => Moment == HudMoment.Planning && !Arena;
    public bool Threat => Ledger && !Arena;
    public bool PreparationTrack => Ledger;
    public bool Navigation => !Arena && Moment is HudMoment.Planning or HudMoment.ReadyWaiting or HudMoment.RewardWaiting;
    public bool CanChooseReward => Moment == HudMoment.ChoosingReward;

    public static int DisplayedWave(MatchSnapshot state, bool finishingCombat)
        => finishingCombat && state.Phase is not (Phase.Combat or Phase.Defeat) && state.LastRewardedWave > 0
            ? state.LastRewardedWave : state.Wave;

    public static string PhaseLabel(MatchSnapshot state, bool finishingCombat)
        => finishingCombat && state.Phase != Phase.Combat ? "Finishing combat"
            : state.Phase is Phase.Building or Phase.Preparation ? $"Preparation {state.Turn}/3" : state.Phase.ToString();

    public static HudPresentation Project(MatchSnapshot state, int localPlayer, bool finishingCombat, bool previewArena, bool acceptedChoiceAwaitingSnapshot = false)
    {
        var local = state.Players.Single(p => p.Id == localPlayer);
        bool combat = state.Phase == Phase.Combat || finishingCombat;
        var moment = combat ? local.Eliminated ? HudMoment.Observer : HudMoment.Combat
            : state.Phase is Phase.Victory or Phase.Defeat ? HudMoment.Terminal
            : local.Eliminated ? HudMoment.Observer
            : state.Phase == Phase.RewardDraft ? local.Run.Choice is null && !acceptedChoiceAwaitingSnapshot ? HudMoment.ChoosingReward : HudMoment.RewardWaiting
            : local.Ready ? HudMoment.ReadyWaiting : HudMoment.Planning;
        bool planning = moment is HudMoment.Planning or HudMoment.ReadyWaiting;
        return new(moment, combat || previewArena && planning, combat);
    }
}
