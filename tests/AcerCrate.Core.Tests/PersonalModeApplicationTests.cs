using AcerCrate.Core.Control;
using AcerCrate.Core.Lighting;
using AcerCrate.Core.Settings;

namespace AcerCrate.Core.Tests;

public sealed class PersonalModeApplicationTests
{
    private static readonly MachineSettings Previous = new() { Profile = new() { LaptopMode = LaptopMode.Quiet }, Power = new() { ChargeLimit = false }, Lighting = new() };
    private static PersonalMode Preset => new() { Profile = new() { LaptopMode = LaptopMode.Balanced }, Power = new() { ChargeLimit = true }, Lighting = new LightingConfig().With("keyboard", new()) };

    [Fact]
    public async Task Successful_application_commits_after_all_device_calls()
    {
        var calls = new List<string>();
        await PersonalModeApplication.ApplyAsync(Preset, Previous,
            _ => { calls.Add("profile"); return Task.CompletedTask; }, _ => { calls.Add("power"); return Task.CompletedTask; },
            _ => { calls.Add("lighting"); return Task.CompletedTask; }, () => calls.Add("commit"));
        Assert.Equal(["profile", "power", "lighting", "commit"], calls);
    }

    [Fact]
    public async Task Failed_lighting_restore_still_restores_power_and_cooling()
    {
        var calls = new List<string>();
        await Assert.ThrowsAsync<AggregateException>(() => PersonalModeApplication.ApplyAsync(Preset, Previous,
            profile => { calls.Add(profile == Previous.Profile ? "restore-profile" : "apply-profile"); return Task.CompletedTask; },
            power => { calls.Add(power == Previous.Power ? "restore-power" : "apply-power"); return Task.CompletedTask; },
            _ => { calls.Add("lighting-failed"); throw new IOException("fixture"); }, () => calls.Add("commit")));
        Assert.Equal(["apply-profile", "apply-power", "lighting-failed", "lighting-failed", "restore-power", "restore-profile"], calls);
    }

    [Fact]
    public async Task Lost_profile_acknowledgement_attempts_restoration_without_committing()
    {
        var profiles = new List<ControlProfile>(); var committed = false;
        await Assert.ThrowsAsync<IOException>(() => PersonalModeApplication.ApplyAsync(Preset, Previous,
            profile => { profiles.Add(profile); if (profiles.Count == 1) throw new IOException("lost acknowledgement"); return Task.CompletedTask; },
            _ => Task.CompletedTask, _ => Task.CompletedTask, () => committed = true));
        Assert.Equal(2, profiles.Count); Assert.Same(Previous.Profile, profiles[1]); Assert.False(committed);
    }

    [Fact]
    public async Task Failed_cooling_restore_attempts_firmware_automation()
    {
        var profiles = new List<ControlProfile>();
        await Assert.ThrowsAsync<AggregateException>(() => PersonalModeApplication.ApplyAsync(Preset, Previous,
            profile => { profiles.Add(profile); throw new IOException("fixture"); }, _ => Task.CompletedTask, _ => Task.CompletedTask, () => { }));
        Assert.Equal(3, profiles.Count); Assert.Equal(FanControlMode.Auto, profiles[^1].Mode); Assert.False(profiles[^1].AutoBoost);
    }

    [Fact]
    public async Task Failed_settings_save_rolls_back_device_changes()
    {
        var profiles = new List<ControlProfile>();
        await Assert.ThrowsAsync<IOException>(() => PersonalModeApplication.ApplyAsync(new PersonalMode { Profile = Preset.Profile }, Previous,
            profile => { profiles.Add(profile); return Task.CompletedTask; }, _ => Task.CompletedTask, _ => Task.CompletedTask, () => throw new IOException("settings fixture")));
        Assert.Same(Previous.Profile, profiles[^1]);
    }
}
