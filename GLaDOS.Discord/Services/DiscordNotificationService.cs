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


        var skillGroups = allUpdates
            .SelectMany(user => user.Changes.StatChanges.Select(stat => new { user.User, Stat = stat }))
            .GroupBy(userSkills => userSkills.Stat.StatName)
            .ToList();

        if (skillGroups.Any())
        {
            foreach (var group in skillGroups)
            {
                var embed = new EmbedBuilder()
                    .WithTitle(group.Key)
                    .WithDescription("The following players have made progress!")
                    .WithThumbnailUrl("https://oldschool.runescape.wiki/images/Skills_icon.png")
                    .WithColor(Color.DarkRed)
                    .WithCurrentTimestamp();

                string skillText = "";
                foreach (var userSkills in group)
                {
                    var diff = userSkills.Stat.NewLevel - userSkills.Stat.OldLevel;
                    skillText +=
                        $"**{userSkills.User.Username}**: {userSkills.Stat.OldLevel} → **{userSkills.Stat.NewLevel}** (+{diff})\n";
                }

                embed.AddField(group.Key, skillText, inline: false);
                await channel.SendMessageAsync(embed: embed.Build());
            }
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
                    // bossText += $"{flavor.Icon} **{entry.User.Username}**: {entry.Activity.NewScore} KC (+{entry.Activity.ScoreDifference})\n";
                    bossText +=
                        $"**{entry.User.Username}**: {entry.Activity.NewScore} KC (+{entry.Activity.ScoreDifference})\n";
                }

                embed.WithDescription(bossText);
                await channel.SendMessageAsync(embed: embed.Build());
            }
        }
    }
}