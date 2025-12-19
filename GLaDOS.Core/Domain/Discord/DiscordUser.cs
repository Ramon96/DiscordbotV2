using GLaDOS.Domain.OldschoolRunescape;

namespace GLaDOS.Domain.Discord;

public class DiscordUser : Entity
{
    public ulong DiscordId { get; set; }
    public ICollection<OldschoolRunescapeUser> OldschoolRunescapeUsers { get; set; } = new List<OldschoolRunescapeUser>();
}