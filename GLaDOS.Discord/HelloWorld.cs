using Discord;
using Discord.WebSocket;
using Glados.Discord.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Glados.Discord;

public class HelloWorld : IHelloWorld, IHostedService, IAsyncDisposable
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;

    public HelloWorld(DiscordSocketClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
        _client.Log += async (msg) =>
        {
            await Task.CompletedTask;
            Console.WriteLine(msg);
        };
    }

    public async Task SayHelloAsync(CancellationToken cancellationToken = default)
    {
        var guild = _client.GetGuild(867074325824012379);

        if (guild == null)
        {
            return;
        }

        var runescapeChannel = guild.TextChannels.FirstOrDefault(channel => channel.Name.Contains("runescape"));

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
}