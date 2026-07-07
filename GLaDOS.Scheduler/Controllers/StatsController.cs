using GLaDOS.Domain.OldschoolRunescape;
using GLaDOS.Domain.OsrsFlipping;
using GLaDOS.Infra.EntityFramework;
using GLaDOS.Infra.EntityFramework.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public StatsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        // The users table is small, so an exact count is cheap.
        var trackedUsers = await _dbContext.Set<OldschoolRunescapeUser>().CountAsync(cancellationToken);

        // The price-snapshot and logs tables grow by millions of rows, so an exact COUNT(*) is a
        // multi-second sequential scan. Postgres already tracks an estimate (via autovacuum/ANALYZE)
        // that is plenty accurate for a headline dashboard stat.
        var priceSnapshots = await GetEstimatedRowCountAsync(typeof(OsrsPriceSnapshot), cancellationToken);
        var logEntries = await GetEstimatedRowCountAsync(typeof(LogEntry), cancellationToken);

        // Backed by IX_OsrsPriceSnapshots_Timestamp so this is an index scan, not a full sort.
        var latestPriceAt = await _dbContext.Set<OsrsPriceSnapshot>()
            .OrderByDescending(snapshot => snapshot.Timestamp)
            .Select(snapshot => (DateTime?)snapshot.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new StatsResponse(trackedUsers, priceSnapshots, logEntries, latestPriceAt));
    }

    private async Task<long> GetEstimatedRowCountAsync(Type entityType, CancellationToken cancellationToken)
    {
        var tableName = _dbContext.Model.FindEntityType(entityType)?.GetTableName();
        if (tableName is null)
        {
            return 0;
        }

        // reltuples is -1 until the table has been analysed; treat that as 0 rather than negative.
        var estimate = await _dbContext.Database
            .SqlQueryRaw<long>(
                "SELECT reltuples::bigint AS \"Value\" FROM pg_class WHERE relname = {0} AND relkind = 'r'",
                tableName)
            .FirstOrDefaultAsync(cancellationToken);

        return estimate < 0 ? 0 : estimate;
    }
}

public record StatsResponse(int TrackedUsers, long PriceSnapshots, long LogEntries, DateTime? LatestPriceAt);
