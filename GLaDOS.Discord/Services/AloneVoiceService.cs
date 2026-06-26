using Discord.Audio;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace Glados.Discord.Services;

public class AloneVoiceService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private IAudioClient? _audioClient;
    private ulong? _currentChannelId;
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
                await EvaluateVoiceChannels();
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

    private Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        // Run off the gateway thread. Discord.Net invokes event callbacks on the gateway task,
        // and JoinVoice -> ConnectAsync needs subsequent gateway events (the voice-server
        // handshake) to complete; awaiting it here would block the gateway and deadlock the join.
        _ = Task.Run(() => HandleVoiceStateAsync(user, oldState, newState));
        return Task.CompletedTask;
    }

    private async Task HandleVoiceStateAsync(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
    {
        Console.WriteLine($"[AloneVoice] Voice state event: user={user.Username}, isBot={user.IsBot}, oldChannel={oldState.VoiceChannel?.Name}, newChannel={newState.VoiceChannel?.Name}");

        if (user.IsBot || !_startupComplete)
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

    private SocketVoiceChannel? GetBotChannel()
    {
        if (_currentChannelId == null)
            return null;

        foreach (var guild in _client.Guilds)
        {
            var vc = guild.GetVoiceChannel(_currentChannelId.Value);
            if (vc != null)
                return vc;
        }
        return null;
    }

    /// <summary>
    /// Recomputes the desired state from scratch on every voice event instead of reacting to
    /// the old/new channel delta. The bot belongs in a voice channel if and only if that channel
    /// has exactly one human: stay put if already correct, otherwise leave and/or join. This
    /// avoids spurious leaves when a user changes mute/deafen state (which fires an event whose
    /// old channel equals the bot's channel even though nobody actually left).
    /// </summary>
    private async Task EvaluateVoiceChannels()
    {
        var botChannel = GetBotChannel();

        // If we're already sitting with exactly one human, nothing to do.
        if (botChannel != null && botChannel.Users.Count(u => !u.IsBot) == 1)
            return;

        // Find a channel that has exactly one human — that's where the bot belongs.
        SocketVoiceChannel? target = null;
        foreach (var guild in _client.Guilds)
        {
            foreach (var vc in guild.VoiceChannels)
            {
                if (vc.Users.Count(u => !u.IsBot) == 1)
                {
                    target = vc;
                    break;
                }
            }
            if (target != null)
                break;
        }

        // We're connected somewhere that no longer has exactly one human — leave.
        if (botChannel != null && botChannel.Id != target?.Id)
        {
            Console.WriteLine($"[AloneVoice] Leaving {botChannel.Name} (no longer exactly one human present)");
            await LeaveVoice();
        }

        // There's a channel with a lone human and we're not already there — join it.
        if (target != null && target.Id != _currentChannelId)
        {
            var human = target.Users.First(u => !u.IsBot);
            Console.WriteLine($"[AloneVoice] {human.Username} is alone in {target.Name}, joining");
            await JoinVoice(target);
        }
    }

    private async Task JoinVoice(SocketVoiceChannel channel)
    {
        try
        {
            Console.WriteLine($"[AloneVoice] Calling ConnectAsync on {channel.Name}...");
            _audioClient = await channel.ConnectAsync();
            _currentChannelId = channel.Id;
            Console.WriteLine($"[AloneVoice] Successfully connected to {channel.Name}");
        }
        catch (Exception ex)
        {
            _currentChannelId = null;
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
        _currentChannelId = null;
    }
}
