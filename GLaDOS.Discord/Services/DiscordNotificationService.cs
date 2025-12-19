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
    
    public async Task SendConsolidatedUpdatesAsync(List<(OldschoolRunescapeUser User, OldschoolRunescapeHiscoreChanges Changes)> allUpdates)
{
    var channel = _client.GetChannel(_notificationChannelId) as IMessageChannel;
    if (channel == null) return;

   
    var skillGroups = allUpdates
        .SelectMany(user => user.Changes.StatChanges.Select(stat => new { user.User, Stat = stat }))
        .GroupBy(userSkills => userSkills.Stat.StatName) 
        .ToList();

    if (skillGroups.Any())
    {
        var embed = new EmbedBuilder()
            .WithTitle("🌍 OSRS Level Gains Report")
            .WithDescription("The following players have made progress!")
            .WithColor(Color.Green)
            .WithCurrentTimestamp();

        foreach (var group in skillGroups)
        {
            string skillText = "";
            foreach (var userSkills in group)
            {
                int diff = userSkills.Stat.NewLevel - userSkills.Stat.OldLevel;
                skillText += $"**{userSkills.User.Username}**: {userSkills.Stat.OldLevel} → **{userSkills.Stat.NewLevel}** (+{diff})\n";
            }

            embed.AddField(group.Key, skillText, inline: false);
        }
        
        await channel.SendMessageAsync(embed: embed.Build());
    }
    
    var activityGroups = allUpdates
        .SelectMany(u => u.Changes.ActivityChanges.Select(act => new { u.User, Activity = act }))
        .GroupBy(x => x.Activity.ActivityName)
        .ToList();

    foreach (var group in activityGroups)
    {
        // Voor bosses maken we per BOSS een embed (zodat we het plaatje kunnen gebruiken),
        // maar we zetten wel alle spelers die die boss deden in die ene embed.
        
        // var flavor = ActivityFlavorHelper.GetFlavor(group.Key);
        
        var embed = new EmbedBuilder()
            .WithTitle(group.Key) // Bv. "Theatre of Blood"
            // .WithThumbnailUrl(flavor.ImageUrl)
            // .WithColor(flavor.Color)
            .WithCurrentTimestamp();

        string bossText = "";
        foreach (var entry in group)
        {
            // bossText += $"{flavor.Icon} **{entry.User.Username}**: {entry.Activity.NewScore} KC (+{entry.Activity.ScoreDifference})\n";
            bossText += $"**{entry.User.Username}**: {entry.Activity.NewScore} KC (+{entry.Activity.ScoreDifference})\n";
            
        }
        
        embed.WithDescription(bossText);
        await channel.SendMessageAsync(embed: embed.Build());
    }
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