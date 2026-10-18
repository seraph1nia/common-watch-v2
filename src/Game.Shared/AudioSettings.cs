using System.Security;
using System.Text.Json;

namespace Game.Shared;

/// <summary>Local presentation preferences, independent of authoritative gameplay.</summary>
public sealed class AudioSettings
{
    public const float DefaultMusicVolume = .5f;
    private readonly string _path;
    private float _musicVolume = DefaultMusicVolume;

    private AudioSettings(string path) => _path = path;

    public float MusicVolume
    {
        get => _musicVolume;
        set => _musicVolume = float.IsFinite(value) ? Math.Clamp(value, 0, 1) : DefaultMusicVolume;
    }

    public static AudioSettings Load(string path)
    {
        var settings = new AudioSettings(path);
        try
        {
            StoredSettings? stored = JsonSerializer.Deserialize<StoredSettings>(File.ReadAllText(path));
            if (stored is not null) settings.MusicVolume = stored.MusicVolume;
        }
        catch (Exception exception) when (IsPersistenceFailure(exception)) { }
        return settings;
    }

    /// <summary>Returns false if preferences cannot be saved; gameplay remains available.</summary>
    public bool Save()
    {
        try
        {
            string? directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllText(_path, JsonSerializer.Serialize(new StoredSettings { MusicVolume = MusicVolume }));
            return true;
        }
        catch (Exception exception) when (IsPersistenceFailure(exception)) { return false; }
    }

    private static bool IsPersistenceFailure(Exception exception) => exception is
        IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException or SecurityException;

    private sealed class StoredSettings
    {
        public float MusicVolume { get; init; } = DefaultMusicVolume;
    }
}

public static class MusicTrack
{
    public const string RelativePath = "LVS04_11_Echoes_of_Valhalla_bpm82_loop.wav";
    public const int SampleRate = 44100;
    public const int LoopStart = 1548939;
    public const int LoopEnd = 6195530; // Inclusive WAV sample index.
    public const int LoopLength = LoopEnd - LoopStart + 1;
}
