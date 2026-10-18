using Game.Shared;
using Xunit;

namespace Game.Shared.Tests;

public sealed class AudioSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "common-watch-audio-" + Guid.NewGuid().ToString("N"));
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    [Fact]
    public void MissingSettingsUseHalfVolumeWithoutCreatingFiles()
    {
        AudioSettings settings = AudioSettings.Load(SettingsPath);
        Assert.Equal(.5f, settings.MusicVolume);
        Assert.False(File.Exists(SettingsPath));
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(2f, 1f)]
    [InlineData(.25f, .25f)]
    [InlineData(float.NaN, .5f)]
    [InlineData(float.PositiveInfinity, .5f)]
    [InlineData(float.NegativeInfinity, .5f)]
    public void VolumeAlwaysHasAFiniteUsableValue(float supplied, float expected)
    {
        AudioSettings settings = AudioSettings.Load(SettingsPath);
        settings.MusicVolume = supplied;
        Assert.Equal(expected, settings.MusicVolume);
    }

    [Fact]
    public void VolumePersistsAndMuteRemainsMutedOnRestart()
    {
        AudioSettings settings = AudioSettings.Load(SettingsPath);
        settings.MusicVolume = .23f;
        Assert.True(settings.Save());
        Assert.Equal(.23f, AudioSettings.Load(SettingsPath).MusicVolume);
        settings.MusicVolume = 0;
        Assert.True(settings.Save());
        Assert.Equal(0, AudioSettings.Load(SettingsPath).MusicVolume);
    }

    [Theory]
    [InlineData("not JSON", .5f)]
    [InlineData("null", .5f)]
    [InlineData("{}", .5f)]
    [InlineData("{\"MusicVolume\":\"loud\"}", .5f)]
    [InlineData("{\"MusicVolume\":-10}", 0f)]
    [InlineData("{\"MusicVolume\":10}", 1f)]
    public void CorruptOrOutOfRangeStoredPreferencesRemainUsable(string json, float expected)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(SettingsPath, json);
        AudioSettings settings = AudioSettings.Load(SettingsPath);
        Assert.Equal(expected, settings.MusicVolume);
        settings.MusicVolume = .75f;
        Assert.True(settings.Save());
        Assert.Equal(.75f, AudioSettings.Load(SettingsPath).MusicVolume);
    }

    [Fact]
    public void InaccessiblePersistenceReturnsFalseWithoutCrashing()
    {
        Directory.CreateDirectory(_directory);
        // A directory cannot be read or overwritten as a JSON settings file.
        AudioSettings settings = AudioSettings.Load(_directory);
        Assert.Equal(.5f, settings.MusicVolume);
        settings.MusicVolume = .3f;
        Assert.False(settings.Save());
        Assert.Equal(.3f, settings.MusicVolume);
        Assert.False(AudioSettings.Load("").Save());
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }
}
