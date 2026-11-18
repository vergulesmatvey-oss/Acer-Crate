using AcerCrate.Core.Control;

namespace AcerCrate.Core.Tests;

public sealed class ProcessorPowerLimitTests
{
    [Fact]
    public void Emergency_override_requires_100_degrees_and_sustained_cooling()
    {
        var time = new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc);
        var power = new FakePlans();
        var original = power.Current;
        Guid? saved = null;
        var guard = new ProcessorPowerLimit(power, null, previous => { saved = previous; return true; });

        Assert.False(guard.Observe(99, time));
        Assert.True(guard.Observe(100, time.AddSeconds(1)));
        Assert.Equal(5, guard.Apply(ProcessorPowerLimit.MinimumPercent).Percent);
        Assert.Equal(original, saved);
        Assert.Equal(ProcessorPowerLimit.SchemeId, power.Current);
        Assert.True(guard.Observe(null, time.AddSeconds(2)));
        for (var second = 3; second < 18; second += 3)
            Assert.True(guard.Observe(90, time.AddSeconds(second)));
        Assert.False(guard.Observe(89, time.AddSeconds(18)));
        Assert.True(guard.Apply(null).Confirmed);
        Assert.Equal(original, power.Current);
        Assert.True(power.Deleted);
        Assert.Null(saved);
    }

    [Fact]
    public void Stop_after_restart_restores_the_persisted_scheme()
    {
        var power = new FakePlans();
        var original = power.Current;
        Guid? saved = null;
        bool Save(Guid? previous) { saved = previous; return true; }
        var first = new ProcessorPowerLimit(power, null, Save);
        Assert.True(first.Apply(65).Confirmed);

        var restarted = new ProcessorPowerLimit(power, saved, Save);
        Assert.True(restarted.Apply(null).Confirmed);
        Assert.Equal(original, power.Current);
        Assert.Null(saved);
    }

    [Fact]
    public void Failure_to_save_recovery_state_leaves_the_current_plan_intact()
    {
        var power = new FakePlans();
        var original = power.Current;
        var guard = new ProcessorPowerLimit(power, null, _ => false);

        Assert.False(guard.Apply(5).Confirmed);
        Assert.Equal(original, power.Current);
        Assert.Equal(0, power.Writes);
    }

    [Fact]
    public void Restore_leaves_a_plan_the_user_selected_outside_the_app_active()
    {
        var power = new FakePlans();
        var guard = new ProcessorPowerLimit(power, null, _ => true);
        Assert.True(guard.Apply(65).Confirmed);
        var selected = Guid.NewGuid();
        power.Current = selected;
        Assert.True(guard.Apply(null).Confirmed);
        Assert.Equal(selected, power.Current);
        Assert.True(power.Deleted);
    }

    private sealed class FakePlans : IProcessorPowerPlans
    {
        public Guid Current { get; set; } = Guid.NewGuid();
        public int Writes { get; private set; }
        public bool Deleted { get; private set; }

        public Guid? Active() => Current;

        public bool ConfigureLimit(Guid source, int percent)
        {
            Assert.NotEqual(ProcessorPowerLimit.SchemeId, source);
            Writes++;
            return true;
        }

        public bool SetActive(Guid id)
        {
            Current = id;
            return true;
        }

        public bool DeleteLimit()
        {
            Assert.NotEqual(ProcessorPowerLimit.SchemeId, Current);
            Deleted = true;
            return true;
        }
    }
}
