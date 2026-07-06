using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GLaDOS.Scheduler.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        var expectedUser = _configuration["Hangfire:Username"];
        var expectedPass = _configuration["Hangfire:Password"];

        if (string.IsNullOrEmpty(expectedUser) ||
            request.Username != expectedUser ||
            request.Password != expectedPass)
        {
            return Unauthorized();
        }

        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, expectedUser) },
            CookieAuthenticationDefaults.AuthenticationScheme);

        // IsPersistent makes the browser keep the cookie across restarts (for the scheme's
        // ExpireTimeSpan); without it the cookie is a session cookie and login is lost on close.
        var authProperties = new AuthenticationProperties { IsPersistent = true };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            authProperties);

        return Ok(new CurrentUserResponse(expectedUser));
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
        return Ok(new CurrentUserResponse(User.Identity?.Name ?? string.Empty));
    }
}

public record LoginRequest(string Username, string Password);

public record CurrentUserResponse(string Username);
