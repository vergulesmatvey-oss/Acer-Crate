using AcerCrate.Core.Hardware;

namespace AcerCrate.Core.Monitoring;

/// <summary>Only a confirmed change after the initial reading can produce a background notification.</summary>
public sealed class ModeChangeTracker
{
    private OperatingMode? previous;

    public bool Observe(OperatingMode? mode, bool confirmed, bool failsafe, bool windowVisible)
    {
        if (mode is null || !confirmed || failsafe) return false;
        var changed = previous.HasValue && previous != mode;
        previous = mode;
        return changed && !windowVisible;
    }
}
