using OpenSense.Core.Control;
using OpenSense.Core.Hardware;
using OpenSense.Core.Lighting;
using OpenSense.Core.Settings;

namespace OpenSense.Core.Tests;

public sealed class PersonalModeTests
{
    private static DeviceCapabilities Caps => DeviceCapabilities.None with { OperatingModes = [OperatingMode.Quiet, OperatingMode.Balanced, OperatingMode.Performance] };

    [Fact]
    public void Personal_quiet_always_uses_automatic_cooling()
    {
        var result = PersonalMode.Prepare(new ControlProfile { Mode = FanControlMode.Custom, AutoBoost = true, CoolBoost = true, BatteryOperatingMode = OperatingMode.Balanced, GpuClocks = new() { Enabled = true } }, LaptopMode.Quiet, Caps with { CoolBoost = true });
        Assert.Equal(FanControlMode.Auto, result.Mode);
        Assert.False(result.AutoBoost);
        Assert.False(result.CoolBoost);
        Assert.False(result.GpuClocks.Enabled);
        Assert.Null(result.BatteryOperatingMode);
        Assert.Equal(OperatingMode.Quiet, result.OperatingMode);
        Assert.False(LaptopModePolicy.ManualAllowed(result));
    }

    [Theory]
    [InlineData(LaptopMode.Balanced, OperatingMode.Balanced)]
    [InlineData(LaptopMode.Performance, OperatingMode.Performance)]
    public void Personal_modes_keep_the_users_fan_settings(LaptopMode basis, OperatingMode native)
    {
        var profile = new ControlProfile { Mode = FanControlMode.Custom, Manual = new Dictionary<FanId, ManualFanSetting> { [FanId.Cpu] = new(68, true) } };
        var result = PersonalMode.Prepare(profile, basis, Caps);
        Assert.Equal(FanControlMode.Custom, result.Mode);
        Assert.Equal(new ManualFanSetting(68, true), result.ManualFor(FanId.Cpu));
        Assert.Equal(native, result.OperatingMode);
        Assert.True(LaptopModePolicy.ManualAllowed(result));
    }

    [Fact]
    public void Unsupported_firmware_mode_is_not_claimed_as_native()
    {
        var result = PersonalMode.Prepare(new(), LaptopMode.Performance, DeviceCapabilities.None);
        Assert.Null(result.OperatingMode);
    }

    [Fact]
    public void Invalid_basis_is_rejected() => Assert.Throws<ArgumentOutOfRangeException>(() => PersonalMode.Prepare(new(), (LaptopMode)123, Caps));

    [Fact]
    public void Profile_match_ignores_dictionary_insertion_order_but_detects_changed_values()
    {
        var profile = new ControlProfile { Manual = new Dictionary<FanId, ManualFanSetting> { [FanId.Cpu] = new(20), [FanId.Gpu] = new(30) } };
        var preset = new PersonalMode { Profile = profile };
        Assert.True(preset.Matches(profile with { Manual = new Dictionary<FanId, ManualFanSetting> { [FanId.Gpu] = new(30), [FanId.Cpu] = new(20) } }));
        Assert.False(preset.Matches(profile with { Manual = new Dictionary<FanId, ManualFanSetting> { [FanId.Cpu] = new(21), [FanId.Gpu] = new(30) } }));
        Assert.False(new PersonalMode().Matches(profile));
    }

    [Fact]
    public void Personal_mode_and_selection_survive_restart()
    {
        var folder = Path.Combine(Path.GetTempPath(), "OpenSense.PersonalMode.Tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new SettingsStore<UserSettings>(Path.Combine(folder, "settings.json"));
            var profile = PersonalMode.Prepare(new ControlProfile { Mode = FanControlMode.Custom }, LaptopMode.Balanced, Caps);
            store.Save(new UserSettings
            {
                PersonalMode = new PersonalMode { Name = "Работа", Profile = profile, Power = new PowerSettings { ChargeLimit = true }, Lighting = new LightingConfig().With("keyboard", new LightingSettings { Effect = LightingEffect.Breathing, Brightness = 75 }) },
                Ui = new UiSettings { PersonalModeSelected = true }
            });
            var restored = store.Load();
            Assert.Equal("Работа", restored.PersonalMode.Name);
            Assert.True(restored.PersonalMode.Matches(profile));
            Assert.True(restored.PersonalMode.Power!.ChargeLimit);
            Assert.Equal(LightingEffect.Breathing, restored.PersonalMode.Lighting!.For("keyboard")!.Effect);
            Assert.True(restored.Ui.PersonalModeSelected);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }
}
