using Game.Shared;

try
{
    if (args.FirstOrDefault() is "reward-economy-review" or "build-review")
    {
        var runs = (from doctrine in Game.Core.RunCatalog.Doctrines
                    from family in new[] { "opening", "frontline", "ranged", "mixed", "towers", "research" }
                    from seed in new[] { 1UL, SoloSession.DefaultSeed }
                    select RewardEconomyReview.Run(doctrine.Id, family, seed)).ToArray();
        string report = System.Text.Json.JsonSerializer.Serialize(runs, Game.Core.WireJson.Options);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!);
        File.WriteAllText(args[1], report);
        foreach (var run in runs) Console.WriteLine($"{run.Doctrine}/{run.Family}/{run.Seed}: {run.Battles.Length} battles {run.Battles[^1].Result}; lost {run.Battles.Sum(b => b.Casualties)}, Keep {run.Battles.Sum(b => b.KeepDamage)}, shortages {run.Shortages.Length}");
        Console.WriteLine($"REWARD_ECONOMY_REVIEW_PASS {runs.Length} runs {runs.Sum(r => r.Battles.Length)} battles; ordinary accepted commands only");
        return 0;
    }
    if (args.FirstOrDefault() == "cooperation-review")
    {
        var runs = (from players in Enumerable.Range(2, 5)
                    from policy in new[] { "frontline", "limited" }
                    from seed in new[] { 1UL, SoloSession.DefaultSeed }
                    select CooperationReview.Run(players, policy, seed)).ToArray();
        string report = System.Text.Json.JsonSerializer.Serialize(runs, Game.Core.WireJson.Options);
        if (args.Length > 1) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!); File.WriteAllText(args[1], report); }
        foreach (var run in runs) Console.WriteLine($"{run.Players}p {run.Policy} {run.Seed}: {run.Result}, {run.Battles.Length} waves; first fall W{run.FallWave}; spectator {run.SpectatorTicks} ticks; cross-sector {run.Battles.Sum(b => b.CrossSectorSamples)} samples");
        return 0;
    }
    if (args.FirstOrDefault() == "campaign-review")
    {
        var runs = (from seed in new[] { 1UL, SoloSession.DefaultSeed }
                    from family in new[] { "frontline", "ranged", "mixed", "towers", "research" }
                    from adaptive in new[] { false, true }
                    select CampaignReview.Run(family, seed, adaptive)).ToArray();
        string report = System.Text.Json.JsonSerializer.Serialize(runs, Game.Core.WireJson.Options);
        if (args.Length > 1) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!); File.WriteAllText(args[1], report); }
        foreach (var run in runs) Console.WriteLine($"{run.Family} seed={run.Seed} adaptive={run.Adaptive}: {run.Waves.Length} waves, {run.Waves[^1].Result}; ticks {run.Waves.Sum(w => w.Ticks)}, lost {run.Waves.Sum(w => w.Lost)}, Keep {run.Waves.Sum(w => w.KeepDamage)}; responses {run.Waves.Sum(w => w.Adaptations.Length)}");
        return 0;
    }
    if (args.FirstOrDefault() == "veteran-review")
    {
        var cases = VeteranReview.Collect();
        string report = System.Text.Json.JsonSerializer.Serialize(cases, Game.Core.WireJson.Options);
        if (args.Length > 1) { Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1]))!); File.WriteAllText(args[1], report); }
        foreach (var item in cases) Console.WriteLine($"{item.Name} seed={item.Seed} W{item.Wave}\n{item.Comparison.Summary}\n{item.Comparison.Details}");
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
        using var solo = GameplayScenarios.CreateAt(checkpoint, enrich: !args.Contains("--raw"));
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
