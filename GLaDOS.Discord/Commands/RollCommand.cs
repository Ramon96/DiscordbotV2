using Discord;
using Discord.WebSocket;

namespace Glados.Discord.Commands;

public class RollCommand : IDiscordCommand
{
    public string Name => "roll";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Roll a random number between 1 and 100")
            .Build();
    }

    public Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        var result = Random.Shared.Next(1, 101);
        return command.RespondAsync($"You rolled a **{result}**!");
    }
}
