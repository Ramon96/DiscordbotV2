namespace GLaDOS.Scheduler.Application.Dashboard.Metrics;

public record MetricsSnapshot(
    DateTimeOffset Timestamp,
    ProcessMetrics Process,
    HostMetrics? Host);

public record ProcessMetrics(
    double CpuPercent,
    double WorkingSetMb,
    double ManagedHeapMb,
    int ThreadCount,
    double UptimeSeconds);

public record HostMetrics(
    double CpuPercent,
    double MemoryUsedMb,
    double MemoryTotalMb);
