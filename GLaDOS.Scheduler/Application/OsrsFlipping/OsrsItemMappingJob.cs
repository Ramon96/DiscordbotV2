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
public class OsrsItemMappingJob : IHangfireJob
{
    private readonly ILogger<OsrsItemMappingJob> _logger;
    private readonly IOsrsPriceClient _priceClient;
    private readonly IServiceScopeFactory _scopeFactory;

    public OsrsItemMappingJob(
        ILogger<OsrsItemMappingJob> logger,
        IOsrsPriceClient priceClient,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _priceClient = priceClient;
        _scopeFactory = scopeFactory;
    }

    public async Task ExecuteAsync(PerformContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting OSRS item mapping job");

        var progressBar = context.WriteProgressBar();

        var mappings = await _priceClient.GetItemMappingsAsync(cancellationToken);

        if (mappings.Count == 0)
        {
            context.SetTextColor(ConsoleTextColor.Yellow);
            context.WriteLine("[Warning] Empty mapping data received from OSRS Wiki API.");
            context.ResetTextColor();
            return;
        }

        context.WriteLine($"Fetched {mappings.Count} item mappings from API.");

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existingIds = await dbContext.Set<OsrsItemMapping>()
            .Select(m => m.OsrsItemId)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<int>(existingIds);
        var newItems = new List<OsrsItemMapping>();
        var updated = 0;

        for (var i = 0; i < mappings.Count; i++)
        {
            var entry = mappings[i];
            progressBar.SetValue((double)(i + 1) / mappings.Count * 100);

            if (existingSet.Contains(entry.Id))
            {
                var existing = await dbContext.Set<OsrsItemMapping>()
                    .FirstAsync(m => m.OsrsItemId == entry.Id, cancellationToken);

                existing.GeLimit = entry.Limit;
                updated++;
            }
            else
            {
                newItems.Add(new OsrsItemMapping
                {
                    OsrsItemId = entry.Id,
                    Name = entry.Name,
                    GeLimit = entry.Limit
                });
            }
        }

        if (newItems.Count > 0)
        {
            await dbContext.Set<OsrsItemMapping>().AddRangeAsync(newItems, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        context.SetTextColor(ConsoleTextColor.Green);
        context.WriteLine($"[OK] Inserted {newItems.Count} new items, updated {updated} existing items.");
        context.ResetTextColor();

        _logger.LogInformation("Item mapping job complete. Inserted: {New}, Updated: {Updated}",
            newItems.Count, updated);
    }
}
