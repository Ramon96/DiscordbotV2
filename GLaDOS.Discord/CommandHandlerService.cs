using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

public class CommandHandlerService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IEnumerable<IDiscordCommand> _commands;

    public CommandHandlerService(DiscordSocketClient client, IEnumerable<IDiscordCommand> commands)
    {
        _client = client;
        _commands = commands;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Ready += OnReadyAsync;
        _client.SlashCommandExecuted += OnSlashCommandAsync;

        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Ready -= OnReadyAsync;
        _client.SlashCommandExecuted -= OnSlashCommandAsync;

        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        Console.WriteLine("Discord client is ready, registering slash commands...");

        // For testing - register to specific guild (instant)
        const ulong guildId = 867074325824012379;
        var guild = _client.GetGuild(guildId);

        foreach (var command in _commands)
        {
            Console.WriteLine($"Registering command: {command.Name}");
            if (guild != null)
            {
                await guild.CreateApplicationCommandAsync(command.GetCommandDefinition());
            }
        }
    }

    private async Task OnSlashCommandAsync(SocketSlashCommand command)
    {
        var handler = _commands.FirstOrDefault(currentCommand => currentCommand.Name == command.Data.Name);
        if (handler != null)
        {
            await handler.ExecuteAsync(command);
        }
    }
}