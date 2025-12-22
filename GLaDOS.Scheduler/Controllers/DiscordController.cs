using Glados.Discord.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("[controller]")]
public class DiscordController : ControllerBase
{
    private readonly IDiscordClient _discordClient;

    public DiscordController(IDiscordClient discordClient)
    {
        _discordClient = discordClient;
    }

    [HttpPost("Speak")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Speak([FromBody] string message, ulong? channelId, bool tts, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(message))
        {
            return BadRequest("Message cannot be empty.");
        }

        await _discordClient.Speak(message, channelId, tts, cancellationToken);
        
        return Ok(message);
    }
}