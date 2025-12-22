namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeStat : Entity
{
    public virtual OldschoolRunescapeUser User { get; init; }
    public Guid OldschoolRunescapeUserId { get; init; }
    
    public uint SkillId { get; init; }
    public required string Name { get; init; }
    public int Rank { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
}