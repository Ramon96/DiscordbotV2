using Hangfire.Server;

namespace GLaDOS.Scheduler.Application.Hangfire.Contracts;

public interface IHangfireJob
{
    Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default);
}