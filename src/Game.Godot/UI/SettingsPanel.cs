using Godot;

namespace Game.GodotClient;

/// <summary>Native modal Controls; preferences never submit simulation commands.</summary>
public partial class SettingsPanel : Control
{
    private AudioController _audio = null!;
    private EnvironmentController _environment = null!;
    private OptionButton _effects = null!;
    private OptionButton _preset = null!;
    private Control? _previousFocus;
    public Control? FallbackFocus { private get; set; }
    public PanelContainer Panel => GetNode<PanelContainer>("Center/Panel");
    private const string Content = "Center/Panel/Margin/Content/";
    public HSlider Slider => GetNode<HSlider>(Content + "Slider");
    public Button CloseButton => GetNode<Button>(Content + "Close");
    public event Action? Opened, ReturnToBattleRequested;
    private Label _livePhase = null!;
    internal Button BattleButton { get; private set; } = null!;
    internal bool CoveredByMenu { get; set; }

    public void Bind(AudioController audio, Texture2D? background, EnvironmentController environment)
    {
        if (MenuBackground.Attach(this, background))
            GetNode<ColorRect>("Shade").Color = new Color(0, 0, 0, 0.25f);
        _environment = environment;
        _effects = GetNode<OptionButton>(Content + "EnvironmentEffects");
        foreach (string name in new[] { "Off", "Subtle", "Full" }) _effects.AddItem(name);
        _effects.Selected = (int)environment.Settings.Quality;
        _effects.ItemSelected += OnEffects;
        _environment.QualityChanged += RefreshEffects;
        _preset = GetNode<OptionButton>(Content + "WeatherPreset");
        _preset.AddItem("Scene default");
        foreach (var preset in Enum.GetValues<AtmospherePreset>()) _preset.AddItem(preset.ToString());
        RefreshPreset();
        _preset.ItemSelected += OnPreset;
        _environment.PresetChanged += RefreshPreset;
        _audio = audio;
        _audio.VolumeChanged += Refresh;
        Slider.SetValueNoSignal(audio.Settings.MusicVolume * 100);
        Slider.ValueChanged += OnVolume;
        CloseButton.Pressed += Close;
        var content = GetNode<VBoxContainer>("Center/Panel/Margin/Content");
        _livePhase = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart }; content.AddChild(_livePhase);
        BattleButton = new Button { Text = "Return to battle", CustomMinimumSize = new(0, 44) }; content.AddChild(BattleButton);
        BattleButton.Pressed += ReturnToBattle;
        SetLivePhase(false, "");
        Refresh();
    }
    private void ReturnToBattle() { Close(); ReturnToBattleRequested?.Invoke(); }
    public void SetLivePhase(bool live, string notice)
    {
        _livePhase.Text = notice; _livePhase.Visible = notice.Length > 0; BattleButton.Visible = live;
    }
    private void OnEffects(long selected) => _environment.SetQuality((EnvironmentQuality)selected);
    private void RefreshEffects() => _effects.Selected = (int)_environment.Settings.Quality;
    private void OnPreset(long selected) => _environment.SetPreset(selected == 0 ? null : (AtmospherePreset)(selected - 1), immediate: true, save: true);
    private void RefreshPreset() => _preset.Selected = _environment.Settings.Preset is { } preset ? (int)preset + 1 : 0;
    private void OnVolume(double percent) => _audio.SetVolume((float)percent / 100);
    private void Refresh()
    {
        GetNode<Label>(Content + "Volume").Text = $"Music volume: {_audio.Settings.MusicVolume * 100:0}%";
        GetNode<Label>(Content + "Status").Text = _audio.Status;
    }
    public void Open()
    {
        _previousFocus = GetViewport().GuiGetFocusOwner();
        Show();
        Slider.GrabFocus();
        Opened?.Invoke();
    }
    public void Close()
    {
        Hide();
        _audio.Settings.Save();
        if (IsInstanceValid(_previousFocus) && _previousFocus!.IsVisibleInTree()) _previousFocus.GrabFocus();
        else if (IsInstanceValid(FallbackFocus) && FallbackFocus!.IsVisibleInTree()) FallbackFocus.GrabFocus();
        _previousFocus = null;
    }
    public override void _Input(InputEvent input)
    {
        if (CoveredByMenu || !IsVisibleInTree() || !input.IsActionPressed("ui_cancel")) return;
        Close();
        GetViewport().SetInputAsHandled();
    }
    public override void _ExitTree()
    {
        if (_environment is not null)
        {
            _environment.QualityChanged -= RefreshEffects;
            _environment.PresetChanged -= RefreshPreset;
        }
        if (_preset is not null) _preset.ItemSelected -= OnPreset;
        if (_effects is not null) _effects.ItemSelected -= OnEffects;
        if (_audio is not null) _audio.VolumeChanged -= Refresh;
        Slider.ValueChanged -= OnVolume;
        CloseButton.Pressed -= Close;
        BattleButton.Pressed -= ReturnToBattle;
        Opened = ReturnToBattleRequested = null;
    }
}
