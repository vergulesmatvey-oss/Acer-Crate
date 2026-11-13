using AcerCrate.Core.Control;
using AcerCrate.Core.Hardware;
using AcerCrate.Core.Monitoring;

namespace AcerCrate.Core.Tests;

public sealed class ModeRequestStateTests
{
    [Fact]
    public void Older_firmware_confirms_the_application_profile_without_a_native_mode()
    {
        var state = new ModeRequestState(); var now = DateTime.UtcNow;
        state.Request(OperatingMode.Quiet); state.Accepted(OperatingMode.Quiet, now);
        Assert.True(state.Confirm(new Telemetry { Timestamp = now.AddSeconds(1), ControlConfirmed = true, AppliedProfile = new ControlProfile { LaptopMode = LaptopMode.Quiet } }));
    }
    [Fact]
    public void Selection_is_available_immediately_but_old_confirmation_is_not_accepted()
    {
        var state = new ModeRequestState(); var now = DateTime.UtcNow;
        state.Request(OperatingMode.Performance);
        Assert.Equal(OperatingMode.Performance, state.Pending);
        Assert.False(state.Confirm(new Telemetry { Timestamp = now, ControlConfirmed = true, OperatingMode = OperatingMode.Performance }));
        state.Accepted(OperatingMode.Performance, now);
        Assert.False(state.Confirm(new Telemetry { Timestamp = now.AddSeconds(-1), ControlConfirmed = true, OperatingMode = OperatingMode.Performance }));
        Assert.True(state.Confirm(new Telemetry { Timestamp = now.AddSeconds(1), ControlConfirmed = true, OperatingMode = OperatingMode.Performance }));
        Assert.Null(state.Pending);
    }
    [Fact]
    public void Delayed_reply_for_an_earlier_press_does_not_replace_the_latest_selection()
    {
        var state = new ModeRequestState(); var now = DateTime.UtcNow;
        state.Request(OperatingMode.Performance); state.Request(OperatingMode.Quiet);
        state.Accepted(OperatingMode.Performance, now);
        Assert.False(state.Confirm(new Telemetry { Timestamp = now.AddSeconds(1), ControlConfirmed = true, OperatingMode = OperatingMode.Performance }));
        Assert.Equal(OperatingMode.Quiet, state.Pending);
    }
}
