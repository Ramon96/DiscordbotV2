using GLaDOS.Domain.OldschoolRunescape;

namespace GLaDOS.Domain.OsrsWiki;

public class OsrsWikiDiary : Entity
{
    public virtual OldschoolRunescapeUser User { get; init; }
    public Guid OldschoolRunescapeUserId { get; init; }
   
    public string Region { get; set; }
    public List<DiaryDifficulty> Difficulties { get; set; } = new();
}

public class DiaryDifficulty
{
    public Difficulty Difficulty { get; set; }
    public bool IsCompleted { get; set; }
    public List<bool> Tasks { get; set; } = new();
}

public enum Difficulty
{
    Easy,
    Medium,
    Hard,
    Elite
}