namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeActivitySnapshot : Entity
{
    public Guid OldschoolRunescapeUserId { get; init; }
    public virtual OldschoolRunescapeUser User { get; init; }
    public DateTime SnapshotDate { get; init; }
    public uint ActivityId { get; init; }
    public required string Name { get; init; }
    public int Score { get; init; }
    public int Rank { get; init; }
}
