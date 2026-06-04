using Discord;
using Discord.WebSocket;
using Glados.Discord.Commands;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Infra.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class CommandHandlerService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly IEnumerable<IDiscordCommand> _commands;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;
    private readonly FeatureCommand _featureCommand;

    public CommandHandlerService(
        DiscordSocketClient client,
        IEnumerable<IDiscordCommand> commands,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        FeatureCommand featureCommand)
    {
        _client = client;
        _commands = commands;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
        _featureCommand = featureCommand;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _client.Ready += OnReadyAsync;
        _client.SlashCommandExecuted += OnSlashCommandAsync;
        _client.AutocompleteExecuted += OnAutocompleteAsync;
        _client.ButtonExecuted += OnButtonExecutedAsync;
        _client.ModalSubmitted += OnModalSubmittedAsync;

        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.Ready -= OnReadyAsync;
        _client.SlashCommandExecuted -= OnSlashCommandAsync;
        _client.AutocompleteExecuted -= OnAutocompleteAsync;
        _client.ButtonExecuted -= OnButtonExecutedAsync;
        _client.ModalSubmitted -= OnModalSubmittedAsync;

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

    private async Task OnButtonExecutedAsync(SocketMessageComponent component)
    {
        if (component.Data.CustomId.StartsWith("feature-refine-"))
        {
            await _featureCommand.HandleClarifyButtonAsync(component);
        }
    }

    private async Task OnModalSubmittedAsync(SocketModal modal)
    {
        if (modal.Data.CustomId.StartsWith("feature-clarify-"))
        {
            await _featureCommand.HandleClarifyModalAsync(modal);
        }
    }

    private async Task OnAutocompleteAsync(SocketAutocompleteInteraction interaction)
    {
        if (interaction.Data.CommandName != "lookup")
            return;

        var focused = interaction.Data.Options.FirstOrDefault(o => o.Focused);
        if (focused?.Name != "username")
            return;

        var input = (focused.Value as string ?? "").Trim();

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var matches = await dbContext.Set<OldschoolRunescapeUser>()
            .Where(u => u.Username.ToLower().Contains(input.ToLower()))
            .OrderBy(u => u.Username)
            .Take(25)
            .Select(u => u.Username)
            .ToListAsync();

        var results = matches.Select(name =>
            new AutocompleteResult(name, name)).ToList();

        await interaction.RespondAsync(results);
    }
}
