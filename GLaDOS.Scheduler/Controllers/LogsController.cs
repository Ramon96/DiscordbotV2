using GLaDOS.Infra.EntityFramework;
using GLaDOS.Infra.EntityFramework.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private const int MaxLimit = 500;

    private readonly ApplicationDbContext _dbContext;

    public LogsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Get(
        [FromQuery] string? level,
        [FromQuery] string? search,
        [FromQuery] DateTimeOffset? since,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, MaxLimit);

        var query = _dbContext.Set<LogEntry>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(level))
        {
            query = query.Where(log => log.Level == level);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(log => EF.Functions.ILike(log.Message, pattern));
        }

        if (since is not null)
        {
            query = query.Where(log => log.Timestamp >= since);
        }

        var items = await query
            .OrderByDescending(log => log.Timestamp)
            .Take(take)
            .Select(log => new LogDto(
                log.Id,
                log.Timestamp,
                log.Level,
                log.Message,
                log.Exception,
                log.SourceContext))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }
}

public record LogDto(
    long Id,
    DateTimeOffset Timestamp,
    string Level,
    string Message,
    string? Exception,
    string? SourceContext);
