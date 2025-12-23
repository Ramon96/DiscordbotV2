using GLaDOS.Domain.OldschoolRunescape;

namespace GLaDOS.Domain.OsrsWiki;

public class OsrsWikiMusic : Entity
{
    public virtual OldschoolRunescapeUser User { get; init; }
    public Guid OldschoolRunescapeUserId { get; init; }

    public string Song { get; set; }
    public bool IsUnlocked { get; set; }
}