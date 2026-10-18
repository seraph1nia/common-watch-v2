using Game.Core;

namespace Game.Shared;

/// <summary>Player intent only. Identity and all concurrency expectations come from accepted state.</summary>
public sealed record GameplayIntent(GameplayAction Action, int Slot = -1, Building Building = Building.Empty,
    UnitType SoldierType = UnitType.Swordsman, TechnologyId Technology = TechnologyId.None,
    Resource Resource = Resource.Wood, int Bundles = 0, ConstructionPayment Payment = ConstructionPayment.Standard,
    int UnitId = 0, int SquadId = 0, int Cell = 0)
{
    public Command Capture(MatchSnapshot state, long sequence, int localPlayer)
        => Command.FromSnapshot(state, sequence, Name(Action), localPlayer, Slot, Building, SoldierType,
            Technology, Resource, Bundles, Payment, UnitId, SquadId, Cell);
    public static string Name(GameplayAction action) => action switch
    {
        GameplayAction.BuyPlot => "buy-plot", GameplayAction.BuyHome => "buy-home",
        GameplayAction.Research => "research-tech", GameplayAction.Deploy => "deploy-squad",
        GameplayAction.Regroup => "regroup-member", GameplayAction.Capacity => "upgrade-capacity",
        GameplayAction.Healing => "upgrade-healing", GameplayAction.TowerBranch => "upgrade-tower",
        _ => action.ToString().ToLowerInvariant()
    };
}
public enum GameplayAction { Build, BuyPlot, Upgrade, Sell, Trade, Research, Recruit, BuyHome,
    Deploy, Regroup, Store, Send, Retire, Capacity, Healing, TowerBranch, Start, Ready, Unready, Pause, Resume }
