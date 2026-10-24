// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using System.Text.Json;
namespace Game.Shared.Reviews;
public static class MajorReview
{
    public static int Run(string output, int? onlyMajor = null, bool heavy = false)
    {
        output=Path.GetFullPath(output);Directory.CreateDirectory(output);var summaries=new List<object>();int failures=0;
        foreach(int major in new[]{10,20}.Where(n=>onlyMajor is null || n==onlyMajor))foreach(ulong seed in DecisionCases.Seeds)
        foreach(string variant in heavy ? (major==20 ? new[]{"elite","baseline-frost","elite-frost"} : new[]{"elite","elite-assault","elite-frost","elite-venom"}) : major==20?new[]{"baseline","elite"}:new[]{"baseline","elite","baseline-assault","elite-assault","baseline-guardian","elite-guardian","baseline-precision","elite-precision","baseline-venom","elite-venom","baseline-fire","elite-fire","baseline-frost","elite-frost"})
        {
            string directory=Path.Combine(output,$"w{major}-{seed}-{variant}");
            try
            {
                object summary;
                using(var run=new ReviewSession(seed,$"major-review-{major}-{seed}",rules:variant.StartsWith("elite",StringComparison.Ordinal)?MajorEncounterComparison.FormationRules(major,heavy):MajorEncounterComparison.SingleBossRules(),directory:directory))
                {
                    run.Reach(major,"mixed",true);var before=run.State;DecisionCases.Choose(run,"major",variant);var chosen=run.State;
                    run.Send("ready");var battle=run.Fight();run.Finish();
                    if(major==20&&(run.State.Phase!=Phase.Victory||run.City.Run.DraftWave!=19||run.City.Run.Choice is null||run.City.Run.Rewards.Length!=19))throw new InvalidOperationException("Final encounter must reach Victory without a new draft.");
                    summary=new{major,seed,variant,heavy,matched=seed is 1 or 20261018,before,chosen,battle,final=run.State};
                }
                var replay=LegalRecorder.Verify(directory);File.WriteAllText(Path.Combine(directory,"summary.json"),JsonSerializer.Serialize(summary,WireJson.Options));summaries.Add(summary);
                Console.WriteLine($"MAJOR_COMPARISON_PASS W{major}/{seed}/{variant}");
            }
            catch(Exception e){failures++;var failure=new{major,seed,variant,error=e.Message};summaries.Add(failure);Console.WriteLine(JsonSerializer.Serialize(failure));}
        }
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(summaries,WireJson.Options));return failures==0?0:1;
    }
}
