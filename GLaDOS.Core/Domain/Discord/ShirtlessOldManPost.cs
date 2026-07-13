using GLaDOS.Domain;

namespace GLaDOS.Domain.Discord;

public class ShirtlessOldManPost : Entity
{
    public ulong MessageId { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public ulong TaggedDiscordUserId { get; init; }
    public string? TaggedUsername { get; init; }
    public DateTime PostedAt { get; init; }
}
