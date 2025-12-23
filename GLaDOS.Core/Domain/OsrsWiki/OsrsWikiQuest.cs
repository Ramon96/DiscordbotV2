using GLaDOS.Domain.OldschoolRunescape;

namespace GLaDOS.Domain.OsrsWiki;

public class OsrsWikiQuest : Entity
{
    public virtual OldschoolRunescapeUser User { get; init; }
    public Guid OldschoolRunescapeUserId { get; init; }
    
    public required string Name { get; init; }
    public QuestStatus Status { get; set; }
}

public enum QuestStatus
{
    NotStarted,
    InProgress,
    Completed
}