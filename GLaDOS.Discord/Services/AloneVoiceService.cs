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
        Console.WriteLine("[AloneVoice] Service starting, subscribing to voice state events...");
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
        Console.WriteLine($"[AloneVoice] Voice state event: user={user.Username}, isBot={user.IsBot}, oldChannel={oldState.VoiceChannel?.Name}, newChannel={newState.VoiceChannel?.Name}");

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
        Console.WriteLine("[AloneVoice] EvaluateVoiceChannels called");

        SocketVoiceChannel? botChannel = null;

        foreach (var guild in _client.Guilds)
        {
            Console.WriteLine($"[AloneVoice]   Checking guild: {guild.Name} ({guild.Id}), voice channels: {guild.VoiceChannels.Count}");

            foreach (var vc in guild.VoiceChannels)
            {
                var users = vc.Users.ToList();
                Console.WriteLine($"[AloneVoice]     Channel: {vc.Name} ({vc.Id}), users: {users.Count} — {string.Join(", ", users.Select(u => u.Username))}");

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
            Console.WriteLine($"[AloneVoice] Bot is in {botChannel.Name}, human users: {humanCount}");

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

        Console.WriteLine("[AloneVoice] Bot is not in any voice channel, searching for alone users...");

        foreach (var guild in _client.Guilds)
        {
            foreach (var vc in guild.VoiceChannels)
            {
                var humanUsers = vc.Users.Where(u => !u.IsBot).ToList();
                Console.WriteLine($"[AloneVoice]   {guild.Name}/{vc.Name}: {humanUsers.Count} human users");
                if (humanUsers.Count == 1)
                {
                    Console.WriteLine($"[AloneVoice] {humanUsers[0].Username} is alone in {vc.Name}, joining");
                    await JoinVoice(vc);
                    return;
                }
            }
        }

        Console.WriteLine("[AloneVoice] No alone users found");
    }

    private async Task JoinVoice(SocketVoiceChannel channel)
    {
        try
        {
            Console.WriteLine($"[AloneVoice] Calling ConnectAsync on {channel.Name}...");
            _audioClient = await channel.ConnectAsync();
            Console.WriteLine($"[AloneVoice] Successfully connected to {channel.Name}");
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
