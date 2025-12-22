using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

public class CommandHandlerService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IEnumerable<IDiscordCommand> _commands;
    private readonly IConfiguration _configuration;

    public CommandHandlerService(DiscordSocketClient client, IEnumerable<IDiscordCommand> commands, IConfiguration configuration)
    {
        _client = client;
        _commands = commands;
        _configuration = configuration;
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
        
        var guildIdString = _configuration["Discord:GuildId"];
        if (string.IsNullOrWhiteSpace(guildIdString))
        {
            Console.WriteLine("Guild ID not configured, skipping guild-specific command registration.");
            return;
        }

        if (!ulong.TryParse(guildIdString, out var guildId))
        {
            Console.WriteLine("Invalid Guild ID format in configuration.");
            return;
        }

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