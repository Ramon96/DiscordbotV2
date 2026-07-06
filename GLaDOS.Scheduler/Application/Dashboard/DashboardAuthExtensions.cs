using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

namespace GLaDOS.Scheduler.Application.Dashboard;

public static class DashboardAuthExtensions
{
    public static IServiceCollection AddDashboardAuth(this IServiceCollection services)
    {
        // Persist the Data Protection key ring to a mounted volume. Cookie auth encrypts the
        // cookie with these keys; without a stable key ring, every container recreation (each
        // deploy) generates new keys and invalidates existing login cookies — logging everyone
        // out on every deploy.
        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
            .SetApplicationName("glados-dashboard");

        // TLS terminates at the nginx reverse proxy, so trust its forwarded scheme/ip
        // headers; without this the app thinks every request is plain HTTP and refuses
        // to send the Secure dashboard cookie.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "glados.dashboard";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;

                // The SPA drives its own routing, so the API must answer with status codes
                // rather than redirecting to a server-rendered login page.
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        return Task.CompletedTask;
                    }

                    context.Response.Redirect(context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

        services.AddAuthorization();

        return services;
    }
}
