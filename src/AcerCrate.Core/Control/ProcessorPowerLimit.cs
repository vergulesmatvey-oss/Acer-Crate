using AcerCrate.Core.Hardware;

namespace AcerCrate.Core.Control;

public interface IProcessorPowerPlans
{
    Guid? Active();
    bool ConfigureLimit(Guid source, int percent);
    bool SetActive(Guid id);
    bool DeleteLimit();
}

public sealed class WindowsProcessorPowerPlans : IProcessorPowerPlans
{
    public Guid? Active() => PowerPlans.Active();
    public bool ConfigureLimit(Guid source, int percent) => PowerPlans.ConfigureProcessorLimit(source, percent);
    public bool SetActive(Guid id) => PowerPlans.SetActive(id);
    public bool DeleteLimit() => PowerPlans.DeleteProcessorLimit();
}

/// <summary>Applies a CPU cap using a temporary copy of the active Windows power scheme.</summary>
public sealed class ProcessorPowerLimit(IProcessorPowerPlans plans, Guid? previousScheme, Func<Guid?, bool> savePrevious)
{
    public const int MinimumPercent = 5;
    public static readonly Guid SchemeId = new("6eae192d-02a0-4d44-91ee-cc0000000004");
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan MaxReadingGap = TimeSpan.FromSeconds(6);

    private readonly object gate = new();
    private Guid? previous = previousScheme == SchemeId ? null : previousScheme;
    private int? applied;
    private bool overheating;
    private DateTime? coolSince;
    private DateTime? lastReading;

    public bool HasLease
    {
        get { lock (gate) return previous is not null; }
    }

    public bool Observe(double? temperature, DateTime time)
    {
        if (temperature >= 100)
            overheating = true;

        if (overheating && temperature is > 0 and <= 90)
        {
            if (coolSince is null || lastReading is not { } last || time < last || time - last > MaxReadingGap)
                coolSince = time;
            if (time - coolSince >= Cooldown)
                overheating = false;
        }
        else
        {
            coolSince = null;
        }
        lastReading = time;
        return overheating;
    }

    public (Guid? Active, int? Percent, bool Confirmed) Apply(int? limit)
    {
        lock (gate)
        {
            try
            {
                return limit is { } percent ? Limit(Math.Clamp(percent, MinimumPercent, 99)) : Restore();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                return (null, null, false);
            }
        }
    }

    private (Guid?, int?, bool) Limit(int percent)
    {
        if (plans.Active() is not { } source)
            return (null, null, false);

        if (source == SchemeId)
        {
            if (previous is not { } saved)
                return (source, null, false);
            if (applied == percent)
                return (source, applied, true);
            source = saved;
        }
        else if (previous != source)
        {
            if (!savePrevious(source))
                return (source, null, false);
            previous = source;
        }

        applied = null;
        if (!plans.ConfigureLimit(source, percent) || !plans.SetActive(SchemeId) || plans.Active() != SchemeId)
            return (plans.Active(), null, false);

        applied = percent;
        return (SchemeId, applied, true);
    }

    private (Guid?, int?, bool) Restore()
    {
        if (previous is not { } saved)
            return (null, null, true);

        if (plans.Active() is not { } active)
            return (null, null, false);
        if (active == SchemeId)
        {
            if ((!plans.SetActive(saved) && !plans.SetActive(PowerPlans.Balanced)) ||
                plans.Active() is not { } restored || restored == SchemeId)
                return (active, applied, false);
            active = restored;
        }

        applied = null;
        if (!plans.DeleteLimit() || !savePrevious(null))
            return (active, null, false);
        previous = null;
        return (active, null, true);
    }
}
