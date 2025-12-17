namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeStat : Entity
{
    public Guid RunescapeUserId { get; init; }
    public required OldschoolRunescapeUser User { get; init; }
    public int SkillId { get; init; }
    public required string Name { get; init; }
    public long Rank { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
}