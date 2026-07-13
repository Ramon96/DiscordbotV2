using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using GLaDOS.Domain.Discord;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.Scheduler.Application.Hangfire.Contracts;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Application.Discord;

// One-off (idempotent) backfill: shirtless-old-man posts predate the storage table, so scan the
// channel history for the bot's prior posts and record them. Safe to re-run — rows de-dupe by
// MessageId. Enqueued on startup with a short delay so the Discord client has time to connect.
[DisableConcurrentExecution(300)]
[AutomaticRetry(Attempts = 5)]
public class ShirtlessOldManBackfillJob : IHangfireJob
{
    private readonly ILogger<ShirtlessOldManBackfillJob> _logger;
    private readonly DiscordSocketClient _discord;
    private readonly IServiceScopeFactory _scopeFactory;

    private const ulong GeneralChannelId = 867074325824012382;
    private const int MessagesToScan = 1000;

    private static readonly Regex MentionPattern = new(@"<@!?(?<id>\d+)>", RegexOptions.Compiled);

    public ShirtlessOldManBackfillJob(
        ILogger<ShirtlessOldManBackfillJob> logger,
        DiscordSocketClient discord,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _discord = discord;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default)
    {
        if (_discord.ConnectionState != ConnectionState.Connected)
        {
            // On startup the gateway may not be connected yet — throw so Hangfire retries with backoff.
            throw new InvalidOperationException($"Discord not connected (state: {_discord.ConnectionState})");
        }

        if (_discord.GetChannel(GeneralChannelId) is not ITextChannel channel)
        {
            _logger.LogWarning("General channel {ChannelId} not found for shirtless backfill", GeneralChannelId);
            context.WriteLine($"Channel {GeneralChannelId} not found.");
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var known = (await dbContext.Set<ShirtlessOldManPost>()
                .Select(post => post.MessageId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var selfId = _discord.CurrentUser.Id;
        var toAdd = new List<ShirtlessOldManPost>();

        var messages = await channel.GetMessagesAsync(MessagesToScan).FlattenAsync();
        foreach (var message in messages)
        {
            if (message.Author.Id != selfId || known.Contains(message.Id))
            {
                continue;
            }

            if (!TryGetShirtlessImage(message, out var imageUrl))
            {
                continue;
            }

            var taggedId = TryGetMentionedUserId(message.Content);
            toAdd.Add(new ShirtlessOldManPost
            {
                MessageId = message.Id,
                ImageUrl = imageUrl,
                TaggedDiscordUserId = taggedId,
                TaggedUsername = taggedId != 0 ? _discord.GetUser(taggedId)?.Username : null,
                PostedAt = message.Timestamp.UtcDateTime,
            });
            known.Add(message.Id);
        }

        if (toAdd.Count > 0)
        {
            dbContext.Set<ShirtlessOldManPost>().AddRange(toAdd);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Shirtless backfill complete: {Added} new post(s) recorded", toAdd.Count);
        context.WriteLine($"Backfilled {toAdd.Count} shirtless post(s).");
    }

    // A shirtless post is one of the bot's messages carrying the distinctive marker plus an embedded image.
    private static bool TryGetShirtlessImage(IMessage message, out string imageUrl)
    {
        imageUrl = string.Empty;

        if (!message.Content.Contains("check dit", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var embedImage = message.Embeds
            .Select(embed => embed.Image?.Url)
            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

        if (string.IsNullOrWhiteSpace(embedImage))
        {
            return false;
        }

        imageUrl = embedImage;
        return true;
    }

    private static ulong TryGetMentionedUserId(string content)
    {
        var match = MentionPattern.Match(content);
        return match.Success && ulong.TryParse(match.Groups["id"].Value, out var id) ? id : 0;
    }
}
