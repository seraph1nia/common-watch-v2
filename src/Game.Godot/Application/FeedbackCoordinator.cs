// SPDX-License-Identifier: GPL-3.0-only
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Routes read-only session receipts and presented battle events to their audio/UI owners.</summary>
public partial class FeedbackCoordinator : Node
{
    private AudioController _audio = null!;
    private SettlementHud _hud = null!;
    private SettlementView _settlement = null!;
    private BattlefieldView _battlefield = null!;
    private GodotSession? _session;
    private Action<Command, CommandResult>? _receipt;
    private readonly FeedbackTimeline _timeline = new();
    private long _floor;
    private int _epoch, _generation;
    private bool _initialized, _connected;
    public int Transactions { get; private set; }
    public int CombatCues { get; private set; }
    public int Milestones { get; private set; }
    public void Initialize(AudioController audio, SettlementHud hud, SettlementView settlement, BattlefieldView battlefield)
    {
        _audio = audio; _hud = hud; _settlement = settlement; _battlefield = battlefield;
        _hud.Feedback.Initialize(_hud, cue => FeedbackAnchors.Resolve(cue, _session, _hud, _settlement, _battlefield), () => _session?.Playback.VisualSeconds ?? 0);
        _battlefield.CombatEventPresented += OnCombat;
    }
    public void Bind(GodotSession? session)
    {
        if (ReferenceEquals(_session, session)) return;
        if (_session is not null && _receipt is not null) _session.CommandAcknowledged -= _receipt;
        _session = session; _initialized = _connected = false; int epoch = ++_epoch;
        if (IsInstanceValid(_audio)) _audio.Feedback.Reset();
        if (IsInstanceValid(_hud) && IsInstanceValid(_hud.Feedback)) _hud.Feedback.Clear();
        if (session is null) { _receipt = null; return; }
        _floor = session.FeedbackSequence;
        _receipt = (command, result) =>
        {
            if (_epoch != epoch || !ReferenceEquals(_session, session) || !session.HasState || !session.Connected) return;
            EnsureBaseline();
            if (_timeline.Result(command, result) is { } cue) { Transactions++; Emit(cue); }
        };
        session.CommandAcknowledged += _receipt;
        if (session.HasState) EnsureBaseline();
    }
    private void EnsureBaseline()
    {
        if (_initialized || _session is not { HasState: true }) return;
        _timeline.Baseline(_session.Snapshot, _session.PlayerId, _floor);
        _generation = _session.Playback.Generation; _initialized = true; _connected = _session.Connected;
    }
    public void Update(GodotSession? session, bool enabled = true)
    {
        Bind(enabled ? session : null); if (_session is not { HasState: true }) return;
        EnsureBaseline();
        if (!_session.Connected)
        {
            if (_connected) { _audio.Feedback.Reset(); _hud.Feedback.Clear(); }
            _connected = false; return;
        }
        if (!_connected || _generation != _session.Playback.Generation)
            ResetPresentation();
        _connected = true; _audio.Feedback.PauseCombat(_session.Snapshot.Paused);
        foreach (var cue in _timeline.State(_session.Snapshot, _battlefield.Finishing)) { Milestones++; Emit(cue); }
        if (_session.Snapshot.Phase == Phase.Combat && _session.Snapshot.Enemies.Any(u => u.IsBoss)
            && _timeline.Boss(_session.Snapshot, _session.Playback.Units(), _session.Snapshot.Paused) is { } boss) { CombatCues++; Emit(boss); }
    }
    private void OnCombat(CombatEvent entry)
    {
        if (_session is not { HasState: true, Connected: true } || !ReferenceEquals(_battlefield.Playback, _session.Playback)) return;
        EnsureBaseline();
        if (_generation != _session.Playback.Generation) { ResetPresentation(); return; }
        if (_timeline.Presented(entry, _session.Snapshot.Paused) is { } cue) { CombatCues++; Emit(cue); }
    }
    private void ResetPresentation()
    {
        _timeline.BaselinePresentation(_session!.Snapshot); _audio.Feedback.Reset(); _hud.Feedback.Clear();
        _generation = _session.Playback.Generation;
    }
    private void Emit(FeedbackCue cue) { _audio.Feedback.Play(cue); _hud.Feedback.Show(cue); }
    public override void _ExitTree()
    {
        if (IsInstanceValid(_battlefield)) _battlefield.CombatEventPresented -= OnCombat;
        Bind(null);
    }
}
