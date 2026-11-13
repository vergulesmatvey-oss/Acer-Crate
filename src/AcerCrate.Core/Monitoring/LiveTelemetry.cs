using AcerCrate.Core.Control;
using AcerCrate.Core.Hardware;

namespace AcerCrate.Core.Monitoring;

/// <summary>Live displays must never retain an old reading when telemetry stops.</summary>
public static class LiveTelemetry
{
    public static bool IsFresh(Telemetry? sample, DateTime utcNow) => sample is not null &&
        utcNow - sample.Timestamp.ToUniversalTime() is var age &&
        age >= TimeSpan.FromSeconds(-1) && age < TimeSpan.FromSeconds(6);

    public static int? FanRpm(Telemetry? sample, FanId id, DateTime utcNow) =>
        IsFresh(sample, utcNow) ? sample!.Fan(id)?.Rpm : null;
}
