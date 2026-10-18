using Game.Shared;

try
{
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
    string json = ScenarioRunner.Record(checkpoint);
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
