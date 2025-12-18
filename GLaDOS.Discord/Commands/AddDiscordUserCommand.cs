using Discord;
using Discord.WebSocket;
using Glados.Discord.Services.Contracts;
using GLaDOS.Infra.EntityFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Glados.Discord.Commands;

public class AddDiscordUserCommand : IDiscordCommand
{
    public string Name => "connect";
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly ILogger<AddDiscordUserCommand> _logger;

    public AddDiscordUserCommand(DiscordSocketClient client, IServiceProvider services, ILogger<AddDiscordUserCommand> logger)
    {
        _client = client;
        _services = services;
        _logger = logger;
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        var discordUserId = command.User.Id;
        using var scope = _services.CreateScope();
        var discordUserService = scope.ServiceProvider.GetRequiredService<IDiscordUserService>();

        try
        {
           var discordUser = await discordUserService.GetDiscordUserAsync(discordUserId, cancellationToken);
           
           if (discordUser is not null)
           {
               await command.RespondAsync("Your Discord account is already connected to GLaDOS.", ephemeral: true);
                return;
           }
           
           await discordUserService.AddDiscordUserAsync(discordUserId, cancellationToken);
           await command.RespondAsync("Your Discord account has been successfully connected to GLaDOS!", ephemeral: true);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public SlashCommandProperties GetCommandDefinition()
    {
        var command = new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Connect your Discord account to GLaDOS.");

        return command.Build();
    }
}