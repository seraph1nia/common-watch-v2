using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private AudioController _audio = null!;
    private SettingsPanel _settings = null!;
    private Button _settingsButton = null!;
    private bool _quitting, _closeSubscribed;
    private void InitializeMusic()
    {
        _audio = GetNode<AudioController>("Audio");
        _audio.Initialize(Argument("--settings-path") ?? ProjectSettings.GlobalizePath("user://audio-settings.json"),
            Argument("--audio-root") ?? AssetDirectory("audio"));
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += RequestQuit; _closeSubscribed = true;
    }
    private void RequestQuit() => _ = QuitAfterAudioDrain();
    private void CreateAudioUi()
    {
        var layer = new CanvasLayer { Layer = 5 }; AddChild(layer);
        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Theme = _skin.Theme };
        layer.AddChild(root); root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _settings = GD.Load<PackedScene>("res://Scenes/UI/SettingsPanel.tscn").Instantiate<SettingsPanel>();
        root.AddChild(_settings); _settings.Bind(_audio);
        _settings.Opened += StopCameraDrag;
        _settingsButton = _skin.Button("Settings", _settings.Open);
        root.AddChild(_settingsButton);
        _settingsButton.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        _settingsButton.OffsetLeft = -130; _settingsButton.OffsetRight = -14;
        _settingsButton.OffsetTop = 12; _settingsButton.OffsetBottom = 48;
        _settings.FallbackFocus = _settingsButton;
        root.MoveChild(_settings, -1);
    }
    private void StopCameraDrag() => _camera.StopDrag();
    private async Task QuitAfterAudioDrain()
    {
        if (_quitting) return;
        _quitting = true;
        _sessions.Close();
        _audio.Stop();
        // AudioServer releases stopped WAV playback on its next mixing buffers.
        await ToSignal(GetTree().CreateTimer(.25), Godot.Timer.SignalName.Timeout);
        GetTree().Quit();
    }
}
