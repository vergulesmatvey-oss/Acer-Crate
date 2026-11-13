using AcerCrate.Core.Control;
using AcerCrate.Core.Hardware;
using AcerCrate.Core.Settings;
using System.Text.Json;
namespace AcerCrate.Core.Tests;
public sealed class LaptopModeTests
{
    [Theory]
    [InlineData(LaptopMode.Quiet, OperatingMode.Quiet)]
    [InlineData(LaptopMode.Balanced, OperatingMode.Balanced)]
    [InlineData(LaptopMode.Performance, OperatingMode.Performance)]
    public void Three_modes_use_only_detected_firmware_modes(LaptopMode mode, OperatingMode native)
    {
        var d = new AcerDevice(new FakeFirmware { SupportsModes = true });
        var p = LaptopModePolicy.Select(new(), mode, CapabilityProbe.Probe(d));
        Assert.Equal(native,p.OperatingMode); Assert.Equal(FanControlMode.Auto,p.Mode);
        Assert.Null(p.BatteryOperatingMode); Assert.False(p.GpuClocks.Enabled);
    }
    [Fact]
    public void Software_quiet_is_explicit_and_has_no_firmware_claim()
    {
        var p = LaptopModePolicy.Select(new(),LaptopMode.Quiet,DeviceCapabilities.None);
        Assert.Null(p.OperatingMode); Assert.False(p.AutoBoost); Assert.False(LaptopModePolicy.ManualAllowed(p));
    }
    [Theory]
    [InlineData(FanControlMode.Custom)]
    [InlineData(FanControlMode.Max)]
    public void Quiet_blocks_manual_commands_in_controller_even_with_tampered_profile(FanControlMode mode)
    {
        var f = new FakeFirmware(); var d = new AcerDevice(f);
        using var c = new FanControlService(d,CapabilityProbe.Probe(d),new FakeLoad(),new FakePower(),new ControlProfile { LaptopMode = LaptopMode.Quiet, Mode = mode });
        c.Tick(); Assert.Equal(FanControlMode.Auto,c.Latest!.EffectiveMode); Assert.Equal(FanLock.QuietMode,c.Latest.FanLock);
        Assert.All(f.Behavior.Values,b => Assert.Equal(FanBehavior.Auto,b));
    }
    [Fact]
    public void Failed_readback_does_not_report_success_and_returns_auto()
    {
        var f = new FakeFirmware { SupportsModes = true }; var d = new AcerDevice(f);
        using var c = new FanControlService(d,CapabilityProbe.Probe(d),new FakeLoad(),new FakePower(),new ControlProfile { LaptopMode = LaptopMode.Performance, OperatingMode = OperatingMode.Performance, Mode = FanControlMode.Max },modes:new RejectingMode());
        c.Tick(); Assert.Null(c.Latest!.OperatingMode); Assert.False(c.Latest.ControlConfirmed); Assert.Equal(FanControlMode.Auto,c.Latest.EffectiveMode);
    }
    [Fact]
    public void Last_successful_profile_roundtrips_separately_from_requested()
    {
        var s = new MachineSettings { Profile = new() { LaptopMode = LaptopMode.Performance }, LastSuccessfulProfile = new() { LaptopMode = LaptopMode.Quiet } };
        var restored = JsonSerializer.Deserialize<MachineSettings>(JsonSerializer.Serialize(s,SettingsJson.Options),SettingsJson.Options)!;
        Assert.Equal(LaptopMode.Performance,restored.Profile.LaptopMode); Assert.Equal(LaptopMode.Quiet,restored.LastSuccessfulProfile!.LaptopMode);
    }
    [Fact]
    public async Task Restart_restores_successful_profile_instead_of_unconfirmed_request()
    {
        var directory=Path.Combine(Path.GetTempPath(),"AcerCenterTests",Guid.NewGuid().ToString("N"));
        var path=Path.Combine(directory,"settings.json");
        var saved=new ControlProfile { LaptopMode=LaptopMode.Quiet, Mode=FanControlMode.Auto, AutoBoost=false };
        new SettingsStore<MachineSettings>(path).Save(new() { Profile=new() { LaptopMode=LaptopMode.Performance }, LastSuccessfulProfile=saved });
        using var engine=new AcerCrate.Core.Engine.CrateEngine(new SimulatedMachine(SimulatedModel.Nitro2022),path,Microsoft.Extensions.Logging.Abstractions.NullLogger<AcerCrate.Core.Engine.CrateEngine>.Instance);
        Assert.Equal(JsonSerializer.Serialize(saved,SettingsJson.Options),JsonSerializer.Serialize((await engine.GetSnapshotAsync()).Settings.Profile,SettingsJson.Options));
    }
    [Fact]
    public async Task Successful_software_quiet_is_committed_by_engine_telemetry()
    {
        var directory=Path.Combine(Path.GetTempPath(),"AcerCenterTests",Guid.NewGuid().ToString("N"));var path=Path.Combine(directory,"settings.json");
        using var engine=new AcerCrate.Core.Engine.CrateEngine(new SimulatedMachine(SimulatedModel.Nitro2021),path,Microsoft.Extensions.Logging.Abstractions.NullLogger<AcerCrate.Core.Engine.CrateEngine>.Instance);
        engine.Start();var snapshot=await engine.GetSnapshotAsync();var p=LaptopModePolicy.Select(snapshot.Settings.Profile,LaptopMode.Quiet,snapshot.Capabilities);
        var done=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        engine.TelemetryUpdated+=(_,t)=>{if(t.AppliedProfile==p&&t.ControlConfirmed)done.TrySetResult();};
        await engine.SetProfileAsync(p);await done.Task.WaitAsync(TimeSpan.FromSeconds(5),TestContext.Current.CancellationToken);
        Assert.Equal(LaptopMode.Quiet,(await engine.GetSnapshotAsync()).Settings.LastSuccessfulProfile!.LaptopMode);
    }
    private sealed class RejectingMode : IOperatingModeChannel
    { public OperatingMode? Read() => OperatingMode.Balanced; public bool Write(OperatingMode mode) => true; }
}
