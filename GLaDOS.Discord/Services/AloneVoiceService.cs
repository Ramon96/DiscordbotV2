using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace Glados.Discord.Services;

public class AloneVoiceService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private IAudioClient? _audioClient;
    private readonly SemaphoreSlim _evalLock = new(1, 1);

    public AloneVoiceService(DiscordSocketClient client)
    {
        _client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.UserVoiceStateUpdated -= OnUserVoiceStateUpdated;
        return Task.CompletedTask;
    }

    private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        if (user.IsBot)
            return;

        await Task.Delay(1500);

        if (!await _evalLock.WaitAsync(0))
            return;

        try
        {
            await EvaluateVoiceChannels();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AloneVoice] Error: {ex.Message}");
        }
        finally
        {
            _evalLock.Release();
        }
    }

    private async Task EvaluateVoiceChannels()
    {
        SocketVoiceChannel? botChannel = null;

        foreach (var guild in _client.Guilds)
        {
            foreach (var vc in guild.VoiceChannels)
            {
                if (vc.Users.Any(u => u.Id == _client.CurrentUser.Id))
                {
                    botChannel = vc;
                    break;
                }
            }
            if (botChannel != null) break;
        }

        if (botChannel != null)
        {
            var humanCount = botChannel.Users.Count(u => !u.IsBot);

            if (humanCount == 0)
            {
                Console.WriteLine($"[AloneVoice] No one left in {botChannel.Name}, leaving");
                await LeaveVoice();
                return;
            }

            if (humanCount >= 2)
            {
                Console.WriteLine($"[AloneVoice] {humanCount} users in {botChannel.Name}, leaving");
                await LeaveVoice();
                return;
            }

            return;
        }

        foreach (var guild in _client.Guilds)
        {
            foreach (var vc in guild.VoiceChannels)
            {
                var humanUsers = vc.Users.Where(u => !u.IsBot).ToList();
                if (humanUsers.Count == 1)
                {
                    Console.WriteLine($"[AloneVoice] {humanUsers[0].Username} is alone in {vc.Name}, joining");
                    await JoinVoice(vc);
                    return;
                }
            }
        }
    }

    private async Task JoinVoice(SocketVoiceChannel channel)
    {
        try
        {
            _audioClient = await channel.ConnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AloneVoice] Failed to join {channel.Name}: {ex.Message}");
        }
    }

    private async Task LeaveVoice()
    {
        if (_audioClient != null)
        {
            try
            {
                await _audioClient.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AloneVoice] Error leaving voice: {ex.Message}");
            }
            _audioClient = null;
        }
    }
}
