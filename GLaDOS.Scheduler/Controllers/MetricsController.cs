using GLaDOS.Scheduler.Application.Dashboard.Metrics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("api/metrics")]
[Authorize]
public class MetricsController : ControllerBase
{
    private readonly MetricsHistory _history;

    public MetricsController(MetricsHistory history)
    {
        _history = history;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult Get()
    {
        return Ok(new MetricsResponse(_history.Latest(), _history.Snapshot()));
    }
}

public record MetricsResponse(MetricsSnapshot? Current, IReadOnlyList<MetricsSnapshot> History);
