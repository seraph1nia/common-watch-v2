// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
namespace Game.Shared.Reviews;
public static class DecisionCases
{
    public static readonly ulong[] Seeds = [1,20261018,42,98765];
    public static Rules EliteRules(int major) => MajorEncounterComparison.EliteRules(major);
    public static object Budgets(bool heavy = false)
    {
        var rules = MajorEncounterComparison.SingleBossRules(); var combat = new CombatConfiguration(rules);
        return new[]{10,20}.Select(n=>new {Wave=n, Baseline=rules.Campaign.Waves[n-1], Elite=MajorEncounterComparison.FormationRules(n,heavy).Campaign.Waves[n-1],
            BaselinePower=WavePower.Total(rules.Campaign.Waves[n-1],combat), ElitePower=WavePower.Total(MajorEncounterComparison.FormationRules(n,heavy).Campaign.Waves[n-1],combat),
            BaselineProfiles=rules.Campaign.Waves[n-1].Entries.Select(e=>combat.Unit(e.Type,e.Rank,e.IsBoss,e.Level,faction:Faction.Skeletons)),
            EliteProfiles=MajorEncounterComparison.FormationRules(n,heavy).Campaign.Waves[n-1].Entries.Select(e=>combat.Unit(e.Type,e.Rank,e.IsBoss,e.Level,faction:Faction.Skeletons))}).ToArray();
    }
    public static void FreeObsoleteHome(ReviewSession run)
    {
        // Common paid-history decision for both arms: retire the same old front squad,
        // without refund. Thereafter placement arms have identical IDs/profiles/health.
        var group=run.City.Soldiers.Where(u=>u.Assignment is {Stored:false,Tile:<3}).GroupBy(u=>u.SquadId)
            .OrderBy(g=>g.Max(u=>u.Level)).ThenBy(g=>g.Key).First();
        foreach(var u in group.ToArray())run.Act(new(GameplayAction.Retire,UnitId:u.Id));
    }
    public static void Choose(ReviewSession run,string scenario,string variant)
    {
        var city=run.City; var state=run.State;
        switch(scenario)
        {
            case "placement":
                if(variant=="exposed")
                {
                    var support=city.Soldiers.First(u=>u.Type==UnitType.Mage && u.Assignment is {Stored:false});
                    int home=city.Army!.Homes.First(h=>h.Purchased&&h.Cell<3&&h.Used==0).Cell;
                    int cell=state.Arena!.Sectors.Single(s=>s.Owner==run.Player).DeploymentCells[home];
                    run.Act(new(GameplayAction.Deploy,SquadId:support.SquadId,Cell:cell));
                }
                break;
            case "budget":
                RecruitBudget(run,variant=="frontline"?UnitType.Swordsman:UnitType.Berserker,15);break;
            case "home":
                if(variant=="tower")
                {
                    int home=city.Army!.Homes.First(h=>h.Purchased&&h.Used==0).PlotId;
                    run.Act(new(GameplayAction.Build,home,Building.ArrowTower));
                }
                else RecruitBudget(run,UnitType.Swordsman,15);
                break;
            case "investment":
                if(variant=="recruit-now")RecruitBudget(run,UnitType.Swordsman,city.Metal);
                else run.Act(new(GameplayAction.Upgrade,variant=="trainer"?2:1));
                break;
            case "grove-space":
                if(variant=="stone") {run.Act(new(GameplayAction.Sell,4));run.Act(new(GameplayAction.Build,4,Building.Stonecutter));}
                break;
            case "major":
                var ids=variant.Split('-');
                if(ids.Length>1)
                {
                    var foundation=ids[1] is "fire" or "frost"?TechnologyId.MagicFoundation:ids[1] is "venom" or "precision"?TechnologyId.RangedFoundation:TechnologyId.MeleeFoundation;
                    run.Act(new(GameplayAction.Research,Technology:foundation));
                    run.Act(new(GameplayAction.Research,Technology:Enum.Parse<TechnologyId>(ids[1],true)));
                }
                break;
            default:throw new ArgumentException(scenario);
        }
    }
    public static void RecruitBudget(ReviewSession run,UnitType type,int metalBudget)
    {
        var city=run.City; int trainer=Array.FindIndex(city.Slots,s=>run.State.BuildingCatalog.SingleOrDefault(b=>b.Type==s.Type)?.Recruits?.Contains(type)==true);
        var quote=city.RecruitmentQuotes.Single(q=>q.Type==type&&q.Level==city.Slots[trainer].Level);
        int spent=0;
        for(int n=0;n<3 && spent+quote.Cost.Metal<=metalBudget && run.City.Resources.TryPay(quote.Cost,out _);n++)
        {
            var army=new ArmyConfiguration(run.State.Rules.Army,new HexBoard(run.State.Arena!.Board),run.City.Army!.SquadCapacity);
            if(army.Find(run.City.Soldiers,run.City.Army.PurchasedHomes,plots:run.City.Army.Homes.Select(h=>h.Plot).ToArray(),type:type) is null)break;
            run.Act(new(GameplayAction.Recruit,trainer,SoldierType:type));spent+=quote.Cost.Metal;
        }
    }
}
