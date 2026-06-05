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

        var boyeRole = guild.Roles.FirstOrDefault(r => r.Name.Equals("boye", StringComparison.OrdinalIgnoreCase));
        if (boyeRole == null)
        {
            _logger.LogWarning("Boye role not found, skipping hottie selection");
            context.SetTextColor(ConsoleTextColor.Red);
            context.WriteLine("Boye role not found!");
            context.ResetTextColor();
            return;
        }

        var members = guild.Users
            .Where(u => !u.IsBot && !u.IsWebhook && u.Roles.Any(r => r.Id == boyeRole.Id))
            .ToList();

        _logger.LogInformation("Guild has {Total} users, {Eligible} eligible (boye role)", guild.Users.Count, members.Count);
        context.WriteLine($"Guild: {guild.Name} ({guild.Users.Count} users, {members.Count} with boye role)");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var boyeUserIds = members.Select(m => m.Id).ToHashSet();

        // Remove records for users who no longer have the boye role
        var recordsToRemove = await dbContext.Set<HottieOfTheDay>()
            .Where(h => !boyeUserIds.Contains(h.DiscordUserId))
            .ToListAsync(cancellationToken);

        if (recordsToRemove.Count > 0)
        {
            _logger.LogInformation("Removing {Count} hottie record(s) for users without boye role", recordsToRemove.Count);
            context.WriteLine($"Removing {recordsToRemove.Count} record(s) for users without boye role");
            dbContext.Set<HottieOfTheDay>().RemoveRange(recordsToRemove);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Check if today already has a winner
        var todayRecord = await dbContext.Set<HottieOfTheDay>()
            .FirstOrDefaultAsync(h => h.DateAwarded == today, cancellationToken);

        if (todayRecord != null)
        {
            if (boyeUserIds.Contains(todayRecord.DiscordUserId))
            {
                _logger.LogInformation("Hottie already awarded today for {Id}", todayRecord.DiscordUserId);
                return;
            }

            _logger.LogInformation("Today's winner {Id} no longer has boye role, re-rolling", todayRecord.DiscordUserId);
            context.WriteLine($"Today's winner no longer has boye role, re-rolling");
            dbContext.Set<HottieOfTheDay>().Remove(todayRecord);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (members.Count == 0)
        {
            _logger.LogWarning("No eligible members with boye role found");
            context.WriteLine("No eligible members with boye role.");
            return;
        }

        // Determine available members who haven't won today
        var todaysWinnerIds = await dbContext.Set<HottieOfTheDay>()
            .Where(h => h.DateAwarded == today)
            .Select(h => h.DiscordUserId)
            .ToListAsync(cancellationToken);

        var availableMembers = members
            .Where(m => !todaysWinnerIds.Contains(m.Id))
            .ToList();

        if (availableMembers.Count == 0)
        {
            _logger.LogWarning("No available members to pick (all eligible users already won today)");
            context.WriteLine("No available members to pick.");
            return;
        }

        var winner = availableMembers[Random.Shared.Next(availableMembers.Count)];

        _logger.LogInformation("Hottie of the Day: {Username}#{Discriminator} ({Id})",
            winner.Username, winner.Discriminator, winner.Id);

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
