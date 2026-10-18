using Godot;
using Game.Shared;

namespace Game.GodotClient;

public partial class Main
{
    private AudioStreamPlayer _music = null!;
    private AudioSettings _audioSettings = null!;
    private string _audioSettingsPath = "", _musicPath = "";
    private string? _musicUnavailable;
    private Control _settingsOverlay = null!;
    private PanelContainer _settingsPanel = null!;
    private Button _settingsButton = null!, _closeSettingsButton = null!;
    private HSlider _musicSlider = null!;
    private Label _musicVolumeLabel = null!, _musicStatus = null!;
    private bool _quitting;

    private void InitializeMusic()
    {
        _audioSettingsPath = Argument("--settings-path") ?? ProjectSettings.GlobalizePath("user://audio-settings.json");
        _audioSettings = AudioSettings.Load(_audioSettingsPath);
        _music = new AudioStreamPlayer { Name = "BackgroundMusic", VolumeLinear = _audioSettings.MusicVolume };
        AddChild(_music);
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += async () => await QuitAfterAudioDrain();
        string audioRoot = Argument("--audio-root") ?? AssetDirectory("audio");
        _musicPath = System.IO.Path.Combine(audioRoot, MusicTrack.RelativePath);
        if (!System.IO.File.Exists(_musicPath))
        {
            _musicUnavailable = "Music file unavailable. The game remains playable.";
            return;
        }
        try
        {
            // Detect the WAV's embedded smpl loop, preserving the original PCM and sample rate.
            var stream = AudioStreamWav.LoadFromFile(_musicPath, new Godot.Collections.Dictionary
            {
                ["compress/mode"] = 0,
                ["edit/loop_mode"] = 0,
                ["force/max_rate"] = false
            });
            if (stream is null) throw new InvalidDataException("WAV decoder did not return an audio stream.");
            _music.Stream = stream;
            _music.Play();
        }
        catch (Exception exception)
        {
            _musicUnavailable = "Music unavailable. The game remains playable.";
            GD.PushWarning($"Background music: {exception.Message}");
        }
    }

