using Hangfire.Dashboard;

namespace GLaDOS.Scheduler.Application.Hangfire;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // TODO: Implement proper authorization logic here
        return true; 
    }
}