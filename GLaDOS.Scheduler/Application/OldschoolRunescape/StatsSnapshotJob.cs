using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.Scheduler.Application.Hangfire.Contracts;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GLaDOS.Scheduler.Application.OldschoolRunescape;

[DisableConcurrentExecution(0)]
[AutomaticRetry(Attempts = 1)]
public class StatsSnapshotJob : IHangfireJob
{
    private readonly ILogger<StatsSnapshotJob> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public StatsSnapshotJob(ILogger<StatsSnapshotJob> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting stats snapshot job");

        List<Guid> userIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            userIds = await dbContext.Set<OldschoolRunescapeUser>()
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }

        var snapshotDate = DateTime.UtcNow.Date;
        var progressBar = context.WriteProgressBar();

        for (var i = 0; i < userIds.Count; i++)
        {
            var progress = (double)(i + 1) / userIds.Count * 100;
            progressBar.SetValue(progress);

            var userId = userIds[i];

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var user = await dbContext.Set<OldschoolRunescapeUser>()
                    .Include(u => u.Stats)
                    .Include(u => u.Activities)
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                if (user?.Stats == null || !user.Stats.Any())
                {
                    _logger.LogDebug("Skipping snapshot for {Username} - no stats yet", user?.Username);
                    continue;
                }

                var existingSnapshot = await dbContext.Set<OldschoolRunescapeStatsSnapshot>()
                    .AnyAsync(s => s.OldschoolRunescapeUserId == userId && s.SnapshotDate == snapshotDate, cancellationToken);

                if (existingSnapshot)
                {
                    _logger.LogDebug("Snapshot already exists for {Username} on {Date}", user.Username, snapshotDate);
                    continue;
                }

                var statSnapshots = user.Stats.Select(stat => new OldschoolRunescapeStatsSnapshot
                {
                    OldschoolRunescapeUserId = userId,
                    SnapshotDate = snapshotDate,
                    SkillId = stat.SkillId,
                    Name = stat.Name,
                    Level = stat.Level,
                    Experience = stat.Experience,
                    Rank = stat.Rank
                });

                var activitySnapshots = user.Activities.Select(act => new OldschoolRunescapeActivitySnapshot
                {
                    OldschoolRunescapeUserId = userId,
                    SnapshotDate = snapshotDate,
                    ActivityId = act.ActivityId,
                    Name = act.Name,
                    Score = act.Score,
                    Rank = act.Rank
                });

                dbContext.Set<OldschoolRunescapeStatsSnapshot>().AddRange(statSnapshots);
                dbContext.Set<OldschoolRunescapeActivitySnapshot>().AddRange(activitySnapshots);
                await dbContext.SaveChangesAsync(cancellationToken);

                context.WriteLine($"Snapshot saved for {user.Username}");
            }
            catch (Exception ex)
            {
                context.SetTextColor(ConsoleTextColor.Red);
                context.WriteLine($"[Error] Snapshot failed for user {userId}: {ex.Message}");
                context.ResetTextColor();
                _logger.LogError(ex, "Snapshot failed for user {UserId}", userId);
            }
        }

        _logger.LogInformation("Stats snapshot job completed. {Count} users processed", userIds.Count);
        context.WriteLine("Completed stats snapshot job.");
    }
}
