using GLaDOS.Domain.OsrsFlipping;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.Scheduler.Application.Hangfire.Contracts;
using GLaDOS.Scheduler.Application.OsrsFlipping.Clients;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Application.OsrsFlipping;

[DisableConcurrentExecution(0)]
[AutomaticRetry(Attempts = 1)]
public class OsrsPriceFetcherJob : IHangfireJob
{
    private readonly ILogger<OsrsPriceFetcherJob> _logger;
    private readonly IOsrsPriceClient _priceClient;
    private readonly IServiceScopeFactory _scopeFactory;

    public OsrsPriceFetcherJob(
        ILogger<OsrsPriceFetcherJob> logger,
        IOsrsPriceClient priceClient,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _priceClient = priceClient;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting OSRS price fetcher job");

        var response = await _priceClient.GetLatestPricesAsync(cancellationToken);

        if (response?.Data is null || response.Data.Count == 0)
        {
            context.SetTextColor(ConsoleTextColor.Yellow);
            context.WriteLine("[Warning] Empty price data received from OSRS Wiki API.");
            context.ResetTextColor();
            _logger.LogWarning("Empty price data received from OSRS Wiki API");
            return;
        }

        var now = DateTime.UtcNow;
        var snapshots = new List<OsrsPriceSnapshot>();
        var totalVolume = 0L;

        foreach (var (itemIdStr, priceData) in response.Data)
        {
            if (!int.TryParse(itemIdStr, out var itemId))
            {
                continue;
            }

            var avgBuyPrice = priceData.AvgLowPrice ?? 0;
            var avgSellPrice = priceData.AvgHighPrice ?? 0;
            var volume = priceData.HighPriceVolume + priceData.LowPriceVolume;

            if (avgBuyPrice <= 0 && avgSellPrice <= 0)
            {
                continue;
            }

            snapshots.Add(new OsrsPriceSnapshot
            {
                OsrsItemId = itemId,
                AvgBuyPrice = avgBuyPrice,
                AvgSellPrice = avgSellPrice,
                Volume = volume,
                Timestamp = now
            });

            totalVolume += volume;
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        dbContext.Set<OsrsPriceSnapshot>().AddRange(snapshots);
        await dbContext.SaveChangesAsync(cancellationToken);

        context.SetTextColor(ConsoleTextColor.Green);
        context.WriteLine($"[OK] Saved {snapshots.Count} price snapshots. Total volume: {totalVolume:N0}");
        context.ResetTextColor();

        _logger.LogInformation("Saved {Count} OSRS price snapshots. Total volume: {Volume:N0}",
            snapshots.Count, totalVolume);
    }
}
