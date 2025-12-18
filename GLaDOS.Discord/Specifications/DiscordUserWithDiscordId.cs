using System.Linq.Expressions;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.Specifications;

namespace Glados.Discord.Specifications;

public class DiscordUserWithDiscordId : SpecificationBase<DiscordUser>
{
    private readonly ulong _discordId;

    public DiscordUserWithDiscordId(ulong discordId)
    {
        _discordId = discordId;
    }

    public override Expression<Func<DiscordUser, bool>> Criteria => discordUser => discordUser.DiscordId ==  _discordId;
}