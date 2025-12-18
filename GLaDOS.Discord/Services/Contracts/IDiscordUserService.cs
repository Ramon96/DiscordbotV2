

using GLaDOS.Domain.Discord;

namespace Glados.Discord.Services.Contracts;

public interface IDiscordUserService
{
    Task<bool> AddDiscordUserAsync(ulong discordId);
    Task<DiscordUser?> GetDiscordUserAsync(ulong discordId);
}