using Glados.Discord.Services;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Domain.OsrsWiki;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.OsrsWiki.Clients.Contracts;
using GLaDOS.OsrsWiki.Requests;
using GLaDOS.OsrsWiki.Responses;
using GLaDOS.Scheduler.Application.Hangfire.Contracts;
using GLaDOS.Scheduler.Extensions.OsrsWiki;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Application;


[DisableConcurrentExecution(0)]
[AutomaticRetry(Attempts = 0)]
public class OsrsWikiSyncJob : IHangfireJob
{
    private readonly IOsrsWikiSyncClient _client;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OsrsWikiSyncJob> _logger;
    private readonly DiscordNotificationService _notificationService;
    
    public OsrsWikiSyncJob(IOsrsWikiSyncClient client, IServiceScopeFactory scopeFactory, ILogger<OsrsWikiSyncJob> logger, DiscordNotificationService notificationService)
    {
        _client = client;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> userIds;
        var allUpdates = new List<(OldschoolRunescapeUser User, OsrsWikiSyncChanges Changes)>();

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            userIds = await context.OldschoolRunescapeUsers
                .Where(osrsUser => osrsUser.WikiSyncEnabled ||
                                   (osrsUser.ModifiedDate < DateTime.UtcNow.AddHours(-24)))
                .Select(user => user.Id)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        foreach (var userId in userIds)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var user = await dbContext.Set<OldschoolRunescapeUser>()
                    .Include(user => user.CollectionLog)
                    .Include(user => user.Diaries)
                    .Include(user => user.Songs)
                    .Include(user => user.Quests)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                if (user == null)
                {
                    continue;
                }
                
                var request =
                    await _client.GetOsrsWikiSyncDataAsync(new OsrsWikiSyncRequest { Username = user.Username },
                        cancellationToken);
                
                if (request == null)
                {
                    _logger.LogWarning("User '{Username}' not found in OSRS Wiki.", user.Username);
                    user.WikiSyncEnabled = false;
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue;
                }
                
                if (user.CollectionLog == null || !user.Quests.Any() || !user.Diaries.Any() || !user.Songs.Any())
                {
                    InitializeUserData(user, request);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    continue; 
                }

                var changes = new OsrsWikiSyncChanges();
                
                var incomingLogIds = request.CollectionLog?.ToList() ?? [];
                var newItems = incomingLogIds.Except(user.CollectionLog.ItemIds).ToList();
                
                if (newItems.Any())
                {
                    changes.NewCollectionLogItems.AddRange(newItems);
                    user.CollectionLog.ItemIds = incomingLogIds;
                }
                
                var incomingQuests = request.ToQuestEntities(user.Id);
                var currentQuestMap = user.Quests.ToDictionary(quest => quest.Name, quest => quest);
                
                var questsToProcess = incomingQuests.Where(incoming => 
                {
                    if (currentQuestMap.TryGetValue(incoming.Name, out var existing))
                    {
                        return incoming.Status > existing.Status;
                    }
                    return true;
                }).ToList();
                
                foreach (var incQuest in questsToProcess)
                {
                    if (currentQuestMap.TryGetValue(incQuest.Name, out var existing))
                    {
                        existing.Status = incQuest.Status;
                    }
                    else
                    {
                        user.Quests.Add(incQuest);
                    }
                    
                    if (incQuest.Status == QuestStatus.Completed)
                    {
                        changes.CompletedQuests.Add(incQuest.Name);
                    }
                }
                
                var incomingSongs = request.ToMusicEntities(user.Id);
                var currentSongMap = user.Songs.ToDictionary(song => song.Song, song => song);

                foreach (var incSong in incomingSongs)
                {
                    if (currentSongMap.TryGetValue(incSong.Song, out var existing))
                    {

                        if (incSong.IsUnlocked && !existing.IsUnlocked)
                        {
                            existing.IsUnlocked = true;
                            changes.UnlockedTracks.Add(incSong.Song);
                        }
                    }
                    else
                    {
                        user.Songs.Add(incSong);

                        if (incSong.IsUnlocked)
                        {
                            changes.UnlockedTracks.Add(incSong.Song);
                        }
                    }
                }

                var currentDiaryMap = user.Diaries.ToDictionary(diary => diary.Region, diary => diary);
                
                if (request.AchievementDiaries != null)
                {
                    foreach (var (region, apiData) in request.AchievementDiaries)
                    {
                        if (!currentDiaryMap.TryGetValue(region, out var dbDiary))
                        {
                            dbDiary = new OsrsWikiDiary 
                            { 
                                Region = region, 
                                OldschoolRunescapeUserId = user.Id 
                            };
                            user.Diaries.Add(dbDiary);
                            continue;
                        }
                        
                        CheckAndUpdateTier(dbDiary.Easy, apiData.Easy, region, "Easy", changes);
                        CheckAndUpdateTier(dbDiary.Medium, apiData.Medium, region, "Medium", changes);
                        CheckAndUpdateTier(dbDiary.Hard, apiData.Hard, region, "Hard", changes);
                        CheckAndUpdateTier(dbDiary.Elite, apiData.Elite, region, "Elite", changes);
                    }
                }
                
                if (!changes.HasChanges)
                {
                    _logger.LogInformation("No changes detected for user: {Username}", user.Username);
                    continue;
                }
                
                user.ModifiedDate = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                allUpdates.Add((user, changes));
                
                
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                
                _logger.LogInformation("Successfully synced OSRS Wiki data for userId {userId}", userId);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error syncing OSRS Wiki data for userId {userId}", userId);
            }
        }
        
        if (allUpdates.Any())
        {
            try
            {
                await _notificationService.SendWikiUpdatesAsync(allUpdates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Discord Wiki notifications");
            }
        }
    }
    
    private void InitializeUserData(OldschoolRunescapeUser user, OsrsWikiSyncResponse request)
    {
        if (user.CollectionLog == null) user.CollectionLog = request.ToCollectionLogEntity(user.Id);
        if (!user.Quests.Any()) user.Quests.AddRange(request.ToQuestEntities(user.Id));
        if (!user.Diaries.Any()) user.Diaries.AddRange(request.ToDiaryEntities(user.Id));
        if (!user.Songs.Any()) user.Songs.AddRange(request.ToMusicEntities(user.Id));
        
        user.ModifiedDate = DateTime.UtcNow;
        user.WikiSyncEnabled = true;
    }
    
    private void CheckAndUpdateTier(DiaryTier dbTier, DiaryDifficulty? apiTier, string region, string tierName, OsrsWikiSyncChanges changes)
    {
        if (apiTier == null) return;
        
        dbTier.Tasks = apiTier.Tasks.ToList();
        
        if (apiTier.Complete && !dbTier.IsComplete)
        {
            dbTier.IsComplete = true;
            changes.CompletedDiaries.Add((region, tierName));
        }
    }

}
