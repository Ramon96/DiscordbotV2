using System.Diagnostics;

namespace GLaDOS.Scheduler.Application.Dashboard.Metrics;

/// <summary>
/// Samples process and host resource usage on a fixed interval and pushes each snapshot into the
/// in-memory <see cref="MetricsHistory"/>. CPU percentages are computed from deltas between
/// samples, so this service is the only place that owns the "previous reading" state.
/// </summary>
public class MetricsSampler : BackgroundService
{
    private static readonly TimeSpan SampleInterval = TimeSpan.FromSeconds(5);
    private const double BytesPerMb = 1024.0 * 1024.0;

    private readonly MetricsHistory _history;
    private readonly Process _process = Process.GetCurrentProcess();
    private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

    private TimeSpan _previousCpuTime;
    private DateTimeOffset _previousSampleAt;
    private HostCpuReading? _previousHostCpu;

    public MetricsSampler(MetricsHistory history)
    {
        _history = history;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _process.Refresh();
        _previousCpuTime = _process.TotalProcessorTime;
        _previousSampleAt = DateTimeOffset.UtcNow;
        _previousHostCpu = TryReadHostCpu();

        // Seed one sample immediately so the dashboard isn't blank for the first interval.
        _history.Add(Collect());

        using var timer = new PeriodicTimer(SampleInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                _history.Add(Collect());
            }
            catch
            {
                // A single failed sample must never tear down the loop.
            }
        }
    }

    private MetricsSnapshot Collect()
    {
        _process.Refresh();
        var now = DateTimeOffset.UtcNow;

        var cpuTime = _process.TotalProcessorTime;
        var wallSeconds = (now - _previousSampleAt).TotalSeconds;
        var cpuDeltaSeconds = (cpuTime - _previousCpuTime).TotalSeconds;
        var cpuPercent = wallSeconds > 0
            ? Math.Clamp(cpuDeltaSeconds / (wallSeconds * Environment.ProcessorCount) * 100.0, 0, 100)
            : 0;

        _previousCpuTime = cpuTime;
        _previousSampleAt = now;

        var process = new ProcessMetrics(
            Math.Round(cpuPercent, 1),
            Math.Round(_process.WorkingSet64 / BytesPerMb, 1),
            Math.Round(GC.GetTotalMemory(forceFullCollection: false) / BytesPerMb, 1),
            _process.Threads.Count,
            Math.Round((now - _startedAt).TotalSeconds, 0));

        return new MetricsSnapshot(now, process, CollectHost());
    }

    private HostMetrics? CollectHost()
    {
        var cpuReading = TryReadHostCpu();
        var memory = TryReadHostMemory();

        // Not on Linux (e.g. local Windows dev): no /proc, so report no host metrics.
        if (cpuReading is null && memory is null)
        {
            return null;
        }

        var hostCpuPercent = 0.0;
        if (cpuReading is not null && _previousHostCpu is not null)
        {
            var totalDelta = (double)(cpuReading.Total - _previousHostCpu.Total);
            var idleDelta = (double)(cpuReading.Idle - _previousHostCpu.Idle);
            if (totalDelta > 0)
            {
                hostCpuPercent = Math.Clamp((1 - idleDelta / totalDelta) * 100.0, 0, 100);
            }
        }

        if (cpuReading is not null)
        {
            _previousHostCpu = cpuReading;
        }

        return new HostMetrics(
            Math.Round(hostCpuPercent, 1),
            Math.Round(memory?.UsedMb ?? 0, 1),
            Math.Round(memory?.TotalMb ?? 0, 1));
    }

    private static HostCpuReading? TryReadHostCpu()
    {
        try
        {
            if (!File.Exists("/proc/stat"))
            {
                return null;
            }

            var line = File.ReadLines("/proc/stat").FirstOrDefault(l => l.StartsWith("cpu "));
            if (line is null)
            {
                return null;
            }

            // Format: "cpu  user nice system idle iowait irq softirq steal ..."
            var values = line
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Take(8)
                .Select(ulong.Parse)
                .ToArray();

            if (values.Length < 4)
            {
                return null;
            }

            var idle = values[3] + (values.Length > 4 ? values[4] : 0); // idle + iowait
            ulong total = 0;
            foreach (var value in values)
            {
                total += value;
            }

            return new HostCpuReading(idle, total);
        }
        catch
        {
            return null;
        }
    }

    private static (double UsedMb, double TotalMb)? TryReadHostMemory()
    {
        try
        {
            if (!File.Exists("/proc/meminfo"))
            {
                return null;
            }

            var lines = File.ReadLines("/proc/meminfo").Take(5).ToArray();
            var totalKb = ParseMeminfoKb(lines, "MemTotal:");
            var availableKb = ParseMeminfoKb(lines, "MemAvailable:");

            if (totalKb <= 0)
            {
                return null;
            }

            return ((totalKb - availableKb) / 1024.0, totalKb / 1024.0);
        }
        catch
        {
            return null;
        }
    }

    private static double ParseMeminfoKb(IEnumerable<string> lines, string key)
    {
        var line = lines.FirstOrDefault(l => l.StartsWith(key));
        if (line is null)
        {
            return 0;
        }

        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && double.TryParse(parts[1], out var kb) ? kb : 0;
    }

    private record HostCpuReading(ulong Idle, ulong Total);
}
