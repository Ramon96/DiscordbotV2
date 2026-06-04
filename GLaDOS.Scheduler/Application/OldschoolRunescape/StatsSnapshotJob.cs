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

        var snapshotDate = DateTime.UtcNow.Date;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existingSnapshotUserIds = await dbContext.Set<OldschoolRunescapeStatsSnapshot>()
            .Where(s => s.SnapshotDate == snapshotDate)
            .Select(s => s.OldschoolRunescapeUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<Guid>(existingSnapshotUserIds);

        var allUsers = await dbContext.Set<OldschoolRunescapeUser>()
            .Include(u => u.Stats)
            .Include(u => u.Activities)
            .AsSplitQuery()
            .Where(u => u.Stats.Any())
            .ToListAsync(cancellationToken);

        var usersNeedingSnapshot = allUsers
            .Where(u => !existingSet.Contains(u.Id))
            .ToList();

        _logger.LogInformation("Found {Count} users needing snapshots (out of {Total})",
            usersNeedingSnapshot.Count, allUsers.Count);

        var progressBar = context.WriteProgressBar();

        for (var i = 0; i < usersNeedingSnapshot.Count; i++)
        {
            var progress = (double)(i + 1) / usersNeedingSnapshot.Count * 100;
            progressBar.SetValue(progress);

            var user = usersNeedingSnapshot[i];

            try
            {
                var statSnapshots = user.Stats.Select(stat => new OldschoolRunescapeStatsSnapshot
                {
                    OldschoolRunescapeUserId = user.Id,
                    SnapshotDate = snapshotDate,
                    SkillId = stat.SkillId,
                    Name = stat.Name,
                    Level = stat.Level,
                    Experience = stat.Experience,
                    Rank = stat.Rank
                });

                var activitySnapshots = user.Activities.Select(act => new OldschoolRunescapeActivitySnapshot
                {
                    OldschoolRunescapeUserId = user.Id,
                    SnapshotDate = snapshotDate,
                    ActivityId = act.ActivityId,
                    Name = act.Name,
                    Score = act.Score,
                    Rank = act.Rank
                });

                dbContext.Set<OldschoolRunescapeStatsSnapshot>().AddRange(statSnapshots);
                dbContext.Set<OldschoolRunescapeActivitySnapshot>().AddRange(activitySnapshots);

                context.WriteLine($"Snapshot saved for {user.Username}");
            }
            catch (Exception ex)
            {
                context.SetTextColor(ConsoleTextColor.Red);
                context.WriteLine($"[Error] Snapshot failed for user {user.Username}: {ex.Message}");
                context.ResetTextColor();
                _logger.LogError(ex, "Snapshot failed for user {Username}", user.Username);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Stats snapshot job completed. {Count} users processed", usersNeedingSnapshot.Count);
        context.WriteLine("Completed stats snapshot job.");
    }
}
