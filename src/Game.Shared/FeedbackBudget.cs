// SPDX-License-Identifier: GPL-3.0-only
namespace Game.Shared;

/// <summary>Audio-only real-time backpressure, independent of simulation and playback advancement.</summary>
public sealed class FeedbackBudget
{
    private readonly Dictionary<FeedbackKind, double> _last = [];
    private readonly Queue<double> _ui = new(), _combat = new();
    public void Clear() { _last.Clear(); _ui.Clear(); _combat.Clear(); }
    public bool Allow(FeedbackCue cue, double now)
    {
        if (!double.IsFinite(now)) return false;
        var window = cue.Combat ? _combat : _ui;
        while (window.TryPeek(out double time) && now - time >= 1) window.Dequeue();
        double gap = cue.Kind switch { FeedbackKind.Structure => .35, FeedbackKind.Impact => .10, FeedbackKind.Ranged => .14,
            FeedbackKind.Spell => .18, FeedbackKind.BossArrival => 1, _ => .08 };
        if (_last.TryGetValue(cue.Kind, out double last) && now - last < gap) return false;
        if (cue.Priority < 3 && window.Count >= (cue.Combat ? 12 : 10)) return false;
        _last[cue.Kind] = now;
        if (window.Count < 16) window.Enqueue(now);
        return true;
    }
}
