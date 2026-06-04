using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Glados.Discord.Commands;

public class RandomEmojiCommand : IDiscordCommand
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;

    public RandomEmojiCommand(DiscordSocketClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public string Name => "random-emoji";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Displays a random emoji from this server")
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        var guildIdString = _configuration["Discord:GuildId"];
        if (!ulong.TryParse(guildIdString, out var guildId))
        {
            await command.RespondAsync("Could not determine the server. Contact the bot admin.", ephemeral: true);
            return;
        }

        var guild = _client.GetGuild(guildId);
        if (guild == null)
        {
            await command.RespondAsync("Could not find the server.", ephemeral: true);
            return;
        }

        var emotes = guild.Emotes.ToList();
        if (emotes.Count == 0)
        {
            await command.RespondAsync("This server has no custom emojis.", ephemeral: true);
            return;
        }

        var randomEmote = emotes[Random.Shared.Next(emotes.Count)];
        var format = randomEmote.Animated ? $"<a:{randomEmote.Name}:{randomEmote.Id}>" : $"<:{randomEmote.Name}:{randomEmote.Id}>";

        await command.RespondAsync(format);
    }
}
