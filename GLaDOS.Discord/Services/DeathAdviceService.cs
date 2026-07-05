using Discord.WebSocket;
using Glados.Discord.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Glados.Discord.Services;

public class DeathAdviceService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly AIService _ai;
    private readonly int _chance;

    private static readonly string SystemPrompt =
        "You are a sarcastic gaming advisor. Someone just died in Old School RuneScape. " +
        "Give them a short, completely obvious piece of useless advice as if they're too stupid to play. " +
        "Vary your responses every time. Keep it under 2 sentences. No emojis. No roleplay. No apology. " +
        "Examples: 'Have you tried keeping your HP above 0?', " +
        "'Maybe don't stand in the fire next time.', " +
        "'Pro tip: eating food prevents death.', " +
        "'Did you consider just not dying?', " +
        "'The wilderness is dangerous, who knew?'";

    public DeathAdviceService(DiscordSocketClient client, AIService ai, IConfiguration configuration)
    {
        _client = client;
        _ai = ai;
        _chance = configuration.GetValue<int>("DeathAdvice:Chance", 3);
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

    private static readonly ulong DeathWebhookId = 1164299879904981012;

    private async Task OnMessageReceived(SocketMessage message)
    {
        if (message.Author.Id != DeathWebhookId)
            return;

        if (!message.Attachments.Any(a => a.ContentType?.StartsWith("image/") == true))
            return;

        Console.WriteLine($"[DeathAdvice] Webhook message with image detected: {message.Id} in channel {message.Channel.Id}");

        if (Random.Shared.Next(_chance) != 0)
        {
            Console.WriteLine($"[DeathAdvice] Skipped by random roll ({_chance - 1}/{_chance} chance)");
            return;
        }

        Console.WriteLine($"[DeathAdvice] Calling AI for reply...");
        var reply = await _ai.SendAsync(
            SystemPrompt,
            "Generate a short, sarcastic, useless piece of advice for someone who just died:",
            model: "deepseek-v4-flash-free",
            maxTokens: 150,
            temperature: 0.9,
            ct: CancellationToken.None);

        if (string.IsNullOrWhiteSpace(reply))
        {
            Console.WriteLine($"[DeathAdvice] AI returned null/empty (LastError: {_ai.LastError})");
            return;
        }

        Console.WriteLine($"[DeathAdvice] AI replied: \"{reply[..Math.Min(reply.Length, 100)]}\"");
        try
        {
            await message.Channel.SendMessageAsync(reply);
            Console.WriteLine($"[DeathAdvice] Reply sent successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DeathAdvice] Failed to send: {ex.Message}");
        }
    }
}
