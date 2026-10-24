// SPDX-License-Identifier: GPL-3.0-only
using System.Text.Json;
using Game.Core;
namespace Game.Shared.Reviews;
public static class ShippingArchive
{
    public static int Run(string output)
    {
        Directory.CreateDirectory(output);var summaries=new List<object>();
        foreach(string family in new[]{"frontline","ranged","mixed","towers","research"})foreach(ulong seed in new[]{1UL,20261018UL})
        {
            string directory=Path.Combine(output,$"{family}-{seed}");object summary;
            using(var run=new ReviewSession(seed,$"shipping-baseline-{family}-{seed}",directory:directory))
            {
                var fights=new List<BattleObservation>();
                for(int boundary=0;run.State.Phase is not(Phase.Victory or Phase.Defeat);boundary++)
                {
                    if(boundary>=65)throw new InvalidOperationException("Shipping baseline boundary bound exceeded.");
                    if(run.State.Phase==Phase.RewardDraft)run.Send("reward:"+GameplayScenarios.RewardChoice(run.City.Run));
                    run.Prepare(family,new());run.Send("ready");if(run.State.Phase==Phase.Combat)fights.Add(run.Fight());
                }
                run.Finish();summary=new{family,seed,battles=fights,final=run.State};
            }
            LegalRecorder.Verify(directory);File.WriteAllText(Path.Combine(directory,"summary.json"),JsonSerializer.Serialize(summary,WireJson.Options));summaries.Add(summary);
            Console.WriteLine($"SHIPPING_BASELINE_REPLAY_PASS {family}/{seed}");
        }
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(summaries,WireJson.Options));return 0;
    }
    public static int VerifyMatrix(string directory,bool shipping)
    {
        var files=Directory.EnumerateFiles(directory,"*.json").Where(f=>Path.GetFileName(f).StartsWith("elite-",StringComparison.Ordinal)
            ||!shipping&&Path.GetFileName(f).StartsWith("boss-",StringComparison.Ordinal)).Order().ToArray();
        int expected=RunCatalog.Doctrines.Count()*5*DecisionCases.Seeds.Length*(shipping?1:2);
        if(files.Length!=expected)throw new InvalidOperationException($"Expected{expected} complete matrix rows, found{files.Length}.");
        foreach(string file in files)
        {
            var run=JsonSerializer.Deserialize<RewardEconomyRun>(File.ReadAllText(file),WireJson.Options)!;
            CalibrationMatrix.Verify(run,useDefault:shipping);
        }
        Console.WriteLine($"SHIPPING_MATRIX_REPLAY_PASS rows={files.Length} currentDefault={shipping} exact-commands-final-state entry-cells profiles food");return 0;
    }
    public static int VerifyCooperation(string directory)
    {
        var folders=Directory.EnumerateDirectories(directory,"coop-*").Order().ToArray();
        if(folders.Length!=36)throw new InvalidOperationException("Expected36 cooperative recorded campaigns.");
        foreach(string folder in folders)LegalRecorder.Verify(folder);
        var files=Directory.EnumerateFiles(directory,"marginal-*.json").ToArray();
        if(files.Length!=12)throw new InvalidOperationException("Expected12 marginal campaigns.");
        foreach(string file in files)CalibrationMatrix.Verify(JsonSerializer.Deserialize<RewardEconomyRun>(File.ReadAllText(file),WireJson.Options)!,useDefault:true);
        Console.WriteLine("COOPERATIVE_INTEGRITY_PASS 36groups 12marginal exact-entry-cells profiles funding outcome grants");return 0;
    }
}
