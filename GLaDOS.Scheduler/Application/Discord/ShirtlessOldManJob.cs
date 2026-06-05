using Discord.WebSocket;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using GLaDOS.Scheduler.Application.Hangfire.Contracts;

namespace GLaDOS.Scheduler.Application.Discord;

[DisableConcurrentExecution(60)]
[AutomaticRetry(Attempts = 1)]
public class ShirtlessOldManJob : IHangfireJob
{
    private readonly ILogger<ShirtlessOldManJob> _logger;
    private readonly DiscordSocketClient _discord;
    private readonly IConfiguration _configuration;

    private const ulong GeneralChannelId = 867074325824012382;

    private static readonly string[] ImageUrls =
    [
        "https://i.imgur.com/2Cq0Wxk.jpeg",
        "https://i.imgur.com/L1fRt0C.jpeg",
        "https://i.imgur.com/g5mFZq5.jpeg",
        "https://i.imgur.com/3X0H0nH.jpeg",
    ];

    public ShirtlessOldManJob(
        ILogger<ShirtlessOldManJob> logger,
        DiscordSocketClient discord,
        IConfiguration configuration)
    {
        _logger = logger;
        _discord = discord;
        _configuration = configuration;
    }

    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Shirtless Old Man job");

        if (_discord.ConnectionState != global::Discord.ConnectionState.Connected)
        {
            _logger.LogWarning("Discord client not connected (state: {State}), skipping", _discord.ConnectionState);
            context.SetTextColor(ConsoleTextColor.Red);
            context.WriteLine($"Discord not connected (state: {_discord.ConnectionState})");
            context.ResetTextColor();
            return;
        }

        var guild = _discord.Guilds.FirstOrDefault();
        if (guild == null)
        {
            _logger.LogWarning("No guild available, skipping");
            context.WriteLine("No guild found.");
            return;
        }

        _logger.LogInformation("Guild: {Name}, downloading members...", guild.Name);
        context.WriteLine($"Guild: {guild.Name}, downloading members...");
        await guild.DownloadUsersAsync();
        _logger.LogInformation("Members downloaded: {Count} total", guild.Users.Count);

        var eligibleMembers = guild.Users
            .Where(u => !u.IsBot && !u.IsWebhook)
            .ToList();

        if (eligibleMembers.Count == 0)
        {
            _logger.LogWarning("No eligible members found");
            context.WriteLine("No eligible members found.");
            return;
        }

        var winner = eligibleMembers[Random.Shared.Next(eligibleMembers.Count)];
        var imageUrl = ImageUrls[Random.Shared.Next(ImageUrls.Length)];

        _logger.LogInformation("Selected member: {Username} ({Id})", winner.Username, winner.Id);

        var channel = guild.GetTextChannel(GeneralChannelId);
        if (channel == null)
        {
            _logger.LogWarning("General channel {ChannelId} not found", GeneralChannelId);
            context.SetTextColor(ConsoleTextColor.Red);
            context.WriteLine($"Channel {GeneralChannelId} not found!");
            context.ResetTextColor();
            return;
        }

        var message = $"<@{winner.Id}> check dit! :older_man:";
        await channel.SendMessageAsync(message, embed: new global::Discord.EmbedBuilder()
            .WithImageUrl(imageUrl)
            .WithColor(global::Discord.Color.Gold)
            .Build());

        _logger.LogInformation("Shirtless old man posted in #{ChannelName} tagging {Username}", channel.Name, winner.Username);
        context.WriteLine($"Posted in #{channel.Name} tagging {winner.Username}");
    }
}
