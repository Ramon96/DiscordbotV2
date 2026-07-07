using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace GLaDOS.Scheduler.Application.Dashboard;

public static class DashboardAuthExtensions
{
    public const string DiscordScheme = "Discord";
    private const string AvatarClaim = "urn:discord:avatar";
    private const string GlobalNameClaim = "urn:discord:global_name";

    public static IServiceCollection AddDashboardAuth(this IServiceCollection services, IConfiguration configuration)
    {
        // Persist the Data Protection key ring to a mounted volume. Cookie auth encrypts the
        // cookie with these keys; without a stable key ring, every container recreation (each
        // deploy) generates new keys and invalidates existing login cookies.
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
            .SetApplicationName("glados-dashboard");

        // TLS terminates at the nginx reverse proxy, so trust its forwarded scheme/ip headers.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var clientId = configuration["Discord:ClientId"];
        var clientSecret = configuration["Discord:ClientSecret"];

        var authenticationBuilder = services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "glados.dashboard";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;

                // The SPA drives its own routing, so the API answers with status codes rather
                // than redirecting to a server-rendered login page.
                options.Events.OnRedirectToLogin = context => WriteStatus(context, StatusCodes.Status401Unauthorized);
                options.Events.OnRedirectToAccessDenied = context => WriteStatus(context, StatusCodes.Status403Forbidden);

                // Reject any cookie that isn't a real Discord sign-in. Data Protection keys persist
                // across deploys, so legacy cookies from the old password login (no Discord id, no
                // role) still decrypt and would grant access — this forces every session through
                // the current Discord OAuth flow.
                options.Events.OnValidatePrincipal = async context =>
                {
                    var hasDiscordId = !string.IsNullOrEmpty(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier));
                    var hasRole = context.Principal?.FindFirst(ClaimTypes.Role) is not null;
                    if (!hasDiscordId || !hasRole)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                };
            });

        // Only register the Discord scheme when it's actually configured. The OAuth handler
        // participates in every request (to detect its callback) and validates its options on
        // init, so registering it with an empty ClientId/Secret throws on every request and takes
        // the whole site down. Without these secrets the dashboard simply can't be signed into.
        if (!string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(clientSecret))
        {
            authenticationBuilder.AddOAuth(DiscordScheme, options =>
            {
                options.ClientId = clientId;
                options.ClientSecret = clientSecret;
                options.CallbackPath = "/api/auth/callback";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.AuthorizationEndpoint = "https://discord.com/api/oauth2/authorize";
                options.TokenEndpoint = "https://discord.com/api/oauth2/token";
                options.UserInformationEndpoint = "https://discord.com/api/users/@me";

                options.Scope.Add("identify");
                options.Scope.Add("guilds");

                var guildId = configuration["Discord:GuildId"];
                var adminIds = (configuration["Dashboard:AdminDiscordIds"] ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                options.Events.OnCreatingTicket = async context =>
                {
                    using var user = await GetJsonAsync(context, context.Options.UserInformationEndpoint!);
                    var root = user.RootElement;
                    var identity = context.Identity!;

                    var discordId = root.GetProperty("id").GetString();
                    if (discordId is not null)
                    {
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, discordId));
                    }
                    AddStringClaim(identity, root, "username", ClaimTypes.Name);
                    AddStringClaim(identity, root, "global_name", GlobalNameClaim);
                    AddStringClaim(identity, root, "avatar", AvatarClaim);

                    var isAdmin = discordId is not null && adminIds.Contains(discordId);
                    var isMember = isAdmin || await IsGuildMemberAsync(context, guildId);
                    if (!isMember)
                    {
                        context.Fail("You must be a member of the GLaDOS server to sign in.");
                        return;
                    }

                    identity.AddClaim(new Claim(ClaimTypes.Role, isAdmin ? "Admin" : "Viewer"));
                };

                // Persist the cookie across browser restarts (survives for the scheme's ExpireTimeSpan).
                options.Events.OnTicketReceived = context =>
                {
                    context.Properties!.IsPersistent = true;
                    return Task.CompletedTask;
                };

                // Denied consent or non-member: send the SPA back to a friendly login screen.
                options.Events.OnRemoteFailure = context =>
                {
                    context.Response.Redirect("/dashboard/?authError=1");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
        }

        services.AddAuthorization();

        return services;
    }

    private static Task WriteStatus(Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> context, int statusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    private static void AddStringClaim(ClaimsIdentity identity, JsonElement root, string jsonKey, string claimType)
    {
        if (root.TryGetProperty(jsonKey, out var value) && value.ValueKind == JsonValueKind.String)
        {
            identity.AddClaim(new Claim(claimType, value.GetString()!));
        }
    }

    private static async Task<JsonDocument> GetJsonAsync(OAuthCreatingTicketContext context, string url)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await context.Backchannel.SendAsync(request, context.HttpContext.RequestAborted);
        response.EnsureSuccessStatusCode();

        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(context.HttpContext.RequestAborted));
    }

    private static async Task<bool> IsGuildMemberAsync(OAuthCreatingTicketContext context, string? guildId)
    {
        if (string.IsNullOrEmpty(guildId))
        {
            return false;
        }

        try
        {
            using var guilds = await GetJsonAsync(context, "https://discord.com/api/users/@me/guilds");
            return guilds.RootElement.EnumerateArray()
                .Any(guild => guild.TryGetProperty("id", out var id) && id.GetString() == guildId);
        }
        catch
        {
            return false;
        }
    }
}
