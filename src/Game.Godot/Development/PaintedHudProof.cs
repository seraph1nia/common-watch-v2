using Godot;

namespace Game.GodotClient;

/// <summary>CLI-only painting proof; no game session or invented simulation snapshot.</summary>
public partial class PaintedHudProof : Control
{
    public override async void _Ready()
    {
        var art = new PaintedHudArt();
        bool wide = GetViewportRect().Size.X >= 1600;
        float width = wide ? 380 : 350;
        var background = new ColorRect { Color = new Color("23352e"), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(background); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var title = new Label { Text = "PAINTED HUD · component quality proof · native text", Position = new(32, 24) }; AddChild(title);
        var panel = new PanelContainer { Position = new(GetViewportRect().Size.X - width - 32, GetViewportRect().Size.Y - 350), Size = new(width, 300) };
        AddChild(panel); panel.AddThemeStyleboxOverride("panel", art.Frame());
        var body = new VBoxContainer(); panel.AddChild(body);
        body.AddChild(new Label { Text = "Wave · Preparation", HorizontalAlignment = HorizontalAlignment.Center });
        body.AddChild(new HSeparator());
        foreach (var text in new[] { "●  Preparation 1", "○  Preparation 2", "○  Preparation 3", "○  Combat" }) body.AddChild(new Label { Text = text });
        foreach (var text in new[] { "Readiness · your city", "Next consequence / feedback" })
        {
            var label = new Label { Text = text }; label.AddThemeFontSizeOverride("font_size", 14); body.AddChild(label);
        }
        var buttons = new HBoxContainer(); body.AddChild(buttons);
        foreach (var (id, text) in new[] { ("ready", "Ready"), ("pause", "Pause") })
        {
            var button = new Button { Text = text, CustomMinimumSize = new(0, 48), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            button.AddThemeStyleboxOverride("normal", new StyleBoxTexture { Texture = art.Texture(id+"_normal"), TextureMarginLeft = 22, TextureMarginRight = 22, TextureMarginTop = 16, TextureMarginBottom = 16 });
            buttons.AddChild(button);
        }
        art.Attach(panel, "squirrel", new(88, 88), new(1, 0), new(-44, 26), new(.5f, .95f));
        art.Attach(panel, "clock", new(50, 50), Vector2.Zero, new(24, 24), new(.5f, .5f));
        art.Attach(panel, "mushrooms-right", new(62, 76), Vector2.One, new(-6, -12));
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var args = OS.GetCmdlineUserArgs(); int at = Array.IndexOf(args, "--proof-output");
        if (at >= 0 && at + 1 < args.Length)
        {
            GetViewport().GetTexture().GetImage().SavePng(args[at + 1]);
            GetTree().Quit();
        }
    }
}
