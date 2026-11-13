using AcerCrate.Core.Hardware;
using AcerCrate.Core.Monitoring;

namespace AcerCrate.Core.Tests;

public sealed class ModeChangeTrackerTests
{
    [Fact]
    public void Initial_and_repeated_readings_do_not_show_an_osd()
    {
        var tracker = new ModeChangeTracker();
        Assert.False(tracker.Observe(OperatingMode.Quiet, true, false, false));
        Assert.False(tracker.Observe(OperatingMode.Quiet, true, false, false));
        Assert.True(tracker.Observe(OperatingMode.Balanced, true, false, false));
    }

    [Fact]
    public void Visible_changes_are_not_replayed_after_hiding_the_window()
    {
        var tracker = new ModeChangeTracker();
        tracker.Observe(OperatingMode.Quiet, true, false, true);
        Assert.False(tracker.Observe(OperatingMode.Performance, true, false, true));
        Assert.False(tracker.Observe(OperatingMode.Performance, true, false, false));
        Assert.True(tracker.Observe(OperatingMode.Quiet, true, false, false));
    }

    [Fact]
    public void Missing_unconfirmed_and_failsafe_states_cannot_report_a_successful_switch()
    {
        var tracker = new ModeChangeTracker();
        tracker.Observe(OperatingMode.Balanced, true, false, false);
        Assert.False(tracker.Observe(null, true, false, false));
        Assert.False(tracker.Observe(OperatingMode.Performance, false, false, false));
        Assert.False(tracker.Observe(OperatingMode.Performance, true, true, false));
        Assert.True(tracker.Observe(OperatingMode.Performance, true, false, false));
    }
}
