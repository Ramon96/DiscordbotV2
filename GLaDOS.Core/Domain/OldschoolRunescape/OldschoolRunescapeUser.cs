using GLaDOS.Domain.Discord;
using GLaDOS.Domain.OsrsWiki;

namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeUser : Entity
{
    public required string Username { get; set; }
    public Guid? DiscordUserId { get; init; }
    public DiscordUser? DiscordUser { get; init; }
    public bool WikiSyncEnabled { get; set; } = true;
    public ICollection<OldschoolRunescapeStat> Stats { get; init; } = new List<OldschoolRunescapeStat>();
    public ICollection<OldschoolRunescapeActivity> Activities { get; init; } = new List<OldschoolRunescapeActivity>();
    
    
    public ICollection<OsrsWikiQuest> Quests { get; init; } = new List<OsrsWikiQuest>();
    public ICollection<OsrsWikiDiary> Diaries { get; init; } = new List<OsrsWikiDiary>();
    public ICollection<OsrsWikiMusic> Songs { get; init; } = new List<OsrsWikiMusic>();
    public OsrsWikiCollectionLog? CollectionLog { get; init; }

}