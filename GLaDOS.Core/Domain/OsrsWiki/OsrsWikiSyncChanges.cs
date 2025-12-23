namespace GLaDOS.Domain.OsrsWiki;

public class OsrsWikiSyncChanges
{
    public bool HasChanges => NewCollectionLogItems.Any() || CompletedQuests.Any() || CompletedDiaries.Any() || UnlockedTracks.Any();

    public List<int> NewCollectionLogItems { get; set; } = new();
    public List<string> CompletedQuests { get; set; } = new();
    public List<(string Region, string Tier)> CompletedDiaries { get; set; } = new();
    public List<string> UnlockedTracks { get; set; } = new();
}

