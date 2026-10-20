using System.Diagnostics;
using System.Text.Json;
using Game.Core;
using Game.Shared;
using Godot;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task CaptureEnvironment(string directory)
    {
        directory = System.IO.Path.GetFullPath(directory); System.IO.Directory.CreateDirectory(directory);
        var receipts = new List<object>();
        _frozen = true;
        if (_battleLayer is not null) _battleLayer.Hide();
        await CheckEnvironmentRegressions(directory);
        foreach (int players in new[] { 1, 6 })
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
        foreach (var view in new[] { ViewPreset.Topdown, ViewPreset.Angled })
        {
            _battle = new("arena", players) { Paused = true }; SeekBattle(74);
            GetWindow().Size = size; _options = _options with { Mode = RenderMode.Sprites, View = view };
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            _arenaCameraKey = ""; RefreshWorld(); _gameUi.Hide();
            foreach (bool overview in new[] { false, true })
            {
                if (overview) OverviewArena(); else _camera.Reset();
                string prefix = $"arena-{players}-{size.X}-{view}-{(overview ? "overview" : "local")}";
                double tick = _battlefield.Playback.Tick;
                _environment.SetPreset(AtmospherePreset.Ember, immediate: true);
                _environment.Sample(2);
                _environment.SetQuality(EnvironmentQuality.Off, save: false);
                byte[] off = await Capture(prefix + "-off");
                _environment.SetQuality(EnvironmentQuality.Subtle, save: false);
                byte[] subtle = await Capture(prefix + "-subtle");
                _environment.SetQuality(EnvironmentQuality.Full, save: false);
                byte[] full = await Capture(prefix + "-full-t2");
                _environment.Sample(8);
                byte[] later = await Capture(prefix + "-full-t8");
                Require(!off.SequenceEqual(full) && !subtle.SequenceEqual(full) && !full.SequenceEqual(later), "quality and sampled ambient animation change actual pixels");
                _environment.Sample(2);
                byte[] repeated = await Capture(prefix + "-repeat-t2");
                Require(full.SequenceEqual(repeated), "explicit time is reproducible including pause");
                _environment.SetQuality(EnvironmentQuality.Off, save: false);
                byte[] offAgain = await Capture(prefix + "-off-again");
                Require(off.SequenceEqual(offAgain), "Off restores original rendering after effects were active");
                Require(_battlefield.Playback.Tick == tick, "weather sampling never advances combat playback");
                // Whole-client measured frame durations/allocations, with a fixed legal paused battle.
                foreach (var quality in new[] { EnvironmentQuality.Off, EnvironmentQuality.Full })
                {
                    _environment.SetQuality(quality, save: false); RefreshWorld();
                    for (int i = 0; i < 6; i++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    int decodes = _assets.DecodeAttempts;
                    long allocated = GC.GetTotalAllocatedBytes();
                    var durations = new double[30];
                    var watch = Stopwatch.StartNew();
                    for (int i = 0; i < durations.Length; i++)
                    {
                        _environment.Sample(2 + i / 60.0);
                        double start = watch.Elapsed.TotalMilliseconds;
                        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                        durations[i] = watch.Elapsed.TotalMilliseconds - start;
                    }
                    long bytes = GC.GetTotalAllocatedBytes() - allocated;
                    Require(_assets.DecodeAttempts == decodes, "no texture decoding during steady ambient animation");
                    Array.Sort(durations);
                    receipts.Add(new { scene = prefix, quality = quality.ToString(), medianMs = durations[15], p95Ms = durations[28], allocatedBytesPerFrame = bytes / 30, textureDecodes = _assets.DecodeAttempts - decodes });
                }
                GD.Print($"GODOT_ENVIRONMENT_FRAME_PASS {prefix}");
            }
        }
        // Isolate every retained layer: prove that each shader animates independently.
        var composition = _battlefield.Atmosphere;
        composition.ParticlesEnabled = false; composition.EmittersEnabled = false; composition.LightingEnabled = false;
        foreach (var layer in composition.Layers) layer.Enabled = false;
        foreach (var layer in composition.Layers)
        {
            _environment.SetPreset(layer.Name == "CloudShadows" ? AtmospherePreset.Clear : AtmospherePreset.Ember, immediate: true);
            layer.Enabled = true;
            _environment.Sample(2); byte[] first = await Capture("isolated-" + layer.Name + "-t2");
            _environment.Sample(12); byte[] later = await Capture("isolated-" + layer.Name + "-t12");
            Require(!first.SequenceEqual(later), "independent layer animates: " + layer.Name);
            layer.Enabled = false;
        }
        foreach (var layer in composition.Layers) layer.Enabled = true;
        composition.ParticlesEnabled = true; composition.EmittersEnabled = true; composition.LightingEnabled = true;
        _environment.SetPreset(AtmospherePreset.Ember, immediate: true);
        composition.UpdateEnvironment(2, 0, sampled: true);
        var nearRain = composition.Layers.Single(l => l.Name == "NearRain");
        float oldRain = nearRain.Opacity;
        _environment.SetPreset(AtmospherePreset.Night);
        composition.UpdateEnvironment(2, .1f, sampled: true);
        Require(nearRain.Opacity > 0 && nearRain.Opacity < oldRain, "preset blending approaches the new weather smoothly");
        _environment.SetPreset(AtmospherePreset.Ember, immediate: true);
        // Technical shader swatches use an existing texture, not fabricated gameplay/water regions.
        foreach (string shader in new[] { "WindSway", "WaterRipple", "DistanceBlur" })
        {
            using var material = new ShaderMaterial { Shader = _environment.Shader(shader) };
            var swatch = new Sprite2D { Texture = _assets.Load("arena/crystal_glow"), Material = material, Scale = Vector2.One * .3f, ZIndex = 4000 };
            _battlefield.AddChild(swatch);
            material.SetShaderParameter("visual_time", 8f);
            await Capture("shader-swatch-" + shader);
            swatch.QueueFree();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        _battle = null; _battleLayer?.Hide();
        ReplaceSession(ScenarioRunner.CreateAt("sprite-settlement")); _frozen = true; RefreshUi(true); RefreshWorld();
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(1920, 1080) })
        {
            GetWindow().Size = size;
            foreach (var preset in Enum.GetValues<AtmospherePreset>())
            {
                _environment.SetQuality(EnvironmentQuality.Full, save: false); _environment.SetPreset(preset, immediate: true);
                _environment.Sample(2); byte[] first = await Capture($"settlement-{size.X}-{preset}-t2");
                _environment.Sample(12); byte[] second = await Capture($"settlement-{size.X}-{preset}-t12");
                Require(!first.SequenceEqual(second), "settlement atmosphere moves: " + preset);
                for (int slot = 0; slot < 9; slot++) Require(_settlementView.Pick(_camera.ToWorld(_camera.ToScreen(_settlementView.PlotPosition(slot)))) == slot, "effects preserve all nine ellipse picks");
            }
        }
        ReturnToMenu(); _environment.SetPreset(AtmospherePreset.Enchanted, immediate: true);
        _environment.Sample(2); byte[] menu = await Capture("menu-t2");
        _environment.Sample(12); byte[] menuLater = await Capture("menu-t12"); Require(!menu.SequenceEqual(menuLater), "menu atmosphere moves behind native controls");
        Require(!_settlementView.Visible && !_battlefield.Visible && !_arenaPreview.Visible, "no stale world scene after returning to menu");
        // Actual missing-pack/debug runs are performed by the wrapper in separate clients.
        System.IO.File.WriteAllText(System.IO.Path.Combine(directory, "performance.json"), JsonSerializer.Serialize(receipts, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print("GODOT_ENVIRONMENT_PASS animated deterministic off/subtle/full 1/6-sectors local/overview 720/1080 topdown/angled presets picking lifecycle cache");

        async Task<byte[]> Capture(string name)
        {
            RefreshWorld(); await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using var image = GetViewport().GetTexture().GetImage();
            Require(image.SavePng(System.IO.Path.Combine(directory, name + ".png")) == Error.Ok, "environment viewport capture");
            return image.GetData();
        }
    }
}
