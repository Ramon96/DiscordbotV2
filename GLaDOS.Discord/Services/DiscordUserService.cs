using Glados.Discord.Services.Contracts;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.Repositories.Contracts;

namespace Glados.Discord.Services;

public class DiscordUserService : IDiscordUserService
{
    private readonly IRepository<DiscordUser> _repository;

    public Task<bool> AddDiscordUserAsync(ulong discordId)
    {
        throw new NotImplementedException();
    }

    public Task<DiscordUser?> GetDiscordUserAsync(ulong discordId)
    {
        throw new NotImplementedException();
    }
}