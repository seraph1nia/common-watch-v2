using Game.MonoGameClient;

try
{
    using var game = new WatchGame(args);
    game.Run();
}
catch (Exception exception)
{
    Console.Error.WriteLine($"MonoGame failed: {exception}");
    Environment.ExitCode = 1;
}
