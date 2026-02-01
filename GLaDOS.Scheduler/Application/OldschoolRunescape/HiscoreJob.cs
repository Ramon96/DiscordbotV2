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

[DisableConcurrentExecution(0)] 
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

        List<Guid> userIds;
        var updates = new List<(OldschoolRunescapeUser User, OldschoolRunescapeHiscoreChanges Changes)>();

        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            userIds = await dbContext.Set<OldschoolRunescapeUser>()
                .Select(user => user.Id)
                .ToListAsync(cancellationToken);
        }

        for (int i = 0; i < userIds.Count; i++)
        {
            double progress = (double)(i + 1) / userIds.Count * 100;
            progressBar.SetValue(progress);

            var userId = userIds[i];
            

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await dbContext.Set<OldschoolRunescapeUser>()
                .Include(user => user.Stats)
                .Include(user => user.Activities)
                .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);

            if (user == null)
            {
                continue;
            }

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
                throw; 
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

                await dbContext.SaveChangesAsync(cancellationToken);
                continue;
            }

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

            await dbContext.SaveChangesAsync(cancellationToken);
            updates.Add((user, changes));

            if (!updates.Any())
            {
                _logger.LogInformation("No updates to process after checking user: {Username}", user.Username);
                continue;
            }

            _logger.LogInformation("Updated {StatCount} stats and {ActivityCount} activities for {Username}",
                changes.StatChanges.Count, changes.ActivityChanges.Count, user.Username);
            
            if (changes.HasChanges)
            {
                context.SetTextColor(ConsoleTextColor.Green);
                context.WriteLine($"[Update] {user.Username}: +{changes.StatChanges.Count} stats.");
                context.ResetTextColor();
            }
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
}