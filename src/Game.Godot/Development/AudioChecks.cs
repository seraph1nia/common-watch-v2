using Godot;
using Game.Shared;

namespace Game.GodotClient;

public partial class GameApplication
{
    private async Task SmokeAudioSettings()
    {
        float original = _audio.Settings.MusicVolume;
        ulong playerId = _audio.Player.GetInstanceId();
        _settingsButton.EmitSignal(BaseButton.SignalName.Pressed);
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
        _settings.CloseButton.EmitSignal(BaseButton.SignalName.Pressed);
        Require(!_settings.Visible, "settings close button");
        Require(Math.Abs(AudioSettings.Load(_audio.SettingsPath).MusicVolume - 1) < .0001, "audio settings save and reload");
        Require(_session.Snapshot.Revision == revision && _session.Snapshot.Tick == tick, "settings do not change gameplay");
        Require(_audio.Player.GetInstanceId() == playerId, "settings retain music player");
        _settings.Slider.Value = original * 100;
        _audio.Settings.Save();
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

}
