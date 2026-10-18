using System.Diagnostics;
using Xunit;

namespace Game.Integration.Tests;

public sealed class NetworkTests
{
    [Fact, Trait("Layer", "multiplayer")]
    public Task GodotTwoThroughFourPlayers() => Run("multiplayer.sh");
    [Fact, Trait("Layer", "reconnect")]
    public Task GodotReconnectAndFailures() => Run("reconnect.sh");

    private static async Task Run(string script)
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "CommonWatch.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        var start = new ProcessStartInfo("bash") { WorkingDirectory = root.FullName, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("tools/" + script);
        if (script == "multiplayer.sh") start.ArgumentList.Add("test");
        using var process = Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        try { await process.WaitForExitAsync(deadline.Token); }
        catch (OperationCanceledException) { process.Kill(true); throw new TimeoutException("Network process supervisor exceeded deadline."); }
        string transcript = await output + await error;
        Assert.True(process.ExitCode == 0, transcript);
        Assert.DoesNotContain("ERROR:", transcript);
        Assert.DoesNotContain("Unhandled exception", transcript);
    }
}