    private void CreateAudioUi(Control root)
    {
        _settingsButton = Button("Settings", OpenSettings);
        root.AddChild(_settingsButton);
        _settingsButton.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _settingsButton.OffsetLeft = -130; _settingsButton.OffsetRight = -14;
        _settingsButton.OffsetTop = 12; _settingsButton.OffsetBottom = 48;

        _settingsOverlay = new Control { Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        root.AddChild(_settingsOverlay); _settingsOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0, 0, 0, .72f), MouseFilter = Control.MouseFilterEnum.Ignore };
        _settingsOverlay.AddChild(shade); shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _settingsPanel = new PanelContainer(); _settingsOverlay.AddChild(_settingsPanel);
        _settingsPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _settingsPanel.OffsetLeft = -280; _settingsPanel.OffsetRight = 280;
        _settingsPanel.OffsetTop = -180; _settingsPanel.OffsetBottom = 180;
        _settingsPanel.AddThemeStyleboxOverride("panel", PanelStyle(new Color("14212c"), _gold));
        var content = new VBoxContainer(); _settingsPanel.AddChild(Margin(content, 24));
        content.AddChild(new Label { Text = "SETTINGS", Modulate = _gold });
        content.AddChild(WrapLabel("Background music • Echoes of Valhalla"));
        _musicVolumeLabel = new Label(); content.AddChild(_musicVolumeLabel);
        _musicSlider = new HSlider
        {
            MinValue = 0, MaxValue = 100, Step = 1,
            CustomMinimumSize = new Vector2(480, 40), FocusMode = Control.FocusModeEnum.All,
            TooltipText = "Music volume. Set to zero to mute."
        };
        _musicSlider.SetValueNoSignal(_audioSettings.MusicVolume * 100);
        _musicSlider.ValueChanged += value => SetMusicVolume((float)value / 100);
        content.AddChild(_musicSlider);
        content.AddChild(WrapLabel("0% mutes music. Changes apply immediately."));
        _musicStatus = WrapLabel(); content.AddChild(_musicStatus);
        _closeSettingsButton = Button("Close [Esc]", CloseSettings); content.AddChild(_closeSettingsButton);
        SetMusicVolume(_audioSettings.MusicVolume);
    }

    private void SetMusicVolume(float volume)
    {
        _audioSettings.MusicVolume = volume;
        _music.VolumeLinear = _audioSettings.MusicVolume;
        _musicVolumeLabel.Text = $"Music volume: {_audioSettings.MusicVolume * 100:0}%";
        _musicStatus.Text = _musicUnavailable ?? (_audioSettings.MusicVolume == 0 ? "Music muted."
            : AudioServer.GetDriverName() == "Dummy" ? "Audio output unavailable (silent)." : "Music playing.");
        if (!_audioSettings.Save()) _musicStatus.Text += " Volume applies for this session; settings could not be saved.";
    }
    private void OpenSettings()
    {
        _dragging = false;
        _settingsOverlay.Visible = true;
        _musicSlider.GrabFocus();
    }
    private void CloseSettings()
    {
        _settingsOverlay.Visible = false;
        if (!_audioSettings.Save()) _message.Text = "Settings could not be saved. Changes apply for this session.";
        _settingsButton.GrabFocus();
    }

    private async Task SmokeAudioSettings()
    {
        float original = _audioSettings.MusicVolume;
        ulong playerId = _music.GetInstanceId();
        _settingsButton.EmitSignal(BaseButton.SignalName.Pressed);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // Layout consumes a normal simulation frame; changing preferences must not issue commands.
        long revision = _session.Snapshot.Revision;
        long tick = _session.Snapshot.Tick;
        Require(_settingsOverlay.Visible, "settings button opens overlay");
        Require(GetViewportRect().Encloses(_settingsPanel.GetGlobalRect()), $"settings overlay fits viewport (panel={_settingsPanel.GetGlobalRect()}, viewport={GetViewportRect()})");
        foreach (double percent in new double[] { 0, 50, 100 })
        {
            _musicSlider.Value = percent;
            Require(Math.Abs(_music.VolumeLinear - percent / 100) < .0001, $"music slider applies {percent}%");
            Require(Math.Abs(_audioSettings.MusicVolume - percent / 100) < .0001, "audio setting follows slider");
        }
        _closeSettingsButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(!_settingsOverlay.Visible, "settings close button");
        Require(Math.Abs(AudioSettings.Load(_audioSettingsPath).MusicVolume - 1) < .0001, "audio settings save and reload");
        Require(_session.Snapshot.Revision == revision && _session.Snapshot.Tick == tick, "settings do not change gameplay");
        Require(_music.GetInstanceId() == playerId, "settings retain music player");
        _musicSlider.Value = original * 100;
        _audioSettings.Save();
        if (System.IO.File.Exists(_musicPath))
        {
            Require(_music.Stream is AudioStreamWav, "actual upstream WAV decoded");
            var stream = (AudioStreamWav)_music.Stream;
            Require(stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward && stream.LoopBegin == MusicTrack.LoopStart
                && stream.LoopEnd == MusicTrack.LoopEnd, "embedded WAV loop bounds retained");
            Require(stream.MixRate == MusicTrack.SampleRate, "upstream music sample rate retained");
            Require(_music.Playing, "background music player started");
            GD.Print($"GODOT_AUDIO_PASS loaded=true playing={_music.Playing} driver={AudioServer.GetDriverName()} loop={stream.LoopBegin}..{stream.LoopEnd} settings=0/50/100 persisted=true");
        }
        else
        {
            Require(_musicUnavailable is not null, "missing music is reported gracefully");
            GD.Print("GODOT_AUDIO_PASS loaded=false missing-file-fallback=true settings=0/50/100 persisted=true");
        }
    }

    private void ShutdownMusic()
    {
        if (_music is null) return;
        _music.Stop();
        AudioStream? stream = _music.Stream;
        _music.Stream = null;
        stream?.Dispose();
    }

    private async Task QuitAfterAudioDrain()
    {
        if (_quitting) return;
        _quitting = true;
        ShutdownMusic();
        // AudioServer releases stopped WAV playback on its next mixing buffers.
        await ToSignal(GetTree().CreateTimer(.25), Godot.Timer.SignalName.Timeout);
        GetTree().Quit();
    }
}
