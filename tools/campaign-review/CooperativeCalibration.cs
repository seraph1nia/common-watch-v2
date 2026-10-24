// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using System.Text.Json;
namespace Game.Shared.Reviews;

public static class CooperativeCalibration
{
    public static int Run(string output)
    {
        output=Path.GetFullPath(output);Directory.CreateDirectory(output);var results=new List<object>();
        foreach(var doctrine in RunCatalog.Doctrines)foreach(ulong seed in new[]{1UL,42UL})
        {
            var run=RewardEconomyReview.Run(doctrine.Id,"frontline",seed,recordCheckpoints:true,calibration:new(MaximumHomes:3,MaximumTier:2));
            CalibrationMatrix.Verify(run,useDefault:true);
            File.WriteAllText(Path.Combine(output,$"marginal-{doctrine.Id}-{seed}.json"),JsonSerializer.Serialize(run,WireJson.Options));
            results.Add(new{kind="marginal",doctrine=doctrine.Id,seed,last=run.Battles[^1]});
            Console.WriteLine($"MARGINAL_REPLAY_PASS {doctrine.Id}/{seed} wave={run.Battles[^1].Wave} result={run.Battles[^1].Result}");
        }
        foreach(int players in Enumerable.Range(1,6))foreach(ulong seed in new[]{1UL,42UL})foreach(string mode in new[]{"invested","weak","idle"})
        {
            string directory=Path.Combine(output,$"coop-{players}-{seed}-{mode}");
            object result=Group(directory,players,seed,mode);results.Add(result);
            File.WriteAllText(Path.Combine(directory,"summary.json"),JsonSerializer.Serialize(result,WireJson.Options));
            Console.WriteLine($"COOPERATIVE_REPLAY_PASS {players}p/{seed}/{mode}");
        }
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(results,WireJson.Options));return 0;
    }
    private static object Group(string directory,int count,ulong seed,string mode)
    {
        object result;
        using(var match=new Match(matchId:$"cooperative-calibration-{count}-{seed}",combatSeed:seed))
        {
            int[] players=Enumerable.Range(0,count).Select(_=>match.Join()!.Id).ToArray();
            var sequence=players.ToDictionary(p=>p,_=>0L);
            using var recorder=new LegalRecorder(directory,match.Snapshot());
            void Submit(int player,Command command)
            {
                long tick=match.Tick;var accepted=match.Apply(player,command);recorder.Command(tick,player,command,accepted,match.Snapshot());
                if(!accepted.Accepted)throw new InvalidOperationException(accepted.Message);
            }
            void Send(int p,string action)=>Submit(p,Command.FromSnapshot(match.Snapshot(),++sequence[p],action,p));
            void Act(int p,GameplayIntent intent)=>Submit(p,intent.Capture(match.Snapshot(),++sequence[p],p));
            foreach(var (p,index) in players.Select((p,i)=>(p,i)))Send(p,"doctrine:"+RunCatalog.Doctrines[index].Id);
            Send(players[0],"start");var battles=new List<object>();var eliminated=new Dictionary<int,int>();
            for(int boundary=0;match.Phase is not(Phase.Victory or Phase.Defeat);boundary++)
            {
                if(boundary>=80)throw new InvalidOperationException("Group boundary bound exceeded.");
                if(match.Phase==Phase.RewardDraft)
                    foreach(var c in match.Snapshot().Players.Where(c=>!c.Eliminated&&c.Run.Choice is null))Send(c.Id,"reward:"+GameplayScenarios.RewardChoice(c.Run));
                var forecasts=new Dictionary<int,BattleFoodForecast>();
                foreach(int p in players)
                {
                    var city=match.Snapshot().Players.Single(c=>c.Id==p);if(city.Eliminated)continue;
                    var plan=mode=="weak"&&p==players[^1]?new CampaignCalibrationPlan(MaximumHomes:3,MaximumTier:2):new();
                    string family=new[]{"frontline","mixed","towers","research","frontline","mixed"}[p-1];
                    if(!(mode=="idle"&&p==players[^1]))
                        for(int n=0;n<150;n++)
                        {
                            var intent=CampaignCalibration.Next(match.Snapshot(),p,family,plan);if(intent is null)break;Act(p,intent);
                            if(n==149)throw new InvalidOperationException("Group preparation bound exceeded.");
                        }
                    if(match.Snapshot().Turn==match.Rules.PreparationStages)forecasts[p]=match.Snapshot().Players.Single(c=>c.Id==p).FoodForecast!;
                    Send(p,"ready");
                }
                if(match.Phase!=Phase.Combat)continue;
                var before=match.Snapshot();recorder.BeginEncounter(before);long resolved=-1;
                foreach(var p in forecasts)
                {
                    var paid=before.Players.Single(c=>c.Id==p.Key).LastUpkeep!;
                    if(!p.Value.Participating.SequenceEqual(paid.Participating)||p.Value.Paid!=paid.Paid)throw new InvalidOperationException("Group funding forecast differs.");
                }
                for(int n=0;n<18000&&(match.Phase==Phase.Combat||match.Snapshot().DyingBodies.Length>0);n++)
                {match.Step();recorder.Observe(match.Snapshot());if(resolved<0&&match.Phase!=Phase.Combat)resolved=match.Tick;}
                var after=match.Snapshot();if(match.Phase==Phase.Combat||after.DyingBodies.Length>0)throw new InvalidOperationException("Group battle bound exceeded.");
                recorder.EndEncounter(after);
                foreach(var c in after.Players.Where(c=>c.Eliminated))eliminated.TryAdd(c.Id,before.Wave);
                battles.Add(new{wave=before.Wave,result=after.Phase,combatTicks=resolved-before.Tick,cleanupTicks=after.Tick-resolved,
                    originalPlayers=count,alive=after.Players.Count(c=>!c.Eliminated),before,after});
            }
            recorder.Complete(match.Snapshot());
            result=new{kind="cooperative",count,seed,mode,terminal=match.Phase,eliminated,battles,
                continuedAfterElimination=eliminated.Values.Any(w=>match.Wave>w),final=match.Snapshot()};
        }
        LegalRecorder.Verify(directory);return result;
    }
}
