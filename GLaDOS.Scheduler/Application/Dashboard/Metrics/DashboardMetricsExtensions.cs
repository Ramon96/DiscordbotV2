namespace GLaDOS.Scheduler.Application.Dashboard.Metrics;

public static class DashboardMetricsExtensions
{
    public static IServiceCollection AddDashboardMetrics(this IServiceCollection services)
    {
        services.AddSingleton<MetricsHistory>();
        services.AddHostedService<MetricsSampler>();
        return services;
    }
}
