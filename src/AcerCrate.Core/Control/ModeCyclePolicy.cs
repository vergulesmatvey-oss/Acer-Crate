using AcerCrate.Core.Hardware;

namespace AcerCrate.Core.Control;

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

    /// <summary>Requested state advances before delayed firmware telemetry catches up.</summary>
    public static LaptopMode? NextRequested(LaptopMode? requested, OperatingMode? observed,
        IReadOnlyCollection<OperatingMode> supported, PowerLimit limit)
    {
        if (supported.Count == 0)
        {
            // Older firmware has no native mode method. LaptopModePolicy keeps that field null.
            var count = limit == PowerLimit.None ? 3 : 2;
            var current = (int)(requested ?? LaptopMode.Quiet);
            return current >= count ? LaptopMode.Quiet : (LaptopMode)((current + 1) % count);
        }
        return Next(requested switch
        {
            LaptopMode.Quiet => OperatingMode.Quiet,
            LaptopMode.Balanced => OperatingMode.Balanced,
            LaptopMode.Performance => OperatingMode.Performance,
            _ => observed
        }, supported, limit);
    }
}
