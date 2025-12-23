using GLaDOS.Domain.OldschoolRunescape;

namespace GLaDOS.Domain.OsrsWiki;

public class OsrsWikiCollectionLog : Entity
{
    public virtual OldschoolRunescapeUser User { get; init; }
    public Guid OldschoolRunescapeUserId { get; init; }
    
    public List<int> ItemIds { get; set; } = new();
}