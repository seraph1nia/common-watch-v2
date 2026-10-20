using Godot;

namespace Game.GodotClient;

/// <summary>Native modal Controls; preferences never submit simulation commands.</summary>
public partial class SettingsPanel : Control
{
    private AudioController _audio = null!;
    private EnvironmentController _environment = null!;
    private OptionButton _effects = null!;
    private Control? _previousFocus;
    public Control? FallbackFocus { private get; set; }
    public PanelContainer Panel => GetNode<PanelContainer>("Center/Panel");
    private const string Content = "Center/Panel/Margin/Content/";
    public HSlider Slider => GetNode<HSlider>(Content + "Slider");
    public Button CloseButton => GetNode<Button>(Content + "Close");
    public event Action? Opened;

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
        _audio = audio;
        _audio.VolumeChanged += Refresh;
        Slider.SetValueNoSignal(audio.Settings.MusicVolume * 100);
        Slider.ValueChanged += OnVolume;
        CloseButton.Pressed += Close;
        Refresh();
    }
    private void OnEffects(long selected) => _environment.SetQuality((EnvironmentQuality)selected);
    private void RefreshEffects() => _effects.Selected = (int)_environment.Settings.Quality;
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
        if (!IsVisibleInTree() || !input.IsActionPressed("ui_cancel")) return;
        Close();
        GetViewport().SetInputAsHandled();
    }
    public override void _ExitTree()
    {
        if (_environment is not null) _environment.QualityChanged -= RefreshEffects;
        if (_effects is not null) _effects.ItemSelected -= OnEffects;
        if (_audio is not null) _audio.VolumeChanged -= Refresh;
        Slider.ValueChanged -= OnVolume;
        CloseButton.Pressed -= Close;
        Opened = null;
    }
}
