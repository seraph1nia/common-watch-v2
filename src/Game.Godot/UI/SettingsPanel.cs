using Godot;

namespace Game.GodotClient;

/// <summary>Native modal Controls; preferences never submit simulation commands.</summary>
public partial class SettingsPanel : Control
{
    private AudioController _audio = null!;
    private Control? _previousFocus;
    public Control? FallbackFocus { private get; set; }
    public PanelContainer Panel => GetNode<PanelContainer>("Center/Panel");
    private const string Content = "Center/Panel/Margin/Content/";
    public HSlider Slider => GetNode<HSlider>(Content + "Slider");
    public Button CloseButton => GetNode<Button>(Content + "Close");
    public event Action? Opened;

    public void Bind(AudioController audio)
    {
        _audio = audio;
        _audio.VolumeChanged += Refresh;
        Slider.SetValueNoSignal(audio.Settings.MusicVolume * 100);
        Slider.ValueChanged += OnVolume;
        CloseButton.Pressed += Close;
        Refresh();
    }
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
    public override void _ExitTree()
    {
        if (_audio is not null) _audio.VolumeChanged -= Refresh;
        Slider.ValueChanged -= OnVolume;
        CloseButton.Pressed -= Close;
        Opened = null;
    }
}
