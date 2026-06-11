using System.Text;
using Discord;
using Discord.WebSocket;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

public class OsrsFuckupsCommand : IDiscordCommand
{
    private readonly IServiceProvider _services;

    public OsrsFuckupsCommand(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "osrsfuckups";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("View all recorded OSRS fuckups.")
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var totalCount = await dbContext.Set<OsrsFuckup>().CountAsync(cancellationToken);
        var fuckups = await dbContext.Set<OsrsFuckup>()
            .OrderByDescending(f => f.FuckupDate)
            .ThenByDescending(f => f.CreatedDate)
            .ToListAsync(cancellationToken);

        var grouped = fuckups
            .GroupBy(f => f.FuckupDate)
            .OrderByDescending(g => g.Key)
            .ToList();

        var streak = await CalculateStreak(dbContext, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"Total fuckups: **{totalCount}**");
        sb.AppendLine($"Current streak: **{streak}** day{(streak == 1 ? "" : "s")}");
        sb.AppendLine();

        foreach (var group in grouped.Take(20))
        {
            var count = group.Count();
            var users = string.Join(", ", group.Select(f => $"<@{f.DiscordUserId}>"));
            sb.AppendLine($"`{group.Key:yyyy-MM-dd}` — **{count}** fuckup{(count == 1 ? "" : "s")} by {users}");
        }

        if (grouped.Count > 20)
        {
            sb.AppendLine($"... and {totalCount - grouped.Take(20).Sum(g => g.Count())} more.");
        }

        var embed = new EmbedBuilder()
            .WithTitle("OSRS Fuckup Tracker")
            .WithColor(Color.Red)
            .WithDescription(sb.ToString())
            .WithCurrentTimestamp()
            .Build();

        await command.RespondAsync(embed: embed);
    }

    private static async Task<int> CalculateStreak(ApplicationDbContext dbContext, CancellationToken ct)
    {
        var fuckupDates = await dbContext.Set<OsrsFuckup>()
            .Select(f => f.FuckupDate)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync(ct);

        if (fuckupDates.Count == 0)
            return 0;

        var streak = 0;
        var expected = fuckupDates[0];

        foreach (var date in fuckupDates)
        {
            if (date == expected)
            {
                streak++;
                expected = expected.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }
}
