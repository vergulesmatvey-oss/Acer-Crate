using Microsoft.Extensions.Time.Testing;
using AcerCrate.App.Helpers;

namespace AcerCrate.Core.Tests;

public class UiTelemetryBufferTests
{
    [Fact]
    public void BackgroundRefreshBatchesFiveSecondsWithoutLosingHistory()
    {
        var clock = new FakeTimeProvider();
        var buffer = new UiTelemetryBuffer<int>(300, clock);
        Assert.True(buffer.Add(0, false));
        Assert.Equal([0], buffer.Drain());
        for (var i = 1; i < 5; i++)
        {
            clock.Advance(TimeSpan.FromSeconds(1));
            Assert.False(buffer.Add(i, false));
        }
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(buffer.Add(5, false));
        Assert.Equal([1, 2, 3, 4, 5], buffer.Drain());
    }

    [Fact]
    public void ForegroundRefreshDoesNotWaitForBackgroundDeadline()
    {
        var buffer = new UiTelemetryBuffer<int>(300, new FakeTimeProvider());
        Assert.True(buffer.Add(0, false));
        buffer.Drain();
        Assert.False(buffer.Add(1, false));
        Assert.True(buffer.Add(2, true));
        Assert.Equal([1, 2], buffer.Drain());
    }

    [Fact]
    public void SlowDispatcherHasOneWorkItemAndBoundedRecentHistory()
    {
        var buffer = new UiTelemetryBuffer<int>(300);
        Assert.True(buffer.Add(0, true));
        for (var i = 1; i <= 1000; i++) Assert.False(buffer.Add(i, true));
        Assert.Equal(Enumerable.Range(701, 300), buffer.Drain());
        Assert.Empty(buffer.Drain());
        Assert.True(buffer.Add(1001, true));
    }
}
