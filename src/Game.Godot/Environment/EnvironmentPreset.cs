using Godot;

namespace Game.GodotClient;

/// <summary>Authored resource: all scene-independent atmosphere composition lives here.</summary>
[GlobalClass]
public partial class EnvironmentPreset : Godot.Resource
{
    [Export] public Color Tint { get; set; } = new(.64f, .76f, .85f, .03f);
    [Export] public float Fog { get; set; } = .12f;
    [Export] public float Clouds { get; set; } = .15f;
    [Export] public float Rain { get; set; }
    [Export] public float Snow { get; set; }
    [Export] public float Smoke { get; set; }
    [Export] public float Embers { get; set; }
    [Export] public float Motes { get; set; } = .25f;
    [Export] public float Glow { get; set; } = .3f;
    [Export] public Color ParticleTint { get; set; } = new("dfd9b0");
    [Export] public Vector2 Wind { get; set; } = new(7, 0);
}

/// <summary>Mutable blended values; no transient Resource allocations during transitions.</summary>
public sealed class EnvironmentAtmosphere
{
    public Color Tint, ParticleTint;
    public float Fog, Clouds, Rain, Snow, Smoke, Embers, Motes, Glow;
    public Vector2 Wind;
    public void Approach(EnvironmentPreset p, float weight)
    {
        Tint = Tint.Lerp(p.Tint, weight); ParticleTint = ParticleTint.Lerp(p.ParticleTint, weight);
        Fog = Mathf.Lerp(Fog, p.Fog, weight); Clouds = Mathf.Lerp(Clouds, p.Clouds, weight);
        Rain = Mathf.Lerp(Rain, p.Rain, weight); Snow = Mathf.Lerp(Snow, p.Snow, weight);
        Smoke = Mathf.Lerp(Smoke, p.Smoke, weight); Embers = Mathf.Lerp(Embers, p.Embers, weight);
        Motes = Mathf.Lerp(Motes, p.Motes, weight); Glow = Mathf.Lerp(Glow, p.Glow, weight);
        Wind = Wind.Lerp(p.Wind, weight);
    }
}
