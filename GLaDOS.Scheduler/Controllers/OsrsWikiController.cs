using GLaDOS.OsrsWiki.Clients.Contracts;
using GLaDOS.OsrsWiki.Requests;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("[controller]")]
public class OsrsWikiController : ControllerBase
{
    private readonly IOsrsWikiSyncClient _client;

    public OsrsWikiController(IOsrsWikiSyncClient client)
    {
        _client = client;
    }

    [HttpGet("UserSyncData")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetUserSyncDataAsync([FromQuery] string username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest("Username cannot be empty.");
        }

        var syncData =
            await _client.GetOsrsWikiSyncDataAsync(new OsrsWikiSyncRequest { Username = username }, cancellationToken);

        if (syncData == null)
        {
            return NotFound($"User '{username}' not found.");
        }

        return Ok(syncData);
    }
}