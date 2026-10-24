using Godot;
using Game.Shared;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task SmokeAudioSettings()
    {
        float original = _audio.Settings.MusicVolume;
        ulong playerId = _audio.Player.GetInstanceId();
        _gameUi.MenuButton.EmitSignal(BaseButton.SignalName.Pressed);
        _menuPopup.SettingsButton.EmitSignal(BaseButton.SignalName.Pressed);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // Layout consumes a normal simulation frame; changing preferences must not issue commands.
        long revision = _session.Snapshot.Revision;
        long tick = _session.Snapshot.Tick;
        Require(_settings.Visible, "settings button opens overlay");
        Require(GetViewport().GetVisibleRect().Encloses(_settings.Panel.GetGlobalRect()), $"settings overlay fits viewport (panel={_settings.Panel.GetGlobalRect()}, viewport={GetViewport().GetVisibleRect()})");
        foreach (double percent in new double[] { 0, 50, 100 })
        {
            _settings.Slider.Value = percent;
            Require(Math.Abs(AudioServer.GetBusVolumeLinear(_audio.MusicBus) - percent / 100) < .0001, $"music slider applies {percent}%");
            Require(Math.Abs(_audio.Settings.MusicVolume - percent / 100) < .0001, "audio setting follows slider");
            Require(AudioServer.IsBusMute(_audio.MusicBus) == (percent == 0), "native Music bus mute");
        }
        SmokeEnvironmentSettings();
        _settings.CloseButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(!_settings.Visible, "settings close button");
        Require(Math.Abs(AudioSettings.Load(_audio.SettingsPath).MusicVolume - 1) < .0001, "audio settings save and reload");
        Require(_session.Snapshot.Revision == revision && _session.Snapshot.Tick == tick, "settings do not change gameplay");
        Require(_audio.Player.GetInstanceId() == playerId, "settings retain music player");
        _settings.Slider.Value = original * 100;
        _audio.Settings.Save();
        if (Argument("--gameplay-captures") is not null)
        {
            _settings.Open();
            await GameplayCapture("settings-weather-" + GetWindow().Size.X);
            _settings.Close();
        }
        if (System.IO.File.Exists(_audio.MusicPath))
        {
            Require(_audio.Player.Stream is AudioStreamWav, "actual upstream WAV decoded");
            var stream = (AudioStreamWav)_audio.Player.Stream;
            Require(stream.LoopMode == AudioStreamWav.LoopModeEnum.Forward && stream.LoopBegin == MusicTrack.LoopStart
                && stream.LoopEnd == MusicTrack.LoopEnd, "embedded WAV loop bounds retained");
            Require(stream.MixRate == MusicTrack.SampleRate, "upstream music sample rate retained");
            Require(_audio.Player.Playing, "background music player started");
            GD.Print($"GODOT_AUDIO_PASS loaded=true playing={_audio.Player.Playing} driver={AudioServer.GetDriverName()} loop={stream.LoopBegin}..{stream.LoopEnd} settings=0/50/100 persisted=true");
        }
        else
        {
            Require(_audio.Unavailable is not null, "missing music is reported gracefully");
            GD.Print("GODOT_AUDIO_PASS loaded=false missing-file-fallback=true settings=0/50/100 persisted=true");
        }
    }

    private void SmokeEnvironmentSettings()
    {
        const string content = "Center/Panel/Margin/Content/";
        var effects = _settings.GetNode<OptionButton>(content + "EnvironmentEffects");
        var presets = _settings.GetNode<OptionButton>(content + "WeatherPreset");
        var original = _environment.Settings;
        Require(presets.ItemCount == 8 && presets.GetItemText(0) == "Scene default", "all weather presets and scene default available in settings");
        Require(presets.Selected == (original.Preset is { } originalPreset ? (int)originalPreset + 1 : 0), "settings reflect launch or saved weather");
        Select(effects, (int)EnvironmentQuality.Full);
        foreach (var preset in Enum.GetValues<AtmospherePreset>())
        {
            Select(presets, (int)preset + 1);
            Require(_environment.Settings.Preset == preset, "weather selection applies immediately: " + preset);
            var saved = EnvironmentSettings.Load(_audio.SettingsPath + ".environment.json");
            Require(saved.Quality == EnvironmentQuality.Full && saved.Preset == preset, "quality and weather save together");
            Select(effects, (int)EnvironmentQuality.Subtle);
            Require(EnvironmentSettings.Load(_audio.SettingsPath + ".environment.json").Preset == preset, "quality changes retain chosen weather");
            Select(effects, (int)EnvironmentQuality.Full);
        }
        Select(presets, 0);
        Require(_environment.Settings.Preset is null && EnvironmentSettings.Load(_audio.SettingsPath + ".environment.json").Preset is null, "scene default clears saved weather override");
        Require(_environment.Preset(EnvironmentScene.Settlement).ResourcePath.EndsWith("/clear.tres")
            && _environment.Preset(EnvironmentScene.Arena).ResourcePath.EndsWith("/ember.tres")
            && _environment.Preset(EnvironmentScene.Menu).ResourcePath.EndsWith("/enchanted.tres"), "scene default restores each scene's authored atmosphere");
        _environment.SetPreset(AtmospherePreset.Night, immediate: true);
        Require(presets.Selected == (int)AtmospherePreset.Night + 1, "settings follow external preset changes");
        Select(presets, original.Preset is { } previous ? (int)previous + 1 : 0);
        Select(effects, (int)original.Quality);
        GD.Print("GODOT_WEATHER_SETTINGS_PASS presets=7 scene-default=true persisted=true");

        static void Select(OptionButton option, int index)
        {
            option.Select(index);
            option.EmitSignal(OptionButton.SignalName.ItemSelected, (long)index);
        }
    }

}
