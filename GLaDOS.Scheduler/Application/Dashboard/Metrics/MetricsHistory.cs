namespace GLaDOS.Scheduler.Application.Dashboard.Metrics;

/// <summary>
/// Fixed-capacity, thread-safe ring buffer of recent metric samples. Kept in memory so the
/// dashboard has a short history to chart without needing a time-series database.
/// </summary>
public class MetricsHistory
{
    private readonly int _capacity;
    private readonly LinkedList<MetricsSnapshot> _samples = new();
    private readonly object _lock = new();

    public MetricsHistory(int capacity = 120)
    {
        _capacity = capacity;
    }

    public void Add(MetricsSnapshot snapshot)
    {
        lock (_lock)
        {
            _samples.AddLast(snapshot);
            while (_samples.Count > _capacity)
            {
                _samples.RemoveFirst();
            }
        }
    }

    public IReadOnlyList<MetricsSnapshot> Snapshot()
    {
        lock (_lock)
        {
            return _samples.ToArray();
        }
    }

    public MetricsSnapshot? Latest()
    {
        lock (_lock)
        {
            return _samples.Count > 0 ? _samples.Last!.Value : null;
        }
    }
}
