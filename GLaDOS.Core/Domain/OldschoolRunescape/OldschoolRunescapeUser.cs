using GLaDOS.Domain.Discord;
using GLaDOS.Domain.OsrsWiki;

namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeUser : Entity
{
    public required string Username { get; set; }
    public Guid? DiscordUserId { get; init; }
    public DiscordUser? DiscordUser { get; init; }
    public bool WikiSyncEnabled { get; set; } = true;
    public virtual List<OldschoolRunescapeStat> Stats { get; init; } = [];
    public virtual List<OldschoolRunescapeActivity> Activities { get; init; } = [];
    
    
    public virtual List<OsrsWikiQuest> Quests { get; init; }  = [];
    public virtual List<OsrsWikiDiary> Diaries { get; init; } = [];
    public virtual List<OsrsWikiMusic> Songs { get; init; } = [];
    public virtual OsrsWikiCollectionLog? CollectionLog { get; set; }

}