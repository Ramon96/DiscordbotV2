using Glados.Discord.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("api/changelog")]
[Authorize]
public class ChangelogController : ControllerBase
{
    private readonly GitHubService _gitHub;

    public ChangelogController(GitHubService gitHub)
    {
        _gitHub = gitHub;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var entries = await _gitHub.GetRecentMergedPullRequestsAsync(50, cancellationToken);
        return Ok(entries);
    }
}
