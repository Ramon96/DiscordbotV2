using GLaDOS.Domain.Discord;
using GLaDOS.Infra.EntityFramework;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("api/overview")]
[Authorize]
public class OverviewController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public OverviewController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("shirtless")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetShirtlessAsync([FromQuery] int limit = 12, CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var posts = await _dbContext.Set<ShirtlessOldManPost>()
            .OrderByDescending(post => post.PostedAt)
            .Take(limit)
            .Select(post => new ShirtlessPostResponse(post.ImageUrl, post.PostedAt, post.TaggedUsername))
            .ToListAsync(cancellationToken);

        return Ok(posts);
    }
}

public record ShirtlessPostResponse(string ImageUrl, DateTime PostedAt, string? TaggedUsername);
