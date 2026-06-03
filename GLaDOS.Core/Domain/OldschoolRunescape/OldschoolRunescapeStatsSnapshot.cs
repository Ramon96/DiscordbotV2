namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeStatsSnapshot : Entity
{
    public Guid OldschoolRunescapeUserId { get; init; }
    public virtual OldschoolRunescapeUser User { get; init; }
    public DateTime SnapshotDate { get; init; }
    public uint SkillId { get; init; }
    public required string Name { get; init; }
    public int Level { get; init; }
    public long Experience { get; init; }
    public int Rank { get; init; }
}
