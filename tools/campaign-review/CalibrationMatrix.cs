// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using System.Text.Json;
namespace Game.Shared.Reviews;
public static class CalibrationMatrix
{
    public static int Run(string output)
    {
        Directory.CreateDirectory(output);var receipts=new List<object>();
        // Same explicit policy in both campaigns. The Grove conversion remains
        // off here so old constrained-space failures cannot disappear silently.
        foreach(bool elite in new[]{false,true})foreach(var doctrine in RunCatalog.Doctrines)
        foreach(string family in new[]{"frontline","ranged","mixed","towers","research"})foreach(ulong seed in DecisionCases.Seeds)
        {
            var rules=elite?MajorEncounterComparison.FormationRules(10,true):MajorEncounterComparison.SingleBossRules();
            string name=$"{(elite?"elite":"boss")}-{doctrine.Id}-{family}-{seed}";
            string file=Path.Combine(output,name+".json");
            var result=File.Exists(file)?JsonSerializer.Deserialize<RewardEconomyRun>(File.ReadAllText(file),WireJson.Options)!
                :RewardEconomyReview.Run(doctrine.Id,family,seed,rules:rules,recordCheckpoints:true,calibration:new());
            Verify(result);
            File.WriteAllText(Path.Combine(output,name+".json"),JsonSerializer.Serialize(result,WireJson.Options));
            var last=result.Battles[^1];receipts.Add(new{elite,doctrine=doctrine.Id,family,seed,last.Wave,last.Result,battles=result.Battles.Length,lost=result.Battles.Sum(b=>b.Casualties),keepDamage=result.Battles.Sum(b=>b.KeepDamage)});
            Console.WriteLine($"CALIBRATION_REPLAY_PASS {name} wave={last.Wave} result={last.Result}");
        }
        File.WriteAllText(Path.Combine(output,"summary.json"),JsonSerializer.Serialize(receipts,WireJson.Options));return 0;
    }
    public static void Verify(RewardEconomyRun recorded, bool useDefault = false)
    {
        var joined=recorded.Initial??throw new InvalidOperationException("Missing initial rules/roster.");
        using var match=new Match(useDefault?null:joined.Rules,joined.MatchId,joined.CombatSeed);
        foreach(var p in joined.Players)if(match.Join()?.Id!=p.Id)throw new InvalidOperationException("Replay roster mismatch.");
        if(LegalRecorder.Hash(match.Snapshot())!=LegalRecorder.Hash(joined))throw new InvalidOperationException("Recorded initial rules/roster differs from selected authority.");
        foreach(var row in recorded.Commands)
        {
            for(int n=0;match.Tick<row.AtTick;n++)
            {
                if(n>=18000||match.Phase!=Phase.Combat&&match.Snapshot().DyingBodies.Length==0)throw new InvalidOperationException("Invalid replay tick boundary.");
                match.Step();
            }
            var before = match.Snapshot();
            if(match.Tick!=row.AtTick||match.Apply(row.Command.City,row.Command)!=row.Result||LegalRecorder.Hash(match.Snapshot())!=row.SnapshotSha256)
                throw new InvalidOperationException("Calibration command/snapshot replay differs.");
            EncounterIntegrity.RequireCommand(before,match.Snapshot(),row.Command);
        }
        for(int n=0;match.Tick<recorded.FinalTick;n++)
        {
            if(n>=18000||match.Phase!=Phase.Combat&&match.Snapshot().DyingBodies.Length==0)throw new InvalidOperationException("Invalid final replay boundary.");
            match.Step();
        }
        if(LegalRecorder.Hash(match.Snapshot())!=recorded.FinalSnapshotSha256)throw new InvalidOperationException("Calibration final replay differs.");
    }
}
