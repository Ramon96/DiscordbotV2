namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeStat : Entity
{
    public Guid RunescapeUserId { get; set; }
    public required OldschoolRunescapeUser User { get; set; }
    public int SkillId { get; set; }
    public required string Name { get; set; }
    public long Rank { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
}