using Discord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

/// <summary>
/// Exposes the bot's Discord commands so the dashboard can render a command reference.
///
/// The list is built from the same auto-discovered <c>IDiscordCommand</c> instances the bot
/// registers with Discord, so newly added commands (including ones added via /feature) appear
/// automatically — there is no hand-maintained list to keep in sync.
/// </summary>
[ApiController]
[Route("api/commands")]
[Authorize]
public class CommandsController : ControllerBase
{
    private readonly IEnumerable<IDiscordCommand> _commands;

    public CommandsController(IEnumerable<IDiscordCommand> commands)
    {
        _commands = commands;
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult Get()
    {
        var commands = _commands
            .Select(command => command.GetCommandDefinition())
            .Select(definition => new CommandResponse(
                definition.Name.IsSpecified ? definition.Name.Value : string.Empty,
                definition.Description.IsSpecified ? definition.Description.Value : string.Empty,
                MapOptions(definition.Options)))
            .Where(command => !string.IsNullOrWhiteSpace(command.Name))
            .OrderBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Ok(commands);
    }

    private static IReadOnlyList<CommandOptionResponse> MapOptions(
        Optional<List<ApplicationCommandOptionProperties>> options)
    {
        if (!options.IsSpecified || options.Value is null)
            return Array.Empty<CommandOptionResponse>();

        return options.Value
            .Select(option => new CommandOptionResponse(
                option.Name,
                option.Description,
                option.Type.ToString(),
                option.IsRequired ?? false))
            .ToList();
    }
}

public record CommandResponse(string Name, string Description, IReadOnlyList<CommandOptionResponse> Options);

public record CommandOptionResponse(string Name, string Description, string Type, bool Required);
