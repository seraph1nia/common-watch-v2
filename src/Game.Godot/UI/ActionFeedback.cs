// SPDX-License-Identifier: GPL-3.0-only
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Four short, input-free labels. Combat aging follows presented visual time.</summary>
public partial class ActionFeedback : Control
{
    private sealed record Pulse(FeedbackCue Cue, double Started, bool Playback);
    private readonly List<Pulse> _pulses = [];
    private Func<FeedbackCue, Vector2> _anchor = _ => Vector2.Zero;
    private Func<double> _playback = () => 0;
    private SettlementHud _hud = null!;
    private double _clock;
    private readonly List<Rect2> _bounds = [];
    public int Count => _pulses.Count;
    public string Text => string.Join(" · ", _pulses.Select(p => p.Cue.Text));
    public bool AvoidsGameplay => _bounds.All(r => !ProtectedAreas().Any(p => p.Intersects(r)));
    private Rect2[] ProtectedAreas() => _hud.Draft.IsVisibleInTree() ? _hud.Draft.FeedbackAreas
        : _hud.Details.IsVisibleInTree() ? _hud.Details.FeedbackAreas
        : new[] { _hud.LedgerArea, _hud.PhaseArea, new Rect2(0, Size.Y - 104, Size.X, 104) }
        .Concat(_hud.PreparationSummary.IsVisibleInTree() ? new[] { _hud.PreparationArea } : [])
        .Concat(_hud.OpeningGuide.IsVisibleInTree() ? new[] { _hud.OpeningGuide.GetGlobalRect() } : [])
        .Concat(_hud.Team.IsVisibleInTree() ? new[] { _hud.Team.GetGlobalRect() } : [])
        .Concat(_hud.LiveInspector.IsVisibleInTree() ? new[] { _hud.LiveInspector.GetGlobalRect() } : []).ToArray();
    public bool Fits => _bounds.All(r => GetViewport().GetVisibleRect().Encloses(r));
    public void Initialize(SettlementHud hud, Func<FeedbackCue, Vector2> anchor, Func<double> playback)
    {
        MouseFilter = MouseFilterEnum.Ignore; SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _hud = hud; _anchor = anchor; _playback = playback;
    }
    public void Show(FeedbackCue cue)
    {
        if (!cue.Visual) return;
        if (cue.Kind is FeedbackKind.WaveClear or FeedbackKind.Defeat or FeedbackKind.Victory) _pulses.Clear();
        _pulses.RemoveAll(p => p.Cue.Kind == cue.Kind && p.Cue.City == cue.City && p.Cue.Slot == cue.Slot);
        if (_pulses.Count == 4) _pulses.RemoveAt(_pulses.Select((p, i) => (p, i)).OrderBy(x => x.p.Cue.Priority).First().i);
        _pulses.Add(new(cue, cue.Combat ? _playback() : _clock, cue.Combat)); QueueRedraw();
    }
    public void Clear() { _pulses.Clear(); _bounds.Clear(); QueueRedraw(); }
    public override void _Process(double delta)
    {
        _clock += Math.Clamp(delta, 0, .1);
        _pulses.RemoveAll(p => Age(p) >= 1.5); if (_pulses.Count > 0) QueueRedraw(); else if (_bounds.Count > 0) { _bounds.Clear(); QueueRedraw(); }
    }
    private double Age(Pulse pulse) => Math.Max(0, (pulse.Playback ? _playback() : _clock) - pulse.Started);
    public override void _Draw()
    {
        _bounds.Clear(); var used = new List<Rect2>(); var reserved = ProtectedAreas();
        foreach (var pulse in _pulses.OrderByDescending(p => p.Cue.Priority))
        {
            float age = (float)Age(pulse), alpha = Mathf.Min(1, (1.5f - age) / .3f);
            string text = pulse.Cue.City > 0 && !pulse.Cue.Text.StartsWith($"P{pulse.Cue.City} ", StringComparison.Ordinal) ? $"P{pulse.Cue.City} · {pulse.Cue.Text}" : pulse.Cue.Text;
            if (text.Length > 42) text = text[..39] + "…";
            float width = Mathf.Min(330, ThemeDB.FallbackFont.GetStringSize(text, fontSize: 14).X + 20);
            Vector2 point = _anchor(pulse.Cue) - new Vector2(0, age * 10);
            Rect2 rect = default; bool placed = false;
            for (int attempt = 0; attempt < 24 && !placed; attempt++)
            {
                Vector2 candidate = attempt == 0 ? point : attempt < 8 ? point + new Vector2(0, (attempt % 2 == 0 ? 1 : -1) * ((attempt + 1) / 2) * 34)
                    : attempt < 16 ? point + new Vector2((attempt % 2 == 0 ? 1 : -1) * ((attempt - 6) / 2) * 80, 0)
                    : new Vector2(Size.X / 2, 132 + (attempt - 16) * 34);
                rect = new(new(Mathf.Clamp(candidate.X - width / 2, 8, Size.X - width - 8), Mathf.Clamp(candidate.Y - 30, 8, Size.Y - 80)), new(width, 30));
                placed = !reserved.Any(r => r.Intersects(rect)) && !used.Any(r => r.Intersects(rect));
            }
            if (!placed) continue;
            used.Add(rect); _bounds.Add(rect);
            var color = new Color(pulse.Cue.Kind == FeedbackKind.Rejected || pulse.Cue.Kind == FeedbackKind.Defeat ? "efac99" : "efd478") with { A = alpha };
            if (pulse.Cue.Kind == FeedbackKind.Production && !_hud.BlocksWorld) DrawRect(_hud.LedgerArea.Grow(2), color with { A = alpha * .5f }, false, 2, true);
            DrawRect(rect, new Color(.035f, .09f, .09f, .94f * alpha)); DrawRect(rect, color, false, 1, true);
            DrawString(ThemeDB.FallbackFont, rect.Position + new Vector2(10, 20), text, HorizontalAlignment.Left, width - 20, 14, color);
        }
    }
}
