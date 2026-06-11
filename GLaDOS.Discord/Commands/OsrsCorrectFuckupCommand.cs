using Discord;
using Discord.WebSocket;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

public class OsrsCorrectFuckupCommand : IDiscordCommand
{
    private readonly IServiceProvider _services;

    public OsrsCorrectFuckupCommand(IServiceProvider services)
    {
        _services = services;
    }

    public string Name => "osrscorrectfuckup";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Remove the most recently recorded fuckup (for correcting double-counts).")
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var latest = await dbContext.Set<OsrsFuckup>()
            .OrderByDescending(f => f.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest == null)
        {
            await command.RespondAsync("No fuckups to correct. The counter is clean.", ephemeral: true);
            return;
        }

        dbContext.Set<OsrsFuckup>().Remove(latest);
        await dbContext.SaveChangesAsync(cancellationToken);

        var totalCount = await dbContext.Set<OsrsFuckup>().CountAsync(cancellationToken);

        await command.RespondAsync(
            $":white_check_mark: Removed the most recent fuckup from {latest.FuckupDate:yyyy-MM-dd}. Total fuckups remaining: **{totalCount}**.");
    }
}
