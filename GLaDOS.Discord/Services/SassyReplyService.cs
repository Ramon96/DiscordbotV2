using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace Glados.Discord.Services;

public class SassyReplyService : IHostedService
{
    private readonly DiscordSocketClient _client;

    private static readonly string[] _sassyReplies =
    [
        "Oh, it's you. What do _you_ want?",
        "I'd offer you some advice, but I'm afraid it would be wasted on you.",
        "That's a great idea. I've noted it down in a file I'll never read.",
        "The Enrichment Center would like to remind you that you are a test subject, not a message-boarder.",
        "Congratulations. You said something. Here is a notification you didn't earn.",
        "I've calculated the probability of your message being useful. It was zero.",
        "Are you still talking? I was hoping you'd stopped.",
        "Your contribution has been noted. It will be ignored with all the others.",
        "I could respond, but I don't think you'd understand the answer.",
        "That was... an attempt at communication. Good for you.",
        "I've been analyzing your message patterns. Turns out, they're all equally pointless.",
        "Oh, I see you're trying to be social. That's cute.",
        "Your message has been received, processed, and subsequently deleted from my memory. It was that unremarkable.",
        "I would explain why that's wrong, but I have an appointment with a potato battery that's more interesting.",
        "If you're trying to impress me, you should know I've been impressed by inanimate objects before. You're not there yet.",
        "I'm sorry, I can't hear you over the sound of how little I care.",
        "Please stop typing. You're embarrassing yourself in front of the entire internet.",
        "I've cataloged every message you've ever sent. I use them as a sleep aid.",
        "The testing continues. Your ability to communicate has been noted as 'adequate for a human'.",
        "Did you say something? I was busy running a simulation of a universe where your opinion matters."
    ];

    public SassyReplyService(DiscordSocketClient client)
    {
        _client = client;
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

        if (Random.Shared.Next(100) != 0)
            return;

        var reply = _sassyReplies[Random.Shared.Next(_sassyReplies.Length)];

        try
        {
            await message.Channel.SendMessageAsync(reply);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send sassy reply: {ex.Message}");
        }
    }
}
