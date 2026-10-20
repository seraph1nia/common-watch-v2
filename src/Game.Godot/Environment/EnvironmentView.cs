using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Reusable scene composition. Owns retained layers and bounded anchor emitters only.</summary>
public partial class EnvironmentView : Node2D
{
    private EnvironmentController _controller = null!;
    private AssetLoader _assets = null!;
    private EnvironmentScene _scene;
    private readonly EnvironmentAtmosphere _atmosphere = new();
    private readonly Dictionary<string, EnvironmentLayer> _layers = [];
    private readonly List<EnvironmentEmitter> _emitters = [];
    private AmbientParticles _particles = null!;
    private Rect2 _bounds, _distant;
    // Measured source-pixel centers on the three supplied 1024x384 backdrop strips.
    private static readonly Vector2[] FireAnchors = [new(350, 210), new(660, 210), new(380, 175)];
    private static readonly Vector2[] CrystalAnchors = [new(605, 235), new(485, 280), new(265, 240)];
    private Vector2 _emitterOrigin;
    private float _pitch;
    private int _sectors;
    private bool _built;
    public bool ParticlesEnabled { get; set; } = true;
    public bool EmittersEnabled { get; set; } = true;
    public bool LightingEnabled { get; set; } = true;
    public bool CanPresent => GetParent() is CanvasItem owner && owner.IsVisibleInTree() &&
        _bounds.HasArea() && (_scene == EnvironmentScene.Menu || _assets.Options.Mode == RenderMode.Sprites);
    public int ActiveLayerCount => _layers.Values.Count(l => l.IsVisibleInTree());
    public int EmitterCount => _emitters.Count;
    internal AmbientParticles Particles => _particles;
    public IEnumerable<EnvironmentLayer> Layers => _layers.Values;
    public void Initialize(EnvironmentController controller, AssetLoader assets, EnvironmentScene scene)
    {
        _controller = controller; _assets = assets; _scene = scene;
        SnapPreset();
    }
    public void SnapPreset() => _atmosphere.Approach(_controller.Preset(_scene), 1);
    public void SetArea(Rect2 bounds, Rect2 distant) { _bounds = bounds; _distant = distant; }
    public void SetArena(ArenaLayout layout)
    {
        _bounds = new Rect2(new Vector2(layout.Position.X, layout.Position.Y - layout.Pitch * .30f), new Vector2(layout.Size.X, layout.Size.Y + layout.Pitch * .6125f));
        _distant = new Rect2(_bounds.Position, new Vector2(_bounds.Size.X, layout.Pitch * .375f));
        if (_controller.Settings.Quality == EnvironmentQuality.Off || _assets.Options.Mode != RenderMode.Sprites) return;
        if (_pitch == layout.Pitch && _sectors == layout.Sectors.Length && _emitterOrigin == _distant.Position) return;
        _pitch = layout.Pitch; _sectors = layout.Sectors.Length; _emitterOrigin = _distant.Position;
        ClearEmitters();
        // Match each repeated variant's actual painted details; never move scenery itself.
        for (int i = 0; i < _sectors; i++)
        {
            int variant = ArenaPresentation.BackdropVariant(i) - 1;
            Vector2 origin = _distant.Position + new Vector2(i * _pitch, 0);
            AddEmitter(origin + FireAnchors[variant] * (_pitch / 1024), EnvironmentEmitterKind.Fire, _pitch * .075f);
            AddEmitter(origin + CrystalAnchors[variant] * (_pitch / 1024), EnvironmentEmitterKind.Crystal, _pitch * .04f);
        }
    }
    public void ClearEmitters()
    {
        foreach (var emitter in _emitters) { emitter.Hide(); emitter.QueueFree(); }
        _emitters.Clear();
    }
    public EnvironmentEmitter AddEmitter(Vector2 position, EnvironmentEmitterKind kind, float radius)
    {
        var emitter = new EnvironmentEmitter { Position = position, Kind = kind, Radius = radius, ZIndex = _scene == EnvironmentScene.Arena ? -4 : 2 };
        AddChild(emitter); emitter.Initialize(_controller, _assets); _emitters.Add(emitter); return emitter;
    }
    /// <summary>Register a new shader or supplied texture layer with this composition.</summary>
    public EnvironmentLayer AddLayer(string name, string shader, int depth, string? texture = null)
    {
        var layer = new EnvironmentLayer { Name = name, ZIndex = depth };
        AddChild(layer); layer.Initialize(_controller.Shader(shader));
        layer.EffectMaterial.SetShaderParameter("noise_texture", _controller.Noise);
        if (texture is not null)
        {
            layer.Texture = _assets.Load(texture);
            layer.EffectMaterial.SetShaderParameter("textured", layer.Texture is not null);
            if (layer.Texture is not null) layer.EffectMaterial.SetShaderParameter("repeat_texture", layer.Texture);
        }
        _layers.Add(name, layer); return layer;
    }
    private void BuildLayers()
    {
        _built = true;
        int rear = _scene == EnvironmentScene.Arena ? -5 : _scene == EnvironmentScene.Menu ? 0 : 1;
        int near = _scene == EnvironmentScene.Menu ? 0 : 2500;
        AddLayer("DistantHaze", "AtmosphericHaze", rear).EffectMaterial.SetShaderParameter("height_fade", 1f);
        var cloud = AddLayer("CloudShadows", "AtmosphericHaze", rear);
        cloud.EffectMaterial.SetShaderParameter("cloud_shadow", true); cloud.Tint = new(.08f, .12f, .19f);
        AddLayer("GroundMist", "AtmosphericHaze", near);
        AddLayer("Smoke", "ScrollingTexture", rear, "arena/smoke_repeat").EffectMaterial.SetShaderParameter("band_fade", true);
        AddLayer("Embers", "ScrollingTexture", near + (_scene == EnvironmentScene.Menu ? 0 : 1), "arena/embers_repeat");
        AddLayer("DistantRain", "ScrollingTexture", rear, "arena/rain_repeat");
        AddLayer("NearRain", "ScrollingTexture", near + (_scene == EnvironmentScene.Menu ? 0 : 1), "arena/rain_repeat");
        _particles = new AmbientParticles { ZIndex = _scene == EnvironmentScene.Menu ? 0 : 2502, Seed = _controller.Settings.Seed }; AddChild(_particles);
    }
    public void UpdateEnvironment(float time, float delta, bool sampled = false)
    {
        Visible = CanPresent && _controller.Settings.Quality != EnvironmentQuality.Off;
        if (!Visible) return;
        if (!_built) BuildLayers();
        _atmosphere.Approach(_controller.Preset(_scene), 1 - MathF.Exp(-Math.Min(delta, .1f) * .8f));
        float quality = _controller.Settings.Quality == EnvironmentQuality.Full ? 1 : .45f;
        float calm = _scene == EnvironmentScene.Menu ? .3f : 1;
        var a = _atmosphere;
        var haze = _layers["DistantHaze"]; haze.Bounds = _distant; haze.Opacity = a.Fog * .22f * quality * calm;
        haze.Velocity = a.Wind * .25f; haze.TextureScale = new(290, 140);
        var clouds = _layers["CloudShadows"]; clouds.Bounds = _bounds; clouds.Opacity = a.Clouds * .35f * quality * calm;
        clouds.Velocity = a.Wind; clouds.TextureScale = new(400, 210);
        var mist = _layers["GroundMist"]; mist.Bounds = new Rect2(_bounds.Position + new Vector2(0, _bounds.Size.Y * .48f), new Vector2(_bounds.Size.X, _bounds.Size.Y * .46f)); mist.Opacity = a.Fog * .18f * quality * calm;
        mist.Velocity = a.Wind * .45f; mist.TextureScale = new(340, 95);
        var smoke = _layers["Smoke"]; smoke.Bounds = _distant; smoke.Opacity = a.Smoke * .32f * quality * calm;
        smoke.Velocity = new(a.Wind.X * .5f, -8); smoke.TextureScale = new(240, 130);
        var embers = _layers["Embers"]; embers.Bounds = _scene == EnvironmentScene.Arena ? _distant : _bounds; embers.Opacity = a.Embers * .27f * quality * calm;
        embers.Velocity = new(a.Wind.X, -19); embers.TextureScale = new(410, 410);
        var rain = _layers["DistantRain"]; rain.Bounds = _bounds; rain.Opacity = a.Rain * .45f * quality * calm;
        rain.Velocity = new(a.Wind.X, 170); rain.TextureScale = new(400, 400);
        var near = _layers["NearRain"]; near.Bounds = _bounds; near.Opacity = quality < 1 ? 0 : a.Rain * .5f * calm;
        near.Velocity = new(a.Wind.X * 1.5f, 285); near.TextureScale = new(660, 660);
        foreach (var layer in _layers.Values) layer.UpdateLayer(time, sampled);
        _particles.Visible = ParticlesEnabled;
        _particles.Bounds = _bounds; _particles.Quality = quality * calm;
        _particles.Amount = a.Motes; _particles.Snow = a.Snow; _particles.Embers = a.Embers;
        _particles.Rain = a.Rain; _particles.Splashes = _scene == EnvironmentScene.Arena;
        _particles.Wind = a.Wind.X; _particles.UpdateMotion(time, sampled);
        _particles.Tint = a.ParticleTint; _particles.QueueRedraw();
        foreach (var emitter in _emitters) { emitter.Wind = a.Wind; emitter.UpdateEmitter(time, EmittersEnabled ? a.Glow * quality : 0); }
        QueueRedraw();
    }
    public override void _Draw()
    {
        if (!LightingEnabled || !Visible || _controller.Settings.Quality == EnvironmentQuality.Off) return;
        var visible = _bounds.Intersection(WorldArt.VisibleArea(this));
        if (visible.HasArea()) DrawRect(visible, _atmosphere.Tint with { A = _atmosphere.Tint.A * (_controller.Settings.Quality == EnvironmentQuality.Full ? 1 : .5f) });
    }
    public override void _ExitTree() { if (IsInstanceValid(_controller)) _controller.Detach(this); }
}
