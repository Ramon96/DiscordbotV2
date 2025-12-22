using Discord;
using Discord.WebSocket;
using Glados.Discord.Services.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

public class AddDiscordUserCommand : IDiscordCommand
{
    public string Name => "connect";
    private readonly IServiceProvider _services;

    public AddDiscordUserCommand(IServiceProvider services)
    {
        _services = services;
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