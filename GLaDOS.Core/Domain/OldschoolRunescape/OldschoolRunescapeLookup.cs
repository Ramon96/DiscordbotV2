namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeLookup : Entity
{
    public Guid OldschoolRunescapeUserId { get; init; }
    public virtual OldschoolRunescapeUser User { get; init; }
    public ulong DiscordUserId { get; init; }
    public DateTime LookupDate { get; set; }
}
