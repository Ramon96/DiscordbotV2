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
using Hangfire.Console;
using Hangfire.Server;
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

    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default)
    {
        var allUpdates = new List<(OldschoolRunescapeUser User, OsrsWikiSyncChanges Changes)>();

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        context.WriteLine("Fetching users eligible for Wiki Sync...");

        var users = await dbContext.OldschoolRunescapeUsers
            .Where(osrsUser => osrsUser.WikiSyncEnabled ||
                               (osrsUser.ModifiedDate < DateTime.UtcNow.AddHours(-24)))
            .Include(user => user.CollectionLog)
            .Include(user => user.Diaries)
            .Include(user => user.Songs)
            .Include(user => user.Quests)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        var progressBar = context.WriteProgressBar();
        context.WriteLine($"Found {users.Count} users to sync.");

        for (int i = 0; i < users.Count; i++)
        {
            var user = users[i];
            var progress = (double)(i + 1) / users.Count * 100;
            progressBar.SetValue(progress);

            try
            {
                var request =
                    await _client.GetOsrsWikiSyncDataAsync(new OsrsWikiSyncRequest { Username = user.Username },
                        cancellationToken);

                if (request == null)
                {
                    _logger.LogWarning("User '{Username}' not found in OSRS Wiki.", user.Username);
                    user.WikiSyncEnabled = false;
                    continue;
                }

                user.ModifiedDate = DateTime.UtcNow;

                if (user.CollectionLog == null || !user.Quests.Any() || !user.Diaries.Any() || !user.Songs.Any())
                {
                    InitializeUserData(user, request);
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
                    context.WriteLine($"- {user.Username}: No new data found.");
                    continue;
                }

                allUpdates.Add((user, changes));

                context.SetTextColor(ConsoleTextColor.Green);
                context.WriteLine($"[Success] {user.Username}: Updated {changes.NewCollectionLogItems.Count} items, {changes.CompletedQuests.Count} quests.");
                context.ResetTextColor();

                _logger.LogInformation("Successfully synced OSRS Wiki data for userId {userId}", user.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error syncing OSRS Wiki data for userId {userId}", user.Id);
                context.SetTextColor(ConsoleTextColor.Red);
                context.WriteLine($"[Error] Failed to sync userId {user.Id}: {e.Message}");
                context.ResetTextColor();
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (allUpdates.Any())
        {
            context.WriteLine("Sending Discord notifications...");
            try
            {
                await _notificationService.SendWikiUpdatesAsync(allUpdates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Discord Wiki notifications");
                context.WriteLine("Failed to send Discord notification.");
            }
        }

        context.WriteLine("Job finished.");
    }

    private void InitializeUserData(OldschoolRunescapeUser user, OsrsWikiSyncResponse request)
    {
        if (user.CollectionLog == null) user.CollectionLog = request.ToCollectionLogEntity(user.Id);
        if (!user.Quests.Any()) user.Quests.AddRange(request.ToQuestEntities(user.Id));
        if (!user.Diaries.Any()) user.Diaries.AddRange(request.ToDiaryEntities(user.Id));
        if (!user.Songs.Any()) user.Songs.AddRange(request.ToMusicEntities(user.Id));

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
