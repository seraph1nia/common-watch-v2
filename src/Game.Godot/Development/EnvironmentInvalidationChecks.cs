using System.Runtime.InteropServices;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    [DllImport("libX11.so.6")] private static extern nint XOpenDisplay(nint name);
    [DllImport("libX11.so.6")] private static extern int XSetInputFocus(nint display, nuint window, int revertTo, nuint time);
    [DllImport("libX11.so.6")] private static extern int XCloseDisplay(nint display);

    private static void FocusEnvironmentWindow(Window window)
    {
        window.GrabFocus();
        if (DisplayServer.GetName() != "X11") return;
        Require(!window.IsEmbedded(), "native focus probe uses a separate operating-system window");
        nint display = XOpenDisplay(0);
        Require(display != 0, "native focus probe connects to its owned X display");
        try { XSetInputFocus(display, (nuint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle, window.GetWindowId()), 2, 0); }
        finally { XCloseDisplay(display); }
    }

    private async Task CheckEnvironmentInvalidation(string directory)
    {
        var composition = _battlefield.Atmosphere;
        var layers = composition.Layers.ToArray();
        var anchor = composition.GetChildren().OfType<EnvironmentEmitter>().First(e => e.IsVisibleInTree());
        var originalKind = anchor.Kind;
        float originalRadius = anchor.Radius;
        anchor.Kind = EnvironmentEmitterKind.Smoke; anchor.Radius = 24;
        var emitters = composition.GetChildren().OfType<EnvironmentEmitter>().ToArray();
        CanvasItem[] items = [composition, composition.Particles, .. layers, .. emitters];
        var draws = new int[items.Length];
        var handlers = new Action[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            int index = i;
            handlers[i] = () => draws[index]++;
            items[i].Draw += handlers[i];
        }
        _environment.SetPreset(AtmospherePreset.Ember, immediate: true);
        _environment.Sample(null);
        await Settle();
        Require(_battle is { Paused: true } && GetWindow().HasFocus(), "invalidation regression starts paused and focused");
        double time = _environment.VisualTime;
        await Unchanged("paused live clock");
        Require(_environment.VisualTime == time, "paused invalidation check freezes time");

        bool embedded = GetTree().Root.GuiEmbedSubwindows;
        GetTree().Root.GuiEmbedSubwindows = false;
        var focus = new Window { Title = "Environment focus check", Size = new Vector2I(160, 100), Position = GetWindow().Position + new Vector2I(1100, 0) };
        AddChild(focus); focus.Show();
        try
        {
            await Settle();
            FocusEnvironmentWindow(focus);
            HandleBattleKey(Key.Space);
            await Settle();
            Require(!GetWindow().HasFocus() && _battle is { Paused: false }, "native focus loss suspends a playing showcase's atmosphere");
            time = _environment.VisualTime;
            await Unchanged("native focus loss");
            Require(_environment.VisualTime == time, "unfocused visual time stays fixed");
        }
        finally
        {
            _battle!.Paused = true;
            focus.Hide(); focus.QueueFree();
            GetTree().Root.GuiEmbedSubwindows = embedded;
            FocusEnvironmentWindow(GetWindow());
        }
        _environment.Sample(2);
        await Settle();
        byte[] reference = await Capture("static-reference");
        await Unchanged("fixed explicit sample");
        Require(Enumerable.SequenceEqual(reference, await Capture("static-repeat")), "static retained drawing preserves actual pixels");

        int[] layerDraws = draws.Skip(2).Take(layers.Length).ToArray();
        int[] parameters = Parameters();
        _environment.Sample(8);
        await Settle();
        Require(draws.Skip(2).Take(layers.Length).SequenceEqual(layerDraws), "time changes shader phase without rebuilding layer geometry");
        Require(!Parameters().SequenceEqual(parameters), "explicit resampling updates retained shader parameters");
        Require(!Enumerable.SequenceEqual(reference, await Capture("static-resampled")), "explicit resampling redraws frozen particles and smoke and animates glow");
        await Unchanged("resampled static frame");
        _environment.Sample(2);
        await Settle();
        Require(Enumerable.SequenceEqual(reference, await Capture("static-restored")), "retained drawing reproduces earlier samples exactly");

        int[] beforeCamera = draws.ToArray();
        for (int i = 0; i < 8; i++) _camera.ZoomAt(WorldArea.GetCenter(), true);
        _camera.SetPose(_camera.Magnification, _camera.PanPixels + new Vector2(90, 45));
        await Settle();
        Require(draws[0] > beforeCamera[0] && draws[1] > beforeCamera[1] && draws.Skip(2).Take(layers.Length).Where((count, i) => count > beforeCamera[i + 2]).Any(), "paused native pan/zoom invalidates clipped tint, particles and layer geometry");
        await Capture("static-pan-zoom");
        await Unchanged("stationary pan/zoom pose");
        Vector2I size = GetWindow().Size;
        int[] beforeResize = draws.ToArray();
        GetWindow().Size = new Vector2I(1920, 1080);
        await Settle();
        Require(!draws.SequenceEqual(beforeResize), "native resize invalidates retained clipping");
        await Capture("static-resized");
        await Unchanged("stationary resized frame");
        GetWindow().Size = size;
        _arenaCameraKey = ""; RefreshWorld(); _camera.Reset();
        await Settle();

        var near = layers.Single(l => l.Name == "NearRain");
        await Changed(() => near.Enabled = false, "layer-disabled");
        await Changed(() => near.Enabled = true, "layer-enabled");
        await Changed(() => near.Strength = .3f, "layer-strength");
        await Changed(() => near.Tint = new Color(.5f, .7f, 1), "layer-tint");
        await Changed(() => near.SpeedScale = -.5f, "layer-speed");
        near.Strength = 1; near.Tint = Colors.White; near.SpeedScale = 1;
        await Settle();
        await Changed(() => composition.ParticlesEnabled = false, "particles-disabled");
        await Changed(() => composition.ParticlesEnabled = true, "particles-enabled");
        await Changed(() => composition.EmittersEnabled = false, "emitters-disabled");
        await Changed(() => composition.EmittersEnabled = true, "emitters-enabled");
        await Changed(() => composition.LightingEnabled = false, "lighting-disabled");
        await Changed(() => composition.LightingEnabled = true, "lighting-enabled");
        await Changed(() => _environment.SetQuality(EnvironmentQuality.Subtle, save: false), "quality-subtle");
        await Changed(() => _environment.SetQuality(EnvironmentQuality.Off, save: false), "quality-off");
        await Changed(() => _environment.SetQuality(EnvironmentQuality.Full, save: false), "quality-full");
        await Changed(() => _environment.SetPreset(AtmospherePreset.Night, immediate: true), "preset-night");
        await Changed(() => _environment.SetPreset(AtmospherePreset.Ember, immediate: true), "preset-ember");
        _options = _options with { Mode = Game.Shared.RenderMode.Debug };
        RefreshWorld(); await Settle();
        Require(!composition.IsVisibleInTree(), "debug fallback hides retained environment");
        await Unchanged("hidden debug composition");
        _options = _options with { Mode = Game.Shared.RenderMode.Sprites };
        RefreshWorld(); await Settle();
        Require(Enumerable.SequenceEqual(reference, await Capture("static-shown-again")), "hidden composition restores retained rendering without stale state");
        anchor.Kind = originalKind; anchor.Radius = originalRadius;
        await Settle();
        await Unchanged("restored fire/crystal emitter");
        for (int i = 0; i < items.Length; i++) items[i].Draw -= handlers[i];
        GD.Print("GODOT_ENVIRONMENT_INVALIDATION_PASS paused focus sampled pan zoom resize quality preset controls hidden");

        int[] Parameters() => layers.Select(l => l.ShaderUpdates).Concat(emitters.Select(e => e.ShaderUpdates)).ToArray();
        async Task Settle()
        {
            for (int i = 0; i < 3; i++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
        }
        async Task Unchanged(string check)
        {
            int[] before = draws.ToArray(), shaders = Parameters();
            await Settle();
            Require(draws.SequenceEqual(before) && Parameters().SequenceEqual(shaders), "no unchanged draw callbacks or shader submissions: " + check);
        }
        async Task<byte[]> Capture(string name)
        {
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(System.IO.Path.Combine(directory, "invalidation-" + name + ".png")) == Error.Ok, "invalidation viewport capture");
            return image.GetData();
        }
        async Task Changed(Action change, string check)
        {
            byte[] before = await Capture(check + "-before");
            change(); await Settle();
            Require(!Enumerable.SequenceEqual(before, await Capture(check)), "static controls invalidate actual pixels: " + check);
            await Unchanged(check);
        }
    }
}
