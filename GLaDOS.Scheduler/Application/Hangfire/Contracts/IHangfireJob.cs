namespace GLaDOS.Scheduler.Application.Hangfire.Contracts;

public interface IHangfireJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}