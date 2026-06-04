using Discord.WebSocket;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.Scheduler.Application.Hangfire.Contracts;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Application.Discord;

[DisableConcurrentExecution(60)]
[AutomaticRetry(Attempts = 1)]
public class HottieOfTheDayJob : IHangfireJob
{
    private readonly ILogger<HottieOfTheDayJob> _logger;
    private readonly DiscordSocketClient _discord;
    private readonly IServiceScopeFactory _scopeFactory;

    private const ulong AnnounceChannelId = 867074325824012382;

    public HottieOfTheDayJob(
        ILogger<HottieOfTheDayJob> logger,
        DiscordSocketClient discord,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _discord = discord;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Hottie of the Day job");

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
            _logger.LogWarning("No guild available, skipping hottie selection");
            context.WriteLine("No guild found.");
            return;
        }

        _logger.LogInformation("Guild: {Name}, downloading members...", guild.Name);
        context.WriteLine($"Guild: {guild.Name}, downloading members...");
        await guild.DownloadUsersAsync();
        _logger.LogInformation("Members downloaded: {Count} total", guild.Users.Count);

        var members = guild.Users
            .Where(u => !u.IsBot && !u.IsWebhook)
            .ToList();

        _logger.LogInformation("Guild has {Total} users, {Eligible} eligible (non-bot)", guild.Users.Count, members.Count);
        context.WriteLine($"Guild: {guild.Name} ({guild.Users.Count} users, {members.Count} eligible)");

        if (members.Count == 0)
        {
            _logger.LogWarning("No eligible members found");
            context.WriteLine("No eligible members.");
            return;
        }

        var winner = members[Random.Shared.Next(members.Count)];
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _logger.LogInformation("Hottie of the Day: {Username}#{Discriminator} ({Id})",
            winner.Username, winner.Discriminator, winner.Id);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var alreadyAwarded = await dbContext.Set<HottieOfTheDay>()
            .AnyAsync(h => h.DiscordUserId == winner.Id && h.DateAwarded == today, cancellationToken);

        if (alreadyAwarded)
        {
            _logger.LogWarning("Hottie already awarded today for {Id}", winner.Id);
            return;
        }

        dbContext.Set<HottieOfTheDay>().Add(new HottieOfTheDay
        {
            DiscordUserId = winner.Id,
            DateAwarded = today
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var totalWins = await dbContext.Set<HottieOfTheDay>()
            .CountAsync(h => h.DiscordUserId == winner.Id, cancellationToken);

        var streak = await CalculateStreakAsync(dbContext, winner.Id, today, cancellationToken);

        var channel = guild.GetTextChannel(AnnounceChannelId);
        if (channel != null)
        {
            var streakText = streak > 1 ? $" (🔥 {streak} day streak!)" : "";
            var msg = $"<@{winner.Id}> is the **Hottie of the Day**!{streakText} That's {totalWins} time(s) total! :heart_eyes:";
            await channel.SendMessageAsync(msg);
            _logger.LogInformation("Announced hottie in channel {ChannelId}", AnnounceChannelId);
            context.WriteLine($"Announced in #{channel.Name}");
        }
        else
        {
            _logger.LogWarning("Announce channel {ChannelId} not found", AnnounceChannelId);
            context.SetTextColor(ConsoleTextColor.Red);
            context.WriteLine($"Channel {AnnounceChannelId} not found!");
            context.ResetTextColor();
        }

        context.WriteLine($"Hottie of the Day: {winner.Username}#{winner.Discriminator} ({totalWins} total wins, {streak} streak)");
    }

    private static async Task<int> CalculateStreakAsync(ApplicationDbContext dbContext, ulong userId, DateOnly today, CancellationToken ct)
    {
        var streak = 0;
        var date = today;

        while (true)
        {
            var wonOnDate = await dbContext.Set<HottieOfTheDay>()
                .AnyAsync(h => h.DiscordUserId == userId && h.DateAwarded == date, ct);

            if (!wonOnDate) break;

            streak++;
            date = date.AddDays(-1);
        }

        return streak;
    }
}
