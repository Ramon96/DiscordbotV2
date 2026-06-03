using GLaDOS.Domain.OsrsFlipping;
using GLaDOS.Infra.EntityFramework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("[controller]")]
public class FlippingController : ControllerBase
{
    private readonly IServiceScopeFactory _scopeFactory;

    public FlippingController(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    [HttpGet("opportunities")]
    [ProducesResponseType(typeof(List<FlippingOpportunityDto>), 200)]
    public async Task<IActionResult> GetOpportunitiesAsync(
        [FromQuery] long minNetProfit = 100_000,
        [FromQuery] long minVolume = 200,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var latestTimestamp = await dbContext.Set<OsrsPriceSnapshot>()
            .MaxAsync(s => (DateTime?)s.Timestamp, cancellationToken);

        if (latestTimestamp is null)
        {
            return Ok(new List<FlippingOpportunityDto>());
        }

        var latestSnapshots = await dbContext.Set<OsrsPriceSnapshot>()
            .Where(s => s.Timestamp == latestTimestamp.Value)
            .ToListAsync(cancellationToken);

        var itemIds = latestSnapshots.Select(s => s.OsrsItemId).Distinct().ToList();

        var mappings = await dbContext.Set<OsrsItemMapping>()
            .Where(m => itemIds.Contains(m.OsrsItemId))
            .ToDictionaryAsync(m => m.OsrsItemId, cancellationToken);

        var opportunities = latestSnapshots
            .Select(s =>
            {
                mappings.TryGetValue(s.OsrsItemId, out var mapping);

                var avgBuyPrice = s.AvgBuyPrice;
                var avgSellPrice = s.AvgSellPrice;

                    var grossMargin = avgBuyPrice - avgSellPrice;
                var tax = (long)(avgBuyPrice * 0.02);
                var netProfit = grossMargin - tax;

                return new FlippingOpportunityDto
                {
                    OsrsItemId = s.OsrsItemId,
                    Name = mapping?.Name ?? $"Item #{s.OsrsItemId}",
                    GeLimit = mapping?.GeLimit,
                    AvgBuyPrice = avgBuyPrice,
                    AvgSellPrice = avgSellPrice,
                    GrossMargin = grossMargin,
                    Tax = tax,
                    NetProfit = netProfit,
                    Volume = s.Volume,
                    LastUpdated = s.Timestamp
                };
            })
            .Where(o => o.AvgBuyPrice > 0 && o.AvgSellPrice > 0 && o.NetProfit > minNetProfit && o.Volume > minVolume)
            .OrderByDescending(o => o.NetProfit)
            .ToList();

        return Ok(opportunities);
    }
}
