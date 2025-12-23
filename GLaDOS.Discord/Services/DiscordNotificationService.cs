using Discord;
using Discord.WebSocket;
using GLaDOS.Domain.OldschoolRunescape;
using Microsoft.Extensions.Configuration;

namespace Glados.Discord.Services;

public class DiscordNotificationService
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _notificationChannelId;

    public DiscordNotificationService(DiscordSocketClient client, IConfiguration configuration)
    {
        _client = client;
        _notificationChannelId = configuration.GetValue<ulong>("Discord:OldschoolRunescapeChannelId");
    }

    public async Task SendConsolidatedUpdatesAsync(
        List<(OldschoolRunescapeUser User, OldschoolRunescapeHiscoreChanges Changes)> allUpdates)
    {
        var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
        if (channel == null) return;

        var levelUpdates = allUpdates
            .Where(u => u.Changes.StatChanges.Any())
            .ToList();

        foreach (var update in levelUpdates)
        {
            var stats = update.Changes.StatChanges;

            var embed = new EmbedBuilder()
                .WithTitle($"{update.User.Username}'s level gains!")
                .WithDescription("The following skill(s) have increased")
                .WithThumbnailUrl("https://oldschool.runescape.wiki/images/Skills_icon.png")
                .WithColor(Color.DarkRed)
                .WithCurrentTimestamp();

            var overallStat = stats.FirstOrDefault(s => s.StatName == "Overall");

            var normalSkills = stats
                .Where(s => s.StatName != "Overall")
                .OrderBy(s => s.StatId)
                .ToList();

            foreach (var stat in normalSkills)
            {
                var diff = stat.NewLevel - stat.OldLevel;
                embed.AddField(stat.StatName, $"{stat.OldLevel} → **{stat.NewLevel}** (+{diff})", inline: false);
            }

            if (overallStat != null)
            {
                var diff = overallStat.NewLevel - overallStat.OldLevel;
                embed.WithFooter($"Overall: {overallStat.OldLevel} → {overallStat.NewLevel} (+{diff})");
            }

            await channel.SendMessageAsync(embed: embed.Build());
        }

        var activityGroups = allUpdates
            .SelectMany(u => u.Changes.ActivityChanges.Select(act => new { u.User, Activity = act }))
            .GroupBy(x => x.Activity.ActivityName)
            .ToList();

        if (activityGroups.Any())
        {
            foreach (var group in activityGroups)
            {
                var embed = new EmbedBuilder()
                    .WithTitle(group.Key)
                    .WithDescription("The following players have made progress!")
                    .WithThumbnailUrl("https://oldschool.runescape.wiki/images/HiScores_icon.png")
                    .WithColor(Color.Gold)
                    .WithCurrentTimestamp();

                var bossText = "";
                foreach (var entry in group)
                {
                    embed.AddField(entry.User.Username, $"{entry.Activity.NewScore} KC (+{entry.Activity.ScoreDifference}", inline: false);
                }

                embed.WithDescription(bossText);
                await channel.SendMessageAsync(embed: embed.Build());
            }
        }
    }
}