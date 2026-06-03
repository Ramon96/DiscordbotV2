using System.Globalization;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
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

    private static readonly HttpClient _aiClient = new()
    {
        BaseAddress = new Uri("https://opencode.ai/zen/v1/"),
        Timeout = TimeSpan.FromSeconds(60)
    };

    public OsrsFlipsCommand(IServiceProvider services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }

    public string Name => "osrsflips";

    public SlashCommandProperties GetCommandDefinition()
    {
        return new SlashCommandBuilder()
            .WithName(Name)
            .WithDescription("Get top OSRS Grand Exchange flipping opportunities with optional AI analysis")
            .AddOption("limit", ApplicationCommandOptionType.Integer, "Number of opportunities (1-10, default: 5)", isRequired: false)
            .AddOption("min-profit", ApplicationCommandOptionType.Integer, "Minimum net profit in gp (default: 0)", isRequired: false)
            .AddOption("min-volume", ApplicationCommandOptionType.Integer, "Minimum 24h volume (default: 0)", isRequired: false)
            .AddOption("include-analysis", ApplicationCommandOptionType.Boolean, "Include AI analysis (default: true)", isRequired: false)
            .Build();
    }

    public async Task ExecuteAsync(SocketSlashCommand command, CancellationToken cancellationToken = default)
    {
        await command.DeferAsync();

        var limit = (int)Math.Clamp(GetOptionLong(command, "limit", 5), 1, 10);
        var minProfit = GetOptionLong(command, "min-profit", 0);
        var minVolume = GetOptionLong(command, "min-volume", 0);
        var includeAnalysis = GetOptionBool(command, "include-analysis", true);

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

        if (includeAnalysis)
        {
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
    }

    private static long GetOptionLong(SocketSlashCommand command, string name, long defaultValue)
    {
        var option = command.Data.Options.FirstOrDefault(o => o.Name == name);
        if (option?.Value is long l) return l;
        return defaultValue;
    }

    private static bool GetOptionBool(SocketSlashCommand command, string name, bool defaultValue)
    {
        var option = command.Data.Options.FirstOrDefault(o => o.Name == name);
        if (option?.Value is bool b) return b;
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

        var prompt = $"You are an OSRS (Old School RuneScape) Grand Exchange trading analyst. Analyze these top {opportunities.Count} flipping opportunities considering the 2% GE tax. For each item, provide:\n" +
                     "1. Recommended buy price and sell price (use the data; suggest exact gp values)\n" +
                     "2. Profitability and risk (considering volume, GE limit, and margin stability)\n" +
                     "3. Market sentiment (bullish/bearish based on spread patterns)\n" +
                     "4. Recommended approach (active flip vs passive investment)\n" +
                     "5. Overall portfolio recommendation\n\n" +
                     $"Item data (Buy=what you pay, Sell=what you get when selling, 2% tax is on Sell price):\n{itemSummaries}\n\n" +
                     "Respond with 1-2 sentence bullet points. Keep it very concise. Use headers: ## Market Overview, ## Per-Item Analysis, ## Risk Assessment, ## Recommended Strategy. For each item under ## Per-Item Analysis, include a specific \"Buy at X gp, sell at Y gp\" recommendation.";

        try
        {
            var requestBody = new
            {
                model = "nemotron-3-super-free",
                messages = new[]
                {
                    new { role = "system", content = "You are an expert OSRS Grand Exchange trading analyst. Be concise and data-driven." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 4000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            httpRequest.Headers.Add("Authorization", $"Bearer {apiKey}");

            var response = await _aiClient.SendAsync(httpRequest, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var message = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message");

            var analysis = message.GetProperty("content").GetString();
            return analysis;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI analysis failed: {ex.Message}");
            return null;
        }
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
