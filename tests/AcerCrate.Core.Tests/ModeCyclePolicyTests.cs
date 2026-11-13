using AcerCrate.Core.Control;
using AcerCrate.Core.Hardware;

namespace AcerCrate.Core.Tests;

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
    public void Fast_presses_follow_requested_state_instead_of_stale_firmware()
    {
        Assert.Equal(LaptopMode.Performance, ModeCyclePolicy.NextRequested(LaptopMode.Balanced, OperatingMode.Quiet, Supported, PowerLimit.None));
        Assert.Equal(LaptopMode.Quiet, ModeCyclePolicy.NextRequested(LaptopMode.Performance, OperatingMode.Quiet, Supported, PowerLimit.None));
    }

    [Fact]
    public void Missing_requested_state_and_power_restrictions_are_handled()
    {
        Assert.Equal(LaptopMode.Performance, ModeCyclePolicy.NextRequested(null, OperatingMode.Balanced, Supported, PowerLimit.None));
        Assert.Equal(LaptopMode.Quiet, ModeCyclePolicy.NextRequested(LaptopMode.Balanced, OperatingMode.Quiet, Supported, PowerLimit.Battery));
    }

    [Fact]
    public void Older_firmware_can_cycle_safe_application_profiles_without_native_modes()
    {
        Assert.Equal(LaptopMode.Performance, ModeCyclePolicy.NextRequested(LaptopMode.Balanced, null, [], PowerLimit.None));
        Assert.Equal(LaptopMode.Quiet, ModeCyclePolicy.NextRequested(LaptopMode.Balanced, null, [], PowerLimit.Battery));
        var profile = LaptopModePolicy.Select(new ControlProfile(), LaptopMode.Performance, DeviceCapabilities.None);
        Assert.Null(profile.OperatingMode);
    }

    [Fact]
    public void The_accepted_cycle_shortcut_is_enabled_for_older_settings_and_can_be_disabled()
    {
        Assert.True(System.Text.Json.JsonSerializer.Deserialize<AcerCrate.Core.Settings.UserSettings>("{}")!.Ui.ModeCycleHotkey);
        Assert.False(System.Text.Json.JsonSerializer.Deserialize<AcerCrate.Core.Settings.UserSettings>("{\"Ui\":{\"ModeCycleHotkey\":false}}")!.Ui.ModeCycleHotkey);
    }
}
