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
    private readonly IOldschoolRunescapeClient _client;
    public OldschoolRunescapeController(IOldschoolRunescapeClient client)
    {
        _client = client;
    }

    [HttpGet("User")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetUserAsync([FromQuery] string username, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username cannot be empty.");
        }

        var hiscores = await _client.GetHiScoresByUsernameAsync(username, cancellationToken);

        if (hiscores == null)
        {
            return NotFound($"User '{username}' not found.");
        }

        return Ok(hiscores);
    }
}