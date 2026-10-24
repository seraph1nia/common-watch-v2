// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
namespace Game.Shared.Reviews;
public static class DecisionReview
{
    private static void Save(string file,object value)=>File.WriteAllText(file,JsonSerializer.Serialize(value,WireJson.Options));
    public static int Run(string[] args)
    {
        if(args[0]=="legacy-selected")
        {
            var runs=(from family in new[]{"frontline","ranged","mixed","towers","research"}
                from seed in new[]{1UL,20261018UL} from adaptive in new[]{false,true}
                select CampaignReview.Run(family,seed,adaptive)).ToArray();
            Save(args[1],runs);Console.WriteLine($"LEGACY_SELECTED_PASS runs={runs.Length} battles={runs.Sum(r=>r.Waves.Length)} victories={runs.Count(r=>r.Waves[^1].Result=="Victory")}");return 0;
        }
        if(args[0]=="archive-shipping") return ShippingArchive.Run(args[1]);
        if(args[0]=="verify-matrix") return ShippingArchive.VerifyMatrix(args[1],args.Contains("--shipping"));
        if(args[0]=="verify-cooperation") return ShippingArchive.VerifyCooperation(args[1]);
        if(args[0]=="marginal-coop") return CooperativeCalibration.Run(args[1]);
        if(args[0]=="campaign-matrix") return CalibrationMatrix.Run(args[1]);
        if(args[0]=="checkpoint")
        {
            using var solo=MajorEncounterComparison.CreateAt(int.Parse(args[1]),bool.Parse(args[2]));Save(args[3],solo.Snapshot);return 0;
        }
        if(args[0]=="verify"){Console.WriteLine(JsonSerializer.Serialize(LegalRecorder.Verify(args[1]),WireJson.Options));return 0;}
        if(args[0]=="majors-heavy") return MajorReview.Run(args[1],heavy:true);
        if(args[0]=="majors") return MajorReview.Run(args[1]);
        if(args[0]=="majors20") return MajorReview.Run(args[1],20);
        if(args[0]=="budgets-heavy"){Console.WriteLine(JsonSerializer.Serialize(DecisionCases.Budgets(true),WireJson.Options));return 0;}
        if(args[0]=="budgets"){Console.WriteLine(JsonSerializer.Serialize(DecisionCases.Budgets(),WireJson.Options));return 0;}
        if(args[0]=="cases")
        {
            string output=Path.GetFullPath(args[1]);Directory.CreateDirectory(output);var summaries=new List<object>();int failures=0;
            foreach(string scenario in new[]{"placement","budget","home","investment","grove-space"})
            foreach(ulong seed in DecisionCases.Seeds)
            foreach(string variant in scenario switch {"placement"=>new[]{"protected","exposed"},"budget"=>["frontline","damage"],"home"=>["soldiers","tower"],"investment"=>["recruit-now","producer","trainer"],_=>["metal","stone"]})
            {
                string directory=Path.Combine(output,$"{scenario}-{seed}-{variant}");
                try
                {
                    object result=Case(directory,scenario,variant,seed);summaries.Add(result);Save(Path.Combine(directory,"summary.json"),result);
                    Console.WriteLine($"DECISION_CASE_PASS {scenario}/{seed}/{variant}");
                }
                catch(Exception e){failures++;var failure=new{scenario,seed,variant,error=e.Message};summaries.Add(failure);Console.WriteLine(JsonSerializer.Serialize(failure));}
            }
            Save(Path.Combine(output,"summary.json"),summaries);return failures==0?0:1;
        }
        if(args[0]=="inspect")
        {
            foreach(string family in new[]{"frontline","mixed","towers","research"})foreach(int wave in new[]{2,4,8,10,20})
            {
                try{using var run=new ReviewSession(1,$"consequence-inspect-{family}");run.Reach(wave,family,true);
                    Console.WriteLine(JsonSerializer.Serialize(new{family,wave,run.City.Resources,run.City.Research,run.City.Slots,Army=run.City.Army,
                        Units=run.City.Soldiers.Select(u=>new{u.Id,u.Type,u.Level,u.Health,u.SquadId,u.Assignment}),run.City.RecruitmentQuotes,
                        Buildings=run.State.BuildingCatalog.Select(b=>new{b.Type,b.Construction})},WireJson.Options));}
                catch(Exception e){Console.WriteLine(family+"/"+wave+": "+e.Message);}
            }return 0;
        }
        throw new ArgumentException("Unknown consequence review operation.");
    }
    private static object Case(string directory,string scenario,string variant,ulong seed)
    {
        int wave=scenario is "investment"?3:scenario=="grove-space"?2:8;
        string family=scenario is "placement" or "budget"?"mixed":"frontline";
        object result;
        using(var run=new ReviewSession(seed,$"decision-{scenario}-{seed}",scenario=="grove-space"?"grovekeeper":"warden",directory:directory))
        {
            run.Reach(wave,family,prepareFinal:scenario is not ("investment" or "grove-space"));
            if(scenario is "placement" or "budget" or "home")DecisionCases.FreeObsoleteHome(run);
            var common=run.State;DecisionCases.Choose(run,scenario,variant);var chosen=run.State;
            var battles=new List<BattleObservation>();
            int end=wave+(scenario is "investment" or "grove-space" or "home"?2:0);
            while(run.State.Wave<=end && run.State.Phase is not(Phase.Defeat or Phase.Victory))
            {
                if(run.State.Phase==Phase.RewardDraft)run.Send("reward:"+GameplayScenarios.RewardChoice(run.City.Run));
                if(run.State.Wave>end)break;
                if(scenario=="grove-space")run.Prepare(family);
                else if(scenario=="investment")
                {
                    // Same continuation: spend newly earned Metal on current-tier members,
                    // capped at the initial purchased field space; no automatic replacement.
                    if(!(run.State.Wave==wave&&run.State.Turn==1))DecisionCases.RecruitBudget(run,UnitType.Swordsman,run.City.Metal);
                }
                if(run.State.Turn==run.State.Rules.PreparationStages)Save(Path.Combine(directory,$"forecast-{run.State.Wave}.json"),run.State);
                run.Send("ready");if(run.State.Phase==Phase.Combat)battles.Add(run.Fight());
            }
            run.Finish();result=new{scenario,seed,variant,matched=seed is 1 or 20261018,common,chosen,battles,final=run.State};
        }
        var replay=LegalRecorder.Verify(directory);Save(Path.Combine(directory,"replay.json"),replay);return result;
    }
}
