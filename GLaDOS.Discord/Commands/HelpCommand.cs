using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Glados.Discord.Commands;

/// <summary>
/// Points users at the dashboard's command reference, where every command and its description
/// is listed. The dashboard builds that list from the live command set, so there is nothing to
/// keep in sync here.
///
/// The dashboard URL is read from configuration ("Dashboard:BaseUrl"); when it is not set the
/// command still responds with a helpful message.
/// </summary>
public class HelpCommand : IDiscordCommand
{
    // Aperture orange, matching the dashboard theme.
    private static readonly Color AccentColor = new(0xFF, 0x9E, 0x2C);

    private readonly IConfiguration _configuration;

    public HelpCommand(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Name => "help";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Show where to find the full list of GLaDOS commands.")
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        var baseUrl = _configuration["Dashboard:BaseUrl"]?.Trim().TrimEnd('/');

        var embed = new EmbedBuilder()
            .WithTitle("GLaDOS commands")
            .WithColor(AccentColor);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            embed.WithDescription(
                "Every command and what it does is listed on the GLaDOS dashboard, under **Commands**.");
        }
        else
        {
            var url = $"{baseUrl}/dashboard/commands";
            embed.WithDescription(
                $"See every command and what it does on the **[GLaDOS dashboard]({url})**.");
        }

        await command.RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}
