using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace Glados.Discord.Services;

public class AloneVoiceService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private IAudioClient? _audioClient;
    private readonly SemaphoreSlim _evalLock = new(1, 1);
    private bool _startupComplete;

    public AloneVoiceService(DiscordSocketClient client)
    {
        _client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("[AloneVoice] Service starting, subscribing to voice state events...");
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
        _ = DelayedStartupAsync();
        return Task.CompletedTask;
    }

    private async Task DelayedStartupAsync()
    {
        await Task.Delay(5000);

        if (await _evalLock.WaitAsync(0))
        {
            try
            {
                var botChannel = FindBotChannel();

                if (botChannel == null)
                {
                    foreach (var guild in _client.Guilds)
                    foreach (var vc in guild.VoiceChannels)
                    {
                        var humans = vc.Users.Where(u => !u.IsBot).ToList();
                        if (humans.Count == 1)
                        {
                            Console.WriteLine($"[AloneVoice] Found {humans[0].Username} alone in {vc.Name} after startup, joining");
                            await JoinVoice(vc);
                            break;
                        }
                    }
                }
            }
            finally
            {
                _evalLock.Release();
            }
        }

        _startupComplete = true;
        Console.WriteLine("[AloneVoice] Startup complete, now processing live events");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _client.UserVoiceStateUpdated -= OnUserVoiceStateUpdated;
        return Task.CompletedTask;
    }

    private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        Console.WriteLine($"[AloneVoice] Voice state event: user={user.Username}, isBot={user.IsBot}, oldChannel={oldState.VoiceChannel?.Name}, newChannel={newState.VoiceChannel?.Name}");

        if (user.IsBot || !_startupComplete)
            return;

        await Task.Delay(1500);

        if (!await _evalLock.WaitAsync(0))
            return;

        try
        {
            await EvaluateVoiceChannels(user, oldState.VoiceChannel, newState.VoiceChannel);
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

    private SocketVoiceChannel? FindBotChannel()
    {
        if (_audioClient == null)
            return null;

        foreach (var guild in _client.Guilds)
        foreach (var vc in guild.VoiceChannels)
            if (vc.Users.Any(u => u.Id == _client.CurrentUser.Id))
                return vc;
        return null;
    }

    private async Task EvaluateVoiceChannels(SocketUser user, SocketVoiceChannel? leftChannel, SocketVoiceChannel? joinedChannel)
    {
        var botChannel = FindBotChannel();

        // User left a channel where the bot is — check if bot is now alone
        if (leftChannel != null && botChannel?.Id == leftChannel.Id)
        {
            var humanCount = leftChannel.Users.Count(u => !u.IsBot);
            if (humanCount <= 1)
            {
                Console.WriteLine($"[AloneVoice] User left {leftChannel.Name}, leaving");
                await LeaveVoice();
                botChannel = null;
            }
        }

        // User joined a channel
        if (joinedChannel != null)
        {
            var humanUsers = joinedChannel.Users.Where(u => !u.IsBot).ToList();

            if (humanUsers.Count == 1)
            {
                if (botChannel == null || botChannel.Id != joinedChannel.Id)
                {
                    if (botChannel != null)
                        await LeaveVoice();

                    Console.WriteLine($"[AloneVoice] {humanUsers[0].Username} is alone in {joinedChannel.Name}, joining");
                    await JoinVoice(joinedChannel);
                }
            }
            else if (humanUsers.Count >= 2 && botChannel?.Id == joinedChannel.Id)
            {
                Console.WriteLine($"[AloneVoice] {humanUsers.Count} users in {botChannel.Name}, leaving");
                await LeaveVoice();
            }
        }
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
