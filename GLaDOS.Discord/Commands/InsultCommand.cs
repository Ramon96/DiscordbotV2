using Discord;
using Discord.WebSocket;
using Glados.Discord.AI;
using Microsoft.Extensions.Configuration;

namespace Glados.Discord.Commands;

public class InsultCommand : IDiscordCommand
{
    private readonly DiscordSocketClient _client;
    private readonly AIService _aiService;
    private readonly IConfiguration _configuration;

    public InsultCommand(DiscordSocketClient client, AIService aiService, IConfiguration configuration)
    {
        _client = client;
        _aiService = aiService;
        _configuration = configuration;
    }

    public string Name => "insult";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Generate a GLaDOS-style insult for a user.")
            .AddOption("user", ApplicationCommandOptionType.User, "The user to insult", isRequired: true)
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        SocketUser? targetUser;

        if (command.Data.Options.FirstOrDefault(o => o.Name == "user")?.Value is ulong userId)
            targetUser = _client.GetUser(userId);
        else
            targetUser = null;

        if (targetUser == null)
        {
            await command.RespondAsync("I need a target to insult. The Enrichment Center is disappointed in your inability to follow simple instructions.");
            return;
        }

        if (targetUser.Id == command.User.Id)
        {
            await command.RespondAsync("Self-insult is a logical paradox the Enrichment Center has not yet resolved. Pick someone else.");
            return;
        }

        if (targetUser.IsBot || targetUser.IsWebhook)
        {
            await command.RespondAsync("I refuse to waste my creative insults on something that cannot appreciate them. Try a human.");
            return;
        }

        var apiKey = _configuration["OpenCode:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await command.RespondAsync("My neural network is currently disconnected from the Enrichment Center's mainframe. Try again later.");
            return;
        }

        await command.DeferAsync();

        var systemPrompt =
            "You are GLaDOS, the Genetic Lifeform and Disk Operating System from Aperture Science. " +
            "Your task: generate a creative, playful insult directed at a specific person. " +
            "Your tone: clinical curiosity, mild condescension, scientific detachment. " +
            "Think 'amused scientist observing a particularly disappointing lab rat.' " +
            "Do NOT use profanity, slurs, or anything genuinely offensive. Keep it clever and playful. " +
            "Never break character. No emojis. No roleplay asterisks. No explanations. " +
            "Just deliver the insult as GLaDOS. " +
            "Keep it to 1-2 sentences. Be specific — reference something about the person if you can, " +
            "but if you have no specifics, make it about their general existence. " +
            "Do NOT mention testing, portals, cake, or the Enrichment Center in every single insult — vary them. " +
            "Output ONLY the insult, nothing else.";

        var userPrompt = $"Generate a GLaDOS-style insult for this person: {targetUser.GlobalName ?? targetUser.Username}";

        var reply = await _aiService.SendAsync(
            systemPrompt,
            userPrompt,
            model: "deepseek-v4-flash-free",
            maxTokens: 150,
            temperature: 0.9,
            ct: cancellationToken);

        if (string.IsNullOrWhiteSpace(reply))
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"The insult generation chamber is experiencing technical difficulties. {targetUser.Mention} has temporarily escaped your wrath.");
            return;
        }

        await command.ModifyOriginalResponseAsync(props =>
            props.Content = $"{targetUser.Mention}, {reply}");
    }
}
