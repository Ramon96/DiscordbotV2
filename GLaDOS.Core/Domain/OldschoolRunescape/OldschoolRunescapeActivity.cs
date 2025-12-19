namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeActivity : Entity
{
    public virtual OldschoolRunescapeUser User { get; init; }
    public Guid OldschoolRunescapeUserId { get; init; }
    public uint ActivityId { get; init; }
    public required string Name { get; init; }
    public int Rank { get; set; }
    public int Score { get; set; }
}