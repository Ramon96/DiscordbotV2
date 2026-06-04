using Discord;
using Discord.WebSocket;

namespace Glados.Discord.Commands;

public class PingCommand : IDiscordCommand
{
    public string Name => "ping";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Returns pong")
            .Build();
    }

    public Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        return command.RespondAsync("pong");
    }
}
