using GLaDOS.Domain.OldschoolRunescape;

namespace GLaDOS.Domain.OsrsWiki;

public class OsrsWikiDiary : Entity
{
    public virtual OldschoolRunescapeUser User { get; init; }
    public Guid OldschoolRunescapeUserId { get; init; }
   
    public string Region { get; set; }
    
    public DiaryTier Easy { get; set; } = new();
    public DiaryTier Medium { get; set; } = new();
    public DiaryTier Hard { get; set; } = new();
    public DiaryTier Elite { get; set; } = new();
}


public class DiaryTier
{
    public bool IsComplete { get; set; }
    public List<bool> Tasks { get; set; } = new();
}