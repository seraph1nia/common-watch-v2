using Game.Shared;
using Godot;

namespace Game.GodotClient;

/// <summary>Owns native audio playback for the application, independently of sessions and screens.</summary>
public partial class AudioController : Node
{
    public AudioStreamPlayer Player { get; private set; } = null!;
    public AudioSettings Settings { get; private set; } = null!;
    public string SettingsPath { get; private set; } = "";
    public string MusicPath { get; private set; } = "";
    public string? Unavailable { get; private set; }
    public int MusicBus { get; private set; }
    public bool Saved { get; private set; } = true;
    public event Action? VolumeChanged;
    public FeedbackAudio Feedback { get; private set; } = null!;

    public void Initialize(string settingsPath, string audioRoot)
    {
        Feedback = new FeedbackAudio(); AddChild(Feedback); Feedback.Initialize();
        SettingsPath = settingsPath;
        Settings = AudioSettings.Load(settingsPath);
        MusicBus = AudioServer.GetBusIndex("Music");
        if (MusicBus < 0) throw new InvalidOperationException("Native Music audio bus is missing.");
        Player = new AudioStreamPlayer { Name = "BackgroundMusic", Bus = "Music" };
        AddChild(Player);
        SetVolume(Settings.MusicVolume);
        MusicPath = System.IO.Path.Combine(audioRoot, MusicTrack.RelativePath);
        if (!System.IO.File.Exists(MusicPath))
        {
            Unavailable = "Music file unavailable. The game remains playable.";
            return;
        }
        try
        {
            // Decode the unchanged PCM and embedded smpl markers with Godot's native decoder.
            Player.Stream = AudioStreamWav.LoadFromFile(MusicPath, new Godot.Collections.Dictionary
            {
                ["compress/mode"] = 0, ["edit/loop_mode"] = 0, ["force/max_rate"] = false
            }) ?? throw new InvalidDataException("WAV decoder did not return an audio stream.");
            Player.Play();
        }
        catch (Exception exception)
        {
            Unavailable = "Music unavailable. The game remains playable.";
            GD.PushWarning($"Background music: {exception.Message}");
        }
    }

    public void SetVolume(float volume)
    {
        Settings.MusicVolume = volume;
        AudioServer.SetBusVolumeLinear(MusicBus, Settings.MusicVolume);
        AudioServer.SetBusMute(MusicBus, Settings.MusicVolume == 0);
        Saved = Settings.Save();
        VolumeChanged?.Invoke();
    }
    public string Status => (Unavailable ?? (Settings.MusicVolume == 0 ? "Music muted."
        : AudioServer.GetDriverName() == "Dummy" ? "Audio output unavailable (silent)." : "Music playing."))
        + (Saved ? "" : " Volume applies for this session; settings could not be saved.");

    public void Stop()
    {
        Feedback?.Reset();
        if (Player is null) return;
        Player.Stop();
        AudioStream? stream = Player.Stream;
        Player.Stream = null;
        stream?.Dispose();
    }
    public override void _ExitTree()
    {
        Settings?.Save();
        Stop();
        VolumeChanged = null;
    }
}
