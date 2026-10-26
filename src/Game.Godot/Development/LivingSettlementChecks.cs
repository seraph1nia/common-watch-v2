using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckLivingSettlementMap(string directory)
    {
        // Isolate map pixels from weather, UI timers and simulation advancement.
        var settings = _environment.Settings;
        var atmosphere = _settlementView.Atmosphere;
        bool particles = atmosphere.ParticlesEnabled, emitters = atmosphere.EmittersEnabled,
            lighting = atmosphere.LightingEnabled;
        var layers = new List<(EnvironmentLayer Layer, bool Enabled)>();
        try
        {
            _environment.SetQuality(EnvironmentQuality.Full, save: false);
            _environment.Sample(2);
            await Capture("warmup");
            layers.AddRange(atmosphere.Layers.Select(l => (l, l.Enabled)));
            atmosphere.ParticlesEnabled = false;
            atmosphere.EmittersEnabled = false;
            atmosphere.LightingEnabled = false;
            foreach (var (layer, _) in layers) layer.Enabled = false;
            byte[] first = await Capture("t2");
            _environment.Sample(2.1);
            byte[] held = await Capture("held");
            Require(first.SequenceEqual(held), "painted map holds frames at eight fps");
            _environment.Sample(3);
            byte[] later = await Capture("t3");
            Require(!first.SequenceEqual(later), "painted map moves independently of atmosphere");
            _environment.Sample(10);
            byte[] loop = await Capture("loop");
            Require(first.SequenceEqual(loop), "painted map closes its eight-second loop exactly");
            _environment.SetQuality(EnvironmentQuality.Off, save: false);
            byte[] off = await Capture("off");
            _environment.Sample(4);
            byte[] offLater = await Capture("off-later");
            Require(off.SequenceEqual(offLater), "Off leaves the painted map static");
            Require(!off.SequenceEqual(first), "painted map animation changes source pixels");
            GD.Print("GODOT_LIVING_MAP_PASS isolated-motion held-8fps loop-8seconds static-off");
        }
        finally
        {
            foreach (var (layer, enabled) in layers) layer.Enabled = enabled;
            atmosphere.ParticlesEnabled = particles;
            atmosphere.EmittersEnabled = emitters;
            atmosphere.LightingEnabled = lighting;
            _environment.SetQuality(settings.Quality, save: false);
            _environment.Sample(settings.SampleTime);
        }

        async Task<byte[]> Capture(string name)
        {
            RefreshWorld();
            // Allow the shared clock, backdrop material and composition to settle.
            for (int i = 0; i < 3; i++)
            {
                await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
            System.IO.Directory.CreateDirectory(directory);
            using var image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(System.IO.Path.Combine(directory, "living-map-" + name + ".png")) == Error.Ok,
                "living settlement capture");
            return image.GetData();
        }
    }
}
