using System.Text.Json;
using System.Text.Json.Nodes;
using OpenSense.Core.Control;
using OpenSense.Core.Hardware;
using OpenSense.Core.Lighting;

namespace OpenSense.Core.Settings;

public sealed record PersonalMode
{
    public string Name { get; init; } = "Мой";
    public ControlProfile? Profile { get; init; }
    public LightingConfig? Lighting { get; init; }
    public PowerSettings? Power { get; init; }

    public bool Matches(ControlProfile profile) => Profile is not null && Equivalent(Profile, profile);

    public static bool Equivalent<T>(T left, T right) => JsonNode.DeepEquals(
        JsonSerializer.SerializeToNode(left, SettingsJson.Options), JsonSerializer.SerializeToNode(right, SettingsJson.Options));

    public static ControlProfile Prepare(ControlProfile profile, LaptopMode mode, DeviceCapabilities capabilities)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        var native = mode switch { LaptopMode.Quiet => OperatingMode.Quiet, LaptopMode.Balanced => OperatingMode.Balanced, _ => OperatingMode.Performance };
        return LaptopModePolicy.Normalize(profile with
        {
            LaptopMode = mode,
            OperatingMode = capabilities.OperatingModes.Contains(native) ? native : null,
            CoolBoost = capabilities.CoolBoost ? mode == LaptopMode.Quiet ? false : profile.CoolBoost : null,
            BatteryOperatingMode = mode == LaptopMode.Quiet ? null : profile.BatteryOperatingMode,
            GpuClocks = mode == LaptopMode.Quiet ? profile.GpuClocks with { Enabled = false } : profile.GpuClocks,
        });
    }
}
