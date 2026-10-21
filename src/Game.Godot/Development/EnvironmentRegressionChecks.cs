using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CheckEnvironmentRegressions(string directory)
    {
        _environment.SetQuality(EnvironmentQuality.Full, save: false);
        _environment.SetPreset(AtmospherePreset.Ember, immediate: true);
        SeekBattle(74);
        _environment.Sample(null);
        GetWindow().GrabFocus();
        await Frames(3);
        Require(GetWindow().HasFocus() && _frozen && _battle is { Paused: true }, "live-clock regression starts with a frozen backing checkpoint and focused showcase");
        HandleBattleKey(Key.Space);
        await Frames(2);
        double time = _environment.VisualTime, tick = _battlefield.Playback.Tick;
        await Frames(12);
        Require(_environment.VisualTime > time && _battlefield.Playback.Tick > tick, "playing showcase advances live ambient and combat clocks with Sample(null)");
        HandleBattleKey(Key.Space);
        await Frames(2);
        time = _environment.VisualTime; tick = _battlefield.Playback.Tick;
        await Frames(12);
        Require(_environment.VisualTime == time && _battlefield.Playback.Tick == tick, "showcase pause freezes both clocks");
        HandleBattleKey(Key.Space);
        await Frames(2);
        time = _environment.VisualTime;
        await Frames(6);
        Require(_environment.VisualTime > time, "showcase unpause resumes ambient time");
        HandleBattleKey(Key.Space);
        await Frames(2);
        CheckEnvironmentMotion();
        await CheckShaderIdentity(directory);
        await CheckEnvironmentInvalidation(directory);
        GD.Print("GODOT_ENVIRONMENT_REGRESSION_PASS live-clock pause shader-identity integrated-wind");

        async Task Frames(int count)
        {
            for (int i = 0; i < count; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
    }

    private void CheckEnvironmentMotion()
    {
        var composition = _battlefield.Atmosphere;
        var layers = composition.Layers.ToArray();
        var particles = composition.Particles;
        _environment.Sample(600);
        _environment.SetPreset(AtmospherePreset.Clear, immediate: true);
        composition.UpdateEnvironment(600, 0, sampled: true);
        composition.UpdateEnvironment(600, 0);
        float time = 600;
        var fallSpeeds = new HashSet<int>();
        foreach (var preset in new[] { AtmospherePreset.Rain, AtmospherePreset.Snow, AtmospherePreset.Clear })
        {
            _environment.SetPreset(preset);
            for (int frame = 0; frame < 480; frame++)
            {
                var previous = layers.Select(l => l.MotionOffset).ToArray();
                Vector2 particleOffset = particles.MotionOffset;
                float snowOffset = particles.SnowOffset;
                foreach (var layer in layers) layer.SpeedScale = frame < 60 ? 1 : -.5f;
                float next = time + 1f / 60;
                composition.UpdateEnvironment(next, next - time);
                for (int i = 0; i < layers.Length; i++)
                {
                    var layer = layers[i];
                    Vector2 expected = previous[i] + layer.Velocity * layer.SpeedScale * (next - time);
                    Require(layer.MotionOffset.DistanceTo(expected) < .001f, "both motion axes integrate velocity and speed changes without rebasing: " + layer.Name);
                }
                bool rain = particles.Rain > .01f, snow = !rain && particles.Snow > .01f;
                int fallSpeed = rain ? 185 : snow ? 17 : -4;
                fallSpeeds.Add(fallSpeed);
                Vector2 particleVelocity = new(particles.Wind, fallSpeed);
                Require(particles.MotionOffset.DistanceTo(particleOffset + particleVelocity * (next - time)) < .001f, "particle wind and fall/rise integrate through late-time preset reversals");
                Require(Math.Abs(particles.SnowOffset - (snowOffset + (snow ? 20 : 0) * (next - time))) < .001f, "varied snow speeds preserve vertical phase");
                time = next;
            }
        }
        Require(fallSpeeds.SetEquals([-4, 17, 185]), "motion regression exercises rain, snow and rising mote speeds");
        foreach (var layer in layers) layer.SpeedScale = 1;
        _environment.SetPreset(AtmospherePreset.Ember, immediate: true);
        composition.UpdateEnvironment(8, 0, sampled: true);
        var sampled = layers.Select(l => l.MotionOffset).ToArray();
        Vector2 sampledParticles = particles.MotionOffset;
        float sampledSnow = particles.SnowOffset;
        composition.UpdateEnvironment(2, 0, sampled: true);
        composition.UpdateEnvironment(8, 0, sampled: true);
        Require(layers.Select(l => l.MotionOffset).SequenceEqual(sampled) && particles.MotionOffset == sampledParticles && particles.SnowOffset == sampledSnow, "explicit motion samples are independent of history");
        composition.UpdateEnvironment(1, 0);
        Require(layers.Select(l => l.MotionOffset).SequenceEqual(sampled) && particles.MotionOffset == sampledParticles && particles.SnowOffset == sampledSnow, "resuming a differently based live clock preserves sampled phase");
        composition.UpdateEnvironment(1, 0);
        Require(layers.Select(l => l.MotionOffset).SequenceEqual(sampled) && particles.MotionOffset == sampledParticles && particles.SnowOffset == sampledSnow, "stationary time freezes integrated motion");
    }

    private async Task CheckShaderIdentity(string directory)
    {
        using var world = new World2D();
        var viewport = new SubViewport
        {
            Size = new Vector2I(256, 256), TransparentBg = true, World2D = world,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        AddChild(viewport);
        foreach (string textureName in new[] { "fire_glow", "crystal_glow" })
        foreach (string shaderName in new[] { "SoftGlow", "WaterRipple", "DistanceBlur" })
        {
            var texture = _assets.Load("arena/" + textureName);
            Require(texture is not null, "identity swatch uses supplied translucent artwork");
            using var baseline = new CanvasItemMaterial { BlendMode = shaderName == "SoftGlow" ? CanvasItemMaterial.BlendModeEnum.Add : CanvasItemMaterial.BlendModeEnum.Mix };
            using var effect = new ShaderMaterial { Shader = _environment.Shader(shaderName) };
            effect.SetShaderParameter("pulse_amplitude", 0f);
            effect.SetShaderParameter("ripple_strength", 0f);
            effect.SetShaderParameter("highlight_strength", 0f);
            effect.SetShaderParameter("blur_strength", 0f);
            effect.SetShaderParameter("distance_fade", 0f);
            Vector2 size = texture!.GetSize();
            var swatch = new Polygon2D
            {
                Polygon = [new(24, 24), new(232, 24), new(232, 232), new(24, 232)],
                UV = [Vector2.Zero, new(size.X, 0), size, new(0, size.Y)],
                VertexColors = [new(.9f, .6f, .8f, .7f), new(.5f, .9f, .7f, .5f), new(.8f, .5f, .9f, .8f), new(.6f, .8f, .5f, .6f)],
                Texture = texture, Material = baseline,
                Modulate = new(.73f, .5f, .84f, .62f), SelfModulate = new(.6f, .92f, .7f, .71f)
            };
            viewport.AddChild(swatch);
            using var expected = await Frame();
            swatch.Material = effect;
            using var actual = await Frame();
            byte[] a = expected.GetData(), b = actual.GetData();
            Require(a.Any(value => value > 8) && a.Where((_, i) => i % 4 == 3).Any(alpha => alpha > 0 && alpha < 255), "identity reference contains visible translucent pixels");
            Require(a.Length == b.Length && a.Zip(b).All(pair => Math.Abs(pair.First - pair.Second) <= 1), "neutral shader preserves supplied texture, vertex tint, modulation and alpha exactly once: " + shaderName + "/" + textureName);
            string prefix = System.IO.Path.Combine(directory, "identity-" + shaderName + "-" + textureName);
            Require(expected.SavePng(prefix + "-reference.png") == Error.Ok && actual.SavePng(prefix + "-shader.png") == Error.Ok, "shader identity captures");
            swatch.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        viewport.QueueFree();
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        async Task<Image> Frame()
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            return viewport.GetTexture().GetImage();
        }
    }
}
