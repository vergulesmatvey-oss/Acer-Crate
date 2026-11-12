using OpenSense.Core.Hardware;

namespace OpenSense.Core.Control;

public sealed record CoolingPreset(
    FanControlMode Mode,
    bool AutoBoost,
    IReadOnlyDictionary<FanId, ManualFanSetting> Manual,
    IReadOnlyDictionary<FanId, CurveFanSetting> Curves)
{
    public static CoolingPreset Capture(ControlProfile profile) =>
        new(profile.Mode, profile.AutoBoost, profile.Manual, profile.Curves);

    public ControlProfile Restore(ControlProfile current)
    {
        if (!LaptopModePolicy.ManualAllowed(current))
            throw new InvalidOperationException("Для настройки вентиляторов выберите сбалансированный или производительный режим.");
        return current with { Mode = Mode, AutoBoost = AutoBoost, Manual = Manual, Curves = Curves };
    }
}
