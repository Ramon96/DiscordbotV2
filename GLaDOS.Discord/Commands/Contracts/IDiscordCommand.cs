using Discord;
using Discord.WebSocket;

public interface IDiscordCommand
{
    string Name { get; }
    SlashCommandProperties GetCommandDefinition();
    Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default);
}