using AcerCrate.Core.Settings;
using System.Text.Json;

namespace AcerCrate.Core.Tests;

public sealed class UiColorsTests
{
    [Theory]
    [InlineData("#12e1d2", "#12E1D2")]
    [InlineData("123abc", "#123ABC")]
    [InlineData(" #f80 ", "#FF8800")]
    [InlineData("#000000", "#000000")]
    public void Picker_colours_are_normalized(string value, string expected)
    {
        Assert.True(UiColors.TryNormalize(value, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("##123456")]
    [InlineData("#12GG00")]
    [InlineData("#1234")]
    [InlineData("#00112233")]
    public void Invalid_or_transparent_colours_are_rejected(string? value) => Assert.False(UiColors.TryNormalize(value, out _));

    [Fact]
    public void Custom_colours_persist_and_older_settings_keep_theme_defaults()
    {
        var original = new UiSettings { AccentHex = "#FF8800", TextHex = "#ABCDEF", SidebarHex = "#080808" };
        Assert.Equal(original, JsonSerializer.Deserialize<UiSettings>(JsonSerializer.Serialize(original)));
        var old = JsonSerializer.Deserialize<UiSettings>("{\"AccentHex\":\"#12E1D2\"}")!;
        Assert.Null(old.TextHex);
        Assert.Null(old.SidebarHex);
    }
}
