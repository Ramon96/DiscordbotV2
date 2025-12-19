using GLaDOS.Domain.Discord;

namespace GLaDOS.Domain.OldschoolRunescape;

public class OldschoolRunescapeUser : Entity
{
    public required string Username { get; set; }
    public Guid? DiscordUserId { get; set; }
    public DiscordUser? DiscordUser { get; set; }
    public ICollection<OldschoolRunescapeStat> Stats { get; set; } = new List<OldschoolRunescapeStat>();
    public ICollection<OldschoolRunescapeActivity> Activities { get; set; } = new List<OldschoolRunescapeActivity>();
}