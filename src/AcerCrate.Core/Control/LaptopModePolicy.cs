using AcerCrate.Core.Hardware;
namespace AcerCrate.Core.Control;
public enum LaptopMode { Quiet, Balanced, Performance }
public static class LaptopModePolicy
{
    public static ControlProfile Select(ControlProfile profile, LaptopMode mode, DeviceCapabilities caps)
    {
        var native = mode switch { LaptopMode.Quiet => OperatingMode.Quiet, LaptopMode.Balanced => OperatingMode.Balanced, _ => OperatingMode.Performance };
        return profile with { LaptopMode = mode, Mode = FanControlMode.Auto, AutoBoost = false, ProcessorLimitPercent = null,
            OperatingMode = caps.OperatingModes.Contains(native) ? native : null,
            BatteryOperatingMode = null, CoolBoost = caps.CoolBoost ? false : null,
            GpuClocks = profile.GpuClocks with { Enabled = false } };
    }
    public static bool ManualAllowed(ControlProfile p) => p.LaptopMode is null or LaptopMode.Balanced or LaptopMode.Performance;
    public static ControlProfile Normalize(ControlProfile p)
    {
        var mode = ManualAllowed(p) ? p.Mode : FanControlMode.Auto;
        var limit = p.ProcessorLimitPercent is >= ProcessorPowerLimit.MinimumPercent and < 100 ? p.ProcessorLimitPercent : null;
        return p.Mode == mode && !p.AutoBoost && p.ProcessorLimitPercent == limit ? p :
            p with { Mode = mode, AutoBoost = false, ProcessorLimitPercent = limit };
    }
}
