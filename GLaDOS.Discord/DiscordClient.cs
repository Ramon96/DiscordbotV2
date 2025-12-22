using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using IDiscordClient = Glados.Discord.Contracts.IDiscordClient;

namespace Glados.Discord;

public class DiscordClient : IDiscordClient, IHostedService, IAsyncDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;

    public DiscordClient(DiscordSocketClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
        _client.Log += async (msg) =>
        {
            await Task.CompletedTask;
            Console.WriteLine(msg);
        };
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var token = _configuration["Discord:Token"];
        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException("Discord token not found in configuration");
        }

        await _client.LoginAsync(TokenType.Bot, token);

        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _client.StopAsync();
        await _client.LogoutAsync();
    }

    public Task Speak(string message, ulong? channelId, bool tts, CancellationToken cancellationToken = default)
    {
        var channel = channelId ?? 867074325824012382;
        var discordChannel = _client.GetChannel(channel) as IMessageChannel;
        
        if (discordChannel == null)
        {
            throw new InvalidOperationException($"Channel with ID {channel} not found.");
        }
        
        return discordChannel.SendMessageAsync(message, tts);
    }
}