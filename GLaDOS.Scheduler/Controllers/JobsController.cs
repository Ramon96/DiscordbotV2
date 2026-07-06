using Hangfire;
using Hangfire.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController : ControllerBase
{
    private readonly IRecurringJobManager _recurringJobManager;

    public JobsController(IRecurringJobManager recurringJobManager)
    {
        _recurringJobManager = recurringJobManager;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult Get()
    {
        var statistics = JobStorage.Current.GetMonitoringApi().GetStatistics();

        using var connection = JobStorage.Current.GetConnection();
        var recurring = connection.GetRecurringJobs()
            .OrderBy(job => job.Id)
            .Select(job => new RecurringJobSummary(
                job.Id,
                job.Cron,
                job.LastExecution,
                job.NextExecution,
                job.LastJobState))
            .ToList();

        var stats = new JobStatistics(
            statistics.Succeeded,
            statistics.Failed,
            statistics.Processing,
            statistics.Enqueued,
            statistics.Scheduled);

        return Ok(new JobsResponse(stats, recurring));
    }

    [HttpPost("{id}/trigger")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(202)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public IActionResult Trigger(string id)
    {
        using var connection = JobStorage.Current.GetConnection();
        var exists = connection.GetRecurringJobs().Any(job => job.Id == id);
        if (!exists)
        {
            return NotFound();
        }

        _recurringJobManager.Trigger(id);
        return Accepted();
    }
}

public record JobsResponse(JobStatistics Statistics, IReadOnlyList<RecurringJobSummary> Recurring);

public record JobStatistics(long Succeeded, long Failed, long Processing, long Enqueued, long Scheduled);

public record RecurringJobSummary(
    string Id,
    string? Cron,
    DateTime? LastExecution,
    DateTime? NextExecution,
    string? LastJobState);
