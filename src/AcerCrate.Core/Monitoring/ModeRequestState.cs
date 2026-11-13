using AcerCrate.Core.Control;
using AcerCrate.Core.Hardware;

namespace AcerCrate.Core.Monitoring;

/// <summary>Separates immediate selection feedback from confirmed hardware state.</summary>
public sealed class ModeRequestState
{
    public OperatingMode? Pending { get; private set; }
    public int Revision { get; private set; }
    private DateTime? acceptedAfter;
    public void Request(OperatingMode mode) { Revision++; Pending = mode; acceptedAfter = null; }
    public void Accepted(OperatingMode mode, DateTime utcNow) { if (Pending == mode) acceptedAfter = utcNow; }
    public bool Confirm(Telemetry sample)
    {
        if (Pending is not { } mode || acceptedAfter is not { } after || sample.Timestamp.ToUniversalTime() < after ||
            !sample.ControlConfirmed || sample.Failsafe) return false;
        var expectedProfile = mode switch { OperatingMode.Quiet => LaptopMode.Quiet, OperatingMode.Performance => LaptopMode.Performance, _ => LaptopMode.Balanced };
        if (sample.OperatingMode != mode && !(sample.OperatingMode is null && sample.AppliedProfile?.LaptopMode == expectedProfile)) return false;
        Pending = null; acceptedAfter = null; return true;
    }
    public void Fail() { Pending = null; acceptedAfter = null; Revision++; }
}
