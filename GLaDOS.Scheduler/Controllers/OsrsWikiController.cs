using GLaDOS.OsrsWiki.Clients.Contracts;
using GLaDOS.OsrsWiki.Requests;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("[controller]")]
public class OsrsWikiController : ControllerBase
{
    private readonly IOsrsWikiSyncClient _wikiSyncClient;
    private readonly IOsrsWikiItemClient _wikiItemClient;

    public OsrsWikiController(IOsrsWikiSyncClient wikiSyncClient, IOsrsWikiItemClient wikiItemClient)
    {
        _wikiSyncClient = wikiSyncClient;
        _wikiItemClient = wikiItemClient;
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
            await _wikiSyncClient.GetOsrsWikiSyncDataAsync(new OsrsWikiSyncRequest { Username = username }, cancellationToken);

        if (syncData == null)
        {
            return NotFound($"User '{username}' not found.");
        }

        return Ok(syncData);
    }
    
    [HttpGet("Item")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetUserSyncDataAsync([FromQuery] int itemId,
        CancellationToken cancellationToken = default)
    {

        var item = await _wikiItemClient.GetItemDetailsAsync(itemId, cancellationToken);

        if (item == null)
        {
            return NotFound($"Item '{itemId}' not found.");
        }

        return Ok(item);
    }
}