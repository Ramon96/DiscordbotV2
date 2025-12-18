

using GLaDOS.Domain.Discord;

namespace Glados.Discord.Services.Contracts;

public interface IDiscordUserService
{
    Task AddDiscordUserAsync(ulong discordId, CancellationToken cancellationToken = default);
    Task<DiscordUser?> GetDiscordUserAsync(ulong discordId, CancellationToken cancellationToken = default);
}