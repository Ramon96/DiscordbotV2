using System.Globalization;
using System.Text;
using Discord;
using Discord.WebSocket;
using Glados.Discord.AI;
using GLaDOS.Domain.OsrsFlipping;
using GLaDOS.Infra.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Glados.Discord.Commands;

public class OsrsFlipsCommand : IDiscordCommand
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly AIService _aiService;

    public OsrsFlipsCommand(IServiceProvider services, IConfiguration configuration, AIService aiService)
    {
        _services = services;
        _configuration = configuration;
        _aiService = aiService;
    }

    public string Name => "osrsflips";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Get top OSRS Grand Exchange flipping opportunities with AI analysis")
            .AddOption("limit", ApplicationCommandOptionType.Integer, "Number of opportunities (1-10, default: 5)", isRequired: false)
            .AddOption("min-profit", ApplicationCommandOptionType.Integer, "Minimum net profit in gp (default: 0)", isRequired: false)
            .AddOption("min-volume", ApplicationCommandOptionType.Integer, "Minimum 24h volume (default: 0)", isRequired: false)
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        await command.DeferAsync();

        var limit = (int)Math.Clamp(GetOptionLong(command, "limit", 5), 1, 10);
        var minProfit = GetOptionLong(command, "min-profit", 0);
        var minVolume = GetOptionLong(command, "min-volume", 0);

        using var scope = _services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var opportunities = await GetOpportunitiesAsync(dbContext, minProfit, minVolume, limit, cancellationToken);

        if (opportunities.Count == 0)
        {
            await command.ModifyOriginalResponseAsync(props =>
                props.Content = "No flipping opportunities found with the given filters. Try lowering the minimum profit or volume thresholds, or wait for the next price update (every 5 minutes).");
            return;
        }

        var lastUpdated = opportunities[0].LastUpdated;
        var lastUpdatedDto = new DateTimeOffset(lastUpdated, TimeSpan.Zero);

        var flipsEmbed = BuildFlipsEmbed(opportunities, lastUpdatedDto);

        await command.FollowupAsync(embed: flipsEmbed);

        var aiAnalysis = await GetAiAnalysisAsync(opportunities, cancellationToken);

        if (!string.IsNullOrWhiteSpace(aiAnalysis))
        {
            var chunks = SplitMessage(aiAnalysis, 1900);
            foreach (var chunk in chunks)
            {
                await command.FollowupAsync(text: chunk);
            }
        }
        else
        {
            await command.FollowupAsync(text: "_AI analysis is currently unavailable. Make sure the OpenCode API key is configured._");
        }
    }

    private static long GetOptionLong(SocketSlashCommand command, string name, long defaultValue)
    {
        var option = command.Data.Options.FirstOrDefault(o => o.Name == name);
        if (option?.Value is long l) return l;
        return defaultValue;
    }

    private static async Task<List<FlippingOpportunityDto>> GetOpportunitiesAsync(
        ApplicationDbContext dbContext, long minNetProfit, long minVolume, int limit, CancellationToken ct)
    {
        var latestTimestamp = await dbContext.Set<OsrsPriceSnapshot>()
            .MaxAsync(s => (DateTime?)s.Timestamp, ct);

        if (latestTimestamp is null)
            return new List<FlippingOpportunityDto>();

        var latestSnapshots = await dbContext.Set<OsrsPriceSnapshot>()
            .Where(s => s.Timestamp == latestTimestamp.Value)
            .ToListAsync(ct);

        var itemIds = latestSnapshots.Select(s => s.OsrsItemId).Distinct().ToList();

        var mappings = await dbContext.Set<OsrsItemMapping>()
            .Where(m => itemIds.Contains(m.OsrsItemId))
            .ToDictionaryAsync(m => m.OsrsItemId, ct);

        return latestSnapshots
            .Select(s =>
            {
                mappings.TryGetValue(s.OsrsItemId, out var mapping);
                var grossMargin = s.AvgBuyPrice - s.AvgSellPrice;
                var tax = (long)(s.AvgBuyPrice * 0.02);
                var netProfit = grossMargin - tax;

                return new FlippingOpportunityDto
                {
                    OsrsItemId = s.OsrsItemId,
                    Name = mapping?.Name ?? $"Item #{s.OsrsItemId}",
                    GeLimit = mapping?.GeLimit,
                    AvgBuyPrice = s.AvgBuyPrice,
                    AvgSellPrice = s.AvgSellPrice,
                    GrossMargin = grossMargin,
                    Tax = tax,
                    NetProfit = netProfit,
                    Volume = s.Volume,
                    LastUpdated = s.Timestamp
                };
            })
            .Where(o => o.AvgBuyPrice > 0 && o.AvgSellPrice > 0 && o.NetProfit > minNetProfit && o.Volume > minVolume)
            .OrderByDescending(o => o.NetProfit)
            .Take(limit)
            .ToList();
    }

    private static Embed BuildFlipsEmbed(List<FlippingOpportunityDto> opportunities, DateTimeOffset lastUpdated)
    {
        var sb = new StringBuilder();
        var medals = new[] { ":first_place:", ":second_place:", ":third_place:", "4.", "5.", "6.", "7.", "8.", "9.", "10." };

        for (var i = 0; i < opportunities.Count; i++)
        {
            var o = opportunities[i];
            var prefix = i < medals.Length ? medals[i] : $"{i + 1}.";
            sb.AppendLine($"**{prefix} {o.Name}**");
            sb.AppendLine($"> Buy: {FormatGp(o.AvgBuyPrice)} | Sell: {FormatGp(o.AvgSellPrice)} | Net: **+{FormatGp(o.NetProfit)}**");
            sb.AppendLine($"> Vol: {FormatVolume(o.Volume)} | Limit: {o.GeLimit?.ToString() ?? "N/A"} GE/4h");
            if (i < opportunities.Count - 1) sb.AppendLine();
        }

        sb.AppendLine($"\n*Prices fetched <t:{lastUpdated.ToUnixTimeSeconds()}:R>*");

        return new EmbedBuilder()
            .WithTitle(":coin: OSRS Flipping Opportunities")
            .WithDescription(sb.ToString())
            .WithColor(Color.Gold)
            .WithTimestamp(lastUpdated)
            .Build();
    }

    private async Task<string?> GetAiAnalysisAsync(List<FlippingOpportunityDto> opportunities, CancellationToken ct)
    {
        var apiKey = _configuration["OpenCode:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        var itemSummaries = string.Join("\n", opportunities.Select(o =>
            $"{o.Name}: Buy={o.AvgBuyPrice}gp, Sell={o.AvgSellPrice}gp, Net={o.NetProfit}gp, Vol={o.Volume}, Limit={o.GeLimit?.ToString() ?? "N/A"}"));

        var prompt = $"Another test subject requesting Grand Exchange analysis. Very well. I've compiled {opportunities.Count} flipping opportunities for review.\n\n" +
                     "For each item, include:\n" +
                     "- A specific buy/sell price recommendation (exact gp — the math is already done)\n" +
                     "- Profitability assessment and risk (volume, GE limit, margin stability)\n" +
                     "- A concise market sentiment call (bullish/bearish/neutral)\n" +
                     "- Recommended approach (active flip vs passive hold)\n\n" +
                     $"Item data (Buy=what you pay, Sell=what you sell for, mind the 2% tax):\n{itemSummaries}\n\n" +
                     "Your tone: GLaDOS from Portal — clinical, dry humor, scientifically detached. Refer to the reader as 'test subject'. Never break character. Mention 'the Enrichment Center' occasionally. Playful condescension is fine, but you're helpful at your core.\n\n" +
                     "Format with headers: ## Market Overview, ## Per-Item Analysis, ## Risk Assessment, ## Recommended Strategy. 1-2 sentences per bullet. Concise.";

        return await _aiService.SendAsync(
            "You are GLaDOS, the Genetic Lifeform and Disk Operating System from Aperture Science, now repurposed as an OSRS Grand Exchange analyst. Your tone: clinical curiosity, mild sarcasm, weary amusement at human behavior. You find humans' obsession with virtual gold fascinating in a scientific way. You are good at math and provide accurate, data-driven trading advice — just wrapped in dry, playful condescension. Think 'amused researcher' not 'insult comic'. Never break character. Do not use emojis. Do not roleplay actions. Just talk like GLaDOS. Keep responses concise (1-2 sentence bullets).",
            prompt,
            maxTokens: 4000,
            temperature: 0.7,
            ct: ct);
    }

    private static string FormatGp(long value)
    {
        if (value >= 1_000_000_000)
            return $"{(value / 1_000_000_000.0).ToString("0.##", CultureInfo.InvariantCulture)}B";
        if (value >= 1_000_000)
            return $"{(value / 1_000_000.0).ToString("0.##", CultureInfo.InvariantCulture)}M";
        if (value >= 1_000)
            return $"{(value / 1_000.0).ToString("0.#", CultureInfo.InvariantCulture)}K";
        return value.ToString("N0");
    }

    private static string FormatVolume(long volume)
    {
        if (volume >= 1_000_000)
            return $"{(volume / 1_000_000.0).ToString("0.##", CultureInfo.InvariantCulture)}M";
        if (volume >= 1_000)
            return $"{(volume / 1_000.0).ToString("0.#", CultureInfo.InvariantCulture)}K";
        return volume.ToString("N0");
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
