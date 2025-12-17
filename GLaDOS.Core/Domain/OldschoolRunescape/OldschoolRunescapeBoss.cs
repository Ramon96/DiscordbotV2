namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeBoss : Entity
{
    public Guid RunescapeUserId { get; init; }
    public required OldschoolRunescapeUser User { get; init; }
    public int BossId { get; init; }
    public required string Name { get; init; }
    public long Rank { get; set; }
    public int Score { get; set; }
}