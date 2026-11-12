namespace OpenSense.App.Helpers;

/// <summary>Coalesces UI work without dropping the recent sensor history or allowing an unbounded backlog.</summary>
internal sealed class UiTelemetryBuffer<T>(int capacity, TimeProvider? clock = null)
{
    private readonly object gate = new();
    private readonly Queue<T> pending = new();
    private readonly TimeProvider time = clock ?? TimeProvider.System;
    private DateTimeOffset nextBackgroundRefresh;
    private bool scheduled;

    public bool Add(T sample, bool visible)
    {
        lock (gate)
        {
            pending.Enqueue(sample);
            while (pending.Count > capacity) pending.Dequeue();
            var now = time.GetUtcNow();
            if (scheduled || !visible && now < nextBackgroundRefresh) return false;
            scheduled = true;
            nextBackgroundRefresh = now.AddSeconds(5);
            return true;
        }
    }

    public T[] Drain()
    {
        lock (gate)
        {
            var batch = pending.ToArray();
            pending.Clear();
            scheduled = false;
            return batch;
        }
    }
}
