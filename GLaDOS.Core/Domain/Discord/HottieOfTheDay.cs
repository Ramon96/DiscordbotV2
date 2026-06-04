namespace GLaDOS.Domain.Discord;

public class HottieOfTheDay : Entity
{
    public ulong DiscordUserId { get; init; }
    public DateOnly DateAwarded { get; init; }
}
