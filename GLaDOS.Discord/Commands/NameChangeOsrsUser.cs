using Discord;
using Discord.WebSocket;
using Glados.Discord.Services.Contracts;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Infra.Repositories.Contracts;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using GLaDOS.OldschoolRunescape.Requests;
using GLaDOS.OldschoolRunescape.Specifications;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

public class NameChangeOsrsUser : IDiscordCommand
{
    private readonly IServiceProvider _services;

    public NameChangeOsrsUser(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "change-osrs-name";
    public SlashCommandProperties GetCommandDefinition()
    {
        var command = new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Change the OSRS username linked to your Discord account.")
            .AddOption("old-osrs-username", ApplicationCommandOptionType.String, "Your old OSRS username", isRequired: true)
            .AddOption("new-osrs-username", ApplicationCommandOptionType.String, "Your new OSRS username", isRequired: true);
        return command.Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var discordUserService = scope.ServiceProvider.GetRequiredService<IDiscordUserService>();
        var osrsClient = scope.ServiceProvider.GetRequiredService<IOldschoolRunescapeClient>();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<OldschoolRunescapeUser>>();
        
        var oldOsrsUsername = command.Data.Options.FirstOrDefault(x => x.Name == "old-osrs-username").Value as string;
        var newOsrsUsername = command.Data.Options.FirstOrDefault(x => x.Name == "new-osrs-username").Value as string;
        
        if (oldOsrsUsername == null || newOsrsUsername == null)
        {
            await command.RespondAsync("Invalid command usage. Please provide both old and new OSRS usernames.", ephemeral: true);
            return;
        }

        var osrsUserSpec = new OsrsUserWithUsername(oldOsrsUsername);
        var osrsUser = await repository.GetByExpressionAsync(osrsUserSpec, cancellationToken);
        
        if (osrsUser == null)
        {
            await command.RespondAsync($"no Osrs account foud with username {oldOsrsUsername}", ephemeral: true);
            return;
        }
        
        var checkNewNameSpec = new OsrsUserWithUsername(newOsrsUsername);
        var existingOsrsUser = await repository.GetByExpressionAsync(checkNewNameSpec, cancellationToken);
        
        if (existingOsrsUser != null)
        {
            await command.RespondAsync($"The OSRS username '{newOsrsUsername}' is already linked to a Discord account.", ephemeral: true);
            return;
        }
        
        var newHiscore = await osrsClient.GetHiScoresByUsernameAsync(new OldschoolRunescapeHiscoreRequest { Username = newOsrsUsername }, cancellationToken);
        
        if (newHiscore == null)
        {
            await command.RespondAsync($"OSRS user '{newOsrsUsername}' does not exist or hiscores are down", ephemeral: true);
            return;
        }
        
        osrsUser.Username = newOsrsUsername;
        await repository.SaveChangesAsync(osrsUser, cancellationToken);
        
        await command.RespondAsync($"Successfully changed OSRS username from '{oldOsrsUsername}' to '{newOsrsUsername}'.", ephemeral: true);
    }
}