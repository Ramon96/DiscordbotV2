using Discord;
using Discord.WebSocket;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

public class OsrsFuckupCommand : IDiscordCommand
{
    private readonly IServiceProvider _services;

    public OsrsFuckupCommand(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "osrsfuckup";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Record that OSRS made a fuckup. +1 to the counter.")
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        dbContext.Set<OsrsFuckup>().Add(new OsrsFuckup
        {
            FuckupDate = today,
            DiscordUserId = command.User.Id,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        var totalCount = await dbContext.Set<OsrsFuckup>().CountAsync(cancellationToken);
        var streak = await CalculateStreak(dbContext, cancellationToken);

        await command.RespondAsync(
            $":middle_finger: Fuckup recorded (date: {today:yyyy-MM-dd}). Total fuckups: **{totalCount}**. Current streak: **{streak}** day{(streak == 1 ? "" : "s")}.");
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
