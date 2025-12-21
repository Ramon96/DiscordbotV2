using Glados.Discord.Services;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using GLaDOS.Scheduler.Extensions;
using Hangfire;
using Microsoft.EntityFrameworkCore;


namespace GLaDOS.Scheduler.Application.OldschoolRunescape;

[DisableConcurrentExecution(60 * 30)] // 30 minutes
[AutomaticRetry(Attempts = 1)]
public class HiscoreJob
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

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting hiscore job");

        List<Guid> userIds;
        var updates = new List<(OldschoolRunescapeUser User, OldschoolRunescapeHiscoreChanges Changes)>();
        
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            userIds = await context.Set<OldschoolRunescapeUser>()
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }

        foreach (var userId in userIds)
        {
            
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

            var freshData = await _client.GetHiScoresByUsernameAsync(user.Username, cancellationToken);
            
            if (freshData == null)
            {
                _logger.LogWarning("User '{Username}' not found in hiscores.", user.Username);
                continue;
            }
            
            if (user.Stats == null || !user.Stats.Any() || user.Activities == null || !user.Activities.Any())
            {
                _logger.LogWarning("User '{Username}' has null Stats or Activities collections.", user.Username);
                
                var newStats = freshData.Skills.Select(s => s.ToEntity(user.Id));
                var newActivities = freshData.Activities.Select(a => a.ToEntity(user.Id));
                
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
                var stat = user.Stats!.First(s => s.Name == change.StatName);
                stat.Level = change.NewLevel;
                stat.Experience = change.NewExperience;
                stat.Rank = change.NewRank;
            }

            foreach (var change in changes.ActivityChanges)
            {
                var activity = user.Activities!.First(a => a.Name == change.ActivityName);
                activity.Score = change.NewScore;
                activity.Rank = change.NewRank;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            
            updates.Add((user, changes));
            
            if  (!updates.Any())
            {
                _logger.LogInformation("No updates to process after checking user: {Username}", user.Username);
                continue;
            }
            
     
            
            _logger.LogInformation("Updated {StatCount} stats and {ActivityCount} activities for {Username}", 
                changes.StatChanges.Count, changes.ActivityChanges.Count, user.Username);
        }
        
        try 
        {
            await _notificationService.SendConsolidatedUpdatesAsync(updates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Discord notification");
        }

        _logger.LogInformation("Completed hiscore job");
    }
}