using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace GLaDOS.Scheduler.Application.Swagger;

public class SwaggerBasicAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public SwaggerBasicAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger"))
        {
            string authHeader = context.Request.Headers["Authorization"];

            if (authHeader != null && authHeader.StartsWith("Basic "))
            {
                try
                {
                    var headerValue = AuthenticationHeaderValue.Parse(authHeader);
                    var encoding = Encoding.UTF8;
                    var credentials = encoding.GetString(Convert.FromBase64String(headerValue.Parameter));

                    var parts = credentials.Split(':', 2);
                    var username = parts[0];
                    var password = parts[1];
                    
                    var expectedUser = _configuration["Hangfire:Username"];
                    var expectedPass = _configuration["Hangfire:Password"];
                    
                    
                    if (username == expectedUser && password == expectedPass)
                    {
                        await _next(context);
                        return;
                    }
                }
                catch
                {
                    throw new Exception("Invalid Authorization Header");
                }
            }
            
            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Swagger\"";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return;
        }
        await _next(context);
    }
}