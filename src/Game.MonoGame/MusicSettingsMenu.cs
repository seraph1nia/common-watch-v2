using Game.Shared;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using MonoGameGum;

namespace Game.MonoGameClient;

internal sealed class MusicSettingsMenu
{
    readonly AudioSettings settings;
    readonly LoopingMusic music;
    readonly StackPanel panel;
    readonly Label heading, information;
    public Button OpenButton { get; }
    public Button CloseButton { get; }
    public Slider VolumeSlider { get; }
    public bool IsOpen { get; private set; }
    public int VolumeChanges { get; private set; }

    public MusicSettingsMenu(AudioSettings settings, LoopingMusic music)
    {
        this.settings = settings;
        this.music = music;
        OpenButton = new Button { Text = "Settings [F10]", Width = 175, Height = 32, Y = 15 };
        OpenButton.Visual.HeightUnits = DimensionUnitType.Absolute;
        OpenButton.AddToRoot();
        OpenButton.Click += (_, _) => SetOpen(!IsOpen);
        panel = new StackPanel { Width = 440, Height = 270, Spacing = 15 };
        panel.AddToRoot();
        heading = Text("SETTINGS", 65);
        VolumeSlider = new Slider { Width = 420, Height = 35, Minimum = 0, Maximum = 100, SmallChange = 1, LargeChange = 10, IsMoveToPointEnabled = true, Value = settings.MusicVolume * 100 };
        VolumeSlider.Visual.HeightUnits = DimensionUnitType.Absolute;
        panel.AddChild(VolumeSlider);
        information = Text(music.Status + "\nVolume is saved automatically. Zero mutes music.", 80);
        CloseButton = new Button { Text = "Close settings", Width = 420, Height = 35 };
        CloseButton.Visual.HeightUnits = DimensionUnitType.Absolute;
        panel.AddChild(CloseButton);
        CloseButton.Click += (_, _) => SetOpen(false);
        VolumeSlider.ValueChanged += (_, _) =>
        {
            VolumeChanges++;
            settings.MusicVolume = (float)VolumeSlider.Value / 100;
            music.Volume = settings.MusicVolume;
            bool saved = settings.Save();
            information.Text = music.Status + "\n" + (saved ? "Volume saved. Zero mutes music." : "Volume changed; settings file could not be saved.");
            UpdateHeading();
        };
        SetOpen(false);
        UpdateHeading();
    }

    Label Text(string text, float height)
    {
        var label = new Label { Text = text, Width = 420, Height = height };
        label.Visual.WidthUnits = DimensionUnitType.Absolute;
        label.Visual.HeightUnits = DimensionUnitType.Absolute;
        panel.AddChild(label);
        return label;
    }
    void UpdateHeading() => heading.Text = $"SETTINGS\nMusic volume: {Math.Round(VolumeSlider.Value)}%";
    public void SetOpen(bool open) { IsOpen = open; panel.Visual.Visible = open; }
    public void Layout(int width, int height)
    {
        OpenButton.X = width - 195;
        panel.X = (width - 440) / 2f + 10;
        panel.Y = (height - 320) / 2f + 20;
    }
    public void Draw(ShapeCanvas canvas, int width, int height)
    {
        if (!IsOpen) return;
        canvas.Rect(0, 112, width, height - 112, new Color(12, 19, 28));
        canvas.Rect((width - 480) / 2f, (height - 320) / 2f, 480, 320, new Color(41, 54, 72));
    }
}
