using AcerCrate.Core.Control;
using AcerCrate.Core.Hardware;
using AcerCrate.Core.Monitoring;

namespace AcerCrate.Core.Tests;

public sealed class LiveTelemetryTests
{
    private static readonly DateTime Now = new(2026, 11, 11, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Fan_display_uses_the_new_sample_instead_of_a_cached_editor_value()
    {
        var old = Sample(2800);
        var current = Sample(4250);
        Assert.Equal(2800, LiveTelemetry.FanRpm(old, FanId.Cpu, Now));
        Assert.Equal(4250, LiveTelemetry.FanRpm(current, FanId.Cpu, Now));
    }

    [Fact]
    public void Lost_telemetry_clears_the_reading_instead_of_freezing_it()
    {
        Assert.Null(LiveTelemetry.FanRpm(Sample(4250), FanId.Cpu, Now.AddSeconds(6)));
        Assert.Null(LiveTelemetry.FanRpm(null, FanId.Cpu, Now));
        Assert.Null(LiveTelemetry.FanRpm(Sample(4250), FanId.Gpu, Now));
    }

    [Fact]
    public void A_stopped_fan_is_zero_while_an_unavailable_reading_is_null()
    {
        Assert.Equal(0, LiveTelemetry.FanRpm(Sample(0), FanId.Cpu, Now));
        Assert.Null(LiveTelemetry.FanRpm(Sample(null), FanId.Cpu, Now));
        Assert.False(LiveTelemetry.IsFresh(Sample(1000) with { Timestamp = Now.AddMinutes(1) }, Now));
    }

    private static Telemetry Sample(int? rpm) => new()
    {
        Timestamp = Now,
        Fans = [new FanTelemetry(FanId.Cpu, rpm, FanBehavior.Auto, null)]
    };
}
