using GLaDOS.Domain.OsrsWiki;
using GLaDOS.OsrsWiki.Responses;

namespace GLaDOS.Scheduler.Extensions.OsrsWiki;

public static class OsrsWikiSyncMappingExtensions
{
    public static List<OsrsWikiMusic> ToMusicEntities(this OsrsWikiSyncResponse response, Guid userId)
    {
        return response.MusicTracks.Select(track => new OsrsWikiMusic
        {
            OldschoolRunescapeUserId = userId,
            Song = track.Key,
            IsUnlocked = track.Value
        }).ToList();
    }
    
    public static OsrsWikiCollectionLog ToCollectionLogEntity(this OsrsWikiSyncResponse response, Guid userId)
    {
        return new OsrsWikiCollectionLog
        {
            OldschoolRunescapeUserId = userId,
            ItemIds = response.CollectionLog?.ToList() ?? []
        };
    }
    
    public static List<OsrsWikiDiary> ToDiaryEntities(this OsrsWikiSyncResponse response, Guid userId)
    {
        var diaries = new List<OsrsWikiDiary>();
        
        foreach (var (region, data) in response.AchievementDiaries)
        {
            var diary = new OsrsWikiDiary
            {
                OldschoolRunescapeUserId = userId,
                Region = region,
                Easy = data.Easy.ToTier(),
                Medium = data.Medium.ToTier(),
                Hard = data.Hard.ToTier(),
                Elite = data.Elite.ToTier()
            };

            diaries.Add(diary);
        }
        
        return diaries;
    }
    
    public static List<OsrsWikiQuest> ToQuestEntities(this OsrsWikiSyncResponse response, Guid userId)
    {
        return response.Quests.Select(quest => new OsrsWikiQuest
        {
            OldschoolRunescapeUserId = userId,
            Name = quest.Key,
            Status = (QuestStatus)quest.Value
        }).ToList();
    }
    
    private static DiaryTier ToTier(this DiaryDifficulty? dto)
    {
        if (dto == null)
        {
            return new DiaryTier();
        }
        
        return new DiaryTier
        {
            IsComplete = dto.Complete,
            Tasks = dto.Tasks?.ToList() ?? new List<bool>()
        };
    }
}