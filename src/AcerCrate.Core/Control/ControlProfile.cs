using AcerCrate.Core.Hardware;
using AcerCrate.Core.Monitoring;

namespace AcerCrate.Core.Control;

/// <summary>
/// Fan modes. The firmware's Custom behaviour is not a fixed speed: it adds speed on top of Auto
/// (0 % is Auto, 100 % is full speed), so every percentage AcerCrate sends is such a boost.
/// </summary>
public enum FanControlMode
{
    /// <summary>The firmware controls the fans.</summary>
    Auto,

    /// <summary>All fans at full speed.</summary>
    Max,

    /// <summary>A boost per fan, fixed or following a curve.</summary>
    Custom,
}

/// <summary>One fan in Custom mode: a fixed boost, or the boost its curve gives.</summary>
public sealed record ManualFanSetting(int Percent = 30, bool UseCurve = false);

/// <summary>A fan's curve in Custom mode. It follows the fan's own chip: the CPU fan the CPU, GPU fans the GPU.</summary>
public sealed record CurveFanSetting(FanCurve Curve);

public sealed record SafetySettings
{
    public bool RestoreAutoOnExit { get; init; } = true;
}

/// <summary>Everything the control loop needs to know about what the user wants.</summary>
public sealed record ControlProfile
{
    public LaptopMode? LaptopMode { get; init; }
    public int SensorIntervalSeconds { get; init; } = 1;
    public FanControlMode Mode { get; init; } = FanControlMode.Auto;

    /// <summary>Legacy setting retained for older profiles. Automatic cooling now always follows the firmware.</summary>
    public bool AutoBoost { get; init; }

    /// <summary>Optional Windows maximum processor state for a personal mode.</summary>
    public int? ProcessorLimitPercent { get; init; }

    public IReadOnlyDictionary<FanId, ManualFanSetting> Manual { get; init; } = new Dictionary<FanId, ManualFanSetting>
    {
        [FanId.Cpu] = new(),
        [FanId.Gpu] = new(),
    };

    /// <summary>Custom mode's curves, used by fans set to <see cref="ManualFanSetting.UseCurve"/>.</summary>
    public IReadOnlyDictionary<FanId, CurveFanSetting> Curves { get; init; } = new Dictionary<FanId, CurveFanSetting>
    {
        [FanId.Cpu] = DefaultCurve(FanId.Cpu),
        [FanId.Gpu] = DefaultCurve(FanId.Gpu),
    };

    /// <summary>Desired CoolBoost state; null leaves the firmware setting alone.</summary>
    public bool? CoolBoost { get; init; }

    /// <summary>
    /// The embedded controller's fan curve, which Auto follows and boosts add to; null leaves the firmware setting
    /// alone.
    /// </summary>
    public FanTable? FanTable { get; init; }

    /// <summary>Desired operating mode on AC power; null leaves the firmware setting alone.</summary>
    public OperatingMode? OperatingMode { get; init; }

    /// <summary>
    /// Desired operating mode on battery (Eco, Quiet or Balanced); null keeps the AC mode where the battery allows it,
    /// else Balanced (see <see cref="OperatingModePolicy"/>).
    /// </summary>
    public OperatingMode? BatteryOperatingMode { get; init; }

    /// <summary>What the laptop's Mode key does.</summary>
    public ModeKeyAction ModeKey { get; init; } = ModeKeyAction.Cycle;

    /// <summary>The mode the Mode key goes back to when it turns Turbo off (the one before Turbo).</summary>
    public OperatingMode? TurboReturnMode { get; init; }

    /// <summary>The discrete GPU's clock offsets, by operating mode; none by default.</summary>
    public GpuClockSettings GpuClocks { get; init; } = new();

    public SafetySettings Safety { get; init; } = new();

    public ManualFanSetting ManualFor(FanId fan) => Manual.GetValueOrDefault(fan) ?? new ManualFanSetting();

    public CurveFanSetting CurveFor(FanId fan) => Curves.GetValueOrDefault(fan) ?? DefaultCurve(fan);

    /// <summary>A curve nobody has set yet: Auto's, for the usual limits (the app offers the laptop's own as a preset).</summary>
    private static CurveFanSetting DefaultCurve(FanId fan) => new(AntiThrottle.For(FanChannel.Get(fan).Chip, ThermalLimits.Default));
}
