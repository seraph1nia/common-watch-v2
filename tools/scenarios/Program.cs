using Game.Shared;

try
{
    if (args.FirstOrDefault() == "preparation-food")
    {
        Directory.CreateDirectory(args[1]);
        foreach (int stages in new[] { 3, 2 })
        {
            using var solo = GameplayScenarios.CreateAt("forecast", rules: new Game.Core.Rules { PreparationStages = stages });
            var before = solo.Snapshot; var forecast = solo.City.FoodForecast!;
            var accepted = solo.Send("ready");
            if (!accepted.Accepted || !forecast.Participating.SequenceEqual(solo.City.LastUpkeep!.Participating) || forecast.Paid != solo.City.LastUpkeep.Paid)
                throw new InvalidOperationException("Partial Food forecast must match actual final payment.");
            File.WriteAllText(Path.Combine(args[1], $"partial-food-{stages}.json"), System.Text.Json.JsonSerializer.Serialize(new { before, after = solo.Snapshot }, Game.Core.WireJson.Options));
            Console.WriteLine($"PREPARATION_FOOD_PASS stages={stages} funded={forecast.Funded.Length} unfunded={forecast.Unfed.Length} paid={forecast.Paid}");
        }
        return 0;
    }
    if (args.FirstOrDefault() == "preparation-campaigns")
    {
        Directory.CreateDirectory(args[1]);
        foreach (int stages in new[] { 3, 2 })
        {
            var rules = new Game.Core.Rules { PreparationStages = stages };
            var adaptive = (from family in new[] { "frontline", "ranged", "mixed", "towers", "research" }
                            from seed in new[] { 1UL, SoloSession.DefaultSeed }
                            from response in new[] { false, true }
                            select CampaignReview.Run(family, seed, response, rules)).ToArray();
            File.WriteAllText(Path.Combine(args[1], $"forecast-{stages}.json"), System.Text.Json.JsonSerializer.Serialize(adaptive, Game.Core.WireJson.Options));
            var reordered = (from doctrine in Game.Core.RunCatalog.Doctrines
                             from family in new[] { "frontline", "mixed", "towers", "research" }
                             from seed in new[] { 1UL, SoloSession.DefaultSeed }
                             select RewardEconomyReview.Run(doctrine.Id, family, seed, rules: rules, openingOrder: "economy-first", adaptOpening: true)).ToArray();
            File.WriteAllText(Path.Combine(args[1], $"reordered-{stages}.json"), System.Text.Json.JsonSerializer.Serialize(reordered, Game.Core.WireJson.Options));
            Console.WriteLine($"PREPARATION_CAMPAIGNS_PASS stages={stages} forecastVictories={adaptive.Count(r => r.Waves[^1].Result == "Victory")}/20 reorderedVictories={reordered.Count(r => r.Battles[^1].Result == "Victory")}/48");
        }
        return 0;
    }
    if (args.FirstOrDefault() == "preparation-focused")
    {
        Directory.CreateDirectory(args[1]);
        foreach (int stages in new[] { 3, 2 })
        {
            var rules = new Game.Core.Rules { PreparationStages = stages };
            var runs = (from family in new[] { "reserve", "research", "towers" }
                        from seed in new[] { 1UL, SoloSession.DefaultSeed }
                        select RewardEconomyReview.Run("warden", family, seed, rules: rules, recordPreparations: true)).ToArray();
            File.WriteAllText(Path.Combine(args[1], $"focused-{stages}.json"), System.Text.Json.JsonSerializer.Serialize(runs, Game.Core.WireJson.Options));
            Console.WriteLine($"PREPARATION_FOCUSED_PASS stages={stages} healing={runs.Sum(r => r.Commands.Where(c => c.Preparation is not null).Sum(c => c.SoldierHealing))} payments={runs.Sum(r => r.Commands.Count(c => c.Preparation is not null))}");
        }
        return 0;
    }
    if (args.FirstOrDefault() == "preparation-opening-orders")
    {
        Directory.CreateDirectory(args[1]);
        foreach (string order in new[] { "canonical", "army-first", "economy-first" })
            foreach (int stages in new[] { 3, 2 })
            {
                var rules = new Game.Core.Rules { PreparationStages = stages };
                var runs = (from doctrine in Game.Core.RunCatalog.Doctrines
                            from seed in new[] { 1UL, SoloSession.DefaultSeed }
                            select RewardEconomyReview.Run(doctrine.Id, "opening", seed, rules: rules, openingOrder: order)).ToArray();
                File.WriteAllText(Path.Combine(args[1], $"{order}-{stages}.json"), System.Text.Json.JsonSerializer.Serialize(runs, Game.Core.WireJson.Options));
                Console.WriteLine($"PREPARATION_OPENING_ORDER_PASS order={order} stages={stages} runs={runs.Length} threeWaves={runs.Count(r => r.Battles.Length == 3 && r.Battles[^1].Result != "Defeat")}");
            }
        return 0;
    }
    if (args.FirstOrDefault() == "preparation-review")
    {
        Directory.CreateDirectory(args[1]);
        foreach (int stages in new[] { 3, 2 })
        {
            var rules = new Game.Core.Rules { PreparationStages = stages };
            var runs = (from doctrine in Game.Core.RunCatalog.Doctrines
                        from family in new[] { "opening", "frontline", "ranged", "mixed", "towers", "research" }
                        from seed in new[] { 1UL, SoloSession.DefaultSeed }
                        select RewardEconomyReview.Run(doctrine.Id, family, seed, rules: rules)).ToArray();
            File.WriteAllText(Path.Combine(args[1], $"{stages}-stage.json"), System.Text.Json.JsonSerializer.Serialize(runs, Game.Core.WireJson.Options));
            Console.WriteLine($"PREPARATION_CANDIDATE_PASS stages={stages} runs={runs.Length} battles={runs.Sum(r => r.Battles.Length)} fullVictories={runs.Count(r => r.Family != "opening" && r.Battles[^1].Result == "Victory")}");
        }
        return 0;
    }
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
