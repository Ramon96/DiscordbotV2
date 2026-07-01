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
                // Best-effort scan at startup: if someone is already alone in a channel, join them.
                await ScanAndJoinIfAloneAsync();
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
            await EvaluateVoiceChannels(oldState.VoiceChannel, newState.VoiceChannel);
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

    /// <summary>
    /// Reacts to a single voice-state change using the channel objects from the event (not a
    /// guild-cache scan, which proved unreliable). The bot should be present only while a channel
    /// has exactly one human.
    /// </summary>
    private async Task EvaluateVoiceChannels(SocketVoiceChannel? leftChannel, SocketVoiceChannel? joinedChannel)
    {
        var botChannelId = _currentChannelId;

        // A user actually LEFT the bot's channel (moved elsewhere or disconnected). This must be a
        // real move — leftChannel == bot's channel AND they did not stay in it. A mute/deafen event
        // reports old == new == the same channel, so it is intentionally NOT treated as a leave.
        if (botChannelId != null && leftChannel?.Id == botChannelId && joinedChannel?.Id != botChannelId)
        {
            var humansRemaining = leftChannel!.Users.Count(u => !u.IsBot);
            if (humansRemaining != 1)
            {
                // 0 humans -> empty; 2+ -> not "alone". Exactly 1 means the last person is now alone,
                // so the bot stays.
                Console.WriteLine($"[AloneVoice] {humansRemaining} human(s) left in {leftChannel.Name}, leaving");
                await LeaveVoice();
                botChannelId = null;
            }
        }

        // Someone joined (or is present in) a channel.
        if (joinedChannel != null)
        {
            var humans = joinedChannel.Users.Count(u => !u.IsBot);

            if (humans == 1 && botChannelId != joinedChannel.Id)
            {
                if (botChannelId != null)
                    await LeaveVoice();

                Console.WriteLine($"[AloneVoice] Someone is alone in {joinedChannel.Name}, joining");
                await JoinVoice(joinedChannel);
            }
            else if (humans >= 2 && botChannelId == joinedChannel.Id)
            {
                Console.WriteLine($"[AloneVoice] {humans} humans in {joinedChannel.Name}, leaving");
                await LeaveVoice();
            }
        }
    }

    private async Task ScanAndJoinIfAloneAsync()
    {
        if (_currentChannelId != null)
            return;

        foreach (var guild in _client.Guilds)
        {
            foreach (var vc in guild.VoiceChannels)
            {
                if (vc.Users.Count(u => !u.IsBot) == 1)
                {
                    Console.WriteLine($"[AloneVoice] Someone is alone in {vc.Name} at startup, joining");
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
