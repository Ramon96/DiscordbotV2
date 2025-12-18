using System.Threading;
using System.Threading.Tasks;
using GLaDOS.OldschoolRunescape.Clients.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("[controller]")]
public class OldschoolRunescapeController : ControllerBase
{
    private readonly ILogger<OldschoolRunescapeController> _logger;
    private readonly IOldschoolRunescapeClient _client;
    public OldschoolRunescapeController(ILogger<OldschoolRunescapeController> logger, IOldschoolRunescapeClient client)
    {
        _logger = logger;
        _client = client;
    }

    [HttpGet("User")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetUserAsync([FromQuery] string username, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username cannot be empty.");
        }

        var hiscores = await _client.GetHiScoresByUsernameAsync(username);

        if (hiscores == null)
        {
            return NotFound($"User '{username}' not found.");
        }

        return Ok(hiscores);
    }

    //  Add player
}