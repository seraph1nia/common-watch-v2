using System.Diagnostics;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Game.Integration.Tests;

// Real graphics checks intentionally fail clearly when Linux Xvfb prerequisites are absent.
// Headless-only environments can still run test-core and test-shared independently.
public sealed class ClientTests
{
    [Fact]
    public Task GodotFullLoopBothModes() => VerifyClient();

    [Fact]
    public Task GodotInstalledArt() => Run(FindRoot(), Path.Combine(FindRoot(), "assets/sprites"), illustrated: true);

    [Fact]
    public Task GodotNormalPlayDefaults() => Run(FindRoot(), Path.Combine(FindRoot(), "assets/sprites"), defaults: true);

    private static async Task VerifyClient()
    {
        string root = FindRoot();
        string temporary = Path.Combine(Path.GetTempPath(), "common-watch-integration-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            // Empty directory exercises permanent artwork independence in both modes.
            await Run(root, temporary);
            await Run(root, temporary, missingHud: true);
            // Corrupt files are textual fixtures, never generated images. The debug
            // renderer must ignore them; sprite mode must fall back without crashing.
            Directory.CreateDirectory(Path.Combine(temporary, "buildings"));
            Directory.CreateDirectory(Path.Combine(temporary, "units"));
            await File.WriteAllTextAsync(Path.Combine(temporary, "buildings", "farm.png"), "invalid PNG - fallback test");
            await File.WriteAllTextAsync(Path.Combine(temporary, "units", "swordsman.png"), "invalid PNG - fallback test");
            await Run(root, temporary);
        }
        finally { Directory.Delete(temporary, recursive: true); }
    }

    private static async Task Run(string root, string assets, bool illustrated = false, bool missingHud = false, bool defaults = false)
    {
        var start = new ProcessStartInfo("bash")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string argument in new[] { "tools/graphical.sh", defaults ? "test-defaults" : "test", "--assets-root", assets })
            start.ArgumentList.Add(argument);
        if (illustrated)
            foreach (string argument in new[] { "--view", "angled", "--hud-art", "illustrated", "--debug-overlay", "on", "--expect-installed-art" }) start.ArgumentList.Add(argument);
        if (missingHud)
            foreach (string argument in new[] { "--view", "angled", "--hud-art", "illustrated" }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Failed to launch graphical checks.");
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException) { process.Kill(entireProcessTree: true); throw new TimeoutException($"Godot graphical integration exceeded four minutes."); }
        string transcript = await output + "\n" + await error;
        string logDirectory = Path.Combine(root, "artifacts", "test-logs");
        Directory.CreateDirectory(logDirectory);
        await File.WriteAllTextAsync(Path.Combine(logDirectory, "godot-" + (defaults ? "defaults" : illustrated ? "installed" : missingHud ? "empty-illustrated" : Directory.EnumerateFiles(assets, "*.png", SearchOption.AllDirectories).Any() ? "invalid" : "empty") + ".log"), transcript);
        Assert.True(process.ExitCode == 0, $"Godot actual-graphics check failed ({process.ExitCode}):\n{transcript}");
        Assert.DoesNotContain("ERROR:", transcript);
        Assert.DoesNotContain("Unhandled exception", transcript);
        int passes = defaults ? 1 : 2;
        Assert.Equal(passes, transcript.Split("SMOKE PASS", StringSplitOptions.None).Length - 1);
        Assert.True(transcript.Split("GODOT_AUDIO_PASS loaded=true", StringSplitOptions.None).Length - 1 >= passes,
            "Every smoke pass must decode and start bundled music, including settings checks.");
        Assert.Contains("settings=0/50/100 persisted=true", transcript);
        Assert.Equal(passes, transcript.Split("GODOT_CAMERA_PASS", StringSplitOptions.None).Length - 1);
        if (!defaults) Assert.Contains("mode=Debug", transcript);
        Assert.Contains("mode=Sprites", transcript);
        if (defaults) Assert.Contains("GODOT_DEFAULTS_PASS mode=Sprites view=Angled hud=Illustrated overlay=false main-menu=true", transcript);
        if (illustrated || defaults)
        {
            Assert.Equal(passes, transcript.Split("_ART_PASS", StringSplitOptions.None).Length - 1);
            Assert.Contains("view=Angled hud=Illustrated", transcript);
        }
    }

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CommonWatch.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Cannot locate CommonWatch.slnx.");
    }
}
