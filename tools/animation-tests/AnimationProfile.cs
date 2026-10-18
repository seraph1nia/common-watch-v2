using System.Diagnostics;
using System.Text.Json;
using Game.Core;
using Game.Shared;

namespace Game.Development;

// A bounded development probe, compiled into each executable like OpeningProbe.
public sealed class AnimationProfile(string path)
{
    readonly List<object> runs = [];
    readonly List<double> cpu = [];
    int run, peakEffects, frames;
    long started, bytes, allocated;
    double wall;
    public int Fps => new[] { 30, 60, 120 }[run / 3];
    public CombatSpeed Speed => (CombatSpeed)(run % 3);
    public double Step => 1.0 / Fps;
    public bool Done => run == 9;
    public void Begin(double delta)
    { started = Stopwatch.GetTimestamp(); bytes = GC.GetAllocatedBytesForCurrentThread(); wall += delta; }
    public bool End(BattleShowcase battle, double animationTick, int effects, int actors)
    {
        if (started == 0) return false;
        cpu.Add(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        allocated += GC.GetAllocatedBytesForCurrentThread() - bytes; started = 0;
        frames++; peakEffects = Math.Max(peakEffects, effects);
        if (!battle.Complete || animationTick < battle.Snapshot.Tick + 61) return false;
        if (effects != 0) throw new InvalidOperationException("Animation effects remained after terminal cleanup.");
        double[] sorted = cpu.Order().ToArray();
        runs.Add(new { targetFps = Fps, speed = CombatSpeedRates.Label(Speed), frames, measuredFps = frames / wall,
            meanSubmissionMs = cpu.Average(), p95SubmissionMs = sorted[(int)((sorted.Length - 1) * .95)], maxSubmissionMs = cpu.Max(),
            allocatedBytesPerFrame = allocated / frames, peakEffects, finalEffects = effects, finalActors = actors, liveManagedBytes = GC.GetTotalMemory(false) });
        run++; cpu.Clear(); frames = 0; wall = 0; allocated = 0; peakEffects = 0;
        if (Done)
        {
            string full = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, JsonSerializer.Serialize(runs, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("ANIMATION_PROFILE_PASS " + full);
        }
        else { battle.Restart(); battle.Paused = false; battle.Speed = Speed; }
        return true;
    }
}
