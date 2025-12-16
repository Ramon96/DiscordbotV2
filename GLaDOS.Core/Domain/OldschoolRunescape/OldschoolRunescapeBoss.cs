namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeBoss : Entity
{
    public Guid RunescapeUserId { get; set; }
    public required OldschoolRunescapeUser User { get; set; }
    public int BossId { get; set; }
    public required string Name { get; set; }
    public long Rank { get; set; }
    public int Score { get; set; }
}