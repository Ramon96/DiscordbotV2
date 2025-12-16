namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeUser : Entity
{
    public required string Username { get; set; }
    public ICollection<OldschoolRunescapeStat> Stats { get; set; } = new List<OldschoolRunescapeStat>();
    public ICollection<OldschoolRunescapeBoss> Bosses { get; set; } = new List<OldschoolRunescapeBoss>();
}