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
        var trackedUsers = await _dbContext.Set<OldschoolRunescapeUser>().CountAsync(cancellationToken);
        var priceSnapshots = await _dbContext.Set<OsrsPriceSnapshot>().LongCountAsync(cancellationToken);
        var logEntries = await _dbContext.Set<LogEntry>().LongCountAsync(cancellationToken);

        var latestPriceAt = await _dbContext.Set<OsrsPriceSnapshot>()
            .OrderByDescending(snapshot => snapshot.Timestamp)
            .Select(snapshot => (DateTime?)snapshot.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        return Ok(new StatsResponse(trackedUsers, priceSnapshots, logEntries, latestPriceAt));
    }
}

public record StatsResponse(int TrackedUsers, long PriceSnapshots, long LogEntries, DateTime? LatestPriceAt);
