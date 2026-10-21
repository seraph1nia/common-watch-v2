using System.Diagnostics;
using Xunit;

namespace Game.Integration.Tests;

public sealed class GameplayUiTests
{
    [Fact]
    public async Task NativeGameplayAndCooperativeInspection()
    {
        DirectoryInfo? root = new(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "CommonWatch.slnx"))) root = root.Parent;
        Assert.NotNull(root);
        foreach (string operation in new[] { "test-gameplay", "test-gameplay-network" })
        {
            var start = new ProcessStartInfo("bash") { WorkingDirectory = root.FullName, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("tools/graphical.sh"); start.ArgumentList.Add(operation);
            using var process = Process.Start(start)!;
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(4));
            try { await process.WaitForExitAsync(deadline.Token); }
            catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw new TimeoutException("Gameplay native checks exceeded four minutes."); }
            string transcript = await output + await error;
            Directory.CreateDirectory(Path.Combine(root.FullName, "artifacts/test-logs"));
            await File.WriteAllTextAsync(Path.Combine(root.FullName, "artifacts/test-logs", operation + ".log"), transcript);
            Assert.True(process.ExitCode == 0, transcript);
            Assert.DoesNotContain("ERROR:", transcript);
            Assert.DoesNotContain("Unhandled exception", transcript);
            if (operation == "test-gameplay")
            {
                Assert.Contains("GODOT_GAMEPLAY_PASS mode=Sprites view=Angled hud=Illustrated", transcript);
                if (Environment.GetEnvironmentVariable("COMMON_WATCH_GAMEPLAY_ART") != "illustrated")
                    Assert.Contains("GODOT_GAMEPLAY_PASS mode=Debug view=Topdown hud=Plain", transcript);
            }
            Assert.Contains(operation == "test-gameplay" ? "GODOT_GAMEPLAY_PASS" : "GODOT_GAMEPLAY_NETWORK_PASS", transcript);
        }
    }
}
