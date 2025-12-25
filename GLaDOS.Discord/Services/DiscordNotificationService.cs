using Discord;
using Discord.WebSocket;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Domain.OsrsWiki;
using GLaDOS.OsrsWiki.Clients.Contracts;
using GLaDOS.OsrsWiki.Responses;
using Microsoft.Extensions.Configuration;

namespace Glados.Discord.Services;

public class DiscordNotificationService
{
    private readonly DiscordSocketClient _client;
    private readonly IOsrsWikiItemClient _wikiItemClient;
    private readonly ulong _notificationChannelId;

    public DiscordNotificationService(DiscordSocketClient client, IConfiguration configuration, IOsrsWikiItemClient wikiItemClient)
    {
        _client = client;
        _wikiItemClient = wikiItemClient;
        _notificationChannelId = configuration.GetValue<ulong>("Discord:OldschoolRunescapeChannelId");
    }

    public async Task SendConsolidatedUpdatesAsync(
        List<(OldschoolRunescapeUser User, OldschoolRunescapeHiscoreChanges Changes)> allUpdates)
    {
        var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
        if (channel == null) return;

        var levelUpdates = allUpdates
            .Where(user => user.Changes.StatChanges.Any())
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
                .Where(stat => stat.StatName != "Overall")
                .OrderBy(stat => stat.StatId)
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
            .SelectMany(user => user.Changes.ActivityChanges.Select(act => new { user.User, Activity = act }))
            .GroupBy(user => user.Activity.ActivityName)
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
                    embed.AddField(entry.User.Username, $"{entry.Activity.NewScore} KC (+{entry.Activity.ScoreDifference})", inline: false);
                }

                embed.WithDescription(bossText);
                await channel.SendMessageAsync(embed: embed.Build());
            }
        }
    }
    
    public async Task SendWikiUpdatesAsync(List<(OldschoolRunescapeUser User, OsrsWikiSyncChanges Changes)> updates)
    {
        var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
        if (channel == null) return;

        foreach (var (user, changes) in updates)
        {
            if (changes.CompletedQuests.Any())
            {
                foreach (var quest in changes.CompletedQuests)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle($"{user.Username} has a quest completion!")
                        .AddField(quest, "Completed!", inline: false)
                        .WithColor(Color.Blue)
                        .WithThumbnailUrl("https://oldschool.runescape.wiki/images/Quest_point_icon.png?dc356")
                        .WithFooter("More")
                        .WithCurrentTimestamp()
                        .Build();
                    
                    await channel.SendMessageAsync(embed: embed);
                }
            }
            
            if (changes.CompletedDiaries.Any())
            {
                foreach (var diary in changes.CompletedDiaries)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle($"{user.Username} has completed the achievement diary")
                        .AddField($"{diary.Region} Diary", $"{diary.Tier} Tier Completed!", inline: false)
                        .WithColor(Color.Green)
                        .WithThumbnailUrl("https://oldschool.runescape.wiki/images/Achievement_Diaries_icon.png?b4e0c")
                        .WithFooter("Let's go dude!")
                        .WithCurrentTimestamp()
                        .Build();
                    
                    await channel.SendMessageAsync(embed: embed);
                }
            }

            if (changes.UnlockedTracks.Any())
            {
                foreach (var song in changes.UnlockedTracks)
                {
                    var embed = new EmbedBuilder()
                        .WithTitle($"{user.Username} has unlocked a new music track!")
                        .AddField(song, "Unlocked!", inline: false)
                        .WithColor(Color.Purple)
                        .WithThumbnailUrl("https://oldschool.runescape.wiki/images/archive/20150606083743%21Music.png?2a5be")
                        .WithFooter("Rock on!")
                        .WithCurrentTimestamp()
                        .Build();
                    
                    await channel.SendMessageAsync(embed: embed);
                }
            }
            
            if (changes.NewCollectionLogItems.Any())
            {
                foreach (var collectionLog in changes.NewCollectionLogItems)
                {
                    var itemDetails = await _wikiItemClient.GetItemDetailsAsync(collectionLog, CancellationToken.None);
                    if (itemDetails == null) continue;

                    var itemImageUrl = _wikiItemClient.GetImageUrl(itemDetails.Images.LastOrDefault() ??
                                                                   "https://oldschool.runescape.wiki/w/Collection_log#/media/File:Collection_log.png"); 
                    
                    var embed = BuildCollectionLogEmbed(user, itemDetails, itemImageUrl);
                    await channel.SendMessageAsync(embed: embed);
                }
            }
        }
    }
    

    private Embed BuildCollectionLogEmbed(OldschoolRunescapeUser user, OsrsWikiItemInfo item, string itemImageUrl)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{user.Username} has obtained a collection log item!")
            .WithColor(196, 67, 45)
            .WithThumbnailUrl(itemImageUrl)
            .AddField(item.Name, item.Examine, inline: false)
            .WithFooter("Congrats!")
            .WithCurrentTimestamp();

        if (item.Value.HasValue)
        {
            embed.AddField("Grand Exchange Value", $"{item.Value.Value:N0} coins", inline: true);
        }

        if (item.HighAlch.HasValue)
        {
            embed.AddField("High Alchemy Value", $"{item.HighAlch.Value:N0} coins", inline: true);
        }

        return embed.Build();
    }
}