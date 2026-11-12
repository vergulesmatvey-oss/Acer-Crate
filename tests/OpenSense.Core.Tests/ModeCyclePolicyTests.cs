using OpenSense.Core.Control;
using OpenSense.Core.Hardware;

namespace OpenSense.Core.Tests;

public sealed class ModeCyclePolicyTests
{
    private static readonly OperatingMode[] Supported = [OperatingMode.Quiet, OperatingMode.Balanced, OperatingMode.Performance];

    [Theory]
    [InlineData(OperatingMode.Quiet, LaptopMode.Balanced)]
    [InlineData(OperatingMode.Balanced, LaptopMode.Performance)]
    [InlineData(OperatingMode.Performance, LaptopMode.Quiet)]
    public void Mains_cycle_follows_the_three_application_modes(OperatingMode current, LaptopMode expected) =>
        Assert.Equal(expected, ModeCyclePolicy.Next(current, Supported, PowerLimit.None));

    [Theory]
    [InlineData(PowerLimit.Battery)]
    [InlineData(PowerLimit.Adapter)]
    [InlineData(PowerLimit.LowBattery)]
    public void Restricted_power_never_selects_performance(PowerLimit limit)
    {
        Assert.Equal(LaptopMode.Balanced, ModeCyclePolicy.Next(OperatingMode.Quiet, Supported, limit));
        Assert.Equal(LaptopMode.Quiet, ModeCyclePolicy.Next(OperatingMode.Balanced, Supported, limit));
    }

    [Fact]
    public void No_supported_modes_means_no_hardware_command() => Assert.Null(ModeCyclePolicy.Next(null, [], PowerLimit.None));

    [Fact]
    public void The_accepted_cycle_shortcut_is_enabled_for_older_settings_and_can_be_disabled()
    {
        Assert.True(System.Text.Json.JsonSerializer.Deserialize<OpenSense.Core.Settings.UserSettings>("{}")!.Ui.ModeCycleHotkey);
        Assert.False(System.Text.Json.JsonSerializer.Deserialize<OpenSense.Core.Settings.UserSettings>("{\"Ui\":{\"ModeCycleHotkey\":false}}")!.Ui.ModeCycleHotkey);
    }
}
