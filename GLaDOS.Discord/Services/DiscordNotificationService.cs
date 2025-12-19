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
    
    public async Task SendHiscoreUpdatesAsync(OldschoolRunescapeUser user, OldschoolRunescapeHiscoreChanges changes)
    {
        var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
        if (channel == null) return;
        
        if (changes.StatChanges.Any())
        {
            var statEmbed = BuildStatEmbed(user, changes.StatChanges);
            await channel.SendMessageAsync(embed: statEmbed.Build());
        }
        
        foreach (var activityChange in changes.ActivityChanges)
        {
            // Filter eventueel oninteressante activities (zoals League Points)
            if (activityChange.ScoreDifference <= 0) continue;

            var activityEmbed = BuildActivityEmbed(user, activityChange);
            await channel.SendMessageAsync(embed: activityEmbed.Build());
        }
    }

    private EmbedBuilder BuildStatEmbed(OldschoolRunescapeUser user, List<StatChange> stats)
    {
        var totalLevels = stats.Sum(s => s.NewLevel - s.OldLevel);
        var embed = new EmbedBuilder()
            .WithTitle($"Congratulations to {user.Username}!")
            .WithDescription($"{user.Username} has just completed an epic journey! Behold the amazing levels they've gained:")
            .WithColor(Color.Green) // Runescape groen
            .WithCurrentTimestamp();

        foreach (var stat in stats)
        {
            embed.AddField(
                $"{stat.StatName} (+{stat.NewLevel - stat.OldLevel})", 
                $"from {stat.OldLevel} to {stat.NewLevel}!", 
                inline: true
            );
        }
        
        embed.WithFooter($"Overall, {user.Username} gained an incredible total of ({totalLevels}) levels! Amazing job! 🎉");

        // Optioneel: Voeg user thumbnail toe als je die hebt
        // embed.WithThumbnailUrl("https://oldschool.runescape.wiki/images/Stats_icon.png");

        return embed;
    }

    private EmbedBuilder BuildActivityEmbed(OldschoolRunescapeUser user, ActivityChange activity)
    {
        // TODO Haal specifieke data op voor deze boss (tekstjes, kleurtjes, plaatjes)
        // var flavor = ActivityFlavorHelper.GetFlavor(activity.ActivityName);

        var embed = new EmbedBuilder()
            .WithTitle(activity.ActivityName)
            .WithDescription($" **{user.Username}** ")
            .WithColor(Color.Blue)
            // .WithDescription($"{flavor.Icon} **{user.Username}** {flavor.Message}")
            // .WithColor(flavor.Color)
            // .WithThumbnailUrl(flavor.ImageUrl)
            .WithCurrentTimestamp();

        // Voorbeeld: 72 (+2)
        embed.AddField(user.Username, $"{activity.NewScore} (+{activity.ScoreDifference})", inline: true);
        
        embed.WithFooter($"Keep up the grind! • {DateTime.Now:dd/MM/yyyy HH:mm}");

        return embed;
    }
}