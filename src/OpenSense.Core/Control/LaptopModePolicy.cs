using OpenSense.Core.Hardware;
namespace OpenSense.Core.Control;
public enum LaptopMode { Quiet, Balanced, Performance }
public static class LaptopModePolicy
{
    public static ControlProfile Select(ControlProfile profile, LaptopMode mode, DeviceCapabilities caps)
    {
        var native = mode switch { LaptopMode.Quiet => OperatingMode.Quiet, LaptopMode.Balanced => OperatingMode.Balanced, _ => OperatingMode.Performance };
        return profile with { LaptopMode = mode, Mode = FanControlMode.Auto, AutoBoost = mode != LaptopMode.Quiet,
            OperatingMode = caps.OperatingModes.Contains(native) ? native : null,
            BatteryOperatingMode = null, CoolBoost = caps.CoolBoost ? mode == LaptopMode.Performance : null,
            GpuClocks = profile.GpuClocks with { Enabled = false } };
    }
    public static bool ManualAllowed(ControlProfile p) => p.LaptopMode is null or LaptopMode.Balanced or LaptopMode.Performance;
    public static ControlProfile Normalize(ControlProfile p) => ManualAllowed(p) ? p : p with { Mode = FanControlMode.Auto, AutoBoost = p.LaptopMode != LaptopMode.Quiet };
}
