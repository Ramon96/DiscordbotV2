using System.Security.Claims;
using GLaDOS.Scheduler.Application.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync(
        [FromQuery] string? returnUrl,
        [FromServices] IAuthenticationSchemeProvider schemeProvider)
    {
        // The Discord scheme is only registered when its client id/secret are configured. If it's
        // missing, redirect back to the SPA with a friendly error instead of throwing.
        if (await schemeProvider.GetSchemeAsync(DashboardAuthExtensions.DiscordScheme) is null)
        {
            return Redirect("/dashboard/?authError=1");
        }

        // Kicks off the Discord OAuth flow; the handler signs in the cookie on the callback and
        // redirects back to the SPA.
        var redirect = string.IsNullOrEmpty(returnUrl) ? "/dashboard/" : returnUrl;
        var properties = new AuthenticationProperties { RedirectUri = redirect, IsPersistent = true };
        return Challenge(properties, DashboardAuthExtensions.DiscordScheme);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(204)]
    public async Task<IActionResult> LogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var name = User.FindFirstValue("urn:discord:global_name")
                   ?? User.FindFirstValue(ClaimTypes.Name)
                   ?? "friend";
        var avatarHash = User.FindFirstValue("urn:discord:avatar");
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Viewer";

        var avatarUrl = !string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(avatarHash)
            ? $"https://cdn.discordapp.com/avatars/{id}/{avatarHash}.png"
            : null;

        return Ok(new CurrentUserResponse(id, name, avatarUrl, role));
    }
}

public record CurrentUserResponse(string Id, string Name, string? AvatarUrl, string Role);
