using GLaDOS.Scheduler.Application.Hangfire.Contracts;
using Hangfire;

namespace GLaDOS.Scheduler.Application;

// don't fetch too often wiki doesn't like getting their data scraped
// maybe I should consider round robining between a few IPs in the future
[DisableConcurrentExecution(60 * 60)]
[AutomaticRetry(Attempts = 0)]
public class OsrsWikiSyncJob : IHangfireJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}