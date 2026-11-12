using OpenSense.Core.Control;
using OpenSense.Core.Hardware;
using OpenSense.Core.Lighting;
using OpenSense.Core.Settings;

namespace OpenSense.Core.Tests;

public sealed class CratePresetTests
{
    [Fact]
    public void Balanced_accepts_manual_fans_and_quiet_keeps_firmware_control()
    {
        var manual = new ControlProfile { LaptopMode = LaptopMode.Balanced, Mode = FanControlMode.Custom };
        Assert.Equal(FanControlMode.Custom, LaptopModePolicy.Normalize(manual).Mode);
        var preset = CoolingPreset.Capture(manual);
        var quiet = manual with { LaptopMode = LaptopMode.Quiet };
        Assert.Throws<InvalidOperationException>(() => preset.Restore(quiet));
        Assert.Equal(FanControlMode.Auto, LaptopModePolicy.Normalize(quiet).Mode);
    }

    [Fact]
    public void Named_presets_survive_a_fresh_settings_store_and_restore_only_their_domain()
    {
        var path = Path.Combine(Path.GetTempPath(), "AcerCrateTests", Guid.NewGuid().ToString("N"), "presets.json");
        var manual = new ControlProfile
        {
            LaptopMode = LaptopMode.Balanced, Mode = FanControlMode.Custom,
            Manual = new Dictionary<FanId, ManualFanSetting> { [FanId.Cpu] = new(40), [FanId.Gpu] = new(35, true) }
        };
        var lighting = new LightingSettings { Effect = LightingEffect.Breathing, EffectColor = "#20AABB", Speed = 4 };
        new SettingsStore<UserSettings>(path).Save(new UserSettings
        {
            CoolingPresets = new() { ["Работа"] = CoolingPreset.Capture(manual) },
            LightingPresets = new() { ["Океан"] = lighting }
        });
        var saved = new SettingsStore<UserSettings>(path).Load();
        var target = new ControlProfile { LaptopMode = LaptopMode.Performance, SensorIntervalSeconds = 3 };
        var restored = saved.CoolingPresets["Работа"].Restore(target);
        Assert.Equal(target.LaptopMode, restored.LaptopMode);
        Assert.Equal(3, restored.SensorIntervalSeconds);
        Assert.Equal(40, restored.ManualFor(FanId.Cpu).Percent);
        Assert.True(restored.ManualFor(FanId.Gpu).UseCurve);
        Assert.True(saved.LightingPresets["Океан"].SameAs(lighting));
    }
}
