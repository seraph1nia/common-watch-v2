using System.Globalization;
using System.Text.Json;

namespace Game.GodotClient;

public enum EnvironmentQuality { Off, Subtle, Full }
public enum AtmospherePreset { Clear, Mist, Rain, Night, Enchanted, Ember, Snow }
public enum EnvironmentScene { Arena, Settlement, Menu }

/// <summary>Local presentation preferences, separate from audio and private reconnect data.</summary>
public sealed record EnvironmentSettings
{
    public EnvironmentQuality Quality { get; init; }
    public AtmospherePreset? Preset { get; init; }
    public int Seed { get; init; } = 417;
    public double? SampleTime { get; init; }

    public static EnvironmentSettings Parse(string[] args, EnvironmentQuality saved = EnvironmentQuality.Off, AtmospherePreset? savedPreset = null)
    {
        var result = new EnvironmentSettings { Quality = saved, Preset = savedPreset };
        string Value(ref int i) => ++i < args.Length ? args[i] : throw new ArgumentException("Missing environment option value.");
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--environment-effects":
                    result = result with { Quality = Value(ref i) switch { "off" => EnvironmentQuality.Off, "subtle" => EnvironmentQuality.Subtle, "full" => EnvironmentQuality.Full, _ => throw new ArgumentException("--environment-effects requires off|subtle|full") } }; break;
                case "--environment-preset":
                    result = result with { Preset = Value(ref i) switch
                    {
                        "clear" => AtmospherePreset.Clear,
                        "mist" => AtmospherePreset.Mist,
                        "rain" => AtmospherePreset.Rain,
                        "night" => AtmospherePreset.Night,
                        "enchanted" => AtmospherePreset.Enchanted,
                        "ember" => AtmospherePreset.Ember,
                        "snow" => AtmospherePreset.Snow,
                        _ => throw new ArgumentException("--environment-preset requires clear|mist|rain|night|enchanted|ember|snow")
                    } }; break;
                case "--environment-seed":
                    result = result with { Seed = int.Parse(Value(ref i), CultureInfo.InvariantCulture) }; break;
                case "--environment-time":
                    double time = double.Parse(Value(ref i), CultureInfo.InvariantCulture);
                    if (!double.IsFinite(time) || time < 0 || time > 86400) throw new ArgumentException("--environment-time requires 0..86400 seconds");
                    result = result with { SampleTime = time }; break;
            }
        }
        return result;
    }

    public static EnvironmentSettings Load(string path)
    {
        try
        {
            var value = JsonSerializer.Deserialize<EnvironmentSettings>(File.ReadAllText(path));
            return new EnvironmentSettings
            {
                Quality = value is not null && Enum.IsDefined(value.Quality) ? value.Quality : EnvironmentQuality.Off,
                Preset = value?.Preset is { } preset && Enum.IsDefined(preset) ? preset : null
            };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }

    public bool Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, JsonSerializer.Serialize(new { Quality, Preset }));
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }
}
