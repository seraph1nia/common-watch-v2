using Game.GodotClient;
using Xunit;

namespace Game.Godot.Session.Tests;

public sealed class EnvironmentSettingsTests
{
    [Fact]
    public void DefaultsPreserveScreenshotsAndCliOverridesSavedQuality()
    {
        Assert.Equal(EnvironmentQuality.Off, EnvironmentSettings.Parse([]).Quality);
        var settings = EnvironmentSettings.Parse(["--world-art", "sprites", "--environment-effects", "subtle", "--environment-preset", "rain", "--environment-seed", "42", "--environment-time", "12.5"], EnvironmentQuality.Full);
        Assert.Equal(EnvironmentQuality.Subtle, settings.Quality);
        Assert.Equal(AtmospherePreset.Rain, settings.Preset);
        Assert.Equal(42, settings.Seed);
        Assert.Equal(12.5, settings.SampleTime);
        Assert.Equal(EnvironmentQuality.Full, EnvironmentSettings.Parse([], EnvironmentQuality.Full).Quality);
    }

    [Theory]
    [InlineData("clear", AtmospherePreset.Clear)]
    [InlineData("mist", AtmospherePreset.Mist)]
    [InlineData("rain", AtmospherePreset.Rain)]
    [InlineData("night", AtmospherePreset.Night)]
    [InlineData("enchanted", AtmospherePreset.Enchanted)]
    [InlineData("ember", AtmospherePreset.Ember)]
    [InlineData("snow", AtmospherePreset.Snow)]
    public void AcceptsDocumentedPresetNames(string name, AtmospherePreset expected) =>
        Assert.Equal(expected, EnvironmentSettings.Parse(["--environment-preset", name]).Preset);

    [Theory]
    [InlineData("mist,rain")]
    [InlineData("mist, rain")]
    [InlineData("0")]
    [InlineData("3")]
    [InlineData("-1")]
    [InlineData("Rain")]
    [InlineData("RAIN")]
    [InlineData(" rainy ")]
    [InlineData("rainy")]
    [InlineData("clear-day")]
    [InlineData("magical")]
    [InlineData(" rain")]
    [InlineData("rain ")]
    [InlineData("")]
    public void RejectsUndocumentedPresetSpellings(string value)
    {
        var error = Assert.Throws<ArgumentException>(() => EnvironmentSettings.Parse(["--environment-preset", value]));
        Assert.Equal("--environment-preset requires clear|mist|rain|night|enchanted|ember|snow", error.Message);
    }

    [Theory]
    [InlineData("--environment-effects", "wrong")]
    [InlineData("--environment-preset", "unknown")]
    [InlineData("--environment-preset", "99")]
    [InlineData("--environment-time", "NaN")]
    [InlineData("--environment-time", "Infinity")]
    [InlineData("--environment-time", "-1")]
    [InlineData("--environment-time", "86401")]
    public void RejectsInvalidSettings(string key, string value) => Assert.Throws<ArgumentException>(() => EnvironmentSettings.Parse([key, value]));

    [Theory]
    [InlineData("--environment-effects")]
    [InlineData("--environment-preset")]
    [InlineData("--environment-time")]
    [InlineData("--environment-seed")]
    public void RejectsMissingValues(string key)
    {
        var error = Assert.Throws<ArgumentException>(() => EnvironmentSettings.Parse([key]));
        Assert.Equal("Missing environment option value.", error.Message);
    }

    [Fact]
    public void SavesPreferencesAndRecoversFromMissingCorruptOrInvalidFiles()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            Assert.Equal(EnvironmentQuality.Off, EnvironmentSettings.Load(path).Quality);
            Assert.True(new EnvironmentSettings { Quality = EnvironmentQuality.Full }.Save(path));
            Assert.Equal(EnvironmentQuality.Full, EnvironmentSettings.Load(path).Quality);
            File.WriteAllText(path, "broken");
            Assert.Equal(EnvironmentQuality.Off, EnvironmentSettings.Load(path).Quality);
            File.WriteAllText(path, "{\"Quality\":99}");
            Assert.Equal(EnvironmentQuality.Off, EnvironmentSettings.Load(path).Quality);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void PersistsQualityAndPresetWithoutCaptureOptionsAndCliOverridesRemainTemporary()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var saved = new EnvironmentSettings { Quality = EnvironmentQuality.Full, Preset = AtmospherePreset.Snow, Seed = 42, SampleTime = 12 };
            Assert.True(saved.Save(path));
            string json = File.ReadAllText(path);
            Assert.DoesNotContain("Seed", json);
            Assert.DoesNotContain("SampleTime", json);
            saved = EnvironmentSettings.Load(path);
            Assert.Equal(EnvironmentQuality.Full, saved.Quality);
            Assert.Equal(AtmospherePreset.Snow, saved.Preset);
            Assert.Equal(417, saved.Seed);
            Assert.Null(saved.SampleTime);
            Assert.Equal(AtmospherePreset.Snow, EnvironmentSettings.Parse([], saved.Quality, saved.Preset).Preset);
            var launch = EnvironmentSettings.Parse(["--environment-preset", "rain", "--environment-effects", "subtle"], saved.Quality, saved.Preset);
            Assert.Equal(AtmospherePreset.Rain, launch.Preset);
            Assert.Equal(EnvironmentQuality.Subtle, launch.Quality);
            Assert.Equal(saved, EnvironmentSettings.Load(path));
            Assert.True((saved with { Preset = null }).Save(path));
            Assert.Null(EnvironmentSettings.Load(path).Preset);
            File.WriteAllText(path, "{\"Quality\":2}");
            Assert.Equal(EnvironmentQuality.Full, EnvironmentSettings.Load(path).Quality);
            Assert.Null(EnvironmentSettings.Load(path).Preset);
            File.WriteAllText(path, "{\"Quality\":2,\"Preset\":99}");
            Assert.Equal(EnvironmentQuality.Full, EnvironmentSettings.Load(path).Quality);
            Assert.Null(EnvironmentSettings.Load(path).Preset);
        }
        finally { File.Delete(path); }
    }

}
