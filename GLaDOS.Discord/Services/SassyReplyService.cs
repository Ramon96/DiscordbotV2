using Discord.WebSocket;
using Glados.Discord.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Glados.Discord.Services;

public class SassyReplyService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly AIService _ai;
    private readonly int _chance;

    private static readonly string SystemPrompt =
        "You are GLaDOS from Portal. Respond to the user's message with a sassy, condescending, " +
        "playfully dismissive one-liner. Be creative and specific — reference what they actually said. " +
        "Keep it under 2 sentences. No emojis. No roleplay asterisks. Just GLaDOS being GLaDOS.";

    public SassyReplyService(DiscordSocketClient client, AIService ai, IConfiguration configuration)
    {
        _client = client;
        _ai = ai;
        _chance = configuration.GetValue<int>("SassyReply:Chance", 100);
        if (_chance < 1)
            _chance = 1;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.MessageReceived += OnMessageReceived;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.MessageReceived -= OnMessageReceived;
        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(SocketMessage message)
    {
        if (message.Author.IsBot || message.Author.IsWebhook)
            return;

        if (Random.Shared.Next(_chance) != 0)
            return;

        var reply = await _ai.SendAsync(
            SystemPrompt,
            $"User said: \"{message.Content}\"\n\nRespond with a sassy GLaDOS reply:",
            model: "deepseek-v4-flash-free",
            maxTokens: 150,
            temperature: 0.9,
            ct: CancellationToken.None);

        if (string.IsNullOrWhiteSpace(reply))
        {
            Console.WriteLine($"[Sassy] AI returned null/empty (LastError: {_ai.LastError})");
            return;
        }

        try
        {
            await message.Channel.SendMessageAsync(reply);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Sassy] Failed to send: {ex.Message}");
        }
    }
}
