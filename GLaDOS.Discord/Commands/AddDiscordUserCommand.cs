using Discord;
using Discord.WebSocket;
using GLaDOS.Infra.EntityFramework;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

public class AddDiscordUserCommand : IDiscordCommand
{
    public string Name => "connect";
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;

    public AddDiscordUserCommand(DiscordSocketClient client, IServiceProvider services)
    {
        _client = client;
        _services = services;
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        var discordUserId = command.User.Id;
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            
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