using Game.Shared;

try
{
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
