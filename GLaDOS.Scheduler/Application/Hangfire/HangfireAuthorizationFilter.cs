using System.Text;
using Hangfire.Dashboard;

namespace GLaDOS.Scheduler.Application.Hangfire;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly string _user;
    private readonly string _pass;

    public HangfireAuthorizationFilter(string user, string pass)
    {
        _user = user;
        _pass = pass;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        string header = httpContext.Request.Headers["Authorization"];
        
        if (string.IsNullOrWhiteSpace(header))
        {
            SetChallenge(httpContext);
            return false;
        }

        try
        {
            var authValues = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(header);

            if (!"Basic".Equals(authValues.Scheme, StringComparison.InvariantCultureIgnoreCase))
            {
                SetChallenge(httpContext);
                return false;
            }

            var parameter = Encoding.UTF8.GetString(Convert.FromBase64String(authValues.Parameter));
            var parts = parameter.Split(':');

            if (parts.Length < 2)
            {
                SetChallenge(httpContext);
                return false;
            }

            var username = parts[0];
            var password = parts[1];
            
            if (username == _user && password == _pass)
            {
                return true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        SetChallenge(httpContext);
        return false;
    }
    
    private void SetChallenge(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
    }
}