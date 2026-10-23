// SPDX-License-Identifier: GPL-3.0-only
using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Six retained one-shot voices and cached technical earcons. Music is a separate owner.</summary>
public partial class FeedbackAudio : Node
{
    private sealed record Voice(AudioStreamPlayer Player) { public int Priority; }
    private readonly List<Voice> _ui = [], _sfx = [];
    private readonly Dictionary<FeedbackKind, AudioStreamWav> _tones = [];
    private readonly FeedbackBudget _budget = new();
    private bool _paused;
    public int Played { get; private set; }
    private readonly Dictionary<FeedbackKind, int> _playedKinds = [];
    public IReadOnlyDictionary<FeedbackKind, int> PlayedKinds => _playedKinds;
    public int Suppressed { get; private set; }
    public int Active => _ui.Concat(_sfx).Count(v => v.Player.HasStreamPlayback());
    public int Voices => _ui.Count + _sfx.Count;
    internal float[] CombatPositions => _sfx.Where(v => v.Player.HasStreamPlayback()).Select(v => v.Player.GetPlaybackPosition()).ToArray();
    public int CombatActive => _sfx.Count(v => v.Player.HasStreamPlayback());
    public bool CombatPaused => _paused && _sfx.All(v => !v.Player.HasStreamPlayback() || v.Player.StreamPaused);
    public IReadOnlyDictionary<FeedbackKind, AudioStreamWav> Tones => _tones;
    public void Initialize()
    {
        if (AudioServer.GetBusIndex("UI") < 0 || AudioServer.GetBusIndex("SFX") < 0) throw new InvalidOperationException("Native UI/SFX buses are missing.");
        foreach (var kind in Enum.GetValues<FeedbackKind>())
            _tones[kind] = new AudioStreamWav { Data = FeedbackTones.Pcm(kind), Format = AudioStreamWav.FormatEnum.Format16Bits,
                MixRate = FeedbackTones.Rate, Stereo = false, LoopMode = AudioStreamWav.LoopModeEnum.Disabled };
        for (int i = 0; i < 6; i++)
        {
            var player = new AudioStreamPlayer { Name = "Cue" + i, Bus = i < 2 ? "UI" : "SFX", VolumeDb = i < 2 ? -6 : -12 };
            AddChild(player); (i < 2 ? _ui : _sfx).Add(new(player));
        }
    }
    public bool Play(FeedbackCue cue)
    {
        double now = Time.GetTicksMsec() / 1000d;
        if (cue.Combat && _paused || !_budget.Allow(cue, now)) { Suppressed++; return false; }
        var pool = cue.Combat ? _sfx : _ui;
        var voice = pool.FirstOrDefault(v => !v.Player.HasStreamPlayback()) ?? pool.OrderBy(v => v.Priority).First();
        if (voice.Player.HasStreamPlayback() && voice.Priority >= cue.Priority) { Suppressed++; return false; }
        voice.Player.Stop(); voice.Player.Stream = _tones[cue.Kind]; voice.Player.StreamPaused = false;
        voice.Priority = cue.Priority; voice.Player.Play(); Played++; _playedKinds[cue.Kind] = _playedKinds.GetValueOrDefault(cue.Kind) + 1; return true;
    }
    public void PauseCombat(bool paused)
    {
        _paused = paused;
        foreach (var voice in _sfx) if (IsInstanceValid(voice.Player)) voice.Player.StreamPaused = paused;
    }
    public void Reset()
    {
        foreach (var voice in _ui.Concat(_sfx).Where(v => IsInstanceValid(v.Player))) { voice.Player.Stop(); voice.Player.Stream = null; voice.Priority = 0; }
        _budget.Clear(); PauseCombat(false);
    }
    public override void _ExitTree()
    {
        Reset(); foreach (var tone in _tones.Values) tone.Dispose(); _tones.Clear();
    }
}
