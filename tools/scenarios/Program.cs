using Game.Shared;

try
{
    if (args.FirstOrDefault() == "build-review")
    {
        var runs = new List<BuildRun>();
        foreach (ulong seed in new[] { SoloSession.DefaultSeed, 1UL })
        {
            runs.AddRange(Game.Core.RunCatalog.Doctrines.Select(d => BuildIdentityReview.Review(d.Id, "opening", seed, 5, opening: true)));
            runs.AddRange(new[] { "frontline", "ranged", "mixed", "towers", "research" }.Select(f => BuildIdentityReview.Review("warden", f, seed)));
            runs.Add(BuildIdentityReview.Review("warden", "sparse-early", seed, 2, opening: true, sparseAtEnd: true));
            runs.Add(BuildIdentityReview.Review("warden", "frontline", seed, 18, sparseAtEnd: true));
        }
        string report = System.Text.Json.JsonSerializer.Serialize(runs, Game.Core.WireJson.Options);
        if (args.Length > 1) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!); File.WriteAllText(args[1], report); }
        foreach (var run in runs) Console.WriteLine($"{run.Doctrine}/{run.Family}/{run.Seed}: {run.Waves.Length} waves · " + run.Waves[^1].Result);
        return 0;
    }
    if (args.FirstOrDefault() == "opening-review")
    {
        var runs = Game.Core.RunCatalog.Doctrines.Select(d => OpeningCampaign.Review(d.Id)).ToArray();
        string report = System.Text.Json.JsonSerializer.Serialize(runs, Game.Core.WireJson.Options);
        if (args.Length > 1) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!); File.WriteAllText(args[1], report); }
        foreach (var run in runs) Console.WriteLine(run.Doctrine + ": " + string.Join("; ", run.Waves.Select(w => $"W{w.Wave} {w.Phase} army={string.Join(',', w.Participants.Select(p => p.Key + p.Value))} lost={w.Lost} keep={w.KeepDamage}")));
        return 0;
    }
    if (args.FirstOrDefault() == "battle")
    {
        foreach (string scenario in new[] { "swordsman", "ranged", "status", "burn", "chill", "structures" })
        {
            var battle = new BattleShowcase(scenario);
            Console.WriteLine($"{scenario}: wave={battle.Snapshot.Wave} frames={battle.Frames.Count} outcomes={battle.Frames[^1].Phase}");
            Console.WriteLine(string.Join("; ", battle.Snapshot.Players[0].Soldiers.Select(u => $"{u.Type} L{u.Level} poison={u.Capabilities.PoisonPercent}")));
            Console.WriteLine($"research={battle.Snapshot.Players[0].Research}, statuses={battle.Frames.Sum(f=>f.Enemies.Count(u=>u.Statuses.Active))}");
            foreach (var point in BattleCaptures.Plan(battle, false)) Console.WriteLine($"  {point.Name}: {point.Tick:F2}");
        }
        return 0;
    }
    string checkpoint = args.FirstOrDefault() ?? "post-draft";
    string json;
    if (GameplayScenarios.Checkpoints.Contains(checkpoint))
    {
        using var solo = GameplayScenarios.CreateAt(checkpoint);
        json = System.Text.Json.JsonSerializer.Serialize(new { checkpoint, state = solo.Snapshot }, Game.Core.WireJson.Options);
    }
    else json = ScenarioRunner.Record(checkpoint);
    if (args.Length > 1)
    {
        string path = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
        Console.WriteLine(path);
    }
    else Console.WriteLine(json);
    return 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}
