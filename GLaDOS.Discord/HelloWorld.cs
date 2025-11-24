using Discord;
using Discord.WebSocket;
using Glados.Discord.Contracts;
using Microsoft.Extensions.Hosting;

namespace Glados.Discord;

public class HelloWorld : IHelloWorld, IHostedService, IAsyncDisposable
{
    private readonly DiscordSocketClient _client;

    public HelloWorld(DiscordSocketClient client)
    {
        _client = client;
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
        await _client.LoginAsync(TokenType.Bot,
            "MTQzODYyOTE1MjUxMjAyMDU1MQ.Gvfv_o.RRve0E_J1-nIC7Be81xOcR7Fgfr3cBmHZmUAnc");

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