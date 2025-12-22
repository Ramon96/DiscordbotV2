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

public class ConnectOsrsUser : IDiscordCommand
{
    private readonly IServiceProvider _services;

    public ConnectOsrsUser(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "link-osrs-to-discord";
    public SlashCommandProperties GetCommandDefinition()
    {
        var command = new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Link your OSRS account to your Discord account.")
            .AddOption("osrs-username", ApplicationCommandOptionType.String, "Your osrs username", isRequired: true)
            .AddOption("discord-user", ApplicationCommandOptionType.User, "Discord user you want to link the osrs account too", isRequired: true);
        return command.Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var discordUserService = scope.ServiceProvider.GetRequiredService<IDiscordUserService>();
        var osrsClient = scope.ServiceProvider.GetRequiredService<IOldschoolRunescapeClient>();
        var repository = scope.ServiceProvider.GetRequiredService<IRepository<OldschoolRunescapeUser>>();

        var socketUser = command.Data.Options.FirstOrDefault(x => x.Name == "discord-user").Value as SocketUser;
        var osrsUsername = command.Data.Options.FirstOrDefault(x => x.Name == "osrs-username").Value as string;

        if (socketUser == null || osrsUsername == null)
        {
            await command.RespondAsync("Invalid command usage. Please provide both OSRS username and Discord user.", ephemeral: true);
            return;
        }

        var discordUser = await discordUserService.GetDiscordUserAsync(socketUser.Id, cancellationToken);

        if (discordUser == null)
        {
            await discordUserService.AddDiscordUserAsync(socketUser.Id, cancellationToken);
            discordUser = await discordUserService.GetDiscordUserAsync(socketUser.Id, cancellationToken);
        }

        var hiscoreData = await osrsClient.GetHiScoresByUsernameAsync(new OldschoolRunescapeHiscoreRequest { Username = osrsUsername }, cancellationToken);

        if (hiscoreData == null)
        {
            await command.RespondAsync($"OSRS user '{osrsUsername}' does not exist.", ephemeral: true);
            return;
        }

        var user = await repository.GetByExpressionAsync(new OsrsUserWithUsername(osrsUsername), cancellationToken);

        if (user != null)
        {
            await command.RespondAsync($"OSRS user '{osrsUsername}' is already linked to a Discord account.", ephemeral: true);
            return;
        }

        var cleanUsername = osrsUsername.ToLower();
        cleanUsername = char.ToUpper(cleanUsername[0]) + cleanUsername.Substring(1);

        var osrsUser = new OldschoolRunescapeUser
        {
            Username = cleanUsername,
            DiscordUserId = discordUser.Id,
        };

        await repository.SaveChangesAsync(osrsUser, cancellationToken);

        await command.RespondAsync(
            $"Linked `{osrsUsername}` to <@{socketUser.Id}>.",
            ephemeral: false);
    }
}