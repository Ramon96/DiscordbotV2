using System.Text;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

/// <summary>
/// Lists every command the bot currently exposes, together with its description.
///
/// The list is built at run time from the same <see cref="IDiscordCommand"/> instances the bot
/// registers with Discord, so any newly added command (including ones added via /feature) shows
/// up here automatically. There is no hand-maintained list to keep in sync.
/// </summary>
public class HelpCommand : IDiscordCommand
{
    // Discord caps an embed description at 4096 characters and allows at most 10 embeds per
    // message. We stay comfortably under both so the list can grow without hitting the API limits.
    private const int MaxDescriptionLength = 3900;
    private const int MaxEmbeds = 10;

    // Aperture orange, matching the dashboard theme.
    private static readonly Color AccentColor = new(0xFF, 0x9E, 0x2C);

    private readonly IServiceProvider _services;

    public HelpCommand(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "help";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("List every GLaDOS command and what it does.")
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        // Resolved lazily here rather than injected, so this command can enumerate its peers
        // without creating a constructor dependency cycle on itself.
        var entries = _services.GetServices<IDiscordCommand>()
            .Select(peer => peer.GetCommandDefinition())
            .Select(definition => new CommandEntry(
                definition.Name.IsSpecified ? definition.Name.Value : null,
                definition.Description.IsSpecified ? definition.Description.Value : null))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var lines = entries.Select(entry =>
        {
            var description = string.IsNullOrWhiteSpace(entry.Description)
                ? "_No description provided._"
                : entry.Description;
            return $"**/{entry.Name}** — {description}";
        });

        var embeds = BuildEmbeds(lines, entries.Count);

        await command.RespondAsync(embeds: embeds, ephemeral: true);
    }

    private static Embed[] BuildEmbeds(IEnumerable<string> lines, int commandCount)
    {
        var chunks = new List<StringBuilder> { new() };

        foreach (var line in lines)
        {
            var current = chunks[^1];

            // Start a new embed once appending this line (plus its separating newline) would
            // overflow a single embed description.
            if (current.Length > 0 && current.Length + line.Length + 1 > MaxDescriptionLength)
            {
                current = new StringBuilder();
                chunks.Add(current);
            }

            if (current.Length > 0)
                current.Append('\n');
            current.Append(line);
        }

        var embeds = new List<Embed>();
        for (var i = 0; i < chunks.Count && i < MaxEmbeds; i++)
        {
            var builder = new EmbedBuilder()
                .WithColor(AccentColor)
                .WithDescription(chunks[i].ToString());

            // Title and count live on the first embed only, so a multi-part list reads as one message.
            if (i == 0)
            {
                builder.WithTitle("GLaDOS commands");
                builder.WithFooter($"{commandCount} command{(commandCount == 1 ? "" : "s")} available");
            }

            embeds.Add(builder.Build());
        }

        return embeds.ToArray();
    }

    private sealed record CommandEntry(string? Name, string? Description);
}
