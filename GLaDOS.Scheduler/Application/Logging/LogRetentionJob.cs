using GLaDOS.Infra.EntityFramework;
using GLaDOS.Infra.EntityFramework.Logging;
using GLaDOS.Scheduler.Application.Hangfire.Contracts;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GLaDOS.Scheduler.Application.Logging;

[DisableConcurrentExecution(0)]
[AutomaticRetry(Attempts = 1)]
public class LogRetentionJob : IHangfireJob
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(14);

    private readonly ILogger<LogRetentionJob> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public LogRetentionJob(ILogger<LogRetentionJob> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.UtcNow - RetentionPeriod;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var deleted = await dbContext.Set<LogEntry>()
            .Where(log => log.Timestamp < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogInformation("Log retention removed {Count} entries older than {Cutoff:u}", deleted, cutoff);
        context.WriteLine($"Removed {deleted} log entries older than {cutoff:u}.");
    }
}
