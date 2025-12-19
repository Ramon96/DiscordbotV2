using Discord;
using Discord.WebSocket;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using GLaDOS.OldschoolRunescape.Specifications;
using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Glados.Discord.Commands;

public class ConnectOsrsUser : IDiscordCommand
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ConnectOsrsUser>? _logger;

    public ConnectOsrsUser(IServiceProvider services, ILogger<ConnectOsrsUser>? logger = null)
    {
        _services = services;
        _logger = logger;
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
        var discordUserService = scope.ServiceProvider.GetRequiredService<Glados.Discord.Services.Contracts.IDiscordUserService>();
        var osrsClient = scope.ServiceProvider.GetRequiredService<IOldschoolRunescapeClient>();
        var repository = scope.ServiceProvider.GetRequiredService<GLaDOS.Infra.Repositories.Contracts.IRepository<OldschoolRunescapeUser>>();

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

        var hiscoreData = await osrsClient.GetHiScoresByUsernameAsync(osrsUsername, cancellationToken);

        if (hiscoreData == null)
        {
            await command.RespondAsync($"OSRS user '{osrsUsername}' does not exist.", ephemeral: true);
            return;
        }

        var user = repository.GetByExpressionAsync(new OsrsUserWithUsername(osrsUsername), cancellationToken);

        if (user != null)
        {
            await command.RespondAsync($"OSRS user '{osrsUsername}' is already linked to a Discord account.", ephemeral: true);
            return;
        }

        var osrsUser = new OldschoolRunescapeUser
        {
            Username = osrsUsername,
            DiscordUserId = discordUser.Id,
        };

        await repository.SaveChangesAsync(osrsUser, cancellationToken);
        

        await command.RespondAsync(
            $"Linked `{osrsUsername}` to <@{socketUser.Id}>.",
            ephemeral: false);
    }
}