using Godot;

namespace Game.GodotClient;

/// <summary>One visual clock and resource owner. Never observes or changes simulation rules.</summary>
public partial class EnvironmentController : Node
{
    private readonly Dictionary<AtmospherePreset, EnvironmentPreset> _presets = [];
    private readonly Dictionary<string, Shader> _shaders = [];
    private readonly List<EnvironmentView> _views = [];
    private AssetLoader _assets = null!;
    private string _settingsPath = "";
    private double _seconds;
    private readonly FastNoiseLite _noiseSource = new() { Seed = 417, Frequency = .035f };
    public NoiseTexture2D Noise { get; } = new() { Width = 256, Height = 128, Seamless = true, GenerateMipmaps = false };
    public EnvironmentSettings Settings { get; private set; } = new();
    public bool Suspended { get; set; }
    public bool HasVisibleEffects { get; private set; }
    public double VisualTime => Settings.SampleTime ?? _seconds;
    public event Action? QualityChanged;

    public void Initialize(AssetLoader assets, string[] args, string settingsPath)
    {
        _assets = assets; _settingsPath = settingsPath;
        Settings = EnvironmentSettings.Parse(args, EnvironmentSettings.LoadQuality(settingsPath));
        if (Settings.Quality != EnvironmentQuality.Off) Noise.Noise = _noiseSource;
        foreach (var preset in Enum.GetValues<AtmospherePreset>())
            _presets[preset] = GD.Load<EnvironmentPreset>($"res://Environment/Presets/{preset.ToString().ToLowerInvariant()}.tres");
    }
    public EnvironmentView Attach(Node2D owner, EnvironmentScene scene)
    {
        var view = new EnvironmentView(); owner.AddChild(view);
        view.Initialize(this, _assets, scene); _views.Add(view); return view;
    }
    public void Detach(EnvironmentView view) => _views.Remove(view);
    public EnvironmentPreset Preset(EnvironmentScene scene) => _presets[Settings.Preset ?? (scene switch
    {
        EnvironmentScene.Arena => AtmospherePreset.Ember,
        EnvironmentScene.Settlement => AtmospherePreset.Clear,
        _ => AtmospherePreset.Enchanted
    })];
    public Shader Shader(string name)
    {
        if (!_shaders.TryGetValue(name, out var shader))
            _shaders[name] = shader = GD.Load<Shader>($"res://Environment/Shaders/{name}.gdshader");
        return shader;
    }
    public void SetQuality(EnvironmentQuality quality, bool save = true)
    {
        if (!Enum.IsDefined(quality)) throw new ArgumentOutOfRangeException(nameof(quality));
        Settings = Settings with { Quality = quality };
        if (quality != EnvironmentQuality.Off && Noise.Noise is null) Noise.Noise = _noiseSource;
        if (save && !EnvironmentSettings.SaveQuality(_settingsPath, quality)) GD.PushWarning("Environment quality applies for this session; preferences could not be saved.");
        // Off is immediate, including the legacy static arena rain fallback.
        foreach (var view in _views) view.Visible = quality != EnvironmentQuality.Off;
        QualityChanged?.Invoke();
    }
    public void SetPreset(AtmospherePreset? preset, bool immediate = false)
    {
        Settings = Settings with { Preset = preset };
        if (immediate) foreach (var view in _views) view.SnapPreset();
    }
    public void Sample(double? seconds)
    {
        if (seconds is { } t && (!double.IsFinite(t) || t < 0 || t > 86400)) throw new ArgumentOutOfRangeException(nameof(seconds));
        Settings = Settings with { SampleTime = seconds };
    }
    public override void _Process(double delta)
    {
        if (_assets is null) return;
        HasVisibleEffects = false;
        foreach (var view in _views)
            if (view.CanPresent && Settings.Quality != EnvironmentQuality.Off) { HasVisibleEffects = true; break; }
        // Captures sample explicit time. Hidden/inactive/unfocused scenes do no animation work.
        bool paused = Suspended || !GetWindow().HasFocus();
        if (HasVisibleEffects && !paused && Settings.SampleTime is null) _seconds += Math.Min(delta, .1);
        foreach (var view in _views) view.UpdateEnvironment((float)VisualTime, paused ? 0 : (float)delta);
    }
    public override void _ExitTree()
    {
        QualityChanged = null;
        foreach (var shader in _shaders.Values) shader.Dispose();
        foreach (var preset in _presets.Values) preset.Dispose();
        Noise.Dispose(); _noiseSource.Dispose();
        _views.Clear();
    }
}
