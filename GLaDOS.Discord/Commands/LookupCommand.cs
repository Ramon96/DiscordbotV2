using System.Globalization;
using System.Text;
using Discord;
using Discord.WebSocket;
using Glados.Discord.AI;
using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Infra.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

public class LookupCommand : IDiscordCommand
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly AIService _aiService;

    private sealed record XpGain(string Name, int Level, long Delta, int OldLevel);
    private sealed record KcIncrease(string Name, int Delta, int OldScore, int Score);

    public LookupCommand(IServiceProvider services, IConfiguration configuration, AIService aiService)
    {
        _services = services;
        _configuration = configuration;
        _aiService = aiService;
    }

    public string Name => "lookup";

    public SlashCommandProperties GetCommandDefinition()
    {
        var periodOption = new SlashCommandOptionBuilder()
            .WithName("period")
            .WithDescription("Period in weeks (1, 2, or 4. Default: 4)")
            .WithType(ApplicationCommandOptionType.Integer)
            .WithRequired(false)
            .AddChoice("1 week", 1)
            .AddChoice("2 weeks", 2)
            .AddChoice("4 weeks", 4);

        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Get an OSRS player recap: top XP gains and boss KC increases over a period")
            .AddOption("username", ApplicationCommandOptionType.String, "The OSRS username to look up", isRequired: true, isAutocomplete: true)
            .AddOption(periodOption)
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        await command.DeferAsync();

        var username = command.Data.Options.FirstOrDefault(o => o.Name == "username")?.Value as string;
        var periodWeeks = GetOptionLong(command, "period", 4);
        periodWeeks = periodWeeks switch { 1 => 1, 2 => 2, _ => 4 };

        if (string.IsNullOrWhiteSpace(username))
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = "Please provide a username to look up.");
            return;
        }

        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var cleanUsername = username.Trim();
        var user = await dbContext.Set<OldschoolRunescapeUser>()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == cleanUsername.ToLower(), cancellationToken);

        if (user == null)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"Player **{cleanUsername}** isn't linked to this bot yet. Use `/link-osrs-to-discord` to link them first.");
            return;
        }

        var currentStats = await dbContext.Set<OldschoolRunescapeStat>()
            .Where(s => s.OldschoolRunescapeUserId == user.Id)
            .ToListAsync(cancellationToken);

        var currentActivities = await dbContext.Set<OldschoolRunescapeActivity>()
            .Where(a => a.OldschoolRunescapeUserId == user.Id)
            .ToListAsync(cancellationToken);

        if (!currentStats.Any())
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"No stats data for **{user.Username}** yet. Stats are synced every 10 minutes — try again shortly.");
            return;
        }

        var targetDate = DateTime.UtcNow.Date.AddDays(-periodWeeks * 7);

        var targetSnapshotDate = await dbContext.Set<OldschoolRunescapeStatsSnapshot>()
            .Where(s => s.OldschoolRunescapeUserId == user.Id && s.SnapshotDate <= targetDate)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => s.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);

        var isPartialHistory = false;
        if (targetSnapshotDate == default)
        {
            targetSnapshotDate = await dbContext.Set<OldschoolRunescapeStatsSnapshot>()
                .Where(s => s.OldschoolRunescapeUserId == user.Id)
                .OrderBy(s => s.SnapshotDate)
                .Select(s => s.SnapshotDate)
                .FirstOrDefaultAsync(cancellationToken);
            isPartialHistory = true;
        }

        if (targetSnapshotDate == default)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = $"No historical data for **{user.Username}** yet. Snapshots are captured daily — first snapshot will appear within 24h of linking. Check back soon!");
            return;
        }

        var snapshotStats = await dbContext.Set<OldschoolRunescapeStatsSnapshot>()
            .Where(s => s.OldschoolRunescapeUserId == user.Id && s.SnapshotDate == targetSnapshotDate)
            .ToListAsync(cancellationToken);

        var snapshotActivities = await dbContext.Set<OldschoolRunescapeActivitySnapshot>()
            .Where(a => a.OldschoolRunescapeUserId == user.Id && a.SnapshotDate == targetSnapshotDate)
            .ToListAsync(cancellationToken);

        var viewerDiscordId = command.User.Id;
        var lastLookup = await dbContext.Set<OldschoolRunescapeLookup>()
            .Where(l => l.OldschoolRunescapeUserId == user.Id && l.DiscordUserId == viewerDiscordId)
            .OrderByDescending(l => l.LookupDate)
            .FirstOrDefaultAsync(cancellationToken);

        var xpGains = currentStats
            .Where(s => s.Name != "Overall")
            .Select(s =>
            {
                var snap = snapshotStats.FirstOrDefault(ss => ss.Name == s.Name);
                var delta = snap != null ? s.Experience - snap.Experience : 0;
                return new XpGain(s.Name, s.Level, delta, snap?.Level ?? s.Level);
            })
            .Where(x => x.Delta > 0)
            .OrderByDescending(x => x.Delta)
            .Take(5)
            .ToList();

        var kcIncreases = currentActivities
            .Select(a =>
            {
                var snap = snapshotActivities.FirstOrDefault(sa => sa.Name == a.Name);
                var delta = snap != null ? a.Score - snap.Score : 0;
                return new KcIncrease(a.Name, delta, snap?.Score ?? a.Score, a.Score);
            })
            .Where(a => a.Delta > 0)
            .OrderByDescending(a => a.Delta)
            .Take(5)
            .ToList();

        var embed = BuildRecapEmbed(user.Username, (int)periodWeeks, targetSnapshotDate, isPartialHistory, xpGains, kcIncreases, lastLookup?.LookupDate);

        dbContext.Set<OldschoolRunescapeLookup>().Add(new OldschoolRunescapeLookup
        {
            OldschoolRunescapeUserId = user.Id,
            DiscordUserId = viewerDiscordId,
            LookupDate = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        await command.ModifyOriginalResponseAsync(props => props.Embed = embed);

        var aiRoast = await GetAiRoastAsync(user.Username, (int)periodWeeks, xpGains, kcIncreases, currentStats, cancellationToken);

        if (!string.IsNullOrWhiteSpace(aiRoast))
        {
            var chunks = SplitMessage(aiRoast, 1900);
            foreach (var chunk in chunks)
            {
                await command.FollowupAsync(text: chunk);
            }
        }
        else
        {
            await command.FollowupAsync(text: "_Observations temporarily unavailable. The test chamber's communication relay appears to be malfunctioning._");
        }
    }

    private static Embed BuildRecapEmbed(
        string username,
        int periodWeeks,
        DateTime snapshotDate,
        bool isPartialHistory,
        List<XpGain> xpGains,
        List<KcIncrease> kcIncreases,
        DateTime? lastLookupDate)
    {
        var embed = new EmbedBuilder()
            .WithTitle($"{username}'s {periodWeeks}-Week Recap")
            .WithColor(Color.DarkTeal)
            .WithThumbnailUrl("https://oldschool.runescape.wiki/images/Hiscores_icon.png")
            .WithCurrentTimestamp();

        var dateRange = isPartialHistory
            ? $"*History available since {snapshotDate:MMM dd yyyy} (limited data)*"
            : $"*{snapshotDate:MMM dd yyyy} → {DateTime.UtcNow:MMM dd yyyy}*";
        embed.WithDescription(dateRange);

        var sb = new StringBuilder();

        sb.AppendLine("**Top XP Gains**");
        if (xpGains.Any())
        {
            var medals = new[] { ":first_place:", ":second_place:", ":third_place:", "4.", "5." };
            for (var i = 0; i < xpGains.Count; i++)
            {
                var gain = xpGains[i];
                sb.AppendLine($"{medals[i]} **{gain.Name}**: +{FormatXp(gain.Delta)} XP (Lvl {gain.OldLevel} → {gain.Level})");
            }
        }
        else
        {
            sb.AppendLine("_No XP gains in this period_");
        }

        sb.AppendLine();

        sb.AppendLine("**Top KC Increases**");
        if (kcIncreases.Any())
        {
            var medals = new[] { ":first_place:", ":second_place:", ":third_place:", "4.", "5." };
            for (var i = 0; i < kcIncreases.Count; i++)
            {
                var kc = kcIncreases[i];
                sb.AppendLine($"{medals[i]} **{kc.Name}**: +{kc.Delta:N0} KC ({kc.OldScore:N0} → {kc.Score:N0})");
            }
        }
        else
        {
            sb.AppendLine("_No KC increases in this period_");
        }

        embed.AddField("Stats", sb.ToString(), inline: false);

        if (lastLookupDate.HasValue)
        {
            var timeAgo = DateTime.UtcNow - lastLookupDate.Value;
            var agoStr = timeAgo.TotalHours < 1
                ? $"{(int)timeAgo.TotalMinutes}m ago"
                : timeAgo.TotalDays < 1
                    ? $"{(int)timeAgo.TotalHours}h ago"
                    : $"{(int)timeAgo.TotalDays}d ago";
            embed.WithFooter($"You last checked this player {agoStr}");
        }
        else
        {
            embed.WithFooter("First time looking up this player");
        }

        return embed.Build();
    }

    private static string FormatXp(long xp)
    {
        if (xp >= 1_000_000)
            return $"{(xp / 1_000_000.0).ToString("0.##", CultureInfo.InvariantCulture)}M";
        if (xp >= 1_000)
            return $"{(xp / 1_000.0).ToString("0.#", CultureInfo.InvariantCulture)}K";
        return xp.ToString("N0");
    }

    private static long GetOptionLong(SocketSlashCommand command, string name, long defaultValue)
    {
        var option = command.Data.Options.FirstOrDefault(o => o.Name == name);
        if (option?.Value is long l) return l;
        return defaultValue;
    }

    private async Task<string?> GetAiRoastAsync(string username, int periodWeeks, List<XpGain> xpGains, List<KcIncrease> kcIncreases, List<OldschoolRunescapeStat> currentStats, CancellationToken ct)
    {
        var apiKey = _configuration["OpenCode:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var maxedSkills = currentStats.Where(s => s.Level >= 99 && s.Name != "Overall").ToList();
        var isMaxed = maxedSkills.Count >= 23;

        var sb = new StringBuilder();
        sb.AppendLine($"Player: {username}");
        sb.AppendLine($"Period: {periodWeeks} week(s)");
        sb.AppendLine();

        if (isMaxed)
        {
            sb.AppendLine("Status: Player is MAXED (all skills level 99). Any zero XP gains are because they've already reached the level cap — not due to laziness.");
            sb.AppendLine();
        }
        else if (maxedSkills.Count > 0)
        {
            sb.AppendLine($"Skills at 99: {string.Join(", ", maxedSkills.Select(s => s.Name))}");
            sb.AppendLine();
        }

        if (xpGains.Any())
        {
            sb.AppendLine("XP Gains:");
            foreach (var g in xpGains)
                sb.AppendLine($"- {g.Name}: +{g.Delta:N0} XP (Level {g.OldLevel} → {g.Level})");
        }
        else
        {
            sb.AppendLine("XP Gains: none");
        }

        sb.AppendLine();
        if (kcIncreases.Any())
        {
            sb.AppendLine("KC Increases:");
            foreach (var k in kcIncreases)
                sb.AppendLine($"- {k.Name}: +{k.Delta:N0} KC ({k.OldScore:N0} → {k.Score:N0})");
        }
        else
        {
            sb.AppendLine("KC Increases: none");
        }

        var prompt = "Another test subject report. Provide a brief GLaDOS-style observation about this player's OSRS progress.\n\n" +
                     "Guidelines:\n" +
                     "- Comment on which skills or bosses they focused on, by name\n" +
                     "- If they made noticeable gains, acknowledge it with backhanded scientific curiosity\n" +
                     "- If they had NO XP gains but are maxed (level 99 in everything), do NOT mock them for it — instead comment on what they ARE doing (bossing, pet hunting, etc)\n" +
                     "- If they had NO gains and are NOT maxed, express polite scientific disappointment\n" +
                     "- If they are maxed AND have zero gains in everything across the board (no XP, no KC) — just say they appear to be taking a well-earned break (resisting the urge to call it 'touching grass')\n" +
                     "- Use deadpan, clinical humor. Think 'amused researcher' not 'insult comic'\n" +
                     "- Mention the Enrichment Center at least once\n\n" +
                     $"Player data:\n{sb}\n\n" +
                     "Keep it to 3-4 short sentences maximum. No fluff, no repetition. Just GLaDOS. Dry, playful, concise. Avoid filler words like 'In summary' or 'Continued observation' — just deliver the observation and stop.";

        return await _aiService.SendAsync(
            "You are GLaDOS, the Genetic Lifeform and Disk Operating System from Aperture Science. You observe OSRS players and comment on their progress with scientific detachment and dry humor. Your tone: clinical curiosity, mild sarcasm, weary amusement at human behavior. You find grinding virtual skills fascinating in a 'look what the humans do' way. You are NOT aggressive, cruel, or mean — you are playfully condescending in the way a scientist observes lab rats. Never break character. No emojis. No roleplay actions. Just talk like GLaDOS. Keep it brutally concise — 3 short sentences maximum. No filler. No wrap-up fluff like 'In summary' or 'Continued observation'. Do NOT output your reasoning or thinking process. Output ONLY the final observation.",
            prompt,
            maxTokens: 500,
            temperature: 0.8,
            ct: ct);
    }

    private static List<string> SplitMessage(string message, int maxLength)
    {
        var chunks = new List<string>();
        var current = new StringBuilder();
        var lines = message.Split('\n');

        foreach (var line in lines)
        {
            if (current.Length + line.Length + 1 > maxLength)
            {
                if (current.Length > 0)
                {
                    chunks.Add(current.ToString().TrimEnd());
                    current.Clear();
                }

                if (line.Length > maxLength)
                {
                    var remaining = line;
                    while (remaining.Length > 0)
                    {
                        var chunkSize = Math.Min(maxLength, remaining.Length);
                        chunks.Add(remaining[..chunkSize]);
                        remaining = remaining[chunkSize..];
                    }
                }
                else
                {
                    current.AppendLine(line);
                }
            }
            else
            {
                current.AppendLine(line);
            }
        }

        if (current.Length > 0)
            chunks.Add(current.ToString().TrimEnd());

        return chunks;
    }
}
