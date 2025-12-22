namespace Glados.Discord.Contracts;

public interface IDiscordClient
{ 
    Task Speak(string message, ulong? channelId, bool tts, CancellationToken cancellationToken = default);
}