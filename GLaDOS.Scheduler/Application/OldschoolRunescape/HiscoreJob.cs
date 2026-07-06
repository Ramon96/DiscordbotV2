using Glados.Discord.Services;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using GLaDOS.OldschoolRunescape.Requests;
using GLaDOS.OldschoolRunescape.Responses;
using GLaDOS.Scheduler.Application.Hangfire.Contracts;
using GLaDOS.Scheduler.Extensions;
using GLaDOS.Scheduler.Extensions.OldschoolRunescape;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Application.OldschoolRunescape;

[DisableConcurrentExecution(600)]
[AutomaticRetry(Attempts = 1)]
public class HiscoreJob : IHangfireJob
{
    private readonly ILogger<HiscoreJob> _logger;
    private readonly IOldschoolRunescapeClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HiscoreCalculator _calculator;
    private readonly DiscordNotificationService _notificationService;

    public HiscoreJob(ILogger<HiscoreJob> logger, IOldschoolRunescapeClient client, IServiceScopeFactory scopeFactory, HiscoreCalculator calculator, DiscordNotificationService notificationService)
    {
        _logger = logger;
        _client = client;
        _scopeFactory = scopeFactory;
        _calculator = calculator;
        _notificationService = notificationService;
    }

    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default)
    {
        var progressBar = context.WriteProgressBar();
        _logger.LogInformation("Starting hiscore job");

        var updates = new List<(OldschoolRunescapeUser User, OldschoolRunescapeHiscoreChanges Changes)>();

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var allUsers = await dbContext.Set<OldschoolRunescapeUser>()
            .Include(user => user.Stats)
            .Include(user => user.Activities)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        context.WriteLine($"Loaded {allUsers.Count} users. Starting sync...");

        for (int i = 0; i < allUsers.Count; i++)
        {
            double progress = (double)(i + 1) / allUsers.Count * 100;
            progressBar.SetValue(progress);

            var user = allUsers[i];

            _logger.LogInformation("Fetching hiscores for user: {Username}", user.Username);
            context.WriteLine($"Processing user: {user.Username}...");

            OldschoolRunescapeHiscoreResponse? freshData;

            try
            {
                freshData = await _client.GetHiScoresByUsernameAsync(
                    new OldschoolRunescapeHiscoreRequest { Username = user.Username },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                context.SetTextColor(ConsoleTextColor.Red);
                context.WriteLine($"[Error] Failed to fetch hiscores for {user.Username}: {ex.Message}");
                context.ResetTextColor();

                _logger.LogError(ex, "Failed to fetch hiscores for {Username}", user.Username);
                continue;
            }

            if (freshData == null)
            {
                context.SetTextColor(ConsoleTextColor.Yellow);
                context.WriteLine($"[Warning] User '{user.Username}' not found in hiscores.");
                context.ResetTextColor();

                _logger.LogWarning("User '{Username}' not found in hiscores.", user.Username);
                continue;
            }

            if (user.Stats == null || !user.Stats.Any() || user.Activities == null || !user.Activities.Any())
            {
                _logger.LogWarning("User '{Username}' has null Stats or Activities collections.", user.Username);

                var newStats = freshData.Skills.Select(skill => skill.ToEntity(user.Id));
                var newActivities = freshData.Activities.Select(activity => activity.ToEntity(user.Id));

                dbContext.Set<OldschoolRunescapeStat>().AddRange(newStats);
                dbContext.Set<OldschoolRunescapeActivity>().AddRange(newActivities);

                continue;
            }

            SeedMissingEntries(dbContext, user, freshData);

            var changes = _calculator.CalculateUpdates(user, freshData);

            if (!changes.HasChanges)
            {
                _logger.LogInformation("No changes detected for user: {Username}", user.Username);
                continue;
            }

            foreach (var change in changes.StatChanges)
            {
                var stat = user.Stats!.First(stat => stat.Name == change.StatName);
                stat.Level = change.NewLevel;
                stat.Experience = change.NewExperience;
                stat.Rank = change.NewRank;
            }

            foreach (var change in changes.ActivityChanges)
            {
                var activity = user.Activities!.First(activity => activity.Name == change.ActivityName);
                activity.Score = change.NewScore;
                activity.Rank = change.NewRank;
            }

            updates.Add((user, changes));

            _logger.LogInformation("Updated {StatCount} stats and {ActivityCount} activities for {Username}",
                changes.StatChanges.Count, changes.ActivityChanges.Count, user.Username);

            context.SetTextColor(ConsoleTextColor.Green);
            context.WriteLine($"[Update] {user.Username}: +{changes.StatChanges.Count} stats.");
            context.ResetTextColor();
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved changes to database ({Count} users with notifiable updates)", updates.Count);
        }

        try
        {
            await _notificationService.SendConsolidatedUpdatesAsync(updates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Discord notification");
        }

        context.WriteLine("Completed hiscore job.");
    }

    private void SeedMissingEntries(ApplicationDbContext dbContext, OldschoolRunescapeUser user, OldschoolRunescapeHiscoreResponse freshData)
    {
        var missingStats = freshData.Skills
            .Where(fresh => user.Stats!.All(existing => existing.Name != fresh.Name))
            .Select(fresh => fresh.ToEntity(user.Id))
            .ToList();

        var missingActivities = freshData.Activities
            .Where(fresh => user.Activities!.All(existing => existing.Name != fresh.Name))
            .Select(fresh =>
            {
                var activity = fresh.ToEntity(user.Id);

                // Baseline a newly added boss at 0 so its first sync reports the full
                // jump (e.g. 0 -> 164) as a gain, letting players show off existing kills.
                // Leave unranked (-1) entries as-is to avoid a bogus 0 -> -1 diff; they
                // baseline naturally once the player crosses the hiscore threshold.
                if (fresh.Score > 0)
                {
                    activity.Score = 0;
                }

                return activity;
            })
            .ToList();

        if (missingStats.Count == 0 && missingActivities.Count == 0)
        {
            return;
        }

        foreach (var stat in missingStats)
        {
            dbContext.Set<OldschoolRunescapeStat>().Add(stat);
            user.Stats!.Add(stat);
        }

        foreach (var activity in missingActivities)
        {
            dbContext.Set<OldschoolRunescapeActivity>().Add(activity);
            user.Activities!.Add(activity);
        }

        _logger.LogInformation("Seeded {StatCount} new stats and {ActivityCount} new activities as baselines for {Username}",
            missingStats.Count, missingActivities.Count, user.Username);
    }
}
