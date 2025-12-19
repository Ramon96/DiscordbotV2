using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using GLaDOS.OldschoolRunescape.Responses;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GLaDOS.Scheduler.Application.OldschoolRunescape;

[DisableConcurrentExecution(60 * 30)] // 30 minutes
[AutomaticRetry(Attempts = 1)]
public class HiscoreJob
{
    private readonly ILogger<HiscoreJob> _logger;
    private readonly IOldschoolRunescapeClient _client;
    private readonly ApplicationDbContext _context;

    public HiscoreJob(ILogger<HiscoreJob> logger, IOldschoolRunescapeClient client, ApplicationDbContext context)
    {
        _logger = logger;
        _client = client;
        _context = context;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting hiscore job");

        // 1. Haal alleen IDs op om geheugen schoon te houden
        var userIds = await _context.Set<OldschoolRunescapeUser>()
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            // Reset context voor elke user
            _context.ChangeTracker.Clear();

            // Laad user + data
            var user = await _context.Set<OldschoolRunescapeUser>()
                .Include(u => u.Stats)
                .Include(u => u.Activities)
                .AsNoTracking() // Belangrijk: voorkomt tracking issues bij het ophalen
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null) continue;

            _logger.LogInformation("Fetching hiscores for user: {Username}", user.Username);
            var freshData = await _client.GetHiScoresByUsernameAsync(user.Username, cancellationToken);

            if (freshData == null)
            {
                _logger.LogWarning("User '{Username}' not found in hiscores.", user.Username);
                continue;
            }

            // --- SCENARIO 1: NIEUWE USER (First Time Sync) ---
            if (user.Stats == null || !user.Stats.Any())
            {
                _logger.LogInformation("First-time sync for user: {Username}. Adding stats one by one.", user.Username);

                // 1. Voeg Skills toe
                foreach (var skill in freshData.Skills)
                {
                    try 
                    {
                        var newStat = new OldschoolRunescapeStat
                        {
                            SkillId = (uint)skill.Id,
                            Name = skill.Name,
                            Level = skill.Level,
                            Experience = skill.Xp,
                            Rank = skill.Rank,
                            OldschoolRunescapeUserId = userId // Gebruik de ID direct
                        };

                        _context.Set<OldschoolRunescapeStat>().Add(newStat);
                        await _context.SaveChangesAsync(cancellationToken);
                        
                        // Succesvol opgeslagen? Vergeet het object zodat we geen conflict krijgen bij de volgende
                        _context.ChangeTracker.Clear();
                    }
                    catch (Exception ex)
                    {
                        // Als dit faalt (bijv. Duplicate Key), loggen we het en gaat de loop DOOR
                        _logger.LogError(ex, "Failed to save Stat '{StatName}' for user '{User}'.", skill.Name, user.Username);
                        _context.ChangeTracker.Clear(); // Forceer reset ook bij error
                    }
                }

                // 2. Voeg Activities toe
                foreach (var activity in freshData.Activities)
                {
                    try
                    {
                        var newActivity = new GLaDOS.Domain.OldschoolRunescape.OldschoolRunescapeActivity
                        {
                            ActivityId = (uint)activity.Id,
                            Name = activity.Name,
                            Score = activity.Score,
                            Rank = activity.Rank,
                            OldschoolRunescapeUserId = userId
                        };

                        _context.Set<GLaDOS.Domain.OldschoolRunescape.OldschoolRunescapeActivity>().Add(newActivity);
                        await _context.SaveChangesAsync(cancellationToken);
                        _context.ChangeTracker.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save Activity '{ActivityName}' for user '{User}'.", activity.Name, user.Username);
                        _context.ChangeTracker.Clear();
                    }
                }
                
                continue; // Ga naar de volgende user
            }

            // --- SCENARIO 2: UPDATES ---
            // Voor updates moeten we de user wel tracken, dus halen we hem opnieuw op (tracked)
            var trackedUser = await _context.Set<OldschoolRunescapeUser>()
                .Include(u => u.Stats)
                .Include(u => u.Activities)
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (trackedUser == null) continue;

            var changes = DetectChanges(trackedUser, freshData); // Let op: DetectChanges methode moet static zijn

            if (!changes.HasChanges)
            {
                _logger.LogInformation("No changes detected for user: {Username}", user.Username);
                continue;
            }

            foreach (var statChange in changes.StatChanges)
            {
                var stat = trackedUser.Stats!.First(s => s.Name == statChange.StatName);
                stat.Level = statChange.NewLevel;
                stat.Experience = statChange.NewExperience;
                stat.Rank = statChange.NewRank;
            }

            foreach (var activityChange in changes.ActivityChanges)
            {
                var activity = trackedUser.Activities!.First(a => a.Name == activityChange.ActivityName);
                activity.Score = activityChange.NewScore;
                activity.Rank = activityChange.NewRank;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Changes saved for {Username}", user.Username);
        }

        _logger.LogInformation("Completed hiscore job");
    }

    // --- Helper Methods ---
    private static HiscoreChanges DetectChanges(OldschoolRunescapeUser user, OldschoolRunescapeHiscoreResponse freshData)
    {
        var changes = new HiscoreChanges();

        foreach (var freshStat in freshData.Skills)
        {
            var existingStat = user.Stats!.FirstOrDefault(s => s.Name == freshStat.Name);
            if (existingStat != null && existingStat.Level != freshStat.Level)
            {
                changes.StatChanges.Add(new StatChange
                {
                    StatName = freshStat.Name,
                    OldLevel = existingStat.Level,
                    NewLevel = freshStat.Level,
                    OldExperience = existingStat.Experience,
                    NewExperience = freshStat.Xp,
                    OldRank = existingStat.Rank,
                    NewRank = freshStat.Rank
                });
            }
        }

        foreach (var freshActivity in freshData.Activities)
        {
            var existingActivity = user.Activities!.FirstOrDefault(a => a.Name == freshActivity.Name);
            if (existingActivity != null && existingActivity.Score != freshActivity.Score)
            {
                changes.ActivityChanges.Add(new ActivityChange
                {
                    ActivityName = freshActivity.Name,
                    OldScore = existingActivity.Score,
                    NewScore = freshActivity.Score,
                    ScoreDifference = freshActivity.Score - existingActivity.Score,
                    OldRank = existingActivity.Rank,
                    NewRank = freshActivity.Rank
                });
            }
        }
        return changes;
    }
}

// ... Je StatChange, ActivityChange en HiscoreChanges classes blijven hetzelfde
public class HiscoreChanges
{
    public System.Collections.Generic.List<StatChange> StatChanges { get; set; } = new();
    public System.Collections.Generic.List<ActivityChange> ActivityChanges { get; set; } = new();
    public bool HasChanges => StatChanges.Any() || ActivityChanges.Any();
}

public class StatChange
{
    public required string StatName { get; set; }
    public int OldLevel { get; set; }
    public int NewLevel { get; set; }
    public ulong OldExperience { get; set; }
    public ulong NewExperience { get; set; }
    public int OldRank { get; set; }
    public int NewRank { get; set; }
}

public class ActivityChange
{
    public required string ActivityName { get; set; }
    public int OldScore { get; set; }
    public int NewScore { get; set; }
    public int ScoreDifference { get; set; }
    public int OldRank { get; set; }
    public int NewRank { get; set; }
}