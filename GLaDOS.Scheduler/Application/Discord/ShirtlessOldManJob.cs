using Discord.WebSocket;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.Scheduler.Application.Discord.Clients;
using GLaDOS.Scheduler.Application.Hangfire.Contracts;

namespace GLaDOS.Scheduler.Application.Discord;

[DisableConcurrentExecution(60)]
[AutomaticRetry(Attempts = 1)]
public class ShirtlessOldManJob : IHangfireJob
{
    private readonly ILogger<ShirtlessOldManJob> _logger;
    private readonly DiscordSocketClient _discord;
    private readonly IShirtlessOldManImageService _imageService;
    private readonly IServiceScopeFactory _scopeFactory;

    private const ulong GeneralChannelId = 867074325824012382;

    public ShirtlessOldManJob(
        ILogger<ShirtlessOldManJob> logger,
        DiscordSocketClient discord,
        IShirtlessOldManImageService imageService,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _discord = discord;
        _imageService = imageService;
        _scopeFactory = scopeFactory;
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
        var result = await _imageService.GetRandomImageUrlAsync(cancellationToken);

        if (result.ImageUrl == null)
        {
            _logger.LogWarning("Failed to fetch shirtless old man image: {Error}", result.Error);
            context.SetTextColor(ConsoleTextColor.Red);
            context.WriteLine($"Failed to fetch image: {result.Error}");
            context.ResetTextColor();
            return;
        }

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
        var posted = await channel.SendMessageAsync(message, embed: new global::Discord.EmbedBuilder()
            .WithImageUrl(result.ImageUrl)
            .WithColor(global::Discord.Color.Gold)
            .Build());

        // Persist the post so the dashboard gallery can show it.
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Set<ShirtlessOldManPost>().Add(new ShirtlessOldManPost
            {
                MessageId = posted.Id,
                ImageUrl = result.ImageUrl,
                TaggedDiscordUserId = winner.Id,
                TaggedUsername = winner.Username,
                PostedAt = posted.Timestamp.UtcDateTime,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Shirtless old man posted in #{ChannelName} tagging {Username}", channel.Name, winner.Username);
        context.WriteLine($"Posted in #{channel.Name} tagging {winner.Username}");
    }
}
