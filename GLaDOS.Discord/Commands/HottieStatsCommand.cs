using System.Text;
using Discord;
using Discord.WebSocket;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

public class HottieStatsCommand : IDiscordCommand
{
    private readonly IServiceProvider _services;

    public HottieStatsCommand(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "hottie-stats";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Show the Hottie of the Day leaderboard and stats")
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        await command.DeferAsync();

        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var now = DateOnly.FromDateTime(DateTime.UtcNow);

        var leaderboard = await dbContext.Set<HottieOfTheDay>()
            .GroupBy(h => h.DiscordUserId)
            .Select(g => new
            {
                DiscordUserId = g.Key,
                Count = g.Count(),
                LastWin = g.Max(h => h.DateAwarded)
            })
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.LastWin)
            .Take(10)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine(":crown: **Hottie of the Day Leaderboard** :crown:\n");

        var medals = new[] { ":first_place:", ":second_place:", ":third_place:", "4.", "5.", "6.", "7.", "8.", "9.", "10." };

        for (var i = 0; i < leaderboard.Count; i++)
        {
            var entry = leaderboard[i];
            var isActive = (now.DayNumber - entry.LastWin.DayNumber) <= 1;
            var activeMarker = isActive ? " :fire:" : "";
            sb.AppendLine($"{medals[i]} <@{entry.DiscordUserId}> — **{entry.Count}** win(s){activeMarker}");
        }

        if (leaderboard.Count == 0)
        {
            sb.AppendLine("No hotties yet! The first Hottie of the Day will be crowned soon.");
        }

        var currentStreaks = new List<(ulong UserId, int Streak)>();
        foreach (var entry in leaderboard)
        {
            var streak = await CalculateStreakAsync(dbContext, entry.DiscordUserId, now, cancellationToken);
            if (streak > 1)
                currentStreaks.Add((entry.DiscordUserId, streak));
        }

        if (currentStreaks.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(":fire: **Active Streaks**");
            foreach (var (userId, streak) in currentStreaks.OrderByDescending(s => s.Streak).Take(3))
            {
                sb.AppendLine($"<@{userId}> — {streak} consecutive days!");
            }
        }

        await command.ModifyOriginalResponseAsync(props => props.Content = sb.ToString());
    }

    private static async Task<int> CalculateStreakAsync(ApplicationDbContext dbContext, ulong userId, DateOnly today, CancellationToken ct)
    {
        var streak = 0;
        var date = today;

        while (true)
        {
            var won = await dbContext.Set<HottieOfTheDay>()
                .AnyAsync(h => h.DiscordUserId == userId && h.DateAwarded == date, ct);

            if (!won) break;

            streak++;
            date = date.AddDays(-1);
        }

        return streak;
    }
}
