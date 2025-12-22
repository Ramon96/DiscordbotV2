using Glados.Discord.Services.Contracts;
using Glados.Discord.Specifications;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.Repositories.Contracts;

namespace Glados.Discord.Services;

public class DiscordUserService : IDiscordUserService
{
    private readonly IRepository<DiscordUser> _repository;

    public DiscordUserService(IRepository<DiscordUser> repository)
    {
        _repository = repository;
    }

    public async Task AddDiscordUserAsync(ulong discordId, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByExpressionAsync(new DiscordUserWithDiscordId(discordId), cancellationToken);

        if (user is not null)
        {
          throw new InvalidOperationException($"Discord user with ID {discordId} already exists.");
        }
        
        var discordUser = new DiscordUser
        {
            DiscordId = discordId
        };

        await _repository.AddAsync(discordUser, cancellationToken);
    }

    public async Task<DiscordUser?> GetDiscordUserAsync(ulong discordId, CancellationToken cancellationToken)
    {
        var discordUser = new DiscordUserWithDiscordId(discordId);

        var user = await _repository.GetByExpressionAsync(discordUser, cancellationToken);

        return user;
    }
}