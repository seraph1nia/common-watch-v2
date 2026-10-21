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
    public void SavesOnlyLocalQualityAndRecoversFromMissingCorruptOrInvalidFiles()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            Assert.Equal(EnvironmentQuality.Off, EnvironmentSettings.LoadQuality(path));
            Assert.True(EnvironmentSettings.SaveQuality(path, EnvironmentQuality.Full));
            Assert.Equal(EnvironmentQuality.Full, EnvironmentSettings.LoadQuality(path));
            File.WriteAllText(path, "broken");
            Assert.Equal(EnvironmentQuality.Off, EnvironmentSettings.LoadQuality(path));
            File.WriteAllText(path, "{\"Quality\":99}");
            Assert.Equal(EnvironmentQuality.Off, EnvironmentSettings.LoadQuality(path));
        }
        finally { File.Delete(path); }
    }
}
