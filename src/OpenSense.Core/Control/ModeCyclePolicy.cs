using OpenSense.Core.Hardware;

namespace OpenSense.Core.Control;

public static class ModeCyclePolicy
{
    private static readonly (OperatingMode Native, LaptopMode Mode)[] Order =
    [
        (OperatingMode.Quiet, LaptopMode.Quiet),
        (OperatingMode.Balanced, LaptopMode.Balanced),
        (OperatingMode.Performance, LaptopMode.Performance)
    ];

    public static LaptopMode? Next(OperatingMode? current, IReadOnlyCollection<OperatingMode> supported, PowerLimit limit)
    {
        var allowed = OperatingModePolicy.Allowed(supported, limit);
        var choices = Order.Where(item => allowed.Contains(item.Native)).ToArray();
        if (choices.Length == 0) return null;
        var index = Array.FindIndex(choices, item => item.Native == current);
        return choices[(index + 1) % choices.Length].Mode;
    }
}
