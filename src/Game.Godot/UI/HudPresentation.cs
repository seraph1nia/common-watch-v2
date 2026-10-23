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

    public static HudPresentation Project(MatchSnapshot state, int localPlayer, bool finishingCombat, bool previewArena)
    {
        var local = state.Players.Single(p => p.Id == localPlayer);
        bool combat = state.Phase == Phase.Combat || finishingCombat;
        var moment = combat ? local.Eliminated ? HudMoment.Observer : HudMoment.Combat
            : state.Phase is Phase.Victory or Phase.Defeat ? HudMoment.Terminal
            : local.Eliminated ? HudMoment.Observer
            : state.Phase == Phase.RewardDraft ? local.Run.Choice is null ? HudMoment.ChoosingReward : HudMoment.RewardWaiting
            : local.Ready ? HudMoment.ReadyWaiting : HudMoment.Planning;
        return new(moment, combat || previewArena, combat);
    }
}
